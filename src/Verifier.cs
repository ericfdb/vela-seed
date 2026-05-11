using System.Text;
using System.Text.Json;

namespace VelaSeed;

public static class Verifier
{
    public static async Task<int> RunAsync(SeedConfig config)
    {
        Console.WriteLine($"Target: {config.FuncUrl}");
        Console.WriteLine("Checking transaction logs for seeded data (last 24h)...");
        Console.WriteLine();

        var allPassed = true;

        foreach (var tenant in Tenants.All)
        {
            if (tenant.Seeds.Length == 0) continue;

            Console.WriteLine($"--- {tenant.Name} ({tenant.CustomerType}) ---");

            using var http = new HttpClient { BaseAddress = new Uri(config.FuncUrl) };
            http.DefaultRequestHeaders.Add("x-functions-key", config.FuncKey);
            http.DefaultRequestHeaders.Add("AuthCertificate", "UnitTest");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization",
                Identity.BuildToken(tenant.ParticipantId, tenant.CustomerType));
            http.DefaultRequestHeaders.Add("Accept", "application/json");

            var query = $@"{{
                ""CustomerParticipantIds"": ""{tenant.ParticipantId}"",
                ""TransactionType"": """",
                ""FromDate"": ""{DateTime.UtcNow.AddHours(-24):o}"",
                ""ToDate"": ""{DateTime.UtcNow.AddHours(1):o}"",
                ""PageNumber"": 1,
                ""PageSize"": 50,
                ""SortBy"": """",
                ""SearchingFieldValue"": """",
                ""ErrorCode"": """"
            }}";

            try
            {
                var response = await http.PostAsync("/api/transactionlogs",
                    new StringContent(query, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  FAIL: HTTP {(int)response.StatusCode}");
                    allPassed = false;
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync();
                var recordCount = CountRecords(body);
                var provider = Tenants.Providers[0];

                var marker = tenant.CustomerType == "EHR" ? provider.Vpi : provider.PharmacyNcpdpId;
                var found = body.Contains(marker);

                Console.WriteLine($"  Records: {recordCount}, Marker ({marker}): {(found ? "FOUND" : "NOT FOUND")}");

                if (!found || recordCount == 0)
                    allPassed = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                allPassed = false;
            }
        }

        Console.WriteLine();
        Console.WriteLine(allPassed ? "Verification PASSED" : "Verification FAILED — some data not found");
        return allPassed ? 0 : 1;
    }

    private static int CountRecords(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                return data.GetArrayLength();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.GetArrayLength();
        }
        catch { }
        return 0;
    }
}
