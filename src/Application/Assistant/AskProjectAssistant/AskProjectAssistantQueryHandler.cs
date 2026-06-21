using System.Text.RegularExpressions;
using Application.Ai;
using Application.Assistant.Dtos;
using Application.Assistant.Ports;
using Application.Common;
using MediatR;

namespace Application.Assistant.AskProjectAssistant;

public sealed partial class AskProjectAssistantQueryHandler(IProjectAssistant assistant, IAiFeatureScope? aiScope = null, ICurrentUserAccessor? currentUser = null)
    : IRequestHandler<AskProjectAssistantQuery, AssistantReplyDto>
{
    private const int MaxHistoryTurns = 6;
    private const string ContactReply =
        "EnglishAI support va rasmiy yangiliklari uchun quyidagi kanallardan foydalaning:\n\n" +
        "- **Telegram support:** https://t.me/englishaiuz\n" +
        "- **EnglishAI.uz LinkedIn:** https://www.linkedin.com/company/englishai-uz/\n\n" +
        "Loyiha yaratuvchisi Javohir Sadullayev bilan LinkedIn orqali ham bog'lanishingiz mumkin: " +
        "https://www.linkedin.com/in/javohir-sadullayev-b8737725a/";
    private const string SpeakingProblemReply =
        "**Muammo sizda emas — o'rganish usulida bo'lishi mumkin.** So'z, grammatika, reading va listening alohida o'rganilganda, ularni real suhbatda tez birlashtirish qiyinlashadi.\n\n" +
        "EnglishAI passiv bilimni faol nutqqa aylantirish uchun bir mavzudagi lug'atni Grammar, Reading, Writing, Speaking va Listening mashqlarida qayta ishlatadi. AI tutor esa xavfsiz muhitda gapirtiradi, xatoni tushuntiradi va suhbatni davom ettiradi.\n\n" +
        "Boshlash uchun **Bepul boshlash** tugmasini bosing, darajangizni aniqlang va birinchi mavzuni olti ko'nikmada mashq qiling.";
    private const string TutorReply =
        "**24/7 AI tutor** darajangiz va tanlangan mavzuga mos suhbatni boshqaradi. U savol beradi, javobingizni tinglaydi, talaffuz va gap tuzilishidagi xatolarni tushuntiradi, keyin suhbatni javobingizga mos davom ettiradi.\n\n" +
        "Asosiy foydasi:\n- istalgan vaqtda ovozli mashq;\n- shaxsiy daraja va temp;\n- darhol talaffuz va jumla feedbacki;\n- xato qilishdan qo'rqmasdan qayta urinish.";
    private const string SkillsReply =
        "**Bitta mavzu oltita ko'nikmada ketma-ket ishlatiladi.** Masalan, Travel mavzusidagi so'zlar avval Vocabulary'da o'rganiladi, Grammar va Reading'da qayta uchraydi, Writing va Speaking'da siz tomondan ishlatiladi, Listening'da esa tabiiy nutq ichida taniladi.\n\n" +
        "Shu sabab yangi so'z faqat yodda qolmaydi — uni tushunish, yozish, aytish va eshitish ko'nikmalari birga rivojlanadi.";
    private const string LessonsReply =
        "**Har bir CEFR darajasida 50 ta tartiblangan dars mavjud.** Darslar mavzular bo'yicha ketma-ket ochiladi va har bir mavzuda bog'langan Vocabulary, Grammar, Reading, Writing, Speaking hamda Listening faoliyatlari ishlaydi.\n\n" +
        "Daraja aniqlash bosqichi mos boshlanish nuqtasini topadi; keyin tizim sizga mavzu va keyingi mashqlarni ko'rsatadi. Dastlabki bepul oqim orqali tizimni sinab ko'rish mumkin.";
    private const string BooksVideoReply =
        "**Books** bo'limida CEFR darajangizga mos kitoblarni o'qib, bo'limlar bo'yicha tushunishni tekshirasiz.\n\n" +
        "**Video** bo'limida darajalangan videolar, interaktiv transkript, so'z izohlari, shadowing va yakuniy quiz orqali tabiiy talaffuz hamda jonli iboralarni mashq qilasiz. Bu bo'limlar EnglishAI o'quv jarayonining amaldagi qismlaridir.";
    private const string StartingReply =
        "**EnglishAI'ni bepul boshlash mumkin.** Landing sahifadagi **Bepul boshlash** tugmasini bosing, tizimga kiring va daraja aniqlash yoki onboarding bosqichidan o'ting.\n\n" +
        "Bepul rejada daraja aniqlash testi, dastlabki mavzular, kuniga bitta Speaking sessiyasi va cheklangan SRS so'zlari mavjud. Premium imkoniyatlari sahifada oldindan ko'rsatilgan, ammo Click va Payme checkout ishga tushmaguncha sotib olish ochilmagan.";
    private const string LevelReply =
        "EnglishAI **A1 dan C2 gacha** bo'lgan CEFR darajalariga moslashtirilgan. Qisqa adaptiv daraja aniqlash testi sizga mos boshlanish nuqtasini topadi, keyingi mavzu va mashqlar esa shu darajaga qarab beriladi.";
    private const string MobileReply =
        "**Ha, EnglishAI telefonda ishlaydi.** Saytdan mobil brauzer orqali foydalanish mumkin. Landing sahifada Android uchun APK yuklab olish havolasi ham mavjud; o'rnatilgach, o'quv modullari va AI suhbatlardan telefonda foydalanasiz.";
    private const string CreatorReply =
        "EnglishAI.uz loyihasini **Javohir Sadullayev** yaratgan. Uning tasdiqlangan LinkedIn profili: https://www.linkedin.com/in/javohir-sadullayev-b8737725a/\n\n" +
        "Loyihaning rasmiy LinkedIn sahifasi: https://www.linkedin.com/company/englishai-uz/";
    private const string LearningQuestionReply =
        "Bu savol ingliz tili darsiga tegishli. **Landing'dagi yordamchi EnglishAI loyihasi haqida ma'lumot beradi**, grammatika, tarjima va dars kontekstidagi savollar uchun esa tizimga kirgandan keyingi AI o'quv yordamchisidan foydalaning.\n\n" +
        "Boshlash uchun **Bepul boshlash** tugmasini bosing. Ichki yordamchi mavzu va dars kontekstini ko'rib, o'zbekcha izoh va misollar beradi.";
    private const string SafeFallbackReply =
        "Bu savol bo'yicha hozir faqat tasdiqlangan ma'lumotni bera olaman: EnglishAI.uz o'zbek tilida so'zlashuvchi o'quvchilar uchun A1–C2 darajalarida Vocabulary, Grammar, Reading, Writing, Speaking va Listening mashqlarini bitta o'quv oqimida bog'laydi.\n\n" +
        "Qanday boshlash, modullar, tariflar yoki imkoniyatlardan birini aniqroq so'rasangiz, batafsil tushuntiraman. Rasmiy support: https://t.me/englishaiuz";

    public async Task<AssistantReplyDto> Handle(
        AskProjectAssistantQuery request,
        CancellationToken cancellationToken)
    {
        var question = request.Question.Trim();
        var fallbackReply = request.Locale == "en" ? EnglishSafeFallbackReply : SafeFallbackReply;
        if (string.IsNullOrWhiteSpace(question))
            return new AssistantReplyDto(fallbackReply);

        var deterministicReply = GetDeterministicReply(question, request.Locale);
        if (deterministicReply is not null)
            return new AssistantReplyDto(deterministicReply);

        var history = request.History
            .TakeLast(MaxHistoryTurns)
            .Select(turn => new AssistantTurn(turn.Role.Trim().ToLowerInvariant(), turn.Text.Trim()))
            .ToArray();

        string? reply;
        try
        {
            using var scope = await (aiScope ?? NoOpAiFeatureScope.Instance).EnterAsync(AiFeature.Assistant, currentUser?.LearnerId, cancellationToken);
            reply = await assistant.AnswerAsync(question, history, request.Locale, cancellationToken);
        }
        catch (AiAdmissionException)
        {
            reply = null;
        }
        return new AssistantReplyDto(IsSafeProviderReply(reply) ? reply!.Trim() : fallbackReply);
    }

    private static string? GetDeterministicReply(string question, string locale)
    {
        var normalized = Normalize(question);
        var english = locale == "en";
        if (IsLearningQuestion(normalized)) return english ? EnglishLearningQuestionReply : LearningQuestionReply;
        if (ContainsAny(normalized, "6 skill", "six skills", "olti skill", "6 ko'nikma", "olti ko'nikma") && ContainsAny(normalized, "bog'lan", "boglan", "bir mavzu", "connect", "same topic")) return english ? EnglishSkillsReply : SkillsReply;
        if (ContainsAny(normalized, "support", "contact", "aloqa", "kontakt", "telegram", "linkedin", "kimga yoz", "kim bilan gaplash", "murojaat", "yordam olish", "yordam kerak") || ContactPhraseRegex().IsMatch(normalized)) return english ? EnglishContactReply : ContactReply;
        if (ContainsAny(normalized, "3 yil", "gapira olmay", "gapirish qiyin", "nega gap", "passiv bilim", "speaking harder", "hard to speak", "cannot speak", "can't speak")) return english ? EnglishSpeakingProblemReply : SpeakingProblemReply;
        if (ContainsAny(normalized, "24/7", "ai tutor", "ai o'qituvchi", "ai oqituvchi") && ContainsAny(normalized, "qanday", "ishlay", "nima qiladi", "how", "work", "what does")) return english ? EnglishTutorReply : TutorReply;
        if (ContainsAny(normalized, "50 dars", "dars qanday tuzil", "darslar qanday tuzil", "50 lessons", "learning path")) return english ? EnglishLessonsReply : LessonsReply;
        if (ContainsAny(normalized, "books", "kitob", "kutubxona", "video section", "video catalogue", "video catalog", "video practice") || ContainsAny(normalized, "video bo'lim", "video bolim", "video va")) return english ? EnglishBooksVideoReply : BooksVideoReply;
        if (ContainsAny(normalized, "bepul", "qanday boshl", "ro'yxatdan", "royxatdan", "narx", "premium", "to'lov", "tolov", "payme", "click", "free", "get started", "how to start", "how do i start", "sign up", "pricing", "payment")) return english ? EnglishStartingReply : StartingReply;
        if (ContainsAny(normalized, "a1", "a2", "b1", "b2", "c1", "c2", "daraja", "cefr", "learning level", "my level")) return english ? EnglishLevelReply : LevelReply;
        if (ContainsAny(normalized, "telefon", "mobil", "android", "apk", "iphone", "ios", "phone")) return english ? EnglishMobileReply : MobileReply;
        if (ContainsAny(normalized, "kim yarat", "yaratuvchi", "asoschi", "javohir sadullayev", "who created", "creator", "founder")) return english ? EnglishCreatorReply : CreatorReply;
        return null;
    }

    private static bool IsLearningQuestion(string normalized) =>
        ContainsAny(normalized,
            "present simple", "past simple", "future simple", "present continuous", "past continuous",
            "present perfect", "grammar", "grammatika", "tarjima", "translate", "inglizchaga", "o'zbekchaga",
            "ozbekchaga", "so'z nimani", "soz nimani", "misol tuz", "gap tuz", "talaffuz");

    private static bool IsSafeProviderReply(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply) || reply.Length < 30 || reply.Length > 2500) return false;
        var normalized = Normalize(reply);
        if (ContainsAny(normalized,
            "email orqali ro'yxatdan", "email orqali royxatdan", "telegram orqali ro'yxatdan", "telegram orqali royxatdan",
            "books va video bo'limlari englishai.uz platformasida ingliz tili o'rganishga mo'ljallanmagan",
            "kafolatlangan natija", "guaranteed result")) return false;

        foreach (Match match in UrlRegex().Matches(reply))
        {
            var url = match.Value.TrimEnd('.', ',', ')', ']');
            if (url is not "https://t.me/englishaiuz"
                and not "https://www.linkedin.com/company/englishai-uz/"
                and not "https://www.linkedin.com/in/javohir-sadullayev-b8737725a/")
                return false;
        }

        return true;
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant().Replace('’', '\'').Replace('‘', '\'').Replace('ʻ', '\'').Replace('ʼ', '\'');

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(term => value.Contains(term, StringComparison.Ordinal));

    [GeneratedRegex(@"\b(qanday|qayerdan)\s+bog'lan(?:aman|ish|sa|ishim)?\b|\bkimga\s+murojaat\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ContactPhraseRegex();

    [GeneratedRegex(@"https?://[^\s)\]}]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();
}
