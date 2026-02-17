using System;
using System.Threading.Tasks;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;

namespace OficinaCardozo.ExecutionService.EventHandlers
{
    public class PaymentConfirmedEvent
    {
        public string EventId { get; set; }
        public string OsId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class PaymentConfirmedHandler
    {
        private readonly IInboxService _inbox;
        private readonly IOutboxService _outbox;
        private readonly List<ExecutionJob> _jobs; // Simulação, trocar por persistência real

        public PaymentConfirmedHandler(IInboxService inbox, IOutboxService outbox, List<ExecutionJob> jobs)
        {
            _inbox = inbox;
            _outbox = outbox;
            _jobs = jobs;
        }

        public async Task HandleAsync(PaymentConfirmedEvent evt)
        {
            if (await _inbox.IsDuplicateAsync(evt.EventId))
                return;

            await _inbox.AddEventAsync(new InboxEvent
            {
                Id = Guid.NewGuid(),
                EventId = evt.EventId,
                EventType = "PaymentConfirmed",
                ReceivedAt = DateTime.UtcNow
            });

            if (_jobs.Exists(j => j.OsId == evt.OsId))
                return;

            var job = new ExecutionJob
            {
                Id = Guid.NewGuid(),
                OsId = evt.OsId,
                Status = ExecutionStatus.Queued,
                Attempt = 1,
                CreatedAt = DateTime.UtcNow,
                CorrelationId = evt.CorrelationId
            };
            _jobs.Add(job);

            await _outbox.AddEventAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "ExecutionStarted",
                Payload = $"{{\"OsId\":\"{evt.OsId}\",\"CorrelationId\":\"{evt.CorrelationId}\"}}",
                CreatedAt = DateTime.UtcNow,
                Published = false
            });
        }
    }
}
