using Application.Common;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Images;

public class ImageSafetyAuditJobTests
{
    [Fact]
    public async Task Audit_marks_safe_rows_and_removes_unsafe_rows()
    {
        var store = new InMemoryTopicImageStore();
        var safeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var unsafeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        await store.SaveAsync(Image(safeId, 1), default);
        await store.SaveAsync(Image(unsafeId, 2), default);
        var classifier = new SequentialClassifier(
            new ImageSafetyDecision(true, "test", []),
            new ImageSafetyDecision(false, "test", ["FEMALE_GENITALIA_EXPOSED:0.9"]));
        var job = new ImageSafetyAuditJob(
            store,
            classifier,
            NullLogger<ImageSafetyAuditJob>.Instance);

        var result = await job.RunAsync();

        result.Approved.Should().Be(1);
        result.RemovedUnsafe.Should().Be(1);
        (await store.GetBySlotAsync(safeId, 1, default))!.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
        (await store.GetBySlotAsync(unsafeId, 2, default)).Should().BeNull();
    }

    [Fact]
    public async Task Audit_keeps_replaced_my_first_trip_cover_when_classifier_approves_it()
    {
        var store = new InMemoryTopicImageStore();
        var topicId = Guid.Parse("c90eb41e-d1b1-4550-9b99-b32262bdc32f");
        await store.SaveAsync(Image(topicId, 0), default);
        var classifier = new SequentialClassifier(new ImageSafetyDecision(true, "test", []));
        var job = new ImageSafetyAuditJob(
            store,
            classifier,
            NullLogger<ImageSafetyAuditJob>.Instance);

        var result = await job.RunAsync();

        result.ForcedReplacements.Should().Be(0);
        result.Approved.Should().Be(1);
        (await store.GetBySlotAsync(topicId, 0, default))!.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
    }

    private static TopicImageContent Image(Guid id, int slot) =>
        new(id, slot, [0xFF, 0xD8, 0xFF], "image/jpeg", "test", null, null, 1, 1, DateTimeOffset.UtcNow);

    private sealed class SequentialClassifier(params ImageSafetyDecision[] decisions) : IImageSafetyClassifier
    {
        private int _index;

        public Task<ImageSafetyDecision> ClassifyAsync(
            byte[] data,
            string contentType,
            CancellationToken cancellationToken) => Task.FromResult(decisions[_index++]);
    }
}
