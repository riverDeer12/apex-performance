namespace ApexPerformance.API.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
}