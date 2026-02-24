namespace ApexPerformance.API.Shared.DataTransferObjects;

public record TimeSlotDto(
    Guid Id,
    string Name,
    DayOfWeek Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool Status = false
);