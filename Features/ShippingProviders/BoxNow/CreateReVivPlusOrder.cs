using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;

namespace ApexPerformance.API.Features.ShippingProviders.BoxNow;

public record BoxNowAuthorizationResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn
);

public class CreateReVivPlusOrderEndpoint : EndpointWithoutRequest<BoxNowAuthorizationResponse>
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
       
       var url = "https://api.example.com/data";

       // JSON body
       var json = "{\"name\":\"John Doe\",\"email\":\"john@example.com\"}";
       var content = new StringContent(json, Encoding.UTF8, "application/json");

       using var client = new HttpClient();
       // Add Authorization header
       client.DefaultRequestHeaders.Authorization =
           new AuthenticationHeaderValue("Bearer", authorizationSession.AccessToken);

       // Send POST request
       var response = await client.PostAsync(url, content, cancellationToken);

       if (response.IsSuccessStatusCode)
       {
           var result = await response.Content.ReadAsStringAsync(cancellationToken);
           Console.WriteLine("Response:");
           Console.WriteLine(result);
       }
       else
       {
           Console.WriteLine($"Error: {response.StatusCode}");
           var error = await response.Content.ReadAsStringAsync(cancellationToken);
           Console.WriteLine(error);
       }
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