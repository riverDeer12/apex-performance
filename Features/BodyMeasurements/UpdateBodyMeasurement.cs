using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record UpdateBodyMeasurementRequest(
    Guid Client,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves
);

public record UpdateBodyMeasurementResponse(
    Guid Id,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    BodyMeasurementClientDto Client
);

public class UpdateBodyMeasurementEndpoint : Endpoint<UpdateBodyMeasurementRequest, UpdateBodyMeasurementResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateBodyMeasurementEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/body-measurements/{id}");
        Permissions(nameof(UserPermissions.CanUpdateBodyMeasurement));
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(UpdateBodyMeasurementRequest request, CancellationToken cancellationToken)
    {
        var bodyMeasurementId = Route<Guid>("id", isRequired: true);
        
        var bodyMeasurement = await _context.BodyMeasurements.FirstOrDefaultAsync(x => x.Id == bodyMeasurementId,
            cancellationToken: cancellationToken);
        
        if (bodyMeasurement is null)
            ThrowError(ErrorMessages.NotFound);

        var client =
            await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client,
                cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        bodyMeasurement.Height = request.Height;
        bodyMeasurement.Weight = request.Weight;
        bodyMeasurement.Shoulders = request.Shoulders;
        bodyMeasurement.Chest = request.Chest;
        bodyMeasurement.UpperArm = request.UpperArm;
        bodyMeasurement.Waist = request.Waist;
        bodyMeasurement.Thigh = request.Thigh;
        bodyMeasurement.Calves = request.Calves;
        bodyMeasurement.Client = client;

        _context.BodyMeasurements.Update(bodyMeasurement);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new UpdateBodyMeasurementResponse(
                bodyMeasurement.Id,
                bodyMeasurement.Height,
                bodyMeasurement.Weight,
                bodyMeasurement.Shoulders,
                bodyMeasurement.Chest,
                bodyMeasurement.UpperArm,
                bodyMeasurement.Waist,
                bodyMeasurement.Thigh,
                bodyMeasurement.Calves,
                new BodyMeasurementClientDto(client.Id, client.FirstName, client.LastName)
            ),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateBodyMeasurementValidator : Validator<UpdateBodyMeasurementRequest>
{
    public UpdateBodyMeasurementValidator()
    {
        RuleFor(x => x.Client).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Height).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Weight).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Shoulders).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Chest).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.UpperArm).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Waist).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Thigh).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Calves).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}