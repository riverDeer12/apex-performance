using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.ClientGoals;

public record UpdateClientGoalRequest(
    string? Goal,
    string? CurrentBlock,
    string? Focus,
    string? NextAssessment,
    DateTimeOffset? NextAssessmentDate
);

/// <summary>
/// Coach writes the client's goal and plan, the client is notified.
/// </summary>
public class UpdateClientGoalEndpoint : Endpoint<UpdateClientGoalRequest, ClientGoalResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;

    public UpdateClientGoalEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
    }

    public override void Configure()
    {
        Put("api/client-goals/{clientId}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("ClientGoals"));
    }

    public override async Task HandleAsync(UpdateClientGoalRequest request, CancellationToken cancellationToken)
    {
        var clientId = Route<Guid>("clientId", isRequired: true);

        var client = await _context.Clients.FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken);

        if (client is null ||
            !await TrainingAccess.CanManageClient(_context, _currentUserService, clientId, cancellationToken))
            ThrowError(ErrorCodes.NotFound);

        var goal = await _context.ClientGoals.FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken);

        if (goal is null)
        {
            goal = new ClientGoal { Client = client };
            _context.ClientGoals.Add(goal);
        }

        goal.Goal = Clean(request.Goal);
        goal.CurrentBlock = Clean(request.CurrentBlock);
        goal.Focus = Clean(request.Focus);
        goal.NextAssessment = Clean(request.NextAssessment);
        goal.NextAssessmentDate = request.NextAssessmentDate;

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        BackgroundJob.Enqueue(() => SendClientGoalNotification(client.UserId));

        await SendAsync(ClientGoalResponse.From(clientId, goal), cancellation: cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [AutomaticRetry(Attempts = 0)]
    public async Task SendClientGoalNotification(Guid clientUserId)
    {
        var clientDeviceTokens = await _context.DeviceTokens
            .Where(x => x.UserId == clientUserId)
            .Select(t => t.Token)
            .ToListAsync();

        _ = await _notificationService.SendToMultipleDevices(clientDeviceTokens,
            "Goal and plan",
            "Your coach updated your goal and plan.",
            PushNotificationTypes.Data(PushNotificationTypes.ClientGoal));
    }
}

public sealed class UpdateClientGoalValidator : Validator<UpdateClientGoalRequest>
{
    public UpdateClientGoalValidator()
    {
        RuleFor(x => x.Goal).MaximumLength(1000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.CurrentBlock).MaximumLength(1000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Focus).MaximumLength(1000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.NextAssessment).MaximumLength(500).WithMessage(ErrorCodes.NotValid);
    }
}
