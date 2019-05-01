using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Configuration
{
    public class ContentSettings : MoongladeSettings
    {
        public string DisharmonyWords { get; set; }
        public bool EnableComments { get; set; }

        public ContentSettings()
        {
            DisharmonyWords = string.Empty;
            EnableComments = true;
        }
    }
}
