using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Clients;

public record GetCurrentClientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    int Credits,
    PersonDataDto User,
    string Plan,
    // Approved appointments of this month (Croatian time) that are over; the
    // apps' package "done of total". See ClientPackage.
    int DoneThisMonth
);

public class GetCurrentClientEndpoint : EndpointWithoutRequest<GetCurrentClientResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentClientEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/clients/current-client");
        Options(x => x.WithTags("Clients"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId,
            cancellationToken: cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound);

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId,
            cancellationToken: cancellationToken);
        
        if (user is null)
            ThrowError(ErrorCodes.NotFound);

        var userResponse = new PersonDataDto(user.Id, user.UserName, user.Email, user.UserName);

        var now = DateTimeOffset.Now;
        var monthStart = ClientPackage.MonthStart(now, AppointmentReminder.CroatianTimeZone());

        // Same as ClientPackage.IsDoneThisMonth, in the database.
        var doneThisMonth = await _context.ClientAppointments
            .Where(x => x.ClientId == client.Id)
            .Select(x => x.Appointment)
            .CountAsync(appointment =>
                    !appointment.IsDeleted &&
                    (appointment.AppointmentStatus.Name == BusinessStatuses.Approved ||
                     appointment.AppointmentStatus.Name == BusinessStatuses.InProgress) &&
                    appointment.EndTime <= now &&
                    appointment.StartTime >= monthStart,
                cancellationToken);

        await SendAsync(new GetCurrentClientResponse(
            client.Id,
            client.FirstName,
            client.LastName,
            client.Email,
            client.Phone,
            client.Credits,
            userResponse,
            client.Plan,
            doneThisMonth
            ), cancellation: cancellationToken);
    }
}