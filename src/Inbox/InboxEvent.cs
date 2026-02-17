using System;

namespace OficinaCardozo.ExecutionService.Inbox
{
    public class InboxEvent
    {
        public Guid Id { get; set; }
        public string EventId { get; set; }
        public string EventType { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
