using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Administrators;

public sealed record DeleteAdministratorResponse(Guid Id);

public sealed class DeleteAdministratorEndpoint : EndpointWithoutRequest<DeleteAdministratorResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteAdministratorEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/administrators/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Administrators"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var administratorId = Route<Guid>("id", isRequired: true);

        var administrator =
            await _context.Administrators
                .FirstOrDefaultAsync(x => x.Id == administratorId, cancellationToken: cancellationToken);

        if (administrator is null)
            ThrowError(ErrorMessages.NotFound);

        administrator.Delete();

        _context.Administrators.Update(administrator);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteAdministratorResponse(administrator.Id), cancellation: cancellationToken);
    }
}