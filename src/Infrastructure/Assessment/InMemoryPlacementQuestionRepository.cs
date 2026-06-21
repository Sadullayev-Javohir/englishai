using Application.Assessment.Ports;
using Domain.Assessment;

namespace Infrastructure.Assessment;

/// <summary>
/// In-memory placement question bank backed by <see cref="QuestionBankSeed"/>.
/// When no unused question exists at the exact requested difficulty, it falls back
/// to the nearest available difficulty within the same stage.
/// </summary>
public sealed class InMemoryPlacementQuestionRepository : IPlacementQuestionRepository
{
    private readonly IReadOnlyDictionary<Guid, PlacementQuestion> _byId;
    private readonly IReadOnlyList<PlacementQuestion> _all;

    public InMemoryPlacementQuestionRepository()
        : this(QuestionBankSeed.Questions)
    {
    }

    public InMemoryPlacementQuestionRepository(IReadOnlyList<PlacementQuestion> questions)
    {
        _all = questions;
        _byId = questions.ToDictionary(q => q.Id);
    }

    public Task<PlacementQuestion?> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(questionId, out var question);
        return Task.FromResult(question);
    }

    public Task<PlacementQuestion?> GetNextAsync(
        TestStage stage,
        CefrLevel difficulty,
        IReadOnlyCollection<Guid> excludeQuestionIds,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var excluded = excludeQuestionIds as ISet<Guid> ?? excludeQuestionIds.ToHashSet();

        var available = _all
            .Where(q => q.Stage == stage && !excluded.Contains(q.Id))
            .ToList();

        if (available.Count == 0)
            return Task.FromResult<PlacementQuestion?>(null);

        // Narrow to the items sitting at the nearest available difficulty, then pick
        // one pseudo-randomly so two learners at the same level get a different mix.
        var nearestDistance = available.Min(q => Math.Abs((int)q.Difficulty - (int)difficulty));
        var pool = available
            .Where(q => Math.Abs((int)q.Difficulty - (int)difficulty) == nearestDistance)
            .OrderBy(q => q.Id) // stable base order before seeded pick
            .ToList();

        var rng = new Random(SelectionSeed(sessionId, excluded.Count));
        var candidate = pool[rng.Next(pool.Count)];

        return Task.FromResult<PlacementQuestion?>(candidate);
    }

    private static int SelectionSeed(Guid sessionId, int answeredCount)
    {
        var seed = 17;
        foreach (var b in sessionId.ToByteArray())
            seed = unchecked(seed * 31 + b);
        return unchecked(seed * 31 + answeredCount);
    }
}
