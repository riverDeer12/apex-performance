using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Extensions;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Shop;

public record ShopStockAlertRequest(Guid VariantId, string Email);

public record ShopAdminSettingsResponse(
    decimal? ShippingPrice,
    decimal? FreeShippingFrom,
    decimal ClientDiscountPercent,
    string? NotificationEmail,
    // What applies when a value is left empty (server settings or defaults).
    decimal DefaultShippingPrice,
    decimal DefaultFreeShippingFrom,
    string DefaultNotificationEmail);

public record SaveShopAdminSettingsRequest(
    decimal? ShippingPrice,
    decimal? FreeShippingFrom,
    decimal ClientDiscountPercent,
    string? NotificationEmail);

/// <summary>
/// "Let me know when it's back": the customer gets one email when the sold out
/// size or color is in stock again. Asking twice for the same one is accepted
/// without a second email later.
/// </summary>
public class CreateShopStockAlertEndpoint : Endpoint<ShopStockAlertRequest>
{
    private readonly ApexPerformanceContext _context;

    public CreateShopStockAlertEndpoint(ApexPerformanceContext context)
    {
        _context = context;
    }

    public override void Configure()
    {
        Post("api/shop/stock-alerts");
        AllowAnonymous();
        Options(x => x.WithTags("Shop").RequireRateLimiting(RateLimiting.ShopPromoCheck));
    }

    public override async Task HandleAsync(ShopStockAlertRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var variant = await _context.ShopProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.VariantId && !x.IsDeleted && x.Product.IsActive &&
                                      !x.Product.IsDeleted, cancellationToken);

        if (variant is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var waiting = await _context.ShopStockAlerts.AnyAsync(x =>
            x.VariantId == variant.Id && x.Email == email && x.NotifiedAt == null, cancellationToken);

        if (!waiting)
        {
            _context.ShopStockAlerts.Add(new ShopStockAlert
            {
                VariantId = variant.Id,
                Email = email,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        await SendNoContentAsync(cancellationToken);
    }
}

public sealed class ShopStockAlertValidator : Validator<ShopStockAlertRequest>
{
    public ShopStockAlertValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.Email).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid)
            .EmailAddress().WithMessage(ErrorCodes.NotValid);
    }
}

/// <summary>
/// Settings of the shop changed in the administration. Empty values use the
/// server settings, which the response shows as the defaults.
/// </summary>
public class GetShopAdminSettingsEndpoint : EndpointWithoutRequest<ShopAdminSettingsResponse>
{
    private readonly ShopSettings _settings;

    public GetShopAdminSettingsEndpoint(ShopSettings settings)
    {
        _settings = settings;
    }

    public override void Configure()
    {
        Get("api/shop/admin/settings");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override Task HandleAsync(CancellationToken cancellationToken)
        => SendAsync(ShopAdminSettings.ToResponse(_settings), cancellation: cancellationToken);
}

public class SaveShopAdminSettingsEndpoint : Endpoint<SaveShopAdminSettingsRequest, ShopAdminSettingsResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ShopSettings _settings;

    public SaveShopAdminSettingsEndpoint(ApexPerformanceContext context, ShopSettings settings)
    {
        _context = context;
        _settings = settings;
    }

    public override void Configure()
    {
        Put("api/shop/admin/settings");
        Roles(ShopAdmin.Roles);
        Options(x => x.WithTags("Shop"));
    }

    public override async Task HandleAsync(SaveShopAdminSettingsRequest request, CancellationToken cancellationToken)
    {
        var config = await _context.ShopConfigs.FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            config = new ShopConfig();
            _context.ShopConfigs.Add(config);
        }

        config.ShippingPrice = request.ShippingPrice;
        config.FreeShippingFrom = request.FreeShippingFrom;
        config.ClientDiscountPercent = request.ClientDiscountPercent;
        config.NotificationEmail = string.IsNullOrWhiteSpace(request.NotificationEmail)
            ? null
            : request.NotificationEmail.Trim();
        config.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await SendAsync(ShopAdminSettings.ToResponse(new ShopSettingsView(config, _settings)),
            cancellation: cancellationToken);
    }
}

public sealed class SaveShopAdminSettingsValidator : Validator<SaveShopAdminSettingsRequest>
{
    public SaveShopAdminSettingsValidator()
    {
        RuleFor(x => x.ShippingPrice).InclusiveBetween(0, 1000).When(x => x.ShippingPrice is not null)
            .WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.FreeShippingFrom).InclusiveBetween(0, 100000).When(x => x.FreeShippingFrom is not null)
            .WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.ClientDiscountPercent).InclusiveBetween(0, 90).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.NotificationEmail).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.NotificationEmail)).WithMessage(ErrorCodes.NotValid);
    }
}

// Settings with a just saved row, so the response shows it without reading it again.
internal record ShopSettingsView(ShopConfig Config, ShopSettings Defaults);

internal static class ShopAdminSettings
{
    public static ShopAdminSettingsResponse ToResponse(ShopSettings settings)
        => ToResponse(new ShopSettingsView(settings.Config, settings));

    public static ShopAdminSettingsResponse ToResponse(ShopSettingsView view)
        => new(view.Config.ShippingPrice, view.Config.FreeShippingFrom, view.Config.ClientDiscountPercent,
            view.Config.NotificationEmail, view.Defaults.DefaultShippingPrice, view.Defaults.DefaultFreeShippingFrom,
            view.Defaults.DefaultNotificationEmail);
}
