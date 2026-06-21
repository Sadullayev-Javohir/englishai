namespace Application.Common;

/// <summary>
/// One topic image as stored in the database: the raw bytes plus the provenance kept for
/// attribution (docs/development-guide.md rule 12). A topic owns a small gallery of images, each at its own
/// <see cref="Slot"/> (0 = the cover/thumbnail shown on catalog cards; 1..N illustrate the topic in
/// different places - passage hero, gallery strip, etc.). Every skill that teaches the topic
/// (vocabulary, reading, grammar, writing, listening, video) draws from the same gallery - Speaking
/// is the only module without thumbnails.
/// </summary>
public sealed record TopicImageContent(
    Guid TopicId,
    int Slot,
    byte[]? Data,
    string ContentType,
    string Source,
    string? Attribution,
    string? SourceUrl,
    int Width,
    int Height,
    DateTimeOffset CreatedAt,
    string? ObjectKey = null,
    long? SizeBytes = null,
    string? Checksum = null,
    string? ETag = null,
    DateTimeOffset? StoredAt = null,
    ImageSafetyStatus SafetyStatus = ImageSafetyStatus.Pending,
    string? SafetyModelVersion = null,
    DateTimeOffset? SafetyCheckedAt = null,
    string? SafetyReasons = null);

/// <summary>
/// A single slot in a topic's image gallery without its bytes - used to build the manifest the
/// frontend reads to know how many images a topic has and how to attribute each (rule 12).
/// </summary>
public sealed record TopicImageSlotInfo(
    int Slot,
    string Source,
    string? Attribution,
    string? SourceUrl,
    ImageSafetyStatus SafetyStatus);

/// <summary>
/// Persistence port for a topic's gallery of thumbnail images. Each image is downloaded once from a
/// licensed source and stored in the database (rule 12), then re-served from there on every request
/// so the app never hot-links an external host and a provider API is hit at most once per image
/// (rule 10). Implemented by an EF Core / PostgreSQL adapter in production and an in-memory adapter
/// for dev/tests.
/// </summary>
public interface ITopicImageStore
{
    /// <summary>
    /// Returns the topic's cover image (slot 0), or null if none has been downloaded yet. The cover
    /// is what catalog cards show; this keeps the original single-image serving path intact.
    /// </summary>
    Task<TopicImageContent?> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the image at a specific gallery slot for a topic, or null if that slot has not been
    /// filled. Callers fall back to the cover (slot 0) or a placeholder when null.
    /// </summary>
    Task<TopicImageContent?> GetBySlotAsync(Guid topicId, int slot, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the slots that exist for a topic (bytes excluded), ordered by slot, so the frontend
    /// can render a gallery and attribute each image without downloading every image first.
    /// </summary>
    Task<IReadOnlyList<TopicImageSlotInfo>> GetManifestAsync(Guid topicId, CancellationToken cancellationToken);

    /// <summary>
    /// How many images each topic already has, keyed by topic id (topics with none are absent). Used
    /// by the backfill job to top each topic up to the target gallery size and to stay idempotent
    /// (so a re-run downloads only the missing slots).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetImageCountsAsync(CancellationToken cancellationToken);

    /// <summary>Inserts or replaces the stored image at a topic's slot.</summary>
    Task SaveAsync(TopicImageContent image, CancellationToken cancellationToken);

    Task MarkSafetyAsync(
        Guid topicId,
        int slot,
        ImageSafetyStatus status,
        string? modelVersion,
        DateTimeOffset checkedAt,
        string? reasons,
        CancellationToken cancellationToken);

    Task<int> DeleteAsync(Guid topicId, int slot, CancellationToken cancellationToken);

    Task<ImageSafetyInventory> GetSafetyInventoryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns every stored image (bytes included) so an operator job can re-encode them in place
    /// (e.g. convert legacy JPEG/PNG galleries to compact WebP without re-downloading). The caller
    /// is expected to page/stream this - the full gallery can be large.
    /// </summary>
    IAsyncEnumerable<TopicImageContent> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes every stored topic image and returns how many were removed. Used by the operator
    /// "purge images" path: clearing the galleries lets the image backfill re-download a fresh,
    /// safe-filtered set (the backfill is idempotent and would otherwise keep an already-full -
    /// possibly unsuitable - gallery untouched). A no-op returns 0.
    /// </summary>
    Task<int> DeleteAllAsync(CancellationToken cancellationToken);
}

public sealed record ImageSafetyInventory(int Total, int Safe, int Pending, int Unsafe, int Failed);
