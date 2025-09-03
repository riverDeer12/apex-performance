using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.FunctionalMovementScreens;

public record CreateFunctionalMovementScreenRequest(
    Guid Client,
    string DeepSquat,
    string HurdleStep,
    string InLineLunge,
    string ActiveStraightLegRaise,
    string TrunkStabilityPushUp,
    string RotaryStability,
    string ShoulderMobility
);

public class CreateFunctionalMovementScreenEndpoint : Endpoint<CreateFunctionalMovementScreenRequest, int>
{
    private readonly ApexPerformanceContext _context;

    public CreateFunctionalMovementScreenEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/functional-movement-screens");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("FunctionalMovementScreens"));
    }

    public override async Task HandleAsync(CreateFunctionalMovementScreenRequest request, CancellationToken cancellationToken)
    {
        var client = await _context.Clients
            .SingleAsync(x => x.Id == request.Client, cancellationToken: cancellationToken);

        var newFunctionalMovementScreen = new FunctionalMovementScreen
        {
            DeepSquat = request.DeepSquat,
            HurdleStep = request.HurdleStep,
            InLineLunge = request.InLineLunge,
            ActiveStraightLegRaise = request.ActiveStraightLegRaise,
            TrunkStabilityPushUp = request.TrunkStabilityPushUp,
            RotaryStability = request.RotaryStability,
            ShoulderMobility = request.ShoulderMobility,
            Client = client,
            ClientId = client.Id
        };

        _context.FunctionalMovementScreens.Add(newFunctionalMovementScreen);
        
        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(StatusCodes.Status201Created, cancellation: cancellationToken);
    }
}

public sealed class CreateFunctionalMovementScreenValidator
    : Validator<CreateFunctionalMovementScreenRequest>
{
    public CreateFunctionalMovementScreenValidator()
    {
        RuleFor(x => x.Client)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MustAsync((id, cancellationToken)
                =>
            {
                var db = Resolve<ApexPerformanceContext>();
                return db.Clients.AnyAsync(client => client.Id == id, cancellationToken);
            })
            .WithMessage(ErrorMessages.NotFound);

        RuleFor(x => x.DeepSquat).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.HurdleStep).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.InLineLunge).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.ActiveStraightLegRaise).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.TrunkStabilityPushUp).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.RotaryStability).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.ShoulderMobility).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}