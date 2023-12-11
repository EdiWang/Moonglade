namespace Moonglade.Web.Services.Models
{
	/// <summary>
	/// Ad Date Range Service.
	/// </summary>
	public class AdDateRange
	{
#pragma warning disable SA1401 // Fields should be private
		/// <summary>
		/// The start
		/// </summary>
		public readonly DateTime Start;

		/// <summary>
		/// The end
		/// </summary>
		public readonly DateTime End;

		/// <summary>
		/// The ads
		/// </summary>
		public readonly string[] Ads;
#pragma warning restore SA1401 // Fields should be private

		/// <summary>
		/// Initializes a new instance of the <see cref="AdDateRange"/> class.
		/// </summary>
		/// <param name="startDate">The start date.</param>
		/// <param name="endDate">The end date.</param>
		/// <param name="ads">The ads.</param>
		/// <exception cref="System.InvalidOperationException">Invalid Ads.</exception>
		public AdDateRange(string startDate, string endDate, params string[] ads)
		{
			if (!DateTime.TryParse(startDate, out Start) || !DateTime.TryParse(endDate, out End) || ads == null || ads.Length == 0)
			{
				throw new InvalidOperationException("Invalid Ads");
			}

			Ads = ads;
		}
	}
}
