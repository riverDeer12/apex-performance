using System.Net;
using ApexPerformance.API.Constants;
using ApexPerformance.API.Extensions;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Shop;

public record ShopContactRequest(
    string Name,
    string Email,
    string Message,
    // Order the message is about, e.g. "AP-2026-0012", optional.
    string? OrderNumber,
    // Hidden field of the form; people leave it empty, spam scripts fill it in.
    string? Website);

/// <summary>
/// Contact form of the shop, instead of a public email address. The message is
/// sent to the shop's notification email; replying answers the customer.
/// </summary>
public class SendShopContactEndpoint : Endpoint<ShopContactRequest>
{
    private readonly IEmailService _emailService;
    private readonly ShopSettings _settings;
    private readonly ILogger<SendShopContactEndpoint> _logger;

    public SendShopContactEndpoint(IEmailService emailService, ShopSettings settings,
        ILogger<SendShopContactEndpoint> logger)
    {
        _emailService = emailService;
        _settings = settings;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("api/shop/contact");
        AllowAnonymous();
        Options(x => x.WithTags("Shop").RequireRateLimiting(RateLimiting.ShopContact));
    }

    public override async Task HandleAsync(ShopContactRequest request, CancellationToken cancellationToken)
    {
        // Looks sent to the script, nothing is sent.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogInformation("Shop contact message with the hidden field filled in was ignored.");
            await SendNoContentAsync(cancellationToken);
            return;
        }

        var name = request.Name.Trim();
        var email = request.Email.Trim();
        var order = string.IsNullOrWhiteSpace(request.OrderNumber) ? null : request.OrderNumber.Trim();

        var body =
            $"<p><strong>{E(name)}</strong> &lt;{E(email)}&gt;" +
            (order is null ? "" : $"<br>Narudžba: {E(order)}") + "</p>" +
            $"<p style=\"white-space:pre-line\">{E(request.Message.Trim())}</p>" +
            "<p style=\"color:#6b7180\">Poslano preko kontakt obrasca shopa. Odgovorom na ovaj e-mail odgovarate kupcu.</p>";

        _emailService.SendShopEmail(_settings.NotificationEmail,
            order is null ? $"Poruka iz shopa: {name}" : $"Poruka iz shopa: {name} ({order})",
            ShopEmails.Alert("Nova poruka iz shopa", body),
            replyTo: email);

        await SendNoContentAsync(cancellationToken);
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);
}

public sealed class ShopContactValidator : Validator<ShopContactRequest>
{
    public ShopContactValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(100).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid)
            .EmailAddress().WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Message).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(4000).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.OrderNumber).MaximumLength(40).WithMessage(ErrorCodes.NotValid);
    }
}
