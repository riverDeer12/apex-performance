using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.ClientGoals;

/// <summary>
/// Goal and plan of one client, for the client's coaches and administrators.
/// </summary>
public class GetClientGoalEndpoint : EndpointWithoutRequest<ClientGoalResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientGoalEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/client-goals/{clientId}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("ClientGoals"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clientId = Route<Guid>("clientId", isRequired: true);

        if (!await _context.Clients.AnyAsync(x => x.Id == clientId, cancellationToken) ||
            !await TrainingAccess.CanManageClient(_context, _currentUserService, clientId, cancellationToken))
            ThrowError(ErrorCodes.NotFound);

        var goal = await _context.ClientGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken);

        await SendAsync(ClientGoalResponse.From(clientId, goal), cancellation: cancellationToken);
    }
}

/// <summary>
/// Goal and plan of the logged client.
/// </summary>
public class GetMyClientGoalEndpoint : EndpointWithoutRequest<ClientGoalResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyClientGoalEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/client-goals/my");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("ClientGoals"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clientId = await _context.Clients
            .Where(x => x.UserId == _currentUserService.UserId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (clientId is null)
            ThrowError(ErrorCodes.NotFound);

        var goal = await _context.ClientGoals
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken);

        await SendAsync(ClientGoalResponse.From(clientId.Value, goal), cancellation: cancellationToken);
    }
}
