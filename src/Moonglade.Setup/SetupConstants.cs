using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Setup
{
    public class SetupConstants
    {
        public static string PostContentInitValue = "Moonglade is the new blog system for https://edi.wang. It is a complete rewrite of the old system using .NET Core and runs on Microsoft Azure.";
    }

    public enum JsonDataName
    {
        WatermarkSettings,
        FeedSettings,
        EmailConfiguration,
        BlogOwnerSettings,
        GeneralSettings,
        ContentSettings
    }
}
