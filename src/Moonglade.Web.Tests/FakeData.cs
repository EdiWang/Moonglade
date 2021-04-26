using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;

namespace Moonglade.Web.Tests
{
    [ExcludeFromCodeCoverage]
    internal class FakeData
    {
        public static readonly Guid Uid1 = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");
        public static string Title1 => "“996”工作制，即每天早 9 点到岗，一直工作到晚上 9 点，每周工作 6 天。";
        public static string Title2 => "Work 996 and Get into ICU";
        public static string Title3 => "Work 996";
        public static string Slug1 => "work-996-and-get-into-icu";
        public static string Slug2 => "work-996";
        public static string Content1 =>
            "中国大陆工时规管现况（标准工时）： 一天工作时间为 8 小时，平均每周工时不超过 40 小时；加班上限为一天 3 小时及一个月 36 小时，逾时工作薪金不低于平日工资的 150%。而一周最高工时则为 48 小时。平均每月计薪天数为 21.75 天。";
        public static string Url1 => "https://996.icu";

        public static IReadOnlyList<PostDigest> FakePosts => new List<PostDigest>
        {
            new()
            {
                Title = Title1,
                ContentAbstract = Content1,
                LangCode = "zh-CN",
                PubDateUtc = new(996, 9, 6),
                Slug = "996-icu",
                Tags = new Tag[]{
                    new ()
                    {
                        DisplayName = "996",
                        Id = 996,
                        NormalizedName = "icu"
                    }
                }
            }
        };
    }
}
