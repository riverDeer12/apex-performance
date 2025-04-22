using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IUserService
{
    Task<bool> UsernameExists(string username, CancellationToken cancellationToken);
    Task<User> CreateUserAccount(string username, string password, string email,
        CancellationToken cancellationToken);
}