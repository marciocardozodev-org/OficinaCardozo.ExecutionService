CREATE TABLE atualizacao_status_os (
    id SERIAL PRIMARY KEY,
    ordem_servico_id INTEGER NOT NULL,
    novo_status VARCHAR(100) NOT NULL,
    atualizado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX idx_atualizacao_status_os_ordem_servico_id ON atualizacao_status_os(ordem_servico_id);

-- Tabela: execucao_os
CREATE TABLE execucao_os (
    id SERIAL PRIMARY KEY,
    ordem_servico_id INTEGER NOT NULL,
    status_atual VARCHAR(100) NOT NULL,
    inicio_execucao TIMESTAMP NULL,
    fim_execucao TIMESTAMP NULL,
    diagnostico TEXT NULL,
    reparo TEXT NULL,
    finalizado BOOLEAN NOT NULL DEFAULT FALSE
);
CREATE INDEX idx_execucao_os_ordem_servico_id ON execucao_os(ordem_servico_id);
