using System.Globalization;
using System.Text.Json;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ApexPerformance.API.Database;

public record AuditChange(string Property, string? OldValue, string? NewValue);

/// <summary>
/// Collects changes of entities before they are saved and turns them into
/// audit log rows after saving (when ids of new entities are known).
/// </summary>
public static class AuditTrail
{
    private const int MaxValueLength = 300;

    // Technical or sensitive data that is not shown in the change history.
    private static readonly HashSet<Type> IgnoredTypes =
    [
        typeof(UserSession),
        typeof(DeviceToken),
        typeof(AuditLog)
    ];

    // Audit fields are already part of every log row.
    private static readonly HashSet<string> IgnoredProperties =
    [
        nameof(BaseEntity.CreatedAt), nameof(BaseEntity.CreatedBy),
        nameof(BaseEntity.UpdatedAt), nameof(BaseEntity.UpdatedBy),
        nameof(BaseEntity.DeletedAt), "PeriodStart", "PeriodEnd"
    ];

    private static readonly string[] SensitiveNameParts = ["password", "token", "secret"];

    private static readonly string[] NameProperties = ["FullName", "Name", "Title", "UserName", "Email"];

    public sealed class PendingEntry(EntityEntry entry, string action, List<AuditChange> changes)
    {
        public EntityEntry Entry { get; } = entry;
        public string Action { get; } = action;
        public List<AuditChange> Changes { get; } = changes;
    }

    public static List<PendingEntry> Collect(ChangeTracker changeTracker)
    {
        var pending = new List<PendingEntry>();

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.Entity is not BaseEntity || IgnoredTypes.Contains(entry.Entity.GetType()))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    pending.Add(new PendingEntry(entry, AuditActions.Created, entry.Properties
                        .Where(IsVisible)
                        .Where(x => x.CurrentValue is not null && x.Metadata.Name != nameof(BaseEntity.IsDeleted))
                        .Select(x => new AuditChange(x.Metadata.Name, null, Format(x.CurrentValue)))
                        .ToList()));
                    break;

                case EntityState.Modified:
                    var changes = entry.Properties
                        .Where(x => x.IsModified && IsVisible(x) && !Equals(x.OriginalValue, x.CurrentValue))
                        .Select(x => new AuditChange(x.Metadata.Name, Format(x.OriginalValue), Format(x.CurrentValue)))
                        .ToList();

                    var deletedChange = changes.FirstOrDefault(x => x.Property == nameof(BaseEntity.IsDeleted));

                    // Soft delete is shown as deletion (or restoring), not as a change of the flag.
                    var action = deletedChange is null
                        ? AuditActions.Updated
                        : deletedChange.NewValue == "True" ? AuditActions.Deleted : AuditActions.Restored;

                    if (deletedChange is not null)
                        changes.Remove(deletedChange);

                    if (changes.Count > 0 || action != AuditActions.Updated)
                        pending.Add(new PendingEntry(entry, action, changes));
                    break;

                case EntityState.Deleted:
                    pending.Add(new PendingEntry(entry, AuditActions.Deleted, []));
                    break;
            }
        }

        return pending;
    }

    public static List<AuditLog> ToAuditLogs(IEnumerable<PendingEntry> pending, Guid userId)
    {
        var timestamp = DateTimeOffset.UtcNow;

        return pending.Select(x => new AuditLog
        {
            Timestamp = timestamp,
            UserId = userId == Guid.Empty ? null : userId,
            Action = x.Action,
            EntityType = x.Entry.Metadata.ClrType.Name,
            EntityId = Truncate(string.Join(",", x.Entry.Metadata.FindPrimaryKey()?.Properties
                .Select(key => Format(x.Entry.Property(key.Name).CurrentValue)) ?? []), 64),
            EntityName = GetName(x.Entry),
            Changes = x.Changes.Count > 0 ? JsonSerializer.Serialize(x.Changes) : null
        }).ToList();
    }

    private static bool IsVisible(PropertyEntry property)
    {
        var name = property.Metadata.Name;

        if (IgnoredProperties.Contains(name) || property.Metadata.IsPrimaryKey() || property.Metadata.IsShadowProperty())
            return false;

        return !SensitiveNameParts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetName(EntityEntry entry)
    {
        var properties = entry.Properties.ToDictionary(x => x.Metadata.Name, x => x.CurrentValue);

        if (properties.TryGetValue("FirstName", out var firstName) && properties.TryGetValue("LastName", out var lastName))
            return Truncate($"{firstName} {lastName}".Trim(), 200);

        foreach (var name in NameProperties)
        {
            if (properties.TryGetValue(name, out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
                return Truncate(text, 200);
        }

        return null;
    }

    private static string? Format(object? value) => value switch
    {
        null => null,
        byte[] => "[binary]",
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => Truncate(value.ToString() ?? string.Empty, MaxValueLength)
    };

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..(length - 1)] + "…";
}
