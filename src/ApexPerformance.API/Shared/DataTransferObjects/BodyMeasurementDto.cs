namespace ApexPerformance.API.Shared.DataTransferObjects;

public record BodyMeasurementDto(
    Guid Id,
    decimal Height,
    decimal Weight,
    decimal Shoulders,
    decimal Chest,
    decimal UpperArm,
    decimal Waist,
    decimal Thigh,
    decimal Calves,
    decimal Glutes,
    DateTimeOffset MeasuredAt
);
