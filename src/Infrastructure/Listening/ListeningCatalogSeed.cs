using Domain.Assessment;
using Domain.Listening;

namespace Infrastructure.Listening;

/// <summary>
/// Seed of the curated listening catalog (PROJECT-SPEC Faza 4 - the Listening module), spread
/// across the full CEFR ladder A1→C2 with at least two exercises per level so the level-tolerant
/// catalog always offers real choice and the learner can practise every level, the exercises
/// getting progressively harder up the ladder. Each exercise carries an authored English transcript (synthesized
/// to audio by Azure Neural TTS and cached per exercise - docs/development-guide.md rule 10) and comprehension
/// questions. English text is content data; all quiz hints are vetted Uzbek content
/// (docs/development-guide.md rule 11). A content team verifies/extends these; here they make the module
/// exercisable end to end.
/// </summary>
public static class ListeningCatalogSeed
{
    /// <summary>The reference instant used to stamp seeded exercises.</summary>
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<ListeningExercise> Exercises() => new List<ListeningExercise>
    {
        // ─────────────────────────────── A1 ───────────────────────────────

        Exercise(
            "Introducing Yourself", "everyday", CefrLevel.A1,
            "Hello! My name is Laylo. I am twelve years old. I live in Tashkent with my family. " +
            "I like music and football. Nice to meet you!",
            new[]
            {
                Q("How old is Laylo?",
                    new[] { "Ten", "Twelve", "Fifteen" }, 1, "lhint.intro_age"),
                Q("What does Laylo like?",
                    new[] { "Music and football", "Books and tea", "Films and games" }, 0, "lhint.intro_likes"),
            }),

        Exercise(
            "At the Café", "everyday", CefrLevel.A1,
            "Good morning. I would like a cup of tea and a piece of cake, please. How much is it? " +
            "Thank you very much. Have a nice day!",
            new[]
            {
                Q("What does the person order?",
                    new[] { "Coffee and bread", "Tea and cake", "Juice and soup" }, 1, "lhint.cafe_order"),
                Q("When does this happen?",
                    new[] { "In the morning", "At night", "In the evening" }, 0, "lhint.cafe_time"),
            }),

        // ─────────────────────────────── A2 ───────────────────────────────

        Exercise(
            "My Weekend", "everyday", CefrLevel.A2,
            "Last weekend was very nice. On Saturday I visited my grandparents in the village. " +
            "On Sunday I stayed at home and watched a film with my brother. The weekend was short " +
            "but relaxing.",
            new[]
            {
                Q("Where did the speaker go on Saturday?",
                    new[] { "To the village", "To the city", "To school" }, 0, "lhint.weekend_sat"),
                Q("What did the speaker do on Sunday?",
                    new[] { "Watched a film at home", "Played football", "Went shopping" }, 0, "lhint.weekend_sun"),
            }),

        Exercise(
            "Directions to the Library", "everyday", CefrLevel.A2,
            "Excuse me, how can I get to the library? Go straight ahead, then turn left at the " +
            "bank. The library is next to the post office. It takes about five minutes on foot.",
            new[]
            {
                Q("Where do you turn left?",
                    new[] { "At the bank", "At the school", "At the park" }, 0, "lhint.dir_turn"),
                Q("How long does it take to get there?",
                    new[] { "About five minutes", "About twenty minutes", "About one hour" }, 0, "lhint.dir_time"),
            }),

        // ─────────────────────────────── B1 ───────────────────────────────

        Exercise(
            "A Job Interview", "work", CefrLevel.B1,
            "Thank you for coming today. Can you tell me about your experience? Well, I worked as " +
            "a sales assistant for two years, and I really enjoy helping customers. I am also good " +
            "at working in a team. That sounds great. We will contact you next week.",
            new[]
            {
                Q("What was the candidate's previous job?",
                    new[] { "A sales assistant", "A teacher", "A driver" }, 0, "lhint.interview_job"),
                Q("When will the company contact the candidate?",
                    new[] { "Next week", "Tomorrow", "Next month" }, 0, "lhint.interview_contact"),
            }),

        Exercise(
            "Planning a Trip", "travel", CefrLevel.B1,
            "I'm thinking of visiting Samarkand next month. I've heard the architecture is amazing. " +
            "I'm going to take the fast train because it's more comfortable than the bus. I'll stay " +
            "for three days and visit the famous Registan Square.",
            new[]
            {
                Q("How will the speaker travel to Samarkand?",
                    new[] { "By fast train", "By bus", "By plane" }, 0, "lhint.trip_transport"),
                Q("How long will the speaker stay?",
                    new[] { "Three days", "One week", "One day" }, 0, "lhint.trip_days"),
            }),

        // ─────────────────────────────── B2 ───────────────────────────────

        Exercise(
            "A Weather Report", "news", CefrLevel.B2,
            "Good evening. Here is tomorrow's weather. The morning will be cloudy with a chance of " +
            "light rain in the north. By the afternoon, the clouds will clear and temperatures will " +
            "rise to around twenty-five degrees. It will be a pleasant evening, perfect for a walk " +
            "outside.",
            new[]
            {
                Q("What will the morning weather be like?",
                    new[] { "Cloudy with light rain", "Sunny and hot", "Snowy and cold" }, 0, "lhint.weather_morning"),
                Q("What temperature is expected in the afternoon?",
                    new[] { "Around twenty-five degrees", "Around ten degrees", "Around forty degrees" }, 0, "lhint.weather_temp"),
            }),

        Exercise(
            "A Podcast on Healthy Eating", "health", CefrLevel.B2,
            "Many people think that eating healthily is expensive, but that isn't always true. " +
            "Simple foods like beans, rice and seasonal vegetables are cheap and full of nutrients. " +
            "The key is to cook at home more often, because restaurant meals usually contain more " +
            "salt and sugar than we realise.",
            new[]
            {
                Q("What does the speaker say about healthy eating?",
                    new[] { "It isn't always expensive", "It is always expensive", "It is impossible" }, 0, "lhint.eating_cost"),
                Q("Why is cooking at home better?",
                    new[] { "Restaurant meals have more salt and sugar", "It is always slower", "It is more expensive" }, 0, "lhint.eating_home"),
            }),

        // ─────────────────────────────── C1 ───────────────────────────────

        Exercise(
            "A Lecture on Memory", "education", CefrLevel.C1,
            "Contrary to popular belief, memory is not like a video recording that captures events " +
            "exactly as they happened. Instead, each time we recall a memory, we subtly reconstruct " +
            "it, and small details can change. This is why two people can witness the same event yet " +
            "remember it quite differently.",
            new[]
            {
                Q("What is the lecturer's main point about memory?",
                    new[]
                    {
                        "It reconstructs events rather than recording them exactly",
                        "It records events perfectly",
                        "It never changes over time",
                    }, 0, "lhint.memory_main"),
                Q("Why might two witnesses remember an event differently?",
                    new[]
                    {
                        "Memory is reconstructed each time it is recalled",
                        "They were not really there",
                        "Memory is a perfect recording",
                    }, 0, "lhint.memory_witness"),
            }),

        Exercise(
            "A Discussion on Remote Work", "society", CefrLevel.C1,
            "While remote work offers undeniable flexibility, it also raises subtle challenges that " +
            "companies are only beginning to address. Without the informal conversations of an " +
            "office, new employees can struggle to feel part of a team, and managers must work " +
            "harder to maintain a sense of shared purpose across a scattered workforce.",
            new[]
            {
                Q("What challenge of remote work is mentioned?",
                    new[]
                    {
                        "New employees can struggle to feel part of a team",
                        "It is always cheaper for everyone",
                        "It has no benefits at all",
                    }, 0, "lhint.remotework_challenge"),
                Q("What must managers do, according to the speaker?",
                    new[]
                    {
                        "Work harder to maintain a sense of shared purpose",
                        "Ignore the team completely",
                        "Reduce everyone's salary",
                    }, 0, "lhint.remotework_managers"),
            }),

        // ─────────────────────────────── C2 ───────────────────────────────

        Exercise(
            "The Value of Boredom", "psychology", CefrLevel.C2,
            "We tend to regard boredom as something to be avoided at all costs, yet a growing body " +
            "of research suggests it may be quietly essential. When the mind is left unoccupied, it " +
            "begins to wander, and it is precisely in these idle moments that our most original ideas " +
            "often surface. By reaching for our phones the instant we feel restless, we may be " +
            "depriving ourselves of the very conditions in which creativity thrives.",
            new[]
            {
                Q("What is the speaker's main argument about boredom?",
                    new[]
                    {
                        "It may be essential for creativity",
                        "It should always be avoided",
                        "It harms original thinking",
                    }, 0, "lhint.boredom_main"),
                Q("What does the speaker suggest about reaching for our phones?",
                    new[]
                    {
                        "It may deprive us of the conditions in which creativity thrives",
                        "It makes us more creative",
                        "It has no effect on the mind",
                    }, 0, "lhint.boredom_phones"),
            }),

        Exercise(
            "A Debate on Artificial Intelligence", "society", CefrLevel.C2,
            "Proponents of artificial intelligence often promise a future of unprecedented " +
            "efficiency, but critics caution that we risk delegating judgement to systems we do not " +
            "fully understand. The real danger, they argue, is not that machines will become " +
            "malicious, but that we will come to rely on them so heavily that we gradually lose the " +
            "capacity to question their conclusions.",
            new[]
            {
                Q("According to the critics, what is the real danger of artificial intelligence?",
                    new[]
                    {
                        "We may lose the capacity to question its conclusions",
                        "Machines will certainly become malicious",
                        "It will make no difference to society",
                    }, 0, "lhint.ai_danger"),
                Q("What do the proponents of artificial intelligence emphasise?",
                    new[]
                    {
                        "A future of unprecedented efficiency",
                        "The certainty of machine malice",
                        "The need to abandon technology",
                    }, 0, "lhint.ai_proponents"),
            }),
    };

    private static ListeningExercise Exercise(
        string title, string topic, CefrLevel level, string transcript, ListeningQuestion[] questions) =>
        ListeningExercise.Curate(title, transcript, topic, level, questions, SeededAt);

    private static ListeningQuestion Q(string prompt, string[] options, int correct, string hint) =>
        ListeningQuestion.Create(prompt, options, correct, hint);
}
