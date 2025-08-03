using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services;
using EFCore.BulkExtensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Coaches;

public record UpdateCoachRequest(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    List<Guid> Clients
);

public record UpdateCoachResponse(
    Guid Id
);

public class UpdateCoachEndpoint : Endpoint<UpdateCoachRequest, UpdateCoachResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICoachService _coachService;

    public UpdateCoachEndpoint(ApexPerformanceContext context, ICoachService coachService)
    {
        _context = context;
        _coachService = coachService;
    }

    public override void Configure()
    {
        Put("api/coaches/{id}");
        Roles([UserRoles.SuperAdmin, UserRoles.Administrator]);
        Options(x => x.WithTags("Coaches"));
    }

    public override async Task HandleAsync(UpdateCoachRequest request, CancellationToken cancellationToken)
    {
        var coachId = Route<Guid>("id", isRequired: true);

        var coach =
            await _context.Coaches
                .FirstOrDefaultAsync(x => x.Id == coachId, cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        coach.FirstName = request.FirstName;
        coach.LastName = request.LastName;
        coach.Email = request.Email;
        coach.Phone = request.Phone;

        _context.Coaches.Update(coach);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await _coachService.UpdateCoachClients(coach, request.Clients, cancellationToken);

        await SendAsync(new(coach.Id),
            cancellation: cancellationToken);
    }
}

public sealed class UpdateCoachValidator : Validator<UpdateCoachRequest>
{
    public UpdateCoachValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Phone).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}