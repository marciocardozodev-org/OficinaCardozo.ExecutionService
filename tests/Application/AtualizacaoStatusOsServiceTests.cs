using Xunit;
using FluentAssertions;
using OFICINACARDOZO.EXECUTIONSERVICE.Application;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;
using OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.Application
{
    /// <summary>
    /// Testes unitários para AtualizacaoStatusOsService.
    /// Cobertura: Registro e listagem de atualizações de status.
    /// </summary>
    public class AtualizacaoStatusOsServiceTests
    {
        [Fact]
        public void AtualizarStatus_ComDadosValidos_DeveRetornarAtualizacao()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            const int ordemServicoId = 100;
            const string novoStatus = "Em Diagnóstico";
            var beforeCall = DateTime.UtcNow;

            // Act
            var resultado = service.AtualizarStatus(ordemServicoId, novoStatus);

            // Assert
            resultado.Should().NotBeNull();
            resultado.OrdemServicoId.Should().Be(ordemServicoId);
            resultado.NovoStatus.Should().Be(novoStatus);
            resultado.AtualizadoEm.Should().BeOnOrAfter(beforeCall);
            resultado.AtualizadoEm.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public void AtualizarStatus_ComMultiplosStatus_DeveRejistrarTodos()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            const int ordemServicoId = 101;

            // Act
            var atualizacao1 = service.AtualizarStatus(ordemServicoId, "Fila");
            var atualizacao2 = service.AtualizarStatus(ordemServicoId, "Em Diagnóstico");
            var atualizacao3 = service.AtualizarStatus(ordemServicoId, "Finalizado");

            // Assert
            var listagem = service.ListarPorOrdem(ordemServicoId).ToList();
            listagem.Should().HaveCount(3);
            listagem[0].NovoStatus.Should().Be("Fila");
            listagem[1].NovoStatus.Should().Be("Em Diagnóstico");
            listagem[2].NovoStatus.Should().Be("Finalizado");
        }

        [Fact]
        public void AtualizarStatus_ComStatusVazio_DeveAceitarERegistrar()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            const int ordemServicoId = 102;

            // Act
            var resultado = service.AtualizarStatus(ordemServicoId, "");

            // Assert
            resultado.NovoStatus.Should().Be("");
            resultado.OrdemServicoId.Should().Be(ordemServicoId);
        }

        [Fact]
        public void ListarPorOrdem_ComOrdensSeparadas_DeveRetornarApenasOsDaOrderSolicitada()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            
            // Act
            service.AtualizarStatus(100, "Status1");
            service.AtualizarStatus(100, "Status2");
            service.AtualizarStatus(200, "Status3");
            service.AtualizarStatus(200, "Status4");
            service.AtualizarStatus(200, "Status5");

            // Assert
            var ordem100 = service.ListarPorOrdem(100).ToList();
            var ordem200 = service.ListarPorOrdem(200).ToList();
            
            ordem100.Should().HaveCount(2);
            ordem200.Should().HaveCount(3);
            ordem100.All(a => a.OrdemServicoId == 100).Should().BeTrue();
            ordem200.All(a => a.OrdemServicoId == 200).Should().BeTrue();
        }

        [Fact]
        public void ListarPorOrdem_ComOrdemInexistente_DeveRetornarListaVazia()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();

            // Act
            var resultado = service.ListarPorOrdem(999);

            // Assert
            resultado.Should().BeEmpty();
        }

        [Fact]
        public void AtualizarStatus_MantémOrdenCronologica()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            const int ordemServicoId = 103;

            // Act
            var primeira = service.AtualizarStatus(ordemServicoId, "Status1");
            var segunda = service.AtualizarStatus(ordemServicoId, "Status2");
            var terceira = service.AtualizarStatus(ordemServicoId, "Status3");

            // Assert
            var listagem = service.ListarPorOrdem(ordemServicoId).ToList();
            listagem[0].AtualizadoEm.Should().BeOnOrBefore(listagem[1].AtualizadoEm);
            listagem[1].AtualizadoEm.Should().BeOnOrBefore(listagem[2].AtualizadoEm);
        }

        [Fact]
        public void AtualizarStatus_ComMesmaOrderMultiplasVezes_SeparandoPorTempo()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            const int ordemServicoId = 104;

            // Act
            var primeira = service.AtualizarStatus(ordemServicoId, "Em andamento");
            System.Threading.Thread.Sleep(10);
            var segunda = service.AtualizarStatus(ordemServicoId, "Pausado");
            System.Threading.Thread.Sleep(10);
            var terceira = service.AtualizarStatus(ordemServicoId, "Retomado");

            // Assert
            primeira.AtualizadoEm.Should().BeBefore(segunda.AtualizadoEm);
            segunda.AtualizadoEm.Should().BeBefore(terceira.AtualizadoEm);
        }

        [Fact]
        public void AtualizarStatus_ComStatusNull_DeveAceitarNull()
        {
            // Arrange
            var service = new AtualizacaoStatusOsService();
            const int ordemServicoId = 105;
            
            // Act
#pragma warning disable CS8625
            var resultado = service.AtualizarStatus(ordemServicoId, null);
#pragma warning restore CS8625

            // Assert
            resultado.NovoStatus.Should().BeNull();
        }
    }
}
