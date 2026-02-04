using System.Globalization;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApexPerformance.API.Features.Payments;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects.BoxNow;
using ApexPerformance.API.Shared.Validations;
using FastEndpoints;
using Hangfire;
using PhoneNumbers;
using Stripe;
using Stripe.Checkout;
using LineItem = ApexPerformance.API.Features.Payments.LineItem;

namespace ApexPerformance.API.Features.ShippingProviders.BoxNow;

public class CreateReVivPlusOrderEndpoint : EndpointWithoutRequest<GetCheckoutSessionResponse>
{
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly IHostEnvironment _environment;

    public CreateReVivPlusOrderEndpoint(IConfiguration configuration, IEmailService emailService,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _emailService = emailService;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("api/shipping-providers/box-now/reviv-plus/create-order/{sessionId}");
        AllowAnonymous();
        Options(x => x.WithTags("ShippingProviders"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var sessionId = Route<string>("sessionId", isRequired: true);

        var authorizationSession = await GetBoxNowAuthorizationSession(cancellationToken);

        var checkoutData = await GetCheckoutSessionData(sessionId, cancellationToken);

        checkoutData.PaymentIntentMetadata.TryGetValue("BoxNowLockerId", out var locationId);

        if (_environment.IsDevelopment()) locationId = _configuration["BoxNow:ReVivPlus:BoxNowLockerId"]!;

        var checkoutItems = checkoutData.LineItems.Select(
            x => new Item(Guid.NewGuid().ToString(), x.Description, x.Amount!, 0,
                1)
        ).ToList();
        
        var parsed = PhoneNumberUtil.GetInstance()
            .Parse(checkoutData.CustomerPhone, "HR");

        var normalizedCustomerPhone = PhoneNumberUtil.GetInstance()
            .Format(parsed, PhoneNumberFormat.E164);

        // JSON body
        var requestBody = new BoxNowDeliveryRequest(
            OrderNumber: sessionId,
            InvoiceValue: checkoutData.Amount!,
            PaymentMode: "prepaid",
            AmountToBeCollected: "0.00",
            AllowReturn: true,
            Origin: new Origin(
                _configuration["BoxNow:ReVivPlus:ContactNumber"]!,
                _configuration["BoxNow:ReVivPlus:ContactEmail"]!,
                _configuration["BoxNow:ReVivPlus:ContactName"]!,
                _configuration["BoxNow:ReVivPlus:WarehouseId"]!),
            Destination: new Destination(
                normalizedCustomerPhone,
                checkoutData.CustomerEmail,
                checkoutData.CustomerName,
                locationId),
            Items: checkoutItems
        );

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authorizationSession.AccessToken);

        var url = _configuration["BoxNow:ReVivPlus:ApiUrl"] + "/api/v1/delivery-requests";

        var response = await client.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new BadHttpRequestException(errorContent + "Checkout Data => " + requestBody);
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var deliveryResponse = JsonSerializer.Deserialize<BoxNowDeliveryRequestResponse>(responseContent, options) ??
                               throw new InvalidOperationException("BoxNow Delivery Details Were Not Provided.");

        BackgroundJob.Enqueue(() =>
            SendPdfLabel(deliveryResponse.Parcels[0].Id, authorizationSession.AccessToken,
                cancellationToken));
        
        await SendAsync(checkoutData, cancellation: cancellationToken);
    }
    
    public void SendFiscalizationReminder(Session checkoutSession) => 
        _emailService.SendFiscalizationReminderEmail(_configuration["BoxNow:ReVivPlus:ContactEmail"]!, checkoutSession);

    public async Task SendPdfLabel(string parcelNumber, string accessToken, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var url = _configuration["BoxNow:ReVivPlus:ApiUrl"] + "/api/v1/parcels/" + parcelNumber + "/label.pdf";

        var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            ThrowError(errorContent);
        }

        var pdfLabelStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        if (pdfLabelStream.CanSeek)
            pdfLabelStream.Position = 0;

        _emailService.SendBoxNowPdfLabel(_configuration["BoxNow:ReVivPlus:ContactEmail"]!, parcelNumber,
            pdfLabelStream);
    }

    private async Task<GetCheckoutSessionResponse> GetCheckoutSessionData(string sessionId,
        CancellationToken cancellationToken)
    {
        var sessionService = new SessionService();

        var session = await sessionService.GetAsync(sessionId, new SessionGetOptions
            {
                Expand = new List<string>
                {
                    "payment_intent",
                    "total_details.breakdown",
                    "customer",
                    "shipping_cost.shipping_rate"
                },
            },
            requestOptions: new RequestOptions
            {
                ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
            },
            cancellationToken: cancellationToken);

        var lineItemService = new SessionLineItemService();

        var lineItems = await lineItemService.ListAsync(sessionId, new SessionLineItemListOptions
            {
                Limit = 100,
                Expand = new List<string> { "data.price.product" }
            },
            requestOptions: new RequestOptions
            {
                ApiKey = _configuration["Stripe:ReVivPlus:SecretKey"]
            },
            cancellationToken: cancellationToken);

        var items = lineItems.Data.Select(li => new LineItem
        (
            (li.Description ?? li.Price?.Nickname ?? li.Price?.Product?.ToString())!,
            li.Quantity ?? 0,
            li.Currency,
            $"{((li.AmountTotal) / 100m).ToString("0.00", CultureInfo.InvariantCulture)}",
            $"{(li.Price?.UnitAmount ?? 0) / 100m:0.00} €",
            $"{(li.AmountSubtotal) / 100m:0.00} €",
            $"{(li.AmountTotal) / 100m:0.00} €"
        )).ToList();

        var checkoutResponse = new GetCheckoutSessionResponse
        (
            session.Id,
            session.Status,
            session.PaymentStatus,
            session.Currency,
            $"{((session.AmountTotal ?? 0) / 100m).ToString("0.00", CultureInfo.InvariantCulture)}",
            $"{(session.AmountSubtotal ?? 0) / 100m:0.00} €",
            $"{(session.AmountTotal ?? 0) / 100m:0.00} €",
            $"{(session.TotalDetails?.AmountTax ?? 0) / 100m:0.00} €",
            $"{(session.TotalDetails?.AmountDiscount ?? 0) / 100m:0.00} €",
            $"{(session.ShippingCost?.AmountSubtotal ?? 0) / 100m:0.00} €",
            session.ShippingCost?.ShippingRate?.DisplayName!,
            session.CustomerDetails?.Email!,
            session.CustomerDetails?.Name!,
            session.CustomerDetails?.Phone!,
            session.PaymentIntent.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            session.PaymentIntent?.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            session.Customer?.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
            items
        );
        
        var euBillingAddress = PaymentValidations.IsOutsideEu(session);

        if (!euBillingAddress) BackgroundJob.Enqueue(() => 
            SendFiscalizationReminder(session));

        return checkoutResponse;
    }

    /// <summary>
    /// Get BoxNow Authorization Session
    /// with access_token to send be able
    /// to send requests to BoxNow API.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<BoxNowAuthorizationResponse> GetBoxNowAuthorizationSession(CancellationToken cancellationToken)
    {
        var authorizationObject = new
        {
            grant_type = "client_credentials",
            client_id = _configuration["BoxNow:ReVivPlus:ClientId"]!,
            client_secret = _configuration["BoxNow:ReVivPlus:ClientSecret"]!
        };

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_configuration["BoxNow:ReVivPlus:ApiUrl"]!)
        };

        var httpRequest = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, "/api/v1/auth-sessions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(authorizationObject),
                Encoding.UTF8,
                "application/json"
            )
        };

        var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<BoxNowAuthorizationResponse>(responseContent, options) ??
               throw new InvalidOperationException("BoxNow Auth Session Was Not Provided.");
    }
}