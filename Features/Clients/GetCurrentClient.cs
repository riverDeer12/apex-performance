using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record GetCurrentClientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits,
    PersonDataDto User
);

public class GetCurrentClientEndpoint : EndpointWithoutRequest<GetCurrentClientResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentClientEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/clients/current-client");
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId,
            cancellationToken: cancellationToken);
        
        if (user is null)
            ThrowError(ErrorMessages.NotFound);

        var userResponse = new PersonDataDto(user.Id, user.UserName, user.Email);

        await SendAsync(new GetCurrentClientResponse(
            client.Id,
            client.FirstName,
            client.LastName,
            client.Email,
            client.Phone,
            client.Credits,
            userResponse
            ), cancellation: cancellationToken);
    }
}