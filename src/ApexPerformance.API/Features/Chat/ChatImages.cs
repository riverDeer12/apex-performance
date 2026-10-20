using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Extensions;
using ApexPerformance.API.Features.Profile;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record SendImageRequest(
    IFormFile File,
    // Optional caption.
    string? Content,
    // Size of the picture, so the apps can lay it out before loading it.
    int? Width,
    int? Height);

/// <summary>
/// Sends one picture (photo or GIF) to the conversation. It is stored
/// encrypted and loaded by the apps from GET .../attachments/{id}.
/// </summary>
public class SendImageEndpoint : Endpoint<SendImageRequest, ChatMessageResponse>
{
    // Apps shrink photos before sending; GIFs are sent as they are.
    public const int MaxSizeInBytes = 10 * 1024 * 1024;

    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatEncryptionService _encryption;
    private readonly IHubContext<ChatHub> _hub;

    public SendImageEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IChatEncryptionService encryption, IHubContext<ChatHub> hub)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryption = encryption;
        _hub = hub;
    }

    public override void Configure()
    {
        Post("api/chat/conversations/{id}/images");
        Roles(UserRoles.Coach, UserRoles.Client);
        AllowFileUploads();
        Options(x => x.WithTags("Chat").RequireRateLimiting(RateLimiting.ChatMessages));
    }

    public override async Task HandleAsync(SendImageRequest request, CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        var conversation = await ChatAccess.GetConversation(_context, userId, conversationId, cancellationToken);

        if (conversation is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        if (!ChatAccess.CanWrite(conversation))
            ThrowError(ErrorCodes.UnauthorizedAction);

        if (request.File is null || request.File.Length == 0)
            ThrowError(ErrorCodes.Required);

        if (request.File.Length > MaxSizeInBytes)
            ThrowError(ErrorCodes.NotValid, StatusCodes.Status413PayloadTooLarge);

        var content = request.Content?.Trim() ?? string.Empty;

        if (content.Length > 4000)
            ThrowError(ErrorCodes.NotValid);

        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);
        var data = memoryStream.ToArray();

        var contentType = DetectContentType(data);

        if (contentType is null)
            ThrowError(ErrorCodes.NotValid);

        var (encryptedData, dataKeyId) = _encryption.EncryptBytes(data, conversation.Id);
        var (encryptedContent, keyId) = _encryption.Encrypt(content, conversation.Id);
        var now = DateTimeOffset.UtcNow;

        var attachment = new ChatAttachment
        {
            ConversationId = conversation.Id,
            ContentType = contentType,
            EncryptedData = encryptedData,
            KeyId = dataKeyId,
            CreatedAt = now
        };

        _context.ChatAttachments.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderUserId = userId,
            Type = ChatMessageType.Image,
            EncryptedContent = encryptedContent,
            KeyId = keyId,
            AttachmentId = attachment.Id,
            AttachmentContentType = contentType,
            AttachmentWidth = request.Width is > 0 ? request.Width : null,
            AttachmentHeight = request.Height is > 0 ? request.Height : null,
            CreatedAt = now
        };

        _context.ChatMessages.Add(message);
        conversation.LastMessageAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        var response = ChatAccess.ToResponse(message, content, null);

        await SendMessageEndpoint.Deliver(_hub, conversation, userId, response, cancellationToken);

        await SendAsync(response, cancellation: cancellationToken);
    }

    // From the file's content, not its name or the header the app sent.
    private static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 6 && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)))
            return "image/gif";

        return ProfilePictureRules.DetectContentType(bytes);
    }
}

/// <summary>
/// Picture of an image message, for the two participants only.
/// </summary>
public class GetAttachmentEndpoint : EndpointWithoutRequest
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatEncryptionService _encryption;

    public GetAttachmentEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IChatEncryptionService encryption)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryption = encryption;
    }

    public override void Configure()
    {
        Get("api/chat/conversations/{id}/attachments/{attachmentId}");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);
        var attachmentId = Route<Guid>("attachmentId", isRequired: true);

        var isVisible = await ChatAccess.VisibleConversations(_context, _currentUserService.UserId)
            .AnyAsync(x => x.Id == conversationId, cancellationToken);

        var attachment = isVisible
            ? await _context.ChatAttachments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == attachmentId && x.ConversationId == conversationId,
                    cancellationToken)
            : null;

        var data = attachment is null
            ? null
            : _encryption.DecryptBytes(attachment.EncryptedData, attachment.KeyId, conversationId);

        if (attachment is null || data is null)
        {
            await SendNotFoundAsync(cancellationToken);
            return;
        }

        // A picture never changes, the apps can keep it.
        HttpContext.Response.Headers.CacheControl = "private, max-age=31536000, immutable";

        await SendBytesAsync(data, contentType: attachment.ContentType, cancellation: cancellationToken);
    }
}
