using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Shop;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Confirms shop orders whose customer paid but closed the browser before
/// returning to the shop, and cancels the ones whose checkout expired.
/// </summary>
public class ShopOrderSyncJob
{
    private const string JobId = "shop-order-sync";
    private const string Schedule = "*/5 * * * *";

    // Checkouts expire after an hour, older pending orders without a checkout are leftovers.
    private static readonly TimeSpan GiveUpAfter = TimeSpan.FromDays(1);

    private readonly ApexPerformanceContext _context;
    private readonly ShopOrderService _orderService;
    private readonly ILogger<ShopOrderSyncJob> _logger;

    public ShopOrderSyncJob(ApexPerformanceContext context, ShopOrderService orderService,
        ILogger<ShopOrderSyncJob> logger)
    {
        _context = context;
        _orderService = orderService;
        _logger = logger;
    }

    public static void ScheduleJob()
        => RecurringJob.AddOrUpdate<ShopOrderSyncJob>(JobId, job => job.Run(CancellationToken.None), Schedule);

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    public async Task Run(CancellationToken cancellationToken)
    {
        var pending = await _context.ShopOrders
            .AsNoTracking()
            .Where(x => x.Status == ShopOrderStatuses.Pending)
            .Select(x => new { x.Id, x.StripeSessionId, x.CreatedAt })
            .ToListAsync(cancellationToken);

        foreach (var order in pending)
        {
            if (order.StripeSessionId is null)
            {
                // Stripe refused the checkout, nothing was paid.
                if (order.CreatedAt < DateTimeOffset.UtcNow - GiveUpAfter)
                {
                    await _context.ShopOrders
                        .Where(x => x.Id == order.Id && x.Status == ShopOrderStatuses.Pending)
                        .ExecuteUpdateAsync(x => x.SetProperty(o => o.Status, ShopOrderStatuses.Cancelled),
                            cancellationToken);
                }

                continue;
            }

            try
            {
                await _orderService.ConfirmAsync(order.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                // One order Stripe can't answer for must not stop the others.
                _logger.LogError(ex, "Shop order {OrderId} could not be checked with Stripe.", order.Id);
            }
        }
    }
}
