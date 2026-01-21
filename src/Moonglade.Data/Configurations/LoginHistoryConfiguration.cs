using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistoryEntity>
{
    public void Configure(EntityTypeBuilder<LoginHistoryEntity> builder)
    {
        ConfigureIdentityColumn(builder);
    }

    protected virtual void ConfigureIdentityColumn(EntityTypeBuilder<LoginHistoryEntity> builder)
    {
        // Default: ValueGeneratedOnAdd
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
    }
}
