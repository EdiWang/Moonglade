using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class BlogCacheTests
    {
        private IMemoryCache _memoryCache;
        private BlogMemoryCache _blogMemory;

        [SetUp]
        public void Setup()
        {
            _memoryCache = Create.MockedMemoryCache();
            _blogMemory = new(_memoryCache);
        }

        [Test]
        public void CtorEmptyCacheDivision()
        {
            Assert.IsNotNull(_blogMemory.CacheDivision);
        }

        [Test]
        public void GetOrCreate_Success()
        {
            var fubao = _blogMemory.GetOrCreate(CacheDivision.General, "fubao", _ => 996);
            Assert.AreEqual(996, fubao);

            var cd = _blogMemory.CacheDivision["General"];

            Assert.IsNotNull(cd);
            Assert.IsNotNull(cd.First());
            Assert.AreEqual("fubao", cd.First());

            var number = _memoryCache.Get<int>("General-fubao");
            Assert.AreEqual(996, number);
        }

        [Test]
        public async Task GetOrCreateAsync_Success()
        {
            var pdd = await _blogMemory.GetOrCreateAsync(CacheDivision.General, "pdd", _ => Task.FromResult(007));
            Assert.AreEqual(007, pdd);

            var cd = _blogMemory.CacheDivision["General"];

            Assert.IsNotNull(cd);
            Assert.IsNotNull(cd.First());
            Assert.AreEqual("pdd", cd.First());

            var number = _memoryCache.Get<int>("General-pdd");
            Assert.AreEqual(007, number);
        }
    }
}
