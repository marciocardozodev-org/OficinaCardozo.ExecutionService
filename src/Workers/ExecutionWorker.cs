using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OFICINACARDOZO.EXECUTIONSERVICE;
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
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExecutionWorker> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        public ExecutionWorker(IServiceScopeFactory scopeFactory, ILogger<ExecutionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExecutionWorker iniciado. Intervalo de processamento: {Interval}s", _interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
                    var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();

                    // Buscar jobs que precisam ser processados (não finalizados, não falhados, não cancelados)
                    var jobs = await context.ExecutionJobs
                        .Where(j => j.Status == ExecutionStatus.Queued 
                                 || j.Status == ExecutionStatus.Diagnosing 
                                 || j.Status == ExecutionStatus.Repairing)
                        .ToListAsync(stoppingToken);

                    foreach (var job in jobs)
                    {
                        if (job.Status == ExecutionStatus.Queued)
                        {
                            await TransitionToAsync(job, ExecutionStatus.Diagnosing, outbox, stoppingToken);
                        }
                        else if (job.Status == ExecutionStatus.Diagnosing)
                        {
                            await TransitionToAsync(job, ExecutionStatus.Repairing, outbox, stoppingToken);
                        }
                        else if (job.Status == ExecutionStatus.Repairing)
                        {
                            await TransitionToAsync(job, ExecutionStatus.Finished, outbox, stoppingToken);
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro no ExecutionWorker");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("ExecutionWorker foi interrompido");
        }

        private async Task TransitionToAsync(ExecutionJob job, ExecutionStatus newStatus, IOutboxService outbox, CancellationToken stoppingToken)
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

                    await outbox.AddEventAsync(new OutboxEvent
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

                    await outbox.AddEventAsync(new OutboxEvent
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
