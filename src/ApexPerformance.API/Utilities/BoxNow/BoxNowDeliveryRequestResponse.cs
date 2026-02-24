using System.Text.Json.Serialization;

namespace ApexPerformance.API.Utilities.BoxNow;

public record BoxNowDeliveryRequestResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("parcels")]
    ParcelItem[] Parcels
);

public record ParcelItem(
    [property: JsonPropertyName("id")] string Id);