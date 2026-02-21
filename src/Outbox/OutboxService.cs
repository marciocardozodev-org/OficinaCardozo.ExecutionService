using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.EXECUTIONSERVICE;
using OficinaCardozo.ExecutionService.Outbox;

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
        private readonly ExecutionDbContext _context;

        public OutboxService(ExecutionDbContext context)
        {
            _context = context;
        }

        public async Task AddEventAsync(OutboxEvent evt)
        {
            await _context.OutboxEvents.AddAsync(evt);
            await _context.SaveChangesAsync();
        }

        public async Task MarkAsPublishedAsync(Guid id)
        {
            var evt = await _context.OutboxEvents.FindAsync(id);
            if (evt != null)
            {
                evt.Published = true;
                evt.PublishedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<OutboxEvent>> GetUnpublishedEventsAsync()
        {
            return await _context.OutboxEvents
                .Where(e => !e.Published)
                .ToListAsync();
        }
    }
}
