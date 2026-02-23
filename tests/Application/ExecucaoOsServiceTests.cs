using Xunit;
using FluentAssertions;
using OFICINACARDOZO.EXECUTIONSERVICE;
using OFICINACARDOZO.EXECUTIONSERVICE.Application;
using OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.Application
{
    /// <summary>
    /// Testes unitários para ExecucaoOsService.
    /// Cobertura: Criação, leitura e atualização de execuções de OS.
    /// </summary>
    public class ExecucaoOsServiceTests
    {
        [Fact]
        public void CriarExecucao_ComOrdemServicoIdValido_DeveCriarComStatusFila()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            const int ordemServicoId = 100;

            // Act
            var resultado = service.CriarExecucao(ordemServicoId);

            // Assert
            resultado.Should().NotBeNull();
            resultado.OrdemServicoId.Should().Be(ordemServicoId);
            resultado.StatusAtual.Should().Be("Fila");
            resultado.Finalizado.Should().BeFalse();
            resultado.Diagnostico.Should().BeNull();
            resultado.Reparo.Should().BeNull();
            resultado.InicioExecucao.Should().BeNull();
            resultado.FimExecucao.Should().BeNull();
        }

        [Fact]
        public void CriarExecucao_DevePersistitNoDbContext()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            const int ordemServicoId = 101;

            // Act
            service.CriarExecucao(ordemServicoId);

            // Assert
            dbContext.ExecucoesOs.Should().HaveCount(1);
            var execucaoSalva = dbContext.ExecucoesOs.First();
            execucaoSalva.OrdemServicoId.Should().Be(ordemServicoId);
        }

        [Fact]
        public void CriarExecucao_ComMultiplasOrdens_DeveAceitarTodas()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);

            // Act
            var execucao1 = service.CriarExecucao(100);
            var execucao2 = service.CriarExecucao(101);
            var execucao3 = service.CriarExecucao(102);

            // Assert
            dbContext.ExecucoesOs.Should().HaveCount(3);
            execucao1.OrdemServicoId.Should().Be(100);
            execucao2.OrdemServicoId.Should().Be(101);
            execucao3.OrdemServicoId.Should().Be(102);
        }

        [Fact]
        public void ObterExecucao_ComIdExistente_DeveRetornarExecucao()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            var created = service.CriarExecucao(150);

            // Act
            var obtida = service.ObterExecucao(150);

            // Assert
            obtida.Should().NotBeNull();
            obtida?.OrdemServicoId.Should().Be(150);
            obtida?.Id.Should().Be(created.Id);
        }

        [Fact]
        public void ObterExecucao_ComIdInexistente_DeveRetornarNull()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);

            // Act
            var obtida = service.ObterExecucao(999);

            // Assert
            obtida.Should().BeNull();
        }

        [Fact]
        public void AtualizarStatus_ComStatusValido_DeveAtualizar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(200);
            const string novoStatus = "Em Diagnóstico";

            // Act
            service.AtualizarStatus(200, novoStatus);

            // Assert
            var execucao = dbContext.ExecucoesOs.First();
            execucao.StatusAtual.Should().Be(novoStatus);
        }

        [Fact]
        public void AtualizarStatus_ParaEmDiagnostico_DeveDefinirInicioExecucao()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(201);
            var beforeCall = DateTime.UtcNow;

            // Act
            service.AtualizarStatus(201, "Em Diagnóstico");

            // Assert
            var execucao = dbContext.ExecucoesOs.First();
            execucao.InicioExecucao.Should().NotBeNull();
            execucao.InicioExecucao.Should().BeOnOrAfter(beforeCall);
        }

        [Fact]
        public void AtualizarStatus_ParaFinalizado_DeveDefinirFimExecucaoEFinalizadoTrue()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(202);
            var beforeCall = DateTime.UtcNow;

            // Act
            service.AtualizarStatus(202, "Finalizado");

            // Assert
            var execucao = dbContext.ExecucoesOs.First();
            execucao.StatusAtual.Should().Be("Finalizado");
            execucao.FimExecucao.Should().NotBeNull();
            execucao.FimExecucao.Should().BeOnOrAfter(beforeCall);
            execucao.Finalizado.Should().BeTrue();
        }

        [Fact]
        public void AtualizarStatus_ComIdInexistente_NaoDeveLancarExcecao()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);

            // Act & Assert
            service.Invoking(s => s.AtualizarStatus(999, "Novo Status"))
                .Should().NotThrow();
        }

        [Fact]
        public void AtualizarDiagnostico_ComDiagnosticoValido_DeveAtualizar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(300);
            const string diagnostico = "Problema no motor";

            // Act
            service.AtualizarDiagnostico(300, diagnostico);

            // Assert
            var execucao = dbContext.ExecucoesOs.First();
            execucao.Diagnostico.Should().Be(diagnostico);
        }

        [Fact]
        public void AtualizarDiagnostico_ComDiagnosticoVazio_DeveAtualizar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(301);

            // Act
            service.AtualizarDiagnostico(301, "");

            // Assert
            var execucao = dbContext.ExecucoesOs.First();
            execucao.Diagnostico.Should().Be("");
        }

        [Fact]
        public void AtualizarDiagnostico_ComIdInexistente_NaoDeveLancarExcecao()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);

            // Act & Assert
            service.Invoking(s => s.AtualizarDiagnostico(999, "Diagnostico teste"))
                .Should().NotThrow();
        }

        [Fact]
        public void AtualizarReparo_ComReparoValido_DeveAtualizar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(400);
            const string reparo = "Troca do motor";

            // Act
            service.AtualizarReparo(400, reparo);

            // Assert
            var execucao = dbContext.ExecucoesOs.First();
            execucao.Reparo.Should().Be(reparo);
        }

        [Fact]
        public void ObterReparo_DeveRetornarReparoAtualizado()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new ExecucaoOsService(dbContext);
            service.CriarExecucao(401);
            const string reparo = "Reparo concluído";

            // Act
            service.AtualizarReparo(401, reparo);
            var execucao = service.ObterExecucao(401);

            // Assert
            execucao?.Reparo.Should().Be(reparo);
        }
    }
}
