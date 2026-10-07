using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Coach's review of the client's progress in one month,
/// written before the 1st of the next month for the client to read.
/// </summary>
public class MonthlyReview : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public required string Content { get; set; }

    public required Client Client { get; set; }
    public Guid ClientId { get; set; }
}
