using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class OwnerAccountConfiguration : IEntityTypeConfiguration<OwnerAccountEntity>
{
    public void Configure(EntityTypeBuilder<OwnerAccountEntity> builder)
    {
        builder.HasKey(account => account.Id).HasName("PK_OwnerAccount");
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.Slot).ValueGeneratedNever();
        builder.Property(account => account.Email).IsRequired().HasMaxLength(254);
        builder.Property(account => account.NormalizedEmail).IsRequired().HasMaxLength(254);
        builder.Property(account => account.Revision).IsConcurrencyToken();
        builder.Property(account => account.PasswordHash).HasMaxLength(256);
        builder.Property(account => account.PasswordSalt).HasMaxLength(128);
        builder.Property(account => account.TotpSecret).HasMaxLength(128);
        builder.HasIndex(account => account.Slot).IsUnique().HasDatabaseName("IX_OwnerAccount_Slot");
        builder.HasIndex(account => account.NormalizedEmail).IsUnique().HasDatabaseName("IX_OwnerAccount_NormalizedEmail");
    }

    public static void ConfigureConstraints(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<OwnerAccountEntity>();
        string Column(string property) => $"\"{builder.Metadata.FindProperty(property)!.GetColumnName()}\"";
        var initialized = Column(nameof(OwnerAccountEntity.LocalInitializedUtc));
        var hash = Column(nameof(OwnerAccountEntity.PasswordHash));
        var salt = Column(nameof(OwnerAccountEntity.PasswordSalt));
        var secret = Column(nameof(OwnerAccountEntity.TotpSecret));
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_OwnerAccount_Slot", $"{Column(nameof(OwnerAccountEntity.Slot))} = 1");
            table.HasCheckConstraint("CK_OwnerAccount_LocalCredentials",
                $"({initialized} IS NULL AND {hash} IS NULL AND {salt} IS NULL AND {secret} IS NULL) OR " +
                $"({initialized} IS NOT NULL AND {hash} IS NOT NULL AND {hash} <> '' AND " +
                $"{salt} IS NOT NULL AND {salt} <> '' AND ({secret} IS NULL OR {secret} <> ''))");
        });
    }
}
