using ApexPerformance.API.Database;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApexPerformanceContext _context;

    public AppointmentService(ApexPerformanceContext context)
    {
        _context = context;
    }
    
    public async Task<bool> CheckFreeTimeSlot(DateTimeOffset appointmentStartTime, CancellationToken cancellationToken)
    {
        var appointmentsInTimeSlot = await _context.Appointments
            .Where(x => x.StartTime < appointmentStartTime && x.EndTime > appointmentStartTime)
            .ToListAsync(cancellationToken: cancellationToken);

        return appointmentsInTimeSlot.Count == 0;
    }
}