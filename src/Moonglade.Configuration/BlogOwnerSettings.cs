using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class BlogOwnerSettings : MoongladeSettings
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string ShortDescription { get; set; }

        public string AvatarBase64 { get; set; }

        public BlogOwnerSettings()
        {
            AvatarBase64 = string.Empty;
        }
    }
}
