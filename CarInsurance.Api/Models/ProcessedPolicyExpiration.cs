namespace CarInsurance.Api.Models;

public class ProcessedPolicyExpiration
{
    public long Id { get; set; }

    // PK-ul poliței expirate pe care am procesat-o
    public long PolicyId { get; set; }

    // Data de sfârșit a poliței (redundanță utilă pt. debugging)
    public DateOnly EndDate { get; set; }

    // Când am procesat (UTC)
    public DateTime ProcessedAtUtc { get; set; }
}
