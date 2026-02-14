namespace Moonglade.ActivityLog;

public enum EventType
{
    Default = 0,

    // Category operations (100-199)
    CategoryCreated = 100,
    CategoryUpdated = 101,
    CategoryDeleted = 102,

    // Post operations (200-299)
    // Reserved for future use

    // Comment operations (300-399)
    // Reserved for future use

    // Page operations (400-499)
    // Reserved for future use

    // User operations (500-599)
    // Reserved for future use

    // Tag operations (600-699)
    TagCreated = 600,
    TagUpdated = 601,
    TagDeleted = 602,

    // System operations (900-999)
    // Reserved for future use
}
