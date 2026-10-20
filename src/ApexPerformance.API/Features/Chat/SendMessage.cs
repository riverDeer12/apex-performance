using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Extensions;
using ApexPerformance.API.Hubs;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record SendMessageRequest(
    string? Content,
    // Coach only: shares a workout, the content is then optional.
    Guid? WorkoutId = null);

/// <summary>
/// Sends a message to the conversation. The content is stored encrypted,
/// both sides get it live through the chat hub and the other side also
/// gets a push notification (without the content of the message).
/// A coach can share a workout, which opens in the client's app.
/// </summary>
public class SendMessageEndpoint : Endpoint<SendMessageRequest, ChatMessageResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatEncryptionService _encryption;
    private readonly IHubContext<ChatHub> _hub;
    private readonly INotificationService _notificationService;

    public SendMessageEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IChatEncryptionService encryption, IHubContext<ChatHub> hub, INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryption = encryption;
        _hub = hub;
        _notificationService = notificationService;
    }

    public override void Configure()
    {
        Post("api/chat/conversations/{id}/messages");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat").RequireRateLimiting(RateLimiting.ChatMessages));
    }

    public override async Task HandleAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        var conversation = await ChatAccess.GetConversation(_context, userId, conversationId, cancellationToken);

        if (conversation is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        // A disabled or blocked conversation is read only for both sides.
        if (!ChatAccess.CanWrite(conversation))
            ThrowError(ErrorCodes.UnauthorizedAction);

        Workout? workout = null;

        if (request.WorkoutId is { } workoutId)
        {
            // Only coaches share workouts with their clients.
            if (conversation.Coach.UserId != userId)
                ThrowError(ErrorCodes.UnauthorizedAction);

            workout = await _context.Workouts.AsNoTracking()
                .Include(x => x.WorkoutTypes)
                .ThenInclude(x => x.WorkoutType)
                .FirstOrDefaultAsync(x => x.Id == workoutId, cancellationToken);

            if (workout is null)
                ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);
        }

        var content = request.Content?.Trim() ?? string.Empty;
        var (encryptedContent, keyId) = _encryption.Encrypt(content, conversation.Id);

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderUserId = userId,
            Type = workout is null ? ChatMessageType.Text : ChatMessageType.Workout,
            EncryptedContent = encryptedContent,
            KeyId = keyId,
            WorkoutId = workout?.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.ChatMessages.Add(message);
        conversation.LastMessageAt = message.CreatedAt;

        await _context.SaveChangesAsync(cancellationToken);

        var response = ChatAccess.ToResponse(message, content, workout);

        await Deliver(_hub, conversation, userId, response, cancellationToken);

        await SendAsync(response, cancellation: cancellationToken);
    }

    /// <summary>
    /// Sends a new message to both sides through the hub and a push
    /// notification to the other side.
    /// </summary>
    public static async Task Deliver(IHubContext<ChatHub> hub, ChatConversation conversation, Guid senderUserId,
        ChatMessageResponse response, CancellationToken cancellationToken)
    {
        var recipientUserId = conversation.Coach.UserId == senderUserId
            ? conversation.Client.UserId
            : conversation.Coach.UserId;
        var senderName = conversation.Coach.UserId == senderUserId
            ? conversation.Coach.FullName
            : conversation.Client.FullName;

        await hub.Clients.Users(senderUserId.ToString(), recipientUserId.ToString())
            .SendAsync(ChatHub.Events.MessageReceived, response, cancellationToken);

        var conversationId = conversation.Id;
        BackgroundJob.Enqueue<SendMessageEndpoint>(endpoint =>
            endpoint.SendPushNotification(recipientUserId, senderName, conversationId));
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task SendPushNotification(Guid recipientUserId, string senderName, Guid conversationId)
    {
        var recipients = await _context.DeviceTokens
            .Where(x => x.UserId == recipientUserId)
            .Select(x => new PushRecipient(x.Token, x.Language, x.UserId))
            .ToListAsync();

        var data = PushNotificationTypes.Data(PushNotificationTypes.ChatMessage);
        data["conversationId"] = conversationId.ToString();

        await _notificationService.SendLocalized(recipients, language => PushTexts.ChatMessage(language, senderName),
            data);
    }
}

public sealed class SendMessageValidator : Validator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.Content)
            .Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage(ErrorCodes.Required)
            .When(x => x.WorkoutId is null);
        RuleFor(x => x.Content)
            .MaximumLength(4000).WithMessage(ErrorCodes.NotValid);
    }
}
