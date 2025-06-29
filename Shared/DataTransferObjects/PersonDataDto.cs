namespace ApexPerformance.API.Shared.DataTransferObjects;

public record PersonDataDto(
    Guid Id,
    string FirstName,
    string LastName
);