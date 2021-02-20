using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;

namespace Moonglade.Caching.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class BlogCacheTests
    {
        private IMemoryCache _memoryCache;
        private BlogMemoryCache _blogCache;

        [SetUp]
        public void Setup()
        {
            _memoryCache = Create.MockedMemoryCache();
            _blogCache = new(_memoryCache);
        }

        [Test]
        public void CtorEmptyCacheDivision()
        {
            Assert.IsNotNull(_blogCache.CacheDivision);
        }

        [Test]
        public void GetOrCreate_Success()
        {
            var fubao = _blogCache.GetOrCreate(CacheDivision.General, "fubao", _ => 996);
            Assert.AreEqual(996, fubao);

            var cd = _blogCache.CacheDivision["General"];

            Assert.IsNotNull(cd);
            Assert.IsNotNull(cd.First());
            Assert.AreEqual("fubao", cd.First());

            var number = _memoryCache.Get<int>("General-fubao");
            Assert.AreEqual(996, number);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void GetOrCreate_EmptyKey(string key)
        {
            var fubao = _blogCache.GetOrCreate(CacheDivision.General, key, _ => 996);
            Assert.AreEqual(0, fubao);
        }

        [Test]
        public async Task GetOrCreateAsync_Success()
        {
            var pdd = await _blogCache.GetOrCreateAsync(CacheDivision.General, "pdd", _ => Task.FromResult(007));
            Assert.AreEqual(007, pdd);

            var cd = _blogCache.CacheDivision["General"];

            Assert.IsNotNull(cd);
            Assert.IsNotNull(cd.First());
            Assert.AreEqual("pdd", cd.First());

            var number = _memoryCache.Get<int>("General-pdd");
            Assert.AreEqual(007, number);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public async Task GetOrCreateAsync_EmptyKey(string key)
        {
            var pdd = await _blogCache.GetOrCreateAsync(CacheDivision.General, key, _ => Task.FromResult(007));
            Assert.AreEqual(0, pdd);
        }

        [Test]
        public void Remove_NonExistingKey()
        {
            Assert.DoesNotThrow(() =>
            {
                _blogCache.Remove(CacheDivision.Page);
            });
        }

        [Test]
        public void Remove_EmptyExistingKey()
        {
            Assert.DoesNotThrow(() =>
            {
                _blogCache.GetOrCreate(CacheDivision.General, "fubao", _ => 996);
                _blogCache.Remove(CacheDivision.General, "fubao");
            });
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Remove_EmptyKey(string key)
        {
            Assert.DoesNotThrow(() =>
            {
                _blogCache.Remove(CacheDivision.General, key);
            });
        }

        [Test]
        public void RemoveAllCache_Success()
        {
            _blogCache.GetOrCreate(CacheDivision.General, "postcount", _ => 996);
            _blogCache.GetOrCreate(CacheDivision.General, "ali", _ => "fubao");
            _blogCache.GetOrCreate(CacheDivision.PostCountCategory, "pdd", _ => 007);
            _blogCache.GetOrCreate(CacheDivision.PostCountTag, "hw", _ => 251);

            _blogCache.RemoveAllCache();

            var postcount = _memoryCache.Get<int>("General-postcount");
            var ali = _memoryCache.Get<string>("General-ali");
            var pdd = _memoryCache.Get<int>("PostCountCategory-pdd");
            var hw = _memoryCache.Get<int>("PostCountTag-hw");

            Assert.AreEqual(0, postcount);
            Assert.AreEqual(null, ali);
            Assert.AreEqual(0, pdd);
            Assert.AreEqual(0, hw);
        }

        [Test]
        public void Remove_Success()
        {
            _blogCache.GetOrCreate(CacheDivision.General, "postcount", _ => 996);
            _blogCache.GetOrCreate(CacheDivision.General, "ali", _ => "fubao");
            _blogCache.GetOrCreate(CacheDivision.PostCountCategory, "pdd", _ => 007);
            _blogCache.GetOrCreate(CacheDivision.PostCountTag, "hw", _ => 251);

            _blogCache.Remove(CacheDivision.General, "postcount");
            _blogCache.Remove(CacheDivision.PostCountCategory);
            _blogCache.Remove(CacheDivision.PostCountTag);

            var postcount = _memoryCache.Get<int>("General-postcount");
            var ali = _memoryCache.Get<string>("General-ali");
            var pdd = _memoryCache.Get<int>("PostCountCategory-pdd");
            var hw = _memoryCache.Get<int>("PostCountTag-hw");

            Assert.AreEqual(0, postcount);
            Assert.AreEqual("fubao", ali);
            Assert.AreEqual(0, pdd);
            Assert.AreEqual(0, hw);
        }
    }
}
