using System;
using Moonglade.ImageStorage;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
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

        [TestCase("007 Stupid")]
        [TestCase(".icu")]
        [TestCase(" ")]
        public void TestInvalidFileName(string name)
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            Assert.Catch<ArgumentException>(() =>
            {
                var fileName = gen.GetFileName(name);
            });
        }

        [TestCase("Choose .NET Core.png", "")]
        [TestCase("And Microsoft Azure.png", null)]
        [TestCase("Stay away from 996.png", " ")]
        public void TestNullEmptyWhiteSpaceAppendix(string name, string appendix)
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName(name, appendix);
            Assert.IsTrue(fileName == $"img-{uid}.png");
        }
    }
}