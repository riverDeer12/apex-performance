using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Administrators;

public sealed record CreateAdministratorRequest(Guid User, string FirstName, string LastName);

public sealed record CreateAdministratorResponse(Guid Id);

public sealed class CreateAdministratorEndpoint : Endpoint<CreateAdministratorRequest, CreateAdministratorResponse>
{
    private readonly ApexPerformanceContext _context;

    public CreateAdministratorEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/administrators");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Administrators"));
    }

    public override async Task HandleAsync(CreateAdministratorRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.User, cancellationToken);

        if (user is null)
            ThrowError(ErrorMessages.NotFound);

        var administratorExists = _context.Administrators.Any(x => x.UserId == user.Id);

        if (administratorExists)
            ThrowError(ErrorMessages.AlreadyExists);

        var newAdministrator = new Administrator
        {
            DeletedAt = null,
            User = user,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        _context.Administrators.Add(newAdministrator);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new CreateAdministratorResponse(newAdministrator.Id),
            cancellation: cancellationToken);
    }
}

public sealed class CreateAdministratorValidator : Validator<CreateAdministratorRequest>
{
    public CreateAdministratorValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.User).NotEmpty().WithMessage(ValidationMessages.Required);
    }
}