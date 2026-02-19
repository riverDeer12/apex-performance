using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexPerformance.API.Database.Configurations;

public class ClientLastCreditIncreaseConfiguration
    : IEntityTypeConfiguration<ClientLastCreditIncreaseDto>
{
    public void Configure(EntityTypeBuilder<ClientLastCreditIncreaseDto> builder)
    {
        builder.HasNoKey();
        builder.ToView(null);
    }
}