# 🎯 Action Items - BillingService Team

## 📌 Contexto

O ExecutionService está consumindo eventos de **billing-events**, mas o BillingService está publicando `PaymentConfirmed` diretamente na fila **execution-events**. Isso viola os princípios de arquitetura de microserviços orientados a eventos.

## ⚠️ Problema Atual

```
BillingService 
    ↓ (ERRADO)
SQS: execution-events
    ↓
ExecutionService ❌ NÃO CONSOME (ouve billing-events)
```

## ✅ Solução Arquitetural

```
BillingService 
    ↓ Publica no SNS
SNS Topic: payment-confirmed
    ↓ Fan-out automático
    ├─→ SQS: billing-events → ExecutionService ✅
    └─→ SQS: execution-events → (outros serviços)
```

## 🔧 Mudanças Necessárias

### 1. Trocar `SQS.SendMessage` por `SNS.Publish`

#### ❌ Antes (código atual)
```csharp
// Publicação direta na fila (acoplamento forte)
await _sqs.SendMessageAsync(new SendMessageRequest
{
    QueueUrl = "https://sa-east-1.queue.amazonaws.com/953082827427/execution-events",
    MessageBody = JsonSerializer.Serialize(paymentConfirmedEvent),
    MessageAttributes = attributes
});
```

#### ✅ Depois (desacoplado via SNS)
```csharp
// Publicação no topic SNS (desacoplamento)
await _sns.PublishAsync(new PublishRequest
{
    TopicArn = "arn:aws:sns:sa-east-1:953082827427:payment-confirmed",
    Message = JsonSerializer.Serialize(paymentConfirmedEvent),
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
        }
    }
});
```

### 2. Atualizar IAM Policy

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "sns:Publish",
      "Resource": "arn:aws:sns:sa-east-1:953082827427:payment-confirmed"
    }
  ]
}
```

### 3. Adicionar NuGet Package (se necessário)

```xml
<PackageReference Include="AWSSDK.SimpleNotificationService" Version="3.7.*" />
```

## 📋 Checklist

- [ ] **Código**: Trocar `_sqs.SendMessageAsync` por `_sns.PublishAsync`
- [ ] **Config**: Substituir `QUEUE_URL` por `TOPIC_ARN` na configuração
- [ ] **IAM**: Adicionar permissão `sns:Publish` no role do BillingService
- [ ] **Testes**: Validar em dev/staging publicando evento de teste
- [ ] **Logs**: Confirmar SNS MessageId nos logs
- [ ] **Monitoring**: Acompanhar métricas `NumberOfMessagesPublished` no CloudWatch

## 🧪 Como Testar

### 1. Publicar teste via AWS CLI
```bash
aws sns publish \
  --topic-arn "arn:aws:sns:sa-east-1:953082827427:payment-confirmed" \
  --message '{"OsId":"00000000-0000-0000-0000-000000000099","PaymentId":"test","Status":1,"Amount":999.99}' \
  --message-attributes \
    'EventType={DataType=String,StringValue=PaymentConfirmed}' \
    'CorrelationId={DataType=String,StringValue=test-correlation-id}' \
  --region sa-east-1
```

### 2. Verificar entrega no ExecutionService
```bash
# Logs do ExecutionService devem mostrar:
kubectl logs -l app=executionservice -n default --tail=50 | grep "PaymentConfirmed"

# Saída esperada:
# [CorrelationId: test-correlation-id] PaymentConfirmed processado para OS 00000000-0000-0000-0000-000000000099
# [CorrelationId: test-correlation-id] ExecutionJob criado para OS 00000000-0000-0000-0000-000000000099
```

### 3. Verificar métricas SNS
```bash
aws cloudwatch get-metric-statistics \
  --namespace AWS/SNS \
  --metric-name NumberOfMessagesPublished \
  --dimensions Name=TopicName,Value=payment-confirmed \
  --start-time $(date -u -d '5 minutes ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 60 \
  --statistics Sum \
  --region sa-east-1
```

## 📊 Casos de Uso Já Testados

### OS 19 (CorrelationId: 886bd83f-8dca-487a-848e-cd95f961f146)
- ✅ Consumido da execution-events (workaround temporário)
- ✅ Idempotência validada: "Job já existe para OS 19. Ignorando."

### OS 21 (CorrelationId: 9b99ce40-7ae8-4214-b522-dfe49f31eb1a)
- ⏳ **Aguardando** publicação via SNS
- 📍 Atualmente na execution-events (fila errada)

## 🎯 Benefícios da Mudança

| Aspecto | Antes (SQS direto) | Depois (SNS) |
|---------|-------------------|--------------|
| **Acoplamento** | ❌ Forte (conhece ExecutionService) | ✅ Fraco (evento de domínio) |
| **Escalabilidade** | ❌ Precisa conhecer todas as filas | ✅ Adiciona consumidores sem mudança |
| **Rastreabilidade** | ⚠️ Difícil rastrear fan-out | ✅ SNS MessageId + CloudWatch |
| **Resiliência** | ⚠️ Falha em uma fila afeta publicação | ✅ SNS garante entrega em todas |
| **Conformidade** | ❌ Fora do padrão arquitetural | ✅ Alinhado com fluxo E2E |

## 📞 Suporte

- **Documentação completa**: [PAYMENT_CONFIRMED_INTEGRATION.md](./PAYMENT_CONFIRMED_INTEGRATION.md)
- **Infraestrutura SNS**: Já configurada e testada pelo ExecutionService Team
- **Dúvidas**: Contatar ExecutionService Team

## ⏰ Timeline Sugerido

- **Semana 1**: Implementação + testes em dev
- **Semana 2**: Deploy em staging + validação E2E
- **Semana 3**: Rollout produção com monitoramento ativo

---

**Prioridade**: 🔴 **ALTA** - Bloqueando fluxo E2E em produção  
**Criado**: 2026-02-21  
**Owner**: BillingService Team
