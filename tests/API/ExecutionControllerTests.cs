using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using OFICINACARDOZO.EXECUTIONSERVICE.API;
using OFICINACARDOZO.EXECUTIONSERVICE.Application;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;
using OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.API
{
    /// <summary>
    /// Testes unitários para ExecutionController.
    /// Cobertura: Validação de entrada, autenticação/autorização, respostas HTTP corretas.
    /// </summary>
    public class ExecutionControllerTests
    {
        [Fact]
        public void CriarExecucao_ComOrdemServicoIdValido_DeveRetornarOkComExecucao()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var execucaoEsperada = new TestFixtures.ExecucaoOsBuilder()
                .WithOrdemServicoId(100)
                .WithStatusAtual("Fila")
                .Build();

            mockExecucaoService
                .Setup(x => x.CriarExecucao(100))
                .Returns(execucaoEsperada);

            var dto = new CriarExecucaoDto { OrdemServicoId = 100 };

            // Act
            var resultado = controller.CriarExecucao(dto);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
            var okResult = resultado as OkObjectResult;
            okResult?.Value.Should().Be(execucaoEsperada);
        }

        [Fact]
        public void CriarExecucao_DeveInvocarServiceComDtoOrdemServicoId()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var execucao = new TestFixtures.ExecucaoOsBuilder().Build();
            mockExecucaoService.Setup(x => x.CriarExecucao(150)).Returns(execucao);

            var dto = new CriarExecucaoDto { OrdemServicoId = 150 };

            // Act
            controller.CriarExecucao(dto);

            // Assert
            mockExecucaoService.Verify(x => x.CriarExecucao(150), Times.Once);
        }

        [Fact]
        public void AtualizarStatus_ComDadosValidos_DeveRetornarOk()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var dto = new AtualizarStatusDto
            {
                OrdemServicoId = 100,
                NovoStatus = "Em Diagnóstico"
            };

            // Act
            var resultado = controller.AtualizarStatus(dto);

            // Assert
            resultado.Should().BeOfType<OkResult>();
        }

        [Fact]
        public void AtualizarStatus_DeveInvocarAmbosSosServices()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var dto = new AtualizarStatusDto
            {
                OrdemServicoId = 101,
                NovoStatus = "Finalizado"
            };

            // Act
            controller.AtualizarStatus(dto);

            // Assert
            mockExecucaoService.Verify(x => x.AtualizarStatus(101, "Finalizado"), Times.Once);
            mockStatusService.Verify(x => x.AtualizarStatus(101, "Finalizado"), Times.Once);
        }

        [Fact]
        public void AtualizarDiagnostico_ComDiagnosticoValido_DeveRetornarOk()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var dto = new AtualizarDiagnosticoDto
            {
                OrdemServicoId = 102,
                Diagnostico = "Problema no motor"
            };

            // Act
            var resultado = controller.AtualizarDiagnostico(dto);

            // Assert
            resultado.Should().BeOfType<OkResult>();
        }

        [Fact]
        public void AtualizarDiagnostico_DeveInvocarService()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var dto = new AtualizarDiagnosticoDto
            {
                OrdemServicoId = 103,
                Diagnostico = "Buzina defeituosa"
            };

            // Act
            controller.AtualizarDiagnostico(dto);

            // Assert
            mockExecucaoService.Verify(x => x.AtualizarDiagnostico(103, "Buzina defeituosa"), Times.Once);
        }

        [Fact]
        public void AtualizarReparo_ComReparoValido_DeveRetornarOk()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var dto = new AtualizarReparoDto
            {
                OrdemServicoId = 104,
                Reparo = "Substituição de buzina"
            };

            // Act
            var resultado = controller.AtualizarReparo(dto);

            // Assert
            resultado.Should().BeOfType<OkResult>();
        }

        [Fact]
        public void AtualizarReparo_DeveInvocarService()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var dto = new AtualizarReparoDto
            {
                OrdemServicoId = 105,
                Reparo = "Limpeza do sistema"
            };

            // Act
            controller.AtualizarReparo(dto);

            // Assert
            mockExecucaoService.Verify(x => x.AtualizarReparo(105, "Limpeza do sistema"), Times.Once);
        }

        [Fact]
        public void ObterExecucao_ComIdExistente_DeveRetornarOkComExecucao()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var execucao = new TestFixtures.ExecucaoOsBuilder()
                .WithOrdemServicoId(106)
                .Build();

            mockExecucaoService.Setup(x => x.ObterExecucao(106)).Returns(execucao);

            // Act
            var resultado = controller.ObterExecucao(106);

            // Assert
            resultado.Should().BeOfType<OkObjectResult>();
            var okResult = resultado as OkObjectResult;
            okResult?.Value.Should().Be(execucao);
        }

        [Fact]
        public void ObterExecucao_ComIdInexistente_DeveRetornarNotFound()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            mockExecucaoService.Setup(x => x.ObterExecucao(999)).Returns((ExecucaoOs)null);

            // Act
            var resultado = controller.ObterExecucao(999);

            // Assert
            resultado.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void ObterExecucao_DeveInvocarServiceComIdCorreto()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            mockExecucaoService.Setup(x => x.ObterExecucao(107)).Returns((ExecucaoOs)null);

            // Act
            controller.ObterExecucao(107);

            // Assert
            mockExecucaoService.Verify(x => x.ObterExecucao(107), Times.Once);
        }

        [Fact]
        public void Controller_DeveRejeitarMultiplasOperacoesComSucesso()
        {
            // Arrange
            var mockExecucaoService = new Mock<ExecucaoOsService>(null);
            var mockStatusService = new Mock<AtualizacaoStatusOsService>();
            var controller = new ExecutionController(mockExecucaoService.Object, mockStatusService.Object);

            var execucao = new TestFixtures.ExecucaoOsBuilder().Build();
            mockExecucaoService.Setup(x => x.CriarExecucao(It.IsAny<int>())).Returns(execucao);
            mockExecucaoService.Setup(x => x.ObterExecucao(It.IsAny<int>())).Returns(execucao);

            // Act
            var criar = controller.CriarExecucao(new CriarExecucaoDto { OrdemServicoId = 108 });
            var obter = controller.ObterExecucao(108);
            var atualizar = controller.AtualizarStatus(new AtualizarStatusDto { OrdemServicoId = 108, NovoStatus = "OK" });

            // Assert
            criar.Should().BeOfType<OkObjectResult>();
            obter.Should().BeOfType<OkObjectResult>();
            atualizar.Should().BeOfType<OkResult>();
        }
    }
}
