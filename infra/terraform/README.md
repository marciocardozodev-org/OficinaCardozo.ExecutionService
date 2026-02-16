
# Infraestrutura - Terraform (RDS)

Esta pasta contém os arquivos Terraform necessários para provisionar o banco de dados RDS PostgreSQL do BillingService.

## Origem
- Baseado no repositório: https://github.com/marciocardozodev-org/OficinaCardozo.InfraDB.git
- Sempre sincronize manualmente com o InfraDB para manter o padrão e as melhores práticas.
- Documente aqui o commit de referência do InfraDB utilizado na última atualização.

## Como usar
1. Configure as variáveis necessárias (consulte os arquivos .tf e o README do InfraDB).
2. Execute:
   ```sh
   terraform init
   terraform plan
   terraform apply
   ```
3. O output trará a string de conexão para ser usada no deploy do serviço.

## Observações
- Não altere a estrutura sem revisar o InfraDB.
- Para atualizar, copie novamente os arquivos/módulos do InfraDB e ajuste este README com o novo commit de referência.
