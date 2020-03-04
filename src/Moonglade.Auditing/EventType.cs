using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Auditing
{
    public enum EventType
    {
        General = 0,
        Authentication = 100,
        Settings = 200,
        Content = 300
    }
}
