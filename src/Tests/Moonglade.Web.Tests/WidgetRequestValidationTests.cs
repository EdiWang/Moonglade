using Moonglade.Data.Entities;
using Moonglade.Widgets;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests;

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

    private static EditWidgetRequest CreateRequest(WidgetType widgetType, string contentCode) =>
        new()
        {
            Title = "Widget",
            WidgetType = widgetType,
            DisplayOrder = 0,
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
