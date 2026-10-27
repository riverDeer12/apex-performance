using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Utilities.BoxNow;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using PhoneNumbers;
using Stripe;
using Stripe.Checkout;

namespace ApexPerformance.API.Features.Shop;

/// <summary>
/// What happens with an order after Stripe Checkout: the payment is checked
/// with Stripe (never trusted from the browser), the stock lowered, the BoxNow
/// parcel created and the emails sent. Called when the customer comes back to
/// the shop and by ShopOrderSyncJob for customers who closed the browser first.
/// </summary>
public class ShopOrderService
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;
    private readonly IEmailService _emailService;
    private readonly ILogger<ShopOrderService> _logger;

    public ShopOrderService(ApexPerformanceContext context, ShopSettings settings, IEmailService emailService,
        ILogger<ShopOrderService> logger)
    {
        _context = context;
        _settings = settings;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Marks a pending order paid when Stripe says it is, or cancelled when its
    /// checkout expired. Safe to call any number of times and at the same time:
    /// only the first call that changes the status does the rest.
    /// </summary>
    public async Task ConfirmAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _context.ShopOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is not { Status: ShopOrderStatuses.Pending, StripeSessionId: not null })
            return;

        var session = await new SessionService().GetAsync(order.StripeSessionId, new SessionGetOptions(),
            new RequestOptions { ApiKey = _settings.StripeSecretKey }, cancellationToken);

        if (session.Status == "expired")
        {
            await SetStatus(orderId, ShopOrderStatuses.Pending, ShopOrderStatuses.Cancelled, cancellationToken);
            return;
        }

        if (session.PaymentStatus != "paid")
            return;

        // Only one caller gets to change Pending to Paid.
        if (!await SetStatus(orderId, ShopOrderStatuses.Pending, ShopOrderStatuses.Paid, cancellationToken))
            return;

        var paid = await _context.ShopOrders
            .Include(x => x.Items).ThenInclude(x => x.Variant)
            .FirstAsync(x => x.Id == orderId, cancellationToken);

        paid.PaidAt = DateTimeOffset.UtcNow;
        paid.CustomerName = session.CustomerDetails?.Name?.Trim();
        paid.CustomerEmail = session.CustomerDetails?.Email?.Trim();
        paid.CustomerPhone = session.CustomerDetails?.Phone?.Trim();
        paid.StripePaymentIntentId = session.PaymentIntentId;

        foreach (var item in paid.Items)
        {
            if (item.Variant.Stock < item.Quantity)
                paid.HasStockIssue = true;

            var before = item.Variant.Stock;
            item.Variant.Stock = Math.Max(0, before - item.Quantity);

            ShopStock.Record(_context, item.Variant, item.Variant.Stock - before, ShopStockReasons.Sale,
                note: paid.Number, orderId: paid.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Shop order {Number} paid, {Total} EUR.", paid.Number, paid.Total);

        BackgroundJob.Enqueue<ShopOrderService>(service => service.SendOrderEmails(orderId));
        BackgroundJob.Enqueue<ShopOrderService>(service => service.CreateBoxNowParcel(orderId));
    }

    private async Task<bool> SetStatus(Guid orderId, string from, string to, CancellationToken cancellationToken)
        => await _context.ShopOrders
            .Where(x => x.Id == orderId && x.Status == from)
            .ExecuteUpdateAsync(x => x
                .SetProperty(o => o.Status, to)
                .SetProperty(o => o.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken) == 1;

    public async Task SendOrderEmails(Guid orderId)
    {
        var order = await _context.ShopOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == orderId);

        if (!string.IsNullOrEmpty(order.CustomerEmail))
        {
            _emailService.SendShopEmail(order.CustomerEmail, $"Narudžba {order.Number} je zaprimljena",
                ShopEmails.Confirmation(order, _settings.ShopUrl), replyTo: _settings.NotificationEmail);
        }

        _emailService.SendShopEmail(_settings.NotificationEmail,
            $"Nova narudžba {order.Number} ({ShopSettings.FormatPrice(order.Total)})" +
            (order.HasStockIssue ? " – PROVJERI ZALIHU" : ""),
            ShopEmails.NewOrder(order));
    }

    /// <summary>
    /// Creates the BoxNow parcel to the chosen locker and emails its label to the shop.
    /// Retried by Hangfire when BoxNow fails; an order already sent is skipped.
    /// </summary>
    [AutomaticRetry(Attempts = 5)]
    public async Task CreateBoxNowParcel(Guid orderId)
    {
        var order = await _context.ShopOrders
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == orderId);

        if (order.BoxNowParcelId is not null || order.Status == ShopOrderStatuses.Cancelled)
            return;

        var apiUrl = _settings.BoxNow("ApiUrl") ??
                     throw new InvalidOperationException("BoxNow settings of the shop are missing.");

        using var client = new HttpClient { BaseAddress = new Uri(apiUrl) };

        var token = await Authorize(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var phoneUtil = PhoneNumberUtil.GetInstance();
        var phone = phoneUtil.Format(phoneUtil.Parse(order.CustomerPhone, "HR"), PhoneNumberFormat.E164);

        var request = new BoxNowDeliveryRequest(
            OrderNumber: order.Number,
            InvoiceValue: order.Total.ToString("0.00", CultureInfo.InvariantCulture),
            PaymentMode: "prepaid",
            AmountToBeCollected: "0.00",
            AllowReturn: true,
            Origin: new Origin(
                _settings.BoxNow("ContactNumber")!,
                _settings.BoxNow("ContactEmail")!,
                _settings.BoxNow("ContactName")!,
                _settings.BoxNow("WarehouseId")!),
            Destination: new Destination(phone, order.CustomerEmail!, order.CustomerName ?? order.CustomerEmail!,
                order.BoxNowLockerId),
            Items: order.Items.Select(x => new Item(x.Id.ToString(),
                    string.IsNullOrEmpty(x.VariantName) ? x.ProductName : $"{x.ProductName} ({x.VariantName})",
                    ((x.UnitPrice - x.UnitDiscount) * x.Quantity).ToString("0.00", CultureInfo.InvariantCulture), 0, 1))
                .ToList());

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var response = await client.PostAsync("/api/v1/delivery-requests",
            new StringContent(json, Encoding.UTF8, "application/json"));

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"BoxNow refused the parcel of order {order.Number}: {content}");

        var delivery = JsonSerializer.Deserialize<BoxNowDeliveryRequestResponse>(content,
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
                       throw new InvalidOperationException("BoxNow returned no parcel.");

        order.BoxNowParcelId = delivery.Parcels[0].Id;
        await _context.SaveChangesAsync();

        _logger.LogInformation("BoxNow parcel {Parcel} created for shop order {Number}.", order.BoxNowParcelId, order.Number);

        var label = await client.GetAsync($"/api/v1/parcels/{order.BoxNowParcelId}/label.pdf");

        if (!label.IsSuccessStatusCode)
        {
            // The parcel exists, the label can also be printed in the BoxNow partner portal.
            _logger.LogWarning("BoxNow label of parcel {Parcel} could not be loaded: {Status}.", order.BoxNowParcelId,
                label.StatusCode);
            return;
        }

        _emailService.SendShopEmail(_settings.NotificationEmail,
            $"BoxNow naljepnica za narudžbu {order.Number}",
            ShopEmails.Label(order),
            attachment: ($"{order.Number}-{order.BoxNowParcelId}.pdf", await label.Content.ReadAsByteArrayAsync(),
                "application/pdf"));
    }

    private async Task<string> Authorize(HttpClient client)
    {
        var body = JsonSerializer.Serialize(new
        {
            grant_type = "client_credentials",
            client_id = _settings.BoxNow("ClientId"),
            client_secret = _settings.BoxNow("ClientSecret")
        });

        var response = await client.PostAsync("/api/v1/auth-sessions", new StringContent(body, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"BoxNow login failed: {response.StatusCode}");

        var session = JsonSerializer.Deserialize<BoxNowAuthorizationResponse>(await response.Content.ReadAsStringAsync()) ??
                      throw new InvalidOperationException("BoxNow returned no access token.");

        return session.AccessToken;
    }
}

/// <summary>
/// HTML of the shop emails, with inline styles so every email program shows them the same.
/// </summary>
internal static class ShopEmails
{
    private const string Blue = "#0034C4";

    private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string Items(ShopOrder order)
    {
        var rows = new StringBuilder();

        foreach (var item in order.Items)
        {
            rows.Append("<tr>" +
                        $"<td style=\"padding:8px 0;border-bottom:1px solid #e3e7ef\">{E(item.ProductName)}" +
                        (string.IsNullOrEmpty(item.VariantName) ? "" : $"<br><span style=\"color:#5b6475;font-size:13px\">{E(item.VariantName)}</span>") +
                        "</td>" +
                        $"<td style=\"padding:8px;border-bottom:1px solid #e3e7ef;text-align:center\">{item.Quantity}</td>" +
                        $"<td style=\"padding:8px 0;border-bottom:1px solid #e3e7ef;text-align:right;white-space:nowrap\">{ShopSettings.FormatPrice((item.UnitPrice - item.UnitDiscount) * item.Quantity)}</td>" +
                        "</tr>");
        }

        string Row(string label, decimal value, bool bold = false)
            => $"<tr><td colspan=\"2\" style=\"padding:6px 0;{(bold ? "font-weight:bold" : "color:#5b6475")}\">{label}</td>" +
               $"<td style=\"padding:6px 0;text-align:right;white-space:nowrap;{(bold ? "font-weight:bold" : "")}\">{ShopSettings.FormatPrice(value)}</td></tr>";

        return "<table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"border-collapse:collapse;font-size:15px\">" +
               rows +
               Row("Proizvodi", order.Subtotal) +
               (order.Discount > 0 ? Row($"Promo kod {E(order.PromoCode)}", -order.Discount) : "") +
               Row("Dostava (BoxNow paketomat)", order.ShippingPrice) +
               Row("Ukupno", order.Total, bold: true) +
               "</table>";
    }

    private static string Layout(string title, string body)
        => "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#1b1f29;max-width:600px;margin:0 auto\">" +
           $"<div style=\"border-bottom:3px solid {Blue};padding:16px 0\">" +
           "<img src=\"https://apex-performance.fit/assets/images/logo_light_theme.png\" width=\"110\" alt=\"Apex Performance\" style=\"display:block;border:0\">" +
           "</div>" +
           $"<h2 style=\"color:{Blue};margin:24px 0 8px\">{E(title)}</h2>" +
           body +
           "<p style=\"color:#5b6475;font-size:12px;margin-top:32px;border-top:1px solid #e3e7ef;padding-top:12px\">" +
           "Apex Performance, obrt za usluge, vl. Gabriel Mihelčić · Trtni 21, Mladenići, Viškovo · OIB 81729432712" +
           "</p></div>";

    public static string Confirmation(ShopOrder order, string shopUrl)
        => Layout($"Hvala na narudžbi, {order.CustomerName}!",
            $"<p>Zaprimili smo vašu narudžbu <b>{E(order.Number)}</b> i plaćanje. Paket šaljemo u odabrani " +
            $"BoxNow paketomat: <b>{E(order.BoxNowLockerAddress)}, {E(order.BoxNowLockerPostalCode)}</b>.</p>" +
            "<p>Kad paket stigne u paketomat, BoxNow vam šalje poruku s kodom za otvaranje. " +
            "Fiskalizirani račun stiže vam u posebnom e-mailu.</p>" +
            Items(order) +
            $"<p style=\"margin-top:24px\">Pitanja? Samo odgovorite na ovaj e-mail.<br>" +
            $"<a href=\"{E(shopUrl)}\" style=\"color:{Blue}\">{E(shopUrl.Replace("https://", ""))}</a></p>");

    public static string NewOrder(ShopOrder order)
        => Layout($"Nova narudžba {order.Number}",
            (order.HasStockIssue
                ? "<p style=\"background:#fff4e5;padding:10px;border-left:4px solid #f59e0b\"><b>Zaliha nije bila dovoljna</b> " +
                  "za sve naručene artikle (netko je kupio isto u isto vrijeme). Provjeri zalihu prije slanja.</p>"
                : "") +
            $"<p><b>Kupac:</b> {E(order.CustomerName)}<br><b>E-mail:</b> {E(order.CustomerEmail)}<br>" +
            $"<b>Telefon:</b> {E(order.CustomerPhone)}<br>" +
            $"<b>Paketomat:</b> {E(order.BoxNowLockerAddress)}, {E(order.BoxNowLockerPostalCode)} (ID {E(order.BoxNowLockerId)})</p>" +
            Items(order) +
            "<p>BoxNow pošiljka se kreira automatski, naljepnica stiže u posebnom e-mailu.</p>");

    public static string Alert(string title, string body) => Layout(title, body);

    public static string Label(ShopOrder order)
        => Layout($"BoxNow naljepnica – {order.Number}",
            $"<p>Naljepnica za pošiljku <b>{E(order.BoxNowParcelId)}</b> je u privitku. " +
            $"Paketomat: {E(order.BoxNowLockerAddress)}, {E(order.BoxNowLockerPostalCode)}.</p>");
}
