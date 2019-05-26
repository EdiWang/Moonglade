using Newtonsoft.Json;

namespace Moonglade.Configuration.Abstraction
{
    public interface IMoongladeSettings
    {
        string GetJson(Formatting formatting = Formatting.None);
    }

    public class MoongladeSettings : IMoongladeSettings
    {
        public string GetJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this, formatting);
        }
    }
}
