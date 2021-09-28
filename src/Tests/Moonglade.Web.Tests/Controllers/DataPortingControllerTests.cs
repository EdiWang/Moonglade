using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data.Porting;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class DataPortingControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private DataPortingController CreateDataPortingController()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task ExportDownload_SingleJsonFile()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<ExportTagsDataCommand>(), default))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleJsonFile,
                    Content = Array.Empty<byte>()
                }));

            var settingsController = CreateDataPortingController();
            ExportType type = ExportType.Tags;

            var result = await settingsController.ExportDownload(type, CancellationToken.None);
            Assert.IsInstanceOf<FileContentResult>(result);
        }

        [Test]
        public async Task ExportDownload_SingleCSVFile()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<ExportCategoryDataCommand>(), default))
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

            ExportType type = ExportType.Categories;

            var result = await settingsController.ExportDownload(type, CancellationToken.None);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

        [Test]
        public async Task ExportDownload_ZippedJsonFiles()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<ExportPostDataCommand>(), default))
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

            ExportType type = ExportType.Posts;

            var result = await settingsController.ExportDownload(type, CancellationToken.None);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

    }
}
