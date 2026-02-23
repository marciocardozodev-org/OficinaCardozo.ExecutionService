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
    /// Testes unitários para OsCanceledHandler.
    /// Cobertura: Cancelamento de jobs, tratamento de estados, idempotência.
    /// </summary>
    public class OsCanceledHandlerTests
    {
        [Fact]
        public async Task HandleAsync_ComJobAtivo_DeveCancelarJob()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-100")
                .WithStatus(ExecutionStatus.Queued)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-100",
                Reason = "Cancelamento solicitado pelo cliente",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            var jobAtualizado = dbContext.ExecutionJobs.FirstOrDefault();
            jobAtualizado?.Status.Should().Be(ExecutionStatus.Canceled);
        }

        [Fact]
        public async Task HandleAsync_ComEventoDuplicado_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-101")
                .WithStatus(ExecutionStatus.Diagnosing)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = "duplicate-event-id",
                OsId = "OS-101",
                Reason = "Cancelamento",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            mockInbox.Verify(x => x.AddEventAsync(It.IsAny<InboxEvent>()), Times.Never);
            var jobAindaativo = dbContext.ExecutionJobs.First();
            jobAindaativo.Status.Should().Be(ExecutionStatus.Diagnosing);
        }

        [Fact]
        public async Task HandleAsync_ComJobJaFinalizado_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-102")
                .WithStatus(ExecutionStatus.Finished)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-102",
                Reason = "Cancelamento",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            var jobAindaFinalizado = dbContext.ExecutionJobs.First();
            jobAindaFinalizado.Status.Should().Be(ExecutionStatus.Finished);
        }

        [Fact]
        public async Task HandleAsync_ComJobFalhado_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-103")
                .WithStatus(ExecutionStatus.Failed)
                .WithLastError("Erro durante processamento")
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-103",
                Reason = "Cancelamento",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            var jobAindaFalhado = dbContext.ExecutionJobs.First();
            jobAindaFalhado.Status.Should().Be(ExecutionStatus.Failed);
        }

        [Fact]
        public async Task HandleAsync_ComJobJaCancelado_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-104")
                .WithStatus(ExecutionStatus.Canceled)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-104",
                Reason = "Cancelamento",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            var jobAindaCancelado = dbContext.ExecutionJobs.First();
            jobAindaCancelado.Status.Should().Be(ExecutionStatus.Canceled);
        }

        [Fact]
        public async Task HandleAsync_ComJobInexistente_DeveIgnorar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-999",
                Reason = "Cancelamento",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            dbContext.ExecutionJobs.Should().BeEmpty();
        }

        [Fact]
        public async Task HandleAsync_DeveRegistrarEventoNoInbox()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-105")
                .WithStatus(ExecutionStatus.Repairing)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-105",
                Reason = "Cliente cancelou",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            mockInbox.Verify(x => x.AddEventAsync(It.IsAny<InboxEvent>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_DeveDefinirUpdatedAtAoCancelar()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var job = new TestFixtures.ExecutionJobBuilder()
                .WithOsId("OS-106")
                .WithStatus(ExecutionStatus.Diagnosing)
                .WithUpdatedAt(null)
                .Build();
            dbContext.ExecutionJobs.Add(job);
            dbContext.SaveChanges();

            var mockInbox = new Mock<IInboxService>();
            var mockOutbox = new Mock<IOutboxService>();
            var mockLogger = new Mock<ILogger<OsCanceledHandler>>();

            mockInbox.Setup(x => x.IsDuplicateAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var handler = new OsCanceledHandler(mockInbox.Object, mockOutbox.Object, dbContext, mockLogger.Object);
            var beforeCall = DateTime.UtcNow;
            var evt = new OsCanceledEvent
            {
                EventId = Guid.NewGuid().ToString(),
                OsId = "OS-106",
                Reason = "Cancelamento",
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await handler.HandleAsync(evt);

            // Assert
            var jobAtualizado = dbContext.ExecutionJobs.First();
            jobAtualizado.UpdatedAt.Should().NotBeNull();
            jobAtualizado.UpdatedAt.Should().BeOnOrAfter(beforeCall);
        }
    }
}
