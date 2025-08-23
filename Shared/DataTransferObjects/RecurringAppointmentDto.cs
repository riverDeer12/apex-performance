namespace ApexPerformance.API.Shared.DataTransferObjects;

public record RecurringAppointmentDto(
    PersonDataDto Client,
    PersonDataDto Coach,
    TimeSlotDto TimeSlot
);