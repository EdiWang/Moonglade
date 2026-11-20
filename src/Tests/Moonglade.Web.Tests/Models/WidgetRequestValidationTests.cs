using Moonglade.Data.Entities;
using Moonglade.Widgets;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.Web.Tests.Models;

public class WidgetRequestValidationTests
{
    [Fact]
    public void LinkList_WithValidContentCode_IsValid()
    {
        var request = CreateRequest(
            WidgetType.LinkList,
            """
            [
              {
                "name": "GitHub",
                "icon": "bi-github",
                "url": "https://github.com",
                "openInNewTab": true,
                "order": 1
              }
            ]
            """);

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Fact]
    public void ImageLink_WithValidContentCode_IsValid()
    {
        var request = CreateRequest(
            WidgetType.ImageLink,
            """
            {
              "imageUrl": "https://example.com/banner.png",
              "title": "Banner",
              "altText": "Example banner",
              "linkUrl": "https://example.com",
              "openInNewTab": true
            }
            """);

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Fact]
    public void ButtonLink_WithValidContentCode_IsValid()
    {
        var request = CreateRequest(
            WidgetType.ButtonLink,
            """
            [
              {
                "text": "Read more",
                "url": "https://example.com",
                "cssClass": "btn-outline-primary",
                "openInNewTab": true
              }
            ]
            """);

        var results = Validate(request);

        Assert.Empty(results);
    }

    [Fact]
    public void LinkList_WithObjectContentCode_IsInvalid()
    {
        var request = CreateRequest(
            WidgetType.LinkList,
            """
            {
              "name": "GitHub",
              "url": "https://github.com"
            }
            """);

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(EditWidgetRequest.ContentCode)));
    }

    [Fact]
    public void ImageLink_WithoutImageUrl_IsInvalid()
    {
        var request = CreateRequest(
            WidgetType.ImageLink,
            """
            {
              "title": "Missing image URL",
              "linkUrl": "https://example.com"
            }
            """);

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(EditWidgetRequest.ContentCode)));
    }

    [Fact]
    public void ButtonLink_WithMoreThanThreeButtons_IsInvalid()
    {
        var request = CreateRequest(
            WidgetType.ButtonLink,
            """
            [
              { "text": "One", "url": "https://example.com/1" },
              { "text": "Two", "url": "https://example.com/2" },
              { "text": "Three", "url": "https://example.com/3" },
              { "text": "Four", "url": "https://example.com/4" }
            ]
            """);

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(EditWidgetRequest.ContentCode)));
    }

    [Fact]
    public void ButtonLink_WithoutText_IsInvalid()
    {
        var request = CreateRequest(
            WidgetType.ButtonLink,
            """
            [
              { "url": "https://example.com" }
            ]
            """);

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(EditWidgetRequest.ContentCode)));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10, true)]
    [InlineData(1000, true)]
    [InlineData(int.MaxValue, true)]
    public void LinkList_SortOrder_EnforcesNonnegativeIntegerRange(int order, bool valid)
    {
        var request = CreateRequest(WidgetType.LinkList, "[]");
        request.SortOrder = order;

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void LinkList_Region_RejectsUndefinedValues(int region, bool valid)
    {
        var request = CreateRequest(WidgetType.LinkList, "[]");
        request.Region = (WidgetRegion)region;

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void LinkList_Title_Enforces128CharacterLimit(int length, bool valid)
    {
        var request = CreateRequest(WidgetType.LinkList, "[]");
        request.Title = new string('A', length);

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("{\"sortOrder\":0}")]
    [InlineData("{\"region\":\"AfterCategories\"}")]
    public void JsonInput_RequiresBothPlacementFields(string json)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        var complete = JsonSerializer.Deserialize<EditWidgetRequest>("{\"region\":\"AfterCategories\",\"sortOrder\":0}", options);
        Assert.NotNull(complete);
        Assert.Equal(WidgetRegion.AfterCategories, complete.Region);
        Assert.Equal(0, complete.SortOrder);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EditWidgetRequest>(json, options));
    }

    [Theory]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void LinkList_ContentCode_EnforcesSerializedLengthLimit(int length, bool valid)
    {
        const string content = "[{\"name\":\"Link\",\"url\":\"https://example.com\"}]";
        var request = CreateRequest(WidgetType.LinkList, content.PadRight(length));

        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("[{\"name\":\"  \",\"url\":\"https://example.com\"}]")]
    [InlineData("[{\"name\":\"Example\",\"url\":\"  \"}]")]
    public void LinkList_WithNullOrBlankRequiredFields_IsInvalid(string content)
    {
        var results = Validate(CreateRequest(WidgetType.LinkList, content));

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(EditWidgetRequest.ContentCode)));
    }

    private static EditWidgetRequest CreateRequest(WidgetType widgetType, string contentCode) =>
        new()
        {
            Title = "Widget",
            WidgetType = widgetType,
            Region = WidgetRegion.AfterCategories,
            SortOrder = 0,
            IsEnabled = true,
            ContentCode = contentCode
        };

    private static List<ValidationResult> Validate(EditWidgetRequest request)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(request);

        Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        return results;
    }
}
