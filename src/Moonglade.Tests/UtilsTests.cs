using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class UtilsTests
    {
        [Test]
        public void TestUtcToZoneTime()
        {
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = Utils.UtcToZoneTime(utc, 8);
            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 8:00:00"));
        }

        [Test]
        public void TestLeft()
        {
            var str = "Microsoft Rocks!";
            var left = Utils.Left(str, 9);
            Assert.IsTrue(left == "Microsoft");
        }

        [Test]
        public void TestRight()
        {
            var str = "996 Sucks!";
            var left = Utils.Right(str, 6);
            Assert.IsTrue(left == "Sucks!");
        }

        [Test]
        public void TestNormalizeTagName()
        {
            var tag1org = ".NET Core";
            var tag2org = "C#";
            var tag1 = Utils.NormalizeTagName(tag1org);
            var tag2 = Utils.NormalizeTagName(tag2org);
            Assert.IsTrue(tag1 == "dotnet-core");
            Assert.IsTrue(tag2 == "csharp");
        }
    }
}
