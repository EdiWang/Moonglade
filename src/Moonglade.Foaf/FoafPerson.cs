using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Foaf
{
    public class FoafPerson
    {
        public FoafPerson(string id)
        {
            Birthday = string.Empty;
            Blog = string.Empty;
            Email = string.Empty;
            FirstName = string.Empty;
            Homepage = string.Empty;
            Image = string.Empty;
            LastName = string.Empty;
            Name = string.Empty;
            Phone = string.Empty;
            PhotoUrl = string.Empty;
            Rdf = string.Empty;
            Title = string.Empty;
            Id = id;
        }

        public FoafPerson(
            string id,
            string name,
            string title,
            string email,
            string homepage,
            string blog,
            string rdf,
            string firstName,
            string lastName,
            string image,
            string birthday,
            string phone)
        {
            PhotoUrl = string.Empty;
            Id = id;
            Name = name;
            Title = title;
            Email = email;
            Homepage = homepage;
            Blog = blog;
            Rdf = rdf;
            FirstName = firstName;
            LastName = lastName;
            Image = image;
            Birthday = birthday;
            Phone = phone;
        }

        public string Birthday { get; set; }
        public string Blog { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public List<FoafPerson> Friends { get; set; }
        public string Homepage { get; set; }
        public string Id { get; set; }
        public string Image { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string PhotoUrl { get; set; }
        public string Rdf { get; set; }
        public string Title { get; set; }
    }
}
