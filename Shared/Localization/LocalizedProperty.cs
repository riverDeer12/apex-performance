using Newtonsoft.Json;

namespace ApexPerformance.API.Shared.Localization;

public class LocalizedProperty
{
    public Dictionary<Language, string> Translations;

    public string[] TranslatedTo => Translations?.Keys?.Select(_ => _.ToString()).ToArray() ?? Array.Empty<string>();
    public string[] NotTranslatedTo => Enum.GetNames(typeof(Language)).Where(_ => !TranslatedTo.Contains(_)).ToArray();

    public LocalizedProperty()
    {
    }

    public LocalizedProperty(string persistedJson)
    {
        // NOTE-IZ: If persisted data can not be deserialized, we want to return empty dictionary.
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
}