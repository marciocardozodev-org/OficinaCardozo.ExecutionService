using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OficinaCardozo.ExecutionService.EventHandlers;

namespace OFICINACARDOZO.EXECUTIONSERVICE.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TestingController : ControllerBase
    {
        private readonly PaymentConfirmedHandler _paymentHandler;
        private readonly OsCanceledHandler _osCanceledHandler;
        private readonly ILogger<TestingController> _logger;

        public TestingController(
            PaymentConfirmedHandler paymentHandler,
            OsCanceledHandler osCanceledHandler,
            ILogger<TestingController> logger)
        {
            _paymentHandler = paymentHandler;
            _osCanceledHandler = osCanceledHandler;
            _logger = logger;
        }

        /// <summary>
        /// Simula o recebimento de um evento PaymentConfirmed localmente (para testes).
        /// Dispara o handler sem necessidade de SQS/SNS.
        /// </summary>
        [HttpPost("payment-confirmed")]
        [AllowAnonymous]
        public async Task<IActionResult> SimulatePaymentConfirmed(
            [FromBody] SimulatePaymentConfirmedRequest request)
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Simulando PaymentConfirmed para OS {OsId}",
                request.CorrelationId, request.OsId);

            var evt = new PaymentConfirmedEvent
            {
                EventId = request.EventId ?? Guid.NewGuid().ToString(),
                OsId = request.OsId,
                PaymentId = request.PaymentId ?? Guid.NewGuid().ToString(),
                Amount = request.Amount,
                Status = request.Status ?? "Confirmed",
                CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString()
            };

            await _paymentHandler.HandleAsync(evt);

            return Ok(new { message = "PaymentConfirmed processado com sucesso", correlationId = evt.CorrelationId });
        }

        /// <summary>
        /// Simula o recebimento de um evento OsCanceled localmente (para testes).
        /// Dispara o handler sem necessidade de SQS/SNS.
        /// </summary>
        [HttpPost("os-canceled")]
        [AllowAnonymous]
        public async Task<IActionResult> SimulateOsCanceled(
            [FromBody] SimulateOsCanceledRequest request)
        {
            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Simulando OsCanceled para OS {OsId}",
                request.CorrelationId, request.OsId);

            var evt = new OsCanceledEvent
            {
                EventId = request.EventId ?? Guid.NewGuid().ToString(),
                OsId = request.OsId,
                Reason = request.Reason ?? "Unknown",
                CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString()
            };

            await _osCanceledHandler.HandleAsync(evt);

            return Ok(new { message = "OsCanceled processado com sucesso", correlationId = evt.CorrelationId });
        }
    }

    public class SimulatePaymentConfirmedRequest
    {
        public string EventId { get; set; }
        public string OsId { get; set; }
        public string PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string CorrelationId { get; set; }
    }

    public class SimulateOsCanceledRequest
    {
        public string EventId { get; set; }
        public string OsId { get; set; }
        public string Reason { get; set; }
        public string CorrelationId { get; set; }
    }
}
