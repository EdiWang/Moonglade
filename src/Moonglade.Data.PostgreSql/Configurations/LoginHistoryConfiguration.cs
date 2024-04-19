using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.PostgreSql.Configurations;

internal class LoginHistoryConfiguration : IEntityTypeConfiguration<LoginHistoryEntity>
{
    public void Configure(EntityTypeBuilder<LoginHistoryEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn();
    }
}