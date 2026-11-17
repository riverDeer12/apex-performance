using ApexPerformance.API.Features.Profile;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public class GetClientCoachesEndpoint : EndpointWithoutRequest<List<PersonDataDto>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetClientCoachesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/coaches/client");
        Options(x => x.WithTags("Coaches"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        var clientCoaches = _context.CoachClients.Where(x => x.ClientId == client.Id)
            .Include(coachClient => coachClient.Coach).ToList();

        if (clientCoaches.Count is 0)
            ThrowError(ErrorCodes.NotFound);

        var pictures = await UserPictures.LoadAsync(_context, clientCoaches.Select(x => x.Coach.UserId),
            cancellationToken);

        await SendAsync(
            clientCoaches.Select(x =>
                new PersonDataDto(x.CoachId, x.Coach.FirstName, x.Coach.LastName, x.Coach.FullName,
                    PictureUrl: pictures.UrlFor(x.Coach.UserId))).ToList(),
            cancellation: cancellationToken);
    }
}