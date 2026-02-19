# 🆘 Prompt para Time de BillingService - SNS→SQS Delivery Issue

## CONTEXTO DO PROBLEMA

Estamos implementando PublishSubscribe pattern via SNS→SQS no **ExecutionService**. O fluxo esperado é:

```
ExecutionService (SNS Publisher)
    ↓
SNS Topics (execution-started, execution-finished, execution-events)
    ↓
SQS Subscriptions (→ billing-events queue)
    ↓
BillingService & OuterServices (SQS Consumers)
```

**STATUS**: SNS publishing funciona ✅, SQS delivery falha ❌

---

## PROBLEMA IDENTIFICADO

### Cenário de Teste
```bash
# 1. ExecutionService publica evento
MessageId: 4215c4d6-0530-5a32-90a7-a6c7a5ebf64d
Status: ✅ Publicado com sucesso no SNS

# 2. SQS billing-events conta ANTES: 0 mensagens
# 3. Aguardamos 10 segundos (timeout padrão é 5s)
# 4. SQS billing-events conta DEPOIS: 0 mensagens ❌

EVIDÊNCIA CRÍTICA:
- SNS acknowledges publicação
- MAS SQS nunca recebe a mensagem
```

### Configuração Validada
```json
✅ SQS Policy: Contém Condition.ArnEquals com tópicos SNS
✅ Subscriptions: 3 criadas (execution-started, execution-finished, execution-events)
✅ RawMessageDelivery: Habilitado em todas
✅ SNS Topic: Policy open (sem restrições)
✅ SQS send-message direto: Funciona (conseguimos enviar mensagem direto para SQS)
```

### Testes Executados
```bash
# ✅ Teste 1: SQS recv direto
aws sqs send-message --queue-url ... → MessageId: 270ba14c-3a08...
✅ SUCESSO

# ❌ Teste 2: SNS→SQS delivery
aws sns publish --topic-arn ... → MessageId: 4215c4d6-0530...
(após 10s) aws sqs receive-message → 0 messages
❌ FALHA

# ✅ Teste 3: Publicação do ExecutionService
CorrelationId: test-sqs-console-1771543821
Logs mostram: "publicado com MessageId: ..."
✅ Publicação funciona
```

---

## SUSPEITAS (Para Investigação do Time BillingService)

### 1️⃣ **VPC/Network Issue**
```
Possibilidade: Fila billing-events está em VPC diferente de SNS?
- Verificar: AWS Console → SQS → billing-events → Network
- Se sim: SNS (public) não consegue acessar SQS (VPC private)
- Solução: SNS Endpoint na VPC ou Lambda intermediária
```

### 2️⃣ **SQS Endpoint/URL Issue**
```
Possibilidade: Subscription tem URL errada?
- Subscription Endpoint: arn:aws:sqs:sa-east-1:953082827427:billing-events ✅ (correto)
- MAS: Há URL vs ARN confusion?
- Ver: AWS Console → SNS → execution-started → Subscriptions
```

### 3️⃣ **Subscription Configuration**
```
Possibilidade: Há FilterPolicy ou RedrivePolicy bloqueando?
- Verificado: Sem FilterPolicy
- Verificado: Sem RedrivePolicy em billing-events
- MAS: Pode haver algo em outra configuração
```

### 4️⃣ **Mensagem Sendo Rejeitada**
```
Possibilidade: SNS envia, MAS SQS rejeita e descarta?
- Como diagnosticar:
  a) Verificar Dead Letter Queue (se houver)
  b) Ativar CloudWatch Logs para SNS delivery
  c) Verficiar SNS Delivery Status logs
```

---

## SOLICITAÇÃO ESPECÍFICA AO TIME DE BILLINGSERVICE

### 🔍 **Investigação 1: Verificar Fila**
```bash
# Na conta de BillingService (ou com acesso), rodar:

# 1. Ver atributos completos da fila
aws sqs get-queue-attributes \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --attribute-names All \
  --region sa-east-1 | jq '.Attributes | {
    QueueArn,
    ReceiveMessageWaitTimeSeconds,
    VisibilityTimeout,
    RedrivePolicy,
    KmsMasterKeyId,
    KmsDataKeyReusePeriodSeconds
  }'

# 2. Contar mensagens e deadletters
aws sqs get-queue-attributes \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --attribute-names ApproximateNumberOfMessages,ApproximateNumberOfMessagesNotVisible,ApproximateNumberOfMessagesDelayed \
  --region sa-east-1

# 3. Tentar receber mensagens na fila
aws sqs receive-message \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --wait-time-seconds 10 \
  --region sa-east-1
```

### 🔍 **Investigação 2: Verificar SNS Subscription**
```bash
# Na conta de BillingService, rodar:

# 1. Listar subscriptions do tópico execution-started
aws sns list-subscriptions-by-topic \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --region sa-east-1 | jq '.Subscriptions[] | {
    SubscriptionArn,
    Protocol,
    Endpoint,
    Owner
  }'

# 2. Ver atributos detalhados de cada subscription
# (Copiar SubscriptionArn e rodar:)
aws sns get-subscription-attributes \
  --subscription-arn <COLA_AQUI_O_ARN> \
  --region sa-east-1 | jq '.Attributes'
```

### 🔍 **Investigação 3: Ativar CloudWatch Logs para SNS**
```bash
# (Pode fazer no AWS Console ou CLI)
# SNS → execution-started Topic → Monitoring → Enable logging to CloudWatch

# Depois rodar um teste e ver logs
aws logs tail /aws/sns/sa-east-1/execution-started/Failure \
  --region sa-east-1 --follow
```

### ✅ **Teste Simples para Validar**
```bash
# ExecutionService team pode rodar:

# 1. Publicar no SNS
MSG_ID=$(aws sns publish \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --message 'BillingService testing message' \
  --region sa-east-1 \
  --output text)

echo "MessageId: $MSG_ID"

# 2. BillingService team recebe:
sleep 5

aws sqs receive-message \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --region sa-east-1 | jq '.Messages | length'
```

---

## CONTEXTO TÉCNICO PARA REFERENCE

### Arquitetura
```
ExecutionService → SNS Topics
  ├── execution-started (arn:aws:sns:sa-east-1:953082827427:execution-started)
  ├── execution-finished (arn:aws:sns:sa-east-1:953082827427:execution-finished)
  └── execution-events (arn:aws:sns:sa-east-1:953082827427:execution-events)
      ↓
  SNS→SQS Subscriptions
      ├── execution-started → arn:aws:sqs:sa-east-1:953082827427:billing-events
      ├── execution-finished → arn:aws:sqs:sa-east-1:953082827427:billing-events
      └── execution-events → arn:aws:sqs:sa-east-1:953082827427:execution-events
          ↓
  SQS Queues (Consumers)
      ├── billing-events (⚠️ PROBLEMA AQUI - mensagens não chegam)
      ├── billing-events-dlq
      ├── execution-events
      └── execution-events-dlq
```

### Commits Refrência
```
d17e460  feat: SNS→SQS subscriptions and queue policies (IaC + documentation)
e30216c  docs: guia completo SNS→SQS integration
55d19c4  docs: diagnóstico detalhado SNS→SQS delivery issue
```

### Documentação Criada
- [SNS_SQS_INTEGRATION_GUIDE.md](https://github.com/marciocardozodev-org/OficinaCardozo.ExecutionService/blob/develop/SNS_SQS_INTEGRATION_GUIDE.md)
- [SNS_SQS_DELIVERY_ISSUE.md](https://github.com/marciocardozodev-org/OficinaCardozo.ExecutionService/blob/develop/SNS_SQS_DELIVERY_ISSUE.md)

---

## SOLICITAÇÃO FINAL

### O que esperamos do time de BillingService:

1. **Executar investigação** (Seção "Investigação 1, 2, 3" acima)
2. **Compartilhar resultados** dos comandos acima
3. **Validar configuração de rede** entre SNS e SQS
4. **Sugerir solução** (pode ser VPC Endpoint, Lambda intermediária, etc)
5. **Testar delivery** com teste simples fornecido

### Informações que teríamos gosto de ter:
```
- [ ] Output completo de `sqs get-queue-attributes`
- [ ] Output completo de `sns get-subscription-attributes`
- [ ] Confirmação: A fila está em VPC? Qual?
- [ ] Há KMS encryption? Qual key?
- [ ] CloudWatch Logs para SNS delivery failures (se houver)
- [ ] Resultado do teste simples (aws sns publish → sqs receive)
```

### Timeline
- **Hoje**: BillingService executa investigações
- **Amanhã**: Discussão sobre solução (VPC Endpoint vs Lambda vs outra)
- **ASAP**: Implementar fix e validar end-to-end

---

## IMPACTO
- Bloqueia integração SNS→SQS do ExecutionService
- Impacta NotificationService e outros que consomem da fila
- Crítico para PublishSubscribe pattern de eventos

**Status**: 🔴 BLOCKING - Aguardando análise de BillingService

---

**Contato**: ExecutionService Team
**Data**: 2026-02-19
**Branch**: develop (commit: 55d19c4)
