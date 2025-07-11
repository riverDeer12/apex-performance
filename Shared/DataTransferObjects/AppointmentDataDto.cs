namespace ApexPerformance.API.Shared.DataTransferObjects;

public record AppointmentDataDto(
    Guid Id,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    CatalogDataDto Type,
    CatalogDataDto Status,
    List<PersonDataDto> Clients,
    List<PersonDataDto> Coaches
);