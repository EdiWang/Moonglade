using System;

namespace Moonglade.Model
{
    public class CustomPageMetaData
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string RouteName { get; set; }

        public DateTime CreateOnUtc { get; set; }
    }
}
