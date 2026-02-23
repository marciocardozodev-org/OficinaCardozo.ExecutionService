using Xunit;
using FluentAssertions;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;
using OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.Domain
{
    /// <summary>
    /// Testes unitários para modelos de domínio.
    /// Cobertura: Validações de regras de negócio, invariantes de agregados.
    /// </summary>
    public class DomainModelTests
    {
        #region ExecucaoOs Tests

        [Fact]
        public void ExecucaoOs_Construtor_DeveInializarComValoresPadrao()
        {
            // Arrange & Act
            var execucao = new ExecucaoOs
            {
                OrdemServicoId = 100,
                StatusAtual = "Fila"
            };

            // Assert
            execucao.OrdemServicoId.Should().Be(100);
            execucao.StatusAtual.Should().Be("Fila");
            execucao.Finalizado.Should().BeFalse();
            execucao.Diagnostico.Should().BeNull();
            execucao.Reparo.Should().BeNull();
        }

        [Fact]
        public void ExecucaoOs_DevePodeTerStatusAtualizado()
        {
            // Arrange
            var execucao = new ExecucaoOs { StatusAtual = "Fila" };

            // Act
            execucao.StatusAtual = "Em Diagnóstico";

            // Assert
            execucao.StatusAtual.Should().Be("Em Diagnóstico");
        }

        [Fact]
        public void ExecucaoOs_DevePermitirDefinirDiagnosticoparaNull()
        {
            // Arrange
            var execucao = new ExecucaoOs { Diagnostico = "Problema encontrado" };

            // Act
            execucao.Diagnostico = null;

            // Assert
            execucao.Diagnostico.Should().BeNull();
        }

        [Fact]
        public void ExecucaoOs_DevePermitirDefinirReparoComTexto()
        {
            // Arrange
            var execucao = new ExecucaoOs();
            var reparo = "Substituir componente defeituoso";

            // Act
            execucao.Reparo = reparo;

            // Assert
            execucao.Reparo.Should().Be(reparo);
        }

        [Fact]
        public void ExecucaoOs_FinalizadoDeveSerAtualizavelParaTrue()
        {
            // Arrange
            var execucao = new ExecucaoOs { Finalizado = false };

            // Act
            execucao.Finalizado = true;

            // Assert
            execucao.Finalizado.Should().BeTrue();
        }

        [Fact]
        public void ExecucaoOs_DevePermitirDatasNulas()
        {
            // Arrange
            var execucao = new ExecucaoOs
            {
                InicioExecucao = null,
                FimExecucao = null
            };

            // Assert
            execucao.InicioExecucao.Should().BeNull();
            execucao.FimExecucao.Should().BeNull();
        }

        [Fact]
        public void ExecucaoOs_DeveArmazenarDatasComTimestamp()
        {
            // Arrange
            var agora = DateTime.UtcNow;
            var execucao = new ExecucaoOs
            {
                InicioExecucao = agora,
                FimExecucao = agora.AddHours(2)
            };

            // Assert
            execucao.InicioExecucao.Should().Be(agora);
            execucao.FimExecucao.Should().Be(agora.AddHours(2));
        }

        #endregion

        #region AtualizacaoStatusOs Tests

        [Fact]
        public void AtualizacaoStatusOs_DeveArmazenarOrdemServicoId()
        {
            // Arrange & Act
            var atualizacao = new AtualizacaoStatusOs
            {
                OrdemServicoId = 150,
                NovoStatus = "Em Progresso"
            };

            // Assert
            atualizacao.OrdemServicoId.Should().Be(150);
        }

        [Fact]
        public void AtualizacaoStatusOs_DeveRegistrarTimestampDeAtualizacao()
        {
            // Arrange
            var atualizacao = new AtualizacaoStatusOs();
            var agora = DateTime.UtcNow;

            // Act
            atualizacao.AtualizadoEm = agora;

            // Assert
            atualizacao.AtualizadoEm.Should().Be(agora);
        }

        [Fact]
        public void AtualizacaoStatusOs_DeveArmazenarStatusVazio()
        {
            // Arrange & Act
            var atualizacao = new AtualizacaoStatusOs { NovoStatus = "" };

            // Assert
            atualizacao.NovoStatus.Should().Be("");
        }

        #endregion

        #region ExecutionJob Tests

        [Fact]
        public void ExecutionJob_DeveIniciarComStatusQueued()
        {
            // Arrange & Act
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithStatus(ExecutionStatus.Queued)
                .Build();

            // Assert
            job.Status.Should().Be(ExecutionStatus.Queued);
        }

        [Fact]
        public void ExecutionJob_DeveManterOsIdUnico()
        {
            // Arrange & Act
            var job1 = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-001")
                .Build();
            var job2 = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-001")
                .Build();

            // Assert
            job1.OsId.Should().Be(job2.OsId);
        }

        [Fact]
        public void ExecutionJob_DeveAumentarAttempt()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithAttempt(1)
                .Build();

            // Act
            job.Attempt = 2;

            // Assert
            job.Attempt.Should().Be(2);
        }

        [Fact]
        public void ExecutionJob_DevePodeTerLastError()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithLastError(null)
                .Build();

            // Act
            job.LastError = "Erro de conexão com banco de dados";

            // Assert
            job.LastError.Should().Contain("Erro de conexão");
        }

        [Fact]
        public void ExecutionJob_DeveManterCorrelationIdParaRastreamento()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();

            // Act
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithCorrelationId(correlationId)
                .Build();

            // Assert
            job.CorrelationId.Should().Be(correlationId);
        }

        [Fact]
        public void ExecutionJob_DeveTransicionarParaDiagnosing()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithStatus(ExecutionStatus.Queued)
                .Build();

            // Act
            job.Status = ExecutionStatus.Diagnosing;

            // Assert
            job.Status.Should().Be(ExecutionStatus.Diagnosing);
        }

        [Fact]
        public void ExecutionJob_DeveTransicionarParaRepairing()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithStatus(ExecutionStatus.Diagnosing)
                .Build();

            // Act
            job.Status = ExecutionStatus.Repairing;

            // Assert
            job.Status.Should().Be(ExecutionStatus.Repairing);
        }

        [Fact]
        public void ExecutionJob_DeveTransicionarParaFinished()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithStatus(ExecutionStatus.Repairing)
                .WithFinishedAt(null)
                .Build();

            // Act
            job.Status = ExecutionStatus.Finished;
            job.FinishedAt = DateTime.UtcNow;

            // Assert
            job.Status.Should().Be(ExecutionStatus.Finished);
            job.FinishedAt.Should().NotBeNull();
        }

        [Fact]
        public void ExecutionJob_DeveTransicionarParaCanceled()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithStatus(ExecutionStatus.Diagnosing)
                .Build();

            // Act
            job.Status = ExecutionStatus.Canceled;

            // Assert
            job.Status.Should().Be(ExecutionStatus.Canceled);
        }

        [Fact]
        public void ExecutionJob_DeveTransicionarParaFailed()
        {
            // Arrange
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithStatus(ExecutionStatus.Repairing)
                .WithLastError(null)
                .Build();

            // Act
            job.Status = ExecutionStatus.Failed;
            job.LastError = "Peça não disponível";

            // Assert
            job.Status.Should().Be(ExecutionStatus.Failed);
            job.LastError.Should().Be("Peça não disponível");
        }

        #endregion

        #region InboxEvent Tests

        [Fact]
        public void InboxEvent_DeveArmazenarEventId()
        {
            // Arrange & Act
            var evt = new TestFixtures.InboxEventBuilder()
                .WithEventId("event-123")
                .Build();

            // Assert
            evt.EventId.Should().Be("event-123");
        }

        [Fact]
        public void InboxEvent_DeveClassificarEventType()
        {
            // Arrange & Act
            var evt = new TestFixtures.InboxEventBuilder()
                .WithEventType("PaymentConfirmed")
                .Build();

            // Assert
            evt.EventType.Should().Be("PaymentConfirmed");
        }

        [Fact]
        public void InboxEvent_DeveRegistrarTimestampDeRecebimento()
        {
            // Arrange
            var agora = DateTime.UtcNow;

            // Act
            var evt = new TestFixtures.InboxEventBuilder()
                .WithReceivedAt(agora)
                .Build();

            // Assert
            evt.ReceivedAt.Should().Be(agora);
        }

        #endregion

        #region OutboxEvent Tests

        [Fact]
        public void OutboxEvent_DeveIniciarComPublishedFalse()
        {
            // Arrange & Act
            var evt = new TestFixtures.OutboxEventBuilder()
                .WithPublished(false)
                .Build();

            // Assert
            evt.Published.Should().BeFalse();
        }

        [Fact]
        public void OutboxEvent_DeveArmazenarPayloadJson()
        {
            // Arrange
            var payload = "{\"OsId\": \"OS-001\", \"Status\": \"Queued\"}";

            // Act
            var evt = new TestFixtures.OutboxEventBuilder()
                .WithPayload(payload)
                .Build();

            // Assert
            evt.Payload.Should().Contain("OsId");
            evt.Payload.Should().Contain("OS-001");
        }

        [Fact]
        public void OutboxEvent_DeveTransicionarParaPublished()
        {
            // Arrange
            var evt = new TestFixtures.OutboxEventBuilder()
                .WithPublished(false)
                .WithPublishedAt(null)
                .Build();

            // Act
            evt.Published = true;
            evt.PublishedAt = DateTime.UtcNow;

            // Assert
            evt.Published.Should().BeTrue();
            evt.PublishedAt.Should().NotBeNull();
        }

        [Fact]
        public void OutboxEvent_DeveTypeEventCorretamente()
        {
            // Arrange & Act
            var evt = new TestFixtures.OutboxEventBuilder()
                .WithEventType("ExecutionStarted")
                .Build();

            // Assert
            evt.EventType.Should().Be("ExecutionStarted");
        }

        #endregion

        #region ExecutionStatus Enum Tests

        [Fact]
        public void ExecutionStatus_DeveContarTodosDosEstados()
        {
            // Assert
            var states = Enum.GetValues(typeof(ExecutionStatus)).Cast<ExecutionStatus>().ToList();
            states.Should().Contain(new[] { 
                ExecutionStatus.Queued, 
                ExecutionStatus.Diagnosing, 
                ExecutionStatus.Repairing,
                ExecutionStatus.Finished, 
                ExecutionStatus.Failed, 
                ExecutionStatus.Canceled 
            });
        }

        #endregion
    }
}
