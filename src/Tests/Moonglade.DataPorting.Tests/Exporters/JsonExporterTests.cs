using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moonglade.Data.Infrastructure;
using Moonglade.DataPorting.Exporters;
using Moq;
using NUnit.Framework;

namespace Moonglade.DataPorting.Tests.Exporters
{
    [TestFixture]
    public class JsonExporterTests
    {
        private MockRepository _mockRepository;
        private Mock<IRepository<int>> _mockRepo;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockRepo = _mockRepository.Create<IRepository<int>>();
        }

        private JsonExporter<int> CreateJsonExporter()
        {
            return new(_mockRepo.Object);
        }

        [Test]
        public async Task ExportData_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<string> data = new List<string>
            {
                "996", "ICU"
            };

            _mockRepo.Setup(p => p.SelectAsync(It.IsAny<Expression<Func<int, string>>>(), true)).Returns(Task.FromResult(data));
            var jsonExporter = CreateJsonExporter();

            var result = await jsonExporter.ExportData(p => "251", CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.AreEqual(ExportFormat.SingleJsonFile, result.ExportFormat);
            Assert.IsNotNull(result.Content);
        }
    }
}
