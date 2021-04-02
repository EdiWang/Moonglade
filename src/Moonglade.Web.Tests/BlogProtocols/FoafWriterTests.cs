using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.FriendLink;
using Moonglade.Web.BlogProtocols;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.BlogProtocols
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FoafWriterTests
    {
        private MockRepository _mockRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
        }

        private FoafWriter CreateFoafWriter()
        {
            return new();
        }

        [Test]
        public async Task GetFoafData_StateUnderTest_ExpectedBehavior()
        {
            var foafWriter = CreateFoafWriter();
            FoafDoc doc = new FoafDoc
            {
                Name = "996.icu",
                BlogUrl = "https://996.icu",
                Email = "fubao@996.icu",
                PhotoUrl = "https://996.icu/fubao.png"
            };
            string currentRequestUrl = "https://996.icu/fubao";
            IReadOnlyList<Link> friends = new List<Link>
            {
                new(){ Id = Guid.Empty, LinkUrl = "https://greenhat.today", Title = "Work 996 and wear green hat" }
            };

            // Act
            var result = await foafWriter.GetFoafData(
                doc,
                currentRequestUrl,
                friends);

            Assert.IsFalse(string.IsNullOrWhiteSpace(result));
        }
    }
}
