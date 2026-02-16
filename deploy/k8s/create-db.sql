-- Script de criação das tabelas principais do BillingService
-- Tabela: orcamento
CREATE TABLE orcamento (
    id SERIAL PRIMARY KEY,
    ordem_servico_id INTEGER NOT NULL,
    valor NUMERIC(12,2) NOT NULL,
    email_cliente VARCHAR(255) NOT NULL,
    status SMALLINT NOT NULL, -- 0: Pendente, 1: Enviado, 2: Aprovado, 3: Rejeitado
    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_orcamento_ordem_servico_id ON orcamento(ordem_servico_id);

-- Tabela: pagamento
CREATE TABLE pagamento (
    id SERIAL PRIMARY KEY,
    ordem_servico_id INTEGER NOT NULL,
    valor NUMERIC(12,2) NOT NULL,
    metodo VARCHAR(100) NOT NULL,
    status SMALLINT NOT NULL, -- 0: Pendente, 1: Confirmado, 2: Falhou
    criado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_pagamento_ordem_servico_id ON pagamento(ordem_servico_id);

-- Tabela: atualizacao_status_os
CREATE TABLE atualizacao_status_os (
    id SERIAL PRIMARY KEY,
    ordem_servico_id INTEGER NOT NULL,
    novo_status VARCHAR(100) NOT NULL,
    atualizado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_atualizacao_status_os_ordem_servico_id ON atualizacao_status_os(ordem_servico_id);
