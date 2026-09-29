using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Profile;

public static class ProfilePictureRules
{
    // Web app resizes pictures before upload, so they are
    // usually far below this, it only protects the database.
    public const int MaxSizeInBytes = 2 * 1024 * 1024;

    /// <summary>
    /// Detect image type from file content (not from
    /// file name or header sent by client).
    /// </summary>
    public static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";

        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return "image/png";

        if (bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        return null;
    }
}

public record UploadProfilePictureRequest(IFormFile File);

public record UploadProfilePictureResponse(DateTimeOffset UpdatedAt);

/// <summary>
/// Upload profile picture of logged user,
/// replacing the previous one.
/// </summary>
public class UploadProfilePictureEndpoint : Endpoint<UploadProfilePictureRequest, UploadProfilePictureResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UploadProfilePictureEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/profile/picture");
        AllowFileUploads();
        Options(x => x.WithTags("Profile"));
    }

    public override async Task HandleAsync(UploadProfilePictureRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
            ThrowError(ErrorCodes.Required);

        if (request.File.Length > ProfilePictureRules.MaxSizeInBytes)
            ThrowError(ErrorCodes.NotValid, StatusCodes.Status413PayloadTooLarge);

        using var memoryStream = new MemoryStream();

        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var content = memoryStream.ToArray();

        var contentType = ProfilePictureRules.DetectContentType(content);

        if (contentType is null)
            ThrowError(ErrorCodes.NotValid);

        var userId = _currentUserService.UserId;

        var picture = await _context.UserProfilePictures
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (picture is null)
        {
            picture = new UserProfilePicture { UserId = userId, ContentType = contentType };
            _context.UserProfilePictures.Add(picture);
        }

        picture.Content = content;
        picture.ContentType = contentType;
        picture.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(new UploadProfilePictureResponse(picture.UpdatedAt), cancellation: cancellationToken);
    }
}

/// <summary>
/// Profile picture of logged user.
/// </summary>
public class GetProfilePictureEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProfilePictureEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Get("api/profile/picture");
        Options(x => x.WithTags("Profile"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var picture = await _context.UserProfilePictures
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

        if (picture is null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        HttpContext.Response.Headers.CacheControl = "private, no-cache";

        await SendBytesAsync(picture.Content, contentType: picture.ContentType,
            lastModified: picture.UpdatedAt, cancellation: cancellationToken);
    }
}

public class DeleteProfilePictureEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteProfilePictureEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Delete("api/profile/picture");
        Options(x => x.WithTags("Profile"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        await _context.UserProfilePictures
            .Where(x => x.UserId == _currentUserService.UserId)
            .ExecuteDeleteAsync(cancellationToken);

        await SendNoContentAsync(cancellationToken);
    }
}
