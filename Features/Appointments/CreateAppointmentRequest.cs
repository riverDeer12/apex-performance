using FastEndpoints;

namespace ApexPerformance.API.Features.Appointments;

public record RequestAppointmentRequest(
    Guid AppointmentType,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    List<Guid> Clients
);

public record RequestAppointmentResponse(
    Guid Id,
    bool Status
);

public class RequestAppointmentEndpoint : Endpoint<RequestAppointmentRequest, RequestAppointmentResponse>
{
    public override void Configure()
    {
        Post("api/appointments/request");
        Options(x => x.WithTags("Appointments"));
    }

    public override async Task HandleAsync(RequestAppointmentRequest request, CancellationToken ct)
    {
        
    }
}