using System.Net;
using System.Text;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Extensions;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Chat;

public record ReportConversationRequest(string? Reason);

/// <summary>
/// Reports a conversation to the administrator (App Store rule for user
/// generated content). Either side can report; the administrator gets an
/// email with the reason and the last messages, which the user agreed to
/// share by reporting. "ChatReports:Email" changes the recipient.
/// </summary>
public class ReportConversationEndpoint : Endpoint<ReportConversationRequest>
{
    private const string DefaultEmail = "info@rdd.software";
    private const int MessagesInReport = 50;

    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatEncryptionService _encryption;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ReportConversationEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IChatEncryptionService encryption, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _currentUserService = currentUserService;
        _encryption = encryption;
        _emailService = emailService;
        _configuration = configuration;
    }

    public override void Configure()
    {
        Post("api/chat/conversations/{id}/report");
        Roles(UserRoles.Coach, UserRoles.Client);
        Options(x => x.WithTags("Chat").RequireRateLimiting(RateLimiting.ChatReports));
    }

    public override async Task HandleAsync(ReportConversationRequest request, CancellationToken cancellationToken)
    {
        var conversationId = Route<Guid>("id", isRequired: true);
        var userId = _currentUserService.UserId;

        var conversation = await ChatAccess.GetConversation(_context, userId, conversationId, cancellationToken);

        if (conversation is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        BackgroundJob.Enqueue(() => SendReportEmail(conversationId, userId, request.Reason));

        await SendNoContentAsync(cancellationToken);
    }

    public async Task SendReportEmail(Guid conversationId, Guid reporterUserId, string? reason)
    {
        var conversation = await _context.ChatConversations
            .Include(x => x.Coach)
            .Include(x => x.Client)
            .SingleAsync(x => x.Id == conversationId);

        var messages = await _context.ChatMessages
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(MessagesInReport)
            .ToListAsync();

        string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        string Name(Guid senderUserId) => senderUserId == conversation.Coach.UserId
            ? $"{conversation.Coach.FullName} (coach)"
            : $"{conversation.Client.FullName} (client)";

        var html = new StringBuilder();
        html.Append($"<p><b>Reported by:</b> {Encode(Name(reporterUserId))}</p>");
        html.Append($"<p><b>Conversation:</b> {Encode(conversation.Coach.FullName)} – " +
                    $"{Encode(conversation.Client.FullName)} ({conversationId})</p>");
        html.Append($"<p><b>Reason:</b> {(string.IsNullOrWhiteSpace(reason) ? "-" : Encode(reason.Trim()))}</p>");
        html.Append($"<p><b>Last {messages.Count} messages (UTC):</b></p><ul>");

        foreach (var message in messages.OrderBy(x => x.CreatedAt))
        {
            var content = _encryption.Decrypt(message.EncryptedContent, message.KeyId, message.ConversationId);
            html.Append($"<li>{message.CreatedAt.UtcDateTime:yyyy-MM-dd HH:mm} <b>{Encode(Name(message.SenderUserId))}</b>: " +
                        $"{(content is null ? "<i>unreadable</i>" : Encode(content))}</li>");
        }

        html.Append("</ul>");

        _emailService.SendErrorAlertEmail(_configuration["ChatReports:Email"] ?? DefaultEmail,
            "Apex Performance: reported chat", html.ToString());
    }
}

public sealed class ReportConversationValidator : Validator<ReportConversationRequest>
{
    public ReportConversationValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage(ErrorCodes.NotValid);
    }
}
