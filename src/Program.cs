using VelaSeed;

EnvLoader.Load();

var config = new SeedConfig();

if (args.Length == 0)
{
    Console.WriteLine("VelaBridge Test Data Seeder");
    Console.WriteLine();
    Console.WriteLine("Usage: dotnet run -- <command>");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  seed     Send test transactions to VelaBridge (NewRx, CancelRx, RxRenewal, RxChange)");
    Console.WriteLine("  verify   Check transaction logs to confirm seeded data arrived");
    return 1;
}

try { config.Validate(); }
catch (Exception ex)
{
    Console.WriteLine($"Config error: {ex.Message}");
    Console.WriteLine("Create a .env file with VB_FUNC_URL and VB_FUNC_KEY. See .env.example.");
    return 1;
}

return args[0].ToLowerInvariant() switch
{
    "seed" => await Seeder.RunAsync(config),
    "verify" => await Verifier.RunAsync(config),
    _ => Error($"Unknown command: {args[0]}")
};

static int Error(string msg) { Console.WriteLine($"ERROR: {msg}"); return 1; }
