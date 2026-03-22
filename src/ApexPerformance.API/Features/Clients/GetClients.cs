using System.Text.Json;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Shared.DataTransferObjects.Clients;
using FastEndpoints;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public class GetClientsEndpoint : EndpointWithoutRequest<List<ClientDataDto>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/clients");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = await GetClientsForUser(cancellationToken);

        if (clients.Count is 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var lastCreditIncreaseList = await GetLastClientCreditIncreases(clients.Select(x => x.Id).ToList());

        await SendAsync(clients.Select(x =>
            new ClientDataDto(x.Id, x.FirstName, x.LastName,
                x.Credits, x.Phone, x.Email, x.CreatedAt,
                x.UpdatedAt,
                lastCreditIncreaseList
                    .FirstOrDefault(creditIncrease => creditIncrease.ClientId == x.Id)?.LastCreditsIncreaseDate,
                x.FullName,
                x.Coaches.Select(coach => new PersonDataDto(coach.Coach.Id, coach.Coach.FirstName,
                        coach.Coach.LastName, coach.Coach.FullName))
                    .ToList(),
                x.BodyMeasurements
                    .Select(bodyMeasurement =>
                        new BodyMeasurementDto(
                            bodyMeasurement.Id,
                            bodyMeasurement.Height,
                            bodyMeasurement.Weight,
                            bodyMeasurement.Shoulders,
                            bodyMeasurement.Chest,
                            bodyMeasurement.UpperArm,
                            bodyMeasurement.Waist,
                            bodyMeasurement.Thigh,
                            bodyMeasurement.Calves,
                            bodyMeasurement.Glutes,
                            bodyMeasurement.CreatedAt
                        )).ToList())).ToList(), cancellation: cancellationToken);
    }

    private async Task<List<Client>> GetClientsForUser(CancellationToken cancellationToken)
    {
        if (_currentUserService.LoggedUserHasRole(UserRoles.SuperAdmin) ||
            _currentUserService.LoggedUserHasRole(UserRoles.Administrator))
            return await GetAllClients(cancellationToken);

        if (_currentUserService.LoggedUserHasRole(UserRoles.Coach))
            return await GetCoachClients(cancellationToken);

        return [];
    }

    private async Task<List<Client>> GetAllClients(CancellationToken cancellationToken)
    {
        return await _context.Clients
            .Include(x => x.Coaches)
            .ThenInclude(x => x.Coach)
            .Include(x => x.BodyMeasurements)
            .OrderBy(x => x.Credits)
            .ThenBy(x => x.UpdatedAt)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<Client>> GetCoachClients(CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError("Coach is not found", StatusCodes.Status400BadRequest);

        var coachClientsIds = await _context.CoachClients
            .Where(x => x.CoachId == coach.Id)
            .Select(x => x.ClientId)
            .ToListAsync(cancellationToken: cancellationToken);

        return await _context.Clients
            .Where(x => coachClientsIds.Contains(x.Id))
            .Include(x => x.Coaches)
            .ThenInclude(x => x.Coach)
            .Include(x => x.BodyMeasurements)
            .OrderBy(x => x.Credits)
            .ThenBy(x => x.UpdatedAt)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    private async Task<List<ClientLastCreditIncreaseDto>> GetLastClientCreditIncreases(List<Guid> clientIds)
    {
        var idsJson = JsonSerializer.Serialize(clientIds); // List<Guid>

        var param = new SqlParameter("@idsJson", idsJson);

        return await _context.Set<ClientLastCreditIncreaseDto>()
            .FromSqlRaw(@"
                WITH ids AS (
                    SELECT CAST([value] AS uniqueidentifier) AS Id
                    FROM OPENJSON(@idsJson)
                ),
                history AS (
                    SELECT c.Id,
                           c.Credits,
                           c.PeriodStart,
                           LAG(c.Credits) OVER (PARTITION BY c.Id ORDER BY c.PeriodStart) AS PrevCredits
                    FROM dbo.Clients FOR SYSTEM_TIME ALL AS c
                    JOIN ids ON ids.Id = c.Id
                ),
                increases AS (
                    SELECT *,
                           ROW_NUMBER() OVER (PARTITION BY Id ORDER BY PeriodStart DESC) AS rn
                    FROM history
                    WHERE PrevCredits IS NOT NULL
                      AND Credits > PrevCredits
                )
                SELECT
                    Id AS ClientId,
                    PeriodStart AS LastCreditIncreaseDate,
                    Credits AS NewCredits,
                    PrevCredits
                FROM increases
                WHERE rn = 1
            ", param)
            .ToListAsync();
    }
}