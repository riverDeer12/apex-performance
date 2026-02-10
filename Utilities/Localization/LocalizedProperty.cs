using Newtonsoft.Json;

namespace ApexPerformance.API.Utilities.Localization;

public class LocalizedProperty
{
    public Dictionary<Language, string> Translations { get; set; } = new();

    public string[] TranslatedTo => Translations?.Keys?.Select(_ => _.ToString()).ToArray() ?? Array.Empty<string>();
    public string[] NotTranslatedTo => Enum.GetNames(typeof(Language)).Where(_ => !TranslatedTo.Contains(_)).ToArray();

    public LocalizedProperty()
    {
    }

    public LocalizedProperty(string persistedJson)
    {
        try
        {
            Translations = JsonConvert.DeserializeObject<Dictionary<Language, string>>(persistedJson) ??
                           new Dictionary<Language, string>();
        }
        catch (Exception e)
        {
            Translations = new Dictionary<Language, string>();
        }
    }

    public string ToJsonString() => Translations == null || Translations.Count == 0
        ? null
        : JsonConvert.SerializeObject(Translations);

    /// <summary>
    /// This method makes request to
    /// Google Cloud Translator with source
    /// language and text that needs to be
    /// translated. It returns JSON string
    /// of LocalizedProperty class.
    /// </summary>
    /// <param name="translateServiceUrl"></param>
    /// <param name="sourceLanguage"></param>
    /// <param name="textForTranslation"></param>
    /// <returns></returns>
    public static async Task<string> PopulateMissingLanguages(string translateServiceUrl, Language sourceLanguage,
        string textForTranslation)
    {
        var translationLanguages = Enum.GetNames(typeof(Language))
            .Where(x => x != sourceLanguage.ToString()).ToList();

        var localizedProperty = new LocalizedProperty();

        foreach (var translationLanguage in translationLanguages)
        {
            var translationResponse = await
                GoogleCloudConnector.TranslateText(translateServiceUrl,
                    textForTranslation, sourceLanguage.ToString(), translationLanguage);
            
            Enum.TryParse<Language>(translationLanguage, ignoreCase: true, out var language);

            localizedProperty.Translations.Add(language, translationResponse.TranslatedText);
        }

        return localizedProperty.ToJsonString();
    }
}