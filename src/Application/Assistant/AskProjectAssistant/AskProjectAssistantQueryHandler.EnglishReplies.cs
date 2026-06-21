namespace Application.Assistant.AskProjectAssistant;

public sealed partial class AskProjectAssistantQueryHandler
{
    private const string EnglishContactReply =
        "For EnglishAI support and official updates, use these channels:\n\n" +
        "- **Telegram support:** https://t.me/englishaiuz\n" +
        "- **EnglishAI.uz LinkedIn:** https://www.linkedin.com/company/englishai-uz/\n\n" +
        "You can also contact the project's creator, Javohir Sadullayev, on LinkedIn: " +
        "https://www.linkedin.com/in/javohir-sadullayev-b8737725a/";
    private const string EnglishSpeakingProblemReply =
        "**The difficulty may be in your learning method, not in you.** Studying words, grammar, reading and listening separately can make it hard to bring them together quickly in a conversation.\n\n" +
        "EnglishAI helps turn passive knowledge into active speaking by reusing vocabulary from the same topic in Grammar, Reading, Writing, Speaking and Listening. The AI tutor gives you a safe place to practise, explains mistakes and keeps the conversation going.\n\n" +
        "Choose **Start for free**, find your level and practise your first topic across all six skills.";
    private const string EnglishTutorReply =
        "**The AI tutor** guides conversations based on your level and chosen topic. It asks questions, listens to your answers, explains pronunciation and sentence-structure mistakes, and adapts the conversation to your responses.\n\n" +
        "It supports:\n- voice practice at a time that suits you;\n- practice matched to your level and pace;\n- immediate pronunciation and sentence feedback;\n- another try without the fear of making mistakes.";
    private const string EnglishSkillsReply =
        "**One topic connects all six skills.** For example, you first learn Travel words in Vocabulary, meet them again in Grammar and Reading, use them in Writing and Speaking, and recognise them in natural speech during Listening.\n\n" +
        "This helps you move beyond memorising a word: you practise understanding, writing, saying and hearing it.";
    private const string EnglishLessonsReply =
        "**Each CEFR level has 50 ordered lessons.** Topics unlock in sequence, with connected Vocabulary, Grammar, Reading, Writing, Speaking and Listening activities for each topic.\n\n" +
        "The level assessment helps you find a suitable starting point. The learning path then shows your next topic and exercises. You can try the initial learning flow for free.";
    private const string EnglishBooksVideoReply =
        "**Books** lets you read books matched to your CEFR level and check your understanding chapter by chapter.\n\n" +
        "**Video** offers levelled videos, interactive transcripts, word explanations, shadowing and a final quiz to practise natural pronunciation and everyday expressions. Both sections are part of the EnglishAI learning experience.";
    private const string EnglishStartingReply =
        "**You can start using EnglishAI for free.** Choose **Start for free** on the landing page, sign in and complete the level assessment or onboarding steps.\n\n" +
        "The free plan includes a level assessment, initial topics, one Speaking session per day and a limited number of SRS words. Premium features are described on the site, but purchases remain closed until Click and Payme checkout is available.";
    private const string EnglishLevelReply =
        "EnglishAI supports CEFR levels **A1 to C2**. A short adaptive level assessment helps you find a suitable starting point, and your next topics and exercises are matched to that level.";
    private const string EnglishMobileReply =
        "**Yes, EnglishAI works on your phone.** You can use the site in a mobile browser. The landing page also provides an Android APK download so you can access the learning modules and AI conversations on your phone.";
    private const string EnglishCreatorReply =
        "EnglishAI.uz was created by **Javohir Sadullayev**. His verified LinkedIn profile is: https://www.linkedin.com/in/javohir-sadullayev-b8737725a/\n\n" +
        "The project's official LinkedIn page is: https://www.linkedin.com/company/englishai-uz/";
    private const string EnglishLearningQuestionReply =
        "This is an English lesson question. **The landing-page assistant explains the EnglishAI product.** For grammar, translation and lesson-specific questions, use the in-app AI learning assistant after signing in.\n\n" +
        "Choose **Start for free** to begin. The in-app assistant can use your topic and lesson context to provide explanations and examples.";
    private const string EnglishSafeFallbackReply =
        "I can currently share this verified information: EnglishAI.uz connects Vocabulary, Grammar, Reading, Writing, Speaking and Listening in one learning flow for Uzbek-speaking learners at CEFR levels A1–C2.\n\n" +
        "Ask a more specific question about getting started, the learning modules, plans or features. Official support: https://t.me/englishaiuz";
}
