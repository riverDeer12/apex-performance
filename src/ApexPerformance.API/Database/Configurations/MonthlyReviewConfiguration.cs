using ApexPerformance.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class MonthlyReviewConfiguration : IEntityTypeConfiguration<MonthlyReview>
{
    public void Configure(EntityTypeBuilder<MonthlyReview> builder)
    {
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.Property(e => e.Content).HasMaxLength(4000);

        builder.HasIndex(e => new { e.ClientId, e.Year, e.Month });

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("MonthlyReviews", c => c.IsTemporal());
    }
}
