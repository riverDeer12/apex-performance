using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services;
using ApexPerformance.API.Shared.Extensions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Appointments.RecurringAppointments;

public record GenerateNextWeekAppointmentsResponse(bool IsGenerated);

public class GenerateNextWeekAppointmentsEndpoint : EndpointWithoutRequest<GenerateNextWeekAppointmentsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAppointmentService _appointmentService;

    public GenerateNextWeekAppointmentsEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IAppointmentService appointmentService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _appointmentService = appointmentService;
    }

    public override void Configure()
    {
        Get("api/recurring-appointments/generate-next-week");
        Roles(UserRoles.Coach);
        Options(x => x.WithTags("RecurringAppointments"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var coach =
            await _context.Coaches
                .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
                    cancellationToken: cancellationToken);

        if (coach is null)
            ThrowError(ErrorMessages.NotFound);

        var recurringAppointments =
            await _context.RecurringAppointments
                .Include(recurringAppointment => recurringAppointment.TimeSlot)
                .ToListAsync(cancellationToken: cancellationToken);

        if (recurringAppointments.Count is 0)
        {
            await SendAsync(new GenerateNextWeekAppointmentsResponse(true), cancellation: cancellationToken);
            return;
        }

        var approvedStatus =
            await _context.AppointmentStatuses.FirstOrDefaultAsync(x => x.Name == BusinessStatuses.Approved,
                cancellationToken: cancellationToken);

        if (approvedStatus is null)
            ThrowError(ErrorMessages.NotFound);

        foreach (var recurring in recurringAppointments)
        {
            // var newAppointment = await CreateAppointment(recurring, approvedStatus, cancellationToken);
            //
            // await _appointmentService.UpdateClients(clients, newAppointment, cancellationToken);
            //
            // await _appointmentService.UpdateCoaches(coaches, newAppointment, cancellationToken);
        }
    }

    // private async Task<Appointment> CreateAppointment(RecurringAppointment recurring, AppointmentStatus status,
    //     CancellationToken cancellationToken)
    // {
    //     var appointmentDate = DateExtensions.GetNextWeekday(recurring.TimeSlot.Day);
    //
    //     var startTime = DateExtensions.CombineDateAndTime(appointmentDate, recurring.TimeSlot.StartTime);
    //
    //     var endTime = DateExtensions.CombineDateAndTime(appointmentDate, recurring.TimeSlot.EndTime);
    //
    //     var newAppointment = new Appointment
    //     {
    //         AppointmentStatus = status,
    //         TimeSlot = recurring.TimeSlot,
    //         StartTime = startTime,
    //         EndTime = endTime,
    //         AppointmentType = new AppointmentType()
    //     };
    //
    //     _context.Appointments.Add(newAppointment);
    //
    //     var result = await _context.SaveChangesAsync(cancellationToken);
    //
    //     if (result == 0)
    //         ThrowError(ErrorMessages.SavingError);
    //
    //     return newAppointment;
    // }
}