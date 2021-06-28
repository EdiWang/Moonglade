using System;
using System.Collections.Generic;
using Moonglade.Core;

namespace Moonglade.Web.Tests
{
    internal class FakeData
    {
        public static readonly Guid Uid1 = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");
        public static readonly Guid Uid2 = Guid.Parse("87359ad7-075f-4d9c-8e45-ebbd69bc50d6");
        public static string Title1 => "“996”工作制，即每天早 9 点到岗，一直工作到晚上 9 点，每周工作 6 天。";
        public static string Title2 => "Work 996 and Get into ICU";
        public static string Title3 => "Work 996";
        public static string Slug1 => "work-996-and-get-into-icu";
        public static string Slug2 => "work-996";
        public static string Content1 =>
            "中国大陆工时规管现况（标准工时）： 一天工作时间为 8 小时，平均每周工时不超过 40 小时；加班上限为一天 3 小时及一个月 36 小时，逾时工作薪金不低于平日工资的 150%。而一周最高工时则为 48 小时。平均每月计薪天数为 21.75 天。";
        public static string Url1 => "https://996.icu";
        public static string ShortString1 => "fubao";
        public static string ShortString2 => "996";
        public static int Int1 => 251;
        public static int Int2 => 996;

        public static string ImageBase64 =>
            "iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAYAAABzenr0AAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAyJpVFh0WE1MOmNvbS5hZG9iZS54bXAAAAAAADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMy1jMDExIDY2LjE0NTY2MSwgMjAxMi8wMi8wNi0xNDo1NjoyNyAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RSZWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZVJlZiMiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENTNiAoV2luZG93cykiIHhtcE1NOkluc3RhbmNlSUQ9InhtcC5paWQ6NkJDQUU0QzZBNjYzMTFFNDg2MzU4N0NCQUUyMEYwNTEiIHhtcE1NOkRvY3VtZW50SUQ9InhtcC5kaWQ6NkJDQUU0QzdBNjYzMTFFNDg2MzU4N0NCQUUyMEYwNTEiPiA8eG1wTU06RGVyaXZlZEZyb20gc3RSZWY6aW5zdGFuY2VJRD0ieG1wLmlpZDo2QkNBRTRDNEE2NjMxMUU0ODYzNTg3Q0JBRTIwRjA1MSIgc3RSZWY6ZG9jdW1lbnRJRD0ieG1wLmRpZDo2QkNBRTRDNUE2NjMxMUU0ODYzNTg3Q0JBRTIwRjA1MSIvPiA8L3JkZjpEZXNjcmlwdGlvbj4gPC9yZGY6UkRGPiA8L3g6eG1wbWV0YT4gPD94cGFja2V0IGVuZD0iciI/Pk2nytYAAANLSURBVHjatJddSBRRFMfvzM6aWqmtGWkUSSVCShqVmlQPFSGVWQ9FkLUWPfgQEUUP0VNhkW+99FAUlH2AIUiE2QcJilqURhRUaC0RW1q5URu71s7O9D82A7PjzO7MunPhx+zOvTP/c88599w7nCzLbCrt+K0tyTw2E7QBF8+cby2gTyd+D2wEmVykyeOY8onCqg24PASvz/j6S/FfFa8GL8gIxwyAGHl3AJSBraAbdGjFwZjgoOv3KeKPDcTJMwEalIocWARawagitAyzz8T1NJCUa4w4whFQH55qCGrBTTBdcy94J7ew7UnWXC9+t4M83cyLQDOtALo/KQTtQ25LynVLItvUpaRfYjWBj97v7gx5OCN7Of4vAIOgEVwAO2ni4HbSIYB4MS7XDMQnmluWWP3oW25x+CeJfwAvQQ/YBd6AGsUQ+wYUeyTPwIirV5RYllF/r19gfYCLThjBYMRC3G6gjAcHQClyoFMdb9uAkMg6PwV5T/9ngYnS5H4JhfVriGPUrxjBV/4aoTwpWvXg+RWIxzzF23T99pLZ0ZXZ02Q2Fv4vojeiIl9k2n4yonbMVwfhYqN38jbE83G5lIaoV8+LFdEaYdKfqSRgcgZAnFNqeq6RyNMvsYvJpH/pVDxwBKw3EpmVLrO/0ckPGPR3G71YsDB7KqdnjfpIZN180fRZTf8f2rltG1DkkXKfjbi6yudE04TkizYdOA6iwL3S3mzefTdxCMZF1uEP8jlmS85CGwd7Id5iNoCP4/oaWnI5JolmoQ1RrYf49XiDeBNxyvbLiCG3Ok6iqZWP0HnoBlgB8cFEVppN6yLIt5JoVPnU9V5ZIAbdPDsE4atW3SQYzJ620R1WX0CVj2p/JMp+D/9wbX4X4HvsxEkfAto4ztt5ATwkVhREzxXMkPPsius94FK22Cw1tuoM4yxBH9hz3yf0JbtGta8+BtZoY/stFDf7KbvLEO+kxbUeoGp3Sh9bSiyD7KfzXCOEW1NxciUD0pWNJs1CmaUzfgPE/ak6OlMIjoKSBOPC4DDYlEpx1QPeBGPo46JeOcvRMk3pxwOvnFqNGkW/CVSp4k40MqDL4P57sBacBBHmYCMD9oNHipBf2fvLdV+0jrV/AgwA8eRFurpiJVsAAAAASUVORK5CYII=";

        public static IReadOnlyList<PostDigest> FakePosts => new List<PostDigest>
        {
            new()
            {
                Title = Title1,
                ContentAbstract = Content1,
                LangCode = "zh-CN",
                PubDateUtc = new(Int2, 9, 6),
                Slug = "996-icu",
                Tags = new Tag[]{
                    new ()
                    {
                        DisplayName = ShortString2,
                        Id = Int2,
                        NormalizedName = "icu"
                    }
                }
            }
        };

        public static BlogPage FakePage => new()
        {
            Id = Guid.Empty,
            CreateTimeUtc = new(Int2, 9, 6),
            CssContent = ".jack-ma .heart {color: black !important;}",
            HideSidebar = false,
            IsPublished = false,
            MetaDescription = "Fuck Jack Ma",
            RawHtmlContent = "<p>Fuck 996</p>",
            Slug = "fuck-jack-ma",
            Title = "Fuck Jack Ma 1000 years!",
            UpdateTimeUtc = new DateTime(1996, 9, 6)
        };
    }
}
