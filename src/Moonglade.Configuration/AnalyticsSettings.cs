namespace Moonglade.Configuration;

public class AnalyticsSettings
{
    public GoogleAnalyticsSettings GoogleAnalytics { get; set; } = new();

    public MicrosoftClaritySettings MicrosoftClarity { get; set; } = new();
}

public class GoogleAnalyticsSettings
{
    public bool Enabled { get; set; }
    public string GTagId { get; set; }
}

public class MicrosoftClaritySettings
{
    public bool Enabled { get; set; }

    public string ProjectId { get; set; }
}
