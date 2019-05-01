using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class MoongladeSettings
    {
        public string GetJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this, formatting);
        }
    }
}
