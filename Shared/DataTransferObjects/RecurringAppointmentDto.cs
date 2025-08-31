namespace ApexPerformance.API.Shared.DataTransferObjects;

public record RecurringAppointmentDto(
    Guid Id,
    List<PersonDataDto> Clients,
    PersonDataDto Coach,
    TimeSlotDto TimeSlot,
    bool Status,
    CatalogDataDto Type
);