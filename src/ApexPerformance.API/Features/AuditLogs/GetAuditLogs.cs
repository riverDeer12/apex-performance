using System.Text.Json;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.AuditLogs;

public record GetAuditLogsRequest(
    int? Page,
    int? PageSize,
    Guid? UserId,
    string? EntityType,
    string? EntityId,
    string? Action,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search
);

public record AuditChangeResponse(string Property, string? OldValue, string? NewValue);

public record AuditLogResponse(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid? UserId,
    string? UserName,
    string Action,
    string EntityType,
    string EntityId,
    string? EntityName,
    List<AuditChangeResponse> Changes
);

public record AuditLogUserResponse(Guid Id, string Name);

public record GetAuditLogsResponse(
    List<AuditLogResponse> Items,
    int Total,
    List<string> EntityTypes,
    List<AuditLogUserResponse> Users
);

/// <summary>
/// Change history of the system: who changed what and when.
/// </summary>
public class GetAuditLogsEndpoint : Endpoint<GetAuditLogsRequest, GetAuditLogsResponse>
{
    private const int MaxPageSize = 100;

    private readonly ApexPerformanceContext _context;

    public GetAuditLogsEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Get("api/audit-logs");
        Roles(UserRoles.SuperAdmin);
        Options(x => x.WithTags("AuditLogs"));
    }

    public override async Task HandleAsync(GetAuditLogsRequest request, CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page ?? 1, 1);
        var pageSize = Math.Clamp(request.PageSize ?? 25, 1, MaxPageSize);

        var query = _context.AuditLogs.AsNoTracking();

        if (request.UserId is not null)
            query = query.Where(x => x.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.EntityType))
            query = query.Where(x => x.EntityType == request.EntityType);

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(x => x.EntityId == request.EntityId);

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(x => x.Action == request.Action);

        if (request.From is not null)
            query = query.Where(x => x.Timestamp >= request.From);

        if (request.To is not null)
            query = query.Where(x => x.Timestamp < request.To);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => (x.EntityName != null && x.EntityName.Contains(search)) ||
                                     (x.Changes != null && x.Changes.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);

        var logs = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var entityTypes = await _context.AuditLogs
            .Select(x => x.EntityType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var userIds = await _context.AuditLogs
            .Where(x => x.UserId != null)
            .Select(x => x.UserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var userNames = await GetUserNames(userIds, cancellationToken);

        var items = logs.Select(x => new AuditLogResponse(
                x.Id,
                x.Timestamp,
                x.UserId,
                x.UserId is null ? null : userNames.GetValueOrDefault(x.UserId.Value),
                x.Action,
                x.EntityType,
                x.EntityId,
                x.EntityName,
                ParseChanges(x.Changes)))
            .ToList();

        var users = userNames
            .Select(x => new AuditLogUserResponse(x.Key, x.Value))
            .OrderBy(x => x.Name)
            .ToList();

        await SendAsync(new GetAuditLogsResponse(items, total, entityTypes, users), cancellation: cancellationToken);
    }

    private async Task<Dictionary<Guid, string>> GetUserNames(List<Guid> userIds, CancellationToken cancellationToken)
        => await _context.Users
            .IgnoreQueryFilters()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Name = x.Administrator != null
                    ? x.Administrator.FirstName + " " + x.Administrator.LastName
                    : x.Coach != null
                        ? x.Coach.FirstName + " " + x.Coach.LastName
                        : x.Client != null
                            ? x.Client.FirstName + " " + x.Client.LastName
                            : x.UserName
            })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

    private static List<AuditChangeResponse> ParseChanges(string? changes)
    {
        if (string.IsNullOrWhiteSpace(changes))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<AuditChangeResponse>>(changes) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
