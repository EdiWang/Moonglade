using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Text.RegularExpressions;
using System.Xml;

namespace Moonglade.Pingback;

public class ReceivePingCommand(string requestBody, string ip, Action<PingbackEntity> action)
    : IRequest<PingbackResponse>
{
    public string RequestBody { get; set; } = requestBody;

    public string IP { get; set; } = ip;

    public Action<PingbackEntity> Action { get; set; } = action;
}

public class ReceivePingCommandHandler(
        ILogger<ReceivePingCommandHandler> logger,
        IPingSourceInspector pingSourceInspector,
        IRepository<PingbackEntity> pingbackRepo,
        IRepository<PostEntity> postRepo) : IRequestHandler<ReceivePingCommand, PingbackResponse>
{
    private string _sourceUrl;
    private string _targetUrl;

    public async Task<PingbackResponse> Handle(ReceivePingCommand request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RequestBody))
            {
                logger.LogError("Pingback requestBody is null");
                return PingbackResponse.GenericError;
            }

            var valid = ValidateRequest(request.RequestBody);
            if (!valid) return PingbackResponse.InvalidPingRequest;

            logger.LogInformation($"Processing Pingback from: {_sourceUrl} ({request.IP}) to {_targetUrl}");

            var pingRequest = await pingSourceInspector.ExamineSourceAsync(_sourceUrl, _targetUrl);
            if (null == pingRequest) return PingbackResponse.InvalidPingRequest;
            if (!pingRequest.SourceHasLink)
            {
                logger.LogError("Pingback error: The source URI does not contain a link to the target URI.");
                return PingbackResponse.Error17SourceNotContainTargetUri;
            }
            if (pingRequest.ContainsHtml)
            {
                logger.LogWarning("Spam detected on current Pingback...");
                return PingbackResponse.SpamDetectedFakeNotFound;
            }

            var (slug, pubDate) = GetSlugInfoFromUrl(pingRequest.TargetUrl);
            var spec = new PostSpec(pubDate, slug);
            var (id, title) = await postRepo.FirstOrDefaultAsync(spec, p => new Tuple<Guid, string>(p.Id, p.Title));
            if (id == Guid.Empty)
            {
                logger.LogError($"Can not get post id and title for url '{pingRequest.TargetUrl}'");
                return PingbackResponse.Error32TargetUriNotExist;
            }

            logger.LogInformation($"Post '{id}:{title}' is found for ping.");

            var pinged = await pingbackRepo.AnyAsync(p =>
                p.TargetPostId == id &&
                p.SourceUrl == pingRequest.SourceUrl &&
                p.SourceIp.Trim() == request.IP, ct);

            if (pinged) return PingbackResponse.Error48PingbackAlreadyRegistered;

            logger.LogInformation("Adding received pingback...");

            var uri = new Uri(_sourceUrl);
            var obj = new PingbackEntity
            {
                Id = Guid.NewGuid(),
                PingTimeUtc = DateTime.UtcNow,
                Domain = uri.Host,
                SourceUrl = _sourceUrl,
                SourceTitle = pingRequest.Title,
                TargetPostId = id,
                TargetPostTitle = title,
                SourceIp = request.IP
            };

            await pingbackRepo.AddAsync(obj, ct);
            request.Action?.Invoke(obj);

            return PingbackResponse.Success;
        }
        catch (Exception e)
        {
            logger.LogError(e, nameof(ReceivePingCommandHandler));
            return PingbackResponse.GenericError;
        }
    }

    private static (string Slug, DateTime PubDate) GetSlugInfoFromUrl(string url)
    {
        var blogSlugRegex = new Regex(@"^https?:\/\/.*\/post\/(?<yyyy>\d{4})\/(?<MM>\d{1,12})\/(?<dd>\d{1,31})\/(?<slug>.*)$");
        Match match = blogSlugRegex.Match(url);
        if (!match.Success)
        {
            throw new FormatException("Invalid Slug Format");
        }

        int year = int.Parse(match.Groups["yyyy"].Value);
        int month = int.Parse(match.Groups["MM"].Value);
        int day = int.Parse(match.Groups["dd"].Value);
        string slug = match.Groups["slug"].Value;
        var date = new DateTime(year, month, day);

        return (slug, date);
    }

    private bool ValidateRequest(string requestBody)
    {
        logger.LogInformation($"Pingback received xml: {requestBody}");

        if (!requestBody.Contains("<methodName>pingback.ping</methodName>"))
        {
            logger.LogWarning("Could not find pingback method, request has been terminated.");
            return false;
        }

        var doc = new XmlDocument();
        doc.LoadXml(requestBody);

        var list = doc.SelectNodes("methodCall/params/param/value/string") ??
                   doc.SelectNodes("methodCall/params/param/value");

        if (list is null)
        {
            logger.LogWarning("Could not find Pingback sourceUrl and targetUrl, request has been terminated.");
            return false;
        }

        _sourceUrl = list[0]?.InnerText.Trim();
        _targetUrl = list[1]?.InnerText.Trim();

        return true;
    }
}