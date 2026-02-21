using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OFICINACARDOZO.EXECUTIONSERVICE;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;

namespace OficinaCardozo.ExecutionService.EventHandlers
{
    public class PaymentConfirmedEvent
    {
        public string EventId { get; set; }
        public string OsId { get; set; }
        public string PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string CorrelationId { get; set; }
    }

    public class PaymentConfirmedHandler
    {
        private readonly IInboxService _inbox;
        private readonly IOutboxService _outbox;
        private readonly ExecutionDbContext _context;
        private readonly ILogger<PaymentConfirmedHandler> _logger;

        public PaymentConfirmedHandler(IInboxService inbox, IOutboxService outbox, ExecutionDbContext context, ILogger<PaymentConfirmedHandler> logger)
        {
            _inbox = inbox;
            _outbox = outbox;
            _context = context;
            _logger = logger;
        }

        public async Task HandleAsync(PaymentConfirmedEvent evt)
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Iniciando handler PaymentConfirmed para OS {OsId}, PaymentId {PaymentId}",
                evt.CorrelationId, evt.OsId, evt.PaymentId);

            // Verificar duplicata via Inbox
            if (await _inbox.IsDuplicateAsync(evt.EventId))
            {
                _logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] Evento duplicado detectado (EventId: {EventId}). Ignorando.",
                    evt.CorrelationId, evt.EventId);
                return;
            }

            // Registrar no Inbox
            await _inbox.AddEventAsync(new InboxEvent
            {
                Id = Guid.NewGuid(),
                EventId = evt.EventId,
                EventType = "PaymentConfirmed",
                ReceivedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Evento registrado no Inbox",
                evt.CorrelationId);

            // Verificar se já existe job para esse OsId
            var existingJob = await _context.ExecutionJobs
                .FirstOrDefaultAsync(j => j.OsId == evt.OsId);
            
            if (existingJob != null)
            {
                _logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] Job já existe para OS {OsId}. Ignorando.",
                    evt.CorrelationId, evt.OsId);
                return;
            }

            // Criar novo ExecutionJob
            var job = new ExecutionJob
            {
                Id = Guid.NewGuid(),
                OsId = evt.OsId,
                Status = ExecutionStatus.Queued,
                Attempt = 1,
                CreatedAt = DateTime.UtcNow,
                CorrelationId = evt.CorrelationId
            };
            await _context.ExecutionJobs.AddAsync(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ExecutionJob criado com Id {JobId}, Status: Queued",
                evt.CorrelationId, job.Id);

            // Publicar ExecutionStarted via Outbox
            var executionStartedPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                OsId = evt.OsId,
                CorrelationId = evt.CorrelationId,
                JobId = job.Id,
                Status = "Queued",
                CreatedAt = job.CreatedAt
            });

            await _outbox.AddEventAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "ExecutionStarted",
                Payload = executionStartedPayload,
                CreatedAt = DateTime.UtcNow,
                Published = false
            });

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Evento ExecutionStarted enfileirado no Outbox",
                evt.CorrelationId);
        }
    }
}
