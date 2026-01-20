using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using System;
using System.Linq;

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
            var stopwatch = Stopwatch.StartNew();
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


            // Log explícito antes do envio
            Serilog.Log.Warning("[DatadogLatencyMiddleware] Enviando métrica: {Metric} {Latency}ms {Path} {Method} {Status}", "api.latency.ms", latencyMs, path, method, statusCode);
            Console.WriteLine($"[DatadogLatencyMiddleware] (Console) Enviando métrica: api.latency.ms {latencyMs}ms {path} {method} {statusCode}");

            // Envia a métrica de latência para o Datadog
            try
            {
                var datadog = new Integrations.DatadogApiClient();
                await datadog.SendMetricAsync(
                    metricName: "api.latency.ms",
                    value: latencyMs,
                    host: host,
                    tags: tags
                );
                Serilog.Log.Warning("[DatadogLatencyMiddleware] Métrica enviada com sucesso para o Datadog.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Falha ao enviar métrica de latência para o Datadog");
            }
        }
    }
}
