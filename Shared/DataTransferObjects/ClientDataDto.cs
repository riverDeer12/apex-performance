namespace ApexPerformance.API.Shared.DataTransferObjects;

public record ClientDataDto(
    Guid Id,
    string FirstName,
    string LastName,
    int Credits,
    string Phone,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);