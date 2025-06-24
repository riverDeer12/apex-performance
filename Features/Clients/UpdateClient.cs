using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using EFCore.BulkExtensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record UpdateClientRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits,
    List<Guid> Coaches
);

public record UpdateClientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits);

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
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.Credits = request.Credits;

        _context.Clients.Update(client);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await AddClientCoaches(client, request.Coaches, cancellationToken);

        await SendAsync(new(client.Id, client.FirstName,
                client.LastName, client.Email, client.Phone, client.Credits),
            cancellation: cancellationToken);
    }

    private async Task AddClientCoaches(Client client, List<Guid> coachesIds, CancellationToken cancellationToken)
    {
        if (coachesIds.Count == 0) return;

        var coaches = await _context.Coaches
            .Where(x => coachesIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var clientCoaches = coaches
            .Select(coach => new CoachClient
            {
                Client = client,
                ClientId = client.Id,
                CoachId = coach.Id,
                Coach = coach
            }).ToList();

        await _context.BulkInsertOrUpdateAsync(clientCoaches, cancellationToken: cancellationToken);
    }
}

public sealed class UpdateClientValidator : Validator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Phone).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Coaches).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}