using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record UpdateClientRequest(string FirstName, string LastName);
public record UpdateClientResponse(Guid Id, string FirstName, string LastName);

public class UpdateClientEndpoint : Endpoint<UpdateClientRequest, UpdateClientResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateClientEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/clients/{id}");
        Permissions(nameof(UserPermissions.CanUpdateClient));
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var clientId = Route<Guid>("id", isRequired: true);

        var client =
            await _context.Clients
                .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        client.FirstName = request.FirstName;
        client.LastName = request.LastName;

        _context.Clients.Update(client);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new(client.Id, client.FirstName, client.LastName), cancellation: cancellationToken);
    }
}