using System;
using Microsoft.Extensions.Logging;

namespace OficinaCardozo.ExecutionService.Messaging
{
    public static class CorrelationIdProvider
    {
        public static void LogWithCorrelationId(ILogger logger, string correlationId, string message)
        {
            logger.LogInformation($"[CorrelationId: {correlationId}] {message}");
        }
    }
}
