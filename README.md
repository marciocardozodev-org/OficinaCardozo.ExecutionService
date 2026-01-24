# OficinaCardozo.App

## Descrição
Repositório principal da aplicação Oficina Cardozo. Responsável pela API, lógica de negócio, integrações e execução do serviço principal em ambiente Docker/Kubernetes.

## Tecnologias Utilizadas
- .NET
- Docker
- Kubernetes (EKS)
- AWS Aurora
- Datadog

## Passos para Execução e Deploy
1. Clone o repositório.
2. Configure as variáveis de ambiente e arquivos de configuração.
3. Execute `docker-compose up` para ambiente local ou utilize os manifests do diretório k8s/ para deploy em EKS.
4. Acompanhe logs e métricas via Datadog.

## Diagrama da Arquitetura
<!-- Insira aqui o diagrama da arquitetura deste repositório quando disponível -->

## Documentação da API
- [Swagger](./OficinaCardozo.API/swagger)
- [Coleção Postman](./docs/postman_collection.json)
