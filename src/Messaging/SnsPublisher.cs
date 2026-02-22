using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OficinaCardozo.ExecutionService.Outbox;

namespace OficinaCardozo.ExecutionService.Messaging
{
    /// <summary>
    /// BackgroundService que publica eventos Outbox não publicados no SNS.
    /// </summary>
    public class SnsPublisher : BackgroundService
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly MessagingConfig _config;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SnsPublisher> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

        public SnsPublisher(
            IAmazonSimpleNotificationService snsClient,
            MessagingConfig config,
            IServiceScopeFactory scopeFactory,
            ILogger<SnsPublisher> logger)
        {
            _snsClient = snsClient;
            _config = config;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SNS Publisher iniciado. Tópico: {Topic}", _config.OutputTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();

                    var unpublishedEvents = await outbox.GetUnpublishedEventsAsync();

                    if (unpublishedEvents.Count > 0)
                    {
                        _logger.LogInformation("Publicando {Count} eventos não publicados", unpublishedEvents.Count);

                        foreach (var evt in unpublishedEvents)
                        {
                            await PublishEventAsync(evt, outbox, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao publicar eventos no SNS");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

        private async Task PublishEventAsync(OutboxEvent evt, IOutboxService outbox, CancellationToken stoppingToken)
        {
            try
            {
                // Extrai CorrelationId do payload para logs
                string correlationId = "unknown";
                string entityId = "unknown";
                try
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload);
                    if (payload.TryGetProperty("CorrelationId", out var corId))
                    {
                        correlationId = corId.GetString() ?? "unknown";
                    }
                    if (payload.TryGetProperty("OsId", out var osId))
                    {
                        entityId = osId.GetString() ?? "unknown";
                    }
                    else if (payload.TryGetProperty("JobId", out var jobId))
                    {
                        entityId = jobId.GetString() ?? "unknown";
                    }
                }
                catch { /* ignore parse errors */ }

                var request = new PublishRequest
                {
                    TopicArn = _config.OutputTopic,
                    Message = evt.Payload,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                    {
                        {
                            "EventType",
                            new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = evt.EventType
                            }
                        },
                        {
                            "EventId",
                            new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = evt.Id.ToString()
                            }
                        },
                        {
                            "CorrelationId",
                            new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = correlationId
                            }
                        },
                        {
                            "PublishedAt",
                            new MessageAttributeValue
                            {
                                DataType = "String",
                                StringValue = DateTime.UtcNow.ToString("o")
                            }
                        }
                    }
                };

                var response = await _snsClient.PublishAsync(request, stoppingToken);

                // Marcar como publicado ANTES do log de sucesso
                await outbox.MarkAsPublishedAsync(evt.Id);

                // Log de negócio: sucesso após confirmação real da AWS
                _logger.LogInformation(
                    "ExecutionService gerou evento {EventType} | CorrelationId: {CorrelationId} | EventId: {EventId} | EntityId: {EntityId} | MessageId: {MessageId} | Status: Published",
                    evt.EventType, correlationId, evt.Id, entityId, response.MessageId);
            }
            catch (Exception ex)
            {
                // Extrai CorrelationId para log de erro
                string correlationId = "unknown";
                string entityId = "unknown";
                try
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload);
                    if (payload.TryGetProperty("CorrelationId", out var corId))
                    {
                        correlationId = corId.GetString() ?? "unknown";
                    }
                    if (payload.TryGetProperty("OsId", out var osId))
                    {
                        entityId = osId.GetString() ?? "unknown";
                    }
                }
                catch { /* ignore */ }

                _logger.LogError(ex,
                    "Erro ao publicar evento {EventType} | CorrelationId: {CorrelationId} | EventId: {EventId} | EntityId: {EntityId}",
                    evt.EventType, correlationId, evt.Id, entityId);
            }
        }
    }
}
