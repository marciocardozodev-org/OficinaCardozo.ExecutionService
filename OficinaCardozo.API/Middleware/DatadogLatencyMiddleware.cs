using Microsoft.AspNetCore.Http;
using System.Diagnostics; // Para Stopwatch
using System.Threading.Tasks;
using System;
using System.Linq;
using StatsdClient; // Para Metrics

namespace OficinaCardozo.API.Middleware
{
    public class DatadogLatencyMiddleware
    {
        private readonly RequestDelegate _next;

        public DatadogLatencyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _next(context);
            stopwatch.Stop();

            var latencyMs = stopwatch.Elapsed.TotalMilliseconds;
            var path = context.Request.Path.HasValue ? context.Request.Path.Value : "unknown";
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var host = Environment.MachineName;

            // Tags para facilitar análise no Datadog
            var tags = new[]
            {
                $"env:dev", // ajuste conforme ambiente
                $"endpoint:{path}",
                $"method:{method}",
                $"status:{statusCode}"
            };

            // Envio de métrica customizada via DogStatsD
            try
            {
                // Inicializa o Metrics apenas uma vez (ideal: singleton via DI, aqui para exemplo)
                // Não existe IsConfigured, então use um static flag se necessário (ou sempre configure, pois é idempotente)
                // Configuração do Metrics agora é global no Program.cs
                Metrics.Timer("api.latency.ms", (int)latencyMs);
                // Envio de métrica teste para diagnóstico
                Metrics.Timer("test.metric.timer", 42);
                Serilog.Log.Warning("[DatadogLatencyMiddleware] Métrica enviada via DogStatsD: {Metric} {Latency}ms {Tags}", "api.latency.ms", latencyMs, string.Join(",", tags));
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Falha ao enviar métrica via DogStatsD");
            }

            // Log local de latência
            Serilog.Log.Warning("[DatadogLatencyMiddleware] Latência: {Metric} {Latency}ms {Path} {Method} {Status}", "api.latency.ms", latencyMs, path, method, statusCode);
            Console.WriteLine($"[DatadogLatencyMiddleware] (Console) Latência: api.latency.ms {latencyMs}ms {path} {method} {statusCode}");
        }
    }
}
