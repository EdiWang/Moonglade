using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.DataPorting;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class DataPortingControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IExportManager> _mockExportManager;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockExportManager = _mockRepository.Create<IExportManager>();
        }

        private DataPortingController CreateDataPortingController()
        {
            return new(
                _mockExportManager.Object);
        }

        [Test]
        public async Task ExportDownload_SingleJsonFile()
        {
            _mockExportManager.Setup(p => p.ExportData(ExportDataType.Tags))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleJsonFile,
                    Content = Array.Empty<byte>()
                }));

            var settingsController = CreateDataPortingController();
            ExportDataType type = ExportDataType.Tags;

            var result = await settingsController.ExportDownload(type);
            Assert.IsInstanceOf<FileContentResult>(result);
        }

        [Test]
        public async Task ExportDownload_SingleCSVFile()
        {
            _mockExportManager.Setup(p => p.ExportData(ExportDataType.Categories))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleCSVFile,
                    FilePath = @"C:\996\icu.csv"
                }));

            var settingsController = CreateDataPortingController();
            settingsController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            ExportDataType type = ExportDataType.Categories;

            var result = await settingsController.ExportDownload(type);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

        [Test]
        public async Task ExportDownload_ZippedJsonFiles()
        {
            _mockExportManager.Setup(p => p.ExportData(ExportDataType.Posts))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.ZippedJsonFiles,
                    FilePath = @"C:\996\icu.zip"
                }));

            var settingsController = CreateDataPortingController();
            settingsController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            ExportDataType type = ExportDataType.Posts;

            var result = await settingsController.ExportDownload(type);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

    }
}
