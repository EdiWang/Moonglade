namespace Moonglade.Configuration;

public class AnalyticsSettings
{
    public MicrosoftClaritySettings MicrosoftClarity { get; set; } = new();
}

public class MicrosoftClaritySettings
{
    public bool Enabled { get; set; }

    public string ProjectId { get; set; }
}
