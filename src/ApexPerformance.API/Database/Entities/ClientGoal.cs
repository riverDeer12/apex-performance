using ApexPerformance.API.Database.Entities.Abstract;

namespace ApexPerformance.API.Database.Entities;

/// <summary>
/// Client's current goal and plan, written by the coach and
/// shown to the client: goal, current block (this month),
/// focus and the next assessment.
/// </summary>
public class ClientGoal : BaseEntity
{
    public string? Goal { get; set; }
    public string? CurrentBlock { get; set; }
    public string? Focus { get; set; }

    // What the next assessment is (e.g. FMS and body measurements) and when.
    public string? NextAssessment { get; set; }
    public DateTimeOffset? NextAssessmentDate { get; set; }

    public required Client Client { get; set; }
    public Guid ClientId { get; set; }
}
