# Handoff: VelaSeed — Status & Next Steps (2026-05-12)

## Goal
Get NCPDP transactions seeded into VelaBridge test environment so they appear in the UI transaction logs. Currently blocked — zero transactions visible in VelaBridge.

## Current Seed Output
```
EHR (NewRx)           → HTTP 400 "Send message function failed"   ← BLOCKED (infra)
EHR (CancelRx)        → HTTP 400 "Send message function failed"   ← BLOCKED (infra)
Pharmacy (RxRenewal)  → HTTP 200, NCPDP 3000                      ← NEEDS INVESTIGATION
Pharmacy (RxChange)   → HTTP 200, NCPDP 3000                      ← NEEDS INVESTIGATION
```

## Bug 1: EHR KV credentials are not Okta format (CONFIRMED, 100% confidence)

**Root cause**: `Tenant-d86262842238006-ClientId/ClientSecret` in Key Vault are 32-char hex tokens. The auth service is configured `AuthType=OKTA` and forwards them to Okta, which rejects (401). VelaBridge BE parses the non-JSON 401 body → `InvalidOperationException: Cannot access child value on Newtonsoft.Json.Linq.JValue` at `ComposeMessageService.GetVelaNetworkResponseAsync:line 475`.

**Evidence (all from 2026-05-12, reproducible)**:
- BE App Insights traces: exact stack trace with timestamp
- Old backend App Insights: `POST /auth/token` → 401 at matching timestamp
- Key Vault values directly compared:
  - EHR: `ClientId=4258115485f6e442868e78b3b818f42b` (hex, wrong)
  - Pharmacy: `ClientId=0oa1s3crploFsjlpJ1d8` (Okta `0oa` prefix, correct)
- Auth function: `AuthType=OKTA`, domain `fdbvelaidentitynp.okta.com`
- Historical: NEVER worked in 67 days of available telemetry

**Architecture** (no per-tenant branching — always OAuth):
```
VelaBridge BE → KV: Tenant-{pid}-ClientId/ClientSecret
             → POST erx-test-us-east.azure-api.net/auth/token (JSON body)
             → Auth function (AuthType=OKTA) → HTTP Basic to Okta
             → Okta rejects hex creds → 401
             → BE: JToken.Parse(error_body) → crash → HTTP 400 to client
```

**Fix required (dev/infra)**: Provision an Okta app for EHR tenant, update KV secrets.

**NOT a .NET 9 regression**: Same crash in both old (.NET 6, line 472) and new (.NET 9, line 475) deployments.

## Issue 2: Pharmacy NCPDP 3000 — status UNKNOWN

**Symptom**: HTTP 200 (full pipeline works: auth → Vela Network → routes to destination → response returned). But the receiving endpoint returns NCPDP error:
```xml
<Code>900</Code>
<DescriptionCode>3000</DescriptionCode>
<Description>Missing/Invalid FormerName,Address,CommunicationNumbers</Description>
```

**What we tried (did NOT help)**:
- Added `<CountryCode>US</CountryCode>` to Prescriber Address
- Added full Pharmacy `<Address>` block to RxChangeRequest
- Still 3000 after fix

**What we know**:
- Rejection comes from the RECEIVING endpoint (mock EHR), not VelaBridge
- Response `From Qualifier="ZZZ"` confirms full network traversal
- Our templates DO have Address and CommunicationNumbers — receiver still rejects
- "FormerName" is an element we never include (unknown if required)

**Hypotheses for next agent to test (ordered by likelihood)**:
1. VelaBridge does server-side XPath substitution on templates (`templateTypeId=3` = system template). It may REPLACE our prescriber data with whatever's in its DB for that provider. If the DB has incomplete prescriber info, the forwarded message lacks those fields regardless of what we send.
   - **Test**: Change `templateTypeId` from 3 to 2 (custom template, no server substitution) in `Tenants.cs`
2. The prescriber NPI `1942991914` is not fully registered in the receiving mock EHR's system — it needs FormerName/Address/CommNums in ITS database, not just in our XML.
   - **Test**: Browse VelaBridge UI → Providers → check provider profile for VPI `1e13c9f21725207`
3. The VPI/provider combination needs specific prescriber data configured in VelaBridge's provider setup.
   - **Test**: Search KB for "XPath" + "prescriber" to understand what VelaBridge does to the template before forwarding

**This may not be a bug we file** — it could be provider/environment configuration.

## What was WRONG in the previous handoff (corrected)

| Previous claim | Reality |
|---------------|---------|
| "DecryptPayloadService crash" is Bug 1 | DecryptPayloadService is NOT in the sendmessage path. It's a separate endpoint. |
| "sendmessage has 0 invocations" | Wrong — we now have App Insights proof of SendComposeMessage executing |
| ".NET 9 migration regression" | NOT a regression — never worked. Same crash in .NET 6 deploy. |
| "AuthenticationType branching lost" | There is NO branching. Code always does OAuth. KV secrets are the problem. |
| "Pharmacy NCPDP 3000 is our template" | Partially wrong — template fixes didn't resolve it. Likely server-side or receiver-side. |

## What Eric can fix himself

1. **Iterate on NCPDP 3000** — try `templateTypeId=2` in Tenants.cs, check provider config in UI
2. **Clean up debug output** — Seeder.cs line ~101 currently prints 1500-char body for NCPDP errors (useful for debugging, remove when done)
3. **Add rate-limit delay** if needed (`await Task.Delay(3000)` between calls)
4. **CancelRx Prescriber Address** — still missing entirely (won't matter until EHR auth fixed)
5. **Commit and push** the XML template improvements already made

## What needs dev/infra (bring to devs with evidence)

1. **EHR Okta credentials** — need real `0oaXXXX` client_id + Okta secret in KV for tenant `d86262842238006`
2. **Possibly**: provider profile data for VPI `1e13c9f21725207` if NCPDP 3000 turns out to be server-side config

## Operational constraints

- **APIM rate limit**: ~4-6 calls/minute to `/auth/token`. After that, 429 (plaintext body → different crash). Wait 3+ minutes between seed runs.
- **App Insights data**: Only 1 day retained on BE function (low traffic). Run seed → query within minutes.
- **Two App Insights resources**: BE (`erxvb-tst-be-app-insights-eu`) vs old backend (`erx-test-us-east`). Sendmessage logs on BE, auth/routing logs on old backend.

## Techniques for next agent

### Running the seed
```bash
cd repos/vela-seed
dotnet run --project src -- seed     # Send transactions (wait 3min between runs!)
dotnet run --project src -- verify   # Check transaction logs
```

### App Insights (primary diagnostic)
```bash
# BE function — sendmessage traces
az monitor app-insights query --app erxvb-tst-be-app-insights-eu --resource-group erxvb-tst-be-rg-eu \
  --analytics-query "traces | where timestamp > ago(10m) | where severityLevel >= 2 | project timestamp, message | order by timestamp desc" -o json

# Old backend — auth endpoint results
az monitor app-insights query --app erx-test-us-east --resource-group erx-test-backend-us-east \
  --analytics-query "requests | where timestamp > ago(10m) | where name contains 'auth' | project timestamp, name, resultCode" -o json
```

### Key Vault
```bash
az keyvault secret show --vault-name erx-test-us-east-hu9ng --name "Tenant-d86262842238006-ClientId" --query "value" -o tsv
```

### Function app config
```bash
az functionapp config appsettings list --name erx-test-auth-us-east --resource-group erx-test-backend-us-east \
  --query "[?contains(name,'Auth') || contains(name,'okta')].{name:name, value:value}" -o table
```

### KB (Otto MCP)
Key chunks from this session:
- `856cfb04` — ComposeMessageService.GetVelaNetworkResponseAsync (full auth flow code)
- `44c9ea8e` — TokenProcessor.GenerateToken (OKTA vs AAD switch)
- `8f484a20` — WI-88079 .NET 9 upgrade QA
- `16df74d8` — DataSeedPharmacyXPath migration (server-side template substitution)

### Gotchas
- `python` not `python3` on this Windows machine
- Key Vault list on large vaults: 30+ seconds — query by name/contains
- BE App Insights only sees HealthCheck + SendComposeMessage
- `erxvb-tst-be-func-eu` DOES host sendmessage (previous handoff was wrong about this)
- Always wait 3+ min between seed runs (APIM rate limit)

## ADO items
- **#88079** — VB API .NET 9 Upgrade QA Part 2 (Sprint 10, Active, Eric)
- **#88084** — Get Stage EHR Baseline (child of #88079, Blocked)
- **#87695** — QA Testing Tech Stack Upgrade Part 3 (Sprint 10, New, Eric)
- **#75719** — Auth token: use Okta instead of PBM secrets (New, never implemented — relevant context for the KV creds issue)

## Repo structure
```
vela-seed/
├── .env.example          # Template (VB_FUNC_URL, VB_FUNC_KEY)
├── .env                  # Real secrets (gitignored)
├── .gitignore
├── README.md
├── HANDOFF.md            # This file
├── VelaSeed.sln
├── seed-manifest.json    # Output from last run
└── src/
    ├── VelaSeed.csproj   # .NET 9, one NuGet (JWT)
    ├── Program.cs        # Entry: seed | verify
    ├── Config.cs         # .env loader + walk-up search
    ├── Identity.cs       # JWT builder (unsigned — func app doesn't validate sigs)
    ├── Tenants.cs        # 2 tenants, 1 provider, 4 message types
    ├── Seeder.cs         # POST /api/sendmessage + manifest writer
    ├── Verifier.cs       # POST /api/transactionlogs
    └── XmlTemplates.cs   # NCPDP SCRIPT 20170715 XML builders (4 types)
```
