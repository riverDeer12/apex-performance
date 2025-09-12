namespace ApexPerformance.API.Shared.DataTransferObjects;

public record ProductDto(
    string Name,
    string Description,
    decimal Price,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);