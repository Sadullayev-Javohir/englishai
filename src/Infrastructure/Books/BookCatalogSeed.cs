using System.Security.Cryptography;
using System.Text;
using Domain.Assessment;
using Domain.Books;

namespace Infrastructure.Books;

/// <summary>
/// Seed of the curated Books library (Home → Books). Every level (A1→C2) has at least ten original
/// graded readers; each book is metadata only here - title, vetted Uzbek subtitle, author,
/// synopsis, cover-image keyword and an ordered section outline. The section text and its ten
/// comprehension questions are generated lazily by the LLM on first open and cached (docs/development-guide.md
/// rules 8, 10). Titles/stories are original works for this app (not copyrighted texts). Book ids
/// are deterministic (derived from a stable slug) so reseeding is idempotent and progress survives.
/// </summary>
public static class BookCatalogSeed
{
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string Author = "EnglishAI Graded Readers";

    public static IReadOnlyList<Book> Books() => A1().Concat(A2()).Concat(B1())
        .Concat(B2()).Concat(C1()).Concat(C2()).ToList();

    // ── A1 - beginners (short, present-tense everyday stories) ──────────────────────────────────
    private static IEnumerable<Book> A1() => new[]
    {
        B("a1-my-first-day", "My First Day", "Birinchi kunim", "everyday school",
            "A shy new student starts at a new school, meets a kind classmate and slowly feels at home.",
            CefrLevel.A1, "school classroom", "Arriving at School", "A New Friend", "Going Home Happy"),
        B("a1-the-lost-cat", "The Lost Cat", "Yo'qolgan mushuk", "pets family",
            "A small family looks all around their neighbourhood for their lost cat and finds it in a surprising place.",
            CefrLevel.A1, "cat pet", "The Cat Is Gone", "Looking Everywhere", "Found at Last"),
        B("a1-a-day-at-the-market", "A Day at the Market", "Bozordagi kun", "food shopping",
            "A girl goes to the market with her grandmother to buy fruit and vegetables for dinner.",
            CefrLevel.A1, "fruit market", "At the Market", "Buying Food", "Dinner Time"),
        B("a1-my-busy-morning", "My Busy Morning", "Bandlik ertalabim", "everyday routine",
            "A boy wakes up late and rushes through his morning routine to get to school on time.",
            CefrLevel.A1, "morning sunrise", "Waking Up Late", "Getting Ready", "Just in Time"),
        B("a1-the-big-rain", "The Big Rain", "Katta yomg'ir", "weather nature",
            "When heavy rain falls, two friends find fun things to do at home all afternoon.",
            CefrLevel.A1, "rain window", "It Starts to Rain", "Fun Inside", "Sunshine Again"),
        B("a1-my-family", "My Family", "Mening oilam", "family people",
            "A child describes each person in their family and the things they like to do together.",
            CefrLevel.A1, "happy family", "Mum and Dad", "My Brother and Sister", "We Are Together"),
        B("a1-the-red-bicycle", "The Red Bicycle", "Qizil velosiped", "everyday hobbies",
            "A boy learns to ride his new red bicycle in the park with help from his father.",
            CefrLevel.A1, "red bicycle", "A New Bicycle", "Learning to Ride", "Riding Alone"),
        B("a1-at-the-zoo", "At the Zoo", "Hayvonot bog'ida", "animals nature",
            "A class visits the zoo and sees many animals, from tall giraffes to small monkeys.",
            CefrLevel.A1, "zoo animals", "Going to the Zoo", "So Many Animals", "Time to Leave"),
        B("a1-the-birthday-cake", "The Birthday Cake", "Tug'ilgan kun torti", "food celebration",
            "A girl helps her mother make a birthday cake and surprises her best friend.",
            CefrLevel.A1, "birthday cake", "A Secret Plan", "Making the Cake", "Happy Birthday"),
        B("a1-my-school-bag", "My School Bag", "Maktab sumkam", "school everyday",
            "A boy cannot find his homework in his messy school bag and learns to keep things tidy.",
            CefrLevel.A1, "school bag", "Where Is It?", "A Big Mess", "Neat and Ready"),
    };

    // ── A2 - elementary (past tense, simple narratives) ─────────────────────────────────────────
    private static IEnumerable<Book> A2() => new[]
    {
        B("a2-the-train-journey", "The Train Journey", "Poyezd sayohati", "travel",
            "Two friends take their first long train journey to the seaside and meet interesting people.",
            CefrLevel.A2, "train travel", "Buying Tickets", "On the Train", "By the Sea"),
        B("a2-the-new-neighbour", "The New Neighbour", "Yangi qo'shni", "everyday community",
            "When a new family moves in next door, a curious boy slowly makes a surprising friendship.",
            CefrLevel.A2, "neighbour house", "Someone New", "Saying Hello", "A New Friend"),
        B("a2-a-walk-in-the-mountains", "A Walk in the Mountains", "Tog'dagi sayr", "nature travel",
            "A family hikes up a mountain, gets caught in fog and learns to stay calm and work together.",
            CefrLevel.A2, "mountain hiking", "Setting Out", "Lost in the Fog", "Finding the Way"),
        B("a2-the-cooking-contest", "The Cooking Contest", "Pazandalik musobaqasi", "food",
            "A nervous teenager enters a local cooking contest and discovers what really matters.",
            CefrLevel.A2, "cooking kitchen", "Signing Up", "The Big Day", "More Than a Prize"),
        B("a2-the-old-photograph", "The Old Photograph", "Eski surat", "family memory",
            "A girl finds an old photograph in the attic and asks her grandfather to tell its story.",
            CefrLevel.A2, "old photograph", "In the Attic", "Grandfather's Story", "A Family Secret"),
        B("a2-the-football-match", "The Football Match", "Futbol o'yini", "sport",
            "A small team practises hard for the most important football match of the season.",
            CefrLevel.A2, "football match", "The Team", "Hard Practice", "The Final Whistle"),
        B("a2-a-letter-from-far-away", "A Letter from Far Away", "Uzoqdan kelgan xat", "everyday",
            "A boy receives a letter from a cousin abroad and starts a friendship across the world.",
            CefrLevel.A2, "letter mail", "The Letter Arrives", "Writing Back", "A Far-Away Friend"),
        B("a2-the-market-day", "The Market Day", "Bozor kuni", "food shopping",
            "A young seller has one busy day at the market that changes how she sees her town.",
            CefrLevel.A2, "street market", "Opening the Stall", "A Busy Day", "Closing Time"),
        B("a2-the-rainy-holiday", "The Rainy Holiday", "Yomg'irli ta'til", "travel weather",
            "A holiday by the sea is ruined by rain, but the family finds new ways to enjoy it.",
            CefrLevel.A2, "rainy beach", "A Spoiled Plan", "Indoor Fun", "Sunny at Last"),
        B("a2-the-helpful-robot", "The Helpful Robot", "Yordamchi robot", "technology",
            "A clever girl builds a small robot for the science fair and learns it is not perfect.",
            CefrLevel.A2, "robot technology", "Building the Robot", "The Science Fair", "A Small Mistake"),
    };

    // ── B1 - intermediate (richer plots, opinions, feelings) ────────────────────────────────────
    private static IEnumerable<Book> B1() => new[]
    {
        B("b1-the-island-secret", "The Island Secret", "Orol siri", "adventure mystery",
            "Three friends explore a small island and uncover a secret that locals have kept for years.",
            CefrLevel.B1, "island sea", "The Boat Trip", "The Hidden Cave", "The Island Secret", "A Promise Kept"),
        B("b1-the-night-train", "The Night Train", "Tungi poyezd", "mystery travel",
            "A young journalist on a night train notices a passenger who is not who they claim to be.",
            CefrLevel.B1, "night train", "Boarding at Midnight", "A Strange Passenger", "The Truth", "Morning Light"),
        B("b1-the-river-and-the-village", "The River and the Village", "Daryo va qishloq", "nature community",
            "When a river changes course, a village must decide whether to fight nature or adapt to it.",
            CefrLevel.B1, "river village", "The Changing River", "A Hard Choice", "Working Together", "A New Beginning"),
        B("b1-following-a-dream", "Following a Dream", "Orzu ortidan", "everyday ambition",
            "A talented young musician must choose between a safe job and the risk of following a dream.",
            CefrLevel.B1, "musician guitar", "A Safe Plan", "The Big Audition", "Doubts", "The Choice"),
        B("b1-the-bookshop-on-the-corner", "The Bookshop on the Corner", "Burchakdagi kitob do'koni", "culture community",
            "An old bookshop is about to close until the neighbourhood comes together to save it.",
            CefrLevel.B1, "bookshop books", "The Closing Sign", "An Idea", "The Whole Street", "Open Again"),
        B("b1-the-science-project", "The Science Project", "Ilmiy loyiha", "science school",
            "Two rivals are forced to work together on a science project and discover an unexpected idea.",
            CefrLevel.B1, "science laboratory", "Forced Together", "First Failures", "A Breakthrough", "Sharing the Prize"),
        B("b1-the-mountain-rescue", "The Mountain Rescue", "Tog'dagi qutqaruv", "adventure",
            "When a hiker goes missing in the mountains, a small rescue team races against the weather.",
            CefrLevel.B1, "mountain rescue", "The Missing Hiker", "Into the Storm", "The Search", "Safe at Home"),
        B("b1-a-taste-of-home", "A Taste of Home", "Vatan ta'mi", "food culture",
            "A young cook far from home opens a small restaurant to share the food of their childhood.",
            CefrLevel.B1, "restaurant food", "Far From Home", "Opening Night", "Hard Lessons", "A Taste of Home"),
        B("b1-the-lost-letter", "The Lost Letter", "Yo'qolgan xat", "mystery history",
            "A letter lost for fifty years arrives at last and reunites two old friends.",
            CefrLevel.B1, "old letter", "A Letter Delayed", "Searching for the Name", "The Meeting", "Time Regained"),
        B("b1-the-green-city", "The Green City", "Yashil shahar", "environment",
            "Students start a project to make their grey city greener and face doubt before success.",
            CefrLevel.B1, "green city park", "A Grey City", "The First Garden", "Spreading the Idea", "A Greener Place"),
    };

    // ── B2 - upper-intermediate (themes, conflict, nuance) ──────────────────────────────────────
    private static IEnumerable<Book> B2() => new[]
    {
        B("b2-the-weight-of-a-promise", "The Weight of a Promise", "Va'da og'irligi", "drama",
            "A young doctor must choose between a promise to family and the demands of a distant career.",
            CefrLevel.B2, "doctor hospital", "The Promise", "A Distant Offer", "Divided Loyalty", "The Decision"),
        B("b2-the-clockmakers-apprentice", "The Clockmaker's Apprentice", "Soatsoz shogirdi", "history craft",
            "An apprentice learns that mastering a craft means more than skill - it means patience and trust.",
            CefrLevel.B2, "clock gears", "The Workshop", "A Costly Mistake", "Learning Patience", "The Master's Trust"),
        B("b2-against-the-current", "Against the Current", "Oqimga qarshi", "sport drama",
            "A swimmer recovering from injury fights doubt and pressure to return to competition.",
            CefrLevel.B2, "swimmer pool", "After the Injury", "Slow Progress", "The Doubt", "The Comeback"),
        B("b2-the-city-that-never-listens", "The City That Never Listens", "Eshitmaydigan shahar", "society",
            "A young activist tries to make a busy, indifferent city pay attention to those it ignores.",
            CefrLevel.B2, "city crowd", "Unheard Voices", "The First Campaign", "Setbacks", "Being Heard"),
        B("b2-the-inheritance", "The Inheritance", "Meros", "family drama",
            "Siblings inherit an old family house and must confront the past hidden within its walls.",
            CefrLevel.B2, "old house", "The Old House", "Buried Memories", "An Argument", "Letting Go"),
        B("b2-the-quiet-experiment", "The Quiet Experiment", "Sokin tajriba", "science ethics",
            "A researcher discovers a result that could help many - but only by bending the rules.",
            CefrLevel.B2, "laboratory research", "An Unexpected Result", "Temptation", "The Line", "Doing Right"),
        B("b2-letters-to-a-stranger", "Letters to a Stranger", "Notanishga xatlar", "drama",
            "Two strangers exchange anonymous letters and slowly change each other's lives.",
            CefrLevel.B2, "letters writing", "The First Letter", "Honest Words", "A Risk", "Face to Face"),
        B("b2-the-last-harvest", "The Last Harvest", "Oxirgi hosil", "environment rural",
            "A farming family faces a failing harvest and a hard choice about leaving the land they love.",
            CefrLevel.B2, "farm field", "A Dry Season", "Hard Numbers", "The Family Meeting", "A New Path"),
        B("b2-the-art-forgery", "The Art Forgery", "Soxta asar", "mystery art",
            "A gallery assistant suspects a famous painting is fake and must decide whether to speak.",
            CefrLevel.B2, "art gallery", "The New Exhibit", "A Small Doubt", "Quiet Investigation", "The Truth Told"),
        B("b2-crossing-borders", "Crossing Borders", "Chegaralarni kesib", "society travel",
            "A young translator helps newcomers in a foreign city and questions where home really is.",
            CefrLevel.B2, "city border", "A New Country", "Words Between Worlds", "Belonging", "Home"),
    };

    // ── C1 - advanced (abstract themes, layered narration) ──────────────────────────────────────
    private static IEnumerable<Book> C1() => new[]
    {
        B("c1-the-architecture-of-memory", "The Architecture of Memory", "Xotira me'morchiligi", "psychology",
            "A neuroscientist studying memory is forced to question the reliability of her own past.",
            CefrLevel.C1, "abstract memory", "The Study", "A Crack in the Story", "Unreliable Witness", "Rebuilding"),
        B("c1-the-cartographers-error", "The Cartographer's Error", "Xaritachining xatosi", "history",
            "A single mistake on an old map sends a historian on a journey that rewrites a local legend.",
            CefrLevel.C1, "old map", "The Faulty Map", "Following the Error", "A Buried History", "The Correction"),
        B("c1-the-ethics-of-silence", "The Ethics of Silence", "Sukunat axloqi", "ethics society",
            "A whistle-blower weighs loyalty, truth and consequence when staying silent feels safest.",
            CefrLevel.C1, "office shadow", "The Discovery", "Weighing the Cost", "Speaking Out", "Aftermath"),
        B("c1-the-language-of-rivers", "The Language of Rivers", "Daryolar tili", "nature philosophy",
            "An engineer and an elder clash over a river, and over two ways of understanding the world.",
            CefrLevel.C1, "river landscape", "Two Visions", "The Negotiation", "Common Ground", "Flowing On"),
        B("c1-the-collector", "The Collector", "Kolleksioner", "psychology drama",
            "A man who collects other people's stories slowly realises he has forgotten his own.",
            CefrLevel.C1, "antique collection", "A Room of Stories", "The Missing One", "Confrontation", "His Own Tale"),
        B("c1-the-paradox-of-progress", "The Paradox of Progress", "Taraqqiyot paradoksi", "society technology",
            "A town welcomes a new factory that promises prosperity but threatens its way of life.",
            CefrLevel.C1, "factory town", "The Promise", "Hidden Costs", "Divided Town", "A Measured Future"),
        B("c1-the-unfinished-portrait", "The Unfinished Portrait", "Tugallanmagan portret", "art",
            "An art restorer uncovers why a master left a famous portrait deliberately unfinished.",
            CefrLevel.C1, "portrait painting", "The Commission", "Layers Beneath", "The Painter's Reason", "Completion"),
        B("c1-the-weight-of-words", "The Weight of Words", "So'zlar og'irligi", "media ethics",
            "A respected editor must decide how much truth a fragile public is ready to be told.",
            CefrLevel.C1, "newspaper print", "The Story", "Responsibility", "Pressure", "The Edition"),
        B("c1-the-migration", "The Migration", "Ko'chish", "nature science",
            "A biologist tracking a vanishing bird confronts what it means to save what cannot be kept.",
            CefrLevel.C1, "birds migration", "The Tagging", "A Shrinking Route", "Hard Truths", "Letting Fly"),
        B("c1-the-borrowed-house", "The Borrowed House", "Qarzga olingan uy", "drama memory",
            "A writer renting an old house begins to live inside the unfinished story of its last owner.",
            CefrLevel.C1, "empty house", "Moving In", "Another's Life", "Entangled", "Leaving"),
    };

    // ── C2 - proficiency (literary, dense, demanding) ───────────────────────────────────────────
    private static IEnumerable<Book> C2() => new[]
    {
        B("c2-the-hermeneutics-of-rain", "The Hermeneutics of Rain", "Yomg'ir germenevtikasi", "philosophy",
            "A philosopher's quiet retreat becomes a meditation on meaning, weather and the limits of interpretation.",
            CefrLevel.C2, "rain abstract", "Arrival", "Reading the Sky", "The Limits of Sense", "Departure"),
        B("c2-an-economy-of-shadows", "An Economy of Shadows", "Soyalar iqtisodi", "society",
            "In a city run on favours and silence, an auditor traces a debt no ledger records.",
            CefrLevel.C2, "city night", "The Audit", "Invisible Ledgers", "Complicity", "Reckoning"),
        B("c2-the-grammar-of-grief", "The Grammar of Grief", "Qayg'u grammatikasi", "drama psychology",
            "A linguist who lost her voice to grief rebuilds language, and herself, one word at a time.",
            CefrLevel.C2, "abstract emotion", "Silence", "First Words", "Old Meanings", "A New Syntax"),
        B("c2-the-cartography-of-exile", "The Cartography of Exile", "Surgun kartografiyasi", "history society",
            "An exiled mapmaker draws the country he can never return to, and questions what a homeland is.",
            CefrLevel.C2, "old atlas", "The Commission", "Lines of Loss", "Memory and Invention", "The Final Map"),
        B("c2-the-observer-effect", "The Observer Effect", "Kuzatuvchi effekti", "science philosophy",
            "A physicist and a novelist argue whether observing a life inevitably alters it.",
            CefrLevel.C2, "physics abstract", "The Wager", "Watching", "Interference", "The Result"),
        B("c2-a-theory-of-ruins", "A Theory of Ruins", "Vayronalar nazariyasi", "history art",
            "An archaeologist defending a doomed ruin asks what we owe to the past we cannot preserve.",
            CefrLevel.C2, "ancient ruins", "The Excavation", "The Case for Decay", "Opposition", "What Remains"),
        B("c2-the-currency-of-trust", "The Currency of Trust", "Ishonch valyutasi", "society economics",
            "A negotiator brokering peace between rivals discovers trust is the one thing she cannot fake.",
            CefrLevel.C2, "handshake business", "The Table", "Small Betrayals", "The Test", "An Agreement"),
        B("c2-the-palimpsest", "The Palimpsest", "Palimpsest", "literature mystery",
            "Beneath a forgotten manuscript lies an older text that overturns a nation's founding story.",
            CefrLevel.C2, "ancient manuscript", "The Discovery", "Reading Beneath", "Dangerous Truth", "Publication"),
        B("c2-the-weather-of-the-mind", "The Weather of the Mind", "Ong ob-havosi", "psychology",
            "A therapist and a meteorologist compare the systems they study and the storms they cannot predict.",
            CefrLevel.C2, "storm sky", "Two Forecasts", "Patterns", "The Unpredictable", "Clear Skies"),
        B("c2-the-last-translation", "The Last Translation", "Oxirgi tarjima", "literature",
            "An aging translator races to finish a final, untranslatable poem before words leave her entirely.",
            CefrLevel.C2, "books library", "The Last Work", "The Untranslatable", "Loss and Gain", "The Final Line"),
    };

    private static Book B(
        string slug, string title, string titleUz, string topic, string synopsis,
        CefrLevel level, string coverQuery, params string[] sections) =>
        Book.Curate(DeterministicId(slug), title, titleUz, Author, synopsis, topic, level, coverQuery, sections, SeededAt);

    /// <summary>Derives a stable GUID from a slug so reseeding is idempotent (same id every run).</summary>
    private static Guid DeterministicId(string slug)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes("book:" + slug));
        return new Guid(hash);
    }
}
