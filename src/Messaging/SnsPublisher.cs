using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
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
        private readonly IOutboxService _outbox;
        private readonly ILogger<SnsPublisher> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

        public SnsPublisher(
            IAmazonSimpleNotificationService snsClient,
            MessagingConfig config,
            IOutboxService outbox,
            ILogger<SnsPublisher> logger)
        {
            _snsClient = snsClient;
            _config = config;
            _outbox = outbox;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SNS Publisher iniciado. Tópico: {Topic}", _config.OutputTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var unpublishedEvents = await _outbox.GetUnpublishedEventsAsync();

                    if (unpublishedEvents.Count > 0)
                    {
                        _logger.LogInformation("Publicando {Count} eventos não publicados", unpublishedEvents.Count);

                        foreach (var evt in unpublishedEvents)
                        {
                            await PublishEventAsync(evt, stoppingToken);
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

        private async Task PublishEventAsync(OutboxEvent evt, CancellationToken stoppingToken)
        {
            try
            {
                // Extrai CorrelationId do payload para logs
                string correlationId = "unknown";
                try
                {
                    var payload = JsonSerializer.Deserialize<JsonElement>(evt.Payload);
                    if (payload.TryGetProperty("CorrelationId", out var corId))
                    {
                        correlationId = corId.GetString();
                    }
                }
                catch { /* ignore */ }

                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Publicando evento {EventType} no SNS",
                    correlationId, evt.EventType);

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

                // Marcar como publicado
                await _outbox.MarkAsPublishedAsync(evt.Id);

                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Evento {EventType} publicado com MessageId {MessageId}",
                    correlationId, evt.EventType, response.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao publicar evento {EventType} com Id {EventId}",
                    evt.EventType, evt.Id);
            }
        }
    }
}
