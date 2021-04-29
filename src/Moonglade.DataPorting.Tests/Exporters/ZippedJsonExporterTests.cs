using Moonglade.Data.Infrastructure;
using Moonglade.DataPorting.Exporters;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.DataPorting.Tests.Exporters
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ZippedJsonExporterTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<int>> _mockRepo;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockRepo = _mockRepository.Create<IRepository<int>>();
        }

        private ZippedJsonExporter<int> CreateZippedJsonExporter()
        {
            return new(_mockRepo.Object, "996", Path.GetTempPath());
        }

        [Test]
        public async Task ExportData_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<KeyValuePair<string, string>> data = new List<KeyValuePair<string, string>>
            {
                new("996", "ICU"),
                new("251", "404")
            };

            _mockRepo.Setup(p => p.SelectAsync(It.IsAny<Expression<Func<int, KeyValuePair<string, string>>>>(), true)).Returns(Task.FromResult(data));
            var zippedJsonExporter = CreateZippedJsonExporter();

            var result = await zippedJsonExporter.ExportData(It.IsAny<Expression<Func<int, KeyValuePair<string, string>>>>());

            Assert.IsNotNull(result);
            Assert.AreEqual(ExportFormat.ZippedJsonFiles, result.ExportFormat);
            Assert.IsNotNull(result.FilePath);

            try
            {
                File.Delete(result.FilePath);
            }
            catch { }
        }
    }
}
