using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        public static void WriteFoaf(Stream ms, string name, string blogUrl, string email, string photoUrl, string currentRequestUrl, IReadOnlyList<FriendLink> friends)
        {
            // begin FOAF
            var writer = GetWriter(ms);

            // write DOCUMENT
            writer.WriteStartElement("foaf", "PersonalProfileDocument", null);
            writer.WriteAttributeString("rdf", "about", null, string.Empty);
            writer.WriteStartElement("foaf", "maker", null);
            writer.WriteAttributeString("rdf", "resource", null, "#me");
            writer.WriteEndElement(); // foaf:maker
            writer.WriteStartElement("foaf", "primaryTopic", null);
            writer.WriteAttributeString("rdf", "resource", null, "#me");
            writer.WriteEndElement(); // foaf:primaryTopic
            writer.WriteEndElement(); // foaf:PersonalProfileDocument

            var me = new FoafPerson("#me")
            {
                Blog = blogUrl,
                Email = email,
                PhotoUrl = photoUrl,
                Friends = new()
            };

            foreach (var friend in friends)
            {
                me.Friends.Add(new("#" + friend.Title) { Homepage = friend.LinkUrl });
            }

            WriteFoafPerson(writer, me, currentRequestUrl);

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }

        private static void WriteFoafPerson(XmlWriter writer, FoafPerson person, string currentRequestUrl)
        {
            writer.WriteStartElement("foaf", "Person", null);
            writer.WriteElementString("foaf", "name", null, person.Name);
            if (person.Title != string.Empty)
            {
                writer.WriteElementString("foaf", "title", null, person.Title);
            }

            if (person.FirstName != string.Empty)
            {
                writer.WriteElementString("foaf", "givenname", null, person.FirstName);
            }

            if (person.LastName != string.Empty)
            {
                writer.WriteElementString("foaf", "family_name", null, person.LastName);
            }

            if (!string.IsNullOrEmpty(person.Email))
            {
                writer.WriteElementString("foaf", "mbox_sha1sum", null, CalculateSha1(person.Email, Encoding.UTF8));
            }

            if (!string.IsNullOrEmpty(person.Homepage))
            {
                writer.WriteStartElement("foaf", "homepage", null);
                writer.WriteAttributeString("rdf", "resource", null, person.Homepage);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(person.Blog))
            {
                writer.WriteStartElement("foaf", "weblog", null);
                writer.WriteAttributeString("rdf", "resource", null, person.Blog);
                writer.WriteEndElement();
            }

            if (person.Rdf != string.Empty && person.Rdf != currentRequestUrl)
            {
                writer.WriteStartElement("rdfs", "seeAlso", null);
                writer.WriteAttributeString("rdf", "resource", null, person.Rdf);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(person.Birthday))
            {
                writer.WriteElementString("foaf", "birthday", null, person.Birthday);
            }

            if (!string.IsNullOrEmpty(person.PhotoUrl))
            {
                writer.WriteStartElement("foaf", "depiction", null);
                writer.WriteAttributeString("rdf", "resource", null, person.PhotoUrl);
                writer.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(person.Phone))
            {
                writer.WriteElementString("foaf", "phone", null, person.Phone);
            }

            if (person.Friends != null && person.Friends.Count > 0)
            {
                foreach (var friend in person.Friends)
                {
                    writer.WriteStartElement("foaf", "knows", null);
                    WriteFoafPerson(writer, friend, currentRequestUrl);
                    writer.WriteEndElement(); // foaf:knows
                }
            }

            writer.WriteEndElement(); // foaf:Person
        }

        private static XmlWriter GetWriter(Stream stream)
        {
            var settings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
            var xmlWriter = XmlWriter.Create(stream, settings);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

            foreach (var prefix in SupportedNamespaces.Keys)
            {
                xmlWriter.WriteAttributeString("xmlns", prefix, null, SupportedNamespaces[prefix]);
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
