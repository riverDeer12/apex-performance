using ApexPerformance.API.Constants;
using FastEndpoints;

namespace ApexPerformance.API.Features.Logs;

public record GetRequestLogsRequest(string? File, string RequestId);

/// <summary>
/// All entries of one request in order (request body, processing,
/// error, response), the context needed to reproduce an error.
/// </summary>
public class GetRequestLogsEndpoint : Endpoint<GetRequestLogsRequest, List<LogEntryResponse>>
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public GetRequestLogsEndpoint(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public override void Configure()
    {
        Get("api/logs/request");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Logs"));
    }

    public override async Task HandleAsync(GetRequestLogsRequest request, CancellationToken cancellationToken)
    {
        var path = LogFileReader.ResolveFile(_webHostEnvironment, request.File);

        if (path is null || string.IsNullOrWhiteSpace(request.RequestId))
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var entries = await LogFileReader.ReadAsync(path, cancellationToken);

        await SendAsync(entries
                .Where(x => x.RequestId == request.RequestId)
                .OrderBy(x => x.Timestamp)
                .ToList(),
            cancellation: cancellationToken);
    }
}
