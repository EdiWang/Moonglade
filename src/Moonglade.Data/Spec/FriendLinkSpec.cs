using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;

namespace Moonglade.Data.Spec
{
    public class FriendLinkSpec : BaseSpecification<FriendLinkEntity>
    {
        public FriendLinkSpec(Guid id) : base(f => f.Id == id)
        {

        }
    }
}
