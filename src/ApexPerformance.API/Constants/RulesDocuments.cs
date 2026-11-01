namespace ApexPerformance.API.Constants;

/// <summary>
/// Cooperation rules clients accept in the app. A new version of the document
/// gets a new Version and Path, clients then accept it again.
/// </summary>
public static class RulesDocuments
{
    public const string IndividualCoaching = "individual-coaching";

    // Rules of individual trainings, accepted by clients with the PrivateCoaching plan.
    public const string IndividualCoachingVersion = "1.1";

    // PDF served by the web app (wwwroot), relative to WebAppUrl.
    public const string IndividualCoachingPath = "/assets/documents/pravila-individualni-treninzi-v1.1.pdf";

    // Translations of the same version, by app language; the Croatian PDF above is the binding one
    // and the one for any other language.
    public static readonly IReadOnlyDictionary<string, string> IndividualCoachingTranslations =
        new Dictionary<string, string>
        {
            ["en"] = "/assets/documents/pravila-individualni-treninzi-v1.1-en.pdf",
            ["it"] = "/assets/documents/pravila-individualni-treninzi-v1.1-it.pdf",
            ["de"] = "/assets/documents/pravila-individualni-treninzi-v1.1-de.pdf",
        };
}
