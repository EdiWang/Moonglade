using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.SqlServer.Configurations;

internal class LoginHistoryConfiguration : Data.Configurations.LoginHistoryConfiguration
{
    protected override void ConfigureIdentityColumn(EntityTypeBuilder<LoginHistoryEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn(1, 1);
    }
}