namespace Moonglade.Configuration;

public class HeadScriptsOptions
{
    public List<HeadScript> HeadScripts { get; set; }
}

public class HeadScript
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Integrity { get; set; }
    public string CrossOrigin { get; set; }
    public bool IsAsync { get; set; } = false;
}
