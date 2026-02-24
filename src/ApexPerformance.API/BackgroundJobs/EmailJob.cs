namespace ApexPerformance.API.BackgroundJobs;

public class EmailJob : IEmailJob
{
    private readonly ILogger<EmailJob> _log;

    public EmailJob(ILogger<EmailJob> log)
    {
        _log = log;
    }
    
    public Task SendWelcomeMessage(string userId, CancellationToken cancellationToken = default)
    {
        _log.LogInformation("Sending welcome email to: {UserId}", userId);

        return Task.CompletedTask;
    }
}