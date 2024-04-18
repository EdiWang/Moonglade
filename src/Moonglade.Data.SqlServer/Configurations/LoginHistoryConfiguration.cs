using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistoryEntity>
{
    public void Configure(EntityTypeBuilder<LoginHistoryEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn(1, 1);
    }
}