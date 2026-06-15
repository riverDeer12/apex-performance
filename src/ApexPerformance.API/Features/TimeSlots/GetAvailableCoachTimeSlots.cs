using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects.TimeSlots;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.TimeSlots;

public record GetAvailableCoachTimeSlotRequest(List<Guid> Coaches, DateTime Day);

public class GetAvailableCoachTimeSlotsEndpoint : Endpoint<GetAvailableCoachTimeSlotRequest, List<GetTimeSlotResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ITimeSlotService _timeSlotService;

    public GetAvailableCoachTimeSlotsEndpoint(ApexPerformanceContext context, ITimeSlotService timeSlotService)
    {
        _context = context;
        _timeSlotService = timeSlotService;
    }

    public override void Configure()
    {
        Post("api/time-slots/available");
        Options(x => x.WithTags("TimeSlots"));
    }

    public override async Task HandleAsync(GetAvailableCoachTimeSlotRequest request,
        CancellationToken cancellationToken)
    {
        var coachesIds = await _context.Coaches
            .Where(x => request.Coaches.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (coachesIds.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var coachesTimeSlots = await _context.CoachTimeSlots
            .Where(x => coachesIds.Contains(x.CoachId) && x.IsActive)
            .Select(x => x.TimeSlot)
            .ToListAsync(cancellationToken: cancellationToken);

        if (coachesTimeSlots.Count == 0)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var coachTimeSlotsForDay = _timeSlotService.GetCoachTimeSlotsForDay(coachesTimeSlots, request.Day);

        var coachTimeSlotsResponse =
            await PrepareTimeSlotsResponse(coachTimeSlotsForDay, request.Day, cancellationToken);

        await SendAsync(coachTimeSlotsResponse, cancellation: cancellationToken);
    }

    private async Task<List<GetTimeSlotResponse>> PrepareTimeSlotsResponse(List<TimeSlot> coachTimeSlotsForDay,
        DateTime day, CancellationToken cancellationToken)
    {
        var coachTimeSlots = new List<GetTimeSlotResponse>();

        var takenTimeSlots = new List<Guid>();

        var takenAppointments = await _context.Appointments
            .Where(x => coachTimeSlotsForDay
                            .Select(x => x.Id)
                            .Contains(x.TimeSlotId)
                        && x.AppointmentStatus.Name == BusinessStatuses.Approved
                        && x.StartTime.Date == day.Date)
            .Include(x => x.Clients)
            .ThenInclude(x => x.Client)
            .ToListAsync(cancellationToken);

        if (takenAppointments.Count is not 0)
            takenTimeSlots = takenAppointments.Select(x => x.TimeSlotId).ToList();

        foreach (var coachTimeSlot in coachTimeSlotsForDay)
        {
            var timeSlotLabel = $"{coachTimeSlot.StartTime} - {coachTimeSlot.EndTime}";

            var (label, isTaken, appointmentId) =
                ModifyTimeSlotLabel(coachTimeSlot, timeSlotLabel, takenTimeSlots, takenAppointments);

            var timeSlotResponse = new GetTimeSlotResponse(coachTimeSlot.Id,
                label,
                Enum.GetName(typeof(DayOfWeek), coachTimeSlot.Day)!,
                coachTimeSlot.StartTime, coachTimeSlot.EndTime, isTaken, appointmentId);

            coachTimeSlots.Add(timeSlotResponse);
        }

        return coachTimeSlots.OrderBy(x => x.StartTime).ToList();
    }

    private (string Label, bool IsTaken, Guid? AppointmentId) ModifyTimeSlotLabel(TimeSlot coachTimeSlot,
        string timeSlotLabel,
        List<Guid> takenTimeSlots, List<Appointment> takenAppointments)
    {
        var appointment = takenAppointments.FirstOrDefault(x => x.TimeSlotId == coachTimeSlot.Id);

        if (appointment is null || !takenTimeSlots.Contains(coachTimeSlot.Id))
            return (timeSlotLabel, false, null);

        var clientNames = appointment.Clients.Select(x => x.Client.FullName).ToList();

        timeSlotLabel += " (" + string.Join(", ", clientNames) + ")";

        return (timeSlotLabel, true, appointment.Id);
    }
}