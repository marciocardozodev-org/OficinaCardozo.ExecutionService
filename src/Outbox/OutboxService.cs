using System;
using System.Threading.Tasks;
using OficinaCardozo.ExecutionService.Outbox;
using System.Collections.Generic;

namespace OficinaCardozo.ExecutionService.Outbox
{
    public interface IOutboxService
    {
        Task AddEventAsync(OutboxEvent evt);
        Task MarkAsPublishedAsync(Guid id);
        Task<List<OutboxEvent>> GetUnpublishedEventsAsync();
    }

    public class OutboxService : IOutboxService
    {
        private readonly List<OutboxEvent> _events = new(); // Simulação, trocar por persistência real

        public Task AddEventAsync(OutboxEvent evt)
        {
            _events.Add(evt);
            return Task.CompletedTask;
        }

        public Task MarkAsPublishedAsync(Guid id)
        {
            var evt = _events.Find(e => e.Id == id);
            if (evt != null)
            {
                evt.Published = true;
                evt.PublishedAt = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task<List<OutboxEvent>> GetUnpublishedEventsAsync()
        {
            return Task.FromResult(_events.FindAll(e => !e.Published));
        }
    }
}
