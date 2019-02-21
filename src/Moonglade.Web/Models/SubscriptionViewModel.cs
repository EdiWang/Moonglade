using System.Collections.Generic;

namespace Moonglade.Web.Models
{
    public class SubscriptionViewModel
    {
        public List<KeyValuePair<string, string>> Cats { get; set; }

        public SubscriptionViewModel()
        {
            Cats = new List<KeyValuePair<string, string>>();
        }
    }
}
