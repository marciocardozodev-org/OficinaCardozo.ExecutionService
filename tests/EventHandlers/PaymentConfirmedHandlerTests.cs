using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;
using OficinaCardozo.ExecutionService.EventHandlers;
using Xunit;

namespace OficinaCardozo.ExecutionService.EventHandlers.Tests
{
    public class PaymentConfirmedHandlerTests
    {
        [Fact]
        public async Task HandleAsync_ShouldCreateJobAndPublishEvent_WhenNotDuplicate()
        {
            var inbox = new InboxService();
            var outbox = new OutboxService();
            var jobs = new List<ExecutionJob>();
            var handler = new PaymentConfirmedHandler(inbox, outbox, jobs);
            var evt = new PaymentConfirmedEvent { EventId = "evt1", OsId = "os1", CorrelationId = "corr1" };

            await handler.HandleAsync(evt);

            Assert.Single(jobs);
            Assert.Equal("os1", jobs[0].OsId);
            var events = await outbox.GetUnpublishedEventsAsync();
            Assert.Single(events);
            Assert.Equal("ExecutionStarted", events[0].EventType);
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCreateDuplicateJob_WhenEventIdIsDuplicate()
        {
            var inbox = new InboxService();
            var outbox = new OutboxService();
            var jobs = new List<ExecutionJob>();
            var handler = new PaymentConfirmedHandler(inbox, outbox, jobs);
            var evt = new PaymentConfirmedEvent { EventId = "evt1", OsId = "os1", CorrelationId = "corr1" };

            await handler.HandleAsync(evt);
            await handler.HandleAsync(evt);

            Assert.Single(jobs);
            var events = await outbox.GetUnpublishedEventsAsync();
            Assert.Single(events);
        }
    }
}
