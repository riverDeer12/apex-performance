using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;

namespace ApexPerformance.API.Features.ShippingProviders.BoxNow;

public record BoxNowAuthorizationResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn
);

public record BoxNowDeliveryRequest(
    string OrderNumber,
    string InvoiceValue,
    string PaymentMode,
    string AmountToBeCollected,
    bool AllowReturn,
    Origin Origin,
    Destination Destination,
    List<Item> Items
);

public record Origin(
    string ContactNumber,
    string ContactEmail,
    string ContactName,
    string LocationId
);

public record Destination(
    string ContactNumber,
    string ContactEmail,
    string ContactName,
    string LocationId
);

public record Item(
    string Id,
    string Name,
    string Value,
    double Weight
);

public class CreateReVivPlusOrderEndpoint : EndpointWithoutRequest<string>
{
    private readonly IConfiguration _configuration;

    public CreateReVivPlusOrderEndpoint(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/shipping-providers/box-now/reviv-plus/create-order");
        AllowAnonymous();
        Options(x => x.WithTags("ShippingProviders"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var authorizationSession = await GetBoxNowAuthorizationSession(cancellationToken);

        var url = _configuration["BoxNow:ReVivPlus:ApiUrl"] + "/api/v1/delivery-requests";

        // JSON body
        var requestBody = new BoxNowDeliveryRequest(
            OrderNumber: "12345",
            InvoiceValue: "25.50",
            PaymentMode: "prepaid",
            AmountToBeCollected: "0.00",
            AllowReturn: true,
            Origin: new Origin(
                "+385 91 1234 1234",
                "partner.example@boxnow.hr",
                "Hrvoje Horvat", 
                "origin-location"),
            Destination: new Destination(
                "+385 91 123 123", 
                "customer.example@boxnow.hr", 
                "Ivan Ivanic",
                "destination-location"),
            Items: new List<Item>
            {
                new Item("1", "Smartphone", "3.45", 0)
            }
        );

        // Serialize it to JSON
        var json = JsonSerializer.Serialize(requestBody);

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var client = new HttpClient();
        // Add Authorization header
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authorizationSession.AccessToken);

        // Send POST request
        var response = await client.PostAsync(url, content, cancellationToken);

        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Error: {response.StatusCode}");
        }

        await SendAsync(result, cancellation: cancellationToken);
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