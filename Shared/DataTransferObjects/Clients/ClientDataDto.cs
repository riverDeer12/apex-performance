namespace ApexPerformance.API.Shared.DataTransferObjects.Clients;

public record ClientDataDto(
    Guid Id,
    string FirstName,
    string LastName,
    int Credits,
    string Phone,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTime? LastCreditsIncrease,
    string FullName,
    List<PersonDataDto> Coaches,
    List<BodyMeasurementDto> BodyMeasurements);