using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record CreateBodyMeasurementRequest(
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

public record CreateBodyMeasurementResponse(
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

public record BodyMeasurementClientDto(
    Guid Id,
    string Firstname,
    string Lastname
);

public class CreateBodyMeasurementEndpoint : Endpoint<CreateBodyMeasurementRequest, CreateBodyMeasurementResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateBodyMeasurementEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/body-measurements");
        Permissions(nameof(UserPermissions.CanCreateBodyMeasurement));
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CreateBodyMeasurementRequest request, CancellationToken cancellationToken)
    {
        var client =
            await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client,
                cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorMessages.NotFound);

        var bodyMeasurement = new BodyMeasurement
        {
            Height = request.Height,
            Weight = request.Weight,
            Shoulders = request.Shoulders,
            Chest = request.Chest,
            UpperArm = request.UpperArm,
            Waist = request.Waist,
            Thigh = request.Thigh,
            Calves = request.Calves,
            Client = client
        };

        _context.BodyMeasurements.Add(bodyMeasurement);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(
            new CreateBodyMeasurementResponse(
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

public sealed class CreateBodyMeasurementValidator : Validator<CreateBodyMeasurementRequest>
{
    public CreateBodyMeasurementValidator()
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