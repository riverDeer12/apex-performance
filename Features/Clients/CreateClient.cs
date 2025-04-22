using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using FastEndpoints;

namespace ApexPerformance.API.Features.Clients;

public record CreateClientRequest(
    string Username,
    string Password,
    string FirstName,
    string LastName
);

public record CreateClientResponse(
    Guid Id,
    string FirstName,
    string LastName
);

public class CreateClientEndpoint : Endpoint<CreateClientRequest, CreateClientResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IUserService _userService;

    public CreateClientEndpoint(ApexPerformanceContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public override void Configure()
    {
        Post("api/clients");
        Permissions(nameof(UserPermissions.CanCreateClient));
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
        var userAccount =
            await _userService.CreateUserAccount(request.Username, request.Password, request.Username,
                cancellationToken);

        var newClient = new Client
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            User = userAccount
        };

        _context.Clients.Add(newClient);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new CreateClientResponse(newClient.Id, newClient.FirstName, newClient.LastName),
            cancellation: cancellationToken);
    }
}