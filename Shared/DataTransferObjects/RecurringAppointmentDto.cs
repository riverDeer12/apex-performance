using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Shared.DataTransferObjects;

public record RecurringAppointmentDto(
    PersonDataDto Client,
    PersonDataDto Coach,
    TimeSlot TimeSlot
);