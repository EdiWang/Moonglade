using Moonglade.Data.Entities;

namespace Moonglade.Menus;

public class Menu
{
    public Guid Id { get; set; }

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

    public Menu(MenuEntity entity)
    {
        if (entity is null) return;

        Id = entity.Id;
        Title = entity.Title.Trim();
        DisplayOrder = entity.DisplayOrder;
        Icon = entity.Icon?.Trim();
        Url = entity.Url?.Trim();
        IsOpenInNewTab = entity.IsOpenInNewTab;
        SubMenus = entity.SubMenus.Select(sm => new SubMenu
        {
            Id = sm.Id,
            Title = sm.Title,
            Url = sm.Url,
            IsOpenInNewTab = sm.IsOpenInNewTab
        }).ToList();
    }
}