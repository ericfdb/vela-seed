# VelaSeed

Seeds test data into the VelaBridge test environment. Sends NCPDP SCRIPT 20170715 transactions through the Azure Function App API and verifies they appear in transaction logs.

## What it does

Sends 4 transaction types through 2 tenants:

| Tenant | Sends | Direction | Status |
|--------|-------|-----------|--------|
| EHR | NewRx, CancelRx | EHR → Pharmacy | Blocked (KV creds) |
| Pharmacy | RxRenewalRequest, RxChangeRequest | Pharmacy → EHR | Working (NCPDP 3000) |

Each transaction hits the VelaBridge Function App, which authenticates via Okta, forwards to the Vela Network (NCPDP), and records the result in TransactionLogs.

## Setup

1. Install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Copy `.env.example` to `.env` and fill in the function key (ask Eric)
3. Run it

## Usage

```bash
# Seed all tenants
dotnet run --project src -- seed

# Seed Pharmacy only (skip EHR which is blocked on infra)
SKIP_EHR=1 dotnet run --project src -- seed

# Verify data in transaction logs
dotnet run --project src -- verify
```

## Expected output

```
Target: https://erxvb-tst-be-func-eu.azurewebsites.net
Tenants: 2, Providers: 1

--- EHR (testemr1velatest) (EHR) ---
  [NewRx] VPI=1e13c9f2... FAILED HTTP 400 (700ms)
  [CancelRx] VPI=1e13c9f2... FAILED HTTP 400 (400ms)

--- Pharmacy Aggregator Child (Pharmacy) ---
  [RxRenewalRequest] VPI=1e13c9f2... OK — NCPDP 3000 (4900ms)
  [RxChangeRequest] VPI=1e13c9f2... OK — NCPDP 3000 (1100ms)

Results: 2 accepted, 2 failed.
```

## Transaction log verification

```
--- Pharmacy Aggregator Child (Pharmacy) ---
  Records: 8, Marker (3198466): FOUND
```

Pharmacy transactions are recorded in VelaBridge's TransactionLogs and visible in the UI, even with NCPDP 3000 error responses.

## Known issues

### EHR HTTP 400 — Key Vault credentials (BLOCKED)

**Root cause** (100% confirmed via App Insights): The EHR tenant's Key Vault secrets (`Tenant-d86262842238006-ClientId/ClientSecret`) contain 32-char hex tokens instead of Okta-format credentials (`0oaXXXX`). The auth function (`AuthType=OKTA`) forwards them to Okta which rejects with 401. VelaBridge BE then crashes parsing the non-JSON error response.

**Fix required**: Dev/infra must provision an Okta app for EHR tenant and update Key Vault secrets. See `HANDOFF.md` for full evidence chain.

### Pharmacy NCPDP 3000 — Prescriber profile incomplete

**Root cause** (confirmed via investigation): The receiving endpoint validates the prescriber's REGISTERED profile in the Vela Network directory. VPI `1e13c9f21725207` lacks FormerName, Address, and CommunicationNumbers in its directory registration. This is a receiver-side validation — our XML content is irrelevant because the receiver looks up the prescriber by VPI, not from the message body.

**Evidence**:
- Same error with `requestType="Testing"` (FDBIdUpdater replaces prescriber data from DB)
- Same error with `requestType="Compose"` (XML sent unmodified)
- Transactions still record to TransactionLogs regardless of NCPDP response code

**Fix**: Update prescriber profile in Vela Network directory to include FormerName, Address, and CommunicationNumbers for VPI `1e13c9f21725207`.

## How it works

1. Loads `.env` (Function App URL + key)
2. Builds a JWT with tenant identity (the Function App reads claims but doesn't validate the signature)
3. Constructs NCPDP SCRIPT XML for each transaction type
4. POSTs to `/api/sendmessage` with function key + JWT + XML payload
5. Writes `seed-manifest.json` with results

## Architecture notes

- **Target**: `erxvb-tst-be-func-eu` (Test, East US)
- **Auth**: Azure Function key + unsigned JWT (test environment only)
- **Tenants**: EHR `d86262842238006`, Pharmacy `c3be396a1304111`
- **Rate limit**: APIM gateway limits auth calls to ~4-6/min. Wait 5+ minutes between runs.
- The Function App resolves Vela Network credentials from Key Vault using the JWT's `CustomerParticipantId` claim
- XML payloads must use CRLF line endings (backend does `Request.Substring(40)` assuming 38-char XML declaration + 2-byte CRLF)
- `templateTypeId=3` (TestTemplate) ensures the transaction is saved to TransactionLogs
- `requestType="Testing"` triggers `FDBIdUpdater` which replaces Prescriber fields (NPI, DEA, Name, Address, CommNums) with values from VelaBridge DB via XPath
- `SKIP_EHR=1` env var skips EHR tenant to preserve rate limit budget

## File structure

```
vela-seed/
├── .env.example          # Template (VB_FUNC_URL, VB_FUNC_KEY)
├── .env                  # Real secrets (gitignored)
├── .gitignore
├── README.md             # This file
├── HANDOFF.md            # Investigation context and evidence
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
