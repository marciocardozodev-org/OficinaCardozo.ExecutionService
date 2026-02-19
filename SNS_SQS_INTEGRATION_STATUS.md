# SQS Policies e SNS→SQS Integration - Status Completo

## ✅ O que foi CONCLUÍDO

### 1. **Infrastructure Preparada**
- 3 tópicos SNS criados: `execution-started`, `execution-finished`, `execution-events`
- 5 filas SQS criadas: `billing-events`, `billing-events-dlq`, `execution-events`, `execution-events-dlq`, `os-status`
- 3 subscriptions SNS→SQS criadas (IDs atualizados após recriar)
  - `execution-started` → `billing-events`
  - `execution-finished` → `billing-events`
  - `execution-events` → `execution-events` (fila)

### 2. **SQS Policy Aplicada Corretamente via AWS Console**
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {"Service": "sns.amazonaws.com"},
      "Action": "sqs:SendMessage",
      "Resource": "arn:aws:sqs:sa-east-1:953082827427:billing-events",
      "Condition": {
        "ArnEquals": {
          "aws:SourceArn": [
            "arn:aws:sns:sa-east-1:953082827427:execution-started",
            "arn:aws:sns:sa-east-1:953082827427:execution-finished"
          ]
        }
      }
    }
  ]
}
```

✅ **Validado via CLI**: Policy contém `Condition.ArnEquals` com tópicos execution-*

### 3. **SNS Publishing Funcionando**
Teste CorrelationId: `test-sqs-console-1771543821`

Eventos publicados com sucesso:
- ExecutionStarted: MessageId `a2173f0b-7a22-520f-95e8-5d1ae56cba69`
- ExecutionProgressed: MessageId `c81b1182-2a21-53ef-8eba-7393b572bb89`
- ExecutionProgressed: MessageId `cb7f90e1-569d-5096-ae77-f2f0c9c528f0`

✅ **Validado via logs**: Sem NotFoundException, publicação bem-sucedida!

### 4. **IaC Criada (Terraform)**
Arquivo: `infra/terraform/sqs_policies.tf`

Contém:
- Data sources para SQS queues
- 3 Subscriptions como resources Terraform
- 2 SQS Queue Policies como resources Terraform
- Outputs para todos os ARNs

**Status**: Pronto para aplicar (git commit pendente)

## ⚠️ O que ainda precisa validar

### 1. **SQS Delivery (CRÍTICO)**
- ApproximateNumberOfMessages ainda mostra **0** mesmo após publicação
- Possíveis causas:
  a) Mensagens foram consumidas automaticamente
  b) Fila tem problemas de recebimento
  c) Hay atraso no delivery (SQS eventual consistency)

### 2. **Próximas ações para validar:**

**Opção A: Verificar Logs detalhados de SNS**
```bash
# CloudWatch Logs para SNS delivery
aws logs describe-log-groups --region sa-east-1 | grep sns
```

**Opção B: Usar SNS Test Message**
```bash
# Publicar diretamente para testar delivery
aws sns publish \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --message '{"test":"message"}' \
  --region sa-east-1
```

**Opção C: Criar Lambda para debug**
- Monitora SQS receive events
- Mostra se mensagens chegam

## 📋 Checklist para validação final

- [x] SNS Policy criada com Condition
- [x] SNS Subscriptions criadas
- [x] SNS Publishing funcionando (zero NotFoundException)
- [ ] **SQS receiving messages (PENDENTE)**
- [ ] SNS→SQS delivery confirmado  
- [ ] End-to-end test com CorrelationId ponta-a-ponta

## 🔍 Diagnóstico Possível

Como você viu que a policy foi aplicada e events são publicados no SNS, o problema pode estar em:

1. **Raw Message Delivery**: Há opção `Enable raw message delivery` nas subscriptions?
   - Isto faz SNS enviar o body da mensagem diretamente vs wrapped

2. **Queue Retention**: As mensagens podem estar chegando mas não "visíveis"
   - ApproximateNumberOfMessages pode ter delay

3. **Subscription Filter Policy**: Há algum filtro na subscription que está rejeitando?

## 🚀 Próximas ações recomendadas:

### 1. Commit Terraform
```bash
git add infra/terraform/sqs_policies.tf
git commit -m "feat: SNS→SQS subscriptions and policies (IaC)"
git push
```

### 2. Executar Terraform Apply
```bash
cd infra/terraform
terraform apply -auto-approve -var="enable_db=true"
```

### 3. Teste com SNS Direct Publish
```bash
aws sns publish \
  --topic-arn arn:aws:sns:sa-east-1:953082827427:execution-started \
  --message 'test' \
  --region sa-east-1 && \
sleep 5 && \
aws sqs receive-message \
  --queue-url https://sqs.sa-east-1.amazonaws.com/953082827427/billing-events \
  --region sa-east-1
```

Se isto funcionar, o problema pode estar no formato da mensagem do ExecutionService.

---

## 📊 Status Resumido

| Componente | Status | Observação |
|-----------|--------|-----------|
| SNS Topics | ✅ Criados | execution-{started,finished,events} |
| SQ Queues | ✅ Criadas | billing-events, execution-events, etc |
| SNS Subscriptions | ✅ Criadas | IDs atualizados após recriar |
| SQS Policy | ✅ Aplicada | Com Condition restrita |
| SNS Publishing | ✅ Funcionando | MessageIds retornados |
| SQS Delivery | ⏳ A validar | Contador mostra 0, mas pode ser eventual consistency |
| IaC (Terraform) | ✅ Pronto | Pronto commit e apply |
