using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Radar;

namespace ApexPerformance.API.Features.Shop;

/// <summary>
/// Events Stripe sends about shop payments: checkout finished or expired,
/// refunds, disputes (chargebacks) and early fraud warnings. Every request is
/// checked against the endpoint's signing secret, so only Stripe can call it.
/// Set up in the Stripe dashboard under Developers → Webhooks.
/// </summary>
public class ShopStripeWebhookEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;
    private readonly ShopOrderService _orderService;
    private readonly ShopPaymentEvents _paymentEvents;

    public ShopStripeWebhookEndpoint(ApexPerformanceContext context, ShopSettings settings,
        ShopOrderService orderService, ShopPaymentEvents paymentEvents)
    {
        _context = context;
        _settings = settings;
        _orderService = orderService;
        _paymentEvents = paymentEvents;
    }

    public override void Configure()
    {
        Post("api/shop/stripe/webhook");
        AllowAnonymous();
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_settings.StripeWebhookSecret))
        {
            Logger.LogError("Stripe webhook received, but Stripe:ApexPerformance:WebhookSecret is not set.");
            await SendAsync(new { }, StatusCodes.Status503ServiceUnavailable, cancellationToken);
            return;
        }

        using var reader = new StreamReader(HttpContext.Request.Body);
        var json = await reader.ReadToEndAsync(cancellationToken);

        Event stripeEvent;

        try
        {
            // The account's API version may be newer than the library's, the fields used here exist in both.
            stripeEvent = EventUtility.ConstructEvent(json, HttpContext.Request.Headers["Stripe-Signature"],
                _settings.StripeWebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            Logger.LogWarning("Stripe webhook with an invalid signature: {Message}", ex.Message);
            await SendAsync(new { }, StatusCodes.Status400BadRequest, cancellationToken);
            return;
        }

        if (await _context.ShopStripeEvents.AnyAsync(x => x.Id == stripeEvent.Id, cancellationToken))
        {
            await SendOkAsync(cancellationToken);
            return;
        }

        switch (stripeEvent.Data.Object)
        {
            // Completed, paid later (async methods) or expired: Stripe is asked for the current state.
            case Session session when stripeEvent.Type.StartsWith("checkout.session.") &&
                                      Guid.TryParse(session.ClientReferenceId, out var orderId):
                await _orderService.ConfirmAsync(orderId, cancellationToken);
                break;

            case Charge charge when stripeEvent.Type == EventTypes.ChargeRefunded:
                await _paymentEvents.Refunded(charge, cancellationToken);
                break;

            case Dispute dispute when stripeEvent.Type.StartsWith("charge.dispute."):
                await _paymentEvents.Disputed(dispute, stripeEvent.Type, cancellationToken);
                break;

            case EarlyFraudWarning warning when stripeEvent.Type == EventTypes.RadarEarlyFraudWarningCreated:
                await _paymentEvents.FraudWarning(warning, cancellationToken);
                break;
        }

        // Saved after handling, so an event that failed is handled again when Stripe retries it.
        _context.ShopStripeEvents.Add(new ShopStripeEvent
        {
            Id = stripeEvent.Id,
            Type = stripeEvent.Type,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The same event handled at the same time by a second delivery.
        }

        await SendOkAsync(cancellationToken);
    }
}

/// <summary>
/// Refunds, disputes and fraud warnings of shop payments, found by the
/// payment intent saved when the order was paid.
/// </summary>
public class ShopPaymentEvents
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;
    private readonly IEmailService _emailService;
    private readonly ILogger<ShopPaymentEvents> _logger;

    public ShopPaymentEvents(ApexPerformanceContext context, ShopSettings settings, IEmailService emailService,
        ILogger<ShopPaymentEvents> logger)
    {
        _context = context;
        _settings = settings;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Saves the refunded amount. A full refund of an order not shipped yet
    /// returns its pieces to the stock; shipped pieces come back only when
    /// received, so they are added in the warehouse then.
    /// </summary>
    public async Task Refunded(Charge charge, CancellationToken cancellationToken)
    {
        var order = await FindOrder(charge.PaymentIntentId, cancellationToken);
        if (order is null) return;

        order.RefundedAmount = charge.AmountRefunded / 100m;
        order.RefundedAt = DateTimeOffset.UtcNow;

        var isFull = charge.Refunded || charge.AmountRefunded >= charge.Amount;
        var returnedToStock = false;

        if (isFull && order.Status is ShopOrderStatuses.Paid or ShopOrderStatuses.Shipped)
        {
            if (order.Status == ShopOrderStatuses.Paid)
            {
                foreach (var item in order.Items)
                {
                    item.Variant.Stock += item.Quantity;
                    ShopStock.Record(_context, item.Variant, item.Quantity, ShopStockReasons.Return,
                        note: order.Number, orderId: order.Id);
                }

                returnedToStock = true;
            }

            order.Status = ShopOrderStatuses.Refunded;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop order {Number}: {Amount} EUR refunded in Stripe.", order.Number, order.RefundedAmount);

        Notify(order, $"Povrat novca za narudžbu {order.Number}",
            $"<p>U Stripeu je vraćeno <b>{ShopSettings.FormatPrice(order.RefundedAmount)}</b> od " +
            $"{ShopSettings.FormatPrice(order.Total)}.</p>" +
            (isFull ? "<p>Narudžba je označena kao „Vraćen novac“.</p>" : "<p>Povrat je djelomičan, status narudžbe se ne mijenja.</p>") +
            (returnedToStock ? "<p>Narudžba nije bila poslana, pa su komadi vraćeni na zalihu.</p>" : "") +
            (isFull && !returnedToStock && order.ShippedAt is not null
                ? "<p>Narudžba je bila poslana: kad roba stigne natrag, dodaj je u Skladištu kao primku.</p>"
                : ""));
    }

    public async Task Disputed(Dispute dispute, string eventType, CancellationToken cancellationToken)
    {
        var order = await FindOrder(dispute.PaymentIntentId, cancellationToken);
        if (order is null) return;

        var isNew = order.DisputeStatus is null;

        order.DisputeStatus = dispute.Status;
        order.DisputedAt ??= DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Shop order {Number}: dispute {Status} ({Event}).", order.Number, dispute.Status, eventType);

        // Updates of evidence in between are only saved.
        if (eventType == EventTypes.ChargeDisputeCreated || isNew)
        {
            var deadline = dispute.EvidenceDetails?.DueBy is { } dueBy ? $" Rok za odgovor: {dueBy:dd.MM.yyyy.}" : "";

            Notify(order, $"SPOR za narudžbu {order.Number} – odgovori u Stripeu",
                $"<p>Kupac je osporio plaćanje od {ShopSettings.FormatPrice(dispute.Amount / 100m)} kod svoje banke " +
                $"(razlog: {dispute.Reason}).{deadline}</p>" +
                (order.Status == ShopOrderStatuses.Paid ? "<p><b>Narudžba još nije poslana – ne šalji je.</b></p>" : "") +
                "<p>Dokaze (potvrdu dostave, komunikaciju s kupcem) predaj u Stripeu pod Payments → Disputes.</p>");
        }
        else if (eventType == EventTypes.ChargeDisputeClosed)
        {
            Notify(order, $"Spor za narudžbu {order.Number} je zatvoren: {(dispute.Status == "won" ? "dobiven" : "izgubljen")}",
                $"<p>Stripe je zatvorio spor sa statusom <b>{dispute.Status}</b>.</p>");
        }
    }

    public async Task FraudWarning(EarlyFraudWarning warning, CancellationToken cancellationToken)
    {
        var order = await FindOrder(warning.PaymentIntentId, cancellationToken);
        if (order is null || order.HasFraudWarning) return;

        order.HasFraudWarning = true;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Shop order {Number}: early fraud warning ({Type}).", order.Number, warning.FraudType);

        Notify(order, $"Upozorenje na prijevaru – narudžba {order.Number}",
            $"<p>Banka kartice javlja da je plaćanje vjerojatno prijevara ({warning.FraudType}).</p>" +
            (order.Status == ShopOrderStatuses.Paid
                ? "<p><b>Ne šalji narudžbu.</b> Preporuka je odmah vratiti novac u Stripeu, prije nego kupac otvori spor.</p>"
                : "<p>Narudžba je već poslana.</p>"));
    }

    private async Task<ShopOrder?> FindOrder(string? paymentIntentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(paymentIntentId))
            return null;

        var order = await _context.ShopOrders
            .Include(x => x.Items).ThenInclude(x => x.Variant)
            .FirstOrDefaultAsync(x => x.StripePaymentIntentId == paymentIntentId, cancellationToken);

        // Payments of other things on the same Stripe account are not shop orders.
        if (order is null)
            _logger.LogInformation("Stripe event for payment {PaymentIntent} that is not a shop order.", paymentIntentId);

        return order;
    }

    private void Notify(ShopOrder order, string subject, string body)
        => _emailService.SendShopEmail(_settings.NotificationEmail, subject,
            ShopEmails.Alert(subject, body + $"<p>Kupac: {System.Net.WebUtility.HtmlEncode(order.CustomerName)}, " +
                                      $"{System.Net.WebUtility.HtmlEncode(order.CustomerEmail)} · " +
                                      $"ukupno {ShopSettings.FormatPrice(order.Total)}</p>"));
}
