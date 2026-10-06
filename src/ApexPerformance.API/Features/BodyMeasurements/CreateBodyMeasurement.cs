using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.BodyMeasurements;

public record CreateBodyMeasurementRequest(
    Guid Client,
    // Asked only at the first measurement, later the
    // client's height from the last measurement is used.
    decimal? Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    decimal Glutes
);

public record CreateBodyMeasurementResponse(
    Guid Id
);

public record BodyMeasurementClientDto(
    Guid Id,
    string FirstName,
    string LastName
);

public class CreateBodyMeasurementEndpoint : Endpoint<CreateBodyMeasurementRequest, CreateBodyMeasurementResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly INotificationService _notificationService;

    public CreateBodyMeasurementEndpoint(ApexPerformanceContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public override void Configure()
    {
        Post("api/body-measurements");
        Permissions(UserPermissions.CanCreateBodyMeasurement);
        Options(x => x.WithTags("BodyMeasurements"));
    }

    public override async Task HandleAsync(CreateBodyMeasurementRequest request, CancellationToken cancellationToken)
    {
        var client =
            await _context.Clients.FirstOrDefaultAsync(x => x.Id == request.Client,
                cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        var height = request.Height is > 0
            ? request.Height.Value
            : await _context.BodyMeasurements
                .Where(x => x.ClientId == client.Id && x.Height > 0)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => (decimal?)x.Height)
                .FirstOrDefaultAsync(cancellationToken);

        if (height is null)
            ThrowError(ErrorCodes.Required);

        var bodyMeasurement = new BodyMeasurement
        {
            Height = height.Value,
            Weight = request.Weight,
            Shoulders = request.Shoulders,
            Chest = request.Chest,
            UpperArm = request.UpperArm,
            Waist = request.Waist,
            Thigh = request.Thigh,
            Calves = request.Calves,
            Client = client,
            Glutes = request.Glutes
        };

        _context.BodyMeasurements.Add(bodyMeasurement);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        BackgroundJob.Enqueue(() => SendBodyMeasurementFcmNotification(client.UserId));

        await SendAsync(
            new CreateBodyMeasurementResponse(
                bodyMeasurement.Id),
            cancellation: cancellationToken);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendBodyMeasurementFcmNotification(Guid clientUserId)
    {
        var clientDeviceTokens = await _context.DeviceTokens
            .Where(x => x.UserId == clientUserId)
            .Select(t => t.Token)
            .ToListAsync();

        _ = await _notificationService.SendToMultipleDevices(clientDeviceTokens,
            "New body measurement",
            "Your coach added a new body measurement.",
            PushNotificationTypes.Data(PushNotificationTypes.BodyMeasurement));
    }
}

public sealed class CreateBodyMeasurementValidator : Validator<CreateBodyMeasurementRequest>
{
    public CreateBodyMeasurementValidator()
    {
        RuleFor(x => x.Client)
            .NotEmpty().WithMessage(ErrorCodes.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                
                return db.Clients.AnyAsync(client => client.Id == id, cancellationToken);
            })
            .WithMessage(ErrorCodes.NotFound);
        
        RuleFor(x => x.Height).GreaterThan(0).WithMessage(ErrorCodes.NotValid)
            .When(x => x.Height is not null);
        RuleFor(x => x.Weight).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Shoulders).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Chest).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.UpperArm).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Waist).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Thigh).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Calves).NotEmpty().WithMessage(ErrorCodes.Required);
    }
}