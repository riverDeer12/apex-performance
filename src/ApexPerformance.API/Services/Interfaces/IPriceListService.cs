using ApexPerformance.API.Database.Entities;
using Hangfire;

namespace ApexPerformance.API.Services.Interfaces;

public interface IPriceListService
{
    /// <summary>
    /// Read current prices from Stripe, build the price list
    /// and publish it as a new file. Files older than 30 days are removed.
    /// </summary>
    /// <param name="reason">Why the file is published, saved with the file.</param>
    /// <returns>Published file.</returns>
    Task<PriceListFile> PublishAsync(string reason);

    /// <summary>
    /// Publish a new price list only if a price in Stripe is different
    /// from the last published file. Called after Stripe reports a change.
    /// </summary>
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    Task PublishIfPricesChangedAsync();

    /// <summary>
    /// Send an alert email if no price list was published today.
    /// </summary>
    Task AlertIfNotPublishedTodayAsync();

    /// <summary>
    /// Anchor prices by Stripe product id.
    /// </summary>
    Task<Dictionary<string, decimal>> GetAnchorPricesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Date the anchor prices were valid on, as shown to customers (e.g. "10.9.2026.").
    /// </summary>
    string AnchorDate { get; }
}
