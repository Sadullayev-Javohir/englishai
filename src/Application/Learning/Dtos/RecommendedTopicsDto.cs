using Domain.Assessment;
using Domain.Common;

namespace Application.Learning.Dtos;

/// <summary>
/// Goal-tailored topic suggestions for the home "for your goal" strip (goal-based onboarding).
/// Carries the learner's <see cref="Goal"/> so the SPA can render the right heading, and the topics
/// ordered by relevance to that goal (most relevant first). When the goal is
/// <see cref="LearningGoal.Unspecified"/> the list is simply the level's catalog order, so nothing
/// regresses for learners who skipped the goal screen.
/// </summary>
public sealed record RecommendedTopicsDto(
    LearningGoal Goal,
    CefrLevel Level,
    IReadOnlyList<RecommendedTopicDto> Topics);

/// <summary>
/// One recommended topic. The Uzbek label (<see cref="TitleUz"/>, rule 11) is shown; <see cref="Id"/>
/// opens the topic's six-module path (/vocabulary/topic/{id}). <see cref="IsGoalRelevant"/> flags the
/// topics that matched the learner's goal so the UI can badge them.
/// </summary>
public sealed record RecommendedTopicDto(
    Guid Id,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level,
    bool IsGoalRelevant);
