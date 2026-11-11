namespace ApexPerformance.API.Constants;

/// <summary>
/// Who a shop product is for. Unisex products are shown under both;
/// products without a gender (bottles, bags) are for everyone.
/// </summary>
public static class ShopGenders
{
    public const string Women = "women";
    public const string Men = "men";
    public const string Unisex = "unisex";

    public static readonly string[] All = [Women, Men, Unisex];
}
