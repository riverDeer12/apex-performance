using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApexPerformance.API.Features.Logs;

public record LogEntryResponse(
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? Exception,
    string? RequestMethod,
    string? RequestPath,
    int? StatusCode,
    double? Elapsed,
    string? SourceContext,
    string? RequestId,
    Dictionary<string, string> Properties
);

public record LogFileResponse(string Name, DateTimeOffset LastModified, long Size);

/// <summary>
/// Reads Serilog JSON log files from the Logs folder of the application.
/// </summary>
public static partial class LogFileReader
{
    // Properties shown in their own fields, the rest is listed as extra properties.
    private static readonly HashSet<string> KnownProperties =
    [
        "RequestMethod", "RequestPath", "StatusCode", "Elapsed", "SourceContext", "RequestId"
    ];

    public static string GetLogsFolder(IWebHostEnvironment environment)
        => Path.Combine(environment.ContentRootPath, "Logs");

    public static List<LogFileResponse> GetFiles(IWebHostEnvironment environment)
    {
        var folder = GetLogsFolder(environment);

        if (!Directory.Exists(folder))
            return [];

        return Directory.GetFiles(folder)
            .Select(x => new FileInfo(x))
            .OrderByDescending(x => x.LastWriteTimeUtc)
            .Select(x => new LogFileResponse(x.Name, new DateTimeOffset(x.LastWriteTimeUtc, TimeSpan.Zero), x.Length))
            .ToList();
    }

    /// <summary>
    /// Path of a log file by its name, only files directly in the Logs folder
    /// can be read. Without a name the newest file is used.
    /// </summary>
    public static string? ResolveFile(IWebHostEnvironment environment, string? name)
    {
        var files = GetFiles(environment);

        var file = string.IsNullOrWhiteSpace(name)
            ? files.FirstOrDefault()
            : files.FirstOrDefault(x => x.Name == Path.GetFileName(name));

        return file is null ? null : Path.Combine(GetLogsFolder(environment), file.Name);
    }

    public static async Task<List<LogEntryResponse>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var entries = new List<LogEntryResponse>();

        FileStream? stream = null;

        // The file is being written by the logger, so it is opened for shared reading.
        for (var retry = 0; retry < 3 && stream is null; retry++)
        {
            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                    4096, FileOptions.Asynchronous);
            }
            catch (IOException) when (retry < 2)
            {
                await Task.Delay(200, cancellationToken);
            }
        }

        if (stream is null)
            return entries;

        await using var _ = stream;
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) && TryParse(line, out var entry))
                entries.Add(entry);
        }

        return entries;
    }

    private static bool TryParse(string line, out LogEntryResponse entry)
    {
        entry = null!;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            var properties = new Dictionary<string, string>();

            if (root.TryGetProperty("Properties", out var propertiesElement) &&
                propertiesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in propertiesElement.EnumerateObject())
                    properties[property.Name] = ToText(property.Value);
            }

            var template = GetString(root, "MessageTemplate") ?? string.Empty;
            var message = GetString(root, "RenderedMessage") ?? Render(template, properties);

            entry = new LogEntryResponse(
                root.TryGetProperty("Timestamp", out var timestamp) && timestamp.TryGetDateTimeOffset(out var date)
                    ? date
                    : DateTimeOffset.MinValue,
                GetString(root, "Level") ?? "Information",
                message,
                GetString(root, "Exception"),
                properties.GetValueOrDefault("RequestMethod"),
                properties.GetValueOrDefault("RequestPath"),
                int.TryParse(properties.GetValueOrDefault("StatusCode"), out var statusCode) ? statusCode : null,
                double.TryParse(properties.GetValueOrDefault("Elapsed"), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var elapsed) ? Math.Round(elapsed, 1) : null,
                properties.GetValueOrDefault("SourceContext"),
                properties.GetValueOrDefault("RequestId"),
                properties.Where(x => !KnownProperties.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value));

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ToText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? string.Empty,
        JsonValueKind.Null => string.Empty,
        _ => value.GetRawText()
    };

    // Replaces {Name}, {@Name} and {Name:format} placeholders with property values.
    private static string Render(string template, Dictionary<string, string> properties)
        => PlaceholderRegex().Replace(template, match =>
            properties.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);

    [GeneratedRegex(@"\{[@$]?([A-Za-z0-9_]+)(?:[,:][^}]*)?\}")]
    private static partial Regex PlaceholderRegex();
}
