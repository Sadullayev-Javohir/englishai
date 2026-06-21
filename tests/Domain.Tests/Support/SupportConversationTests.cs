using Domain.Support;

namespace Domain.Tests.Support;

public sealed class SupportConversationTests
{
    [Fact]
    public void Learner_message_reopens_closed_conversation()
    {
        var now = DateTimeOffset.Parse("2026-08-08T10:00:00Z");
        var learnerId = Guid.NewGuid();
        var conversation = SupportConversation.Create(learnerId, now);
        conversation.Close(now.AddMinutes(1));

        var message = conversation.AddMessage(learnerId, SupportSenderKind.Learner, "Ilova ishlamayapti", [], now.AddMinutes(2));

        Assert.Equal(SupportConversationStatus.Open, conversation.Status);
        Assert.Null(conversation.ClosedAt);
        Assert.Equal("Ilova ishlamayapti", message.Text);
    }

    [Fact]
    public void Message_requires_text_or_attachment()
    {
        var conversation = SupportConversation.Create(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<Domain.Common.DomainException>(() =>
            conversation.AddMessage(conversation.LearnerId, SupportSenderKind.Learner, "  ", [], DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Conversation_tracks_assignment_and_read_times()
    {
        var now = DateTimeOffset.UtcNow;
        var adminId = Guid.NewGuid();
        var conversation = SupportConversation.Create(Guid.NewGuid(), now);
        conversation.Assign(adminId, now.AddMinutes(1));
        conversation.MarkRead(SupportSenderKind.Admin, now.AddMinutes(2));

        Assert.Equal(adminId, conversation.AssignedAdminId);
        Assert.Equal(now.AddMinutes(2), conversation.AdminLastReadAt);
    }
}
