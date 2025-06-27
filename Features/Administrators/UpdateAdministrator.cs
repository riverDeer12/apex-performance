using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Administrators;

public sealed record UpdateAdministratorRequest(string FirstName, string LastName);

public sealed record UpdateAdministratorResponse(Guid Id);

public class UpdateAdministratorEndpoint : Endpoint<UpdateAdministratorRequest, UpdateAdministratorResponse>
{
    private readonly ApexPerformanceContext _context;

    public UpdateAdministratorEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Put("api/administrators/{id}");
        Roles(nameof(UserRoles.SuperAdmin));
        Options(x => x.WithTags("Administrators"));
    }

    public override async Task HandleAsync(UpdateAdministratorRequest request, CancellationToken cancellationToken)
    {
        var administratorId = Route<Guid>("id", isRequired: true);

        var administrator =
            await _context.Administrators
                .FirstOrDefaultAsync(x => x.Id == administratorId, cancellationToken: cancellationToken);

        if (administrator is null)
            ThrowError(ErrorMessages.NotFound);

        administrator.FirstName = request.FirstName;
        administrator.LastName = request.LastName;

        _context.Administrators.Update(administrator);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new(administrator.Id), cancellation: cancellationToken);
    }
}

public sealed class UpdateAdministratorValidator : Validator<UpdateAdministratorRequest>
{
    public UpdateAdministratorValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}