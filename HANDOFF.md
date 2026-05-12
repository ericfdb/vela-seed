# Handoff: VelaSeed — Final State (2026-05-12)

## Mission Status

| Goal | Status | Evidence |
|------|--------|----------|
| Pharmacy transactions in TransactionLogs | DONE | `verify` finds 8 records, marker FOUND |
| Pharmacy NCPDP success (000/010) | BLOCKED | Prescriber profile incomplete in Vela Network |
| EHR transactions | BLOCKED | Key Vault has wrong credential format |
| Seed pipeline presentable to coworker | DONE | Clean code, README, documented blockers |

## Bug 1: EHR KV Credentials (100% CONFIRMED)

**Root cause**: `Tenant-d86262842238006-ClientId/ClientSecret` in Key Vault are 32-char hex tokens. The auth service is configured `AuthType=OKTA` and forwards them to Okta, which rejects (401). VelaBridge BE parses the non-JSON 401 body → `InvalidOperationException` at `ComposeMessageService.cs:line 475`.

**Evidence chain (all from 2026-05-12, independently reproducible)**:

1. BE App Insights: `Cannot access child value on Newtonsoft.Json.Linq.JValue` at line 475
2. Auth endpoint App Insights: `POST /auth/token` → 401 at matching timestamps
3. Key Vault values:
   - EHR: `ClientId=4258115485f6e442868e78b3b818f42b` (32-char hex — WRONG)
   - Pharmacy: `ClientId=0oa1s3crploFsjlpJ1d8` (Okta `0oa` prefix — CORRECT)
4. Auth function config: `AuthType=OKTA`, domain `fdbvelaidentitynp.okta.com`
5. Historical: NEVER worked in 67 days of available telemetry

**Architecture** (no per-tenant branching — always OAuth):
```
VelaBridge BE → KV: Tenant-{pid}-ClientId/ClientSecret
             → POST erx-test-us-east.azure-api.net/auth/token (JSON body)
             → Auth function (AuthType=OKTA) → HTTP Basic to Okta
             → Okta rejects hex creds → 401 (non-JSON body)
             → BE: JToken.Parse(error_body) → JValue not JObject
             → tokenParseResult["access_token"] → InvalidOperationException
             → HandleException → HTTP 400 "Send message function failed"
```

**NOT a .NET 9 regression**: Same crash in both .NET 6 (line 472) and .NET 9 (line 475) deployments.

**Fix required (dev/infra)**: Provision an Okta app for EHR tenant `d86262842238006`, update Key Vault secrets with real `0oaXXXX` ClientId + Okta secret.

**ADO**: Related to #75719 (Auth token: use Okta instead of PBM secrets).

## Issue 2: Pharmacy NCPDP 3000 (100% CONFIRMED — Provider Config)

**Root cause**: The receiving endpoint validates the prescriber's REGISTERED profile in the Vela Network directory. VPI `1e13c9f21725207` lacks FormerName, Address, and CommunicationNumbers in its Vela Network directory registration.

**Evidence chain**:

1. `requestType="Testing"` (FDBIdUpdater replaces prescriber data from VelaBridge DB) → NCPDP 3000
2. `requestType="Compose"` (no substitution, XML sent as-is with full prescriber data) → NCPDP 3000
3. Adding `<FormerName>` to XML template → NCPDP 3000 (unchanged)
4. `templateTypeId=2` (custom template, no XPath substitution) → NCPDP 3000
5. Full NCPDP response shows `From Qualifier="ZZZ"` with TertiaryIdentification — message traverses network successfully
6. Error text explicitly names the missing fields: "Missing/Invalid FormerName,Address,CommunicationNumbers"
7. `ProviderXpathConstants` in KB confirms `FDBIdUpdater` XPath set does NOT include FormerName — only addresses NPI, DEA, Name, Address, CommNums

**Conclusion**: The NCPDP 3000 is a **receiver-side validation** of the sender's registered prescriber profile, NOT a validation of the XML message content. No amount of XML template changes will fix this.

**Fix required (dev/infra)**: Update prescriber registration for VPI `1e13c9f21725207` in Vela Network directory to include FormerName, Address, and CommunicationNumbers.

**Transactions still log**: Despite NCPDP 3000, VelaBridge saves the transaction to TransactionLogs (confirmed by `verify` finding 8 records). The pipeline WORKS end-to-end; only the receiver rejects the prescriber's profile.

## What's Working

- Pharmacy auth: `c3be396a1304111` → Okta → 200 → access_token ✓
- Network traversal: XML sent to Vela Network, routed to receiver, response returned ✓
- TransactionLogs: Records saved with `templateTypeId=3` ✓
- Seed pipeline: clean, documented, handles errors gracefully ✓
- Verify command: confirms records in transaction logs ✓
- SKIP_EHR flag: preserves rate limit budget ✓

## What Eric Should Tell the Dev Team

1. **EHR is blocked on KV credentials**: "The EHR tenant `d86262842238006` has hex tokens in Key Vault instead of Okta credentials. Need a real Okta app provisioned. See App Insights `erxvb-tst-be-app-insights-eu` — every call to `/auth/token` for this tenant returns 401."

2. **Pharmacy needs prescriber profile update**: "Our prescriber VPI `1e13c9f21725207` (NPI `1942991914`) is missing FormerName, Address, and CommunicationNumbers in its Vela Network directory registration. The receiving endpoint validates the registered profile and rejects with NCPDP 3000. Transactions still log — we just need the profile completed for NCPDP 000."

## Key Discovery: FDBIdUpdater Behavior

`FDBIdUpdater` (triggered by `requestType="Testing"`) replaces these XPath nodes with VelaBridge DB values:
- Header: `TertiaryIdentification`, `SecondaryIdentification`
- Prescriber: `NPI`, `DEANumber`, `LastName`, `FirstName`
- Prescriber Address: `AddressLine1`, `AddressLine2`, `City`, `StateProvince`, `PostalCode`, `CountryCode`
- Prescriber CommNums: `PrimaryTelephone/Number`

It does NOT add/replace: `FormerName`, `BusinessName` (at Prescriber level), or any Patient/Pharmacy fields.

Source: `ProviderXpathConstants` in `FDBVelaBridge.Model.Helper`

## Rate Limiting

**Critical operational constraint**: APIM gateway at `erx-test-us-east.azure-api.net` rate limits `/auth/token` to ~4-6 calls per 5-minute window.

- After hitting the limit: returns 429 (plain text body)
- VelaBridge BE tries `JToken.Parse("Rate limit...")` → `JsonReaderException` → HTTP 400
- **Always wait 5+ minutes between seed runs**
- Use `SKIP_EHR=1` to halve the auth call budget

## Operational Commands

```bash
# Seed (Pharmacy only)
cd repos/vela-seed
SKIP_EHR=1 dotnet run --project src -- seed

# Verify
dotnet run --project src -- verify

# BE App Insights (sendmessage traces)
az monitor app-insights query --app erxvb-tst-be-app-insights-eu --resource-group erxvb-tst-be-rg-eu \
  --analytics-query "traces | where timestamp > ago(10m) | where severityLevel >= 2 | project timestamp, message | order by timestamp desc" -o json

# Auth endpoint (rate limit / auth results)
az monitor app-insights query --app erx-test-us-east --resource-group erx-test-backend-us-east \
  --analytics-query "requests | where timestamp > ago(10m) | where name == 'POST /auth/token' | project timestamp, resultCode, duration | order by timestamp desc" -o json

# Key Vault (verify credentials format)
az keyvault secret show --vault-name erx-test-us-east-hu9ng --name "Tenant-d86262842238006-ClientId" --query "value" -o tsv
az keyvault secret show --vault-name erx-test-us-east-hu9ng --name "Tenant-c3be396a1304111-ClientId" --query "value" -o tsv
```

## KB Chunks Referenced

| ID | Content |
|----|---------|
| `856cfb04` | ComposeMessageService.GetVelaNetworkResponseAsync (auth flow, FDBIdUpdater gate) |
| `8cb09ad7` | ComposeMessageService.SendComposeMessageAsync (templateTypeId → SaveTransactionLogs gate) |
| `849f3b3f` | ProviderXpathConstants (exact fields FDBIdUpdater replaces) |
| `b55149e9` | RequestandMessageTypeMapping migration (routing table: MessageTypeId+RequestTypeId → APIUrl) |
| `16df74d8` | DataSeedPharmacyXPath migration (MessageTypeFieldsMapping table) |
| `acf793b5` | ComposeMessageFunction entry point (SendComposeMessage function) |

## ADO Items

- **#88079** — VB API .NET 9 Upgrade QA Part 2 (Sprint 10, Active, Eric)
- **#88084** — Get Stage EHR Baseline (child of #88079, Blocked on KV creds)
- **#75719** — Auth token: use Okta instead of PBM secrets (New, never implemented — context for KV issue)
