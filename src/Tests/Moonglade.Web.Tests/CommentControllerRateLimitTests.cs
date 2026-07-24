using Microsoft.AspNetCore.RateLimiting;
using Moonglade.Web.Controllers;
using Moonglade.Web.Services;
using System.Reflection;

namespace Moonglade.Web.Tests;

public class CommentControllerRateLimitTests
{
    [Fact]
    public void Create_UsesCommentRateLimitPolicy()
    {
        var method = typeof(CommentController).GetMethod(nameof(CommentController.Create));

        var attribute = method!.GetCustomAttribute<EnableRateLimitingAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(CommentRateLimitPolicy.PolicyName, attribute.PolicyName);
    }
}
