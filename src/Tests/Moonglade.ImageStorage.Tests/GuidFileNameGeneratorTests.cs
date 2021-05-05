using System;
using NUnit.Framework;

namespace Moonglade.ImageStorage.Tests
{
    [TestFixture]
    public class GuidFileNameGeneratorTests
    {
        [Test]
        public void NonAppendix()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName("Microsoft Rocks.png");
            Assert.IsTrue(fileName == $"img-{uid}.png");
        }

        [Test]
        public void Appendix()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName("Microsoft Rocks.png", "origin");
            Assert.IsTrue(fileName == $"img-{uid}-origin.png");
        }

        [Test]
        public void LetterCase()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName("996 Sucks.PNG", "ICU");
            Assert.IsTrue(fileName == $"img-{uid}-icu.png");
        }

        [TestCase("007 Stupid")]
        [TestCase(".icu")]
        public void InvalidFileName(string name)
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            Assert.Throws<ArgumentException>(() =>
            {
                gen.GetFileName(name);
            });
        }

        [TestCase(" ")]
        public void EmptyFileName(string name)
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            Assert.Throws<ArgumentNullException>(() =>
            {
                gen.GetFileName(name);
            });
        }

        [Test]
        public void GeneratorName()
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            Assert.AreEqual(gen.Name, nameof(GuidFileNameGenerator));
        }

        [TestCase("Choose .NET 5.png", "")]
        [TestCase("And Microsoft Azure.png", null)]
        [TestCase("Stay away from 996.png", " ")]
        public void NullEmptyWhiteSpaceAppendix(string name, string appendix)
        {
            var uid = Guid.NewGuid();
            var gen = new GuidFileNameGenerator(uid);
            var fileName = gen.GetFileName(name, appendix);
            Assert.IsTrue(fileName == $"img-{uid}.png");
        }
    }
}