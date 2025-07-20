using System.Text.Json;
using ApexPerformance.API.Constants;
using FastEndpoints;

namespace ApexPerformance.API.Features.Logs;

public record GetLogResponse(
    DateTime Timestamp,
    string Level,
    string TraceId,
    string SpanId,
    string Exception,
    LogProperties Properties
);

public record LogProperties(
    string RequestMethod,
    string RequestPath,
    int StatusCode,
    double Elapsed,
    string SourceContext,
    string RequestId,
    string Application
);

public class GetLogsEndpoint : EndpointWithoutRequest<List<GetLogResponse>>
{
    public override void Configure()
    {
        Get("api/logs");
        Roles(nameof(UserRoles.SuperAdmin));
        Options(x => x.WithTags("Logs"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        
        var logsFolder = Path.Combine(projectRoot, "Logs");

        var newestLogFile = Directory.GetFiles(logsFolder)
            .Select(f => new FileInfo(f))
            .MaxBy(f => f.LastWriteTime)?
            .FullName;

        if (newestLogFile is null)
        {
            await SendAsync([], cancellation: cancellationToken); 
            return;
        }

        var logEntries = new List<GetLogResponse>();

        foreach (var line in File.ReadLines(newestLogFile).Reverse())
        {
            var entry = JsonSerializer.Deserialize<GetLogResponse>(line);
            
            if (entry != null)
            {
                logEntries.Add(entry);
            }
        }

        await SendAsync(logEntries.Select(x =>
                new GetLogResponse(x.Timestamp, x.Level, x.TraceId, x.SpanId, x.Exception, x.Properties))
            .ToList(), cancellation: cancellationToken);
    }
}