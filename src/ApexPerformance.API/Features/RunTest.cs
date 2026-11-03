using ApexPerformance.API.BackgroundJobs;
using ApexPerformance.API.Constants;
using FastEndpoints;
using Hangfire;

namespace ApexPerformance.API.Features;

public class RunTestEndpoint : EndpointWithoutRequest<string>
{
    public override void Configure()
    {
        Get("api/test");
        // Schedules a job on every call, so it is not open to anyone.
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Tests"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        BackgroundJob.Schedule<IEmailJob>(j => j.SendWelcomeMessage("USERID", cancellationToken),
            TimeSpan.FromSeconds(5));

        await SendAsync("Testing Job", cancellation: cancellationToken);
    }
}