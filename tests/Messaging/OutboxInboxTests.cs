using Xunit;
using FluentAssertions;
using OFICINACARDOZO.EXECUTIONSERVICE;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;
using OFICINACARDOZO.EXECUTIONSERVICE.Tests.Fixtures;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Tests.Messaging
{
    /// <summary>
    /// Testes unitários para InboxService e OutboxService.
    /// Cobertura: Transactional Inbox, Transactional Outbox, idempotência message delivery.
    /// </summary>
    public class OutboxInboxTests
    {
        #region InboxService Tests

        [Fact]
        public async Task InboxService_AddEventAsync_DeveAdicionarEventoAoDb()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new InboxService(dbContext);
            var evt = new TestFixtures.InboxEventBuilder()
                .WithEventId("event-001")
                .WithEventType("PaymentConfirmed")
                .Build();

            // Act
            await service.AddEventAsync(evt);

            // Assert
            dbContext.InboxEvents.Should().HaveCount(1);
            var saved = dbContext.InboxEvents.First();
            saved.EventId.Should().Be("event-001");
            saved.EventType.Should().Be("PaymentConfirmed");
        }

        [Fact]
        public async Task InboxService_IsDuplicateAsync_ComEventoExistente_DeveRetornarTrue()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new InboxService(dbContext);
            var evt = new TestFixtures.InboxEventBuilder()
                .WithEventId("event-002")
                .Build();
            await service.AddEventAsync(evt);

            // Act
            var isDuplicate = await service.IsDuplicateAsync("event-002");

            // Assert
            isDuplicate.Should().BeTrue();
        }

        [Fact]
        public async Task InboxService_IsDuplicateAsync_ComEventoInexistente_DeveRetornarFalse()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new InboxService(dbContext);

            // Act
            var isDuplicate = await service.IsDuplicateAsync("event-inexistente");

            // Assert
            isDuplicate.Should().BeFalse();
        }

        [Fact]
        public async Task InboxService_GuaranteIdempotencia_EvitaDuplicatas()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new InboxService(dbContext);
            var eventId = "event-003";

            // Act
            var isFistTimeNew = await service.IsDuplicateAsync(eventId);
            var evt = new TestFixtures.InboxEventBuilder()
                .WithEventId(eventId)
                .Build();
            await service.AddEventAsync(evt);
            var isSecondTimeExists = await service.IsDuplicateAsync(eventId);

            // Assert
            isFistTimeNew.Should().BeFalse();
            isSecondTimeExists.Should().BeTrue();
        }

        [Fact]
        public async Task InboxService_AdicionarMultiplosEventos_DeveManterTodos()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new InboxService(dbContext);

            // Act
            for (int i = 0; i < 5; i++)
            {
                var evt = new TestFixtures.InboxEventBuilder()
                    .WithEventId($"event-multi-{i}")
                    .WithEventType($"EventType{i}")
                    .Build();
                await service.AddEventAsync(evt);
            }

            // Assert
            dbContext.InboxEvents.Should().HaveCount(5);
        }

        #endregion

        #region OutboxService Tests

        [Fact]
        public async Task OutboxService_AddEventAsync_DeveAdicionarEventoAoDb()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);
            var payload = "{\"OsId\": \"OS-001\", \"Status\": \"Queued\"}";
            var evt = new TestFixtures.OutboxEventBuilder()
                .WithEventType("ExecutionStarted")
                .WithPayload(payload)
                .WithPublished(false)
                .Build();

            // Act
            await service.AddEventAsync(evt);

            // Assert
            dbContext.OutboxEvents.Should().HaveCount(1);
            var saved = dbContext.OutboxEvents.First();
            saved.EventType.Should().Be("ExecutionStarted");
            saved.Published.Should().BeFalse();
        }

        [Fact]
        public async Task OutboxService_GetUnpublishedEventsAsync_DeveRetornarApenasNaoPublicados()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);

            var evt1 = new TestFixtures.OutboxEventBuilder()
                .WithEventType("Event1")
                .WithPublished(false)
                .Build();
            var evt2 = new TestFixtures.OutboxEventBuilder()
                .WithEventType("Event2")
                .WithPublished(true)
                .Build();
            var evt3 = new TestFixtures.OutboxEventBuilder()
                .WithEventType("Event3")
                .WithPublished(false)
                .Build();

            await service.AddEventAsync(evt1);
            await service.AddEventAsync(evt2);
            await service.AddEventAsync(evt3);

            // Act
            var unpublished = await service.GetUnpublishedEventsAsync();

            // Assert
            unpublished.Should().HaveCount(2);
            unpublished.All(e => !e.Published).Should().BeTrue();
        }

        [Fact]
        public async Task OutboxService_MarkAsPublishedAsync_DeveMudarStatusParaPublished()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);
            var evt = new TestFixtures.OutboxEventBuilder()
                .WithEventType("TestEvent")
                .WithPublished(false)
                .Build();
            await service.AddEventAsync(evt);
            var beforeCall = DateTime.UtcNow;

            // Act
            await service.MarkAsPublishedAsync(evt.Id);

            // Assert
            var published = dbContext.OutboxEvents.First();
            published.Published.Should().BeTrue();
            published.PublishedAt.Should().NotBeNull();
            published.PublishedAt.Should().BeOnOrAfter(beforeCall);
        }

        [Fact]
        public async Task OutboxService_MarkAsPublishedAsync_ComIdInexistente_NaoDeveLancarExcecao()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);

            // Act & Assert
            await service.Invoking(x => x.MarkAsPublishedAsync(Guid.NewGuid()))
                .Should().NotThrowAsync();
        }

        [Fact]
        public async Task OutboxService_TransactionalOutboxPattern_DeveGarantirEntrega()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);

            // Act - Simular processo de outbox
            var evt1 = new TestFixtures.OutboxEventBuilder()
                .WithEventType("Event1")
                .WithPublished(false)
                .Build();
            await service.AddEventAsync(evt1);

            var unpublished = await service.GetUnpublishedEventsAsync();
            unpublished.Should().HaveCount(1);

            await service.MarkAsPublishedAsync(evt1.Id);
            var stillUnpublished = await service.GetUnpublishedEventsAsync();

            // Assert
            stillUnpublished.Should().BeEmpty();
        }

        [Fact]
        public async Task OutboxService_MultipleEvents_DeveManterOrdem()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);

            // Act
            for (int i = 0; i < 10; i++)
            {
                var evt = new TestFixtures.OutboxEventBuilder()
                    .WithEventType($"Event{i}")
                    .WithPayload($"{{\"Sequence\": {i}}}")
                    .WithPublished(false)
                    .Build();
                await service.AddEventAsync(evt);
            }

            // Assert
            var unpublished = await service.GetUnpublishedEventsAsync();
            unpublished.Should().HaveCount(10);
            unpublished.Select(e => e.EventType)
                .Should().ContainInOrder(Enumerable.Range(0, 10).Select(i => $"Event{i}"));
        }

        [Fact]
        public async Task OutboxService_ConcurrentPublishing_DeveHandleCorrectamente()
        {
            // Arrange
            var dbContext = TestFixtures.CreateInMemoryDbContext();
            var service = new OutboxService(dbContext);

            var events = Enumerable.Range(0, 5)
                .Select(i => new TestFixtures.OutboxEventBuilder()
                    .WithEventType($"Event{i}")
                    .WithPublished(false)
                    .Build())
                .ToList();

            // Act
            foreach (var evt in events)
            {
                await service.AddEventAsync(evt);
            }

            var unpublished = await service.GetUnpublishedEventsAsync();
            
            // Simular publicação concorrente (metade)
            var tasksToPublish = unpublished.Take(3)
                .Select(e => service.MarkAsPublishedAsync(e.Id))
                .ToList();
            
            await Task.WhenAll(tasksToPublish);

            // Assert
            var remaining = await service.GetUnpublishedEventsAsync();
            remaining.Should().HaveCount(2);
        }

        #endregion
    }
}
