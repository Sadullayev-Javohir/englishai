using Domain.Assessment;
using Infrastructure.Common;

namespace Infrastructure.Assessment;

/// <summary>
/// Seed of the CEFR placement question bank. Prompts/options are English content
/// (data, not generated). Vocabulary/grammar items follow the Uzbek-speaker priority
/// order from PROJECT-SPEC G.2 (articles, tenses, prepositions, gerund/infinitive,
/// modals). Each stage has several items per CEFR level so the adaptive engine can
/// vary the test across learners and reach the full A1..C2 range without exhausting
/// a level.
///
/// Listening items keep the spoken text in <c>audioScript</c> (synthesized by TTS and
/// streamed as audio); the visible prompt only asks the comprehension question, so the
/// learner cannot read the answer. Reading items carry a real <c>passageText</c> the
/// learner reads before answering the comprehension question.
///
/// The Writing and Speaking stages are no longer multiple-choice: they are productive
/// tasks (free text / recorded audio) supplied by the placement task providers and graded
/// by the writing / speaking assessor ports, so this bank holds no Writing/Speaking items.
/// </summary>
public static class QuestionBankSeed
{
    public static IReadOnlyList<PlacementQuestion> Questions { get; } = Build();

    private static PlacementQuestion Q(
        string key, TestStage stage, CefrLevel level,
        string prompt, string[] options, int correct) =>
        PlacementQuestion.Create(DeterministicGuid.Create(key), stage, level, prompt, options, correct);

    /// <summary>Listening item: <paramref name="audioScript"/> is spoken, not shown.</summary>
    private static PlacementQuestion LQ(
        string key, CefrLevel level,
        string audioScript, string prompt, string[] options, int correct) =>
        PlacementQuestion.Create(
            DeterministicGuid.Create(key), TestStage.Listening, level, prompt, options, correct, audioScript);

    /// <summary>Reading item: <paramref name="passage"/> is shown above the question.</summary>
    private static PlacementQuestion RQ(
        string key, CefrLevel level,
        string passage, string prompt, string[] options, int correct) =>
        PlacementQuestion.Create(
            DeterministicGuid.Create(key), TestStage.Reading, level, prompt, options, correct,
            audioScript: null, passageText: passage);

    private static IReadOnlyList<PlacementQuestion> Build() => new List<PlacementQuestion>
    {
        // =====================================================================
        // Vocabulary (meaning, context, collocation and register, A1 -> C2)
        // =====================================================================
        Q("vc-a1-1", TestStage.Vocabulary, CefrLevel.A1, "Which word means a place where you sleep?", new[] { "bed", "door", "plate", "shoe" }, 0),
        Q("vc-a1-2", TestStage.Vocabulary, CefrLevel.A1, "The opposite of 'big' is ___.", new[] { "long", "small", "fast", "old" }, 1),
        Q("vc-a1-3", TestStage.Vocabulary, CefrLevel.A1, "You drink water from a ___.", new[] { "cup", "sock", "key", "wall" }, 0),
        Q("vc-a1-4", TestStage.Vocabulary, CefrLevel.A1, "Which word is a colour?", new[] { "green", "chair", "walk", "bread" }, 0),
        Q("vc-a1-5", TestStage.Vocabulary, CefrLevel.A1, "A doctor works in a ___.", new[] { "hospital", "station", "farm", "cinema" }, 0),
        Q("vc-a1-6", TestStage.Vocabulary, CefrLevel.A1, "Which word means 'not happy'?", new[] { "sad", "clean", "young", "warm" }, 0),

        Q("vc-a2-1", TestStage.Vocabulary, CefrLevel.A2, "Please ___ the window. It is cold outside.", new[] { "close", "cook", "carry", "climb" }, 0),
        Q("vc-a2-2", TestStage.Vocabulary, CefrLevel.A2, "We missed the bus, so we had to ___.", new[] { "wait", "invite", "borrow", "teach" }, 0),
        Q("vc-a2-3", TestStage.Vocabulary, CefrLevel.A2, "Which word is closest in meaning to 'begin'?", new[] { "finish", "start", "forget", "break" }, 1),
        Q("vc-a2-4", TestStage.Vocabulary, CefrLevel.A2, "You use a map when you need ___.", new[] { "directions", "medicine", "homework", "change" }, 0),
        Q("vc-a2-5", TestStage.Vocabulary, CefrLevel.A2, "I need to ___ a photo of this view.", new[] { "take", "make", "do", "put" }, 0),
        Q("vc-a2-6", TestStage.Vocabulary, CefrLevel.A2, "The train was very ___, so no one could sit down.", new[] { "crowded", "empty", "quiet", "private" }, 0),

        Q("vc-b1-1", TestStage.Vocabulary, CefrLevel.B1, "If you 'avoid' something, you ___.", new[] { "stay away from it", "look for it", "pay for it", "agree with it" }, 0),
        Q("vc-b1-2", TestStage.Vocabulary, CefrLevel.B1, "We need to ___ a decision by Friday.", new[] { "make", "do", "build", "set" }, 0),
        Q("vc-b1-3", TestStage.Vocabulary, CefrLevel.B1, "Her explanation was very ___, so everyone understood.", new[] { "clear", "rare", "narrow", "rough" }, 0),
        Q("vc-b1-4", TestStage.Vocabulary, CefrLevel.B1, "Which word is closest in meaning to 'improve'?", new[] { "get better", "slow down", "give up", "turn back" }, 0),
        Q("vc-b1-5", TestStage.Vocabulary, CefrLevel.B1, "The company plans to ___ twenty new employees.", new[] { "hire", "lend", "deliver", "repair" }, 0),
        Q("vc-b1-6", TestStage.Vocabulary, CefrLevel.B1, "I was ___ to hear that everyone was safe.", new[] { "relieved", "ordinary", "similar", "available" }, 0),

        Q("vc-b2-1", TestStage.Vocabulary, CefrLevel.B2, "The new evidence may ___ the outcome of the trial.", new[] { "influence", "insist", "involve", "interrupt" }, 0),
        Q("vc-b2-2", TestStage.Vocabulary, CefrLevel.B2, "The two reports are broadly ___.", new[] { "consistent", "convenient", "continuous", "considerate" }, 0),
        Q("vc-b2-3", TestStage.Vocabulary, CefrLevel.B2, "She took full ___ for the mistake.", new[] { "responsibility", "opportunity", "permission", "advantage" }, 0),
        Q("vc-b2-4", TestStage.Vocabulary, CefrLevel.B2, "A 'substantial' increase is ___.", new[] { "large and important", "small and temporary", "unexpected but harmless", "slow and regular" }, 0),
        Q("vc-b2-5", TestStage.Vocabulary, CefrLevel.B2, "The campaign aims to ___ awareness of the issue.", new[] { "raise", "rise", "lift up", "grow up" }, 0),
        Q("vc-b2-6", TestStage.Vocabulary, CefrLevel.B2, "His argument was based on a false ___.", new[] { "assumption", "permission", "occasion", "reputation" }, 0),

        Q("vc-c1-1", TestStage.Vocabulary, CefrLevel.C1, "The findings are 'inconclusive', meaning they ___.", new[] { "do not prove anything clearly", "are certainly wrong", "confirm every prediction", "cannot be published" }, 0),
        Q("vc-c1-2", TestStage.Vocabulary, CefrLevel.C1, "The reforms were introduced in an attempt to ___ corruption.", new[] { "curb", "cherish", "compile", "concede" }, 0),
        Q("vc-c1-3", TestStage.Vocabulary, CefrLevel.C1, "Her account of events was internally ___.", new[] { "coherent", "courteous", "conventional", "convenient" }, 0),
        Q("vc-c1-4", TestStage.Vocabulary, CefrLevel.C1, "The announcement ___ widespread criticism.", new[] { "prompted", "performed", "preserved", "prevailed" }, 0),
        Q("vc-c1-5", TestStage.Vocabulary, CefrLevel.C1, "The benefits must be weighed ___ the potential risks.", new[] { "against", "towards", "beneath", "among" }, 0),
        Q("vc-c1-6", TestStage.Vocabulary, CefrLevel.C1, "His apology did little to ___ their concerns.", new[] { "allay", "allocate", "alter", "allege" }, 0),

        Q("vc-c2-1", TestStage.Vocabulary, CefrLevel.C2, "A 'spurious' claim is ___.", new[] { "false but seemingly plausible", "brief but accurate", "widely accepted", "carefully documented" }, 0),
        Q("vc-c2-2", TestStage.Vocabulary, CefrLevel.C2, "The witness gave an ___ account that omitted several key facts.", new[] { "equivocal", "exemplary", "exhaustive", "eloquent" }, 0),
        Q("vc-c2-3", TestStage.Vocabulary, CefrLevel.C2, "The ruling may set a legal ___.", new[] { "precedent", "predecessor", "premise", "pretext" }, 0),
        Q("vc-c2-4", TestStage.Vocabulary, CefrLevel.C2, "The proposal was dismissed as financially ___.", new[] { "untenable", "unassuming", "unfounded", "unrivalled" }, 0),
        Q("vc-c2-5", TestStage.Vocabulary, CefrLevel.C2, "Her criticism was so ___ that its seriousness was easy to miss.", new[] { "understated", "overwrought", "unequivocal", "indiscriminate" }, 0),
        Q("vc-c2-6", TestStage.Vocabulary, CefrLevel.C2, "The evidence ___ his version of events.", new[] { "corroborates", "culminates", "circumvents", "contemplates" }, 0),

        // =====================================================================
        // Grammar (Uzbek-speaker priority order, A1 -> C2)
        // =====================================================================
        Q("vg-a1-1", TestStage.Grammar, CefrLevel.A1, "I have ___ apple.", new[] { "a", "an", "the", "(no word)" }, 1),
        Q("vg-a1-2", TestStage.Grammar, CefrLevel.A1, "She ___ a teacher.", new[] { "am", "is", "are", "be" }, 1),
        Q("vg-a1-3", TestStage.Grammar, CefrLevel.A1, "They ___ happy today.", new[] { "is", "am", "are", "be" }, 2),
        Q("vg-a1-4", TestStage.Grammar, CefrLevel.A1, "This is ___ book. (pointing at one near you)", new[] { "a", "an", "the", "these" }, 0),
        Q("vg-a1-5", TestStage.Grammar, CefrLevel.A1, "I ___ from Uzbekistan.", new[] { "is", "am", "are", "be" }, 1),

        Q("vg-a2-1", TestStage.Grammar, CefrLevel.A2, "I ___ to school every day.", new[] { "go", "goes", "going", "went" }, 0),
        Q("vg-a2-2", TestStage.Grammar, CefrLevel.A2, "Look! He ___ TV right now.", new[] { "watch", "watches", "is watching", "watched" }, 2),
        Q("vg-a2-3", TestStage.Grammar, CefrLevel.A2, "There is ___ water in the glass.", new[] { "some", "any", "many", "a" }, 0),
        Q("vg-a2-4", TestStage.Grammar, CefrLevel.A2, "Yesterday I ___ to the market.", new[] { "go", "goes", "went", "going" }, 2),
        Q("vg-a2-5", TestStage.Grammar, CefrLevel.A2, "She is taller ___ her brother.", new[] { "then", "than", "that", "as" }, 1),
        Q("vg-a2-6", TestStage.Grammar, CefrLevel.A2, "We arrive ___ Monday morning.", new[] { "in", "at", "on", "to" }, 2),

        Q("vg-b1-1", TestStage.Grammar, CefrLevel.B1, "I have lived here ___ 2010.", new[] { "since", "for", "from", "at" }, 0),
        Q("vg-b1-2", TestStage.Grammar, CefrLevel.B1, "If it rains, we ___ at home.", new[] { "stay", "will stay", "stayed", "staying" }, 1),
        Q("vg-b1-3", TestStage.Grammar, CefrLevel.B1, "She enjoys ___ books in the evening.", new[] { "read", "to read", "reading", "reads" }, 2),
        Q("vg-b1-4", TestStage.Grammar, CefrLevel.B1, "I haven't seen that film ___.", new[] { "already", "yet", "still", "ever" }, 1),
        Q("vg-b1-5", TestStage.Grammar, CefrLevel.B1, "He's the man ___ car was stolen.", new[] { "who", "which", "whose", "whom" }, 2),
        Q("vg-b1-6", TestStage.Grammar, CefrLevel.B1, "You ___ smoke here; it's forbidden.", new[] { "mustn't", "don't have to", "needn't", "shouldn't have" }, 0),

        Q("vg-b2-1", TestStage.Grammar, CefrLevel.B2, "By the time we arrived, the film ___.", new[] { "started", "has started", "had started", "starts" }, 2),
        Q("vg-b2-2", TestStage.Grammar, CefrLevel.B2, "He suggested ___ a short break.", new[] { "take", "to take", "taking", "took" }, 2),
        Q("vg-b2-3", TestStage.Grammar, CefrLevel.B2, "I'd rather you ___ now.", new[] { "leave", "left", "to leave", "leaving" }, 1),
        Q("vg-b2-4", TestStage.Grammar, CefrLevel.B2, "If I ___ earlier, I wouldn't have missed the train.", new[] { "left", "had left", "have left", "would leave" }, 1),
        Q("vg-b2-5", TestStage.Grammar, CefrLevel.B2, "The report needs ___ before Friday.", new[] { "finish", "to finishing", "finishing", "finished" }, 2),
        Q("vg-b2-6", TestStage.Grammar, CefrLevel.B2, "She's used to ___ in a big city.", new[] { "live", "living", "lived", "to live" }, 1),

        Q("vg-c1-1", TestStage.Grammar, CefrLevel.C1, "Hardly ___ down when the phone rang.", new[] { "I had sat", "had I sat", "I sat", "did I sit" }, 1),
        Q("vg-c1-2", TestStage.Grammar, CefrLevel.C1, "The proposal was turned ___ by the board.", new[] { "down", "off", "up", "over" }, 0),
        Q("vg-c1-3", TestStage.Grammar, CefrLevel.C1, "Were it not ___ your help, we would have failed.", new[] { "for", "to", "with", "of" }, 0),
        Q("vg-c1-4", TestStage.Grammar, CefrLevel.C1, "No sooner had she left ___ the trouble began.", new[] { "when", "than", "then", "that" }, 1),
        Q("vg-c1-5", TestStage.Grammar, CefrLevel.C1, "The new policy will come ___ next month.", new[] { "into effect", "in effect", "to effect", "on effect" }, 0),

        Q("vg-c2-1", TestStage.Grammar, CefrLevel.C2, "She has an encyclopedic ___ of history.", new[] { "command", "grasp", "knowledge", "hold" }, 2),
        Q("vg-c2-2", TestStage.Grammar, CefrLevel.C2, "His argument was, at best, ___.", new[] { "tenuous", "robust", "concrete", "sound" }, 0),
        Q("vg-c2-3", TestStage.Grammar, CefrLevel.C2, "The committee ___ the decision indefinitely.", new[] { "deferred", "inferred", "conferred", "preferred" }, 0),
        Q("vg-c2-4", TestStage.Grammar, CefrLevel.C2, "Her remarks ___ the very heart of the matter.", new[] { "went to", "got at", "cut to", "came at" }, 2),

        // --- Additional Vocabulary & Grammar items (bank expansion for test variety) ---
        Q("vg-a1-6", TestStage.Grammar, CefrLevel.A1, "We ___ students.", new[] { "is", "am", "are", "be" }, 2),
        Q("vg-a1-7", TestStage.Grammar, CefrLevel.A1, "___ is your name?", new[] { "What", "Where", "When", "Who" }, 0),
        Q("vg-a1-8", TestStage.Grammar, CefrLevel.A1, "I have two ___.", new[] { "book", "books", "bookes", "a book" }, 1),
        Q("vg-a1-9", TestStage.Grammar, CefrLevel.A1, "She ___ like coffee.", new[] { "don't", "doesn't", "isn't", "not" }, 1),
        Q("vg-a1-10", TestStage.Grammar, CefrLevel.A1, "There ___ a cat on the chair.", new[] { "is", "are", "am", "be" }, 0),
        Q("vg-a1-11", TestStage.Grammar, CefrLevel.A1, "He ___ got a new phone.", new[] { "have", "has", "is", "do" }, 1),
        Q("vg-a1-12", TestStage.Grammar, CefrLevel.A1, "My sister is ___ engineer.", new[] { "a", "an", "the", "(no word)" }, 1),
        Q("vg-a1-13", TestStage.Grammar, CefrLevel.A1, "___ you like tea?", new[] { "Do", "Does", "Are", "Is" }, 0),

        Q("vg-a2-7", TestStage.Grammar, CefrLevel.A2, "I ___ TV last night.", new[] { "watch", "watched", "watching", "watches" }, 1),
        Q("vg-a2-8", TestStage.Grammar, CefrLevel.A2, "This box is ___ than that one.", new[] { "heavy", "heavier", "heaviest", "more heavy" }, 1),
        Q("vg-a2-9", TestStage.Grammar, CefrLevel.A2, "I'm good ___ cooking.", new[] { "at", "in", "on", "for" }, 0),
        Q("vg-a2-10", TestStage.Grammar, CefrLevel.A2, "There aren't ___ apples left.", new[] { "some", "any", "much", "a" }, 1),
        Q("vg-a2-11", TestStage.Grammar, CefrLevel.A2, "She ___ to the gym twice a week.", new[] { "go", "goes", "going", "gone" }, 1),
        Q("vg-a2-12", TestStage.Grammar, CefrLevel.A2, "What ___ you do yesterday?", new[] { "do", "does", "did", "done" }, 2),
        Q("vg-a2-13", TestStage.Grammar, CefrLevel.A2, "It's the ___ film I've ever seen.", new[] { "good", "better", "best", "well" }, 2),

        Q("vg-b1-7", TestStage.Grammar, CefrLevel.B1, "If I won the lottery, I ___ buy a house.", new[] { "will", "would", "would have", "am going to" }, 1),
        Q("vg-b1-8", TestStage.Grammar, CefrLevel.B1, "The house ___ in 1990.", new[] { "built", "was built", "has built", "is building" }, 1),
        Q("vg-b1-9", TestStage.Grammar, CefrLevel.B1, "I'm interested ___ learning Spanish.", new[] { "in", "on", "at", "for" }, 0),
        Q("vg-b1-10", TestStage.Grammar, CefrLevel.B1, "She asked me where I ___.", new[] { "live", "lived", "living", "will live" }, 1),
        Q("vg-b1-11", TestStage.Grammar, CefrLevel.B1, "You ___ wear a uniform; it's a rule.", new[] { "must", "can", "might", "would" }, 0),
        Q("vg-b1-12", TestStage.Grammar, CefrLevel.B1, "I've known her ___ five years.", new[] { "since", "for", "from", "during" }, 1),
        Q("vg-b1-13", TestStage.Grammar, CefrLevel.B1, "This is ___ interesting book I've read.", new[] { "more", "most", "the most", "the more" }, 2),

        Q("vg-b2-7", TestStage.Grammar, CefrLevel.B2, "I wish I ___ more time to finish.", new[] { "have", "had", "will have", "having" }, 1),
        Q("vg-b2-8", TestStage.Grammar, CefrLevel.B2, "The project will ___ completed by Friday.", new[] { "be", "been", "being", "have" }, 0),
        Q("vg-b2-9", TestStage.Grammar, CefrLevel.B2, "She speaks English ___ if she were a native.", new[] { "as", "like", "so", "such" }, 0),
        Q("vg-b2-10", TestStage.Grammar, CefrLevel.B2, "Not only ___ late, but he also forgot the tickets.", new[] { "he was", "was he", "he is", "did he" }, 1),
        Q("vg-b2-11", TestStage.Grammar, CefrLevel.B2, "I'd prefer you ___ smoke in here.", new[] { "don't", "didn't", "won't", "not" }, 1),
        Q("vg-b2-12", TestStage.Grammar, CefrLevel.B2, "It's high time we ___ home.", new[] { "go", "went", "have gone", "going" }, 1),
        Q("vg-b2-13", TestStage.Grammar, CefrLevel.B2, "The more you practise, ___ you become.", new[] { "better", "the better", "the best", "more better" }, 1),

        Q("vg-c1-6", TestStage.Grammar, CefrLevel.C1, "Scarcely had I arrived ___ it started to rain.", new[] { "than", "when", "then", "that" }, 1),
        Q("vg-c1-7", TestStage.Grammar, CefrLevel.C1, "Only after the meeting ___ the truth.", new[] { "I learned", "did I learn", "I did learn", "learned I" }, 1),
        Q("vg-c1-8", TestStage.Grammar, CefrLevel.C1, "He talks as though he ___ everything.", new[] { "knows", "knew", "has known", "know" }, 1),
        Q("vg-c1-9", TestStage.Grammar, CefrLevel.C1, "But for your help, I ___ failed.", new[] { "would have", "will have", "had", "would" }, 0),
        Q("vg-c1-10", TestStage.Grammar, CefrLevel.C1, "The matter is ___ consideration.", new[] { "under", "in", "on", "at" }, 0),
        Q("vg-c1-11", TestStage.Grammar, CefrLevel.C1, "Such ___ his anger that he left the room.", new[] { "was", "were", "is", "had" }, 0),
        Q("vg-c1-12", TestStage.Grammar, CefrLevel.C1, "I'd sooner you ___ nothing about it.", new[] { "say", "said", "says", "saying" }, 1),
        Q("vg-c1-13", TestStage.Grammar, CefrLevel.C1, "On no account ___ this door be left open.", new[] { "must", "can", "may", "will" }, 0),

        Q("vg-c2-5", TestStage.Grammar, CefrLevel.C2, "The new evidence ___ doubt on the original verdict.", new[] { "cast", "threw", "put", "laid" }, 0),
        Q("vg-c2-6", TestStage.Grammar, CefrLevel.C2, "She has a ___ for languages.", new[] { "flair", "flare", "glare", "blare" }, 0),
        Q("vg-c2-7", TestStage.Grammar, CefrLevel.C2, "His comments were wide of the ___.", new[] { "mark", "point", "target", "spot" }, 0),
        Q("vg-c2-8", TestStage.Grammar, CefrLevel.C2, "They reached a ___ agreement after hours of talks.", new[] { "tentative", "tenacious", "tendentious", "tenable" }, 0),
        Q("vg-c2-9", TestStage.Grammar, CefrLevel.C2, "The plan was scuppered by a ___ of funding.", new[] { "lack", "miss", "fault", "gap" }, 0),
        Q("vg-c2-10", TestStage.Grammar, CefrLevel.C2, "He is ___ to making rash decisions.", new[] { "prone", "eager", "keen", "fond" }, 0),
        Q("vg-c2-11", TestStage.Grammar, CefrLevel.C2, "The negotiations have reached an ___.", new[] { "impasse", "impact", "impulse", "impetus" }, 0),
        Q("vg-c2-12", TestStage.Grammar, CefrLevel.C2, "Her explanation only served to ___ the confusion.", new[] { "compound", "compose", "comprise", "compress" }, 0),
        Q("vg-c2-13", TestStage.Grammar, CefrLevel.C2, "We must not ___ the gravity of the situation.", new[] { "underestimate", "undermine", "undertake", "undergo" }, 0),

        // =====================================================================
        // Listening (audioScript is spoken aloud; the prompt does not reveal it)
        // =====================================================================
        LQ("ls-a2-1", CefrLevel.A2, "The meeting is at three o'clock.", "When is the meeting?", new[] { "At 2:00", "At 3:00", "At 4:00", "At 5:00" }, 1),
        LQ("ls-a2-2", CefrLevel.A2, "Turn left at the bank, then go straight.", "Where do you turn left?", new[] { "At the school", "At the bank", "At the park", "At the shop" }, 1),
        LQ("ls-a2-3", CefrLevel.A2, "I'd like a coffee and a piece of cake, please.", "What does the speaker order?", new[] { "Tea and cake", "Coffee and cake", "Coffee and bread", "Juice and cake" }, 1),
        LQ("ls-b1-1", CefrLevel.B1, "I'd love to come, but I'm busy on Friday.", "Can the speaker come on Friday?", new[] { "Yes", "No", "Maybe", "Not stated" }, 1),
        LQ("ls-b1-2", CefrLevel.B1, "The train was delayed due to bad weather.", "Why was the train delayed?", new[] { "A strike", "Bad weather", "An accident", "Maintenance" }, 1),
        LQ("ls-b1-3", CefrLevel.B1, "If you ask me, the second option is much better value.", "Which option does the speaker prefer?", new[] { "The first", "The second", "Neither", "Both equally" }, 1),
        LQ("ls-b2-1", CefrLevel.B2, "Although sales rose sharply, profits actually fell.", "What happened to profits?", new[] { "They rose", "They fell", "They stayed the same", "They doubled" }, 1),
        LQ("ls-b2-2", CefrLevel.B2, "We really should have booked the hotel earlier.", "What does the speaker regret?", new[] { "Booking too late", "Booking too early", "Not travelling", "Paying too much" }, 0),
        LQ("ls-b2-3", CefrLevel.B2, "The proposal isn't without merit, but it raises serious concerns.", "What is the speaker's overall view of the proposal?", new[] { "Fully positive", "Fully negative", "Mixed", "Indifferent" }, 2),
        LQ("ls-c1-1", CefrLevel.C1, "Had the committee acted sooner, the crisis might well have been averted.", "According to the speaker, the crisis ___.", new[] { "was unavoidable", "could have been prevented", "was caused by the committee", "did not happen" }, 1),
        LQ("ls-c1-2", CefrLevel.C1, "Her presentation was polished, if a touch overlong.", "What slight criticism does the speaker make?", new[] { "It was too short", "It was disorganized", "It was too long", "It was unclear" }, 2),

        // --- Additional Listening items (bank expansion; now spans A1..C2) ---
        LQ("ls-a1-1", CefrLevel.A1, "My name is Anna. I am ten years old.", "How old is Anna?", new[] { "Eight", "Nine", "Ten", "Eleven" }, 2),
        LQ("ls-a1-2", CefrLevel.A1, "The cat is under the table.", "Where is the cat?", new[] { "On the table", "Under the table", "Behind the door", "In the box" }, 1),
        LQ("ls-a1-3", CefrLevel.A1, "I get up at seven o'clock.", "What time does the speaker get up?", new[] { "At 6:00", "At 7:00", "At 8:00", "At 9:00" }, 1),
        LQ("ls-a1-4", CefrLevel.A1, "It is a sunny day today.", "What is the weather like?", new[] { "Rainy", "Sunny", "Snowy", "Windy" }, 1),

        LQ("ls-a2-4", CefrLevel.A2, "The bus leaves at half past nine.", "When does the bus leave?", new[] { "9:00", "9:15", "9:30", "9:45" }, 2),
        LQ("ls-a2-5", CefrLevel.A2, "Can I have the bill, please?", "Where is the speaker probably?", new[] { "In a library", "In a restaurant", "At a station", "At school" }, 1),
        LQ("ls-a2-6", CefrLevel.A2, "My brother is taller than me, but I am older.", "Who is older?", new[] { "The brother", "The speaker", "They are the same age", "Not stated" }, 1),

        LQ("ls-b1-4", CefrLevel.B1, "I was going to call you, but my phone ran out of battery.", "Why didn't the speaker call?", new[] { "They forgot", "The phone battery died", "They were busy", "They lost the number" }, 1),
        LQ("ls-b1-5", CefrLevel.B1, "We could either go to the beach or visit the museum. I don't mind.", "How does the speaker feel about the choice?", new[] { "Prefers the beach", "Prefers the museum", "Has no preference", "Wants to stay home" }, 2),
        LQ("ls-b1-6", CefrLevel.B1, "The shop closes earlier on Sundays, so we should hurry.", "Why should they hurry?", new[] { "It's raining", "The shop closes early", "The bus is coming", "It's getting dark" }, 1),

        LQ("ls-b2-4", CefrLevel.B2, "I would have joined you, had I known about the trip earlier.", "Did the speaker go on the trip?", new[] { "Yes", "No", "Only part of it", "Not stated" }, 1),
        LQ("ls-b2-5", CefrLevel.B2, "The results were promising, albeit based on a small sample.", "What reservation does the speaker have?", new[] { "The results were poor", "The sample was small", "The test was unfair", "The cost was high" }, 1),
        LQ("ls-b2-6", CefrLevel.B2, "Rather than blaming others, we should focus on solutions.", "What does the speaker suggest?", new[] { "Blaming others", "Finding solutions", "Quitting", "Waiting" }, 1),

        LQ("ls-c1-3", CefrLevel.C1, "The board, by and large, endorsed the proposal, with only minor reservations.", "What was the board's overall stance?", new[] { "Mostly supportive", "Strongly opposed", "Completely neutral", "Evenly split" }, 0),
        LQ("ls-c1-4", CefrLevel.C1, "Far be it from me to criticise, but the timing could have been better.", "What is the speaker doing?", new[] { "Praising warmly", "Offering a gentle criticism", "Refusing to comment", "Changing the subject" }, 1),
        LQ("ls-c1-5", CefrLevel.C1, "Were it not for the funding, the project would have stalled long ago.", "What kept the project going?", new[] { "The funding", "The staff", "The deadline", "The weather" }, 0),
        LQ("ls-c1-6", CefrLevel.C1, "The minister stopped short of admitting any wrongdoing.", "Did the minister admit wrongdoing?", new[] { "Yes, completely", "No, not quite", "Only in writing", "Yes, immediately" }, 1),

        LQ("ls-c2-1", CefrLevel.C2, "His argument, though superficially compelling, does not bear close scrutiny.", "What does the speaker think of the argument?", new[] { "It is flawless", "It seems good but is weak", "It is deliberately false", "It is too complex" }, 1),
        LQ("ls-c2-2", CefrLevel.C2, "She was, if anything, even more determined after the setback.", "How did the setback affect her?", new[] { "It discouraged her", "It made her more determined", "It had no effect", "It confused her" }, 1),
        LQ("ls-c2-3", CefrLevel.C2, "The report pulls no punches in its assessment of the failures.", "How does the report address the failures?", new[] { "It hides them", "It describes them frankly", "It exaggerates them", "It ignores them" }, 1),
        LQ("ls-c2-4", CefrLevel.C2, "Suffice it to say, the outcome was less than satisfactory.", "What is implied about the outcome?", new[] { "It was excellent", "It was disappointing", "It was unclear", "It was surprising" }, 1),

        // =====================================================================
        // Reading (a real passage is shown; the question tests comprehension)
        // =====================================================================
        RQ("rd-a2-1", CefrLevel.A2,
            "Tom works in a big hospital in the city. He is a nurse. He starts work at eight o'clock in the morning and finishes at four in the afternoon. He likes his job because he helps people every day.",
            "What is Tom's job?", new[] { "Doctor", "Nurse", "Driver", "Teacher" }, 1),
        RQ("rd-a2-2", CefrLevel.A2,
            "Tom works in a big hospital in the city. He is a nurse. He starts work at eight o'clock in the morning and finishes at four in the afternoon. He likes his job because he helps people every day.",
            "When does Tom finish work?", new[] { "At 8:00", "At 12:00", "At 4:00", "At 6:00" }, 2),
        RQ("rd-a2-3", CefrLevel.A2,
            "The new shop in our street sells fruit and vegetables. It opens at nine in the morning and closes at six in the evening. On Sundays it is closed all day.",
            "When is the shop closed?", new[] { "On Mondays", "In the morning", "On Sundays", "At lunchtime" }, 2),

        RQ("rd-b1-1", CefrLevel.B1,
            "Despite the heavy rain, the village festival continued as planned. Organisers moved the music and food stalls under large tents, and most families stayed until the evening. Many people said it was the best festival in years.",
            "Did the rain stop the festival?", new[] { "Yes, it was cancelled", "No, it continued", "Only the music stopped", "It is not stated" }, 1),
        RQ("rd-b1-2", CefrLevel.B1,
            "Despite the heavy rain, the village festival continued as planned. Organisers moved the music and food stalls under large tents, and most families stayed until the evening. Many people said it was the best festival in years.",
            "What did the organisers do because of the weather?", new[] { "They sold umbrellas", "They moved the stalls under tents", "They ended early", "They changed the date" }, 1),
        RQ("rd-b1-3", CefrLevel.B1,
            "Lena had been working since early morning and was completely exhausted. Although she wanted to finish her report, she decided to go to bed early and complete it the next day.",
            "What did Lena decide to do?", new[] { "Finish the report that night", "Go to bed early", "Start a new report", "Ask for help" }, 1),

        RQ("rd-b2-1", CefrLevel.B2,
            "The city council's new transport policy aims to reduce, but not entirely eliminate, car use in the centre. Instead of an outright ban, it raises parking fees and invests the money in cheaper buses, hoping drivers will choose public transport voluntarily.",
            "What is the main goal of the policy?", new[] { "To ban all cars", "To lower car use, not remove it", "To increase car use", "To close the city centre" }, 1),
        RQ("rd-b2-2", CefrLevel.B2,
            "The city council's new transport policy aims to reduce, but not entirely eliminate, car use in the centre. Instead of an outright ban, it raises parking fees and invests the money in cheaper buses, hoping drivers will choose public transport voluntarily.",
            "How does the policy try to change behaviour?", new[] { "By making driving more expensive and buses cheaper", "By giving free cars", "By banning buses", "By closing car parks permanently" }, 0),
        RQ("rd-b2-3", CefrLevel.B2,
            "While critics praised the phone's elegant design, ordinary users soon found it impractical. The glass back cracked easily, and the battery rarely lasted a full day, so sales fell far below the company's expectations.",
            "How did everyday users react to the phone?", new[] { "They praised the design", "They found it impractical", "They ignored the critics", "They redesigned it" }, 1),

        RQ("rd-c1-1", CefrLevel.C1,
            "Her response to the proposal was anything but conciliatory. Far from seeking common ground, she dismissed each argument in turn, leaving the negotiators with little hope of a swift agreement.",
            "How would you describe her tone?", new[] { "Friendly", "Hostile", "Indifferent", "Apologetic" }, 1),
        RQ("rd-c1-2", CefrLevel.C1,
            "The findings, though admittedly preliminary, cast considerable doubt on the prevailing theory. The authors are careful to stress that further study is needed before any firm conclusions can be drawn.",
            "What do the findings do to the prevailing theory?", new[] { "Confirm it", "Question it", "Ignore it", "Prove it completely" }, 1),

        RQ("rd-c2-1", CefrLevel.C2,
            "The minister's apology, delivered in a flat monotone and over within seconds, was widely seen as perfunctory. Commentators noted that it conceded no real fault and seemed designed only to placate the press.",
            "How was the apology generally perceived?", new[] { "As heartfelt", "As superficial", "As detailed", "As courageous" }, 1),
        RQ("rd-c2-2", CefrLevel.C2,
            "For all its rhetorical flourish, the essay ultimately says little of substance. Beneath the elegant phrasing lies a thin and largely unsupported argument that crumbles under close scrutiny.",
            "What is the writer's view of the essay?", new[] { "Stylish but lacking substance", "Rigorous and convincing", "Dull but accurate", "Short but profound" }, 0),

        // --- Additional Reading items (bank expansion; now spans A1..C2) ---
        RQ("rd-a1-1", CefrLevel.A1,
            "My name is Sara. I have a dog. Its name is Max. Max is black and white. We walk in the park every morning.",
            "What colour is Max?", new[] { "Brown", "Black and white", "Grey", "Yellow" }, 1),
        RQ("rd-a1-2", CefrLevel.A1,
            "My name is Sara. I have a dog. Its name is Max. Max is black and white. We walk in the park every morning.",
            "When do they walk in the park?", new[] { "Every evening", "Every night", "Every morning", "At lunch" }, 2),
        RQ("rd-a1-3", CefrLevel.A1,
            "Ben likes fruit. He eats an apple every day. He does not like bananas.",
            "What does Ben eat every day?", new[] { "A banana", "An apple", "An orange", "Bread" }, 1),
        RQ("rd-a1-4", CefrLevel.A1,
            "Ben likes fruit. He eats an apple every day. He does not like bananas.",
            "What does Ben NOT like?", new[] { "Apples", "Bananas", "Fruit", "Water" }, 1),

        RQ("rd-a2-4", CefrLevel.A2,
            "Maria is from Spain, but she lives in London now. She works in a small cafe near her flat. She walks to work because it is close.",
            "Why does Maria walk to work?", new[] { "She has no car", "The cafe is close", "She likes exercise", "The bus is late" }, 1),
        RQ("rd-a2-5", CefrLevel.A2,
            "Maria is from Spain, but she lives in London now. She works in a small cafe near her flat. She walks to work because it is close.",
            "Where is Maria from?", new[] { "London", "Spain", "France", "Italy" }, 1),
        RQ("rd-a2-6", CefrLevel.A2,
            "The library is open from Monday to Friday. On Saturday it opens only in the morning. It is closed on Sunday.",
            "When is the library open on Saturday?", new[] { "All day", "Only the morning", "Only the afternoon", "It is closed" }, 1),

        RQ("rd-b1-4", CefrLevel.B1,
            "When Daniel moved to a new city, he knew no one. To make friends, he joined a running club. Within a month, he had met dozens of people and looked forward to every session.",
            "How did Daniel make friends?", new[] { "Through work", "By joining a club", "Through neighbours", "Online" }, 1),
        RQ("rd-b1-5", CefrLevel.B1,
            "When Daniel moved to a new city, he knew no one. To make friends, he joined a running club. Within a month, he had met dozens of people and looked forward to every session.",
            "How did Daniel feel about the sessions later?", new[] { "Bored", "Nervous", "Eager", "Tired" }, 2),
        RQ("rd-b1-6", CefrLevel.B1,
            "The recipe looked simple, but it took far longer than expected. Although the cake finally turned out well, Nadia decided she would buy one next time.",
            "What will Nadia probably do next time?", new[] { "Bake again", "Buy a cake", "Use a new recipe", "Ask for help" }, 1),

        RQ("rd-b2-4", CefrLevel.B2,
            "The museum's decision to charge for entry proved controversial. Supporters argued the fees were essential for maintenance, while opponents feared they would deter the very visitors the museum hoped to attract.",
            "Why did opponents dislike the fees?", new[] { "They were too low", "They might keep visitors away", "They funded maintenance", "They were temporary" }, 1),
        RQ("rd-b2-5", CefrLevel.B2,
            "The museum's decision to charge for entry proved controversial. Supporters argued the fees were essential for maintenance, while opponents feared they would deter the very visitors the museum hoped to attract.",
            "What did supporters say the fees were for?", new[] { "Profit", "Maintenance", "Advertising", "Salaries" }, 1),
        RQ("rd-b2-6", CefrLevel.B2,
            "Although the company reported record revenue, its share price fell. Investors were unsettled by the rising costs that lay behind the headline figures.",
            "Why did the share price fall despite record revenue?", new[] { "Falling sales", "Rising costs worried investors", "A scandal", "New competition" }, 1),

        RQ("rd-c1-3", CefrLevel.C1,
            "The author's nostalgia for a simpler past is palpable, yet she resists romanticising it entirely, acknowledging the hardships that accompanied those quieter times.",
            "What is the author's attitude to the past?", new[] { "Purely nostalgic", "Nostalgic but balanced", "Wholly critical", "Indifferent" }, 1),
        RQ("rd-c1-4", CefrLevel.C1,
            "The author's nostalgia for a simpler past is palpable, yet she resists romanticising it entirely, acknowledging the hardships that accompanied those quieter times.",
            "What does the author acknowledge about the past?", new[] { "Its hardships", "Its wealth", "Its excitement", "Its fame" }, 0),
        RQ("rd-c1-5", CefrLevel.C1,
            "Far from settling the debate, the study has reignited it. Each side has seized on different findings, interpreting the same data to support opposing conclusions.",
            "What effect did the study have on the debate?", new[] { "Ended it", "Reignited it", "Ignored it", "Postponed it" }, 1),
        RQ("rd-c1-6", CefrLevel.C1,
            "Far from settling the debate, the study has reignited it. Each side has seized on different findings, interpreting the same data to support opposing conclusions.",
            "How have the two sides used the data?", new[] { "Both rejected it", "To support opposing conclusions", "They agreed on it", "They ignored it" }, 1),

        RQ("rd-c2-3", CefrLevel.C2,
            "The novel's much-vaunted originality is, on closer inspection, largely borrowed. Its central conceit echoes a dozen earlier works, though few critics seem willing to say so.",
            "What is the reviewer's view of the novel's originality?", new[] { "Truly original", "Overstated", "Universally denied", "Irrelevant" }, 1),
        RQ("rd-c2-4", CefrLevel.C2,
            "The novel's much-vaunted originality is, on closer inspection, largely borrowed. Its central conceit echoes a dozen earlier works, though few critics seem willing to say so.",
            "What does the reviewer suggest about most critics?", new[] { "They are unwilling to admit its borrowing", "They praise its prose", "They wrote the novel", "They ignore it" }, 0),
        RQ("rd-c2-5", CefrLevel.C2,
            "While the policy is couched in the language of fairness, its practical effect is to entrench the very inequalities it claims to address.",
            "What is the real effect of the policy, according to the writer?", new[] { "It reduces inequality", "It reinforces inequality", "It has no effect", "It is fair" }, 1),
        RQ("rd-c2-6", CefrLevel.C2,
            "While the policy is couched in the language of fairness, its practical effect is to entrench the very inequalities it claims to address.",
            "How is the policy presented?", new[] { "As harsh", "As fair", "As temporary", "As costly" }, 1),
    };
}
