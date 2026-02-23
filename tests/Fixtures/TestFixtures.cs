using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.EXECUTIONSERVICE;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures
{
    /// <summary>
    /// Classe utilitária para criar instâncias de DbContext em memória para testes.
    /// </summary>
    public static class TestFixtures
    {
        /// <summary>
        /// Cria um ExecutionDbContext em memória para testes.
        /// </summary>
        public static ExecutionDbContext CreateInMemoryDbContext(string databaseName = null)
        {
            databaseName ??= Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ExecutionDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;

            var context = new ExecutionDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        /// <summary>
        /// Builder para criar instâncias de ExecucaoOs com valores padrão.
        /// </summary>
        public class ExecucaoOsBuilder
        {
            private int _id = 1;
            private int _ordemServicoId = 100;
            private string _statusAtual = "Fila";
            private DateTime? _inicioExecucao = null;
            private DateTime? _fimExecucao = null;
            private string _diagnostico = null;
            private string _reparo = null;
            private bool _finalizado = false;

            public ExecucaoOsBuilder WithId(int id)
            {
                _id = id;
                return this;
            }

            public ExecucaoOsBuilder WithOrdemServicoId(int ordemServicoId)
            {
                _ordemServicoId = ordemServicoId;
                return this;
            }

            public ExecucaoOsBuilder WithStatusAtual(string statusAtual)
            {
                _statusAtual = statusAtual;
                return this;
            }

            public ExecucaoOsBuilder WithInicioExecucao(DateTime? inicioExecucao)
            {
                _inicioExecucao = inicioExecucao;
                return this;
            }

            public ExecucaoOsBuilder WithFimExecucao(DateTime? fimExecucao)
            {
                _fimExecucao = fimExecucao;
                return this;
            }

            public ExecucaoOsBuilder WithDiagnostico(string diagnostico)
            {
                _diagnostico = diagnostico;
                return this;
            }

            public ExecucaoOsBuilder WithReparo(string reparo)
            {
                _reparo = reparo;
                return this;
            }

            public ExecucaoOsBuilder WithFinalizado(bool finalizado)
            {
                _finalizado = finalizado;
                return this;
            }

            public ExecucaoOs Build()
            {
                return new ExecucaoOs
                {
                    Id = _id,
                    OrdemServicoId = _ordemServicoId,
                    StatusAtual = _statusAtual,
                    InicioExecucao = _inicioExecucao,
                    FimExecucao = _fimExecucao,
                    Diagnostico = _diagnostico,
                    Reparo = _reparo,
                    Finalizado = _finalizado
                };
            }
        }

        /// <summary>
        /// Builder para criar instâncias de ExecutionJob com valores padrão.
        /// </summary>
        public class ExecutionJobBuilder
        {
            private Guid _id = Guid.NewGuid();
            private string _osId = "OS-001";
            private ExecutionStatus _status = ExecutionStatus.Queued;
            private int _attempt = 1;
            private string _lastError = null;
            private DateTime _createdAt = DateTime.UtcNow;
            private DateTime? _updatedAt = null;
            private DateTime? _finishedAt = null;
            private string _correlationId = Guid.NewGuid().ToString();

            public ExecutionJobBuilder WithId(Guid id)
            {
                _id = id;
                return this;
            }

            public ExecutionJobBuilder WithOsId(string osId)
            {
                _osId = osId;
                return this;
            }

            public ExecutionJobBuilder WithStatus(ExecutionStatus status)
            {
                _status = status;
                return this;
            }

            public ExecutionJobBuilder WithAttempt(int attempt)
            {
                _attempt = attempt;
                return this;
            }

            public ExecutionJobBuilder WithLastError(string lastError)
            {
                _lastError = lastError;
                return this;
            }

            public ExecutionJobBuilder WithCreatedAt(DateTime createdAt)
            {
                _createdAt = createdAt;
                return this;
            }

            public ExecutionJobBuilder WithUpdatedAt(DateTime? updatedAt)
            {
                _updatedAt = updatedAt;
                return this;
            }

            public ExecutionJobBuilder WithFinishedAt(DateTime? finishedAt)
            {
                _finishedAt = finishedAt;
                return this;
            }

            public ExecutionJobBuilder WithCorrelationId(string correlationId)
            {
                _correlationId = correlationId;
                return this;
            }

            public ExecutionJob Build()
            {
                return new ExecutionJob
                {
                    Id = _id,
                    OsId = _osId,
                    Status = _status,
                    Attempt = _attempt,
                    LastError = _lastError,
                    CreatedAt = _createdAt,
                    UpdatedAt = _updatedAt,
                    FinishedAt = _finishedAt,
                    CorrelationId = _correlationId
                };
            }
        }

        /// <summary>
        /// Builder para criar instâncias de InboxEvent.
        /// </summary>
        public class InboxEventBuilder
        {
            private Guid _id = Guid.NewGuid();
            private string _eventId = $"event-{Guid.NewGuid()}";
            private string _eventType = "TestEvent";
            private DateTime _receivedAt = DateTime.UtcNow;

            public InboxEventBuilder WithId(Guid id)
            {
                _id = id;
                return this;
            }

            public InboxEventBuilder WithEventId(string eventId)
            {
                _eventId = eventId;
                return this;
            }

            public InboxEventBuilder WithEventType(string eventType)
            {
                _eventType = eventType;
                return this;
            }

            public InboxEventBuilder WithReceivedAt(DateTime receivedAt)
            {
                _receivedAt = receivedAt;
                return this;
            }

            public InboxEvent Build()
            {
                return new InboxEvent
                {
                    Id = _id,
                    EventId = _eventId,
                    EventType = _eventType,
                    ReceivedAt = _receivedAt
                };
            }
        }

        /// <summary>
        /// Builder para criar instâncias de OutboxEvent.
        /// </summary>
        public class OutboxEventBuilder
        {
            private Guid _id = Guid.NewGuid();
            private string _eventType = "TestEvent";
            private string _payload = "{}";
            private DateTime _createdAt = DateTime.UtcNow;
            private bool _published = false;
            private DateTime? _publishedAt = null;

            public OutboxEventBuilder WithId(Guid id)
            {
                _id = id;
                return this;
            }

            public OutboxEventBuilder WithEventType(string eventType)
            {
                _eventType = eventType;
                return this;
            }

            public OutboxEventBuilder WithPayload(string payload)
            {
                _payload = payload;
                return this;
            }

            public OutboxEventBuilder WithCreatedAt(DateTime createdAt)
            {
                _createdAt = createdAt;
                return this;
            }

            public OutboxEventBuilder WithPublished(bool published)
            {
                _published = published;
                return this;
            }

            public OutboxEventBuilder WithPublishedAt(DateTime? publishedAt)
            {
                _publishedAt = publishedAt;
                return this;
            }

            public OutboxEvent Build()
            {
                return new OutboxEvent
                {
                    Id = _id,
                    EventType = _eventType,
                    Payload = _payload,
                    CreatedAt = _createdAt,
                    Published = _published,
                    PublishedAt = _publishedAt
                };
            }
        }

        /// <summary>
        /// Builder para criar DTOs do controller.
        /// </summary>
        public class CriarExecucaoDtoBuilder
        {
            private int _ordemServicoId = 100;

            public CriarExecucaoDtoBuilder WithOrdemServicoId(int ordemServicoId)
            {
                _ordemServicoId = ordemServicoId;
                return this;
            }

            public dynamic Build() => new { OrdemServicoId = _ordemServicoId };
        }
    }
}
