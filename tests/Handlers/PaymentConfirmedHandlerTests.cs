using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using OFICINACARDOZO.EXECUTIONSERVICE;
using OficinaCardozo.ExecutionService.EventHandlers;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;
using OficinaCardozo.ExecutionService.Domain;
using OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.Handlers
{
    /// <summary>
    /// Testes unitários para PaymentConfirmedHandler.
    /// Cobertura: Criação de ExecutionJob, tratamento de duplicatas, idempotência.
    /// </summary>
    public class PaymentConfirmedHandlerTests
    {
        [Fact]
        public async Task HandleAsync_ComEventoValido_DeveRegistrarNoInboxECriarJob()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<PaymentConfirmedHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new PaymentConfirmedHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new PaymentConfirmedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-001",
                PaymentId = "PAY-001",
                Amount = 100m,
                Status = "Confirmed",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            mockInbox.Verify(x => x.AddEventAsync(It.IsAny<InboxEvent>()), Times.Once);
            dbContext.ExecutionJobs.Should().HaveCount(1);
            var job = dbContext.ExecutionJobs.First();
            job.OsId.Should().Be("OS-001");
            job.Status.Should().Be(ExecutionStatus.Queued);
        }

        [Fact]
        public async Task HandleAsync_ComEventoDuplicado_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<PaymentConfirmedHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var handler = new PaymentConfirmedHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new PaymentConfirmedEvent
            {
                EventId = "duplicate-event-id",
                OsId = "OS-002",
                PaymentId = "PAY-002",
                Amount = 200m,
                Status = "Confirmed",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            mockInbox.Verify(x => x.AddEventAsync(It.IsAny<InboxEvent>()), Times.Never);
            dbContext.ExecutionJobs.Should().HaveCount(0);
        }

        [Fact]
        public async Task HandleAsync_DevePublicarEventoExecutionStartedNoOutbox()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<PaymentConfirmedHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new PaymentConfirmedHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new PaymentConfirmedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-003",
                PaymentId = "PAY-003",
                Amount = 150m,
                Status = "Confirmed",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            mockOutbox.Verify(x => x.AddEventAsync(It.IsAny<OutboxEvent>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ComJobExistenteParaMesmoOsId_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-004")
                .WithStatus(ExecutionStatus.Queued)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<PaymentConfirmedHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new PaymentConfirmedHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new PaymentConfirmedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-004",
                PaymentId = "PAY-004",
                Amount = 100m,
                Status = "Confirmed",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            dbContext.ExecutionJobs.Should().HaveCount(1);
            mockOutbox.Verify(x => x.AddEventAsync(It.IsAny<OutboxEvent>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_DeveManterIdempotencia()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<PaymentConfirmedHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new PaymentConfirmedHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new PaymentConfirmedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-005",
                PaymentId = "PAY-005",
                Amount = 300m,
                Status = "Confirmed",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act - Chamar duas vezes
            await handler.HandleAsync(evt);
            var countAfterFirst = dbContext.ExecutionJobs.Count();
            
            mockInbox.Setup(x => x.IsDuplicateAsync(evt.EventId))
                .ReturnsAsync(true);
            
            await handler.HandleAsync(evt);

            // Assert
            dbContext.ExecutionJobs.Should().HaveCount(countAfterFirst);
        }

        [Fact]
        public async Task HandleAsync_DeveRegistrarCorrelationIdNoJob()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<PaymentConfirmedHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new PaymentConfirmedHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var correlationId = Guid.NewGuid().ToString();
            var evt = new PaymentConfirmedEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-006",
                PaymentId = "PAY-006",
                Amount = 250m,
                Status = "Confirmed",
                CorrelationId = correlationId
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            var job = dbContext.ExecutionJobs.FirstOrDefault();
            job?.CorrelationId.Should().Be(correlationId);
        }
    }
}
