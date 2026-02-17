using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Outbox;

namespace OficinaCardozo.ExecutionService.Workers
{
    public class ExecutionWorker : BackgroundService
    {
        private readonly List<ExecutionJob> _jobs;
        private readonly IOutboxService _outbox;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

        public ExecutionWorker(List<ExecutionJob> jobs, IOutboxService outbox)
        {
            _jobs = jobs;
            _outbox = outbox;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var job in _jobs)
                {
                    if (job.Status == ExecutionStatus.Queued)
                    {
                        job.Status = ExecutionStatus.Diagnosing;
                        job.UpdatedAt = DateTime.UtcNow;
                        await _outbox.AddEventAsync(new OutboxEvent
                        {
                            Id = Guid.NewGuid(),
                            EventType = "ExecutionProgressed",
                            Payload = $"{{\"OsId\":\"{job.OsId}\",\"Status\":\"Diagnosing\",\"CorrelationId\":\"{job.CorrelationId}\"}}",
                            CreatedAt = DateTime.UtcNow,
                            Published = false
                        });
                    }
                    else if (job.Status == ExecutionStatus.Diagnosing)
                    {
                        job.Status = ExecutionStatus.Repairing;
                        job.UpdatedAt = DateTime.UtcNow;
                        await _outbox.AddEventAsync(new OutboxEvent
                        {
                            Id = Guid.NewGuid(),
                            EventType = "ExecutionProgressed",
                            Payload = $"{{\"OsId\":\"{job.OsId}\",\"Status\":\"Repairing\",\"CorrelationId\":\"{job.CorrelationId}\"}}",
                            CreatedAt = DateTime.UtcNow,
                            Published = false
                        });
                    }
                    else if (job.Status == ExecutionStatus.Repairing)
                    {
                        job.Status = ExecutionStatus.Finished;
                        job.FinishedAt = DateTime.UtcNow;
                        await _outbox.AddEventAsync(new OutboxEvent
                        {
                            Id = Guid.NewGuid(),
                            EventType = "ExecutionFinished",
                            Payload = $"{{\"OsId\":\"{job.OsId}\",\"CorrelationId\":\"{job.CorrelationId}\"}}",
                            CreatedAt = DateTime.UtcNow,
                            Published = false
                        });
                    }
                }
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
