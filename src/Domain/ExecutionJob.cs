using System;

namespace OficinaCardozo.ExecutionService.Domain
{
    public enum ExecutionStatus
    {
        Queued,
        Diagnosing,
        Repairing,
        Finished,
        Failed,
        Canceled
    }

    public class ExecutionJob
    {
        public Guid Id { get; set; }
        public string OsId { get; set; }
        public ExecutionStatus Status { get; set; }
        public int Attempt { get; set; }
        public string LastError { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string CorrelationId { get; set; }
    }
}
