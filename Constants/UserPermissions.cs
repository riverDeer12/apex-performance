using System.Reflection;
using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Constants;

public static class UserPermissions
{
    /**
     * Administrators Permissions
     */
    public static readonly Permission CanGetAdministrators =
        Permission.Init("3808be7c-782b-4fcf-8d2b-b9cd3a2bb8ee", nameof(CanGetAdministrators),
            "Allows viewing administrators.",
            "Administrators");

    public static readonly Permission CanCreateAdministrator =
        Permission.Init("d7775310-c4ff-4fd4-bf3b-719d85b60b4c", nameof(CanCreateAdministrator),
            "Allows creating administrator.",
            "Administrators");

    public static readonly Permission CanUpdateAdministrator =
        Permission.Init("5bdaa142-aae1-4225-8e8e-2539e74bd616", nameof(CanUpdateAdministrator),
            "Allows updating administrator.",
            "Administrators");

    public static readonly Permission CanDeleteAdministrator =
        Permission.Init("2070c041-997a-43d6-8a01-ba04c8f1b1ed", nameof(CanDeleteAdministrator),
            "Allows deleting administrator.",
            "Administrators");

    /**
     * Appointments Permissions
     */
    public static readonly Permission CanGetAppointments =
        Permission.Init("c1a4f730-9c92-47c0-97b0-9ce7e94fc20a", nameof(CanGetAppointments),
            "Allows viewing appointments.",
            "Appointments");

    public static readonly Permission CanCreateAppointment =
        Permission.Init("11c09349-6f32-4a43-a4b2-dbd58c244b1a", nameof(CanCreateAppointment),
            "Allows creating appointment.",
            "Appointments");

    public static readonly Permission CanUpdateAppointment =
        Permission.Init("f9fa3e15-8819-48f0-8751-02cf42e22a1d", nameof(CanUpdateAppointment),
            "Allows updating appointment.",
            "Appointments");

    public static readonly Permission CanDeleteAppointment =
        Permission.Init("6a6c1fd6-8c28-49cf-8a71-91bead303a6f", nameof(CanDeleteAppointment),
            "Allows deleting appointment.",
            "Appointments");
    
    public static readonly Permission CanApproveAppointment =
        Permission.Init("5a0c2c49-6e9a-4c45-a6db-d50f802816ef", nameof(CanApproveAppointment),
            "Allows approving appointment.",
            "Appointments");
    
    public static readonly Permission CanDeclineAppointment =
        Permission.Init("3a3f30a7-c0fc-43e7-aac0-861a53836479", nameof(CanDeclineAppointment),
            "Allows declining appointment.",
            "Appointments");
    
    public static readonly Permission CanProgressAppointment =
        Permission.Init("b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d", nameof(CanProgressAppointment),
            "Allows progressing appointment.",
            "Appointments");

    /**
     * Body Measurements Permissions
     */
    public static readonly Permission CanGetBodyMeasurements =
        Permission.Init("3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae", nameof(CanGetBodyMeasurements),
            "Allows viewing body measurements.",
            "BodyMeasurements");

    public static readonly Permission CanCreateBodyMeasurement =
        Permission.Init("c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f", nameof(CanCreateBodyMeasurement),
            "Allows creating body measurements.",
            "BodyMeasurements");

    public static readonly Permission CanUpdateBodyMeasurement =
        Permission.Init("a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59", nameof(CanUpdateBodyMeasurement),
            "Allows updating body measurements.",
            "BodyMeasurements");

    public static readonly Permission CanDeleteBodyMeasurement =
        Permission.Init("8b13e0cb-4c27-497f-bf13-b2101d8f0efb", nameof(CanDeleteBodyMeasurement),
            "Allows deleting body measurements.",
            "BodyMeasurements");

    /**
     * Clients Permissions
     */
    public static readonly Permission CanGetClients =
        Permission.Init("17ef9141-208a-491a-9cb7-84d4f8375fb9", nameof(CanGetClients),
            "Allows viewing clients.",
            "Clients");

    public static readonly Permission CanCreateClient =
        Permission.Init("28ab969b-c866-470f-b3b4-7c1f1b066a48", nameof(CanCreateClient),
            "Allows creating client.",
            "Clients");

    public static readonly Permission CanUpdateClient =
        Permission.Init("718efa10-761a-4e44-8eda-eae6db4cb0a3", nameof(CanUpdateClient),
            "Allows updating client.",
            "Clients");

    public static readonly Permission CanDeleteClient =
        Permission.Init("b22c672f-6d69-4674-b9cd-5fbb8d497e7a", nameof(CanDeleteClient),
            "Allows deleting client.",
            "Clients");

    /**
     * Coaches Permissions
     */
    public static readonly Permission CanGetCoaches =
        Permission.Init("3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22", nameof(CanGetCoaches),
            "Allows viewing coaches.",
            "Coaches");

    public static readonly Permission CanCreateCoach =
        Permission.Init("d8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d", nameof(CanCreateCoach),
            "Allows creating coach.",
            "Coaches");

    public static readonly Permission CanUpdateCoach =
        Permission.Init("7aeb7c60-844b-4a38-b1ae-55829b8e5f3a", nameof(CanUpdateCoach),
            "Allows updating coach.",
            "Coaches");

    public static readonly Permission CanDeleteCoach =
        Permission.Init("e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff\n\n", nameof(CanDeleteCoach),
            "Allows deleting coach.",
            "Coaches");

    /**
     * Users Permissions
     */
    public static readonly Permission CanGetUsers =
        Permission.Init("fa6a2e89-cf1e-4e4c-bd3a-c95365c52f81", nameof(CanGetUsers),
            "Allows viewing users.",
            "Users");

    public static readonly Permission CanCreateUser =
        Permission.Init("a4e62d67-676d-4f53-9ace-b4c600ea9718", nameof(CanCreateUser),
            "Allows creating user.",
            "Users");

    public static readonly Permission CanUpdateUser =
        Permission.Init("2fe1ad9e-4229-411f-8095-e8f289777455", nameof(CanUpdateUser),
            "Allows updating user.",
            "Users");

    public static readonly Permission CanDeleteUser =
        Permission.Init("f250b493-7826-4a43-968f-d1392d925b96", nameof(CanDeleteUser),
            "Allows deleting user.",
            "Users");


    public static List<Permission> GetUserPermissions()
    {
        return typeof(UserPermissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Permission))
            .Select(field => (Permission)field.GetValue(null))
            .ToList();
    }
}