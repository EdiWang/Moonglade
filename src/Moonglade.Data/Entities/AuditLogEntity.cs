using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Data.Entities;

public class AuditLogEntity
{
    public long Id { get; set; }

    public BlogEventId EventId { get; set; }

    public BlogEventType EventType { get; set; }

    public DateTime EventTimeUtc { get; set; }

    public string WebUsername { get; set; }

    public string IpAddressV4 { get; set; }

    public string MachineName { get; set; }

    public string Message { get; set; }
}

[ExcludeFromCodeCoverage]
internal class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.Property(e => e.Id).UseIdentityColumn();
    }
}