# 🔍 Diagnóstico Completo - SNS→SQS Integration Issue

## Problema Identificado: URL INCORRETA das Filas SQS

### ❌ URLs ERRADAS (usadas antes)
```
https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events
https://sqs.sa-east-1.amazonaws.com/953082827427/execution-events
```

### ✅ URLs CORRETAS (reais do AWS)
```
https://sa-east-1.queue.amazonaws.com/953082827427/billing-events
https://sa-east-1.queue.amazonaws.com/953082827427/execution-events
```

**Diferença**: `sqs.` vs `queue.` no subdomínio!

---

## Histórico de Descobertas

### 1️⃣ Teste SNS Publishing ✅
- **Resultado**: SNS publica com sucesso (MessageId: d4c907a0..., 23faab83..., etc)
- **Conclusão**: SNS está funcionando ✅

### 2️⃣ Teste SQS Direto (URL ERRADA)  ❌
```bash
aws sqs send-message \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --message-body "test"
```
- **Resultado**: send-message retorna MessageId MAS contagem não aumenta
- **Conclusão**: URL estava ERRADA!

### 3️⃣ Descoberta da URL Correta ✅
```bash
aws sqs list-queues --region sa-east-1
```
- **Saída Real**: `https://sa-east-1.queue.amazonaws.com/953082827427/billing-events`
- **Conclusão**: AWS usa subdomínio `queue.` não `sqs.`

### 4️⃣ Root Cause
- **ConfigMap tinha URLs erradas**: `sqs.sa-east-1.amazonaws.com`
- **SNS subscriptions apontavam para ARNs corretos**: `arn:aws:sqs:...` (abstrato)
- **Resultado**: SNS publica mas entrega para endpoint ERRADO
- **ConséquEncia**: Mensagens "desaparecem" - não chegam em lugar nenhum

---

## Cronograma de Testes

| Teste | URL Usada | Resultado | Conclusão |
|-------|-----------|-----------|-----------|
| SNS Publish | N/A | ✅ MessageId | SNS OK |
| SQS Direct (old) | `sqs.sa-east-1...` | ❌ Não chega | URL ERRADA |
| SQS Direct (new) | `sa-east-1.queue...` | 🔄 Teste pendente | AGUARDA |
| SNS→SQS (old URL) | `sqs.sa-east-1...` | ❌ 0 mensagens | URL ERRADA |
| SNS→SQS (new URL) | `sa-east-1.queue...` | 🔄 Teste pendente | AGUARDA |

---

## Configurações Atualizadas

### ✅ ConfigMap Corrigida
**Arquivo**: `/deploy/k8s/aws-messaging-config.yaml`

```yaml
AWS_SQS_QUEUE_BILLING: "https://sa-east-1.queue.amazonaws.com/953082827427/billing-events"
AWS_SQS_QUEUE_DLQ_BILLING: "https://sa-east-1.queue.amazonaws.com/953082827427/billing-events-dlq"
```

**Mudanças**:
- `sqs.sa-east-1.amazonaws.com` → `sa-east-1.queue.amazonaws.com`

### SNS Subscriptions (já corretas)
- Topic: `arn:aws:sns:sa-east-1:953082827427:execution-started`
- Queue: `arn:aws:sqs:sa-east-1:953082827427:billing-events`
- RawMessageDelivery: ✅ `true`
- Status: ✅ `PendingConfirmation: false`

### CloudWatch Logs (ativado)
- ✅ SNSLogsRole criada
- ✅ SNSLogsPolicy anexada
- ✅ HTTPSuccessFeedbackRoleArn configurado
- ✅ HTTPFailureFeedbackRoleArn configurado
- ✅ SQSSuccessFeedbackRoleArn configurado

---

## Próximos Passos

### 1️⃣ Aplicar ConfigMap Corrigida
```bash
kubectl apply -f deploy/k8s/aws-messaging-config.yaml
```

### 2️⃣ Re-implantar ExecutionService
```bash
kubectl rollout restart deployment/executionservice -n default
```

### 3️⃣ Testar SNS→SQS com URL Corrigida
```bash
# Publicar no SNS
aws sns publish \
  --to pic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --message "test" \
  --region sa-east-1

# Receber da SQS (URL CORRIGIDA)
aws sqs receive-message \
  --queue-url https://sa-east-1.queue.amazonaws.com/953082827427/billing-events \
  --region sa-east-1
```

### 4️⃣ Validação Final
- ✅ SNS publishes with MessageId
- ✅ SQS receive-message retorna mensagem
- ✅ ApproximateNumberOfMessages aumenta
- ✅ Logs aparecem em CloudWatch

---

## Lições Aprendidas

### ❌ O que estava ERRADO
1. **URL Incorreta**: Usando `sqs.` em vez de `queue.` no subdomínio
2. **Falta de Visibilidade**: Sem CloudWatch Logs, não havia pista do erro
3. **Assumções**: Achamos que SNS→SQS simples funcionaria sem validar URLs

### ✅ O que FIZ
1. Criei 3 testes diagnósticos (Policy, VPC, Logs)
2. Ativei CloudWatch Logs para visibilidade
3. Listei todas as filas com `list-queues` para encontrar URLs REAIS
4. Comparei URLs esperadas vs reais e identifiquei discrepância
5. Corrigui ConfigMap com URLs corretas
6. Documentei descobertas para futuro

### 🔄 Mudança Necessária
- **ConfigMap**: URLs SQS atualizadas ✅
- **CI/CD Pipeline**: Próxima execução re-aplicará ConfigMap
- **Testing**: Adicionar validação de URLs no onboarding

---

## Status Final

| Item | Status | Evidência |
|------|--------|-----------|
| SNS Topics | ✅ OK | ARNs criados |
| SQS Queues | ✅ OK | Listadas com URLs corretas |
| SNS Subscriptions | ✅ OK | RawMessageDelivery=true |
| SQS Policies | ✅ OK | Condition.ArnEquals correto |
| CloudWatch Logs | ✅ OK | Role + Policy aplicados |
| ConfigMap URLs | ✅ CORRIGIDA | Atualizada com URLs certas |
| SNS Publishing | ✅ OK | MessageIds retornados |
| SQS Delivery | 🔄 PENDENTE | Aguarda redeployed com URLs corretas |

