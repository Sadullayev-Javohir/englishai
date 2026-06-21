using Domain.Assessment;

namespace Application.Writing.Content;

/// <summary>
/// One writing text type (genre) a learner can be asked to produce: its short English name (used to
/// steer the LLM), a concrete prompt template with <c>{title}</c> standing in for the topic, and a
/// few short English "what to include" guidance hints.
/// </summary>
public sealed record WritingGenre(string Name, string PromptTemplate, IReadOnlyList<string> Guidance);

/// <summary>
/// The set of CEFR-appropriate writing genres, plus a deterministic per-topic picker. Previously
/// every topic at a level produced the same task type (an A1/A2 topic was always "a short message to
/// a friend"), which made the Writing module monotonous. Here each level offers a varied range of
/// real text types (PROJECT-SPEC G.3 widened) and a topic is mapped to one of them by a stable hash
/// of its title - so different topics get different genres while the choice stays stable across
/// regenerations (the prompt is cached on the task, rules 8, 10). Difficulty still rises with level:
/// personal/everyday texts at A1–A2, opinion/transactional pieces at B1–B2, extended argument and
/// formal genres at C1–C2.
/// </summary>
public static class WritingGenreCatalog
{
    /// <summary>The genre for a topic at a level, chosen deterministically from its title.</summary>
    public static WritingGenre ForTopic(string title, CefrLevel level)
    {
        var genres = GenresFor(level);
        var index = StableIndex(title, genres.Count);
        return genres[index];
    }

    /// <summary>All genres available at a level (exposed for tests and content tooling).</summary>
    public static IReadOnlyList<WritingGenre> GenresFor(CefrLevel level) => level switch
    {
        CefrLevel.A1 => A1,
        CefrLevel.A2 => A2,
        CefrLevel.B1 => B1,
        CefrLevel.B2 => B2,
        CefrLevel.C1 => C1,
        _ => C2,
    };

    // A1: very short, personal, everyday texts. More guidance as support (rule: richer hints low).
    private static readonly IReadOnlyList<WritingGenre> A1 = new[]
    {
        new WritingGenre(
            "short message to a friend",
            "Write a short message to a friend about {title}. Say what it is and why you like it.",
            new[] { "Use simple sentences.", "Say what you like and why.", "Use everyday words from this topic." }),
        new WritingGenre(
            "postcard",
            "Write a postcard to a friend. Tell them about {title} and what you did.",
            new[] { "Start with 'Hi' and end with your name.", "Say where you are or what happened.", "Use simple past or present." }),
        new WritingGenre(
            "short email",
            "Write a short email to a friend about {title}. Share your news and ask one question.",
            new[] { "Begin with 'Hi' and a greeting.", "Give your news in two or three sentences.", "Finish with a question for your friend." }),
        new WritingGenre(
            "simple description",
            "Write a few sentences describing {title}. Use simple words so a beginner can picture it.",
            new[] { "Say what it is and where.", "Add two or three details.", "Use 'is', 'are' and simple adjectives." }),
        new WritingGenre(
            "diary entry",
            "Write a short diary entry about {title} today. Say what happened and how you felt.",
            new[] { "Start with 'Today'.", "Say what you did, step by step.", "End with how you felt." }),
        new WritingGenre(
            "invitation",
            "Write a short message inviting a friend to {title}. Say when, where and why.",
            new[] { "Say what the event is.", "Give the day, time and place.", "Ask your friend to come." }),
    };

    // A2: still short and personal, but more genres and connected sentences.
    private static readonly IReadOnlyList<WritingGenre> A2 = new[]
    {
        new WritingGenre(
            "informal email",
            "Write an informal email to a friend about {title}. Tell them what happened and how it went.",
            new[] { "Greet your friend.", "Describe what happened with some detail.", "Use linking words like 'and', 'but', 'because'." }),
        new WritingGenre(
            "blog post",
            "Write a short blog post about {title} for other learners. Share your experience and a tip.",
            new[] { "Give your post a short title.", "Describe your experience.", "Finish with one helpful tip." }),
        new WritingGenre(
            "short story",
            "Write a short story about {title}. Give it a beginning, a middle and an end.",
            new[] { "Set the scene (who, where, when).", "Say what happened.", "End with how it finished." }),
        new WritingGenre(
            "short review",
            "Write a short review of something connected to {title} (a place, film, app or product).",
            new[] { "Say what you are reviewing.", "Give your opinion with one reason.", "Say if you recommend it." }),
        new WritingGenre(
            "advice message",
            "A friend asks you about {title}. Write a message giving them some simple advice.",
            new[] { "Say you understand their problem.", "Give two pieces of advice.", "Use 'should' and 'could'." }),
        new WritingGenre(
            "simple instructions",
            "Write simple instructions explaining how to do something related to {title}.",
            new[] { "List the steps in order.", "Use 'first', 'then', 'finally'.", "Keep each step short and clear." }),
    };

    // B1: longer, with opinion and transactional genres.
    private static readonly IReadOnlyList<WritingGenre> B1 = new[]
    {
        new WritingGenre(
            "opinion essay",
            "Some people have strong views about {title}. Write a short essay giving your own opinion with clear reasons.",
            new[] { "State your opinion in the first sentence.", "Give at least two reasons with examples.", "Finish with a short conclusion." }),
        new WritingGenre(
            "informal letter",
            "Write a letter to a friend about {title}, telling them your news and asking about theirs.",
            new[] { "Open and close the letter properly.", "Give your news in detail.", "Ask your friend questions." }),
        new WritingGenre(
            "review",
            "Write a review about something connected to {title} (a film, book, restaurant or product) for a website.",
            new[] { "Describe what you are reviewing.", "Give both good and bad points.", "End with a clear recommendation." }),
        new WritingGenre(
            "article",
            "Write a short article about {title} for a learners' magazine. Make it interesting to read.",
            new[] { "Start with a question or surprising fact.", "Give two or three main points.", "Use linking words to connect ideas." }),
        new WritingGenre(
            "email of request",
            "Write a semi-formal email connected to {title}, asking for information or making a request.",
            new[] { "State your reason for writing.", "Make your request clearly and politely.", "Close politely." }),
        new WritingGenre(
            "story",
            "Write a story about {title}. Build it around a clear event and how it ended.",
            new[] { "Set the scene and the characters.", "Describe the main event.", "Use past tenses and time linkers." }),
    };

    // B2: opinion, formal and report-style genres with structure.
    private static readonly IReadOnlyList<WritingGenre> B2 = new[]
    {
        new WritingGenre(
            "discursive essay",
            "\"{title}\" can be seen in different ways. Write an essay discussing both sides before giving your view.",
            new[] { "Introduce the issue.", "Present arguments for and against in separate paragraphs.", "End with your own reasoned view." }),
        new WritingGenre(
            "formal letter",
            "Write a formal letter connected to {title} (for example a request, application or complaint).",
            new[] { "Use formal openings and closings.", "State your purpose in the first paragraph.", "Keep a polite, formal tone." }),
        new WritingGenre(
            "review",
            "Write a detailed review about something connected to {title} for a magazine or website.",
            new[] { "Give context and a clear opinion.", "Support judgements with specific detail.", "Make a recommendation to a stated reader." }),
        new WritingGenre(
            "report",
            "Write a short report about {title} for a manager or committee, with findings and a recommendation.",
            new[] { "Use headings or clear sections.", "Present findings objectively.", "End with recommendations." }),
        new WritingGenre(
            "proposal",
            "Write a proposal about {title}, suggesting an improvement or a plan and the reasons for it.",
            new[] { "Describe the current situation.", "Set out your proposal clearly.", "Explain the benefits." }),
        new WritingGenre(
            "article",
            "Write an engaging article about {title} for a general audience, with a clear angle.",
            new[] { "Open with a hook.", "Develop two or three points with examples.", "Close with a memorable ending." }),
    };

    // C1: extended argument and formal genres; fewer hints (learner needs less scaffolding).
    private static readonly IReadOnlyList<WritingGenre> C1 = new[]
    {
        new WritingGenre(
            "argumentative essay",
            "Write an argumentative essay about {title}, defending a clear position against possible objections.",
            new[] { "State and develop a clear thesis.", "Address and rebut a counter-argument.", "Use precise vocabulary and cohesion." }),
        new WritingGenre(
            "report",
            "Write an analytical report on {title}, presenting evidence and well-supported recommendations.",
            new[] { "Structure it with clear sections.", "Interpret evidence, do not just list it.", "Make actionable recommendations." }),
        new WritingGenre(
            "proposal",
            "Write a persuasive proposal about {title}, justifying a course of action to a decision-maker.",
            new[] { "Frame the need and the goal.", "Set out the plan and weigh trade-offs.", "Persuade with evidence." }),
        new WritingGenre(
            "critical review",
            "Write a critical review of something connected to {title}, evaluating its strengths and weaknesses.",
            new[] { "Summarise briefly, then evaluate.", "Support each judgement with detail.", "Reach a balanced overall verdict." }),
        new WritingGenre(
            "discursive essay",
            "Write a discursive essay exploring the complexities of {title} before reaching a nuanced conclusion.",
            new[] { "Explore more than one perspective fairly.", "Use hedging and precise modality.", "Conclude with a nuanced position." }),
        new WritingGenre(
            "formal letter",
            "Write a formal letter connected to {title} that argues a case persuasively and professionally.",
            new[] { "Open with your precise purpose.", "Build the case logically.", "Maintain a formal, persuasive register." }),
    };

    // C2: sophisticated argument, evaluation and editorial genres; minimal scaffolding.
    private static readonly IReadOnlyList<WritingGenre> C2 = new[]
    {
        new WritingGenre(
            "argumentative essay",
            "Write a sophisticated argumentative essay on {title}, weighing competing views and defending a considered position.",
            new[] { "Develop a subtle, well-defined argument.", "Engage seriously with opposing views." }),
        new WritingGenre(
            "critical review",
            "Write a critical review connected to {title}, offering a discerning evaluation for an informed reader.",
            new[] { "Evaluate against clear criteria.", "Sustain an authoritative, nuanced voice." }),
        new WritingGenre(
            "proposal",
            "Write a high-level proposal about {title}, making a compelling, well-reasoned case for action.",
            new[] { "Frame strategy, risks and benefits.", "Persuade with rigour and precision." }),
        new WritingGenre(
            "analytical report",
            "Write an analytical report on {title} that synthesises evidence into insightful conclusions.",
            new[] { "Synthesise rather than summarise.", "Draw insightful, defensible conclusions." }),
        new WritingGenre(
            "editorial",
            "Write an editorial-style opinion piece on {title}, taking a clear stance with style and authority.",
            new[] { "Take a clear, confident stance.", "Use rhetoric and varied structure for effect." }),
        new WritingGenre(
            "discursive essay",
            "Write a discursive essay examining the tensions within {title} and arriving at a refined judgement.",
            new[] { "Examine tensions and trade-offs.", "Reach a refined, qualified judgement." }),
    };

    // Deterministic, framework-independent index from the title (.NET string hashing is randomised
    // per process, so we use a fixed FNV-1a hash to keep a topic's genre stable across runs/caches).
    private static int StableIndex(string title, int count)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            var hash = offsetBasis;
            foreach (var ch in title.Trim().ToLowerInvariant())
            {
                hash ^= ch;
                hash *= prime;
            }

            return (int)(hash % (uint)count);
        }
    }
}
