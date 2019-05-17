using Newtonsoft.Json;

namespace Moonglade.Configuration.Abstraction
{
    public class MoongladeSettings
    {
        public string GetJson(Formatting formatting = Formatting.None)
        {
            return JsonConvert.SerializeObject(this, formatting);
        }
    }
}
