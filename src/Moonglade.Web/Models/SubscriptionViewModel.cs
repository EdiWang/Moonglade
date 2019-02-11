using System.Collections.Generic;

namespace Moonglade.Web.Models
{
    public class SubscriptionViewModel
    {
        public List<KeyValuePair<string, string>> cats { get; set; }

        public SubscriptionViewModel()
        {
            cats = new List<KeyValuePair<string, string>>();
        }
    }
}
