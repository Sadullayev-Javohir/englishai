using Domain.Assessment;

namespace Domain.Curriculum;

public sealed class CurriculumGenerationItem
{
    private CurriculumGenerationItem() { Module = SubjectKey = PromptVersion = null!; }

    private CurriculumGenerationItem(Guid runId, string module, string subjectKey, Guid? topicId, Guid? bookId,
        Guid? sectionId, CefrLevel? level, string promptVersion, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); RunId = runId; Module = module; SubjectKey = subjectKey; TopicId = topicId;
        BookId = bookId; SectionId = sectionId; Level = level; PromptVersion = promptVersion;
        Status = CurriculumItemStatus.Pending; CreatedAt = UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid RunId { get; private set; }
    public string Module { get; private set; }
    public string SubjectKey { get; private set; }
    public Guid? TopicId { get; private set; }
    public Guid? BookId { get; private set; }
    public Guid? SectionId { get; private set; }
    public CefrLevel? Level { get; private set; }
    public string PromptVersion { get; private set; }
    public CurriculumItemStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? LeaseExpiresAt { get; private set; }
    public string? LeaseOwner { get; private set; }
    public string? PayloadJson { get; private set; }
    public string? ValidationJson { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CurriculumGenerationItem ForTopic(Guid runId, string module, string subjectKey, Guid topicId,
        CefrLevel level, string promptVersion, DateTimeOffset now) =>
        new(runId, module, subjectKey, topicId, null, null, level, promptVersion, now);

    public static CurriculumGenerationItem ForBookSection(Guid runId, string subjectKey, Guid bookId,
        Guid sectionId, CefrLevel level, string promptVersion, DateTimeOffset now) =>
        new(runId, "books", subjectKey, null, bookId, sectionId, level, promptVersion, now);

    public bool IsReady(DateTimeOffset now) =>
        Status == CurriculumItemStatus.Pending || Status == CurriculumItemStatus.NeedsRetry && NextAttemptAt <= now ||
        Status == CurriculumItemStatus.Generating && LeaseExpiresAt <= now;

    public void Begin(string owner, DateTimeOffset now, TimeSpan lease)
    {
        Status = CurriculumItemStatus.Generating; AttemptCount++; LeaseOwner = owner;
        LeaseExpiresAt = now.Add(lease); ErrorCode = ErrorMessage = null; UpdatedAt = now;
    }

    public void Approve(string payloadJson, string validationJson, DateTimeOffset now)
    {
        PayloadJson = payloadJson; ValidationJson = validationJson; Status = CurriculumItemStatus.Approved;
        LeaseOwner = null; LeaseExpiresAt = null; NextAttemptAt = null; UpdatedAt = now;
    }

    public void Retry(string code, string message, DateTimeOffset now, TimeSpan delay, bool permanent = false)
    {
        Status = permanent ? CurriculumItemStatus.FailedPermanent : CurriculumItemStatus.NeedsRetry;
        ErrorCode = code; ErrorMessage = message.Length > 1000 ? message[..1000] : message;
        NextAttemptAt = permanent ? null : now.Add(delay); LeaseOwner = null; LeaseExpiresAt = null; UpdatedAt = now;
    }

    public void Reset(DateTimeOffset now)
    {
        Status = CurriculumItemStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = null;
        LeaseExpiresAt = null;
        LeaseOwner = null;
        ErrorCode = null;
        ErrorMessage = null;
        UpdatedAt = now;
    }

    public void Publish(DateTimeOffset now) { Status = CurriculumItemStatus.Published; UpdatedAt = now; }
}
