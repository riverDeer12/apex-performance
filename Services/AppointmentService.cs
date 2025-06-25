using ApexPerformance.API.Database;

namespace ApexPerformance.API.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApexPerformanceContext _context;

    public AppointmentService(ApexPerformanceContext context)
    {
        _context = context;
    }

    public Task<bool> CheckFreeSlot(DateTimeOffset appointmentStartTime,
        DateTimeOffset appointmentEndTime, CancellationToken cancellationToken, Guid? appointmentId = null)
    {
        return Task.FromResult(!_context.Appointments.Any(existing =>
            appointmentStartTime < existing.EndTime && appointmentEndTime > existing.StartTime &&
            existing.Id != appointmentId
        ));
    }
}