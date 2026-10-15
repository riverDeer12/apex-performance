using System.Globalization;
using System.Net;
using System.Text;
using ApexPerformance.API.Features.Logs;
using ApexPerformance.API.Services.Interfaces;
using Hangfire;

namespace ApexPerformance.API.BackgroundJobs;

/// <summary>
/// Emails new errors from the Logs folder: errors of the application and
/// failed nightly backups (the backup script writes them to backup-*.json
/// there). One email per run at most, so a burst of errors is one email.
/// Runs everywhere except development and test by default, "ErrorAlerts:Enabled" turns it on or off
/// and "ErrorAlerts:Email" changes the recipient.
/// </summary>
public class ErrorAlertJob
{
    private const string JobId = "error-alerts";
    private const string Schedule = "*/10 * * * *";
    private const string DefaultEmail = "info@rdd.software";

    // Last checked time is kept in Hangfire storage, so it survives restarts and deploys.
    private const string StateKey = "error-alerts";
    private const string LastCheckedField = "LastCheckedAt";

    // Source the backup script writes its errors with.
    private const string BackupSource = "ApexBackup";

    private const int MaxListedEntries = 20;

    private static readonly string[] AlertLevels = ["Error", "Fatal"];

    // Requests the client cancelled (closed the app, lost the connection) are
    // logged as errors by the framework but are not problems of the application.
    private static readonly string[] CancellationExceptions =
        ["System.Threading.Tasks.TaskCanceledException", "System.OperationCanceledException"];

    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<ErrorAlertJob> _logger;

    public ErrorAlertJob(IWebHostEnvironment environment, IConfiguration configuration, IEmailService emailService,
        ILogger<ErrorAlertJob> logger)
    {
        _environment = environment;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public static void ScheduleJob(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var enabled = configuration.GetValue<bool?>("ErrorAlerts:Enabled") ??
                      !(environment.IsDevelopment() || environment.IsEnvironment("Test"));

        if (enabled)
            RecurringJob.AddOrUpdate<ErrorAlertJob>(JobId, job => job.Run(CancellationToken.None), Schedule);
        else
            RecurringJob.RemoveIfExists(JobId);
    }

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task Run(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        using var connection = JobStorage.Current.GetConnection();

        var state = connection.GetAllEntriesFromHash(StateKey);
        var lastCheckedText = state?.GetValueOrDefault(LastCheckedField);

        // First run starts from now, old errors were already there before alerts existed.
        if (DateTimeOffset.TryParse(lastCheckedText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var lastChecked))
        {
            var entries = await ReadNewErrors(lastChecked, now, cancellationToken);

            if (entries.Count > 0)
                SendAlert(entries);
        }

        connection.SetRangeInHash(StateKey,
            [new KeyValuePair<string, string>(LastCheckedField, now.ToString("O"))]);
    }

    private async Task<List<(string File, LogEntryResponse Entry)>> ReadNewErrors(DateTimeOffset from,
        DateTimeOffset to, CancellationToken cancellationToken)
    {
        var errors = new List<(string File, LogEntryResponse Entry)>();

        // Windows doesn't always update the modified time of a file that is still
        // open for writing, so the files of the last day are read too.
        var files = LogFileReader.GetFiles(_environment)
            .Where(x => x.LastModified >= from.AddDays(-1));

        foreach (var file in files)
        {
            var path = LogFileReader.ResolveFile(_environment, file.Name);

            if (path is null)
                continue;

            var entries = await LogFileReader.ReadAsync(path, cancellationToken);

            errors.AddRange(entries
                .Where(x => AlertLevels.Contains(x.Level, StringComparer.OrdinalIgnoreCase))
                .Where(x => x.Exception is null ||
                            !CancellationExceptions.Any(name => x.Exception.StartsWith(name, StringComparison.Ordinal)))
                .Where(x => x.Timestamp > from && x.Timestamp <= to)
                .Select(x => (file.Name, x)));
        }

        return errors.OrderBy(x => x.Entry.Timestamp).ToList();
    }

    private void SendAlert(List<(string File, LogEntryResponse Entry)> errors)
    {
        var to = _configuration["ErrorAlerts:Email"] ?? DefaultEmail;
        var site = _configuration["WebAppUrl"] ?? _environment.EnvironmentName;

        var backupFailed = errors.Any(x => x.Entry.SourceContext == BackupSource);

        var subject = backupFailed
            ? $"Apex Performance: backup nije uspio ({site})"
            : $"Apex Performance: {errors.Count} {(errors.Count == 1 ? "greška" : "grešaka")} u logovima ({site})";

        string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var croatia = GetCroatianTimeZone();

        var rows = new StringBuilder();

        foreach (var (file, entry) in errors.TakeLast(MaxListedEntries))
        {
            var time = TimeZoneInfo.ConvertTime(entry.Timestamp, croatia).ToString("dd.MM.yyyy. HH:mm:ss");
            var request = entry.RequestPath is null ? "" : $"<br><small>{Encode(entry.RequestMethod)} {Encode(entry.RequestPath)}</small>";
            var exception = entry.Exception is null
                ? ""
                : $"<pre style=\"white-space:pre-wrap;font-size:12px;margin:6px 0 0;color:#555\">{Encode(string.Join('\n', entry.Exception.Split('\n').Take(8)))}</pre>";

            rows.Append(
                "<tr>" +
                $"<td style=\"padding:8px;border-bottom:1px solid #ddd;vertical-align:top;white-space:nowrap\">{time}<br><small>{Encode(entry.Level)}</small></td>" +
                $"<td style=\"padding:8px;border-bottom:1px solid #ddd;vertical-align:top\"><b>{Encode(entry.Message)}</b>{request}" +
                $"<br><small>{Encode(entry.SourceContext)} · {Encode(file)}</small>{exception}</td>" +
                "</tr>");
        }

        var more = errors.Count > MaxListedEntries
            ? $"<p>Prikazano je zadnjih {MaxListedEntries} od {errors.Count} zapisa, ostali su u logovima.</p>"
            : "";

        var logsLink = $"{_configuration["WebAppUrl"]}/admin/users/logs";

        var html =
            "<div style=\"font-family:Segoe UI,Arial,sans-serif;color:#222;max-width:800px\">" +
            $"<h2 style=\"margin-bottom:4px\">{Encode(subject)}</h2>" +
            $"<p>Novi zapisi razine Error u logovima aplikacije {Encode(site)}:</p>" +
            $"<table cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"border-collapse:collapse\">{rows}</table>" +
            more +
            $"<p><a href=\"{Encode(logsLink)}\">Otvori logove u aplikaciji</a></p>" +
            "</div>";

        _emailService.SendErrorAlertEmail(to, subject, html);

        _logger.LogInformation("Sent alert about {Count} errors to {Email}.", errors.Count, to);
    }

    private static TimeZoneInfo GetCroatianTimeZone()
    {
        // IANA id on Linux and newer Windows, Windows id as fallback.
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Zagreb", out var timeZone))
            return timeZone;

        return TimeZoneInfo.TryFindSystemTimeZoneById("Central European Standard Time", out timeZone)
            ? timeZone
            : TimeZoneInfo.Utc;
    }
}
