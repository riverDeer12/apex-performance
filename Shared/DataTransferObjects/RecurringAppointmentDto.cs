namespace ApexPerformance.API.Shared.DataTransferObjects;

public record RecurringAppointmentDto(
    Guid Id,
    PersonDataDto Client,
    PersonDataDto Coach,
    TimeSlotDto TimeSlot,
    bool Status
);