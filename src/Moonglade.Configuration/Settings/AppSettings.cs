﻿using System.Collections.Generic;

namespace Moonglade.Configuration.Settings
{
    public class AppSettings
    {
        public EditorChoice Editor { get; set; }
        public int PostAbstractWords { get; set; }
        public IDictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
    }
}