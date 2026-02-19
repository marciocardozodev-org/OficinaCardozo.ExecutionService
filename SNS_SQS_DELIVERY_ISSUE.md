# ⚠️ SNS→SQS Integration - Status Detalhado (19/02/2026)

## 🔴 PROBLEMA IDENTIFICADO

**SNS está publicando com sucesso, MAS SQS não recebe as mensagens.**

### Configurações Validadas ✅
- ✅ SNS Topics criados e funcionais
- ✅ SQS Queues criadas e funcionais
- ✅ SNS Subscriptions criadas corretamente
- ✅ SQS Access Policy com Condition presente
- ✅ RawMessageDelivery ativado nas subscriptions
- ✅ SNS consegue publicar (MessageIds retornados)
- ✅ SQS consegue receber mensagens diretas (bypass SNS)
- ✅ SNS não tem policy restritiva

### O que NÃO está funcionando ❌
- ❌ SNS não entrega mensagens para SQS
- ❌ ApproximateNumberOfMessages não aumenta após SNS publish
- ❌ Nenhuma mensagem recebida via `aws sqs receive-message` após SNS publish

---

## 🔍 Análise de Causa Raiz

### Teoria 1: Problema com Dead Letter Queue
**Resultado**: SQS não tem DLQ configurado - descartado ✅

### Teoria 2: SQS em VPC com restrição de rede
**Status**: A investigar
- Possível que a fila esteja em VPC privada
- SNS pode não conseguir alcançar fila por routing/security groups
- Solução: Verificar VPC/Subnet/SG no console AWS

### Teoria 3: Problema de Subscription - MessageFilter
**Resultado**: Testado, RawMessageDelivery está `true` - descartado ✅

### Teoria 4: Problema regional ou conta AWS
**Resultado**: Região sa-east-1 consistente, account 953082827427 correto - descartado ✅

### Teoria 5: SNS→SQS suporta apenas standard queues (não FIFO)
**Resultado**: Queue é standard, não FIFO - descartado ✅

---

## 📊 Comando de Teste que Comprova o Problema

```bash
# ANTES
aws sqs get-queue-attributes \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --attribute-names ApproximateNumberOfMessages \
  --region sa-east-1
# Resultado: "ApproximateNumberOfMessages": "0"

# PUBLICAR
aws sns publish \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --message "Test message" \
  --region sa-east-1
# Resultado: MessageId: 4215c4d6-0530-5a32-90a7-a6c7a5ebf64d ✅

# AGUARDAR 10s

# DEPOIS
aws sqs get-queue-attributes \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --attribute-names ApproximateNumberOfMessages \
  --region sa-east-1
# Resultado: "ApproximateNumberOfMessages": "0" ❌ MESMA!
```

---

## 🎯 Próximas Ações Recomendadas

### Ação 1: Verificar VPC/Network (AWS Console)
1. Acessar AWS Console → SQS → `billing-events`
2. Procure seção **Network**
3. Verifique se fila está em VPC privada
4. Se sim: SNS pode não conseguir alcançar - considere criar interface endpoint

### Ação 2: Habilitar SNS Delivery Logs
```bash
# Criar IAM role para SNS logs
# Então configurar topic attribute DeliveryPolicy para logs
aws sns set-topic-attributes \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --attribute-name HTTPSuccessFeedbackRoleArn \
  --attribute-value "arn:aws:iam::953082827427:role/sns-logs-role" \
  --region sa-east-1
```

Isto vai registrar logs em CloudWatch mostrando por que SNS não consegue entregar.

### Ação 3: Testar com fila diferente
```bash
# Usar o `execution-events` SQS queue em vez de `billing-events`
# Se funcionar: problema específico da fila billing-events
# Se não funcionar: problema geral SNS→SQS
```

### Ação 4: Criar SNS→Email para comparação
```bash
# Teste se SNS consegue publicar para outro protocolo (email)
# Se funcionar email mas não SQS: problema específico SQS

aws sns subscribe \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --protocol email \
  --notification-endpoint seu-email@example.com \
  --region sa-east-1
```

---

## 📋 Artefatos Criados

| Arquivo | Status | Descrição |
|---------|--------|-----------|
| `infra/terraform/sqs_policies.tf` | ✅ Criado | IaC para subscriptions + policies |
| `SNS_SQS_INTEGRATION_GUIDE.md` | ✅ Criado | Guia executivo |
| `SNS_SQS_INTEGRATION_STATUS.md` | ✅ Criado | Status técnico anterior |
| **Este arquivo** | ✅ Criado | Diagnóstico detalhado |

---

## 🔄 Estado das Subscriptions (19/02/2026 19:43 UTC)

```
execution-started:87fad2b0-... (OLD - DELETADA)
execution-started:63bcb170-... (NEW - ATIVA, RawMessageDelivery=true)
execution-finished:189aee56-... (OLD - DELETADA)
execution-finished:747dd472-... (NEW - ATIVA, RawMessageDelivery=true)
```

---

## 💡 Insight Técnico

O fato de que:
1. SNS consegue publicar (MessageId retornado)
2. SQS consegue receber direto (bypass SNS funciona)
3. Policy está correta
4. Subscription existe e está ativa
5. MAS delivery não funciona

Sugere um **problema sistêmico de conectividade/rede** entre SNS e SQS, provavelmente relacionado a:
- VPC/Networking
- Cross-service permissions em nível AWS
- Ou problema com endpoint da fila SQS (URL vs ARN)

---

## 📞 Recomendação Final

**Ir para AWS Console e abrir um Live Chat Support** para investigar por que SNS não consegue entregar para SQS quando:
- Policy está OK
- Subscription está ativa
- RawMessageDelivery está true
- Ambos serviços estão em sa-east-1
- Ambos na mesma conta (953082827427)

Mencione o MessageId último teste: `4215c4d6-0530-5a32-90a7-a6c7a5ebf64d` para rastreamento.

---

## ✅ Workaround Possível

Se o  problema for VPC/rede:

**Opção A**: Usar SNS→Lambda→SQS (Lambda pode acessar VPC)

**Opção B**: Recriar a fila `billing-events` fora da VPC (se estiver dentro)

**Opção C**: Usar SNS→HTTP (webhook) em vez de SQS

---

**Atualizado**: 2026-02-19 19:43 UTC
**Commits relacionados**: d17e460, e30216c
