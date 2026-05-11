# VelaSeed

Seeds test data into the VelaBridge test environment. Sends NCPDP SCRIPT 20170715 transactions through the Azure Function App API.

## What it does

Sends 4 transaction types through 2 tenants:

| Tenant | Sends | Direction |
|--------|-------|-----------|
| EHR | NewRx, CancelRx | EHR → Pharmacy |
| Pharmacy | RxRenewalRequest, RxChangeRequest | Pharmacy → EHR |

Each transaction hits the VelaBridge Function App, which forwards to the Vela Network (NCPDP).

## Setup

1. Install [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Copy `.env.example` to `.env` and fill in the function key (ask Eric)
3. Run it

## Usage

```bash
# Seed test data
dotnet run -- seed

# Verify data in transaction logs (only works for transactions that pass NCPDP validation)
dotnet run -- verify
```

## Expected output

```
Target: https://erxvb-tst-be-func-eu.azurewebsites.net
Tenants: 2, Providers: 1

--- EHR (testemr1velatest) (EHR) ---
  [NewRx] VPI=1e13c9f2... FAILED HTTP 400 (1200ms)    ← Known: test env .NET 9 upgrade pending
  [CancelRx] VPI=1e13c9f2... FAILED HTTP 400 (900ms)

--- Pharmacy Aggregator Child (Pharmacy) ---
  [RxRenewalRequest] VPI=1e13c9f2... OK — NCPDP 3000 (2500ms)  ← Accepted by VelaBridge
  [RxChangeRequest] VPI=1e13c9f2... OK — NCPDP 3000 (1300ms)

Results: 2 accepted, 2 failed.
```

**NCPDP 3000** = "Missing/Invalid FormerName,Address,CommunicationNumbers" — the transaction reached the Vela Network but was rejected for missing prescriber fields. VelaBridge still records it as a processed transaction.

**EHR HTTP 400** = the test environment's Key Vault credentials for the EHR tenant are disrupted by the .NET 9 upgrade (ADO #88079). This will resolve when the deployment completes.

## How it works

1. Loads `.env` (Function App URL + key)
2. Builds a JWT with tenant identity (the Function App reads claims but doesn't validate the signature)
3. Constructs NCPDP SCRIPT XML for each transaction type
4. POSTs to `/api/sendmessage` with function key + JWT + XML payload
5. Writes `seed-manifest.json` with results

## Environment

- **Target**: `erxvb-tst-be-func-eu` (Test, East US)
- **Auth**: Azure Function key + unsigned JWT (test environment only)
- **Tenants**: EHR `d86262842238006`, Pharmacy `c3be396a1304111`

## Architecture notes

- The Function App resolves Vela Network credentials from Key Vault using the JWT's `CustomerParticipantId` claim: `Tenant-{participantId}-ClientId/ClientSecret`
- XML payloads must use CRLF line endings — the backend does `Request.Substring(40)` assuming 38-char XML declaration + 2-byte CRLF
- `templateId=1` + `templateTypeId=3` ensures the transaction is saved to the TransactionLogs table
