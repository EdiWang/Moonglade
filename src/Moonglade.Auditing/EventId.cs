using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Auditing
{
    public enum EventId
    {
        GeneralOperation = 1,
        
        LoginSuccessAAD = 1001,
        LoginFailedAAD = 1002,
        LoginSuccessLocal = 1003,
        LoginFailedLocal = 1004,

        SettingsSavedGeneral = 2001,
        SettingsSavedContent = 2002,
        SettingsSavedNotification = 2003,
        SettingsSavedSubscription = 2004,
        SettingsSavedWatermark = 2005,
        SettingsSavedFriendLink = 2006,
        SettingsSavedAdvanced = 2007,

        PostCreated = 3001,
        PostPublished = 3002,
        PostUpdated = 3003,
        PostRecycled = 3004,
        PostDeleted = 3005,
        
        TagCreated = 4001,
        TagUpdated = 4002,
        TagDeleted = 4003,

        CategoryCreated = 5001,
        CategoryUpdated = 5002,
        CategoryDeleted = 5003,

        CommentApproval = 6001,
        CommentDisapproval = 6002,
        CommentDeleted = 6003,
        CommentReplied = 6004,

        PageCreated = 7001,
        PageUpdated = 7002,
        PageDeleted = 7003,

        PingbackDeleted = 8001
    }
}
