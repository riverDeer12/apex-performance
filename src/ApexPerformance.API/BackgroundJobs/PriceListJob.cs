using ApexPerformance.API.Services.Implementation;
using ApexPerformance.API.Services.Interfaces;
using Hangfire;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// ReViv Plus web shop must publish its price list every day by 8:00.
/// It is published at 7:30, and at 7:45 an alert email is sent if
/// it is still missing, so there is time to publish it by hand.
/// Price changes in Stripe publish a new file during the day as well
/// (see StripePriceWebhookEndpoint).
/// </summary>
public class PriceListJob
{
    private const string PublishJobId = "reviv-plus-price-list";
    private const string PublishSchedule = "30 7 * * *";
    private const string CheckJobId = "reviv-plus-price-list-check";
    private const string CheckSchedule = "45 7 * * *";

    private readonly IPriceListService _priceListService;

    public PriceListJob(IPriceListService priceListService)
    {
        _priceListService = priceListService;
    }

    public static void Schedule()
    {
        var options = new RecurringJobOptions { TimeZone = PriceListService.GetCroatianTimeZone() };

        RecurringJob.AddOrUpdate<PriceListJob>(PublishJobId, job => job.Publish(), PublishSchedule, options);
        RecurringJob.AddOrUpdate<PriceListJob>(CheckJobId, job => job.Check(), CheckSchedule, options);
    }

    // A few quick retries still fit before the 7:45 check.
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 180 })]
    public async Task Publish()
        => await _priceListService.PublishAsync(PriceListPublishReasons.Scheduled);

    [AutomaticRetry(Attempts = 0)]
    public async Task Check()
        => await _priceListService.AlertIfNotPublishedTodayAsync();
}
