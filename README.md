# Teste de pipeline: alteração para validar CI/CD e gitflow
# ExecutionService

## Fluxo principal

1. Após o pagamento confirmado (evento `PaymentConfirmed` do BillingService), a OS entra na fila de execução local.
2. O serviço simula ou processa a execução, atualizando os estados internos: `Queued` → `Diagnosing` → `Repairing` → `Finished/Failed`.
3. A cada mudança de estado, eventos são publicados para o OSService: `ExecutionStarted`, `ExecutionProgressed` (opcional), `ExecutionFinished`, `ExecutionFailed`.
4. Caso receba o evento `OsCanceled` antes de finalizar, cancela a execução e publica `ExecutionCanceled`.
5. Toda publicação de evento é feita via Transactional Outbox, garantindo confiabilidade.
6. Idempotência e deduplicação são garantidas via Inbox.
7. Configuração de filas/tópicos por variáveis de ambiente.
8. Logs e traces são instrumentados com CorrelationId.

## Pastas principais
- **Messaging**: Configuração de filas/tópicos, CorrelationId.
- **Outbox**: Persistência e publicação de eventos.
- **Inbox**: Deduplicação de eventos recebidos.
- **Domain**: Entidades e state machine.
- **Workers**: BackgroundService para execução.
- **EventHandlers**: Handlers de eventos externos.

## Testes
- Testes unitários para handlers e state machine.
- Testa idempotência: mesmo EventId não cria job duplicado.

## Exemplo de fluxo
```
PaymentConfirmed → ExecutionStarted → Diagnosing → ExecutionProgressed → Repairing → ExecutionProgressed → Finished → ExecutionFinished
```

## Configuração
- `INPUT_QUEUE`: nome da fila de entrada
- `OUTPUT_TOPIC`: nome do tópico/bus de saída

## Concorrência
- O worker garante que apenas um job é processado por vez, evitando duplicidade.

## Observação
- Não há acesso ao banco do OSService, toda comunicação é via eventos.

# OficinaCardozo Execution Service

Este microserviço implementa a gestão de execuções (Execution) seguindo arquitetura em camadas, autenticação JWT, CI/CD, Docker e Kubernetes.

## Sumário
- [Requisitos](#requisitos)
- [Configuração](#configuração)
- [Build e Testes](#build-e-testes)
- [Docker](#docker)
- [Deploy Kubernetes](#deploy-kubernetes)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [CI/CD](#cicd)
- [Documentação da API](#documentação-da-api)

---

## Estrutura de Pastas

- `src/API`: Interface de entrada (controllers, endpoints)
- `src/Application`: Lógica de aplicação (casos de uso, serviços)
- `src/Domain`: Entidades de domínio, agregados, interfaces
- `src/Infrastructure`: Implementações de infraestrutura (repositórios, integrações externas)
- `src/InfraDb`: Integração com banco de dados
- `tests`: Testes automatizados
- `deploy/k8s`: Manifestos Kubernetes
- `.github/workflows`: Pipeline CI/CD

## Requisitos
- .NET 8 SDK
- Docker
- kubectl

## Configuração
1. Clone o repositório:
	```sh
	git clone https://github.com/marciocardozodev-org/OficinaCardozo.App.git
	cd OficinaCardozo.App/OFICINACARDOZO.EXECUTIONSERVICE
	```
2. Configure as variáveis de ambiente (exemplo para desenvolvimento):
	```sh
	export JWT_KEY="sua-chave-secreta"
	export ASPNETCORE_ENVIRONMENT=Development
	```

## Build e Testes
- Build local:
  ```sh
  dotnet build
  ```
- Testes:
  ```sh
  dotnet test
  ```

## Docker
- Build da imagem:
  ```sh
	docker build -t oficinacardozo-executionservice:latest .
  ```
- Rodar localmente:
  ```sh
	docker run -e JWT_KEY="sua-chave-secreta" -e ASPNETCORE_ENVIRONMENT=Development -p 8080:8080 oficinacardozo-executionservice:latest
  ```

## Deploy Kubernetes
1. Crie o secret para a chave JWT:
	```sh
	kubectl create secret generic executionservice-jwt-secret --from-literal=JWT_KEY="sua-chave-secreta" -n <namespace>
	```
2. Crie o ConfigMap para variáveis não sensíveis (ex: ambiente):
	```sh
	kubectl create configmap executionservice-config --from-literal=ASPNETCORE_ENVIRONMENT=Production -n <namespace>
	```
	> O deployment já está configurado para ler ASPNETCORE_ENVIRONMENT do ConfigMap.
3. Aplique os manifests:
	```sh
	kubectl apply -f deploy/k8s/
	```

## Variáveis de Ambiente
| Nome                  | Descrição                        | Exemplo                |
|-----------------------|----------------------------------|------------------------|
| JWT_KEY               | Chave secreta do JWT (obrigatório, use secret) | sua-chave-secreta      |
| ASPNETCORE_ENVIRONMENT| Ambiente (.NET)                  | Development/Production |

## CI/CD
- O pipeline GitHub Actions executa build, testes e publica a imagem Docker.
- Veja o arquivo [.github/workflows/ci-cd.yml](.github/workflows/ci-cd.yml).

## Documentação da API
- Acesse `/swagger` após subir o serviço para explorar e testar os endpoints.

---

> Dúvidas? Abra uma issue ou consulte os comentários XML nos controllers para detalhes dos endpoints.
