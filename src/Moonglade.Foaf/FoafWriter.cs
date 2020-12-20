using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Moonglade.Model;

namespace Moonglade.Foaf
{
    /// <summary>
    /// http://xmlns.com/foaf/spec/20140114.html
    /// </summary>
    public class FoafWriter
    {
        private static Dictionary<string, string> _xmlNamespaces;
        private static Dictionary<string, string> SupportedNamespaces =>
            _xmlNamespaces ??= new()
            {
                { "foaf", "http://xmlns.com/foaf/0.1/" },
                { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" }
            };

        public static string ContentType => "application/rdf+xml";

        public static async Task<byte[]> GetFoafData(FoafDoc doc, string currentRequestUrl, IReadOnlyList<FriendLink> friends)
        {
            await using var ms = new MemoryStream();
            var writer = await GetWriter(ms);

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
                Name = doc.Name,
                Blog = doc.BlogUrl,
                Email = doc.Email,
                PhotoUrl = doc.PhotoUrl,
                Friends = new()
            };

            foreach (var friend in friends)
            {
                me.Friends.Add(new("#" + friend.Id)
                {
                    Name = friend.Title,
                    Homepage = friend.LinkUrl
                });
            }

            await WriteFoafPerson(writer, me, currentRequestUrl);

            await writer.WriteEndElementAsync();
            await writer.WriteEndDocumentAsync();
            writer.Close();

            await ms.FlushAsync();
            var bytes = ms.ToArray();
            return bytes;
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

            if (person.Friends != null && person.Friends.Count > 0)
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

        private static async Task<XmlWriter> GetWriter(Stream stream)
        {
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true, Indent = true };
            var xmlWriter = XmlWriter.Create(stream, settings);

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
            var cryptoTransformSha1 = new SHA1CryptoServiceProvider();
            var hash = BitConverter.ToString(cryptoTransformSha1.ComputeHash(buffer)).Replace("-", string.Empty);

            return hash.ToLower();
        }
    }
}
