# Implementação de Observabilidade - ExecutionService

## Resumo das Mudanças

### 1. Pacotes NuGet Adicionados
- Serilog.AspNetCore (8.0.0)
- Serilog.Sinks.Console (5.0.1)
- Serilog.Enrichers.Environment (2.3.0)
- Serilog.Enrichers.Thread (3.1.0)
- AWS.Logger.SeriLog (3.4.1)

### 2. Configuração de Logging Estruturado (Program.cs)
- Serilog configurado com JsonFormatter para console
- Sink do CloudWatch Logs integrado (AWS.Logger.SeriLog)
- Enriquecimento automático: LogContext, EnvironmentName, ThreadId, Service
- Variáveis de ambiente:
  - `AWS_REGION` (default: sa-east-1)
  - `CLOUDWATCH_LOG_GROUP` (default: /eks/prod/executionservice/application)
  - `ASPNETCORE_ENVIRONMENT` (Development desabilita CloudWatch)
- Try-catch-finally global com Log.CloseAndFlush()

### 3. Middleware de CorrelationId
**Arquivo:** `src/API/CorrelationIdMiddleware.cs`
- Lê header `Correlation-Id` da requisição
- Gera GUID se não existir
- Retorna CorrelationId no response header
- Injeta no contexto de logs via `LogContext.PushProperty`
- Registrado como primeiro middleware na pipeline

### 4. Logs de Negócio - SnsPublisher
**Arquivo:** `src/Messaging/SnsPublisher.cs`
- Log de sucesso APÓS confirmação real da AWS:
  ```
  ExecutionService gerou evento {EventType} | CorrelationId: {CorrelationId} | EventId: {EventId} | EntityId: {EntityId} | MessageId: {MessageId} | Status: Published
  ```
- Log de erro com contexto completo (eventType, correlationId, entityId)
- Extração de EntityId do payload (OsId ou JobId)

### 5. Logs de Negócio - SqsConsumer
**Arquivo:** `src/Messaging/SqsConsumer.cs`
- Log de consumo APÓS processamento bem-sucedido:
  ```
  ExecutionService consumiu evento {EventType} | CorrelationId: {CorrelationId} | EventId: {EventId} | EntityId: {EntityId} | Status: Processed
  ```
- Logs para PaymentConfirmed e OsCanceled

### 6. Logs de Negócio - EventHandlers
**Arquivos:** `src/EventHandlers/PaymentConfirmedHandler.cs` e `src/EventHandlers/OsCanceledHandler.cs`
- Log ao gravar evento no Outbox:
  ```
  ExecutionService gravou evento {EventType} no outbox | CorrelationId: {CorrelationId} | EventType: {EventType} | EntityId: {EntityId} | JobId: {JobId} | Status: {Status}
  ```

### 7. CloudWatch Log Group
- Nome: `/eks/prod/executionservice/application`
- Retenção: 30 dias
- Região: sa-east-1

## Arquivos Modificados

1. **OFICINACARDOZO.EXECUTIONSERVICE.csproj** - Pacotes NuGet
2. **Program.cs** - Configuração Serilog + middleware CorrelationId
3. **src/API/CorrelationIdMiddleware.cs** - NOVO ARQUIVO
4. **src/Messaging/SnsPublisher.cs** - Logs de publicação
5. **src/Messaging/SqsConsumer.cs** - Logs de consumo
6. **src/EventHandlers/PaymentConfirmedHandler.cs** - Logs de outbox
7. **src/EventHandlers/OsCanceledHandler.cs** - Logs de outbox

## Comandos de Validação

### Build
```bash
dotnet restore
dotnet build --no-restore
```

### CloudWatch Log Group
```bash
# Verificar se log group existe
aws logs describe-log-groups --region sa-east-1 --log-group-name-prefix /eks/prod/executionservice

# Verificar retenção
aws logs describe-log-groups --region sa-east-1 --log-group-name-prefix /eks/prod/executionservice | grep -i retention
```

### Deploy (exemplo)
```bash
# Build da imagem Docker
docker build -t executionservice:observability .

# Tag e push para registry
docker tag executionservice:observability <registry>/executionservice:latest
docker push <registry>/executionservice:latest

# Apply no Kubernetes
kubectl apply -f deploy/k8s/
kubectl rollout status deployment/executionservice
```

## Queries CloudWatch Logs Insights

### 1. Eventos de Negócio (Publicação e Consumo)
```
fields @timestamp, @message
| filter @message like /gerou evento|consumiu evento|gravou evento/
| sort @timestamp desc
| limit 100
```

### 2. Correlação por CorrelationId
```
fields @timestamp, @message
| filter @message like /CorrelationId/
| sort @timestamp desc
| limit 200
```

### 3. Eventos Publicados (ExecutionStarted, ExecutionCanceled)
```
fields @timestamp, @message
| filter @message like /ExecutionStarted|ExecutionCanceled/
| sort @timestamp desc
| limit 100
```

### 4. Eventos Consumidos (PaymentConfirmed, OsCanceled)
```
fields @timestamp, @message
| filter @message like /PaymentConfirmed|OsCanceled/
| sort @timestamp desc
| limit 100
```

### 5. Erros e Exceções
```
fields @timestamp, @message
| filter @message like /ERROR|Exception|Erro/
| sort @timestamp desc
| limit 50
```

### 6. Rastreamento por CorrelationId específico
```
fields @timestamp, @message
| filter CorrelationId = "<correlation-id-aqui>"
| sort @timestamp asc
```

## Tabela de Validação Esperada

| Timestamp | Evento | CorrelationId | EntityId | Status | MessageId |
|-----------|--------|---------------|----------|--------|-----------|
| 2026-02-22T10:15:30Z | PaymentConfirmed | abc-123 | OS-001 | Processed | - |
| 2026-02-22T10:15:31Z | ExecutionStarted | abc-123 | OS-001 | Queued | msg-456 |
| 2026-02-22T10:15:32Z | ExecutionStarted | abc-123 | OS-001 | Published | aws-msg-789 |

## Variáveis de Ambiente (deploy/k8s/configmap.yaml)

Adicionar ao ConfigMap:
```yaml
AWS_REGION: "sa-east-1"
CLOUDWATCH_LOG_GROUP: "/eks/prod/executionservice/application"
ASPNETCORE_ENVIRONMENT: "Production"
```

## Permissões IAM Necessárias

O ServiceAccount do pod precisa ter permissões no CloudWatch Logs:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "logs:CreateLogGroup",
        "logs:CreateLogStream",
        "logs:PutLogEvents",
        "logs:DescribeLogGroups",
        "logs:DescribeLogStreams"
      ],
      "Resource": "arn:aws:logs:sa-east-1:*:log-group:/eks/prod/executionservice/*"
    }
  ]
}
```

## Próximos Passos

1. ✅ Build sem erros - CONCLUÍDO
2. ⏳ Deploy da nova imagem no EKS
3. ⏳ Validar logs no CloudWatch (últimos 30 min)
4. ⏳ Executar queries de validação
5. ⏳ Preencher tabela de validação com dados reais
6. ⏳ Verificar correlação ponta a ponta

## Padrão de Logs Implementado

### Formato Padronizado
```
<MicroserviceAction> | CorrelationId: <guid> | EventType: <type> | EntityId: <id> | Status: <status> [| MessageId: <id>]
```

### Exemplos
```
ExecutionService gerou evento ExecutionStarted | CorrelationId: abc-123 | EventId: evt-456 | EntityId: OS-001 | MessageId: aws-789 | Status: Published
ExecutionService consumiu evento PaymentConfirmed | CorrelationId: abc-123 | EventId: evt-456 | EntityId: OS-001 | Status: Processed
ExecutionService gravou evento ExecutionStarted no outbox | CorrelationId: abc-123 | EventType: ExecutionStarted | EntityId: OS-001 | JobId: job-789 | Status: Queued
```

## Diferencial Implementado

✅ Logs estruturados em JSON
✅ CorrelationId em toda a cadeia de request/response
✅ Logs de negócio explícitos (não só técnicos)
✅ Confirmação real antes de log de sucesso
✅ CloudWatch integrado nativamente
✅ Enriquecimento automático (environment, thread, service)
✅ Rastreabilidade ponta a ponta
