namespace ApexPerformance.API.BackgroundJobs;

public interface IEmailJob
{
    Task SendWelcomeMessage(string UserId, CancellationToken cancellationToken);
}