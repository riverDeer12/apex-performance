using ApexPerformance.API.Constants;
using ApexPerformance.API.Extensions;
using FastEndpoints;

namespace ApexPerformance.API.Features.BackgroundJobs;

public record GetJobsDashboardTokenResponse(string Token);

/// <summary>
/// Token for opening Hangfire dashboard (/jobs) in browser,
/// which does not send the login token on page navigation.
/// </summary>
public class GetJobsDashboardTokenEndpoint : EndpointWithoutRequest<GetJobsDashboardTokenResponse>
{
    private readonly IConfiguration _configuration;

    public GetJobsDashboardTokenEndpoint(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public override void Configure()
    {
        Get("api/background-jobs/dashboard-token");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("BackgroundJobs"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await SendAsync(new GetJobsDashboardTokenResponse(
            HangfireDashboardAuthPolicy.CreateToken(_configuration)), cancellation: cancellationToken);
    }
}
