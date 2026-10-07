using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.MonthlyReviews;

public class GetMonthlyReviewsRequest
{
    // Required for staff, clients always get their own reviews.
    [QueryParam] public Guid? ClientId { get; set; }
}

/// <summary>
/// Monthly reviews, newest month first. Clients get their own,
/// coaches and administrators the reviews of the given client.
/// </summary>
public class GetMonthlyReviewsEndpoint : Endpoint<GetMonthlyReviewsRequest, List<MonthlyReviewResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMonthlyReviewsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/monthly-reviews");
        Options(x => x.WithTags("MonthlyReviews"));
    }

    public override async Task HandleAsync(GetMonthlyReviewsRequest request, CancellationToken cancellationToken)
    {
        Guid? clientId;

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
        {
            clientId = await _context.Clients
                .Where(x => x.UserId == _currentUserService.UserId)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            clientId = request.ClientId;

            if (clientId is null ||
                !await TrainingAccess.CanManageClient(_context, _currentUserService, clientId.Value,
                    cancellationToken))
                ThrowError(ErrorCodes.NotFound);
        }

        if (clientId is null)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var reviews = await _context.MonthlyReviews
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync(cancellationToken);

        await SendAsync(reviews.Select(MonthlyReviewResponse.From).ToList(), cancellation: cancellationToken);
    }
}
