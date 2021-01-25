using System;
using System.Collections.Generic;

namespace Moonglade.Auth
{
    public class ApiKey
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Key { get; set; }
        public DateTime Created { get; }
        public IReadOnlyCollection<string> Roles { get; set; }

        public ApiKey()
        {
            Roles = new[] { "Administrator" };
            Created = DateTime.UtcNow;
        }
    }
}