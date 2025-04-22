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
    
    public static readonly Permission CanGetContributors =
        Permission.Init("c9b1e9a7-8bb0-4c29-86db-cbb4389e6393", nameof(CanGetContributors),
            "Allows viewing contributors.", "Contributors");

    public static readonly Permission CanCreateContributor =
        Permission.Init("4b60b7a5-53c3-4b43-9793-b0354a29dca1", nameof(CanCreateContributor),
            "Allows creating new contributors.", "Contributors");

    public static readonly Permission CanUpdateContributor =
        Permission.Init("2f54d3f8-4f36-45f9-914d-12231b9c7a77", nameof(CanUpdateContributor),
            "Allows updating existing contributors.", "Contributors");

    public static readonly Permission CanDeleteContributor =
        Permission.Init("1b5a4f8f-4478-4e66-b636-4b7f5c3cdb28", nameof(CanDeleteContributor),
            "Allows deleting contributors.", "Contributors");

    public static readonly Permission CanGetRecipes =
        Permission.Init("6bfa4f27-39cd-4a23-9a90-503b7562de09", nameof(CanGetRecipes), "Allows viewing recipes.",
            "Recipes");

    public static readonly Permission CanCreateRecipe =
        Permission.Init("74253b8e-6b5d-48f4-becb-02cc909de4bc", nameof(CanCreateRecipe), "Allows creating recipes.",
            "Recipes");

    public static readonly Permission CanUpdateRecipe =
        Permission.Init("d07047e2-9c97-4f86-bb69-46699f9935b1", nameof(CanUpdateRecipe), "Allows updating recipes.",
            "Recipes");

    public static readonly Permission CanDeleteRecipe =
        Permission.Init("b23e50c8-9e16-44b3-b7d4-b4e470306f87", nameof(CanDeleteRecipe), "Allows deleting recipes.",
            "Recipes");

    public static readonly Permission CanGetCategories =
        Permission.Init("d6fbb48f-5b7b-480c-8a12-117a8b5a68fd", nameof(CanGetCategories),
            "Allows viewing recipe categories.",
            "Categories");

    public static readonly Permission CanCreateCategory =
        Permission.Init("2875f239-3d11-452e-acdd-b8bff6a50970", nameof(CanCreateCategory),
            "Allows creating recipe categories.",
            "Categories");

    public static readonly Permission CanUpdateCategory =
        Permission.Init("e8f80d47-e956-4e05-a91b-d51315e629fd", nameof(CanUpdateCategory),
            "Allows updating recipe category.",
            "Categories");

    public static readonly Permission CanDeleteCategory =
        Permission.Init("c02b2833-ded9-4e6b-af6a-74a06cb2ffe0", nameof(CanDeleteCategory),
            "Allows deleting recipe category.",
            "Categories");
    
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