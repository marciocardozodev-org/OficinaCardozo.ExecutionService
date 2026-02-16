# Teste de pipeline: alteração para validar CI/CD e gitflow

# OficinaCardozo Billing Service

Este microserviço implementa a gestão de faturamento (Billing) seguindo arquitetura em camadas, autenticação JWT, CI/CD, Docker e Kubernetes.

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
	cd OficinaCardozo.App/OFICINACARDOZO.BILLINGSERVICE
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
	docker build -t oficinacardozo-billingservice:latest .
  ```
- Rodar localmente:
  ```sh
	docker run -e JWT_KEY="sua-chave-secreta" -e ASPNETCORE_ENVIRONMENT=Development -p 8080:8080 oficinacardozo-billingservice:latest
  ```

## Deploy Kubernetes
1. Crie o secret para a chave JWT:
	```sh
	kubectl create secret generic billingservice-jwt-secret --from-literal=JWT_KEY="sua-chave-secreta" -n <namespace>
	```
2. Crie o ConfigMap para variáveis não sensíveis (ex: ambiente):
	```sh
	kubectl create configmap billingservice-config --from-literal=ASPNETCORE_ENVIRONMENT=Production -n <namespace>
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
