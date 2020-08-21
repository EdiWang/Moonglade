using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Moonglade.Model;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class ModelTests
    {
        [Test]
        public void TestModelCreation()
        {
            Assert.DoesNotThrow(() =>
            {
                var dt = new DegreeTag
                {
                    Degree = 996,
                    DisplayName = ".NET",
                    Id = 251,
                    NormalizedName = "dot-net"
                };

                var mm = new MenuModel
                {
                    DisplayOrder = 404,
                    Icon = "icon",
                    Id = Guid.NewGuid(),
                    IsOpenInNewTab = false,
                    Title = ".NET Rocks",
                    Url = "https://dot.net"
                };

                var cat = new Category
                {
                    DisplayName = "dname",
                    Id = Guid.Empty,
                    Note = "note",
                    RouteName = "route"
                };

                var fl = new FriendLink
                {
                    Id = Guid.Empty,
                    LinkUrl = "https://996.icu",
                    Title = "996 ICU"
                };

                var arch = new Archive(2020, 9, 1);

                JsonSerializer.Serialize(dt);
                JsonSerializer.Serialize(mm);
                JsonSerializer.Serialize(cat);
                JsonSerializer.Serialize(arch);
                JsonSerializer.Serialize(fl);
            });
        }
    }
}
