using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Core.PostFeature;

public record PostponePostCommand(Guid PostId, int Hours) : IRequest;
