using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Moonglade.Webmention;

public record ReceiveWebmentionCommand(string Source, string Target, string RemoteIp) : IRequest<WebmentionResponse>;

