﻿using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class AuditPagingSpec : BaseSpecification<AuditLogEntity>
    {
        public AuditPagingSpec(int pageSize, int offset) : base()
        {
            ApplyPaging(offset, pageSize);
            ApplyOrderByDescending(p => p.EventTimeUtc);
        }
    }
}
