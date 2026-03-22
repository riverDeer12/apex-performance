namespace ApexPerformance.API.Shared.DataTransferObjects.Clients;

public sealed record ClientLastCreditIncreaseDto
{
    public Guid ClientId { get; init; }
    public DateTime LastCreditsIncreaseDate { get; init; }
    public int NewCredits { get; init; }
    public int PrevCredits { get; init; }
}