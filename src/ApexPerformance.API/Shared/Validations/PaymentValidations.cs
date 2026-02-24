using Stripe.Checkout;

namespace ApexPerformance.API.Shared.Validations;

public static class PaymentValidations
{
    private static readonly HashSet<string> Eu = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","HU","IE","IT",
        "LV","LT","LU","MT","NL","PL","PT","RO","SK","SI","ES","SE"
    };

    public static bool IsOutsideEu(Session session)
    {
        var country = session?.CustomerDetails?.Address?.Country
                      ?? session?.Customer?.Address?.Country;

        if (string.IsNullOrWhiteSpace(country)) return false;

        return !Eu.Contains(country.Trim());
    }
}