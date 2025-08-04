namespace ApexPerformance.API.Shared.DataTransferObjects;

public record AppointmentsStatusDto(
    List<AppointmentDataDto> ApprovedAppointments,
    List<AppointmentDataDto> PendingAppointments,
    List<AppointmentDataDto> InProgressAppointments
);