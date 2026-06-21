using Domain.Assessment;

namespace Infrastructure.Content;

/// <summary>
/// A compact, hand-curated baseline of English lemmas a learner is assumed to already know by a
/// given CEFR level - the "known vocabulary" half of <see cref="IComprehensibilityScorer"/>'s
/// i+1 gate (PROJECT-SPEC core principle: generated passages/transcripts should be ~80-90%
/// comprehensible, docs/development-guide.md rule 10's "don't over-build" balanced against actually applying the
/// principle). This is a proxy, not a learner-specific record: the top ~150-200 most frequent
/// English words already cover the large majority of tokens in any short, simple text (Zipf's
/// law), so a level-appropriate cumulative list is a practical, zero-dependency comprehensibility
/// baseline without needing a full learner vocabulary lookup on the content-generation path.
/// Levels are cumulative: everything known at A1 is still known at C2.
/// </summary>
public static class CoreWordLists
{
    /// <summary>
    /// The known-lemma baseline for <paramref name="level"/> - every word introduced at this level
    /// or below. Lemmas are lowercase base forms (matching <see cref="ComprehensibilityScorer"/>'s
    /// normalization).
    /// </summary>
    public static IReadOnlyCollection<string> For(CefrLevel level)
    {
        var words = new HashSet<string>(Core, StringComparer.OrdinalIgnoreCase);
        if (level >= CefrLevel.A2) words.UnionWith(A2Additions);
        if (level >= CefrLevel.B1) words.UnionWith(B1Additions);
        if (level >= CefrLevel.B2) words.UnionWith(B2Additions);
        if (level >= CefrLevel.C1) words.UnionWith(C1Additions);
        return words;
    }

    // Function words + the highest-frequency general verbs/adjectives/nouns - assumed known from
    // the very first lesson (A1 baseline).
    private static readonly string[] Core =
    {
        // Pronouns / determiners.
        "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them",
        "my", "your", "his", "its", "our", "their", "mine", "yours", "this", "that", "these", "those",
        "who", "what", "which", "whose", "whom", "a", "an", "the", "some", "any", "no", "every", "each",
        "both", "all", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        // Be / auxiliary / modal verbs.
        "be", "am", "is", "are", "was", "were", "been", "being",
        "do", "does", "did", "done", "have", "has", "had", "having",
        "will", "would", "can", "could", "shall", "should", "may", "might", "must",
        // Conjunctions / prepositions / question words.
        "and", "or", "but", "so", "because", "if", "when", "while", "than", "then", "as",
        "of", "in", "on", "at", "to", "from", "with", "without", "for", "about", "into", "onto",
        "over", "under", "between", "through", "before", "after", "up", "down", "out", "off", "not",
        "how", "where", "why", "here", "there", "now", "today", "yes",
        // Common verbs.
        "go", "come", "get", "make", "take", "give", "see", "look", "know", "think", "want", "like",
        "need", "use", "find", "tell", "ask", "work", "call", "try", "feel", "leave", "put", "let",
        "start", "stop", "help", "play", "run", "walk", "talk", "speak", "read", "write", "eat",
        "drink", "sleep", "live", "love", "buy", "sell", "pay", "open", "close", "wait", "meet",
        "learn", "teach", "study", "listen", "hear", "watch", "show", "bring", "send",
        "understand", "remember", "forget", "hope", "plan", "become", "happen", "change",
        "move", "stay", "visit", "travel", "cook", "clean", "drive", "ride", "swim", "cry", "laugh",
        "sing", "dance", "draw", "paint", "build", "grow", "sit", "stand", "wear",
        // Common adjectives.
        "good", "bad", "big", "small", "new", "old", "young", "happy", "sad", "easy", "difficult",
        "hard", "nice", "beautiful", "hot", "cold", "warm", "cool", "fast", "slow", "high", "low",
        "long", "short", "tall", "full", "empty", "rich", "poor", "strong", "weak", "clean", "dirty",
        "safe", "important", "expensive", "cheap", "free", "busy", "tired", "hungry", "thirsty",
        "kind", "funny", "quiet", "loud", "dark", "light", "early", "late", "near", "far", "right",
        "wrong", "same", "different", "other", "another", "many", "much", "few", "little", "more",
        "most", "less", "great", "own", "sure", "ready",
        // Common nouns (people, family, daily life).
        "time", "day", "week", "month", "year", "morning", "afternoon", "evening", "night",
        "tomorrow", "yesterday", "man", "woman", "boy", "girl", "child", "children", "people",
        "person", "family", "friend", "mother", "father", "sister", "brother", "home", "house",
        "room", "school", "work", "job", "city", "town", "country", "world", "place", "way",
        "thing", "food", "water", "money", "book", "word", "name", "number", "part", "group",
        "problem", "hand", "eye", "head", "life", "area", "question", "car", "door", "table",
        "chair", "window", "phone", "computer", "shop", "store", "street", "road", "bus", "train",
        "plane", "bag", "clothes", "shirt", "shoe", "hat", "color", "music", "movie", "picture",
        "letter", "story", "class", "teacher", "student", "doctor", "nurse", "police", "shop",
        "market", "park", "garden", "river", "sea", "mountain", "sky", "sun", "moon", "star",
        "rain", "snow", "wind", "tree", "flower", "animal", "dog", "cat", "bird", "fish",
    };

    // Everyday-life vocabulary a beginner learner is expected to have met by A2.
    private static readonly string[] A2Additions =
    {
        "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday",
        "january", "february", "march", "april", "may", "june", "july", "august", "september",
        "october", "november", "december", "spring", "summer", "autumn", "winter", "season",
        "weather", "restaurant", "menu", "waiter", "kitchen", "bedroom", "bathroom", "living",
        "office", "manager", "colleague", "customer", "price", "cash", "card", "bank", "airport",
        "ticket", "passport", "hotel", "holiday", "vacation", "beach", "island", "island", "guide",
        "map", "direction", "left", "right", "straight", "corner", "bridge", "hospital", "medicine",
        "pain", "health", "exercise", "sport", "team", "match", "win", "lose", "score", "gift",
        "birthday", "party", "invite", "guest", "celebrate", "wedding", "neighbor", "village",
        "farm", "farmer", "cow", "sheep", "horse", "chicken", "vegetable", "fruit", "meat", "bread",
        "rice", "egg", "milk", "tea", "coffee", "sugar", "salt", "breakfast", "lunch", "dinner",
        "wash", "brush", "shower", "dress", "hurry", "arrive", "depart", "return", "collect",
    };

    // Wider daily/social vocabulary and simple abstract words expected by B1.
    private static readonly string[] B1Additions =
    {
        "experience", "opportunity", "decision", "advantage", "disadvantage", "environment",
        "pollution", "recycle", "society", "culture", "tradition", "custom", "language",
        "communication", "relationship", "responsibility", "independence", "confidence", "ability",
        "skill", "knowledge", "achievement", "goal", "success", "failure", "challenge", "solution",
        "advice", "opinion", "argument", "agree", "disagree", "compare", "describe", "explain",
        "suggest", "recommend", "improve", "develop", "increase", "decrease", "reduce", "achieve",
        "encourage", "avoid", "prevent", "protect", "support", "provide", "require", "involve",
        "include", "produce", "create", "design", "invent", "discover", "research", "technology",
        "internet", "website", "email", "message", "device", "software", "employee", "employer",
        "salary", "career", "interview", "qualification", "university", "degree", "subject",
        "lesson", "homework", "exam", "grade", "government", "president", "election", "law",
        "crime", "prison", "economy", "industry", "factory", "product", "customer", "advertisement",
    };

    // Abstract, opinion and issue-oriented vocabulary typical of B2 discussion topics.
    private static readonly string[] B2Additions =
    {
        "perspective", "assumption", "phenomenon", "consequence", "significant", "substantial",
        "controversial", "sustainable", "efficient", "innovative", "diverse", "inevitable",
        "consistent", "contemporary", "conventional", "vulnerable", "resilient", "ethical",
        "authority", "institution", "regulation", "policy", "legislation", "infrastructure",
        "globalization", "inequality", "discrimination", "stereotype", "prejudice", "diversity",
        "identity", "integration", "immigration", "generation", "demographic", "statistic",
        "evidence", "hypothesis", "theory", "methodology", "analysis", "evaluate", "interpret",
        "justify", "criticize", "acknowledge", "emphasize", "demonstrate", "illustrate", "imply",
        "contradict", "undermine", "reinforce", "facilitate", "implement", "prioritize",
        "optimize", "collaborate", "negotiate", "compromise", "advocate", "influence",
    };

    // Nuanced, academic-register vocabulary appropriate to C1+ discourse.
    private static readonly string[] C1Additions =
    {
        "nuance", "ambiguity", "paradox", "dichotomy", "premise", "rhetoric", "discourse",
        "cognition", "empirical", "pragmatic", "arbitrary", "coherent", "credible", "plausible",
        "profound", "subtle", "tacit", "intrinsic", "inherent", "paramount", "unprecedented",
        "meticulous", "pervasive", "prevalent", "ubiquitous", "compelling", "discern",
        "reconcile", "articulate", "corroborate", "substantiate", "scrutinize", "delineate",
        "encompass", "underpin", "juxtapose", "mitigate", "exacerbate", "attribute", "constitute",
        "manifest", "prerequisite",
    };
}
