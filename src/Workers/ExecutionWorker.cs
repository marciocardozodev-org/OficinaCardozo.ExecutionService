using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Outbox;

namespace OficinaCardozo.ExecutionService.Workers
{
    /// <summary>
    /// BackgroundService que processa transições de estado dos ExecutionJobs.
    /// Queued → Diagnosing → Repairing → Finished
    /// </summary>
    public class ExecutionWorker : BackgroundService
    {
        private readonly List<ExecutionJob> _jobs;
        private readonly IOutboxService _outbox;
        private readonly ILogger<ExecutionWorker> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        public ExecutionWorker(List<ExecutionJob> jobs, IOutboxService outbox, ILogger<ExecutionWorker> logger)
        {
            _jobs = jobs;
            _outbox = outbox;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExecutionWorker iniciado. Intervalo de processamento: {Interval}s", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var job in _jobs)
                    {
                        if (job.Status == ExecutionStatus.Queued)
                        {
                            await TransitionToAsync(job, ExecutionStatus.Diagnosing, stoppingToken);
                        }
                        else if (job.Status == ExecutionStatus.Diagnosing)
                        {
                            await TransitionToAsync(job, ExecutionStatus.Repairing, stoppingToken);
                        }
                        else if (job.Status == ExecutionStatus.Repairing)
                        {
                            await TransitionToAsync(job, ExecutionStatus.Finished, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no ExecutionWorker");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("ExecutionWorker foi interrompido");
        }

        private async Task TransitionToAsync(ExecutionJob job, ExecutionStatus newStatus, CancellationToken stoppingToken)
        {
            try
            {
                job.Status = newStatus;
                job.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Transição de estado: OS {OsId} → {NewStatus}",
                    job.CorrelationId, job.OsId, newStatus);

                // Se terminou, publicar ExecutionFinished
                if (newStatus == ExecutionStatus.Finished)
                {
                    job.FinishedAt = DateTime.UtcNow;

                    var finishedPayload = JsonSerializer.Serialize(new
                    {
                        OsId = job.OsId,
                        CorrelationId = job.CorrelationId,
                        JobId = job.Id,
                        Status = "Finished",
                        FinishedAt = job.FinishedAt,
                        DurationSeconds = (job.FinishedAt - job.CreatedAt)?.TotalSeconds
                    });

                    await _outbox.AddEventAsync(new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        EventType = "ExecutionFinished",
                        Payload = finishedPayload,
                        CreatedAt = DateTime.UtcNow,
                        Published = false
                    });

                    _logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] Evento ExecutionFinished enfileirado no Outbox",
                        job.CorrelationId);
                }
                else
                {
                    // Publicar ExecutionProgressed para estados intermediários
                    var progressPayload = JsonSerializer.Serialize(new
                    {
                        OsId = job.OsId,
                        CorrelationId = job.CorrelationId,
                        JobId = job.Id,
                        Status = newStatus.ToString(),
                        UpdatedAt = job.UpdatedAt
                    });

                    await _outbox.AddEventAsync(new OutboxEvent
                    {
                        Id = Guid.NewGuid(),
                        EventType = "ExecutionProgressed",
                        Payload = progressPayload,
                        CreatedAt = DateTime.UtcNow,
                        Published = false
                    });

                    _logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] Evento ExecutionProgressed enfileirado no Outbox",
                        job.CorrelationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CorrelationId: {CorrelationId}] Erro ao transicionar job {JobId} para {NewStatus}",
                    job.CorrelationId, job.Id, newStatus);
                job.Status = ExecutionStatus.Failed;
                job.LastError = ex.Message;
                job.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
