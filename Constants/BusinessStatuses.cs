using System.Reflection;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;

namespace ApexPerformance.API.Constants;

public static class BusinessStatuses
{
    public static readonly AppointmentStatus Approved =
        AppointmentStatus.Init("9f1a0b47-1f9d-4a6d-bcfc-5ed5e91cdaf7", nameof(Approved),
            "Approved status.");

    public static readonly AppointmentStatus Declined =
        AppointmentStatus.Init("4f82c9d9-3346-4c53-8d36-2f6e7186e1a3", nameof(Declined),
            "Declined status.");

    public static readonly AppointmentStatus InProgress =
        AppointmentStatus.Init("6a79d224-c6a5-4f14-90ad-ec9e7a2743c2", nameof(InProgress),
            "In Progress status.");

    public static readonly AppointmentStatus Pending =
        AppointmentStatus.Init("27c2b8b8-d95d-4e35-8df6-90f8b31a22aa", nameof(Pending),
            "Pending status.");
    
    public static readonly AppointmentStatus Canceled =
        AppointmentStatus.Init("e2031af4-e2d7-440d-a88b-b7e09fff9805", nameof(Canceled),
            "Canceled status.");

    public static List<AppointmentStatus> GetBusinessStatuses()
    {
        return typeof(BusinessStatuses)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(AppointmentStatus))
            .Select(field => (AppointmentStatus)field.GetValue(null))
            .ToList();
    }
}