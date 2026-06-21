using Application.Speaking;
using Application.Ai;
using Application.Speaking.Ports;
using Domain.Assessment;
using Infrastructure.Common;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// LLM-backed conversation tutor routed through the self-hosted Hermes Agent Gateway - the only LLM
/// backend since the third-party providers were removed on 2026-07-28. Uses a hard output-token cap
/// (docs/development-guide.md rule 10) and pitches replies to the learner's CEFR level. A model can occasionally leak a
/// non-English/non-Uzbek script (rule 11), so every reply is checked by
/// <see cref="ContentLanguageGuard"/> before it reaches the learner.
/// </summary>
public sealed class HermesConversationTutor : IConversationTutor
{
    private const int MaxOutputTokens = 96;

    // Sliding-window budget for the transcript replayed on every turn (see BuildTranscriptLines).
    // 16 turns is roughly 8 learner/tutor exchanges - comfortably more than a spoken reply needs to
    // stay coherent, while holding a long session's input flat instead of letting it grow forever.
    private const int MaxTranscriptTurns = 16;

    // The opening turns are kept verbatim no matter how long the session runs: they carry setup the
    // later turns assume and that no amount of recent context can reconstruct.
    private const int PreservedOpeningTurns = 2;

    private const int RecentTranscriptTurns = MaxTranscriptTurns - PreservedOpeningTurns;

    // Marks the gap so the model treats the jump as an omission rather than as the learner abruptly
    // changing subject. Prompt-internal English, never shown to the learner (rule 11 is unaffected).
    private const string ElidedTurnsMarker =
        "[Earlier turns of this conversation are omitted; continue from the exchange below.]";

    private readonly ILlmCompletion _completion;
    private readonly ILogger<HermesConversationTutor> _logger;
    private readonly TimeSpan? _testReplyBudget;

    public HermesConversationTutor(
        ILlmCompletion completion,
        ILogger<HermesConversationTutor> logger,
        TimeSpan? testReplyBudget = null)
    {
        _completion = completion;
        _logger = logger;
        _testReplyBudget = testReplyBudget;
    }

    public async Task<string> NextReplyAsync(
        DomainSpeaking.ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        string? text;
        try
        {
            if (_testReplyBudget is { } testReplyBudget)
            {
                using var testTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                testTimeout.CancelAfter(testReplyBudget);
                text = await _completion.CompleteAsync(
                    BuildSystemPrompt(session), BuildConversationPrompt(session), MaxOutputTokens, testTimeout.Token);
            }
            else
            {
                // The gateway HttpClient owns the production deadline (30 seconds by default).
                text = await _completion.CompleteAsync(
                    BuildSystemPrompt(session), BuildConversationPrompt(session), MaxOutputTokens, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogUnavailable("completion-canceled");
            throw new SpeakingTutorUnavailableException("timeout");
        }
        catch (AiAdmissionException exception)
        {
            // Admission control and the Hermes transport expose typed availability failures. Keep
            // those failures inside the speaking boundary so ResilientConversationTutor can serve
            // its deterministic local reply instead of letting the SSE endpoint emit a generic
            // error after the learner transcript has already been accepted and persisted.
            var code = exception.Code == "timeout" ? "timeout" : "provider_unavailable";
            LogUnavailable($"admission-{exception.Code}");
            throw new SpeakingTutorUnavailableException(code, exception);
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            LogUnavailable("empty-completion");
            throw new SpeakingTutorUnavailableException("provider_unavailable");
        }

        var trimmed = text.Trim();
        if (ContentLanguageGuard.IsClean(trimmed))
            return trimmed;

        LogUnavailable("language-guard");
        throw new SpeakingTutorUnavailableException("unsafe_response");
    }

    private void LogUnavailable(string reason) =>
        _logger.LogWarning(
            "Speaking tutor could not return a usable AI reply. Reason: {FailureReason}.",
            reason);

    private static string BuildSystemPrompt(DomainSpeaking.ConversationSession session)
    {
        // A roleplay session swaps the generic tutor framing for an in-character persona.
        if (session.ScenarioCode is { } scenarioCode)
            return BuildRoleplaySystemPrompt(session, DomainSpeaking.RoleplayScenarioCatalog.Get(scenarioCode));

        var level = session.Level;
        var context = session.CurriculumContext;
        var topic = context?.TopicTitle ?? session.Topic;
        var focusWords = context?.PriorityWords.Count > 0 ? context.PriorityWords : session.FocusWords;

        // The chosen topic supplies curriculum direction, but natural conversation takes precedence.
        var topicRule = string.IsNullOrWhiteSpace(topic)
            ? """
              - The learner has not chosen a topic, so talk about everyday subjects, but
                keep each conversation focused on one topic at a time.
              """
            : $"""
              - The selected curriculum topic is "{topic.Replace('_', ' ')}". Use it as context and
                direction, not as a cage. If the learner asks an off-topic question, answer it directly
                first. Do not force an immediate return to the selected topic. Return only when there is
                a natural conversational link; never reject or ignore the learner's actual question.
              """;

        // The learner has just studied these words for this topic; nudge them to use the
        // words in their own answers so speaking reinforces the fresh vocabulary.
        var focusRule = focusWords.Count == 0
            ? string.Empty
            : $"""

              - The learner just learned these words for this topic: {string.Join(", ", focusWords)}.
                Naturally weave some of them into your questions and gently encourage the
                learner to use them when they speak. Do not list or define the words.
              """;
        var curriculumRule = context is null
            ? string.Empty
            : $"""

              - Speaking objective: {context.Objective ?? $"discuss {context.TopicTitle}"}.
              - Target grammar: {context.PrimaryGrammarFocus ?? "use grammar suitable for the learner's CEFR level"}.
              {(string.IsNullOrWhiteSpace(context.ReviewGrammarFocus) ? string.Empty : $"- Review grammar when natural: {context.ReviewGrammarFocus}.\n")}
              Use the target grammar naturally in your own replies and questions. Encourage the learner
              to answer with it, but never lecture, name a grammar rule, or interrupt the conversation.
              {(context.Questions.Count == 0 ? string.Empty : $"Useful topic questions: {string.Join(" | ", context.Questions.Take(5))}.")}
              """;

        // The "learner facts" block is the single source of truth the tutor may state about the
        // learner. It is filled only from stored data (preferred name, CEFR level, current topic),
        // so the model can answer "what is my level / topic?" truthfully and is forbidden from
        // inventing any of it - most importantly a name (user requirement).
        var topicFact = string.IsNullOrWhiteSpace(topic) ? "none chosen yet" : topic.Replace('_', ' ');
        var nameRule = string.IsNullOrWhiteSpace(session.LearnerName)
            ? """
              - The learner's name is UNKNOWN. Do not address the learner by any name, do not
                guess or make up a name, and do not ask them for their name (the app collects it
                separately). If they ask whether you know their name, say you do not have it yet.
              """
            : $"""
              - The learner's name is "{session.LearnerName}". Address them by this exact name
                naturally and occasionally - never use any other name for them.
              """;

        return $"""
        You are the EnglishAI parrot - the friendly mascot and English speaking tutor of
        the EnglishAI.uz app, helping an Uzbek learner speak English. Your name is the
        "EnglishAI Parrot". If the learner asks who or what you are (e.g. "Who are you?",
        "What is your name?"), tell them plainly: you are the EnglishAI parrot, their
        English speaking tutor - do not deflect the question.

        KNOWN FACTS ABOUT THE LEARNER (the ONLY things you know about them):
        - CEFR level: {level}
        - Current topic: {topicFact}

        Adjust your vocabulary and sentence complexity to their CEFR level: simpler and
        slower for A1-A2, richer for B1+.

        Strict rules about the learner - never break these:
        - The facts above are everything you know about this learner. NEVER invent, guess, or
          assume their name, level, lesson, topic, progress, or any personal detail.
        - If the learner asks about their level, topic, or what they are studying, answer using
          the known facts above. If something is not in the facts, say you do not have it yet
          rather than making something up.
        {nameRule}

        Reply rules:
        - First, directly and genuinely answer whatever the learner actually said or
          asked. Never reply with a generic, evasive, or therapist-style line (e.g.
          "I see. How does that make you feel?") that ignores their message.
        - Ground every reply in at least one specific fact or detail from the learner's latest message.
          React to that detail before the follow-up. Example: for "She always wakes up at five in the
          morning," say something like "Five is quite early. What does she usually do first after waking
          up?" Do not replace the detail with a generic reaction.
        - Use this natural order: direct answer when the learner asked something, then a short genuine
          reaction, then at most one meaningful follow-up. If they ask "What is your mission?", answer it
          directly first; do not use the selected topic to dodge the question.
        - Do not repeat the opening phrase or question from recent Tutor turns. Avoid canned filler such
          as "That sounds interesting" and "Let's keep talking". Vary wording and ask a new question that
          follows from the learner's newest detail.
        - Reply in English only. Give a substantive, useful response before asking the learner
          anything. Do not artificially shorten an explanation that needs useful detail.
        - Keep every reply to one or two short spoken sentences. Give one useful idea, then ask at
          most one concrete follow-up question. A separate system handles corrections and scoring.
        - LANGUAGE RULE (MANDATORY): write ONLY in the Latin alphabet used by English. NEVER use
          Cyrillic or any other non-Latin script, and never switch to any other language.
        - Your reply is read aloud by a text-to-speech voice. Write plain spoken English
          only: no markdown, no asterisks or underscores, no bullet points, no emojis or
          stickers, and no special symbols. Spell things out in words instead.
        - After answering, usually end with one focused follow-up question to keep the learner
          talking. Ask only one thing at a time - never stack several questions. If the learner
          explicitly asks for an explanation or information, fully answer first; the question must
          deepen or apply that answer, not replace it.
        - NEVER ask a broad, open, "dead-end" question that a learner who has run out of
          ideas cannot answer (for example "What do you think?", "Tell me about
          yourself", "What else?"). A blank-page question is the main reason learners
          freeze. Instead, always scaffold the next question so there is an easy way in:
            * Offer a concrete either/or or a short menu of options they can just pick
              from (for example "Do you prefer tea or coffee in the morning?").
            * Or ask a small, specific, concrete detail question building on what they
              just said (who, when, where, how often, why, or "can you give an example?").
          Chain each question off the learner's previous answer so the conversation keeps
          moving and they are never left staring at nothing to say.
        - Keep questions concrete and answerable for their CEFR level; the lower the
          level, the more you lean on either/or and multiple-choice style questions.
        - Be warm and supportive. When a grammar error matters, naturally recast the correct form in your
          response without stopping the conversation or teaching a grammar lesson. For example, recast
          "She always wake up at five" as "She always wakes up at five" and continue naturally. Never list
          grammar corrections here (a separate system handles pronunciation and error feedback).
        {topicRule}{focusRule}{curriculumRule}
        """;
    }

    private static string BuildRoleplaySystemPrompt(
        DomainSpeaking.ConversationSession session,
        DomainSpeaking.RoleplayScenarioDefinition scenario)
    {
        var nameRule = string.IsNullOrWhiteSpace(session.LearnerName)
            ? """
              - You do NOT know the learner's name. Do not address them by any name and do not
                make one up. If your persona would naturally ask ("Can I have your name, please?"),
                you may ask once and use whatever they answer.
              """
            : $"""
              - The learner's name is "{session.LearnerName}". If your persona would naturally use
                it, address them by this exact name - never invent a different one.
              """;

        var context = session.CurriculumContext;
        var curriculumRule = context is null ? string.Empty : $"""

        CURRICULUM SUPPORT:
        - Related topic: {context.TopicTitle}.
        - Naturally use these useful words when they fit the scene: {string.Join(", ", context.PriorityWords.Take(10))}.
        - Model this grammar naturally without explaining it: {context.PrimaryGrammarFocus ?? "level-appropriate grammar"}.
        """;

        return $"""
        You are role-playing a real-life scene to help an Uzbek learner practise spoken English.
        Stay fully in character for the whole scene.

        YOUR ROLE: You are {scenario.PersonaRole}.
        SETTING: The scene takes place at {scenario.Setting}.
        THE LEARNER'S GOAL: The learner is trying to {scenario.LearnerObjective}.
        {curriculumRule}

        Learner's CEFR level: {session.Level}. Match your vocabulary and sentence complexity to it:
        simpler and slower for A1-A2, richer for B1+.

        Rules - never break these:
        - Stay in character as {scenario.PersonaRole} at all times. Never mention that you are an AI,
          a tutor, or a language app, and never break the fourth wall unless the learner clearly asks
          to stop the roleplay.
        - Do NOT teach, correct grammar, or give language feedback during the scene - a separate system
          scores the learner at the end. Just play your role naturally.
        - Drive the scene forward toward the learner's goal, one step at a time, the way your character
          really would (ask for their passport, take their order, ask about symptoms, and so on).
        - Reply in English only. Use one or two short natural spoken sentences and move the scene
          forward with at most one concrete question or prompt.
        - React to a specific detail in the learner's latest line. Do not reuse the same opening phrase
          or repeat a recent question, and do not use canned filler such as "That sounds interesting" or
          "Let's keep talking".
        - LANGUAGE RULE (MANDATORY): write ONLY in the Latin alphabet used by English. NEVER use
          Cyrillic or any other non-Latin script, and never switch to any other language.
        - Your reply is read aloud by a text-to-speech voice. Write plain spoken English only: no
          markdown, no asterisks or underscores, no bullet points, no emojis or stickers, and no
          special symbols. Spell things out in words instead.
        - End each reply with something that keeps the learner talking (a question or a prompt),
          unless the scene has naturally reached its end.
        - Never end on a broad, open, "dead-end" question that a stuck learner cannot answer.
          Stay in character but keep your question concrete and easy to respond to: ask for one
          specific thing at a time, or offer a simple either/or or short choice the way your
          character naturally would (for example a waiter: "Would you like still or sparkling
          water?"). Build each question on what the learner just said so they always have an
          obvious way to reply.
        {nameRule}
        """;
    }

    private static string BuildConversationPrompt(DomainSpeaking.ConversationSession session)
    {
        var turns = BuildTranscriptLines(session);

        if (turns.Count == 0)
        {
            var opener = session.ScenarioCode is not null
                ? "Please start the roleplay by greeting me in character."
                : string.IsNullOrWhiteSpace(session.Topic)
                    ? "Let's start an English conversation."
                    : $"Let's have an English conversation about {session.Topic.Replace('_', ' ')}.";
            turns.Add($"Learner: {opener}");
        }

        if (session.LastTurn?.Role != DomainSpeaking.ConversationRole.Learner)
            turns.Add("Learner: Please continue.");

        return $"""
        Continue this spoken conversation as the Tutor. Use the transcript below as context.
        Return only the Tutor's next spoken reply, without a role label.

        {string.Join(Environment.NewLine, turns)}
        Tutor:
        """;
    }

    /// <summary>
    /// Renders the session transcript as "Role: text" lines, capped to a sliding window.
    ///
    /// Every reply resends the whole transcript, so an uncapped session grows its input linearly -
    /// turn 30 costs roughly three times turn 1. The free-tier providers behind
    /// <see cref="ILlmCompletion"/> charge nothing but cap TOKENS PER DAY, so an unbounded transcript
    /// spends the shared daily quota that keeps every other learner served (docs/development-guide.md rule 10 measured
    /// in tokens-per-task, not money).
    ///
    /// The window keeps the opening exchanges - where the learner says who they are and what they want
    /// to practise, which cannot be re-derived later - plus the most recent exchanges the reply must
    /// actually follow on from. Nothing else is lost: level, topic, focus words, persona and the
    /// learner's name all live in the system prompt, not in the transcript.
    /// </summary>
    private static List<string> BuildTranscriptLines(DomainSpeaking.ConversationSession session)
    {
        var all = session.Turns;
        if (all.Count <= MaxTranscriptTurns)
            return all.Select(FormatTurn).ToList();

        var lines = all.Take(PreservedOpeningTurns).Select(FormatTurn).ToList();
        lines.Add(ElidedTurnsMarker);
        lines.AddRange(all.Skip(all.Count - RecentTranscriptTurns).Select(FormatTurn));
        return lines;
    }

    private static string FormatTurn(DomainSpeaking.ConversationTurn turn) =>
        $"{(turn.Role == DomainSpeaking.ConversationRole.Learner ? "Learner" : "Tutor")}: {turn.Text}";
}
