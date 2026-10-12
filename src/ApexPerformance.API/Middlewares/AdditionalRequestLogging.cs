using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApexPerformance.API.Middlewares;

public class AdditionalRequestLogging
{
    // Long bodies (imports, pictures) are cut in the log.
    private const int MaxBodyLength = 4000;

    private static readonly string[] SensitiveNameParts = ["password", "token", "secret"];

    private readonly RequestDelegate _next;
    private readonly ILogger<AdditionalRequestLogging> _logger;

    public AdditionalRequestLogging(RequestDelegate next, ILogger<AdditionalRequestLogging> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Processing request from {@RequestUser}, at {@DateTimeUTC}",
            context.User.Identity?.Name,
            DateTime.UtcNow
        );

        context.Request.EnableBuffering(); // Allows us to read the body multiple times

        if (context.Request.Method != "GET" && IsTextBody(context.Request.ContentType))
        {
            var requestBody = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();

            context.Request.Body.Position = 0; // Reset stream position for the next reader

            _logger.LogInformation("Request Body: {RequestBody}", Redact(requestBody));
        }

        await _next(context);

        _logger.LogInformation("Request Completed");
    }

    private static bool IsTextBody(string? contentType)
        => contentType is null || contentType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
           contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Passwords, tokens and secrets are never written to the log.
    /// </summary>
    public static string Redact(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        try
        {
            var node = JsonNode.Parse(body);
            RedactNode(node);
            body = node?.ToJsonString() ?? body;
        }
        catch (JsonException)
        {
            // Not JSON, logged as it is.
        }

        return body.Length > MaxBodyLength ? body[..MaxBodyLength] + "…" : body;
    }

    private static void RedactNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (SensitiveNameParts.Any(part => property.Key.Contains(part, StringComparison.OrdinalIgnoreCase)))
                        jsonObject[property.Key] = "***";
                    else
                        RedactNode(property.Value);
                }
                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                    RedactNode(item);
                break;
        }
    }
}
