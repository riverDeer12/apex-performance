public sealed record ClientLastCreditIncreaseDto
{
    public Guid ClientId { get; init; }
    public DateTime LastCreditIncreaseDate { get; init; }
    public int NewCredits { get; init; }
    public int PrevCredits { get; init; }
}