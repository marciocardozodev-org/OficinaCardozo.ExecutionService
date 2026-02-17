using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OficinaCardozo.ExecutionService.Inbox;

namespace OficinaCardozo.ExecutionService.Inbox
{
    public interface IInboxService
    {
        Task<bool> IsDuplicateAsync(string eventId);
        Task AddEventAsync(InboxEvent evt);
    }

    public class InboxService : IInboxService
    {
        private readonly HashSet<string> _eventIds = new(); // Simulação, trocar por persistência real

        public Task<bool> IsDuplicateAsync(string eventId)
        {
            return Task.FromResult(_eventIds.Contains(eventId));
        }

        public Task AddEventAsync(InboxEvent evt)
        {
            _eventIds.Add(evt.EventId);
            return Task.CompletedTask;
        }
    }
}
