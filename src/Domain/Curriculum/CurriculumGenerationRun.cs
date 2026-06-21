namespace Domain.Curriculum;

public enum CurriculumRunStatus
{
    Running = 1,
    Paused = 2,
    Completed = 3,
    Failed = 4,
    Published = 5,
}

public enum CurriculumItemStatus
{
    Pending = 1,
    Generating = 2,
    Approved = 3,
    NeedsRetry = 4,
    NeedsHumanReview = 5,
    FailedPermanent = 6,
    Published = 7,
}

public sealed class CurriculumGenerationRun
{
    private CurriculumGenerationRun() { Provider = Model = Version = null!; }

    private CurriculumGenerationRun(string version, string provider, string model, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Version = version;
        Provider = provider;
        Model = model;
        Status = CurriculumRunStatus.Running;
        CreatedAt = UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public string Version { get; private set; }
    public string Provider { get; private set; }
    public string Model { get; private set; }
    public CurriculumRunStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    public static CurriculumGenerationRun Start(string version, string provider, string model, DateTimeOffset now) =>
        new(version.Trim(), provider.Trim(), model.Trim(), now);

    public void Resume(DateTimeOffset now) { Status = CurriculumRunStatus.Running; UpdatedAt = now; }
    public void Pause(DateTimeOffset now) { Status = CurriculumRunStatus.Paused; UpdatedAt = now; }
    public void Complete(DateTimeOffset now) { Status = CurriculumRunStatus.Completed; CompletedAt = UpdatedAt = now; }
    public void Publish(DateTimeOffset now) { Status = CurriculumRunStatus.Published; PublishedAt = UpdatedAt = now; }
}
