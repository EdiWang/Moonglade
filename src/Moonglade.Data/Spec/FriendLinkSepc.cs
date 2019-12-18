using System;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class FriendLinkSepc : BaseSpecification<FriendLinkEntity>
    {
        public FriendLinkSepc(Guid id) : base(f => f.Id == id)
        {

        }
    }
}
