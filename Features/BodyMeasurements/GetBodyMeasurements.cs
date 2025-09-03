using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record GetBodyMeasurementResponse(
    Guid Id,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    decimal Glutes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PersonDataDto Client);

public class GetBodyMeasurementsEndpoint : EndpointWithoutRequest<List<GetBodyMeasurementResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBodyMeasurementsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/body-measurements");
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var bodyMeasurements = await GetBodyMeasurementsForUser(cancellationToken);

        await SendAsync(bodyMeasurements.Select(x =>
                new GetBodyMeasurementResponse(x.Id,
                    x.Height, x.Weight, x.Shoulders,
                    x.Chest, x.UpperArm, x.Waist, x.Thigh,
                    x.Calves, x.Glutes, x.CreatedAt, x.UpdatedAt,
                    new PersonDataDto(x.Client.Id, x.Client.FirstName, x.Client.LastName)))
            .ToList(), cancellation: cancellationToken);
    }

    private async Task<List<BodyMeasurement>> GetBodyMeasurementsForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllBodyMeasurements(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachBodyMeasurements(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Client))
            return await GetClientBodyMeasurements(cancellationToken);

        return new List<BodyMeasurement>();
    }


    private async Task<List<BodyMeasurement>> GetAllBodyMeasurements(CancellationToken cancellationToken)
    {
        return await _context.BodyMeasurements
            .Where(x => !x.IsDeleted)
            .Include(x => x.Client)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<BodyMeasurement>> GetClientBodyMeasurements(CancellationToken cancellationToken)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
                cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var bodyMeasurements = await _context.BodyMeasurements
            .Where(x => x.ClientId == client.Id && !x.IsDeleted)
            .Include(bodyMeasurement => bodyMeasurement.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        return bodyMeasurements;
    }

    private async Task<List<BodyMeasurement>> GetCoachBodyMeasurements(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var clientIds = await _context.CoachClients.Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        if (clientIds.Count == 0) return [];

        var bodyMeasurements = await _context.BodyMeasurements
            .Where(x => clientIds.Contains(x.ClientId) && !x.IsDeleted)
            .Include(bodyMeasurement => bodyMeasurement.Client)
            .ToListAsync(cancellationToken: cancellationToken);

        return bodyMeasurements;
    }
}