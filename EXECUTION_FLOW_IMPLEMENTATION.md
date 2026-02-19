# ExecutionService - Fluxo de Execução (Iniciar Execução)

## Visão Geral

Este documento descreve a implementação completa do fluxo **"Iniciar Execução"** no ExecutionService. O serviço consome eventos `PaymentConfirmed` do BillingService via SQS/SNS, cria jobs de execução com transições de estado, publica eventos de progresso e conclusão.

## Arquitetura

```
┌─────────────────────────┐
│   BillingService        │
│   (SNS: PaymentConfirmed)
└────────────┬────────────┘
             │
             ▼
┌──────────────────────────────────┐
│  SNS Topic: billing-events       │
│  (subscription via SQS)          │
└────────────┬─────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│  SQS Queue: billing-events       │  ◄────── SqsConsumer (BackgroundService)
│  (receives SNS messages)         │
└──────────────────────────────────┘

┌──────────────────────────────────────────────────┐
│              ExecutionService                    │
├──────────────────────────────────────────────────┤
│  ▲                                               │
│  │ PaymentConfirmed / OsCanceled (SqsConsumer) │
│  │                                               │
│  ├─► PaymentConfirmedHandler ─────┐            │
│  │   (Inbox + Outbox + Job)        │            │
│  │                                  │            │
│  ├─► OsCanceledHandler ────────────┤            │
│  │   (Cancel Job)                   │            │
│  │                                  │            │
│  │                        [Outbox]  │            │
│  │                                  │            │
│  └─► ExecutionWorker ◄─────────────┘            │
│      (Job State Transitions)                     │
│      Queued ► Diagnosing ► Repairing ► Finished│
│                                                  │
│  [Outbox] ─► SnsPublisher ──► SNS Topic         │
│      (ExecutionStarted, ExecutionProgressed,    │
│       ExecutionFinished, ExecutionCanceled)     │
└──────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────┐
│  SNS Topic: execution-events     │
│  (published for other services)  │
└──────────────────────────────────┘
```

## Componentes Implementados

### 1. **SqsConsumer** (`src/Messaging/SqsConsumer.cs`)
- BackgroundService que ouve a fila SQS `billing-events`
- Extrai mensagens do envelopePN SNS
- Roteia para handlers apropriados (PaymentConfirmed, OsCanceled)
- Deleta mensagens após processamento bem-sucedido
- Log estruturado com CorrelationId

**Configuração:**
```json
{
  "SQS_QUEUE_URL": "arn:aws:sqs:us-east-1:000000000000:billing-events",
  "SNS_TOPIC_ARN": "arn:aws:sns:us-east-1:000000000000:execution-events"
}
```

### 2. **PaymentConfirmedHandler** (`src/EventHandlers/PaymentConfirmedHandler.cs`)
- Recebe eventos `PaymentConfirmed` do BillingService
- Implementa idempotência via **Inbox** (EventId)
- Cria novo `ExecutionJob` com status `Queued`
- Publica evento `ExecutionStarted` via **Outbox**
- Logs com CorrelationId em cada passo

**Fluxo:**
```
PaymentConfirmed {OsId, PaymentId, Amount, Status, CorrelationId}
    ▼
[Inbox] Verifica duplicata via EventId
    ▼
[Job] Cria ExecutionJob(Queued) com CorrelationId
    ▼
[Outbox] Enfileira ExecutionStarted para publicar no SNS
```

### 3. **OsCanceledHandler** (`src/EventHandlers/OsCanceledHandler.cs`)
- Recebe eventos `OsCanceled`
- Busca job ativo para o OsId
- Transiciona job para status `Canceled`
- Publica `ExecutionCanceled` via Outbox

### 4. **ExecutionWorker** (`src/Workers/ExecutionWorker.cs`)
- BackgroundService que processa transições de estado
- Ciclo: `Queued` → `Diagnosing` → `Repairing` → `Finished`
- Publica `ExecutionProgressed` (estados intermediários)
- Publica `ExecutionFinished` (conclusão)
- Intervalo configurável (padrão: 5s)

**Transições:**
```
Queued
  ▼ (5s)
Diagnosing (ExecutionProgressed)
  ▼ (5s)
Repairing (ExecutionProgressed)
  ▼ (5s)
Finished (ExecutionFinished)
```

### 5. **SnsPublisher** (`src/Messaging/SnsPublisher.cs`)
- BackgroundService que publica eventos Outbox no SNS
- Lê eventos não publicados do Outbox
- Publica no SNS com attributes (EventType, EventId, CorrelationId)
- Marca como `Published` após sucesso

**Payload de Publicação:**
```json
{
  "Message": "{\"OsId\":\"...\",\"CorrelationId\":\"...\",...}",
  "MessageAttributes": {
    "EventType": {"DataType":"String", "StringValue":"ExecutionStarted"},
    "EventId": {"DataType":"String", "StringValue":"..."},
    "CorrelationId": {"DataType":"String", "StringValue":"..."},
    "PublishedAt": {"DataType":"String", "StringValue":"2026-02-19T10:30:00Z"}
  }
}
```

### 6. **Serviços de Inbox/Outbox** (Em memória)
- `IInboxService`: Armazena EventIds processados (duplicação)
- `IOutboxService`: Armazena eventos não publicados

⚠️ **Nota:** Implementação em memória. Para produção, use DB (PostgreSQL).

### 7. **TestingController** (`src/API/TestingController.cs`)
- Endpoints para simular PaymentConfirmed e OsCanceled localmente
- Sem autenticação (apenas para testes)
- Retorna CorrelationId para rastrear fluxo

## Como Testar Localmente

### Prerequisitos
```bash
# 1. Build
dotnet build

# 2. Rodar a aplicação
dotnet run
```

### Teste 1: Simular PaymentConfirmed
```bash
curl -X POST http://localhost:5001/api/testing/payment-confirmed \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": "evt-001",
    "osId": "os-123",
    "paymentId": "pay-456",
    "amount": 1500.00,
    "status": "Confirmed",
    "correlationId": "corr-abc123"
  }'
```

**Resposta:**
```json
{
  "message": "PaymentConfirmed processado com sucesso",
  "correlationId": "corr-abc123"
}
```

**Logs esperados:**
```
[CorrelationId: corr-abc123] Iniciando handler PaymentConfirmed para OS os-123, PaymentId pay-456
[CorrelationId: corr-abc123] Evento registrado no Inbox
[CorrelationId: corr-abc123] ExecutionJob criado com Id <job-id>, Status: Queued
[CorrelationId: corr-abc123] Evento ExecutionStarted enfileirado no Outbox
```

### Teste 2: Observar Transições do Worker
```bash
# Após o teste 1, aguarde 5s e observe os logs:
[CorrelationId: corr-abc123] Transição de estado: OS os-123 → Diagnosing
[CorrelationId: corr-abc123] Evento ExecutionProgressed enfileirado no Outbox

[CorrelationId: corr-abc123] Transição de estado: OS os-123 → Repairing
[CorrelationId: corr-abc123] Evento ExecutionProgressed enfileirado no Outbox

[CorrelationId: corr-abc123] Transição de estado: OS os-123 → Finished
[CorrelationId: corr-abc123] Evento ExecutionFinished enfileirado no Outbox
```

### Teste 3: Simular OsCanceled
```bash
# Criar um job primeiro
curl -X POST http://localhost:5001/api/testing/payment-confirmed \
  -H "Content-Type: application/json" \
  -d '{"osId":"os-999","correlationId":"corr-test"}'

# Agora cancelar
curl -X POST http://localhost:5001/api/testing/os-canceled \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": "evt-cancel-001",
    "osId": "os-999",
    "reason": "Cliente cancelou a ordem",
    "correlationId": "corr-test"
  }'
```

**Logs esperados:**
```
[CorrelationId: corr-test] Iniciando handler OsCanceled para OS os-999, Motivo: Cliente cancelou a ordem
[CorrelationId: corr-test] ExecutionJob <job-id> foi cancelado
[CorrelationId: corr-test] Evento ExecutionCanceled enfileirado no Outbox
```

## Configuração de Produção

### Variáveis de Ambiente
```bash
# JWT
JWT_KEY=sua-chave-secreta

# Database
DB_HOST=postgres.default.svc.cluster.local
DB_NAME=executionservice
DB_USER=postgres
DB_PASSWORD=<postgres-password>

# AWS
AWS_REGION=us-east-1
AWS_ACCESS_KEY_ID=<your-key>
AWS_SECRET_ACCESS_KEY=<your-secret>
SQS_QUEUE_URL=https://sqs.us-east-1.amazonaws.com/account-id/billing-events
SNS_TOPIC_ARN=arn:aws:sns:us-east-1:account-id:execution-events
```

## Idempotência e Confiabilidade

1. **Inbox Pattern:**
   - Cada evento tem `EventId` único
   - Antes de processar, verifica se já foi processado (Inbox)
   - Evita duplicatas mesmo com retransmissões SQS

2. **Outbox Pattern:**
   - Eventos publicados são registrados em OutboxEvent
   - BackgroundService lê não-publicados e tenta publicar
   - Marca como `Published` apenas após sucesso SNS

3. **Job Idempotency:**
   - Apenas 1 job por OsId
   - Se PaymentConfirmed for reenviado, job não é recriado

## Próximas Melhorias

1. **Persistência:**
   - Migrar InboxEvent, OutboxEvent, ExecutionJob para PostgreSQL
   - Executar migrations do EF Core

2. **Tratamento de Erros:**
   - Implementar retry logic com exponential backoff
   - Dead Letter Queue para mensagens falhadas

3. **Monitoring:**
   - Adicionar métricas (Prometheus)
   - Tracing distribuído (OpenTelemetry)
   - Alertas para jobs falhados

4. **Testes:**
   - Testes unitários para handlers
   - Testes de integração com SQS/SNS mockado
   - Testes end-to-end

## Estrutura de Arquivos Criados

```
src/
├── Messaging/
│   ├── SqsConsumer.cs (novo)
│   ├── SnsPublisher.cs (novo)
│   ├── CorrelationIdProvider.cs
│   ├── MessagingConfig.cs
│   └── MessagingConfigProvider.cs
├── EventHandlers/
│   ├── PaymentConfirmedHandler.cs (atualizado)
│   └── OsCanceledHandler.cs (atualizado)
├── Workers/
│   └── ExecutionWorker.cs (atualizado)
├── API/
│   └── TestingController.cs (novo)
├── Domain/
│   ├── ExecutionJob.cs
│   └── ExecutionStatus.cs
├── Inbox/
│   ├── InboxEvent.cs
│   └── InboxService.cs
├── Outbox/
│   ├── OutboxEvent.cs
│   └── OutboxService.cs
└── ExecutionDbContext.cs (atualizado)

Program.cs (atualizado)
OFICINACARDOZO.EXECUTIONSERVICE.csproj (atualizado com AWS packages)
```

## Logs Estruturados

Todos os fluxos incluem `[CorrelationId: {correlationId}]` para rastreamento ponta-a-ponta:

```
[CorrelationId: corr-abc123] Processando evento PaymentConfirmed com EventId evt-001
[CorrelationId: corr-abc123] Evento registrado no Inbox
[CorrelationId: corr-abc123] ExecutionJob criado com Id job-xyz
[CorrelationId: corr-abc123] Evento ExecutionStarted enfileirado no Outbox
...
[CorrelationId: corr-abc123] Transição: OS → Finished
[CorrelationId: corr-abc123] Evento ExecutionFinished enfileirado no Outbox
[CorrelationId: corr-abc123] Evento ExecutionFinished publicado com MessageId msg-123
```

---

**Implementado em:** 19/02/2026
**Stack:** .NET 8.0, AWS (SQS/SNS), PostgreSQL, EF Core
