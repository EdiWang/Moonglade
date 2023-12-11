using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Helpers
{
    /// <summary>
    /// Model for calculating the time to read a story.
    /// </summary>
    public class TimeToRead
    {
        /// <summary>
        /// Gets or sets the minutes.
        /// </summary>
        /// <value>
        /// The minutes.
        /// </value>
        [Required]
        public int Minutes { get; set; }

        /// <summary>
        /// Gets or sets the seconds.
        /// </summary>
        /// <value>
        /// The seconds.
        /// </value>
        [Required]
        public int Seconds { get; set; }

        /// <summary>
        /// Gets or sets the string minutes.
        /// </summary>
        /// <value>
        /// The string minutes.
        /// </value>
        [Required]
        public string StrMinutes { get; set; }

        /// <summary>
        /// Gets or sets the string seconds.
        /// </summary>
        /// <value>
        /// The string seconds.
        /// </value>
        [Required]
        public string StrSeconds { get; set; }
    }
}
