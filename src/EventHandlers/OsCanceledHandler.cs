using System;
using System.Threading.Tasks;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;

namespace OficinaCardozo.ExecutionService.EventHandlers
{
    public class OsCanceledEvent
    {
        public string EventId { get; set; }
        public string OsId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class OsCanceledHandler
    {
        private readonly IInboxService _inbox;
        private readonly IOutboxService _outbox;
        private readonly List<ExecutionJob> _jobs; // Simulação, trocar por persistência real

        public OsCanceledHandler(IInboxService inbox, IOutboxService outbox, List<ExecutionJob> jobs)
        {
            _inbox = inbox;
            _outbox = outbox;
            _jobs = jobs;
        }

        public async Task HandleAsync(OsCanceledEvent evt)
        {
            if (await _inbox.IsDuplicateAsync(evt.EventId))
                return;

            await _inbox.AddEventAsync(new InboxEvent
            {
                Id = Guid.NewGuid(),
                EventId = evt.EventId,
                EventType = "OsCanceled",
                ReceivedAt = DateTime.UtcNow
            });

            var job = _jobs.Find(j => j.OsId == evt.OsId && j.Status != ExecutionStatus.Finished && j.Status != ExecutionStatus.Failed);
            if (job == null)
                return;

            job.Status = ExecutionStatus.Canceled;
            job.UpdatedAt = DateTime.UtcNow;

            await _outbox.AddEventAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "ExecutionCanceled",
                Payload = $"{{\"OsId\":\"{evt.OsId}\",\"CorrelationId\":\"{evt.CorrelationId}\"}}",
                CreatedAt = DateTime.UtcNow,
                Published = false
            });
        }
    }
}
