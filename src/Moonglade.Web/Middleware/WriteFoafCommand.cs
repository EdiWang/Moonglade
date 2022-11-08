using Moonglade.FriendLink;
using System.Security.Cryptography;
using System.Xml;

namespace Moonglade.Web.Middleware;

public class WriteFoafCommand : IRequest<string>
{
    public WriteFoafCommand(FoafDoc doc, string currentRequestUrl, IReadOnlyList<Link> links)
    {
        Doc = doc;
        CurrentRequestUrl = currentRequestUrl;
        Links = links;
    }

    public FoafDoc Doc { get; set; }

    public string CurrentRequestUrl { get; set; }

    public IReadOnlyList<Link> Links { get; set; }

    public static string ContentType => "application/rdf+xml";
}

/// <summary>
/// http://xmlns.com/foaf/spec/20140114.html
/// </summary>
public class WriteFoafCommandHandler : IRequestHandler<WriteFoafCommand, string>
{
    private static Dictionary<string, string> _xmlNamespaces;
    private static Dictionary<string, string> SupportedNamespaces =>
        _xmlNamespaces ??= new()
        {
            { "foaf", "http://xmlns.com/foaf/0.1/" },
            { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" }
        };

    public async Task<string> Handle(WriteFoafCommand request, CancellationToken ct)
    {
        var sw = new StringWriter();
        var writer = await GetWriter(sw);

        await writer.WriteStartElementAsync("foaf", "PersonalProfileDocument", null);
        await writer.WriteAttributeStringAsync("rdf", "about", null, string.Empty);
        await writer.WriteStartElementAsync("foaf", "maker", null);
        await writer.WriteAttributeStringAsync("rdf", "resource", null, "#me");
        await writer.WriteEndElementAsync(); // foaf:maker
        await writer.WriteStartElementAsync("foaf", "primaryTopic", null);
        await writer.WriteAttributeStringAsync("rdf", "resource", null, "#me");
        await writer.WriteEndElementAsync(); // foaf:primaryTopic
        await writer.WriteEndElementAsync(); // foaf:PersonalProfileDocument

        var me = new FoafPerson("#me")
        {
            Name = request.Doc.Name,
            Blog = request.Doc.BlogUrl,
            Email = request.Doc.Email,
            PhotoUrl = request.Doc.PhotoUrl,
            Friends = new()
        };

        foreach (var friend in request.Links)
        {
            me.Friends.Add(new("#" + friend.Id)
            {
                Name = friend.Title,
                Homepage = friend.LinkUrl
            });
        }

        await WriteFoafPerson(writer, me, request.CurrentRequestUrl);

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        writer.Close();

        await sw.FlushAsync();
        sw.GetStringBuilder();
        var xml = sw.ToString();
        return xml;
    }

    private static async Task WriteFoafPerson(XmlWriter writer, FoafPerson person, string currentRequestUrl)
    {
        await writer.WriteStartElementAsync("foaf", "Person", null);
        await writer.WriteElementStringAsync("foaf", "name", null, person.Name);
        if (person.Title != string.Empty)
        {
            await writer.WriteElementStringAsync("foaf", "title", null, person.Title);
        }

        if (person.FirstName != string.Empty)
        {
            await writer.WriteElementStringAsync("foaf", "givenname", null, person.FirstName);
        }

        if (person.LastName != string.Empty)
        {
            await writer.WriteElementStringAsync("foaf", "family_name", null, person.LastName);
        }

        if (!string.IsNullOrEmpty(person.Email))
        {
            await writer.WriteElementStringAsync("foaf", "mbox_sha1sum", null, CalculateSha1(person.Email, Encoding.UTF8));
        }

        if (!string.IsNullOrEmpty(person.Homepage))
        {
            await writer.WriteStartElementAsync("foaf", "homepage", null);
            await writer.WriteAttributeStringAsync("rdf", "resource", null, person.Homepage);
            await writer.WriteEndElementAsync();
        }

        if (!string.IsNullOrEmpty(person.Blog))
        {
            await writer.WriteStartElementAsync("foaf", "weblog", null);
            await writer.WriteAttributeStringAsync("rdf", "resource", null, person.Blog);
            await writer.WriteEndElementAsync();
        }

        if (person.Rdf != string.Empty && person.Rdf != currentRequestUrl)
        {
            await writer.WriteStartElementAsync("rdfs", "seeAlso", null);
            await writer.WriteAttributeStringAsync("rdf", "resource", null, person.Rdf);
            await writer.WriteEndElementAsync();
        }

        if (!string.IsNullOrEmpty(person.Birthday))
        {
            await writer.WriteElementStringAsync("foaf", "birthday", null, person.Birthday);
        }

        if (!string.IsNullOrEmpty(person.PhotoUrl))
        {
            await writer.WriteStartElementAsync("foaf", "depiction", null);
            await writer.WriteAttributeStringAsync("rdf", "resource", null, person.PhotoUrl);
            await writer.WriteEndElementAsync();
        }

        if (!string.IsNullOrEmpty(person.Phone))
        {
            await writer.WriteElementStringAsync("foaf", "phone", null, person.Phone);
        }

        if (person.Friends is { Count: > 0 })
        {
            foreach (var friend in person.Friends)
            {
                await writer.WriteStartElementAsync("foaf", "knows", null);
                await WriteFoafPerson(writer, friend, currentRequestUrl);
                await writer.WriteEndElementAsync(); // foaf:knows
            }
        }

        await writer.WriteEndElementAsync(); // foaf:Person
    }

    private static async Task<XmlWriter> GetWriter(TextWriter sw)
    {
        var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true, Indent = true };
        var xmlWriter = XmlWriter.Create(sw, settings);

        await xmlWriter.WriteStartDocumentAsync();
        await xmlWriter.WriteStartElementAsync("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

        foreach (var prefix in SupportedNamespaces.Keys)
        {
            await xmlWriter.WriteAttributeStringAsync("xmlns", prefix, null, SupportedNamespaces[prefix]);
        }

        return xmlWriter;
    }

    private static string CalculateSha1(string text, Encoding enc)
    {
        var buffer = enc.GetBytes(text);
        var cryptoTransformSha1 = SHA1.Create();
        var hash = BitConverter.ToString(cryptoTransformSha1.ComputeHash(buffer)).Replace("-", string.Empty);

        return hash.ToLower();
    }
}