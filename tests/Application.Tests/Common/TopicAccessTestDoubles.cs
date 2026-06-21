using Application.Common;
using Application.Subscription.Access;
using NSubstitute;

namespace Application.Tests.Common;

/// <summary>
/// Test doubles for the topic trial paywall (<see cref="ITopicAccessPolicy"/>) and the authenticated
/// user (<see cref="ICurrentUserAccessor"/>). The defaults are permissive - full access, no
/// authenticated user - so handler tests that are not about the paywall keep their original behavior.
/// </summary>
public static class TopicAccessTestDoubles
{
    /// <summary>An access policy that grants full, unmetered access and never blocks a topic.</summary>
    public static ITopicAccessPolicy AllowAll()
    {
        var access = Substitute.For<ITopicAccessPolicy>();
        access.HasFullAccessAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        return access;
    }

    /// <summary>A current-user accessor with no authenticated learner (enforcement skipped).</summary>
    public static ICurrentUserAccessor Anonymous() => Substitute.For<ICurrentUserAccessor>();
}
