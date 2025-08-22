namespace ApexPerformance.API.Shared.DataTransferObjects;

public record TimeSlotDto(
    Guid Id,
    string Name,
    string Day,
    TimeOnly StartTime,
    TimeOnly EndTime
);