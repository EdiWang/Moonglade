using LiteBus.Queries.Abstractions;
using System.Text.RegularExpressions;

namespace Moonglade.ActivityLog;

public record GetEventTypesQuery : IQuery<List<EventTypeGroup>>;

public class EventTypeGroup
{
    public string Category { get; set; }
    public List<EventTypeItem> Items { get; set; }
}

public class EventTypeItem
{
    public int Value { get; set; }
    public string Name { get; set; }
}

public class GetEventTypesQueryHandler : IQueryHandler<GetEventTypesQuery, List<EventTypeGroup>>
{
    public Task<List<EventTypeGroup>> HandleAsync(GetEventTypesQuery request, CancellationToken ct)
    {
        var eventTypes = Enum.GetValues<EventType>()
            .Where(et => et != EventType.Default)
            .Select(et => new
            {
                Value = (int)et,
                Name = SplitCamelCase(et.ToString()),
                Category = GetCategory(et)
            })
            .GroupBy(x => x.Category)
            .Select(g => new EventTypeGroup
            {
                Category = g.Key,
                Items = g.Select(x => new EventTypeItem
                {
                    Value = x.Value,
                    Name = x.Name
                }).ToList()
            })
            .OrderBy(g => g.Category)
            .ToList();

        return Task.FromResult(eventTypes);
    }

    private static string SplitCamelCase(string input)
    {
        return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
    }

    private static string GetCategory(EventType eventType)
    {
        var value = (int)eventType;
        return value switch
        {
            >= 100 and < 200 => "Category",
            >= 200 and < 300 => "Post",
            >= 300 and < 400 => "Comment",
            >= 400 and < 500 => "Page",
            >= 500 and < 600 => "User",
            >= 600 and < 700 => "Tag",
            >= 700 and < 800 => "Theme",
            >= 800 and < 810 => "Settings",
            >= 810 and < 820 => "Image",
            >= 820 and < 850 => "Asset",
            >= 850 and < 900 => "Widget",
            >= 900 => "System",
            _ => "Other"
        };
    }
}
