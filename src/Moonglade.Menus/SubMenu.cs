namespace Moonglade.Menus;

public record SubMenu
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Url { get; set; }

    public bool IsOpenInNewTab { get; set; }
}