using System.Text.Json.Serialization;

namespace ApexPerformance.API.Shared.DataTransferObjects.BoxNow;

public record BoxNowAuthorizationResponse(
    [property: JsonPropertyName("access_token")]
    string AccessToken,
    [property: JsonPropertyName("token_type")]
    string TokenType,
    [property: JsonPropertyName("expires_in")]
    int ExpiresIn
);