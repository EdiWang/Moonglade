namespace Moonglade.Configuration;

public record SubMenu
{
    public string Title { get; set; }

    public string Url { get; set; }

    public bool IsOpenInNewTab { get; set; }
}