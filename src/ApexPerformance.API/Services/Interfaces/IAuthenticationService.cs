using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services.Interfaces;

public interface IAuthenticationService
{
    Task<string> GenerateJwtToken(bool rememberMe, User user);
}