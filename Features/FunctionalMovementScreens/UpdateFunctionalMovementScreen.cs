using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.FunctionalMovementScreens;

public record UpdateFunctionalMovementScreenRequest(
    Guid Client,
    string DeepSquat,
    string HurdleStep,
    string InLineLunge,
    string ActiveStraightLegRaise,
    string TrunkStabilityPushUp,
    string RotaryStability,
    string ShoulderMobility
);

public class UpdateFunctionalMovementScreenEndpoint : Endpoint<UpdateFunctionalMovementScreenRequest, int>
{
    private readonly ApexPerformanceContext _context;

    public UpdateFunctionalMovementScreenEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/functional-movement-screens/{id}");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("FunctionalMovementScreens"));
    }

    public override async Task HandleAsync(UpdateFunctionalMovementScreenRequest request,
        CancellationToken cancellationToken)
    {
        var functionalMovementScreenId = Route<Guid>("id", isRequired: true);

        var functionalMovementScreen = await _context.FunctionalMovementScreens
            .FirstOrDefaultAsync(x => x.Id == functionalMovementScreenId,
                cancellationToken: cancellationToken);

        if (functionalMovementScreen is null)
            ThrowError(ErrorMessages.NotFound);
        
        var client = await _context.Clients
            .SingleAsync(x => x.Id == request.Client, cancellationToken: cancellationToken);

        functionalMovementScreen.DeepSquat = request.DeepSquat;
        functionalMovementScreen.HurdleStep = request.HurdleStep;
        functionalMovementScreen.InLineLunge = request.InLineLunge;
        functionalMovementScreen.ActiveStraightLegRaise = request.ActiveStraightLegRaise;
        functionalMovementScreen.TrunkStabilityPushUp = request.TrunkStabilityPushUp;
        functionalMovementScreen.RotaryStability = request.RotaryStability;
        functionalMovementScreen.ShoulderMobility = request.ShoulderMobility;
        functionalMovementScreen.Client = client;
        functionalMovementScreen.ClientId = client.Id;

        _context.FunctionalMovementScreens.Update(functionalMovementScreen);

        var result = await _context.SaveChangesAsync(cancellationToken: cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(StatusCodes.Status200OK, cancellation: cancellationToken);
    }
}

public sealed class UpdateFunctionalMovementScreenValidator
    : Validator<UpdateFunctionalMovementScreenRequest>
{
    public UpdateFunctionalMovementScreenValidator()
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