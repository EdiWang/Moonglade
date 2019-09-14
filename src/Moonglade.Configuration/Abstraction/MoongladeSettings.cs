using System.Text.Json;

namespace Moonglade.Configuration.Abstraction
{
    public interface IMoongladeSettings
    {
        string GetJson();
    }

    public class MoongladeSettings : IMoongladeSettings
    {
        public string GetJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
