using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Features.ClientGoals;

/// <summary>
/// Client's goal and plan. All fields are empty
/// while the coach hasn't written it yet.
/// </summary>
public record ClientGoalResponse(
    Guid ClientId,
    string? Goal,
    string? CurrentBlock,
    string? Focus,
    string? NextAssessment,
    DateTimeOffset? NextAssessmentDate,
    DateTimeOffset? UpdatedAt
)
{
    public static ClientGoalResponse From(Guid clientId, ClientGoal? goal) => new(
        clientId,
        goal?.Goal,
        goal?.CurrentBlock,
        goal?.Focus,
        goal?.NextAssessment,
        goal?.NextAssessmentDate,
        goal?.UpdatedAt);
}
