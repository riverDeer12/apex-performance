namespace ApexPerformance.API.Shared.DataTransferObjects.TimeSlots;

public record GetTimeSlotResponse(
    Guid Id,
    string Name,
    string Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsTaken = false,
    Guid? AppointmentId = null);