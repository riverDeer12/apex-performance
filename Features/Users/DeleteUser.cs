using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Users;

public sealed record DeleteUserResponse(Guid Id, string Username);

public class DeleteUserEndpoint : EndpointWithoutRequest<DeleteUserResponse>
{
    private readonly ApexPerformanceContext _context;

    public DeleteUserEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Delete("api/users/{id}");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Users"));
    }
    
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var userId = Route<Guid>("id", isRequired: true);

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken: cancellationToken);

        if (user == null)
            ThrowError(ErrorMessages.NotFound);
        
        user.Delete();

        _context.Users.Update(user);
        
        var result = await _context.SaveChangesAsync(cancellationToken);
        
        if(result == 0)
            ThrowError(ErrorMessages.SavingError);

        await SendAsync(new DeleteUserResponse(user.Id, user.UserName), cancellation: cancellationToken);
    }
}