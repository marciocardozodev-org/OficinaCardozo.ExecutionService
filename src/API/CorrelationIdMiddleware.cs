using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System;
using System.Threading.Tasks;

namespace OficinaCardozo.ExecutionService.API
{
    /// <summary>
    /// Middleware para gerenciar CorrelationId em toda a requisição.
    /// Lê o header Correlation-Id ou gera um novo GUID se não existir.
    /// Retorna o CorrelationId no response header e injeta no contexto de logs.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeaderName = "Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId;

            // Tentar obter CorrelationId do header da requisição
            if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue))
            {
                correlationId = headerValue.ToString();
            }
            else
            {
                // Gerar novo CorrelationId
                correlationId = Guid.NewGuid().ToString();
            }

            // Adicionar ao contexto da requisição
            context.Items["CorrelationId"] = correlationId;

            // Adicionar ao response header
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;

            // Adicionar ao contexto de logs do Serilog
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }
}