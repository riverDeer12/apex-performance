using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ShopProductConfiguration : IEntityTypeConfiguration<ShopProduct>
{
    public void Configure(EntityTypeBuilder<ShopProduct> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Slug).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.Price).HasPrecision(10, 2);

        builder.HasMany(e => e.Variants)
            .WithOne(e => e.Product)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Images)
            .WithOne(e => e.Product)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Slug).IsUnique();

        builder.ToTable("ShopProducts");
    }
}

public class ShopProductVariantConfiguration : IEntityTypeConfiguration<ShopProductVariant>
{
    public void Configure(EntityTypeBuilder<ShopProductVariant> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Size).HasMaxLength(50);
        builder.Property(e => e.Color).HasMaxLength(50);

        builder.ToTable("ShopProductVariants");
    }
}

public class ShopProductImageConfiguration : IEntityTypeConfiguration<ShopProductImage>
{
    public void Configure(EntityTypeBuilder<ShopProductImage> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ContentType).HasMaxLength(50);

        builder.HasIndex(e => new { e.ProductId, e.SortOrder });

        builder.ToTable("ShopProductImages");
    }
}

public class ShopOrderConfiguration : IEntityTypeConfiguration<ShopOrder>
{
    public void Configure(EntityTypeBuilder<ShopOrder> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Number).HasMaxLength(30);
        builder.Property(e => e.Status).HasMaxLength(20);
        builder.Property(e => e.CustomerName).HasMaxLength(200);
        builder.Property(e => e.CustomerEmail).HasMaxLength(200);
        builder.Property(e => e.CustomerPhone).HasMaxLength(50);
        builder.Property(e => e.Subtotal).HasPrecision(10, 2);
        builder.Property(e => e.ShippingPrice).HasPrecision(10, 2);
        builder.Property(e => e.Total).HasPrecision(10, 2);
        builder.Property(e => e.Discount).HasPrecision(10, 2);
        builder.Property(e => e.PromoCode).HasMaxLength(40);
        builder.Property(e => e.RefundedAmount).HasPrecision(10, 2);
        builder.Property(e => e.ClientDiscountPercent).HasPrecision(5, 2);
        builder.HasIndex(e => e.CustomerUserId);
        builder.Property(e => e.DisputeStatus).HasMaxLength(40);
        builder.Property(e => e.StripeSessionId).HasMaxLength(200);
        builder.Property(e => e.StripePaymentIntentId).HasMaxLength(200);
        builder.Property(e => e.BoxNowLockerId).HasMaxLength(50);
        builder.Property(e => e.BoxNowLockerAddress).HasMaxLength(300);
        builder.Property(e => e.BoxNowLockerPostalCode).HasMaxLength(20);
        builder.Property(e => e.BoxNowParcelId).HasMaxLength(50);

        builder.HasMany(e => e.Items)
            .WithOne(e => e.Order)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Number).IsUnique();
        builder.HasIndex(e => new { e.Status, e.CreatedAt });

        builder.ToTable("ShopOrders");
    }
}

public class ShopOrderItemConfiguration : IEntityTypeConfiguration<ShopOrderItem>
{
    public void Configure(EntityTypeBuilder<ShopOrderItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ProductName).HasMaxLength(200);
        builder.Property(e => e.VariantName).HasMaxLength(110);
        builder.Property(e => e.UnitPrice).HasPrecision(10, 2);
        builder.Property(e => e.UnitDiscount).HasPrecision(10, 2);

        builder.HasOne(e => e.Variant)
            .WithMany()
            .HasForeignKey(e => e.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("ShopOrderItems");
    }
}

public class ShopPromoCodeConfiguration : IEntityTypeConfiguration<ShopPromoCode>
{
    public void Configure(EntityTypeBuilder<ShopPromoCode> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(40);
        builder.Property(e => e.DiscountType).HasMaxLength(20);
        builder.Property(e => e.DiscountValue).HasPrecision(10, 2);
        builder.Property(e => e.Note).HasMaxLength(500);

        builder.HasMany(e => e.Products)
            .WithOne(e => e.PromoCode)
            .HasForeignKey(e => e.PromoCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Code);

        builder.ToTable("ShopPromoCodes");
    }
}

public class ShopPromoCodeProductConfiguration : IEntityTypeConfiguration<ShopPromoCodeProduct>
{
    public void Configure(EntityTypeBuilder<ShopPromoCodeProduct> builder)
    {
        builder.HasKey(e => new { e.PromoCodeId, e.ProductId });

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("ShopPromoCodeProducts");
    }
}

public class ShopStockMovementConfiguration : IEntityTypeConfiguration<ShopStockMovement>
{
    public void Configure(EntityTypeBuilder<ShopStockMovement> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason).HasMaxLength(20);
        builder.Property(e => e.Note).HasMaxLength(300);

        builder.HasOne(e => e.Variant)
            .WithMany()
            .HasForeignKey(e => e.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.VariantId, e.CreatedAt });

        builder.ToTable("ShopStockMovements");
    }
}

public class ShopStripeEventConfiguration : IEntityTypeConfiguration<ShopStripeEvent>
{
    public void Configure(EntityTypeBuilder<ShopStripeEvent> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasMaxLength(100);
        builder.Property(e => e.Type).HasMaxLength(100);

        builder.ToTable("ShopStripeEvents");
    }
}

public class ShopConfigConfiguration : IEntityTypeConfiguration<ShopConfig>
{
    public void Configure(EntityTypeBuilder<ShopConfig> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.ShippingPrice).HasPrecision(10, 2);
        builder.Property(e => e.FreeShippingFrom).HasPrecision(10, 2);
        builder.Property(e => e.ClientDiscountPercent).HasPrecision(5, 2);
        builder.Property(e => e.NotificationEmail).HasMaxLength(200);

        builder.ToTable("ShopConfigs");
    }
}

public class ShopStockAlertConfiguration : IEntityTypeConfiguration<ShopStockAlert>
{
    public void Configure(EntityTypeBuilder<ShopStockAlert> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email).HasMaxLength(200);

        builder.HasOne(e => e.Variant)
            .WithMany()
            .HasForeignKey(e => e.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.VariantId, e.NotifiedAt });

        builder.ToTable("ShopStockAlerts");
    }
}

public class ShopVisitDayConfiguration : IEntityTypeConfiguration<ShopVisitDay>
{
    public void Configure(EntityTypeBuilder<ShopVisitDay> builder)
    {
        builder.HasKey(e => new { e.Date, e.Path });

        builder.Property(e => e.Path).HasMaxLength(200);

        builder.ToTable("ShopVisitDays");
    }
}
