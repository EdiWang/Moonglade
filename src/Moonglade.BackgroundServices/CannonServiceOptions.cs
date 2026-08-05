using System.ComponentModel.DataAnnotations;

namespace Moonglade.BackgroundServices;

public class CannonServiceOptions
{
    public const string SectionName = "CannonService";

    [Range(1, int.MaxValue)]
    public int QueueCapacity { get; set; } = 1000;
}
