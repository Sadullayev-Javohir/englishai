using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Reading.Ports;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Reading;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Backfill;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Verifies the content backfill end-to-end with fakes: the module-agnostic
/// <see cref="ContentBackfillJob"/> builds one request per pending (module, topic), routes the batch
/// replies back by <c>CustomId</c>, and the real <see cref="ReadingContentBackfiller"/> parses and
/// persists the reply. Re-running fills nothing (idempotent), and an empty reply leaves the topic
/// pending.
/// </summary>
public class ContentBackfillJobTests
{
    private const string ValidReadingJson =
        """
        {"body":"The cat sat on the warm mat. It was a sunny day and the cat felt happy in the bright sun.",
         "glossary":[{"w":"sunny","uz":"quyoshli","ex":"It was a sunny day and the cat felt happy in the bright sun."}],
         "questions":[{"q":"Where did the cat sit?","options":["On the mat","On the roof"],"answer":0,"why":"The text says the cat sat on the mat."}]}
        """;

    private static VocabularyTopic NewTopic() =>
        VocabularyTopic.Curate(
            "daily-life", "Daily life", "Kundalik hayot", "Life", "present_simple",
            CefrLevel.A1, DateTimeOffset.UtcNow);

    private static ContentBackfillJob BuildJob(
        FakeTopicRepository topics, FakeReadingRepository reading, IContentBatchClient batch) =>
        new(
            topics,
            new IContentBackfiller[] { new ReadingContentBackfiller(reading, new HermesGatewayOptions()) },
            batch,
            NullLogger<ContentBackfillJob>.Instance);

    [Fact]
    public async Task Fills_reading_for_a_pending_topic()
    {
        var topic = NewTopic();
        var topics = new FakeTopicRepository(topic);
        var reading = new FakeReadingRepository();
        var batch = new FakeBatchClient(_ => ValidReadingJson);

        var result = await BuildJob(topics, reading, batch).RunAsync();

        result.Requested.Should().Be(1);
        result.Completed.Should().Be(1);
        result.Filled.Should().Be(1);
        batch.LastCustomIds.Should().ContainSingle().Which.Should().Be($"reading_{topic.Id}");
        // The Anthropic Batch API rejects any custom_id not matching this pattern (no ':' etc.).
        batch.LastCustomIds.Should().OnlyContain(id =>
            System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-zA-Z0-9_-]{1,64}$"));

        var saved = await reading.GetByTopicIdAsync(topic.Id, default);
        saved.Should().NotBeNull();
        saved!.IsFilled.Should().BeTrue();
    }

    [Fact]
    public async Task Is_idempotent_so_a_second_run_requests_nothing()
    {
        var topic = NewTopic();
        var topics = new FakeTopicRepository(topic);
        var reading = new FakeReadingRepository();
        var batch = new FakeBatchClient(_ => ValidReadingJson);
        var job = BuildJob(topics, reading, batch);

        await job.RunAsync();
        var second = await job.RunAsync();

        second.Requested.Should().Be(0);
        second.Filled.Should().Be(0);
    }

    [Fact]
    public async Task Leaves_topic_pending_when_the_reply_is_empty()
    {
        var topic = NewTopic();
        var topics = new FakeTopicRepository(topic);
        var reading = new FakeReadingRepository();
        var batch = new FakeBatchClient(_ => "not json at all");

        var result = await BuildJob(topics, reading, batch).RunAsync();

        result.Requested.Should().Be(1);
        result.Filled.Should().Be(0);
        (await reading.GetByTopicIdAsync(topic.Id, default)).Should().BeNull();
    }

    private sealed class FakeTopicRepository : IVocabularyTopicRepository
    {
        private readonly List<VocabularyTopic> _topics;
        public FakeTopicRepository(params VocabularyTopic[] topics) => _topics = topics.ToList();

        public Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_topics.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.Where(t => ids.Contains(t.Id)).ToList());

        public Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.Where(t => t.Level == level).ToList());

        public Task SaveAsync(VocabularyTopic topic, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.ToList());

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            _topics.RemoveAll(t => t.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReadingRepository : IReadingRepository
    {
        private readonly Dictionary<Guid, ReadingPassage> _byTopic = new();

        public Task<ReadingPassage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReadingPassage?>(null);

        public Task<ReadingPassage?> GetByTopicIdAsync(Guid vocabularyTopicId, CancellationToken cancellationToken) =>
            Task.FromResult(_byTopic.GetValueOrDefault(vocabularyTopicId));

        public Task<IReadOnlyList<ReadingPassage>> GetCatalogForLevelAsync(
            CefrLevel level, int levelTolerance, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ReadingPassage>>(new List<ReadingPassage>());

        public Task SaveAsync(ReadingPassage passage, CancellationToken cancellationToken)
        {
            _byTopic[passage.VocabularyTopicId!.Value] = passage;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ReadingPassage>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ReadingPassage>>(_byTopic.Values.ToList());

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var toRemove = _byTopic.FirstOrDefault(kvp => kvp.Value.Id == id).Key;
            if (toRemove != Guid.Empty)
                _byTopic.Remove(toRemove);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBatchClient : IContentBatchClient
    {
        private readonly Func<BatchContentRequest, string?> _reply;
        public List<string> LastCustomIds { get; } = new();

        public FakeBatchClient(Func<BatchContentRequest, string?> reply) => _reply = reply;

        public Task<IReadOnlyDictionary<string, string>> RunAsync(
            IReadOnlyList<BatchContentRequest> requests, CancellationToken cancellationToken)
        {
            LastCustomIds.Clear();
            LastCustomIds.AddRange(requests.Select(r => r.CustomId));

            var replies = new Dictionary<string, string>();
            foreach (var request in requests)
            {
                var text = _reply(request);
                if (text is not null)
                    replies[request.CustomId] = text;
            }

            return Task.FromResult<IReadOnlyDictionary<string, string>>(replies);
        }
    }
}
