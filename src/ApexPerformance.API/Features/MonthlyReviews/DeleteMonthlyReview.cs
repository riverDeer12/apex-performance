using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.MonthlyReviews;

public record DeleteMonthlyReviewResponse(Guid Id);

public class DeleteMonthlyReviewEndpoint : EndpointWithoutRequest<DeleteMonthlyReviewResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMonthlyReviewEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/monthly-reviews/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("MonthlyReviews"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var reviewId = Route<Guid>("id", isRequired: true);

        var review = await _context.MonthlyReviews.FirstOrDefaultAsync(x => x.Id == reviewId, cancellationToken);

        if (review is null ||
            !await TrainingAccess.CanManageClient(_context, _currentUserService, review.ClientId, cancellationToken))
            ThrowError(ErrorCodes.NotFound);

        review.Delete();

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(new DeleteMonthlyReviewResponse(review.Id), cancellation: cancellationToken);
    }
}
