namespace ApexPerformance.API.Utilities;

/// <summary>
/// Set of methods that are useful
/// when working with YouTube videos.
/// </summary>
public static class YoutubeHelper
{
    public static string GetYoutubeThumbnail(string videoUrl)
    {
        var videoId = ExtractVideoId(videoUrl);

        return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
    }

    private static string ExtractVideoId(string url)
    {
        var uri = new Uri(url);

        if (uri.Host.Contains("youtu.be"))
            return uri.AbsolutePath.Trim('/');

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["v"]!;
    }
}