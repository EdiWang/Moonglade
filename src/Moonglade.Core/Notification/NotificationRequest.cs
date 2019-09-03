namespace Moonglade.Core.Notification
{
    internal class NotificationRequest<T> where T : class
    {
        public string AdminEmail { get; set; }
        public string EmailDisplayName { get; set; }
        public MailMesageTypes MessageType { get; set; }
        public T Payload { get; set; }

        public NotificationRequest(MailMesageTypes messageType, T payload)
        {
            MessageType = messageType;
            Payload = payload;
        }
    }
}