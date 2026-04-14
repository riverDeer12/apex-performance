using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Shared.DataTransferObjects;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record UpdateBodyMeasurementRequest(
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    decimal Glutes
);

public class UpdateBodyMeasurementEndpoint : Endpoint<UpdateBodyMeasurementRequest, StatusResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateBodyMeasurementEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/body-measurements/{id}");
        Permissions(UserPermissions.CanUpdateBodyMeasurement);
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(UpdateBodyMeasurementRequest request, CancellationToken cancellationToken)
    {
        var bodyMeasurementId = Route<Guid>("id", isRequired: true);

        var bodyMeasurement = await _context.BodyMeasurements
            .FirstOrDefaultAsync(x => x.Id == bodyMeasurementId, cancellationToken: cancellationToken);

        if (bodyMeasurement is null)
            ThrowError(ErrorCodes.NotFound);

        bodyMeasurement.Height = request.Height;
        bodyMeasurement.Weight = request.Weight;
        bodyMeasurement.Shoulders = request.Shoulders;
        bodyMeasurement.Chest = request.Chest;
        bodyMeasurement.UpperArm = request.UpperArm;
        bodyMeasurement.Waist = request.Waist;
        bodyMeasurement.Thigh = request.Thigh;
        bodyMeasurement.Calves = request.Calves;
        bodyMeasurement.Glutes = request.Glutes;

        _context.BodyMeasurements.Update(bodyMeasurement);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync(
            new StatusResponse(
                bodyMeasurement.Id, true
            ),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateBodyMeasurementValidator : Validator<UpdateBodyMeasurementRequest>
{
    public UpdateBodyMeasurementValidator()
    {
        RuleFor(x => x.Height).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Weight).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Shoulders).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Chest).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.UpperArm).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Waist).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Thigh).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Calves).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}