using System.Reflection;
using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Constants;

public static class UserPermissions
{
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


    public static List<Permission> GetUserPermissions()
    {
        return typeof(UserPermissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(Permission))
            .Select(field => (Permission)field.GetValue(null))
            .ToList();
    }
}