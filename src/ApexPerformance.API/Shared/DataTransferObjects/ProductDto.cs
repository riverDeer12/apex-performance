namespace ApexPerformance.API.Shared.DataTransferObjects;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    bool Status,
    DateTimeOffset? CreatedAt = null,
    DateTimeOffset? UpdatedAt = null);