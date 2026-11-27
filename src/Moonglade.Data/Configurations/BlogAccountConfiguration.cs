using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Configurations;

public class BlogAccountConfiguration : IEntityTypeConfiguration<BlogAccountEntity>
{
    public void Configure(EntityTypeBuilder<BlogAccountEntity> builder)
    {
        builder.HasKey(account => account.Id).HasName("PK_BlogAccount");
        builder.Property(account => account.Id).ValueGeneratedNever();
        builder.Property(account => account.Slot).ValueGeneratedNever();
        builder.Property(account => account.Email).IsRequired().HasMaxLength(254);
        builder.Property(account => account.NormalizedEmail).IsRequired().HasMaxLength(254);
        builder.Property(account => account.Revision).IsConcurrencyToken();
        builder.Property(account => account.DisplayName).HasMaxLength(100);
        builder.Property(account => account.IsDisabled).HasDefaultValue(false);
        builder.Property(account => account.MustChangePassword).HasDefaultValue(false);
        builder.Property(account => account.PasswordHash).HasMaxLength(256);
        builder.Property(account => account.PasswordSalt).HasMaxLength(128);
        builder.Property(account => account.TotpSecret).HasMaxLength(128);
        builder.HasIndex(account => account.Slot).IsUnique().HasDatabaseName("IX_BlogAccount_Slot");
        builder.HasIndex(account => account.NormalizedEmail).IsUnique().HasDatabaseName("IX_BlogAccount_NormalizedEmail");
    }

    public static void ConfigureConstraints(ModelBuilder modelBuilder, string falseLiteral = "0")
    {
        var builder = modelBuilder.Entity<BlogAccountEntity>();
        string Column(string property) => $"\"{builder.Metadata.FindProperty(property)!.GetColumnName()}\"";
        var initialized = Column(nameof(BlogAccountEntity.LocalInitializedUtc));
        var hash = Column(nameof(BlogAccountEntity.PasswordHash));
        var salt = Column(nameof(BlogAccountEntity.PasswordSalt));
        var secret = Column(nameof(BlogAccountEntity.TotpSecret));
        var slot = Column(nameof(BlogAccountEntity.Slot));
        var disabled = Column(nameof(BlogAccountEntity.IsDisabled));
        var mustChangePassword = Column(nameof(BlogAccountEntity.MustChangePassword));
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_BlogAccount_SlotState",
                $"({slot} = {BlogAccountEntity.OwnerSlot} AND {disabled} = {falseLiteral} AND {mustChangePassword} = {falseLiteral}) OR " +
                $"({slot} > {BlogAccountEntity.OwnerSlot} AND {slot} <= {BlogAccountEntity.LastCollaboratorSlot} AND {initialized} IS NOT NULL)");
            table.HasCheckConstraint("CK_BlogAccount_LocalCredentials",
                $"({initialized} IS NULL AND {hash} IS NULL AND {salt} IS NULL AND {secret} IS NULL) OR " +
                $"({initialized} IS NOT NULL AND {hash} IS NOT NULL AND {hash} <> '' AND " +
                $"{salt} IS NOT NULL AND {salt} <> '' AND ({secret} IS NULL OR {secret} <> ''))");
        });
    }
}
