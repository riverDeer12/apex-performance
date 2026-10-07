using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Features.MonthlyReviews;

public record MonthlyReviewResponse(
    Guid Id,
    Guid ClientId,
    int Year,
    int Month,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
)
{
    public static MonthlyReviewResponse From(MonthlyReview review) => new(
        review.Id,
        review.ClientId,
        review.Year,
        review.Month,
        review.Content,
        review.CreatedAt,
        review.UpdatedAt);
}
