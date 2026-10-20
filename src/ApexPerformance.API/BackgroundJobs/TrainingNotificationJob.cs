using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Tells a client their coach added or changed one of their trainings.
/// </summary>
public class TrainingNotificationJob
{
    private readonly ApexPerformanceContext _context;
    private readonly INotificationService _notificationService;

    public TrainingNotificationJob(ApexPerformanceContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public static void Enqueue(Guid clientUserId, Guid trainingId, string trainingName, bool isNew)
        => BackgroundJob.Enqueue<TrainingNotificationJob>(job => job.Run(clientUserId, trainingId, trainingName, isNew));

    [AutomaticRetry(Attempts = 0)]
    public async Task Run(Guid clientUserId, Guid trainingId, string trainingName, bool isNew)
    {
        var deviceTokens = await _context.DeviceTokens
            .Where(x => x.UserId == clientUserId)
            .Select(x => new PushRecipient(x.Token, x.Language, x.UserId))
            .ToListAsync();

        var data = PushNotificationTypes.Data(PushNotificationTypes.Training);
        data["trainingId"] = trainingId.ToString();

        await _notificationService.SendLocalized(deviceTokens,
            language => isNew ? PushTexts.Training(language, trainingName) : PushTexts.TrainingUpdated(language, trainingName),
            data);
    }
}
