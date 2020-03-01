using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Moonglade.Auditing
{
    public class MoongladeAudit
    {
        private readonly ILogger<MoongladeAudit> _logger;

        private readonly IConfiguration _configuration;
    }
}
