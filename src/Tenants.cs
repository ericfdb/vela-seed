namespace VelaSeed;

public record Tenant(string ParticipantId, string CustomerType, string Name, string Password, SeedMessage[] Seeds);
public record SeedMessage(string MessageType, int MessageTypeId, int RequestTypeId, int TemplateTypeId);
public record Provider(string Vpi, string PrescriberNpi, string PrescriberDea, string PharmacyNcpdpId, string PharmacyNpi)
{
    public string PharmacyNpiXml => string.IsNullOrEmpty(PharmacyNpi) ? "" : $"<NPI>{PharmacyNpi}</NPI>";
    public string PrescriberNpiXml => string.IsNullOrEmpty(PrescriberNpi) ? "" : $"<NPI>{PrescriberNpi}</NPI>";
}

/// <summary>
/// Test environment tenants and providers. These are real IDs in the VelaBridge test environment.
/// </summary>
public static class Tenants
{
    public static readonly Provider[] Providers =
    [
        new("1e13c9f21725207", "1942991914", "FA5623740", "3198466", "1235156548"),
    ];

    public static readonly Tenant[] All =
    [
        new("d86262842238006", "EHR", "EHR (testemr1velatest)", "IFIj4bTQsy",
        [
            new("NewRx", 1, 1, 3),
            new("CancelRx", 4, 1, 3),
        ]),
        new("c3be396a1304111", "Pharmacy", "Pharmacy Aggregator Child", "2ZiqoFiHEc",
        [
            new("RxRenewalRequest", 2, 1, 3),
            new("RxChangeRequest", 6, 1, 3),
        ]),
    ];
}
