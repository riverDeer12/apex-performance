using System.Reflection;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Constants;

public static class BusinessStatuses
{
    public const string Approved = nameof(Approved);
    public const string Declined = nameof(Declined);
    public const string InProgress = nameof(InProgress);
    public const string Pending = nameof(Pending);
    public const string Canceled = nameof(Canceled);
}