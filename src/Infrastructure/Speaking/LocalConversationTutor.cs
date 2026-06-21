using Application.Speaking.Ports;
using Domain.Speaking;
using System.Text.RegularExpressions;

namespace Infrastructure.Speaking;

/// <summary>
/// Deterministic local stand-in for the LLM tutor. Produces an English greeting and
/// rotating follow-up questions so the conversation flow is exercisable without an
/// LLM API key. Replace with a budget-LLM adapter that caps output tokens and uses
/// prompt caching (docs/development-guide.md rule 10).
/// </summary>
public sealed partial class LocalConversationTutor : IConversationTutor
{
    private static readonly string[] TopicFollowUps =
    {
        "What do you like most about it?",
        "When did you first become interested in it?",
        "Can you give me one specific example?",
        "How does it make you feel?",
        "What would you like to tell me about it next?"
    };

    // Curated topic codes (docs/development-guide.md rule 11) → a friendly, level-neutral opening
    // question. Unknown/null codes fall back to the generic greeting below.
    private static readonly IReadOnlyDictionary<string, string> TopicOpenings =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["travel"] = "Hello! Let's talk about travel. Where would you like to go on your next trip?",
            ["work"] = "Hi! Let's talk about work. What do you do, or what would you like to do?",
            ["daily_life"] = "Hello! Let's talk about your daily life. What does a normal day look like for you?",
            ["food"] = "Hi there! Let's talk about food. What is your favourite dish?",
            ["hobbies"] = "Hello! Let's talk about hobbies. What do you like to do in your free time?",
        };

    private const string GenericGreeting =
        "Hello! I'm your English speaking tutor. What would you like to talk about today?";

    // Deterministic in-character openings and follow-ups for a handful of classic roleplay
    // scenarios, so the roleplay flow is exercisable without an LLM key. Any other catalog scenario
    // falls back to a generic scene opening built from its definition (below). The real personas
    // come from HermesConversationTutor.
    private static readonly IReadOnlyDictionary<string, (string Opening, string[] FollowUps)>
        RoleplayLines = new Dictionary<string, (string, string[])>(StringComparer.OrdinalIgnoreCase)
        {
            ["job_interview"] = (
                "Good morning, thanks for coming in. Please, tell me a little about yourself.",
                new[]
                {
                    "That's interesting. Why do you want to work with us?",
                    "Great. What would you say is your biggest strength?",
                    "I see. Can you tell me about your experience?",
                    "Good. Do you have any questions for me?",
                }),
            ["airport"] = (
                "Good morning! Welcome to the check-in desk. May I see your passport, please?",
                new[]
                {
                    "Thank you. Would you like a window or an aisle seat?",
                    "Do you have any bags to check in today?",
                    "Your gate is number twelve. Anything else I can help with?",
                }),
            ["restaurant"] = (
                "Good evening, welcome! Here is your menu. Can I get you something to drink?",
                new[]
                {
                    "Great choice. Are you ready to order your food?",
                    "Would you like anything else with that?",
                    "Of course. Would you like to see the dessert menu or the bill?",
                }),
            ["doctor"] = (
                "Hello, please have a seat. So, what brings you in today?",
                new[]
                {
                    "I'm sorry to hear that. How long have you felt this way?",
                    "I see. Do you have any other symptoms?",
                    "Alright. I'll write you a prescription. Do you have any questions?",
                }),
            ["shop"] = (
                "Hi there! Welcome in. Are you looking for anything in particular today?",
                new[]
                {
                    "Nice choice. What size do you need?",
                    "We have that in a few colours. Which would you like?",
                    "Great. Will you be paying by cash or card?",
                }),
        };

    public Task<string> NextReplyAsync(ConversationSession session, CancellationToken cancellationToken = default)
    {
        var learnerTurns = session.Turns.Count(t => t.Role == ConversationRole.Learner);

        // Roleplay: stay in the chosen persona. The classic scenarios have scripted openings and
        // rotating in-character prompts; every other catalog scenario gets a generic scene opening
        // built from its curated definition, then the generic follow-ups keep the learner talking.
        if (session.ScenarioCode is { } scenarioCode)
        {
            if (RoleplayLines.TryGetValue(scenarioCode, out var lines))
            {
                var roleReply = learnerTurns > 0
                    ? lines.FollowUps[(learnerTurns - 1) % lines.FollowUps.Length]
                    : lines.Opening;
                return Task.FromResult(roleReply);
            }

            var definition = RoleplayScenarioCatalog.Get(scenarioCode);
            var genericReply = learnerTurns > 0
                ? BuildContextualFollowUp(session, learnerTurns)
                : $"Hello, and welcome. Our scene is: {definition.EnglishTitle}. "
                    + "Shall we begin? Please, go ahead.";
            return Task.FromResult(genericReply);
        }

        string reply;
        if (learnerTurns > 0)
        {
            reply = BuildContextualFollowUp(session, learnerTurns);
        }
        else if (session.Topic is not null && TopicOpenings.TryGetValue(session.Topic, out var opening))
        {
            reply = opening;
        }
        else if (session.Topic is not null)
        {
            // A vocabulary-linked topic (its English title). Open on the topic and, if the
            // learner just learned words for it, invite them to use one.
            var subject = session.Topic.Replace('_', ' ');
            reply = session.FocusWords.Count > 0
                ? $"Hello! Let's talk about {subject}. Can you tell me something using the word \"{session.FocusWords[0]}\"?"
                : $"Hello! Let's talk about {subject}. What comes to mind when you think about it?";
        }
        else
        {
            reply = GenericGreeting;
        }

        return Task.FromResult(reply);
    }

    private static string BuildContextualFollowUp(ConversationSession session, int learnerTurns)
    {
        var learnerAnswer = session.Turns
            .Last(turn => turn.Role == ConversationRole.Learner)
            .Text.Trim();
        if (HowAreYouPattern().IsMatch(learnerAnswer))
            return "I'm doing well, thank you. I'm here and ready to practise English with you. How are you today?";

        if (WhatAreYouDoingPattern().IsMatch(learnerAnswer))
            return "I'm talking with you and helping you practise English. What are you doing today?";

        if (GreetingPattern().IsMatch(learnerAnswer))
            return "Hi! It's nice to talk with you. How are you today?";

        if (ThanksPattern().IsMatch(learnerAnswer))
            return "You're welcome! What would you like to talk about next?";

        if (TutorIdentityPattern().IsMatch(learnerAnswer))
            return "I'm your English speaking partner. You can talk with me about everyday life, your interests, or your chosen topic.";

        if (DailyRoutinePattern().IsMatch(learnerAnswer))
        {
            if (PhonePattern().IsMatch(learnerAnswer))
                return "Checking your phone is an important part of your morning routine. Do you usually check messages first, or do you use your phone as an alarm?";

            return "You are describing your daily routine. What is the next thing you usually do after waking up?";
        }

        var curriculumQuestion = session.CurriculumContext?.Questions.Count > 0
            ? session.CurriculumContext.Questions[(learnerTurns - 1) % session.CurriculumContext.Questions.Count]
            : null;
        var followUp = curriculumQuestion ?? TopicFollowUps[(learnerTurns - 1) % TopicFollowUps.Length];
        var activeTopic = session.CurriculumContext?.TopicTitle ?? session.Topic;
        if (!string.IsNullOrWhiteSpace(activeTopic))
        {
            var subject = activeTopic.Replace('_', ' ');
            return $"That sounds interesting. Let's keep talking about {subject}. {followUp}";
        }

        if (learnerAnswer.EndsWith('?'))
            return "That's a good question. I may not have every detail, but we can explore it together. What do you think?";

        return $"That sounds interesting. {followUp}";
    }

    [GeneratedRegex(@"\b(how are you|how're you|how do you feel)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HowAreYouPattern();

    [GeneratedRegex(@"\b(what are you doing|what're you doing|what do you do)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WhatAreYouDoingPattern();

    [GeneratedRegex(@"\b(hello|hi|hey|good morning|good afternoon|good evening)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GreetingPattern();

    [GeneratedRegex(@"\b(thank you|thanks)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ThanksPattern();

    [GeneratedRegex(@"\b(what is your name|what's your name|who are you)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TutorIdentityPattern();

    [GeneratedRegex(@"\b(daily routine|morning routine|wake up|waking up|first thing)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DailyRoutinePattern();

    [GeneratedRegex(@"\b(phone|messages?|alarm|digital world)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();
}
