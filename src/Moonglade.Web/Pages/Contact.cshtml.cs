using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.RazorPages;

using Moonglade.Web.Services.Models;

namespace Moonglade.Web.Pages
{
	public class ContactModel : PageModel
	{
		[Required]
		public string Name { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Subject { get; set; }

		[Required]
		public string Body { get; set; }
		public IList<IFormFile> Attachment { get; set; }
		public void OnGet()
		{
		}

		public void OnPostSubmit(ContactFormModel model)
		{
			Name = model.Name;
			Email = model.Email;
			Subject = model.Subject;
			Body = model.Body;
			Attachment = model.Attachment;

			//Read SMTP settings from AppSettings.json.
			//string host = this.Configuration.GetValue<string>("Smtp:Server");
			//int port = this.Configuration.GetValue<int>("Smtp:Port");
			//string fromAddress = this.Configuration.GetValue<string>("Smtp:FromAddress");
			//string userName = this.Configuration.GetValue<string>("Smtp:UserName");
			//string password = this.Configuration.GetValue<string>("Smtp:Password");

			//using (MailMessage mm = new MailMessage(fromAddress, "admin@aspsnippets.com"))
			//{
			//    mm.Subject = model.Subject;
			//    mm.Body = "Name: " + model.Name + "<br /><br />Email: " + model.Email + "<br />" + model.Body;
			//    mm.IsBodyHtml = true;

			//    if (model.Attachment.Length > 0)
			//    {
			//        string fileName = Path.GetFileName(model.Attachment.FileName);
			//        mm.Attachments.Add(new Attachment(model.Attachment.OpenReadStream(), fileName));
			//    }

			//    using (SmtpClient smtp = new SmtpClient())
			//    {
			//        smtp.Host = host;
			//        smtp.EnableSsl = true;
			//        NetworkCredential NetworkCred = new NetworkCredential(userName, password);
			//        smtp.UseDefaultCredentials = true;
			//        smtp.Credentials = NetworkCred;
			//        smtp.Port = port;
			//        smtp.Send(mm);
			//        this.Message = "Email sent sucessfully.";
			//    }
			//}
		}
	}
}
