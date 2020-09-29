using System;

namespace Moonglade.Model
{
    public class PingbackHistory
    {
        public Guid Id { get; set; }
        public string Domain { get; set; }
        public string SourceUrl { get; set; }
        public string SourceTitle { get; set; }
        public string SourceIp { get; set; }
        public Guid TargetPostId { get; set; }
        public string TargetPostTitle { get; set; }
        public DateTime PingTimeUtc { get; set; }
    }
}
