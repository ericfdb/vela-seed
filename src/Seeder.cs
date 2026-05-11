using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VelaSeed;

public static class Seeder
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public static async Task<int> RunAsync(SeedConfig config)
    {
        Console.WriteLine($"Target: {config.FuncUrl}");
        Console.WriteLine($"Tenants: {Tenants.All.Length}, Providers: {Tenants.Providers.Length}");
        Console.WriteLine();

        var successes = 0;
        var failures = 0;
        var results = new List<SeedResult>();

        foreach (var tenant in Tenants.All)
        {
            Console.WriteLine($"--- {tenant.Name} ({tenant.CustomerType}) ---");

            if (tenant.Seeds.Length == 0)
            {
                Console.WriteLine("  No seeds configured, skipping.");
                continue;
            }

            var token = Identity.BuildToken(tenant.ParticipantId, tenant.CustomerType);

            foreach (var seed in tenant.Seeds)
            {
                foreach (var provider in Tenants.Providers)
                {
                    var result = await Send(config, provider, seed, token, tenant.Password);
                    results.Add(result);

                    if (result.Success)
                        successes++;
                    else
                        failures++;
                }
            }
            Console.WriteLine();
        }

        WriteManifest(results);

        Console.WriteLine();
        Console.WriteLine($"Results: {successes} accepted, {failures} failed.");
        if (failures > 0)
            Console.WriteLine("Note: EHR tenant failures are expected if the test environment .NET 9 upgrade is pending.");
        return failures > 0 && successes == 0 ? 1 : 0;
    }

    private static async Task<SeedResult> Send(SeedConfig config, Provider provider, SeedMessage seed, string token, string password)
    {
        var messageId = $"seed-{provider.Vpi[..6]}-{DateTime.UtcNow:yyMMddHHmmss}";
        var sentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
        var xml = XmlTemplates.Build(seed, provider, messageId, sentTime, password);
        xml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n");

        var payload = new ComposeRequest
        {
            MessageTypeId = seed.MessageTypeId,
            RequestTypeId = seed.RequestTypeId,
            TemplateId = 1,
            TemplateTypeId = seed.TemplateTypeId,
            RequestType = "Testing",
            PayloadFormat = "xml",
            Request = xml,
        };

        Console.Write($"  [{seed.MessageType}] VPI={provider.Vpi[..8]}... ");

        using var http = new HttpClient { BaseAddress = new Uri(config.FuncUrl), Timeout = Timeout };
        http.DefaultRequestHeaders.Add("x-functions-key", config.FuncKey);
        http.DefaultRequestHeaders.Add("AuthCertificate", "UnitTest");
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        try
        {
            var json = JsonSerializer.Serialize(payload, SerializerContext.Relaxed.ComposeRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            var response = await http.PostAsync("/api/sendmessage", content);
            sw.Stop();

            var body = await response.Content.ReadAsStringAsync();
            var ncpdpCode = ExtractTag(body, "DescriptionCode");
            if (ncpdpCode == "?") ncpdpCode = ExtractTag(body, "Code");

            if (response.IsSuccessStatusCode)
            {
                var ok = ncpdpCode is "?" or "000" or "010";
                Console.WriteLine(ok
                    ? $"OK ({sw.ElapsedMilliseconds}ms)"
                    : $"OK — NCPDP {ncpdpCode} ({sw.ElapsedMilliseconds}ms)");
                return new SeedResult(seed.MessageType, messageId, true, (int)response.StatusCode, ncpdpCode, sw.ElapsedMilliseconds);
            }

            Console.WriteLine($"FAILED HTTP {(int)response.StatusCode} ({sw.ElapsedMilliseconds}ms)");
            if (body.Length > 0)
                Console.WriteLine($"       {body[..Math.Min(body.Length, 200)]}");
            return new SeedResult(seed.MessageType, messageId, false, (int)response.StatusCode, ncpdpCode, sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("TIMEOUT (30s)");
            return new SeedResult(seed.MessageType, messageId, false, 0, null, 30000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            return new SeedResult(seed.MessageType, messageId, false, 0, null, 0);
        }
    }

    private static void WriteManifest(List<SeedResult> results)
    {
        var manifest = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            results = results.Select(r => new
            {
                r.MessageType,
                r.MessageId,
                r.Success,
                r.HttpStatus,
                r.NcpdpCode,
                r.ElapsedMs,
            }).ToArray(),
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(Directory.GetCurrentDirectory(), "seed-manifest.json");
        File.WriteAllText(path, json);
        Console.WriteLine($"Manifest: {path}");
    }

    private static string ExtractTag(string s, string tag)
    {
        var start = s.IndexOf($"<{tag}>");
        var end = s.IndexOf($"</{tag}>");
        return start >= 0 && end > start ? s[(start + tag.Length + 2)..end] : "?";
    }
}

public record SeedResult(string MessageType, string MessageId, bool Success, int HttpStatus, string? NcpdpCode, long ElapsedMs);

public record ComposeRequest
{
    [JsonPropertyName("messageTypeId")] public int MessageTypeId { get; init; }
    [JsonPropertyName("requestTypeId")] public int RequestTypeId { get; init; }
    [JsonPropertyName("templateId")] public int TemplateId { get; init; }
    [JsonPropertyName("templateTypeId")] public int TemplateTypeId { get; init; }
    [JsonPropertyName("requestType")] public string RequestType { get; init; } = "";
    [JsonPropertyName("payloadFormat")] public string PayloadFormat { get; init; } = "";
    [JsonPropertyName("request")] public string Request { get; init; } = "";
}

[JsonSerializable(typeof(ComposeRequest))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
public partial class SerializerContext : JsonSerializerContext
{
    private static SerializerContext? _relaxed;
    public static SerializerContext Relaxed => _relaxed ??= new SerializerContext(new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });
}
