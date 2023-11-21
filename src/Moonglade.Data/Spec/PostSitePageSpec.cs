using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class PostSitePageSpec() : BaseSpecification<PostEntity>(p =>
    p.IsPublished && !p.IsDeleted);