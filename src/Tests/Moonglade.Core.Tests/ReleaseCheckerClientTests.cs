using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moq;
using NUnit.Framework;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq.Protected;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class ReleaseCheckerClientTests
    {
        private MockRepository _mockRepository;

        private Mock<IConfiguration> _mockConfiguration;
        private Mock<HttpMessageHandler> _handlerMock;
        private Mock<ILogger<ReleaseCheckerClient>> _mockLogger;
        private HttpClient _magicHttpClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockConfiguration = _mockRepository.Create<IConfiguration>();
            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
            _mockLogger = _mockRepository.Create<ILogger<ReleaseCheckerClient>>();
        }

        private ReleaseCheckerClient CreateReleaseCheckerClient()
        {
            _magicHttpClient = new(_handlerMock.Object);
            return new(_mockConfiguration.Object, _magicHttpClient, _mockLogger.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void CheckNewReleaseAsync_EmptyApiAddress(string apiAddress)
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns(apiAddress);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var releaseCheckerClient = CreateReleaseCheckerClient();
            });
        }

        [Test]
        public void CheckNewReleaseAsync_BadApiAddress()
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns("!@$#@%^$#996");

            Assert.Throws<InvalidOperationException>(() =>
            {
                var releaseCheckerClient = CreateReleaseCheckerClient();
            });
        }

        [Test]
        public void CheckNewReleaseAsync_UnsuccessResponse()
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns("https://996.icu");

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("")
                })
                .Verifiable();

            var releaseCheckerClient = CreateReleaseCheckerClient();

            Assert.ThrowsAsync<Exception>(async () =>
            {
                var result = await releaseCheckerClient.CheckNewReleaseAsync();
            });
        }

        [Test]
        public async Task CheckNewReleaseAsync_SuccessResponse()
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns("https://996.icu");

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\n  \"url\": \"https://api.github.com/repos/EdiWang/Moonglade/releases/41980970\",\n  \"assets_url\": \"https://api.github.com/repos/EdiWang/Moonglade/releases/41980970/assets\",\n  \"upload_url\": \"https://uploads.github.com/repos/EdiWang/Moonglade/releases/41980970/assets{?name,label}\",\n  \"html_url\": \"https://github.com/EdiWang/Moonglade/releases/tag/v11.3\",\n  \"id\": 41980970,\n  \"author\": {\n    \"login\": \"EdiWang\",\n    \"id\": 3304703,\n    \"node_id\": \"MDQ6VXNlcjMzMDQ3MDM=\",\n    \"avatar_url\": \"https://avatars.githubusercontent.com/u/3304703?v=4\",\n    \"gravatar_id\": \"\",\n    \"url\": \"https://api.github.com/users/EdiWang\",\n    \"html_url\": \"https://github.com/EdiWang\",\n    \"followers_url\": \"https://api.github.com/users/EdiWang/followers\",\n    \"following_url\": \"https://api.github.com/users/EdiWang/following{/other_user}\",\n    \"gists_url\": \"https://api.github.com/users/EdiWang/gists{/gist_id}\",\n    \"starred_url\": \"https://api.github.com/users/EdiWang/starred{/owner}{/repo}\",\n    \"subscriptions_url\": \"https://api.github.com/users/EdiWang/subscriptions\",\n    \"organizations_url\": \"https://api.github.com/users/EdiWang/orgs\",\n    \"repos_url\": \"https://api.github.com/users/EdiWang/repos\",\n    \"events_url\": \"https://api.github.com/users/EdiWang/events{/privacy}\",\n    \"received_events_url\": \"https://api.github.com/users/EdiWang/received_events\",\n    \"type\": \"User\",\n    \"site_admin\": false\n  },\n  \"node_id\": \"MDc6UmVsZWFzZTQxOTgwOTcw\",\n  \"tag_name\": \"v11.3\",\n  \"target_commitish\": \"master\",\n  \"name\": \"May 2021 Release\",\n  \"draft\": false,\n  \"prerelease\": false,\n  \"created_at\": \"2021-04-28T05:10:32Z\",\n  \"published_at\": \"2021-04-26T03:17:34Z\",\n  \"assets\": [\n    {\n      \"url\": \"https://api.github.com/repos/EdiWang/Moonglade/releases/assets/35949335\",\n      \"id\": 35949335,\n      \"node_id\": \"MDEyOlJlbGVhc2VBc3NldDM1OTQ5MzM1\",\n      \"name\": \"moonglade-release-v11.3.zip\",\n      \"label\": null,\n      \"uploader\": {\n        \"login\": \"EdiWang\",\n        \"id\": 3304703,\n        \"node_id\": \"MDQ6VXNlcjMzMDQ3MDM=\",\n        \"avatar_url\": \"https://avatars.githubusercontent.com/u/3304703?v=4\",\n        \"gravatar_id\": \"\",\n        \"url\": \"https://api.github.com/users/EdiWang\",\n        \"html_url\": \"https://github.com/EdiWang\",\n        \"followers_url\": \"https://api.github.com/users/EdiWang/followers\",\n        \"following_url\": \"https://api.github.com/users/EdiWang/following{/other_user}\",\n        \"gists_url\": \"https://api.github.com/users/EdiWang/gists{/gist_id}\",\n        \"starred_url\": \"https://api.github.com/users/EdiWang/starred{/owner}{/repo}\",\n        \"subscriptions_url\": \"https://api.github.com/users/EdiWang/subscriptions\",\n        \"organizations_url\": \"https://api.github.com/users/EdiWang/orgs\",\n        \"repos_url\": \"https://api.github.com/users/EdiWang/repos\",\n        \"events_url\": \"https://api.github.com/users/EdiWang/events{/privacy}\",\n        \"received_events_url\": \"https://api.github.com/users/EdiWang/received_events\",\n        \"type\": \"User\",\n        \"site_admin\": false\n      },\n      \"content_type\": \"application/x-zip-compressed\",\n      \"state\": \"uploaded\",\n      \"size\": 25323745,\n      \"download_count\": 2,\n      \"created_at\": \"2021-04-28T05:15:15Z\",\n      \"updated_at\": \"2021-04-28T05:15:21Z\",\n      \"browser_download_url\": \"https://github.com/EdiWang/Moonglade/releases/download/v11.3/moonglade-release-v11.3.zip\"\n    }\n  ],\n  \"tarball_url\": \"https://api.github.com/repos/EdiWang/Moonglade/tarball/v11.3\",\n  \"zipball_url\": \"https://api.github.com/repos/EdiWang/Moonglade/zipball/v11.3\",\n  \"body\": \"# New Features\\r\\n- Support sub menus #411 \\r\\n- Local account sign in is now protected by captcha\\r\\n- Allow users to set MetaWeblog credentials in admin portal\\r\\n- Move notification endpoint settings into admin portal\\r\\n\\r\\n# Update and Fixes\\r\\n- Performance enhancement\\r\\n- Show non production environment warning\\r\\n- Merge security settings into advanced settings\\r\\n- Add modal darkmode style #536 \\r\\n\\r\\n# Upgrade from Previous Version\\r\\n\\r\\n## Run SQL Migration Script\\r\\n\\r\\n```tsql\\r\\nIF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SubMenu')\\r\\nCREATE TABLE [SubMenu](\\r\\n[Id] [uniqueidentifier] PRIMARY KEY NOT NULL,\\r\\n[Title] [nvarchar](64) NOT NULL,\\r\\n[Url] [nvarchar](256) NOT NULL,\\r\\n[IsOpenInNewTab] [bit] NOT NULL,\\r\\n[MenuId] [uniqueidentifier] NULL)\\r\\nGO\\r\\n\\r\\nIF NOT EXISTS(SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_NAME = N'FK_SubMenu_Menu')\\r\\nALTER TABLE [SubMenu] WITH CHECK ADD CONSTRAINT [FK_SubMenu_Menu] FOREIGN KEY([MenuId])\\r\\nREFERENCES [Menu] ([Id])\\r\\nON UPDATE CASCADE\\r\\nON DELETE CASCADE\\r\\nALTER TABLE [SubMenu] CHECK CONSTRAINT [FK_SubMenu_Menu]\\r\\nGO\\r\\n\\r\\nALTER TABLE Post ADD HashCheckSum INT\\r\\nGO\\r\\n\\r\\nUPDATE Post SET HashCheckSum = 996\\r\\nGO\\r\\n\\r\\nALTER TABLE Post ALTER COLUMN HashCheckSum INT NOT NULL\\r\\nGO\\r\\n```\"\n}\n")
                })
                .Verifiable();

            var releaseCheckerClient = CreateReleaseCheckerClient();

            var result = await releaseCheckerClient.CheckNewReleaseAsync();
            Assert.IsNotNull(result);

            Assert.AreEqual("https://github.com/EdiWang/Moonglade/releases/tag/v11.3", result.HtmlUrl);
            Assert.AreEqual("May 2021 Release", result.Name);
            Assert.AreEqual("v11.3", result.TagName);
            Assert.IsFalse(result.PreRelease);
        }
    }
}
