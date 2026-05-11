# Handoff: VelaSeed Debugging Session (2026-05-11)

## What we built
`ericfdb/vela-seed` — lean .NET 9 console app that seeds NCPDP transactions into VelaBridge test env.
Repo is live on GitHub, working code, 6 source files (~560 lines of real code + XML templates).

## Current state
- **Pharmacy tenant**: WORKS (HTTP 200, NCPDP 3000). Two transaction types seed successfully.
- **EHR tenant**: FAILS (HTTP 400 "Something went wrong!Send message function failed")
- **Verify command**: Returns 0 records for both tenants. Likely because NCPDP 3000 (rejected by Vela Network) means no audit record persists to TransactionLogs table.

## What we proved
1. Our code is correct — identical behavior to the original `vela` repo seed pipeline
2. The CRLF fix was essential — `Request.Substring(40)` in backend assumes `\r\n`
3. The JSON encoder fix was essential — `System.Text.Json` HTML-encodes `<>` by default, backend can't parse
4. Pharmacy succeeds, EHR fails, with identical code paths — only the JWT `CustomerParticipantId` differs

## The EHR mystery — what we know
- Server uses JWT's `CustomerParticipantId` to build Key Vault secret names: `Tenant-{participantId}-ClientId` and `Tenant-{participantId}-ClientSecret`
- If those secrets are missing/expired/inaccessible, `sendmessage` throws → generic "Something went wrong" HTTP 400
- We could NOT confirm the root cause because:
  - Eric's `az` account lacks Key Vault secrets list/read permissions on `erxvb-tst-be-kv-euK2` and `erxvb-tst-master-kv-eu`
  - App Insights (`erxvb-tst-be-app-insights-eu`) shows ZERO sendmessage telemetry (even for Pharmacy successes!) — only HealthCheck and our manual curl calls appear
  - ADO #87990 says "test environment is broken" (2026-04-24) and #88079 is Active in Sprint 10, but these may not be directly related

## Next steps to resolve EHR

### Option A: Get Key Vault access
In Azure Portal → Key Vault `erxvb-tst-be-kv-euK2` (RG: `erxvb-tst-be-rg-eu`):
1. Go to Access policies or RBAC
2. Add your account with "Key Vault Secrets User" role (or at minimum Get + List on secrets)
3. Then check if these secrets exist and are enabled/not-expired:
   - `Tenant-d86262842238006-ClientId`
   - `Tenant-d86262842238006-ClientSecret`
4. Compare with `Tenant-c3be396a1304111-ClientId/ClientSecret` (Pharmacy — known working)

### Option B: Ask infrastructure
Ask Josh Starr or DevOps: "Are Key Vault secrets for EHR tenant d86262842238006 still valid in erxvb-tst-be-kv-euK2?"

### Option C: Check if the password hardcoded in SeedData is stale
The EHR tenant password `IFIj4bTQsy` is hardcoded in the XML's `<SecondaryIdentification>`. If it was rotated in Key Vault but our code still has the old value, that could also cause failure. But this is less likely because:
- The 400 error is generic (server-side exception), not an NCPDP rejection
- A wrong password would likely get through to the Vela Network and come back as an NCPDP auth error, not a Function App crash

## Open question: Is verify expected to return 0?
NCPDP code 3000 = "Missing/Invalid FormerName,Address,CommunicationNumbers" means the Vela Network rejected the content. The transaction may or may not persist to TransactionLogs depending on where in the pipeline the rejection happens. If it's rejected at the Vela Network layer (after ComposeMessage), there may still be an audit record. If it's rejected before the DB write — no record. Need to test verify after fixing EHR (which should return 000/010 for NewRx).

## Repo structure
```
vela-seed/
├── .env.example          # Template (2 vars)
├── .env                  # Real secrets (gitignored)
├── .gitignore
├── README.md
├── VelaSeed.sln
└── src/
    ├── VelaSeed.csproj   # .NET 9, one NuGet (JWT)
    ├── Program.cs        # Entry: seed | verify
    ├── Config.cs         # .env loader
    ├── Identity.cs       # JWT builder (unsigned, test only)
    ├── Tenants.cs        # 2 tenants, 1 provider, 4 message types
    ├── Seeder.cs         # POST /api/sendmessage + manifest
    ├── Verifier.cs       # POST /api/transactionlogs
    └── XmlTemplates.cs   # NCPDP SCRIPT 20170715 XML builders
```

## Key technical details for future sessions
- Azure subscription: `FirstDatabank-ePrescription-Test`
- Function App: `erxvb-tst-be-func-eu` (RG: `erxvb-tst-be-rg-eu`) — Running
- App Insights: `erxvb-tst-be-app-insights-eu` (same RG) — matches instrumentation key
- Key Vaults: `erxvb-tst-be-kv-euK2` (backend), `erxvb-tst-master-kv-eu` (terraform)
- Eric lacks Key Vault secrets permissions on both
- `gh auth` must use `ericfdb` account for pushes
