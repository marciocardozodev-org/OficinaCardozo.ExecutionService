using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using OficinaCardozo.ExecutionService.API;
using OFICINACARDOZO.EXECUTIONSERVICE.API;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.API
{
    /// <summary>
    /// Testes unitários para Middlewares.
    /// Cobertura: CorrelationIdMiddleware, ExceptionHandlingMiddleware.
    /// </summary>
    public class MiddlewareTests
    {
        #region CorrelationIdMiddleware Tests

        [Fact]
        public async Task CorrelationIdMiddleware_ComHeaderExistente_DeveUsarValorDoHeader()
        {
            // Arrange
            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next);
            var httpContext = new DefaultHttpContext();
            var correlationIdEsperado = "correlation-123";
            httpContext.Request.Headers["Correlation-Id"] = correlationIdEsperado;

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Items["CorrelationId"].Should().Be(correlationIdEsperado);
            httpContext.Response.Headers["Correlation-Id"].Should().Contain(correlationIdEsperado);
        }

        [Fact]
        public async Task CorrelationIdMiddleware_SemHeader_DeveCriarNovoGuid()
        {
            // Arrange
            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next);
            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            var correlationId = httpContext.Items["CorrelationId"]?.ToString();
            correlationId.Should().NotBeNullOrEmpty();
            Guid.TryParse(correlationId, out _).Should().BeTrue();
        }

        [Fact]
        public async Task CorrelationIdMiddleware_DeveAdicionarAoResponseHeaders()
        {
            // Arrange
            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next);
            var httpContext = new DefaultHttpContext();
            var correlationId = Guid.NewGuid().ToString();
            httpContext.Request.Headers["Correlation-Id"] = correlationId;

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Response.Headers.Should().ContainKey("Correlation-Id");
            httpContext.Response.Headers["Correlation-Id"].ToString().Should().Be(correlationId);
        }

        [Fact]
        public async Task CorrelationIdMiddleware_DeveInvocarProximoMiddleware()
        {
            // Arrange
            var proximoMiddlewareFoiChamado = false;
            RequestDelegate proximoMiddleware = ctx =>
            {
                proximoMiddlewareFoiChamado = true;
                return Task.CompletedTask;
            };

            var middleware = new CorrelationIdMiddleware(proximoMiddleware);
            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            proximoMiddlewareFoiChamado.Should().BeTrue();
        }

        [Fact]
        public async Task CorrelationIdMiddleware_ComMultiplasRequisicoes_DeveCriarIdsDiferentes()
        {
            // Arrange
            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next);
            var httpContext1 = new DefaultHttpContext();
            var httpContext2 = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext1);
            await middleware.InvokeAsync(httpContext2);

            // Assert
            var id1 = httpContext1.Items["CorrelationId"]?.ToString();
            var id2 = httpContext2.Items["CorrelationId"]?.ToString();
            id1.Should().NotBe(id2);
        }

        #endregion

        #region ExceptionHandlingMiddleware Tests

        [Fact]
        public async Task ExceptionHandlingMiddleware_SemExcecao_DevePassarAdianteNormalmente()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new ExceptionHandlingMiddleware(next, loggerMock.Object);
            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Response.StatusCode.Should().NotBe(500);
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_ComExcecao_DeveRetornar500()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            RequestDelegate proximoMiddlewareQueThrow = (HttpContext ctx) => throw new Exception("Erro de teste");

            var middleware = new ExceptionHandlingMiddleware(proximoMiddlewareQueThrow, loggerMock.Object);
            var httpContext = new DefaultHttpContext();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Response.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_ComExcecao_DeveRetornarJsonComErro()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var excecaoEsperada = "Erro crítico do sistema";
            RequestDelegate proximoMiddlewareQueThrow = (HttpContext ctx) => throw new Exception(excecaoEsperada);

            var middleware = new ExceptionHandlingMiddleware(proximoMiddlewareQueThrow, loggerMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Response.ContentType.Should().Be("application/json");
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var bodyLeitor = new StreamReader(httpContext.Response.Body);
            var bodyString = await bodyLeitor.ReadToEndAsync();
            bodyString.Should().Contain("Erro interno no servidor");
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_DeveLogarExcecao()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var exception = new Exception("Erro de teste");
            RequestDelegate proximoMiddlewareQueThrow = (HttpContext ctx) => throw exception;

            var middleware = new ExceptionHandlingMiddleware(proximoMiddlewareQueThrow, loggerMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_ComDiversasExcecoes_DeveHandleTodasComErro500()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var excecoes = new[]
            {
                new InvalidOperationException("Operação inválida"),
                new ArgumentException("Argumento inválido"),
                new ApplicationException("Erro de aplicação"),
                new Exception("Erro genérico")
            };

            // Act & Assert
            foreach (var excecao in excecoes)
            {
                RequestDelegate nextWithException = (HttpContext ctx) => throw excecao;
                var middleware = new ExceptionHandlingMiddleware(nextWithException, loggerMock.Object);
                var httpContext = new DefaultHttpContext();
                httpContext.Response.Body = new MemoryStream();

                await middleware.InvokeAsync(httpContext);

                httpContext.Response.StatusCode.Should().Be(500);
            }
        }

        [Fact]
        public async Task ExceptionHandlingMiddleware_ResponseJson_DeveSerValido()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            var mensagensErro = "Informação do erro";
            RequestDelegate proximoMiddlewareQueThrow = (HttpContext ctx) => throw new Exception(mensagensErro);

            var middleware = new ExceptionHandlingMiddleware(proximoMiddlewareQueThrow, loggerMock.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
            var bodyLeitor = new StreamReader(httpContext.Response.Body);
            var bodyString = await bodyLeitor.ReadToEndAsync();
            
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(bodyString);
            jsonElement.TryGetProperty("Erro", out _).Should().BeTrue();
            jsonElement.TryGetProperty("Detalhe", out _).Should().BeTrue();
        }

        #endregion
    }
}
