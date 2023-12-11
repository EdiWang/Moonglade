
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Services.Models
{
	/// <summary>
	/// Marker object for geo coordinates
	/// </summary>
	public class MarkerData
	{
		/// <summary>
		/// Gets or sets the latitude.
		/// </summary>
		/// <value>
		/// The latitude.
		/// </value>
		[Required]
		public double Latitude { get; set; }

		/// <summary>
		/// Gets or sets the longitude.
		/// </summary>
		/// <value>
		/// The longitude.
		/// </value>
		[Required]
		public double Longitude { get; set; }

		/// <summary>
		/// Gets or sets the city.
		/// </summary>
		/// <value>
		/// The city.
		/// </value>
		public string City { get; set; }
	}
}
