namespace ApexPerformance.API.Utilities;

/// <summary>
/// Set of methods that are useful
/// when working with YouTube videos.
/// </summary>
public static class YoutubeHelper
{
    public static string GetYoutubeThumbnail(string videoUrl)
    {
        if (!TryExtractVideoId(videoUrl, out var videoId))
            throw new ArgumentException($"'{videoUrl}' is not a valid YouTube video URL.", nameof(videoUrl));

        return $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
    }

    /// <summary>
    /// Try to read video id from YouTube URL.
    /// Supports youtube.com/watch?v=, youtu.be/,
    /// youtube.com/shorts/ and youtube.com/embed/ links.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="videoId"></param>
    /// <returns></returns>
    public static bool TryExtractVideoId(string? url, out string videoId)
    {
        videoId = string.Empty;

        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;

        var host = uri.Host.ToLowerInvariant();
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (host == "youtu.be" || host.EndsWith(".youtu.be"))
        {
            videoId = segments.FirstOrDefault() ?? string.Empty;
        }
        else if (host == "youtube.com" || host.EndsWith(".youtube.com"))
        {
            if (segments.Length >= 2 && segments[0] is "shorts" or "embed" or "live")
                videoId = segments[1];
            else
                videoId = System.Web.HttpUtility.ParseQueryString(uri.Query)["v"] ?? string.Empty;
        }

        return !string.IsNullOrWhiteSpace(videoId);
    }
}
