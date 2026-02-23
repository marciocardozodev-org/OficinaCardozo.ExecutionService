=================================================================================
PROMPT PARA EXECUTIONSERVICE TEAM - Verificação de ExecutionFinished
=================================================================================

CONTEXTO TÉCNICO VALIDADO
=========================

Com base na análise do código atual, confirmo que ExecutionService DEVERIA estar publicando ExecutionFinished. 
Aqui está o fluxo esperado:

1. **SqsConsumer** (a cada 10s) lê PaymentConfirmed de billing-events
   - Handler: PaymentConfirmedHandler
   - Cria ExecutionJob com Status=Queued
   - Publica ExecutionStarted no Outbox ✓

2. **ExecutionWorker** (a cada 5s) transiciona estados de jobs
   - Queued → Diagnosing → Repairing → Finished
   - Quando atinge Finished, publica ExecutionFinished no Outbox ✓

3. **SnsPublisher** (a cada 5s) publica eventos do Outbox no SNS
   - Topic: execution-events
   - Marca eventos como Published ✓

4. **SNS Subscription** roteia para os-events
   - OSService recebe ExecutionFinished ✓
   - OSService marca OS como Finished ✓


SITUAÇÃO ATUAL
==============

Analisando logs recentes de ExecutionService (últimos 30 minutos), NÃO encontramos evidência de:

❌ Logs de SqsConsumer processando PaymentConfirmed
   Esperado: "ExecutionService consumiu evento PaymentConfirmed | CorrelationId: xxx | Status: Processed"
   Atual: ??? (VERIFIQUE)

❌ Logs de PaymentConfirmedHandler criando ExecutionJob
   Esperado: "ExecutionJob criado com Id {JobId}, Status: Queued"
   Atual: ??? (VERIFIQUE)

❌ Logs de ExecutionWorker transitioning para Finished
   Esperado: "[CorrelationId: xxx] Transição de estado: OS yyy → Finished"
   Atual: ??? (VERIFIQUE)

❌ Logs de ExecutionFinished sendo publicado
   Esperado: "Evento ExecutionFinished enfileirado no Outbox"
   Esperado: "ExecutionService gerou evento ExecutionFinished | CorrelationId: xxx | Status: Published"
   Atual: ??? (VERIFIQUE)

✅ Encontramos APENAS:
   - Polling logs de OutboxEvents a cada 5s
   - Polling logs de ExecutionJobs a cada 5s
   - Health checks


DESCOBERTAS DO DIAGÓSTICO
===========================

**Discovery #1: SNS Subscription Corrigida ✅**
  ❌ Antes: execution-finished → SQS billing-events (REMOVIDA)
  ✅ Agora: execution-finished → SQS os-events (CRIADA)
  
  Resultado: ExecutionFinished TEM QUE chegar em OSService, IF ExecutionService publicar.

**Discovery #2: ExecutionService Pode Não Estar Publicando ⚠️**
  
  Possíveis causas:
  
  a) SqsConsumer NÃO está consumindo PaymentConfirmed
     └─ Cenários: fila vazia, fila errada, consumer parado
  
  b) PaymentConfirmedHandler NÃO está criando ExecutionJob
     └─ Cenários: duplicata detectada, erro no banco, erro de parsing
  
  c) ExecutionWorker NÃO está transitioning
     └─ Cenários: jobs não estão em estado Queued, erro no worker, intervalo muito grande
  
  d) SnsPublisher NÃO está publicando para SNS
     └─ Cenários: OutboxEvents não estão sendo criados, erro ao publicar SNS


CHECKLIST DE VERIFICAÇÃO
=========================

### PASSO 1: Verificar Consumo de PaymentConfirmed

[ ] Rodar comando:
```bash
kubectl logs -n default deployment/executionservice --since=30m 2>&1 | \
  grep -i "PaymentConfirmed\|ExecutionService consumiu evento PaymentConfirmed"
```

**Esperado:** Ver linhas como:
```
ExecutionService consumiu evento PaymentConfirmed | CorrelationId: 30742e4e-f638-4347-a4e1-4d7987f03831 | Status: Processed
ExecutionJob criado com Id {JobId}, Status: Queued
```

**Se NÃO encontrar nada:**
→ SqsConsumer não está lendo de billing-events
→ Verificar: MessagingConfig.InputQueue = billing-events?
→ Verificar: SQS credentials/permissions?


### PASSO 2: Verificar Criação de ExecutionJob

[ ] Conectar ao banco de ExecutionService e rodar:
```bash
# Substituir POSTGRES_HOST, POSTGRES_USER, POSTGRES_DB conforme suas credenciais
psql -h $POSTGRES_HOST -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "SELECT id, os_id, status, created_at, correlation_id \
   FROM execution_jobs \
   ORDER BY created_at DESC LIMIT 10;"
```

**Esperado:** Ver rows com status = 'Queued', 'Diagnosing', 'Repairing', ou 'Finished'

**Se tabela estiver vazia:**
→ ExecutionJob nunca foi criado
→ Verifique PASSO 1 ou erro no PaymentConfirmedHandler


### PASSO 3: Verificar Outbox Events

[ ] Conectar ao banco e rodar:
```bash
psql -h $POSTGRES_HOST -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "SELECT id, event_type, correlation_id, published, created_at, published_at \
   FROM outbox_events \
   ORDER BY created_at DESC LIMIT 20;"
```

**Esperado:** Ver:
- ExecutionStarted com published=true ou false
- ExecutionProgressed com published=true ou false
- ExecutionFinished com published=true ou false

**Se NÃO houver ExecutionFinished ou todos tiverem published=false:**
→ ExecutionWorker não fez transição para Finished
→ ou SnsPublisher não está publicando
→ Verifique PASSO 4


### PASSO 4: Aumentar Log Level

[ ] Adicione essas linhas ao **PaymentConfirmedHandler.HandleAsync**:

```csharp
public async Task HandleAsync(PaymentConfirmedEvent evt)
{
    _logger.LogInformation("🔵 [START] Handling PaymentConfirmed");
    _logger.LogInformation("   OsId: {OsId}", evt.OsId);
    _logger.LogInformation("   PaymentId: {PaymentId}", evt.PaymentId);
    _logger.LogInformation("   CorrelationId: {CorrelationId}", evt.CorrelationId);
    
    // ... resto do código existente ...
    
    // Antes de criar job, adicione:
    _logger.LogInformation("🔎 Verificando duplicata via Inbox (EventId: {EventId})", evt.EventId);
    
    // Após criar job, adicione:
    _logger.LogInformation("✅ [OK] ExecutionJob criado: Id={JobId}, OsId={OsId}, Status=Queued", 
        job.Id, job.OsId);
}
```

[ ] Adicione essas linhas ao **ExecutionWorker.TransitionToAsync**:

```csharp
private async Task TransitionToAsync(ExecutionJob job, ExecutionStatus newStatus, IOutboxService outbox, CancellationToken stoppingToken)
{
    try
    {
        _logger.LogInformation("🔄 [START] Transitioning Job {JobId} from {OldStatus} to {NewStatus}", 
            job.Id, job.Status, newStatus);
        
        // ... código existente ...
        
        if (newStatus == ExecutionStatus.Finished)
        {
            job.FinishedAt = DateTime.UtcNow;
            // ... criar OutboxEvent ...
            
            _logger.LogInformation("📤 [OK] ExecutionFinished event enqueued to Outbox");
            _logger.LogInformation("   JobId: {JobId}", job.Id);
            _logger.LogInformation("   OsId: {OsId}", job.OsId);
            _logger.LogInformation("   CorrelationId: {CorrelationId}", job.CorrelationId);
            _logger.LogInformation("   Duration: {Duration} seconds", 
                (job.FinishedAt - job.CreatedAt)?.TotalSeconds);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ [ERROR] Failed to transition job {JobId} to {NewStatus}", 
            job.Id, newStatus);
        // ...
    }
}
```


### PASSO 5: Teste Prático com Nova OS

Após adicionar logs:

**5.1 - Limpar e redeployer:**
```bash
kubectl delete deployment executionservice -n default
kubectl apply -f deploy/k8s/deployment.yaml
kubectl wait --for=condition=available --timeout=300s deployment/executionservice -n default
```

**5.2 - Iniciar tail de logs:**
```bash
kubectl logs -n default deployment/executionservice -f --tail=100 > execution-logs.txt 2>&1 &
```

**5.3 - Criar nova OS (OS 8) via BillingService:**
```bash
# (OSService ou BillingService cria OS 8)
# BillingService publica PaymentConfirmed
```

**5.4 - Monitorar queue em tempo real:**
```bash
# Terminal 2
watch -n 1 "aws sqs get-queue-attributes --region sa-east-1 \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/execution-events \
  --attribute-names ApproximateNumberOfMessages | jq '.Attributes.ApproximateNumberOfMessages'"
```

**Esperado:** Número de mensagens começa em 1 (PaymentConfirmed) e vai para 0 (SqsConsumer consome)

**5.5 - Monitorar banco:**
```bash
# Terminal 3 - polling a cada 5s
while true; do
  echo "=== ExecutionJobs ==="; \
  psql -h $HOST -U $USER -d $DB -c \
    "SELECT os_id, status, created_at FROM execution_jobs WHERE os_id like '%8%' ORDER BY created_at DESC LIMIT 1;"; \
  echo ""; \
  echo "=== Outbox Events ==="; \
  psql -h $HOST -U $USER -d $DB -c \
    "SELECT event_type, published, created_at FROM outbox_events WHERE correlation_id like '%' ORDER BY created_at DESC LIMIT 3;"; \
  sleep 5; \
  clear; \
done
```

**Esperado:**
- Minuto 0: ExecutionJob criado (status=Queued)
- Minuto 5: ExecutionJob transicionado (status=Diagnosing)
- Minuto 10: ExecutionJob transicionado (status=Repairing)
- Minuto 15: ExecutionJob transicionado (status=Finished), ExecutionFinished salvo no Outbox (published=false)
- Minuto 20: ExecutionFinished publicado (published=true)


### PASSO 6: Validar Entrega em os-events

```bash
# Verificar que ExecutionFinished chegou em os-events
aws sqs receive-message --region sa-east-1 \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/os-events \
  --max-number-of-messages 3 | jq '.Messages[].Body'
```

**Esperado:** Ver evento com:
```json
{
  "EventType": "ExecutionFinished",
  "OsId": "00000000-0000-0000-0000-000000000008",
  "CorrelationId": "...",
  "Status": "Published"
}
```

**Se NÃO encontrar:**
→ ExecutionFinished não foi publicado em SNS
→ Volte ao PASSO 4 e verifique logs


### PASSO 7: Validar Finalização em OSService

```bash
# Verificar se OSService finalizou OS 8
curl -s http://osservice:8080/api/os/00000000-0000-0000-0000-000000000008 | jq '.status'
```

**Esperado:** `"Finished"` ou `"Completed"`

**Se NÃO for:**
→ OSService não recebeu ExecutionFinished
→ Verifique se mensagem chegou em os-events (PASSO 6)


MAPA MENTAL DE DEBUGGING
========================

```
❓ OS não finaliza em OSService
  ↓
  ├─ ExecutionFinished em os-events?
  │  ├─ SIM → Problema é OSService (não é nossa responsabilidade)
  │  └─ NÃO → Prosseguir...
  │
  ├─ ExecutionFinished publicado em SNS?
  │  ├─ SIM → Problema é SNS subscription (ja_verificado ✓)
  │  └─ NÃO → Prosseguir...
  │
  ├─ ExecutionJob finalizado (status=Finished)?
  │  ├─ SIM → Problema é SnsPublisher (nao_publica_outbox)
  │  └─ NÃO → Prosseguir...
  │
  ├─ ExecutionJob criado?
  │  ├─ SIM → Problema é ExecutionWorker (nao_transiciona)
  │  └─ NÃO → Prosseguir...
  │
  └─ PaymentConfirmed consumido de SQS?
     ├─ SIM → Problema é PaymentConfirmedHandler
     └─ NÃO → Problema é SqsConsumer ou Config
```


RESUMO DE FIXES JÁ APLICADOS
=============================

| Fix | Descrição | Status |
|-----|-----------|--------|
| #1 | Remover subscription errada: payment-confirmed → billing-events | ✅ FEITO |
| #2 | Remover subscription errada: execution-finished → billing-events | ✅ FEITO |
| #3 | Recriar subscription: execution-finished → os-events | ✅ FEITO |
| #4 | Verificar aqui: SqsConsumer consumindo de execution-events | ⏳ PENDENTE |


RESPONSABILIDADES CLARAS
========================

- **ExecutionService Team**: Criar logs verbosos nos handlers e workers
- **ExecutionService Team**: Validar que PaymentConfirmed está sendo consumido
- **ExecutionService Team**: Validar que ExecutionJob está sendo criado
- **ExecutionService Team**: Validar que ExecutionFinished está sendo publicado
- **Infraestrutura Team**: SNS subscriptions (já corrigidas ✓)
- **BillingService Team**: Parar de consumir de billing-events ❌ (em progresso)
- **OSService Team**: Esperar ExecutionFinished (não tem ação imediata)


PRÓXIMO PASSO
=============

[ ] Rodar Passo 1 do checklist acima (verificar logs de consumo)
[ ] Compartilhar resultado:
    - Encontrou logs de PaymentConfirmed? Quais?
    - Ou não encontrou nada?

Com base na resposta, saberemos exatamente onde está o problema.


=================================================================================
Data: 22/02/2026 - 18:00 UTC
Status: Aguardando verificação de logs de SqsConsumer
Responsável: ExecutionService Team
=================================================================================
