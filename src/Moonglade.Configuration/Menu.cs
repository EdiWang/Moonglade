namespace Moonglade.Configuration;

public class Menu
{
    public string Title { get; set; }

    public string Url { get; set; }

    public string Icon { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsOpenInNewTab { get; set; }

    public List<SubMenu> SubMenus { get; set; }

    public Menu()
    {
        SubMenus = new();
        Icon = "icon-file-text2";
    }
}