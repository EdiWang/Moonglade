namespace Moonglade.Utils.Tests.Content;

public class KeywordTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  \t  \n  ", null)]
    public void GetKeywords_WithNullOrWhitespace_ReturnsNull(string rawKeywords, string expected)
    {
        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("keyword1", "keyword1")]
    [InlineData("keyword1,keyword2", "keyword1,keyword2")]
    [InlineData("a,b,c,d,e", "a,b,c,d,e")]
    [InlineData("  technology  ", "technology")]
    [InlineData("technology, programming, csharp", "technology,programming,csharp")]
    [InlineData("  technology  ,   programming   ,  csharp  ", "technology,programming,csharp")]
    [InlineData("technology,programming,technology,csharp,programming", "technology,programming,csharp")]
    [InlineData("Technology,technology,TECHNOLOGY,Programming", "Technology,technology,TECHNOLOGY,Programming")]
    [InlineData("technology,,programming,   ,csharp,", "technology,programming,csharp")]
    [InlineData(" , ,   ,  , ", null)]
    [InlineData("  ASP.NET Core  , ,   Entity Framework   , ASP.NET Core ,  Blazor  ,   ,  C# Programming  ,", "ASP.NET Core,Entity Framework,Blazor,C# Programming")]
    [InlineData("C#, .NET 8, ASP.NET Core, Entity Framework 7.0, Visual Studio 2022", "C#,.NET 8,ASP.NET Core,Entity Framework 7.0,Visual Studio 2022")]
    public void GetKeywords_NormalizesAndPreservesOrder(string rawKeywords, string expected)
    {
        // Act
        var result = ContentProcessor.GetKeywords(rawKeywords);

        // Assert
        Assert.Equal(expected, result);
    }
}
