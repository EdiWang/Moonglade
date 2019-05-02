using System;
using System.Diagnostics;
using Moonglade.ImageStorage;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class GuidFileNameGeneratorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestNonAppendix()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName("Microsoft Rocks.png");
            Assert.IsTrue(fileName == $"img-{uid}.png");
        }

        [Test]
        public void TestAppendix()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName("Microsoft Rocks.png", "origin");
            Assert.IsTrue(fileName == $"img-{uid}-origin.png");
        }

        [Test]
        public void TestLetterCase()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName("996 Sucks.PNG", "ICU");
            Assert.IsTrue(fileName == $"img-{uid}-icu.png");
        }

        [Test]
        public void TestInvalidFileName()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            Assert.Catch<ArgumentException>(() =>
            {
                var fileName = gen.GetFileName("007 Stupid");
            });
            Assert.Catch<ArgumentException>(() =>
            {
                var fileName = gen.GetFileName(".icu");
            });
            Assert.Catch<ArgumentNullException>(() =>
            {
                var fileName = gen.GetFileName(" ");
            });
        }

        [Test]
        public void TestNullEmptyWhiteSpaceAppendix()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName1 = gen.GetFileName("Choose .NET Core.png", string.Empty);
            var fileName2 = gen.GetFileName("And Microsoft Azure.png", null);
            var fileName3 = gen.GetFileName("Stay away from 996.png", " ");
            Assert.IsTrue(fileName1 == $"img-{uid}.png");
            Assert.IsTrue(fileName2 == $"img-{uid}.png");
            Assert.IsTrue(fileName3 == $"img-{uid}.png");
        }
    }
}