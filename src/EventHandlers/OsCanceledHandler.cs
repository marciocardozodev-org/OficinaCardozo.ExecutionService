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
    public class OsCanceledEvent
    {
        public string EventId { get; set; }
        public string OsId { get; set; }
        public string Reason { get; set; }
        public string CorrelationId { get; set; }
    }

    public class OsCanceledHandler
    {
        private readonly IInboxService _inbox;
        private readonly IOutboxService _outbox;
        private readonly ExecutionDbContext _context;
        private readonly ILogger<OsCanceledHandler> _logger;

        public OsCanceledHandler(IInboxService inbox, IOutboxService outbox, ExecutionDbContext context, ILogger<OsCanceledHandler> logger)
        {
            _inbox = inbox;
            _outbox = outbox;
            _context = context;
            _logger = logger;
        }

        public async Task HandleAsync(OsCanceledEvent evt)
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Iniciando handler OsCanceled para OS {OsId}, Motivo: {Reason}",
                evt.CorrelationId, evt.OsId, evt.Reason);

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
                EventType = "OsCanceled",
                ReceivedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Evento registrado no Inbox",
                evt.CorrelationId);

            // Buscar job ativo para esse OsId
            var job = await _context.ExecutionJobs
                .FirstOrDefaultAsync(j => j.OsId == evt.OsId 
                    && j.Status != ExecutionStatus.Finished 
                    && j.Status != ExecutionStatus.Failed 
                    && j.Status != ExecutionStatus.Canceled);
            
            if (job == null)
            {
                _logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] Nenhum job ativo encontrado para OS {OsId}. Ignorando.",
                    evt.CorrelationId, evt.OsId);
                return;
            }

            // Cancelar job
            job.Status = ExecutionStatus.Canceled;
            job.UpdatedAt = DateTime.UtcNow;
            job.LastError = $"Cancelado: {evt.Reason}";
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ExecutionJob {JobId} foi cancelado",
                evt.CorrelationId, job.Id);

            // Publicar ExecutionCanceled via Outbox
            var executionCanceledPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                OsId = evt.OsId,
                CorrelationId = evt.CorrelationId,
                JobId = job.Id,
                Status = "Canceled",
                Reason = evt.Reason,
                CanceledAt = job.UpdatedAt
            });

            await _outbox.AddEventAsync(new OutboxEvent
            {
                Id = Guid.NewGuid(),
                EventType = "ExecutionCanceled",
                Payload = executionCanceledPayload,
                CreatedAt = DateTime.UtcNow,
                Published = false
            });

            _logger.LogInformation(
                "ExecutionService gravou evento ExecutionCanceled no outbox | CorrelationId: {CorrelationId} | EventType: ExecutionCanceled | EntityId: {OsId} | JobId: {JobId} | Status: Canceled",
                evt.CorrelationId, evt.OsId, job.Id);
        }
    }
}
