namespace Moonglade.Configuration;

public class Menu
{
    public string Title { get; set; }

    public string Url { get; set; }

    public string Icon { get; set; } = "icon-file-text2";

    public int DisplayOrder { get; set; }

    public bool IsOpenInNewTab { get; set; }

    public List<SubMenu> SubMenus { get; set; } = new();
}