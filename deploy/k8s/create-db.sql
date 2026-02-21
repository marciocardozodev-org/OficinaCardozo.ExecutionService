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

-- Tabela: ExecutionJobs (nova arquitetura de processamento)
CREATE TABLE IF NOT EXISTS "ExecutionJobs" (
    "Id" UUID PRIMARY KEY,
    "OsId" VARCHAR(50) NOT NULL,
    "Status" INTEGER NOT NULL,
    "Attempt" INTEGER NOT NULL DEFAULT 1,
    "LastError" VARCHAR(500),
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP,
    "FinishedAt" TIMESTAMP,
    "CorrelationId" VARCHAR(100)
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_ExecutionJobs_OsId" ON "ExecutionJobs"("OsId");

-- Tabela: InboxEvents (idempotency pattern)
CREATE TABLE IF NOT EXISTS "InboxEvents" (
    "Id" UUID PRIMARY KEY,
    "EventId" VARCHAR(100) NOT NULL,
    "EventType" VARCHAR(100) NOT NULL,
    "ReceivedAt" TIMESTAMP NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_InboxEvents_EventId" ON "InboxEvents"("EventId");

-- Tabela: OutboxEvents (reliable publishing pattern)
CREATE TABLE IF NOT EXISTS "OutboxEvents" (
    "Id" UUID PRIMARY KEY,
    "EventType" VARCHAR(100) NOT NULL,
    "Payload" TEXT NOT NULL,
    "CorrelationId" VARCHAR(100),
    "CreatedAt" TIMESTAMP NOT NULL,
    "Published" BOOLEAN NOT NULL DEFAULT FALSE,
    "PublishedAt" TIMESTAMP
);
CREATE INDEX IF NOT EXISTS "IX_OutboxEvents_Published" ON "OutboxEvents"("Published");
