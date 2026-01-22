using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record UpdateClientRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits,
    List<Guid>? Coaches
);

public record UpdateClientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits
);

public class UpdateClientEndpoint : Endpoint<UpdateClientRequest, UpdateClientResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IEmailService _emailService;
    private readonly IClientService _clientService;

    public UpdateClientEndpoint(ApexPerformanceContext context, IClientService clientService,
        IEmailService emailService)
    {
        _context = context;
        _clientService = clientService;
        _emailService = emailService;
    }

    public override void Configure()
    {
        Put("api/clients/{id}");
        Permissions(UserPermissions.CanUpdateClient);
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(UpdateClientRequest request, CancellationToken cancellationToken)
    {
        var clientId = Route<Guid>("id", isRequired: true);

        var client =
            await _context.Clients
                .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        client.FirstName = request.FirstName;
        client.LastName = request.LastName;
        client.Email = request.Email;
        client.Phone = request.Phone;
        client.Credits = request.Credits;

        _context.Clients.Update(client);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        if (request.Coaches is null)
        {
            await SendAsync(
                new(client.Id, client.FirstName, client.LastName, client.Email, client.Phone, client.Credits),
                cancellation: cancellationToken);
            return;
        }

        await _clientService.UpdateClientCoaches(client, request.Coaches, cancellationToken);

        BackgroundJob.Enqueue(() => SendAlertEmails(client.Id));

        await SendAsync(new(client.Id, client.FirstName, client.LastName, client.Email, client.Phone, client.Credits),
            cancellation: cancellationToken);
    }

    public async Task SendAlertEmails(Guid clientId)
    {
        var client = await _context.Clients.SingleAsync(x => x.Id == clientId);

        switch (client.Credits)
        {
            case 0:
                _emailService.SendNoCreditsEmail(client);
                break;
            case 1:
                _emailService.SendLowCreditsAlertEmail(client);
                break;
        }
    }
}

public sealed class UpdateClientValidator : Validator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Phone).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}