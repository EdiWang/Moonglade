using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class PingbackHistorySpec : BaseSpecification<PingbackHistoryEntity>
    {
        public PingbackHistorySpec(Guid id) : base(p => p.Id == id)
        {

        }
    }
}
