using System;
using System.Collections.Generic;

namespace Moonglade.Menus
{
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
            Icon = "icon-file-text2";
        }
    }
}
