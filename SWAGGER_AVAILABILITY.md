# ⚠️ Guia de Disponibilidade do Swagger - ExecutionService

## 🎯 Resumo Executivo

**URL do Swagger**: `http://localhost:5000/swagger/index.html`

⚠️ **IMPORTANTE**: Em ambiente com Load Balancer (Kubernetes), o Swagger pode estar **intermitentemente indisponível** durante operações de rolling update, deploys ou reinicializações de pods.

---

## 📊 URLs de Acesso

### Desenvolvimento (Local)

```
HTTP:  http://localhost:5000/swagger/index.html
HTTPS: https://localhost:5001/swagger/index.html
```

### Homologação (Docker Compose)

```
HTTP:  http://localhost:8080/swagger/index.html
Swagger JSON: http://localhost:8080/swagger/v1/swagger.json
```

### Produção (via Load Balancer / Kubernetes)

```
URL: https://execution-service.oficinacardozo.com/swagger/index.html
JSON: https://execution-service.oficinacardozo.com/swagger/v1/swagger.json
```

---

## 🔴 Cenários de Indisponibilidade

### 1. Distribuição de Tráfego
**Impacto**: Baixo  
**Duração**: Milissegundos  
**Causa**: Load Balancer roteia requisição para pod ainda iniciando

**Sintoma**:
```
HTTP 503 Service Unavailable
ou
Connection timeout
```

**Solução**: Retry automático (ver abaixo)

---

### 2. Rolling Update / Deploy
**Impacto**: Moderado  
**Duração**: 30-60 segundos  
**Causa**: Pods antigos sendo drenados gradualmente

**Fluxo**:
```
1. Novo deploy iniciado
2. Novos pods criam containers
3. Tráfego redirecionado
4. Pods antigos terminam
5. ⏱️ Swagger pode estar indisponível entre etapas
```

**Solução**: Usar health checks e retry com backoff exponencial

---

### 3. Pod Restart / Crash
**Impacto**: Alto  
**Duração**: 10-30 segundos  
**Causa**: Pod falhou ou foi reiniciado (OOMKilled, CrashLoop, etc)

**Fluxo**:
```
1. Pod recebe SIGTERM ou crash detectado
2. Graceful shutdown inicia
3. Requisições ativas finalizam
4. ⏱️ Swagger indisponível até novo pod estar ready
5. Load Balancer roteia para outro pod disponível
```

**Detecção**:
```bash
kubectl get pods -n production-executionservice
# STATUS: CrashLoopBackOff ou Pending
```

---

### 4. Network Partition
**Impacto**: Crítico  
**Duração**: Indeterminado  
**Causa**: Problema na conectividade do cluster

**Sintoma**:
```
Timeout em todas as requisições
Connection reset by peer
```

---

## ✅ Estratégias de Resiliência

### 1️⃣ Retry Automático com curl

```bash
# Retry simples (3 tentativas)
curl --retry 3 \
     --retry-delay 1 \
     --retry-connrefused \
     http://localhost:5000/swagger/index.html

# Com timeout
curl --retry 5 \
     --retry-delay 2 \
     --max-time 10 \
     http://localhost:5000/swagger/index.html
```

### 2️⃣ Retry com Loop Bash

```bash
# Retry com backoff exponencial
for i in {1..5}; do
  echo "Tentativa $i..."
  curl -s http://localhost:5000/swagger/index.html && break
  sleep $((2 ** i))  # 2, 4, 8, 16 segundos
done
```

### 3️⃣ Health Check Endpoint

```bash
# Verificar saúde da aplicação
curl -i http://localhost:5000/health

# Resposta esperada (200 OK)
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

### 4️⃣ Salvar Swagger Offline

```bash
# Baixar definição OpenAPI
curl http://localhost:5000/swagger/v1/swagger.json > swagger.json

# Ou usar swagger-cli para validar
swagger-cli validate swagger.json

# Usar com ReDoc localmente
docker run -it -p 8080:80 \
  -e SPEC_URL=file:///swagger.json \
  -v $(pwd)/swagger.json:/swagger.json \
  redocly/redoc
```

### 5️⃣ Configurar Timeout em Aplicação Cliente

**C# HttpClient com Polly**:
```csharp
var retryPolicy = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .Or<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => 
            TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Console.WriteLine($"Retry {retryCount} após {timespan.TotalSeconds}s");
        });

var client = new HttpClientBuilder()
    .AddPolicyHandler(retryPolicy)
    .Build();
```

---

## 📈 SLA Esperado

| Ambiente | Uptime Esperado | Downtime/Mês | SLA |
|----------|-----------------|--------------|-----|
| Dev (Docker Local) | 80% | ~7 horas | N/A |
| Staging (K8s) | 95% | ~1.9 horas | Best Effort |
| Production (K8s) | 99.9% | ~26 segundos | Crítico |

---

## 🔧 Troubleshooting Rápido

### 1. Swagger retorna 503

```bash
# Verificar pods
kubectl get pods -n production-executionservice
kubectl describe pod <pod-name> -n production-executionservice

# Verificar logs
kubectl logs <pod-name> -n production-executionservice
kubectl logs <pod-name> -n production-executionservice --previous  # último crash
```

### 2. Swagger timeout

```bash
# Aumentar timeout no kubectl
export KUBECONFIG_timeout=60s
kubectl exec -it <pod-name> -- /bin/sh

# Testar conectividade interna
curl -v http://execution-service.default.svc.cluster.local:5000/health
```

### 3. Swagger vazio ou com erro de schema

```bash
# Verificar se o swagger.json está saudável
curl http://localhost:5000/swagger/v1/swagger.json | jq '.'

# Validar schema OpenAPI
npx swagger-cli validate swagger.json

# Ou usar ferramenta online
# https://editor.swagger.io/
```

### 4. Intermitência aleatória

**Possível causa**: Load Balancer distribuindo entre pods com saúde diferente

```bash
# Verificar health de cada pod individual
for pod in $(kubectl get pods -n production-executionservice -o name); do
  echo "Pod: $pod"
  kubectl exec $pod -- curl -s localhost:5000/health
done
```

---

## 🚨 Monitoramento Proativo

### Prometheus Metrics

```yaml
# health_check_duration_seconds
# health_check_failures_total
# swagger_requests_total{endpoint="/swagger"}
```

### AlertManager Regras Sugeridas

```yaml
- alert: SwaggerUnavailable
  expr: rate(health_check_failures_total[5m]) > 0.1
  for: 30s
  annotations:
    summary: "ExecutionService Swagger está indisponível"
    
- alert: HighSwaggerLatency
  expr: health_check_duration_seconds > 2
  for: 2m
  annotations:
    summary: "Latência alta no Swagger"
```

---

## 📝 Endpoints Relacionados

| Endpoint | Método | Descrição |
|----------|--------|-----------|
| `/swagger/index.html` | GET | Interface Swagger UI |
| `/swagger/v1/swagger.json` | GET | Definição OpenAPI em JSON |
| `/health` | GET | Health check da aplicação |
| `/api/execution/**` | GET/POST/PUT | Endpoints da API (protegidos) |

---

## 🔒 Segurança e Acesso

### Autenticação Requerida

```bash
# Obter token JWT
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"user", "password":"pass"}'

# Usar token em requisição
curl -H "Authorization: Bearer <TOKEN>" \
  http://localhost:5000/swagger/index.html
```

⚠️ **Nota**: A UI do Swagger é pública, mas os endpoints da API requerem autenticação.

---

## 🌐 CORS e Proxy

Se acessando remotamente via proxy/VPN:

```bash
# Com proxy HTTP
curl -x http://proxy.company.com:8080 \
  --proxy-user user:pass \
  http://localhost:5000/swagger/index.html

# Desabilitar SSL verification (apenas dev!)
curl -k https://localhost:5001/swagger/index.html
```

---

## 📞 Escalation

**Swagger indisponível **>5 minutos**?**

1. Verificar status do cluster: `kubectl cluster-info`
2. Verificar status do nó: `kubectl get nodes`
3. Verificar logs de deploy: `kubectl rollout status deployment/execution-service`
4. Contatar: DevOps Team (ops@oficinacardozo.com)

---

**Última atualização**: 23 de Fevereiro de 2026  
**Status**: ✅ Documentação Completa  
**Versão**: 1.0
