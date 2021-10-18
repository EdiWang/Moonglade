using Moonglade.Data.Infrastructure;
using Moonglade.Data.Porting;
using Moonglade.Data.Porting.Exporters;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Data.Tests.Exporters;

[TestFixture]
public class CSVExporterTests
{
    private MockRepository _mockRepository;

    private Mock<IRepository<int>> _mockRepo;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockRepo = _mockRepository.Create<IRepository<int>>();
    }

    private CSVExporter<int> CreateCSVExporter()
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

        _mockRepo.Setup(p => p.SelectAsync(It.IsAny<Expression<Func<int, KeyValuePair<string, string>>>>())).Returns(Task.FromResult(data));

        var csvExporter = CreateCSVExporter();
        var result = await csvExporter.ExportData(It.IsAny<Expression<Func<int, KeyValuePair<string, string>>>>(), CancellationToken.None);

        Assert.IsNotNull(result);
        Assert.AreEqual(ExportFormat.SingleCSVFile, result.ExportFormat);
        Assert.IsNotNull(result.FilePath);

        try
        {
            File.Delete(result.FilePath);
        }
        catch { }
    }
}