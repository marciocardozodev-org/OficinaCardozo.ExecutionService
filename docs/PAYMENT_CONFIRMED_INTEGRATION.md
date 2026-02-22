# PaymentConfirmed - Integração via SNS

## 📋 Contexto

O evento `PaymentConfirmed` deve ser publicado pelo **BillingService** no SNS topic `payment-confirmed` para garantir desacoplamento entre microserviços.

## 🎯 Arquitetura

```
BillingService
    ↓ Publica evento
SNS Topic: payment-confirmed
    ↓ Fan-out automático
    ├─→ SQS: billing-events (ExecutionService)
    └─→ SQS: execution-events (outros consumidores)
```

## ⚙️ Configuração AWS

### SNS Topic
```
arn:aws:sns:sa-east-1:953082827427:payment-confirmed
```

### Subscriptions
- **billing-events**: `RawMessageDelivery=true`
- **execution-events**: `RawMessageDelivery=true`

### Queue Policies
Ambas as filas já estão configuradas para aceitar mensagens do topic `payment-confirmed`.

## 📤 Como Publicar (BillingService)

### Exemplo em C# (.NET)

```csharp
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

public class SnsPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private const string TopicArn = "arn:aws:sns:sa-east-1:953082827427:payment-confirmed";

    public async Task PublishPaymentConfirmedAsync(PaymentConfirmedEvent evt, string correlationId)
    {
        var request = new PublishRequest
        {
            TopicArn = TopicArn,
            Message = JsonSerializer.Serialize(evt), // Sem envelope!
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new MessageAttributeValue 
                { 
                    DataType = "String", 
                    StringValue = "PaymentConfirmed" 
                },
                ["CorrelationId"] = new MessageAttributeValue 
                { 
                    DataType = "String", 
                    StringValue = correlationId 
                },
                ["Timestamp"] = new MessageAttributeValue 
                { 
                    DataType = "String", 
                    StringValue = DateTime.UtcNow.ToString("o") 
                }
            }
        };

        var response = await _sns.PublishAsync(request);
        
        // Log MessageId para troubleshooting
        _logger.LogInformation(
            "[CorrelationId: {CorrelationId}] PaymentConfirmed publicado. MessageId: {MessageId}",
            correlationId, response.MessageId);
    }
}
```

### Payload do Evento

```json
{
  "OsId": "00000000-0000-0000-0000-000000000021",
  "PaymentId": "00000000-0000-0000-0200-000000000000",
  "Status": 1,
  "Amount": 100.00,
  "ProviderPaymentId": "MP-00000000000000000000000000000021-5-639072360298482092"
}
```

**IMPORTANTE**: 
- ✅ **Publicar JSON puro** (sem envelope SNS)
- ✅ **RawMessageDelivery=true** garante que o JSON chega direto nas filas
- ✅ **MessageAttributes** são obrigatórios: `EventType`, `CorrelationId`

## 🔍 Validação

### Via AWS CLI

```bash
# Publicar teste
aws sns publish \
  --topic-arn "arn:aws:sns:sa-east-1:953082827427:payment-confirmed" \
  --message '{"OsId":"test","PaymentId":"test","Status":1,"Amount":100.00}' \
  --message-attributes \
    'EventType={DataType=String,StringValue=PaymentConfirmed}' \
    'CorrelationId={DataType=String,StringValue=test-123}' \
  --region sa-east-1

# Verificar entrega na billing-events
aws sqs receive-message \
  --queue-url "https://sa-east-1.queue.amazonaws.com/953082827427/billing-events" \
  --max-number-of-messages 1 \
  --message-attribute-names All \
  --region sa-east-1
```

### Logs ExecutionService

```
[CorrelationId: <guid>] PaymentConfirmed processado para OS <osId>
[CorrelationId: <guid>] ExecutionJob criado/atualizado para OS <osId>
```

## ❌ Anti-Patterns

### ❌ Publicar direto na fila
```csharp
// ERRADO - cria acoplamento forte
await _sqs.SendMessageAsync("billing-events", message);
```

### ❌ Usar envelope SNS manual
```csharp
// ERRADO - RawMessageDelivery já está habilitado
var envelope = new { Type = "Notification", Message = json };
```

### ❌ Omitir MessageAttributes
```csharp
// ERRADO - ExecutionService precisa de EventType e CorrelationId
await _sns.PublishAsync(new PublishRequest 
{ 
    TopicArn = topicArn, 
    Message = json 
    // ❌ Faltam MessageAttributes
});
```

## 🔧 Troubleshooting

### Evento não chega no ExecutionService

1. **Verificar SNS Metrics**
```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/SNS \
  --metric-name NumberOfNotificationsFailed \
  --dimensions Name=TopicName,Value=payment-confirmed \
  --start-time $(date -u -d '10 minutes ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 60 \
  --statistics Sum \
  --region sa-east-1
```

2. **Verificar Queue Policy**
```bash
aws sqs get-queue-attributes \
  --queue-url "https://sa-east-1.queue.amazonaws.com/953082827427/billing-events" \
  --attribute-names Policy \
  --region sa-east-1 \
  --query 'Attributes.Policy' \
  --output text | jq '.Statement[0].Condition.ArnEquals."aws:SourceArn"' | grep payment-confirmed
```

3. **Verificar mensagens na DLQ**
```bash
aws sqs receive-message \
  --queue-url "https://sa-east-1.queue.amazonaws.com/953082827427/billing-events-dlq" \
  --max-number-of-messages 10 \
  --region sa-east-1
```

## 📊 Fluxo E2E Esperado

```
1. BillingService: Webhook Mercado Pago confirma pagamento
   └─> Atualiza Payment.Status = Approved

2. BillingService: Publica PaymentConfirmed no SNS
   └─> SNS entrega para billing-events e execution-events

3. ExecutionService: Consome da billing-events
   ├─> Valida idempotência (evita duplicação)
   ├─> Cria/Atualiza ExecutionJob
   └─> Inicia workflow: Queued → Diagnosing → Repairing → Finished

4. ExecutionService: Publica ExecutionStarted, ExecutionFinished
   └─> OSService consome e atualiza status da OS
```

## 🚀 Benefícios da Arquitetura SNS

✅ **Desacoplamento**: BillingService não conhece ExecutionService  
✅ **Escalabilidade**: Novos consumidores sem mudança no publisher  
✅ **Rastreabilidade**: CorrelationId persiste em todos os logs  
✅ **Resiliência**: DLQ captura falhas de processamento  
✅ **Fan-out**: Um publish, múltiplas entregas automáticas

## 📝 Checklist de Implementação

- [ ] Adicionar `Amazon.SimpleNotificationService` NuGet package
- [ ] Configurar IAM role com permissão `sns:Publish` no topic
- [ ] Implementar publisher com MessageAttributes obrigatórios
- [ ] Adicionar logs com CorrelationId e SNS MessageId
- [ ] Testar em dev/staging antes de produção
- [ ] Validar idempotência (publicar 2x o mesmo evento)
- [ ] Monitorar métricas SNS (NumberOfMessagesPublished, NumberOfNotificationsFailed)

---

**Última atualização**: 2026-02-21  
**Responsável**: ExecutionService Team
