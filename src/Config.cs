namespace VelaSeed;

public sealed class SeedConfig
{
    public string FuncUrl { get; } = Environment.GetEnvironmentVariable("VB_FUNC_URL") ?? "";
    public string FuncKey { get; } = Environment.GetEnvironmentVariable("VB_FUNC_KEY") ?? "";

    public void Validate()
    {
        if (string.IsNullOrEmpty(FuncUrl))
            throw new InvalidOperationException("VB_FUNC_URL is not set.");
        if (string.IsNullOrEmpty(FuncKey))
            throw new InvalidOperationException("VB_FUNC_KEY is not set.");
    }
}

public static class EnvLoader
{
    public static void Load()
    {
        var path = FindEnvFile();
        if (path == null) return;

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
            var eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            var key = trimmed[..eq].Trim();
            var val = trimmed[(eq + 1)..].Trim();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, val);
        }
    }

    private static string? FindEnvFile()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = start;
            while (dir != null)
            {
                var candidate = Path.Combine(dir, ".env");
                if (File.Exists(candidate)) return candidate;
                dir = Directory.GetParent(dir)?.FullName;
            }
        }
        return null;
    }
}
