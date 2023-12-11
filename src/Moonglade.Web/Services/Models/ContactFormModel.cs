// MIT License
//
// Copyright (c) 2022 Sascha Manns
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Services.Models
{
	/// <summary>
	/// Model for the Contact Form.
	/// </summary>
	public class ContactFormModel
	{
		/// <summary>
		/// Gets or sets the email.
		/// </summary>
		/// <value>
		/// The email.
		/// </value>
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the message.
		/// </summary>
		/// <value>
		/// The message.
		/// </value>
		[StringLength(700, ErrorMessage = "Your message is too long. Please shorten it to max. 700 chars.")]
		[MinLength(5)]
		[Required]
		public string Body { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// <value>
		/// The name.
		/// </value>
		[Required]
		[StringLength(100, ErrorMessage = "Name is too long. Just 100 chars allowed.")]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the subject.
		/// </summary>
		/// <value>
		/// The subject.
		/// </value>
		[Required]
		[StringLength(150, ErrorMessage = "Subject too long. Just 150 chars allowed.")]
		public string Subject { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the attachment.
		/// </summary>
		/// <value>
		/// The attachment.
		/// </value>
		public IList<IFormFile> Attachment { get; set; }
	}
}
