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
}
