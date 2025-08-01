using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services;

public class UserService : IUserService
{
    private readonly ApexPerformanceContext _context;

    public UserService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public async Task<User> CreateUserAccount(string username, string password, string email, Role? role,
        CancellationToken cancellationToken)
    {
        if (await UsernameExists(username, cancellationToken))
            throw new Exception(ValidationMessages.UsernameAlreadyExists);

        if (await EmailExists(username, cancellationToken))
            throw new Exception(ValidationMessages.EmailAlreadyExists);

        var user = User.Init(username, password, email);

        if (role is not null)
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            _context.UserRoles.Add(userRole);
        }

        _context.Users.Add(user);

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            throw new Exception(ErrorMessages.SavingError);

        return user;
    }

    private async Task<bool> EmailExists(string email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<bool> UsernameExists(string username, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(x => x.UserName == username, cancellationToken);
    }
}