using System.ComponentModel.DataAnnotations;

namespace Moonglade.Data.Entities;

public class BlogConfigurationEntity
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string CfgKey { get; set; }

    public string CfgValue { get; set; }

    public DateTime? LastModifiedTimeUtc { get; set; }
}