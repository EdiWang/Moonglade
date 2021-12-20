namespace Moonglade.Configuration;

public class HtmlPitch
{
    public PitchKey Key { get; set; }
    public string Value { get; set; }
}

public enum PitchKey
{
    Sidebar = 1,
    Footer = 2,
    Callout = 3,
    Comment = 4
}