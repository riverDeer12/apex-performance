namespace ApexPerformance.API.Constants;

public static class UserPermissions
{
    // Appointments Permissions
    public const string CanGetAppointments = nameof(CanGetAppointments);
    public const string CanCreateAppointment = nameof(CanCreateAppointment);
    public const string CanUpdateAppointment = nameof(CanUpdateAppointment);
    public const string CanDeleteAppointment = nameof(CanDeleteAppointment);
    public const string CanApproveAppointment = nameof(CanApproveAppointment);
    public const string CanDeclineAppointment = nameof(CanDeclineAppointment);
    public const string CanProgressAppointment = nameof(CanProgressAppointment);
    public const string CanCancelAppointment = nameof(CanCancelAppointment);

    // Body Measurements Permissions
    public const string CanGetBodyMeasurements = nameof(CanGetBodyMeasurements);
    public const string CanCreateBodyMeasurement = nameof(CanCreateBodyMeasurement);
    public const string CanUpdateBodyMeasurement = nameof(CanUpdateBodyMeasurement);
    public const string CanDeleteBodyMeasurement = nameof(CanDeleteBodyMeasurement);

    // Clients Permissions
    public const string CanGetClients = nameof(CanGetClients);
    public const string CanCreateClient = nameof(CanCreateClient);
    public const string CanUpdateClient = nameof(CanUpdateClient);
    public const string CanDeleteClient = nameof(CanDeleteClient);

    // Coaches Permissions
    public const string CanGetCoaches = nameof(CanGetCoaches);
    public const string CanCreateCoach = nameof(CanCreateCoach);
    public const string CanUpdateCoach = nameof(CanUpdateCoach);
    public const string CanDeleteCoach = nameof(CanDeleteCoach);
    
    // Recurring Appointments Permissions
    public const string CanGetRecurringAppointments = nameof(CanGetRecurringAppointments);
    public const string CanCreateRecurringAppointment = nameof(CanCreateRecurringAppointment);
    public const string CanUpdateRecurringAppointment = nameof(CanUpdateRecurringAppointment);
    public const string CanDeleteRecurringAppointment = nameof(CanDeleteRecurringAppointment);
}