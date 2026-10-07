using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.MonthlyReviews;

public record SaveMonthlyReviewRequest(
    Guid Client,
    int Year,
    int Month,
    string Content
);

/// <summary>
/// Coach writes the review of a client's month. There is one review
/// per client and month, saving it again updates it.
/// </summary>
public class SaveMonthlyReviewEndpoint : Endpoint<SaveMonthlyReviewRequest, MonthlyReviewResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public SaveMonthlyReviewEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public override void Configure()
    {
        Put("api/monthly-reviews");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("MonthlyReviews"));
    }

    public override async Task HandleAsync(SaveMonthlyReviewRequest request, CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client, cancellationToken);

        if (client is null ||
            !await TrainingAccess.CanManageClient(_context, _currentUserService, client.Id, cancellationToken))
            ThrowError(ErrorCodes.NotFound);

        var review = await _context.MonthlyReviews.FirstOrDefaultAsync(x =>
            x.ClientId == client.Id && x.Year == request.Year && x.Month == request.Month, cancellationToken);

        var isNew = review is null;

        if (review is null)
        {
            review = new MonthlyReview
            {
                Client = client,
                Year = request.Year,
                Month = request.Month,
                Content = request.Content.Trim()
            };
            _context.MonthlyReviews.Add(review);
        }
        else
        {
            review.Content = request.Content.Trim();
        }

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        // Client is notified about a new review, not about every correction.
        if (isNew)
            BackgroundJob.Enqueue(() => SendMonthlyReviewNotification(client.UserId));

        await SendAsync(MonthlyReviewResponse.From(review), cancellation: cancellationToken);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendMonthlyReviewNotification(Guid clientUserId)
    {
        var clientDeviceTokens = await _context.DeviceTokens
            .Where(x => x.UserId == clientUserId)
            .Select(t => t.Token)
            .ToListAsync();

        _ = await _notificationService.SendToMultipleDevices(clientDeviceTokens,
            "Monthly review",
            "Your coach wrote the review of your month.",
            PushNotificationTypes.Data(PushNotificationTypes.MonthlyReview));
    }
}

public sealed class SaveMonthlyReviewValidator : Validator<SaveMonthlyReviewRequest>
{
    public SaveMonthlyReviewValidator()
    {
        RuleFor(x => x.Client).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Content).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(4000).WithMessage(ErrorCodes.NotValid);
    }
}
