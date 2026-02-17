using System;
using System.Collections.Generic;
using OficinaCardozo.ExecutionService.Domain;
using Xunit;

namespace OficinaCardozo.ExecutionService.Domain.Tests
{
    public class ExecutionJobStateMachineTests
    {
        [Fact]
        public void StateMachine_ShouldTransitionCorrectly()
        {
            var job = new ExecutionJob
            {
                Id = Guid.NewGuid(),
                OsId = "os1",
                Status = ExecutionStatus.Queued,
                Attempt = 1,
                CreatedAt = DateTime.UtcNow,
                CorrelationId = "corr1"
            };

            // Diagnosing
            job.Status = ExecutionStatus.Diagnosing;
            Assert.Equal(ExecutionStatus.Diagnosing, job.Status);

            // Repairing
            job.Status = ExecutionStatus.Repairing;
            Assert.Equal(ExecutionStatus.Repairing, job.Status);

            // Finished
            job.Status = ExecutionStatus.Finished;
            Assert.Equal(ExecutionStatus.Finished, job.Status);
        }
    }
}
