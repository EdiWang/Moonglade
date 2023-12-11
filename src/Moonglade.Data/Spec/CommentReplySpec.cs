﻿using Moonglade.Data.Generated.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class CommentReplySpec(Guid commentId) : BaseSpecification<CommentReplyEntity>(cr => cr.CommentId == commentId);