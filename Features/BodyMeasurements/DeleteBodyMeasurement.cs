using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record DeleteBodyMeasurementResponse(
    Guid Id
);

public class DeleteBodyMeasurementEndpoint : EndpointWithoutRequest<DeleteBodyMeasurementResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly IClientService _clientService;

    public DeleteBodyMeasurementEndpoint(ApexPerformanceContext context, IClientService clientService)
    {
        _context = context;
        _clientService = clientService;
    }

    public override void Configure()
    {
        Delete("api/body-measurements/{id}");
        Permissions(nameof(UserPermissions.CanDeleteBodyMeasurement));
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var bodyMeasurementId = Route<Guid>("id", isRequired: true);

        var bodyMeasurement =
            await _context.BodyMeasurements
                .FirstOrDefaultAsync(x => x.Id == bodyMeasurementId, cancellationToken: cancellationToken);

        if (bodyMeasurement is null)
            ThrowError(ErrorMessages.NotFound);

        bodyMeasurement.Delete();

        _context.BodyMeasurements.Update(bodyMeasurement);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);
            
        await  SendAsync(
            new DeleteBodyMeasurementResponse(bodyMeasurement.Id),
            cancellation: cancellationToken);
    }
}