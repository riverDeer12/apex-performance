using ApexPerformance.API.Constants;
using FastEndpoints;

namespace ApexPerformance.API.Features.Logs;

public record GetLogsRequest(string? File, string? Level, string? Search, int? Take);

public record LogsSummaryResponse(int Total, int Errors, int Warnings, int Requests, int FailedRequests);

public record ErrorGroupResponse(
    string Title,
    string Level,
    int Count,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    List<string> Paths,
    LogEntryResponse Latest
);

public record GetLogsResponse(
    string? File,
    List<LogFileResponse> Files,
    LogsSummaryResponse Summary,
    List<ErrorGroupResponse> ErrorGroups,
    List<LogEntryResponse> Entries,
    int MatchingCount
);

/// <summary>
/// Log entries of one day (log file), newest first, with errors
/// grouped by their cause so the most frequent ones are fixed first.
/// </summary>
public class GetLogsEndpoint : Endpoint<GetLogsRequest, GetLogsResponse>
{
    private const int DefaultTake = 300;
    private const int MaxTake = 2000;

    private static readonly string[] ErrorLevels = ["Error", "Fatal"];

    private readonly IWebHostEnvironment _webHostEnvironment;

    public GetLogsEndpoint(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public override void Configure()
    {
        Get("api/logs");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("Logs"));
    }

    public override async Task HandleAsync(GetLogsRequest request, CancellationToken cancellationToken)
    {
        var files = LogFileReader.GetFiles(_webHostEnvironment);
        var path = LogFileReader.ResolveFile(_webHostEnvironment, request.File);

        if (path is null)
        {
            await SendAsync(new GetLogsResponse(null, files, new LogsSummaryResponse(0, 0, 0, 0, 0), [], [], 0),
                cancellation: cancellationToken);
            return;
        }

        var entries = await LogFileReader.ReadAsync(path, cancellationToken);

        var requests = entries.Where(x => x.StatusCode is not null).ToList();

        var summary = new LogsSummaryResponse(
            entries.Count,
            entries.Count(x => ErrorLevels.Contains(x.Level)),
            entries.Count(x => x.Level == "Warning"),
            requests.Count,
            requests.Count(x => x.StatusCode >= 500));

        var errorGroups = entries
            .Where(x => ErrorLevels.Contains(x.Level))
            .GroupBy(GroupTitle)
            .Select(x => new ErrorGroupResponse(
                x.Key,
                x.Any(entry => entry.Level == "Fatal") ? "Fatal" : "Error",
                x.Count(),
                x.Min(entry => entry.Timestamp),
                x.Max(entry => entry.Timestamp),
                x.Select(entry => entry.RequestPath).OfType<string>().Distinct().Take(5).ToList(),
                x.MaxBy(entry => entry.Timestamp)!))
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.LastSeen)
            .ToList();

        var matching = entries.AsEnumerable();

        matching = request.Level switch
        {
            "errors" => matching.Where(x => ErrorLevels.Contains(x.Level)),
            "warnings" => matching.Where(x => ErrorLevels.Contains(x.Level) || x.Level == "Warning"),
            "requests" => matching.Where(x => x.StatusCode is not null),
            _ => matching
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            matching = matching.Where(x =>
                x.Message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Exception?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.RequestPath?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                x.RequestId == search);
        }

        var matchingList = matching.OrderByDescending(x => x.Timestamp).ToList();

        await SendAsync(new GetLogsResponse(
                Path.GetFileName(path),
                files,
                summary,
                errorGroups,
                matchingList.Take(Math.Clamp(request.Take ?? DefaultTake, 1, MaxTake)).ToList(),
                matchingList.Count),
            cancellation: cancellationToken);
    }

    /// <summary>
    /// Errors with the same exception (its first line) or the same
    /// message are the same problem, ids and numbers don't matter.
    /// </summary>
    private static string GroupTitle(LogEntryResponse entry)
    {
        var title = !string.IsNullOrWhiteSpace(entry.Exception)
            ? entry.Exception.Split('\n')[0].Trim()
            : entry.Message;

        title = System.Text.RegularExpressions.Regex.Replace(title,
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "{id}");

        return title.Length > 300 ? title[..300] + "…" : title;
    }
}
