# 🤖 Prompt para Copilot - BillingService

## Contexto
Cole este prompt no Copilot Chat do BillingService para implementar a mudança de SQS para SNS.

---

## 📋 PROMPT

```
Preciso refatorar a publicação do evento PaymentConfirmed para usar SNS em vez de SQS direto.

CONTEXTO:
- Atualmente publico PaymentConfirmed diretamente na fila SQS execution-events
- Preciso trocar para publicar no SNS topic payment-confirmed
- O ExecutionService consome da fila billing-events (que está subscrita no topic)
- MessageAttributes devem ser preservados: EventType, CorrelationId, Timestamp

TAREFA:
1. Encontre onde estou publicando PaymentConfirmed usando SQS.SendMessageAsync
2. Refatore para usar SNS.PublishAsync no topic: arn:aws:sns:sa-east-1:953082827427:payment-confirmed
3. Mantenha os MessageAttributes (EventType, CorrelationId, Timestamp)
4. Mantenha o payload JSON sem envelope (RawMessageDelivery=true no SNS)
5. Adicione log com SNS MessageId para troubleshooting
6. Se necessário, adicione AWSSDK.SimpleNotificationService ao csproj

EXEMPLO DE CÓDIGO ESPERADO:

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
        },
        ["Timestamp"] = new MessageAttributeValue 
        { 
            DataType = "String", 
            StringValue = DateTime.UtcNow.ToString("o") 
        }
    }
});

_logger.LogInformation(
    "[CorrelationId: {CorrelationId}] PaymentConfirmed publicado no SNS. MessageId: {MessageId}",
    correlationId, response.MessageId);

IMPORTANTE:
- NÃO adicione envelope SNS manualmente (RawMessageDelivery já está configurado)
- NÃO mude o formato do payload JSON
- Preserve a estrutura: OsId, PaymentId, Status, Amount, ProviderPaymentId
- Certifique-se de injetar IAmazonSimpleNotificationService no DI

Mostre-me:
1. Os arquivos que precisam ser alterados
2. As mudanças necessárias no código
3. As alterações no DI/configuração
4. Se preciso adicionar NuGet packages
```

---

## ✅ Validação Pós-Implementação

Após o Copilot implementar, valide:

### 1. Teste via AWS CLI
```bash
aws sns publish \
  --topic-arn "arn:aws:sns:sa-east-1:953082827427:payment-confirmed" \
  --message '{"OsId":"00000000-0000-0000-0000-000000000099","PaymentId":"test","Status":1,"Amount":100.00}' \
  --message-attributes \
    'EventType={DataType=String,StringValue=PaymentConfirmed}' \
    'CorrelationId={DataType=String,StringValue=test-copilot-validation}' \
  --region sa-east-1
```

### 2. Verificar logs ExecutionService
```bash
kubectl logs -l app=executionservice -n default --tail=50 | grep "test-copilot-validation"
```

**Resultado esperado:**
```
[CorrelationId: test-copilot-validation] PaymentConfirmed processado para OS 00000000-0000-0000-0000-000000000099
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

---

## 🔍 Checklist Pós-Implementação

- [ ] Código refatorado: SQS → SNS
- [ ] NuGet package adicionado (se necessário)
- [ ] DI configurado com IAmazonSimpleNotificationService
- [ ] Logs incluem SNS MessageId
- [ ] MessageAttributes preservados
- [ ] Testado em dev/staging
- [ ] Validado E2E com ExecutionService
- [ ] Deploy em produção

---

**Criado**: 2026-02-21  
**Owner**: ExecutionService Team → BillingService Team
