using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.EXECUTIONSERVICE;
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
        private readonly ExecutionDbContext _context;

        public InboxService(ExecutionDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsDuplicateAsync(string eventId)
        {
            return await _context.InboxEvents.AnyAsync(e => e.EventId == eventId);
        }

        public async Task AddEventAsync(InboxEvent evt)
        {
            await _context.InboxEvents.AddAsync(evt);
            await _context.SaveChangesAsync();
        }
    }
}
