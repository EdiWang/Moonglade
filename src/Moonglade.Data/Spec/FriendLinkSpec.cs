using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class FriendLinkSpec(Guid id) : BaseSpecification<FriendLinkEntity>(f => f.Id == id);