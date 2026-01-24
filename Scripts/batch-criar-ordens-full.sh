#!/bin/sh
# Script para simular o batch completo de criação e avanço de ordens de serviço via API
# Uso: ./batch-criar-ordens-full.sh <URL_BASE_API>


if [ -z "$1" ]; then
  echo "Uso: $0 <URL_BASE_API>"
  exit 1
fi


# Função para autenticar como admin
autenticar_admin() {
  AUTH_RESP=$(curl -s -X POST "$URL_BASE/api/Autenticacao/login" \
    -H "Content-Type: application/json" \
    -d '{"nomeUsuario":"admin","senha":"Password123!"}')
  TOKEN=$(echo "$AUTH_RESP" | jq -r '.token')
  if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "Erro ao autenticar como admin. Resposta: $AUTH_RESP"
    exit 1
  fi
  AUTH_HEADER="Authorization: Bearer $TOKEN"
  echo "Token admin obtido."
}

# Função para autenticar como cliente (CPF)
autenticar_cpf() {
  AUTH_RESP=$(curl -s -X POST "$URL_BASE/api/Autenticacao/login-cpf" \
    -H "Content-Type: application/json" \
    -d '{"cpfCnpj":"'$CPF_EXISTENTE'","senha":"Password123!"}')
  TOKEN=$(echo "$AUTH_RESP" | jq -r '.token')
  if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "Erro ao autenticar como cliente. Resposta: $AUTH_RESP"
    exit 1
  fi
  AUTH_HEADER="Authorization: Bearer $TOKEN"
  echo "Token cliente obtido."
}

# Usar sempre o mesmo cliente já existente
CPF_EXISTENTE="35496518806"


# Buscar veículos realmente vinculados ao cliente
URL_BASE="$1"
echo "URL_BASE: $URL_BASE"

# Autentica inicialmente como admin
echo "Autenticando..."
autenticar_admin

echo "Buscando veículos do cliente $CPF_EXISTENTE..."
VEICULOS_JSON=$(curl -s -X GET "$URL_BASE/api/Veiculos?cpfCnpj=$CPF_EXISTENTE" -H "Content-Type: application/json" -H "$AUTH_HEADER")
PLACAS_EXISTENTES=($(echo "$VEICULOS_JSON" | grep -o '"placa":"[^\"]*"' | cut -d':' -f2 | tr -d '"'))


# Função para autenticar como admin
autenticar_admin() {
  AUTH_RESP=$(curl -s -X POST "$URL_BASE/api/Autenticacao/login" \
    -H "Content-Type: application/json" \
    -d '{"nomeUsuario":"admin","senha":"Password123!"}')
  TOKEN=$(echo "$AUTH_RESP" | jq -r '.token')
  if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "Erro ao autenticar como admin. Resposta: $AUTH_RESP"
    exit 1
  fi
  AUTH_HEADER="Authorization: Bearer $TOKEN"
  echo "Token admin obtido."
}

# Função para autenticar como cliente (CPF)
autenticar_cpf() {
  AUTH_RESP=$(curl -s -X POST "$URL_BASE/api/Autenticacao/login-cpf" \
    -H "Content-Type: application/json" \
    -d '{"cpfCnpj":"'$CPF_EXISTENTE'","senha":"Password123!"}')
  TOKEN=$(echo "$AUTH_RESP" | jq -r '.token')
  if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "Erro ao autenticar como cliente. Resposta: $AUTH_RESP"
    exit 1
  fi
  AUTH_HEADER="Authorization: Bearer $TOKEN"
  echo "Token cliente obtido."
}

# Autentica inicialmente como admin
echo "Autenticando..."
autenticar_admin




# Usar sempre o mesmo cliente já existente
CPF_EXISTENTE="35496518806"
# Buscar serviço pelo nome antes de criar
echo "\nBuscando serviço de teste global..."
SERVICO_LISTA=$(curl -s -X GET "$URL_BASE/api/Servicos" -H "Content-Type: application/json" -H "$AUTH_HEADER")
SERVICO_ID=$(echo "$SERVICO_LISTA" | grep -o '{[^}]*}' | grep 'Serviço Teste' | grep -o '"id":[0-9]*' | grep -o '[0-9]*' | head -1)
if [ -z "$SERVICO_ID" ]; then
  echo "Serviço não encontrado, criando..."
  SERVICO_RESP=$(curl -s -w "\n[HTTP_STATUS]%{http_code}" -X POST "$URL_BASE/api/Servicos" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{
      "nomeServico": "Serviço Teste",
      "preco": 150.50,
      "tempoEstimadoExecucao": 1,
      "descricaoDetalhadaServico": "Serviço criado via batch",
      "frequenciaRecomendada": "12"
    }')
  echo "$SERVICO_RESP"
  SERVICO_ID=$(echo "$SERVICO_RESP" | grep -o '"id":[0-9]*' | grep -o '[0-9]*' | head -1)
  if [ -z "$SERVICO_ID" ]; then
    echo "Erro ao criar serviço global, abortando."
    exit 1
  fi
else
  echo "Serviço já existe. ID: $SERVICO_ID"
fi

if [ ${#PLACAS_EXISTENTES[@]} -eq 0 ]; then
  echo "Não há veículos cadastrados para o cliente $CPF_EXISTENTE."
  exit 1
fi

for PLACA in "${PLACAS_EXISTENTES[@]}"; do
  echo "\n--- Batch $i ---"
  echo "Usando cliente já existente: $CPF_EXISTENTE e veículo $PLACA"
  # Buscar ID do cliente pelo CPF
  CLIENTE_ID=$(curl -s -X GET "$URL_BASE/api/Clientes?cpfCnpj=$CPF_EXISTENTE" -H "Content-Type: application/json" -H "$AUTH_HEADER" | grep -o '"id":[0-9]*' | grep -o '[0-9]*' | head -1)
  if [ -z "$CLIENTE_ID" ]; then
    echo "Cliente com CPF $CPF_EXISTENTE não encontrado, abortando batch."
    exit 1
  fi
  echo "\nCriando ordem de serviço..."
  ORDEM_RESP=$(curl -s -w "\n[HTTP_STATUS]%{http_code}" -X POST "$URL_BASE/api/OrdensServico" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{"clienteCpfCnpj":"'$CPF_EXISTENTE'","veiculoPlaca":"'$PLACA'","veiculoMarcaModelo":"Modelo Teste","veiculoAnoFabricacao":2020,"veiculoCor":"Azul","veiculoTipoCombustivel":"Flex","servicosIds":['$SERVICO_ID'],"pecas":[]}')
  echo "$ORDEM_RESP"
  ORDEM_ID=$(echo "$ORDEM_RESP" | grep -o '"id":[0-9]*' | grep -o '[0-9]*' | head -1)
  echo "Ordem criada com ID: $ORDEM_ID"
  if [ -z "$ORDEM_ID" ]; then
    echo "Erro ao criar ordem, pulando para o próximo batch."
    continue
  fi

  echo "Iniciando diagnóstico da ordem $ORDEM_ID..."
  INICIAR_DIAG_RESP=$(curl -s -w "\n[HTTP_STATUS]%{http_code}" -X POST "$URL_BASE/api/OrdensServico/$ORDEM_ID/iniciar-diagnostico" -H "$AUTH_HEADER")
  echo "Resposta iniciar diagnóstico: $INICIAR_DIAG_RESP"
  INICIAR_DIAG_STATUS=$(echo "$INICIAR_DIAG_RESP" | grep '\[HTTP_STATUS\]' | sed 's/.*\[HTTP_STATUS\]\([0-9]*\)/\1/')
  if [ "$INICIAR_DIAG_STATUS" = "403" ]; then
    echo "403 ao iniciar diagnóstico. Tentando autenticar com CPF..."
    autenticar_cpf
    INICIAR_DIAG_RESP=$(curl -s -w "\n[HTTP_STATUS]%{http_code}" -X POST "$URL_BASE/api/OrdensServico/$ORDEM_ID/iniciar-diagnostico" -H "$AUTH_HEADER")
    echo "Resposta iniciar diagnóstico (com CPF): $INICIAR_DIAG_RESP"
    INICIAR_DIAG_STATUS=$(echo "$INICIAR_DIAG_RESP" | grep '\[HTTP_STATUS\]' | sed 's/.*\[HTTP_STATUS\]\([0-9]*\)/\1/')
  fi
  if [ "$INICIAR_DIAG_STATUS" != "200" ] && [ "$INICIAR_DIAG_STATUS" != "201" ]; then
    echo "Erro ao iniciar diagnóstico da ordem $ORDEM_ID. Pulando para o próximo batch."
    continue
  fi


  echo "Finalizando diagnóstico da ordem $ORDEM_ID..."
  DIAG_RESP=$(curl -s -X POST "$URL_BASE/api/OrdensServico/$ORDEM_ID/finalizar-diagnostico" -H "$AUTH_HEADER")
  echo "Resposta finalizar diagnóstico: $DIAG_RESP"

  # Extrair o id do orçamento diretamente da resposta do diagnóstico
  ORCAMENTO_ID=$(echo "$DIAG_RESP" | jq -r '.id')
  echo "Orçamento encontrado para aprovação: $ORCAMENTO_ID"
  if [ -z "$ORCAMENTO_ID" ] || [ "$ORCAMENTO_ID" = "null" ]; then
    echo "Não foi possível encontrar o id do orçamento para a ordem $ORDEM_ID."
    continue
  fi


  echo "Enviando orçamento para aprovação da ordem $ORDEM_ID..."
  curl -s -X POST "$URL_BASE/api/OrdensServico/enviar-orcamento-para-aprovacao" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{"idOrcamento":'$ORCAMENTO_ID'}'

  # Aguarda a ordem mudar para status 'Aguardando aprovação' (timeout 10s)
  echo "Aguardando status 'Aguardando aprovação' para a ordem $ORDEM_ID..."
  for attempt in $(seq 1 20); do
    STATUS=$(curl -s -X GET "$URL_BASE/api/OrdensServico/$ORDEM_ID" -H "Content-Type: application/json" -H "$AUTH_HEADER" | grep -o '"statusDescricao":"[^"]*"' | cut -d':' -f2 | tr -d '"')
    if [ "$STATUS" = "Aguardando aprovação" ]; then
      echo "Status correto alcançado."
      break
    fi
    if [ "$attempt" = "20" ]; then
      echo "Timeout aguardando status 'Aguardando aprovação'. Status atual: $STATUS"
    fi
  done

  echo "Aprovando orçamento da ordem $ORDEM_ID..."
  curl -s -X POST "$URL_BASE/api/OrdensServico/aprovar-orcamento" \
    -H "Content-Type: application/json" \
    -H "$AUTH_HEADER" \
    -d '{"idOrcamento":'$ORCAMENTO_ID',"aprovado":true}'

  echo "Iniciando execução da ordem $ORDEM_ID..."
  curl -s -X POST "$URL_BASE/api/OrdensServico/$ORDEM_ID/iniciar-execucao" -H "$AUTH_HEADER"
  echo "Finalizando serviço da ordem $ORDEM_ID..."
  curl -s -X POST "$URL_BASE/api/OrdensServico/$ORDEM_ID/finalizar-servico" -H "$AUTH_HEADER"
  echo "Entregando veículo da ordem $ORDEM_ID..."
  curl -s -X POST "$URL_BASE/api/OrdensServico/$ORDEM_ID/entregar-veiculo" -H "$AUTH_HEADER"
  echo "--- Batch $i finalizado ---"
done
