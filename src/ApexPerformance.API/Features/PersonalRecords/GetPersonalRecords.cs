using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.PersonalRecords;

public record GetPersonalRecordsRequest(Guid? ClientId);

public record PersonalRecordResponse(
    Guid Id,
    Guid ClientId,
    Guid WorkoutId,
    // Persisted JSON with workout name translations.
    string WorkoutName,
    Guid TrainingId,
    string Type,
    decimal Value,
    decimal Weight,
    decimal? Reps,
    DateTimeOffset AchievedAt
);

/// <summary>
/// Record history of a client, newest first. Coaches see records
/// of their clients, clients only their own (client id is ignored).
/// </summary>
public class GetPersonalRecordsEndpoint : Endpoint<GetPersonalRecordsRequest, List<PersonalRecordResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPersonalRecordsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/personal-records");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("PersonalRecords"));
    }

    public override async Task HandleAsync(GetPersonalRecordsRequest request, CancellationToken cancellationToken)
    {
        Guid clientId;

        if (TrainingAccess.IsAdministrator(_currentUserService) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Coach))
        {
            if (request.ClientId is null)
                ThrowError(ErrorCodes.Required);

            if (!await TrainingAccess.CanManageClient(_context, _currentUserService, request.ClientId.Value,
                    cancellationToken))
                ThrowError(ErrorCodes.NotFound);

            clientId = request.ClientId.Value;
        }
        else
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

            if (client is null)
                ThrowError(ErrorCodes.NotFound);

            clientId = client.Id;
        }

        var records = await _context.PersonalRecords
            .AsNoTracking()
            .Where(x => x.ClientId == clientId)
            .OrderByDescending(x => x.AchievedAt)
            .Select(x => new PersonalRecordResponse(x.Id, x.ClientId, x.WorkoutId, x.Workout.Name, x.TrainingId,
                x.Type, x.Value, x.Weight, x.Reps, x.AchievedAt))
            .ToListAsync(cancellationToken);

        await SendAsync(records, cancellation: cancellationToken);
    }
}
