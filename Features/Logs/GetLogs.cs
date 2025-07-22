using System.Text.Json;
using ApexPerformance.API.Constants;
using FastEndpoints;
using JetBrains.Annotations;

namespace ApexPerformance.API.Features.Logs;

public record GetLogResponse(
    DateTime Timestamp,
    string Level,
    string TraceId,
    string SpanId,
    string Exception,
    LogProperties Properties
);

[UsedImplicitly]
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
    private readonly IWebHostEnvironment _webHostEnvironment;

    public GetLogsEndpoint(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public override void Configure()
    {
        Get("api/logs");
        Roles(nameof(UserRoles.SuperAdmin));
        Options(x => x.WithTags("Logs"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var logsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Logs");

        var newestLogFile = Directory.GetFiles(logsFolder)
            .Select(f => new FileInfo(f)).MaxBy(f => f.LastWriteTime)?.FullName;

        if (newestLogFile is null)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        var logEntries = new List<GetLogResponse>();
        var lines = new List<string>();

        FileStream stream = null;
        
        for (var retry = 0; retry < 3; retry++)
        {
            try
            {
                stream = new FileStream(
                    newestLogFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    4096,
                    FileOptions.Asynchronous
                );
                break;
            }
            catch (IOException) when (retry < 2)
            {
                await Task.Delay(200, cancellationToken);
            }
        }

        if (stream is null)
        {
            await SendAsync([], cancellation: cancellationToken);
            return;
        }

        await using var streamWrapper = stream;
        
        using var reader = new StreamReader(streamWrapper);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        foreach (var line in lines.AsEnumerable().Reverse())
        {
            try
            {
                var entry = JsonSerializer.Deserialize<GetLogResponse>(line);
                if (entry != null)
                {
                    logEntries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Optional: log or ignore invalid JSON
            }
        }

        await SendAsync(logEntries.Select(x =>
                new GetLogResponse(x.Timestamp, x.Level, x.TraceId, x.SpanId, x.Exception, x.Properties))
            .ToList(), cancellation: cancellationToken);
    }
}