using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Services.Interfaces;
using ApexPerformance.API.Features.Workouts;
using ApexPerformance.API.Shared.DataTransferObjects;
using ApexPerformance.API.Utilities;
using ApexPerformance.API.Utilities.Localization;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

/// <summary>
/// Only the coach and the client of a conversation can read it, nobody else
/// (administrators included), and only while the client is still one of the
/// coach's clients. A conversation the coach disabled stays visible to both
/// sides as read only.
/// </summary>
public static class ChatAccess
{
    public static IQueryable<ChatConversation> VisibleConversations(ApexPerformanceContext context, Guid userId)
        => context.ChatConversations
            .Where(x => x.Coach.UserId == userId || x.Client.UserId == userId)
            .Where(x => context.CoachClients.Any(coachClient =>
                coachClient.CoachId == x.CoachId && coachClient.ClientId == x.ClientId));

    public static async Task<ChatConversation?> GetConversation(ApexPerformanceContext context, Guid userId,
        Guid conversationId, CancellationToken cancellationToken)
        => await VisibleConversations(context, userId)
            .Include(x => x.Coach)
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

    // Nobody can write while the coach has it disabled or the client blocked it.
    public static bool CanWrite(ChatConversation conversation)
        => conversation.IsEnabled && !conversation.IsBlockedByClient;

    // Response without the last message, after the conversation was created or changed.
    public static ChatConversationResponse ToResponse(ChatConversation conversation, Coach coach, Client client)
        => new(conversation.Id, Participant(coach), Participant(client), CanWrite(conversation),
            conversation.LastMessageAt, null, 0, conversation.IsBlockedByClient);

    public static ChatMessageResponse ToResponse(ChatMessage message, IChatEncryptionService encryption,
        IReadOnlyDictionary<Guid, Workout>? workouts = null)
        => ToResponse(message, encryption.Decrypt(message.EncryptedContent, message.KeyId, message.ConversationId),
            message.WorkoutId is { } workoutId ? workouts?.GetValueOrDefault(workoutId) : null);

    public static ChatMessageResponse ToResponse(ChatMessage message, string? content, Workout? workout)
        => new(message.Id, message.ConversationId, message.SenderUserId, content, message.CreatedAt, message.ReadAt,
            TypeName(message.Type),
            message.AttachmentId is { } attachmentId
                ? new ChatAttachmentResponse(attachmentId, message.AttachmentContentType ?? "image/jpeg",
                    message.AttachmentWidth, message.AttachmentHeight)
                : null,
            workout is null ? null : WorkoutResponse(workout));

    private static GetWorkoutResponse WorkoutResponse(Workout workout)
        => new(workout.Id, new LocalizedProperty(workout.Name), new LocalizedProperty(workout.Description),
            workout.ThumbnailUrl, workout.VideoUrl,
            (workout.WorkoutTypes ?? []).Where(x => x.WorkoutType is not null)
                .Select(x => new CatalogDataDto(x.WorkoutType.Id, x.WorkoutType.Name, x.WorkoutType.Description))
                .ToList(),
            WorkoutMuscles.Parse(workout.Muscles), WorkoutMuscles.ParseSecondary(workout));

    public static string TypeName(ChatMessageType type) => type switch
    {
        ChatMessageType.Image => "image",
        ChatMessageType.Workout => "workout",
        _ => "text"
    };

    // Workouts shared in the given messages, by id.
    public static async Task<Dictionary<Guid, Workout>> LoadWorkouts(ApexPerformanceContext context,
        IEnumerable<ChatMessage?> messages, CancellationToken cancellationToken)
    {
        var ids = messages.Select(x => x?.WorkoutId).OfType<Guid>().Distinct().ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, Workout>();

        return await context.Workouts
            .AsNoTracking()
            .Include(x => x.WorkoutTypes)
            .ThenInclude(x => x.WorkoutType)
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
    }

    public static ChatParticipantResponse Participant(Coach coach) => new(coach.Id, coach.UserId, coach.FullName);

    public static ChatParticipantResponse Participant(Client client) => new(client.Id, client.UserId, client.FullName);
}
