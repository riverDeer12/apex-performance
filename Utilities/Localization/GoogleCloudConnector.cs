using Newtonsoft.Json;

namespace ApexPerformance.API.Utilities.Localization;

public static class GoogleCloudConnector
{
    /// <summary>
    /// Sends request to Google Cloud
    /// handmade method which using Google Translate
    /// translates desired text to target language.
    /// </summary>
    /// <param name="translateServiceUrl"></param>
    /// <param name="textToTranslate"></param>
    /// <param name="sourceLanguage"></param>
    /// <param name="targetLanguage"></param>
    /// <returns></returns>
    public static async Task<TranslateTextResponse> TranslateText(string translateServiceUrl, string textToTranslate,
        string sourceLanguage,
        string targetLanguage)
    {
        var httpClient = new HttpClient();

        using var response = await httpClient.PostAsync(translateServiceUrl
            , new MultipartFormDataContent
            {
                { new StringContent(textToTranslate), "text" },
                { new StringContent(sourceLanguage), "source_lang" },
                { new StringContent(targetLanguage), "target_lang" }
            });
        response.EnsureSuccessStatusCode();

        return JsonConvert.DeserializeObject<TranslateTextResponse>(await response.Content.ReadAsStringAsync());
    }
}