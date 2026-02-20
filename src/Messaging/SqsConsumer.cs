using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OficinaCardozo.ExecutionService.EventHandlers;
using OficinaCardozo.ExecutionService.Messaging;

namespace OficinaCardozo.ExecutionService.Messaging
{
    /// <summary>
    /// Consumidor SQS que ouve a fila billing-events e dispara handlers de PaymentConfirmed.
    /// </summary>
    public class SqsConsumer : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly MessagingConfig _config;
        private readonly PaymentConfirmedHandler _paymentHandler;
        private readonly OsCanceledHandler _osCanceledHandler;
        private readonly ILogger<SqsConsumer> _logger;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);

        public SqsConsumer(
            IAmazonSQS sqsClient,
            MessagingConfig config,
            PaymentConfirmedHandler paymentHandler,
            OsCanceledHandler osCanceledHandler,
            ILogger<SqsConsumer> logger)
        {
            _sqsClient = sqsClient;
            _config = config;
            _paymentHandler = paymentHandler;
            _osCanceledHandler = osCanceledHandler;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SQS Consumer iniciado. Ouvindo fila: {Queue}", _config.InputQueue);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _sqsClient.ReceiveMessageAsync(
                        new ReceiveMessageRequest
                        {
                            QueueUrl = _config.InputQueue,
                            MaxNumberOfMessages = 10,
                            WaitTimeSeconds = 10,
                            MessageAttributeNames = new List<string> { "All" }
                        },
                        stoppingToken);

                    if (response.Messages.Count > 0)
                    {
                        _logger.LogInformation("Recebidas {Count} mensagens da fila", response.Messages.Count);

                        foreach (var message in response.Messages)
                        {
                            var processed = await ProcessMessageAsync(message, stoppingToken);

                            if (processed)
                            {
                                // Deletar mensagem após processamento bem-sucedido
                                await _sqsClient.DeleteMessageAsync(
                                    _config.InputQueue,
                                    message.ReceiptHandle,
                                    stoppingToken);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Mensagem nao processada. Mantendo na fila para reprocesso. MessageId: {MessageId}",
                                    message.MessageId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao consumir mensagens da SQS");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

        private async Task<bool> ProcessMessageAsync(Message message, CancellationToken stoppingToken)
        {
            try
            {
                var bodyPreview = GetBodyPreview(message.Body, 500);
                var attributesSummary = GetMessageAttributesSummary(message.MessageAttributes);

                _logger.LogInformation(
                    "Recebida mensagem SQS. MessageId: {MessageId} Attributes: {Attributes} BodyPreview: {BodyPreview}",
                    message.MessageId,
                    attributesSummary,
                    bodyPreview);

                JsonElement payload;
                string eventId;
                string correlationId;
                string eventType;

                // Com RawMessageDelivery=true, a mensagem vem diretamente como JSON (sem envelope SNS)
                // Tentar detectar o formato
                var snsMessage = JsonSerializer.Deserialize<SnsMessageEnvelope>(message.Body);
                
                if (snsMessage != null && !string.IsNullOrEmpty(snsMessage.Message))
                {
                    // Formato: Envelope SNS (RawMessageDelivery=false)
                    _logger.LogDebug("Mensagem com envelope SNS detectada");
                    payload = JsonSerializer.Deserialize<JsonElement>(snsMessage.Message);
                    
                    eventId = snsMessage.MessageAttributes.ContainsKey("EventId")
                        ? snsMessage.MessageAttributes["EventId"].Value
                        : Guid.NewGuid().ToString();

                    correlationId = snsMessage.MessageAttributes.ContainsKey("CorrelationId")
                        ? snsMessage.MessageAttributes["CorrelationId"].Value
                        : Guid.NewGuid().ToString();

                    eventType = snsMessage.MessageAttributes.ContainsKey("EventType")
                        ? snsMessage.MessageAttributes["EventType"].Value
                        : snsMessage.TopicArn?.Split(':').Last() ?? "Unknown";
                }
                else
                {
                    // Formato: Raw Message (RawMessageDelivery=true)
                    _logger.LogDebug("Mensagem raw (sem envelope SNS) detectada");
                    payload = JsonSerializer.Deserialize<JsonElement>(message.Body);
                    
                    // Com RawMessageDelivery, os MessageAttributes vêm do SQS Message
                    eventId = message.MessageAttributes.ContainsKey("EventId")
                        ? message.MessageAttributes["EventId"].StringValue
                        : Guid.NewGuid().ToString();

                    correlationId = message.MessageAttributes.ContainsKey("CorrelationId")
                        ? message.MessageAttributes["CorrelationId"].StringValue
                        : Guid.NewGuid().ToString();

                    eventType = message.MessageAttributes.ContainsKey("EventType")
                        ? message.MessageAttributes["EventType"].StringValue
                        : "Unknown";
                }

                _logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] Processando evento {EventType} com EventId {EventId}",
                    correlationId, eventType, eventId);

                // Rotear para o handler apropriado
                if (eventType == "PaymentConfirmed")
                {
                    var osId = payload.GetProperty("OsId").GetString();
                    var paymentId = payload.GetProperty("PaymentId").GetString();
                    var amount = payload.GetProperty("Amount").GetDecimal();
                    var status = payload.GetProperty("Status").GetString();

                    var evt = new PaymentConfirmedEvent
                    {
                        EventId = eventId,
                        OsId = osId,
                        PaymentId = paymentId,
                        Amount = amount,
                        Status = status,
                        CorrelationId = correlationId
                    };

                    await _paymentHandler.HandleAsync(evt);
                    _logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] PaymentConfirmed processado para OS {OsId}",
                        correlationId, osId);
                }
                else if (eventType == "OsCanceled")
                {
                    var osId = payload.GetProperty("OsId").GetString();
                    var reason = payload.GetProperty("Reason").GetString();

                    var evt = new OsCanceledEvent
                    {
                        EventId = eventId,
                        OsId = osId,
                        Reason = reason,
                        CorrelationId = correlationId
                    };

                    await _osCanceledHandler.HandleAsync(evt);
                    _logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] OsCanceled processado para OS {OsId}",
                        correlationId, osId);
                }
                else
                {
                    _logger.LogWarning(
                        "[CorrelationId: {CorrelationId}] Tipo de evento desconhecido: {EventType}",
                        correlationId, eventType);
                }

                return true;
            }
            catch (Exception ex)
            {
                var bodyPreview = GetBodyPreview(message.Body, 500);
                var attributesSummary = GetMessageAttributesSummary(message.MessageAttributes);

                _logger.LogError(
                    ex,
                    "Erro ao processar mensagem SQS. MessageId: {MessageId} Attributes: {Attributes} BodyPreview: {BodyPreview}",
                    message.MessageId,
                    attributesSummary,
                    bodyPreview);

                return false;
            }
        }

        private static string GetBodyPreview(string body, int maxLength)
        {
            if (string.IsNullOrEmpty(body))
            {
                return "<empty>";
            }

            return body.Length <= maxLength
                ? body
                : body.Substring(0, maxLength) + "...";
        }

        private static string GetMessageAttributesSummary(Dictionary<string, Amazon.SQS.Model.MessageAttributeValue> attributes)
        {
            if (attributes == null || attributes.Count == 0)
            {
                return "<none>";
            }

            var parts = new List<string>();
            foreach (var entry in attributes)
            {
                var value = entry.Value?.StringValue ?? entry.Value?.BinaryValue?.ToString() ?? "<null>";
                parts.Add($"{entry.Key}={value}");
            }

            return string.Join(", ", parts);
        }

        // SNS envelope padrão ao ser entregue via SQS
        private class SnsMessageEnvelope
        {
            public string Message { get; set; }
            public string MessageId { get; set; }
            public string TopicArn { get; set; }
            public Dictionary<string, MessageAttribute> MessageAttributes { get; set; }

            public SnsMessageEnvelope()
            {
                MessageAttributes = new();
            }
        }

        private class MessageAttribute
        {
            public string Type { get; set; }
            public string Value { get; set; }
        }
    }
}
