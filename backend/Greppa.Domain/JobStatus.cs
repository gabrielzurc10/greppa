namespace Greppa.Domain;

public enum JobStatus
{
    Queued,
    Scanning,
    Enriching,
    Completed,
    Failed,
}
