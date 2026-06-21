namespace Application.Grammar.Content;

/// <summary>
/// A vetted, hand-authored Uzbek grammar lesson for one grammar focus code (docs/development-guide.md rule 11 - the
/// learner-facing Uzbek is never free-generated). The explanation is written in clear Uzbek, with
/// English kept verbatim only where it must be exact: the formulas and the example sentences. Each
/// example pairs an English sentence with its checked Uzbek translation, so the learner reads the
/// rule in their own language but always sees the real English form.
/// </summary>
public sealed record CuratedGrammarLesson(
    string FocusCode,
    string TitleUz,
    string SummaryUz,
    IReadOnlyList<string> Formulas,
    IReadOnlyList<CuratedGrammarRule> Rules,
    IReadOnlyList<CuratedGrammarExample> Examples,
    IReadOnlyList<string> CommonMistakesUz);

/// <summary>One explanation block: a short Uzbek heading and its Uzbek body (English forms inline).</summary>
public sealed record CuratedGrammarRule(string HeadingUz, string BodyUz);

/// <summary>An example sentence: the exact English and its vetted Uzbek meaning.</summary>
public sealed record CuratedGrammarExample(string English, string Uzbek);

/// <summary>
/// The curated grammar lesson store, keyed by the learning-spine grammar focus code. It now covers
/// every grammar focus of the spine syllabus - all 10 focuses per CEFR level, 60 in total, ordered
/// from the simplest point (A1 <c>to-be</c>) to the hardest (C2 <c>register-and-formality</c>) - so
/// the Grammar module shows vetted Uzbek for every topic. A focus with no curated entry (e.g. a code
/// outside the spine) still falls back to the generated English lesson, so adding a new spine focus
/// without a curated entry degrades gracefully rather than breaking.
/// </summary>
public static class CuratedGrammarLessonCatalog
{
    /// <summary>Returns the curated Uzbek lesson for a focus code, or null if none is authored yet.</summary>
    public static CuratedGrammarLesson? TryGet(string? focusCode)
    {
        if (string.IsNullOrWhiteSpace(focusCode))
            return null;

        return Lessons.GetValueOrDefault(focusCode.Trim().ToLowerInvariant());
    }

    // ───────────────────────────── A1 - to be (am / is / are) ─────────────────────────────
    private static readonly CuratedGrammarLesson ToBe = new(
        "to-be",
        "«To be» fe'li - am / is / are",
        "«To be» fe'li biror narsa yoki kimdir nima ekanini, qanaqaligini yoki qayerdaligini bildiradi. " +
        "O'zbekchada ko'pincha «-man, -san, -dir» qo'shimchalari yoki «edi» orqali ifodalanadi.",
        new[]
        {
            "I + am  →  I am (qisqasi: I'm)",
            "He / She / It + is  →  He is (qisqasi: He's)",
            "You / We / They + are  →  You are (qisqasi: You're)",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Tasdiq gap",
                "Tuzilishi: ega + am/is/are + qolgan so'zlar. Masalan: I am a student (Men talabaman), " +
                "She is happy (U xursand), They are friends (Ular do'stlar)."),
            new CuratedGrammarRule(
                "Inkor gap",
                "am/is/are dan keyin not qo'shiladi: I am not, he is not (isn't), we are not (aren't). " +
                "Masalan: I am not tired (Men charchamadim)."),
            new CuratedGrammarRule(
                "So'roq gap",
                "am/is/are gap boshiga chiqadi: Are you ready? (Tayyormisan?), Is he your brother? " +
                "(U sening akangmi?)."),
            new CuratedGrammarRule(
                "Qisqartmalar",
                "Kundalik nutqda qisqa shakllar ko'p ishlatiladi: I'm, you're, he's, she's, it's, " +
                "we're, they're."),
        },
        new[]
        {
            new CuratedGrammarExample("I am from Uzbekistan.", "Men O'zbekistondanman."),
            new CuratedGrammarExample("She is my mother.", "U mening onam."),
            new CuratedGrammarExample("We are at home.", "Biz uydamiz."),
            new CuratedGrammarExample("They are not tired.", "Ular charchamagan."),
            new CuratedGrammarExample("Is it cold today?", "Bugun sovuqmi?"),
        },
        new[]
        {
            "«I» har doim «am» bilan keladi: «I is» emas, «I am».",
            "«To be» fe'lini tushirib qoldirmang: «She happy» emas, «She is happy» (U xursand).",
            "Ko'plik bilan «are» ishlatiladi: «They is» emas, «They are»; «My friends are».",
        });

    // ───────────────────────────── A2 - Past Simple ─────────────────────────────
    private static readonly CuratedGrammarLesson PastSimple = new(
        "past-simple",
        "Past Simple - oddiy o'tgan zamon",
        "Past Simple o'tmishda boshlanib tugagan, ma'lum bir vaqtda bo'lib o'tgan ish-harakatni " +
        "bildiradi. O'zbekchadagi «-di» o'tgan zamoniga to'g'ri keladi: bordi, ko'rdi, ishladi.",
        new[]
        {
            "To'g'ri fe'l (regular): fe'l + -ed  →  work → worked, play → played",
            "Noto'g'ri fe'l (irregular): maxsus shakl  →  go → went, see → saw, buy → bought",
            "Inkor: did not (didn't) + fe'lning asosiy shakli  →  I didn't go",
            "So'roq: Did + ega + asosiy fe'l?  →  Did you go?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Tasdiq - to'g'ri fe'llar",
                "Fe'lga -ed qo'shiladi: I worked yesterday (Men kecha ishladim). «-e» bilan tugasa faqat " +
                "«-d» qo'shiladi: live → lived."),
            new CuratedGrammarRule(
                "Tasdiq - noto'g'ri fe'llar",
                "Ba'zi fe'llar -ed olmaydi, ularning shakli yodlanadi: go → went, eat → ate, " +
                "have → had, buy → bought."),
            new CuratedGrammarRule(
                "Inkor",
                "did not (didn't) + fe'lning asosiy shakli ishlatiladi. Diqqat: didn't dan keyin fe'l " +
                "o'tgan zamonga qo'yilmaydi: «I didn't went» emas, «I didn't go»."),
            new CuratedGrammarRule(
                "So'roq",
                "Did + ega + asosiy fe'l: Did she call you? (U senga qo'ng'iroq qildimi?)."),
            new CuratedGrammarRule(
                "Vaqt so'zlari",
                "Ko'pincha shu so'zlar bilan keladi: yesterday (kecha), last week (o'tgan hafta), " +
                "two days ago (ikki kun oldin), in 2010 (2010-yilda)."),
        },
        new[]
        {
            new CuratedGrammarExample("I visited my grandmother last weekend.",
                "Men o'tgan dam olish kunlari buvimni ko'rgani bordim."),
            new CuratedGrammarExample("He bought a new phone.", "U yangi telefon sotib oldi."),
            new CuratedGrammarExample("We didn't watch the film.", "Biz filmni ko'rmadik."),
            new CuratedGrammarExample("Did you finish your homework?", "Uy vazifangni tugatdingmi?"),
            new CuratedGrammarExample("They went to the park yesterday.", "Ular kecha bog'ga borishdi."),
        },
        new[]
        {
            "Inkor va so'roqda fe'lni o'tgan zamonga qo'ymang: «Did he came?» emas, «Did he come?».",
            "Noto'g'ri fe'llarga -ed qo'shmang: «goed» emas, «went».",
            "«was/were» bilan «did» ni aralashtirmang: «I was tired» (bu yerda did kerak emas).",
        });

    // ───────────────────────────── B1 - Present Perfect ─────────────────────────────
    private static readonly CuratedGrammarLesson PresentPerfect = new(
        "present-perfect",
        "Present Perfect - hozirgi tugallangan zamon (have/has + V3)",
        "Present Perfect o'tmishda bo'lgan, ammo natijasi yoki ahamiyati hozir muhim bo'lgan " +
        "ish-harakatni bildiradi. Bunda aniq o'tgan vaqt ko'rsatilmaydi.",
        new[]
        {
            "have / has + past participle (V3)  →  I have seen, She has gone",
            "Inkor: have/has + not + V3  →  I haven't finished",
            "So'roq: Have/Has + ega + V3?  →  Have you eaten?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Qachon ishlatiladi",
                "1) O'tmishdagi tajriba, vaqti muhim emas: I have been to London. " +
                "2) O'tmishda boshlanib, hozir ham davom etayotgan: She has lived here for five years. " +
                "3) Yaqinda bo'lgan, natijasi hozir ko'rinib turibdi: I have lost my keys."),
            new CuratedGrammarRule(
                "have yoki has",
                "I / you / we / they bilan «have». He / she / it bilan «has»."),
            new CuratedGrammarRule(
                "Past Participle (V3)",
                "To'g'ri fe'llarda -ed (work → worked). Noto'g'ri fe'llarda 3-shakl: go → gone, " +
                "see → seen, do → done, be → been."),
            new CuratedGrammarRule(
                "for va since",
                "for + davomiylik: for three years (uch yildan beri). " +
                "since + boshlanish nuqtasi: since 2020, since Monday (dushanbadan beri)."),
            new CuratedGrammarRule(
                "Past Simple bilan farqi",
                "Aniq o'tgan vaqt bo'lsa (yesterday, last year) → Past Simple. Vaqt aniq bo'lmasa yoki " +
                "hozirga bog'liq bo'lsa → Present Perfect. Solishtiring: I saw him yesterday / " +
                "I have seen him (qachondir)."),
        },
        new[]
        {
            new CuratedGrammarExample("I have finished my work.", "Men ishimni tugatdim (natijasi: hozir bo'shman)."),
            new CuratedGrammarExample("She has never been to Paris.", "U hech qachon Parijda bo'lmagan."),
            new CuratedGrammarExample("Have you ever eaten sushi?", "Hech qachon sushi yeganmisan?"),
            new CuratedGrammarExample("We have lived here since 2018.", "Biz bu yerda 2018-yildan beri yashaymiz."),
            new CuratedGrammarExample("He hasn't called me yet.", "U menga hali qo'ng'iroq qilmadi."),
        },
        new[]
        {
            "Aniq o'tgan vaqt bilan ishlatmang: «I have seen him yesterday» emas, «I saw him yesterday».",
            "have/has dan keyin fe'lning 3-shakli (V3) kerak: «I have went» emas, «I have gone».",
            "for va since ni almashtirmang: «since three years» emas, «for three years».",
        });

    // ───────────────────────────── B2 - Present Perfect Continuous ─────────────────────────────
    private static readonly CuratedGrammarLesson PresentPerfectContinuous = new(
        "present-perfect-continuous",
        "Present Perfect Continuous - have/has been + V-ing",
        "Present Perfect Continuous o'tmishda boshlanib, hozirgacha davom etayotgan (yoki endigina " +
        "tugagan) ish-harakatning davomiyligiga urg'u beradi.",
        new[]
        {
            "have / has + been + fe'l-ing  →  I have been working",
            "Inkor: have/has + not + been + V-ing  →  She hasn't been sleeping",
            "So'roq: Have/Has + ega + been + V-ing?  →  Have you been waiting?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Asosiy ma'no",
                "Harakat qancha vaqtdan beri davom etayotganiga urg'u beradi: " +
                "I have been studying English for two hours (Men ikki soatdan beri ingliz tilini o'rganyapman)."),
            new CuratedGrammarRule(
                "Endigina tugagan, natijasi ko'rinadigan",
                "Why are you tired? - I have been running. (Yaqindagina yugurganim uchun charchadim.)"),
            new CuratedGrammarRule(
                "for va since",
                "for + davomiylik: for an hour (bir soatdan beri). since + boshlanish: since morning (ertalabdan beri)."),
            new CuratedGrammarRule(
                "Present Perfect bilan farqi",
                "Continuous → jarayon va davomiylik muhim: I have been reading this book. " +
                "Simple → natija yoki son muhim: I have read three books."),
            new CuratedGrammarRule(
                "Holat fe'llari bilan ishlatilmaydi",
                "know, like, want, be kabi holat fe'llari odatda -ing shaklida kelmaydi: " +
                "«I have been knowing» emas, «I have known»."),
        },
        new[]
        {
            new CuratedGrammarExample("I have been waiting for an hour.", "Men bir soatdan beri kutyapman."),
            new CuratedGrammarExample("It has been raining all day.", "Kun bo'yi yomg'ir yog'yapti."),
            new CuratedGrammarExample("She has been learning to drive.", "U mashina haydashni o'rganib yuribdi."),
            new CuratedGrammarExample("Have you been working here long?", "Bu yerda ancha vaqtdan beri ishlaysizmi?"),
            new CuratedGrammarExample("They haven't been feeling well.", "Ularning o'zlarini yaxshi his qilmayotganiga ancha bo'ldi."),
        },
        new[]
        {
            "Holat fe'llari bilan ishlatmang: «I have been knowing» emas, «I have known».",
            "«been» so'zini tushirib qoldirmang: «I have working» emas, «I have been working».",
            "Natija yoki son muhim bo'lsa Simple kerak: «I have written three emails» (have been writing emas).",
        });

    // ───────────────────────────── C1 - Mixed Conditionals ─────────────────────────────
    private static readonly CuratedGrammarLesson MixedConditionals = new(
        "mixed-conditionals",
        "Mixed Conditionals - aralash shart gaplar",
        "Aralash shart gaplar ikki xil vaqtni bog'laydi: gapning bir qismi o'tmishga, ikkinchi qismi " +
        "esa hozirgi yoki umumiy vaqtga tegishli bo'ladi. Ular ko'pincha real bo'lmagan (xayoliy) " +
        "vaziyatlarni ifodalaydi.",
        new[]
        {
            "O'tmish sharti → hozirgi natija:  If + past perfect, ... would + fe'l",
            "    If I had studied medicine, I would be a doctor now.",
            "Hozirgi holat → o'tmish natijasi:  If + past simple, ... would have + V3",
            "    If I were more careful, I wouldn't have made that mistake.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Nega «aralash» deyiladi",
                "Oddiy shart gaplarda shart ham, natija ham bir vaqtga tegishli. Aralash gaplarda esa " +
                "shart va natija turli vaqtlarda bo'ladi - bu hayotdagi real vaziyatlarni aniqroq ifodalaydi."),
            new CuratedGrammarRule(
                "O'tmish → hozir",
                "O'tmishda bo'lmagan ish bugungi holatga ta'sir qiladi: If she had taken the job, she " +
                "would live in London now (Agar u o'sha ishni qabul qilganida, hozir Londonda yashardi)."),
            new CuratedGrammarRule(
                "Hozir → o'tmish",
                "Umumiy yoki hozirgi holat o'tmishdagi natijani tushuntiradi: If he weren't so shy, he " +
                "would have asked her (Agar u shunchalik uyatchan bo'lmaganida, undan so'ragan bo'lardi)."),
            new CuratedGrammarRule(
                "«were» - barcha shaxslar uchun",
                "Xayoliy shartlarda «was» o'rniga ko'pincha «were» ishlatiladi: If I were you..., " +
                "If he weren't..."),
        },
        new[]
        {
            new CuratedGrammarExample("If I had saved money, I would be on holiday now.",
                "Agar pul yig'ganimda, hozir ta'tilda bo'lardim."),
            new CuratedGrammarExample("If she spoke French, she would have got the job.",
                "Agar u fransuzcha gapira olganida, o'sha ishni olgan bo'lardi."),
            new CuratedGrammarExample("If we had left earlier, we would be there by now.",
                "Agar ertaroq chiqqanimizda, hozirga kelib u yerda bo'lardik."),
            new CuratedGrammarExample("If he weren't afraid of flying, he would have visited us.",
                "Agar u uchishdan qo'rqmaganida, bizni ko'rgani kelgan bo'lardi."),
        },
        new[]
        {
            "Ikkala qismni bir vaqtga qo'ymang - aralash gapning ma'nosi aynan vaqt farqida.",
            "«would have + V3» (o'tmish natijasi) va «would + fe'l» (hozirgi natija) ni adashtirmang.",
            "«If» qismida «would» ishlatilmaydi: «If I would have...» emas, «If I had...».",
        });

    // ───────────────────────────── C2 - Hypothetical Meaning ─────────────────────────────
    private static readonly CuratedGrammarLesson HypotheticalMeaning = new(
        "hypothetical-meaning",
        "Hypothetical Meaning - xayoliy/farazlangan ma'no",
        "Ingliz tilida real bo'lmagan, faraziy yoki istalgan holatlarni ifodalash uchun maxsus " +
        "tuzilmalar ishlatiladi: wish, if only, would rather, it's time, as if/as though. Ularda " +
        "o'tgan zamon shakli o'tmishni emas, real bo'lmagan hozirgi yoki o'tmishni bildiradi.",
        new[]
        {
            "wish / if only + past simple  →  hozirgi holatga afsus:  I wish I knew the answer.",
            "wish / if only + past perfect  →  o'tmishga afsus:  I wish I had listened.",
            "wish + would  →  boshqaning xatti-harakati o'zgarishini istash:  I wish he would stop talking.",
            "would rather + past simple  →  boshqa shaxsdan istak:  I'd rather you didn't smoke.",
            "it's (high) time + past simple  →  endi vaqti keldi:  It's time we left.",
            "as if / as though + past  →  haqiqatga zid taqqoslash:  He talks as if he knew everything.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "O'tgan zamon = farazlik",
                "Bu tuzilmalarda past simple yoki past perfect o'tmishni emas, real bo'lmagan hozirgi " +
                "yoki o'tmishni bildiradi. Masalan: I wish I had a car - hozir mashinam yo'q (afsus)."),
            new CuratedGrammarRule(
                "wish ning uch ko'rinishi",
                "1) past simple → hozirgi holatga afsus: I wish I were taller. " +
                "2) past perfect → o'tmishga afsus: I wish I had studied. " +
                "3) would → boshqaning harakati o'zgarishini istash: I wish it would stop raining."),
            new CuratedGrammarRule(
                "would rather",
                "O'zi haqida: would rather + infinitive (I'd rather stay). " +
                "Boshqa shaxs haqida: would rather + past simple (I'd rather you came tomorrow)."),
            new CuratedGrammarRule(
                "it's time va as if",
                "it's time + past = endi vaqti keldi: It's high time you found a job. " +
                "as if / as though + past = haqiqatga zid taqqoslash: She acts as if she owned the place."),
            new CuratedGrammarRule(
                "«were» - rasmiy shaklda",
                "Farazlik bildirganda barcha shaxslar bilan «were»: If I were..., I wish he were here, " +
                "as if he were the boss."),
        },
        new[]
        {
            new CuratedGrammarExample("I wish I had more free time.", "Koshki ko'proq bo'sh vaqtim bo'lsa edi."),
            new CuratedGrammarExample("If only we had booked earlier!", "Qaniydi ertaroq band qilganimizda edi!"),
            new CuratedGrammarExample("I'd rather you didn't tell anyone.", "Hech kimga aytmaganingizni afzal ko'raman."),
            new CuratedGrammarExample("It's time we made a decision.", "Endi qaror qiladigan vaqt keldi."),
            new CuratedGrammarExample("He spends money as if he were rich.", "U go'yo boy odamdek pul sarflaydi."),
        },
        new[]
        {
            "Holat uchun «I wish I would be» emas, «I wish I were» ishlatiladi.",
            "«would» faqat boshqaning harakati uchun: o'z xohishingizni «I wish I would...» bilan ifodalamang.",
            "it's time / would rather dan keyin past simple kerak: «It's time we go» emas, «It's time we went».",
        });

    // ───────────────────────────── A1 - Present Simple ─────────────────────────────
    private static readonly CuratedGrammarLesson PresentSimple = new(
        "present-simple",
        "Present Simple - hozirgi oddiy zamon",
        "Present Simple takrorlanadigan ish-harakatlarni, odatlarni va umumiy haqiqatlarni bildiradi. " +
        "O'zbekchada «-adi/-ydi» hozirgi-kelasi zamoniga to'g'ri keladi: ishlaydi, yashaydi, biladi.",
        new[]
        {
            "I / You / We / They + fe'l (asosiy shakl)  →  I work, They live",
            "He / She / It + fe'l + -s/-es  →  He works, She goes",
            "Inkor: do/does + not + asosiy fe'l  →  I don't work, He doesn't work",
            "So'roq: Do/Does + ega + asosiy fe'l?  →  Do you work? Does she work?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Uchinchi shaxs birlik (-s)",
                "He / she / it bilan fe'lga -s qo'shiladi: works, plays, reads. «-o, -ss, -sh, -ch, -x» " +
                "bilan tugasa -es: go → goes, watch → watches. Undosh + «-y» bo'lsa «-y» «-ies» ga aylanadi: " +
                "study → studies."),
            new CuratedGrammarRule(
                "Inkor",
                "I / you / we / they bilan don't, he / she / it bilan doesn't. doesn't dan keyin fe'lga -s " +
                "qo'shilmaydi: «He doesn't works» emas, «He doesn't work»."),
            new CuratedGrammarRule(
                "So'roq",
                "Do / Does gap boshiga chiqadi: Do you like tea? Does he speak English? Bu yerda ham asosiy " +
                "fe'l -s olmaydi."),
            new CuratedGrammarRule(
                "Qachon ishlatiladi",
                "1) Odat: I get up at seven. 2) Umumiy haqiqat: Water boils at 100 degrees. " +
                "3) Doimiy holat: She lives in Tashkent."),
        },
        new[]
        {
            new CuratedGrammarExample("I drink coffee every morning.", "Men har kuni ertalab qahva ichaman."),
            new CuratedGrammarExample("She works in a hospital.", "U kasalxonada ishlaydi."),
            new CuratedGrammarExample("They don't watch TV.", "Ular televizor ko'rmaydi."),
            new CuratedGrammarExample("Does he speak English?", "U ingliz tilida gapiradimi?"),
            new CuratedGrammarExample("The sun rises in the east.", "Quyosh sharqdan chiqadi."),
        },
        new[]
        {
            "Uchinchi shaxs birlikda -s ni unutmang: «He work» emas, «He works».",
            "Inkor va so'roqda fe'lga -s qo'shmang: «Does she likes?» emas, «Does she like?».",
            "do/does ni «to be» bilan aralashtirmang: «I am work» emas, «I work».",
        });

    // ───────────────────────────── A1 - Articles (a / an / the) ─────────────────────────────
    private static readonly CuratedGrammarLesson Articles = new(
        "articles",
        "Artikllar - a / an / the",
        "Artikllar otdan oldin keladi va uning aniq yoki noaniq ekanini ko'rsatadi. O'zbekchada artikl yo'q, " +
        "shuning uchun ularni alohida o'rganish kerak: «a/an» - noaniq (har qanday bittasi), «the» - aniq " +
        "(o'sha aniq narsa).",
        new[]
        {
            "a + undosh tovush bilan boshlanadigan so'z  →  a book, a car, a university",
            "an + unli tovush bilan boshlanadigan so'z  →  an apple, an hour, an idea",
            "the + aniq, ma'lum narsa (birlik yoki ko'plik)  →  the book, the books",
        },
        new[]
        {
            new CuratedGrammarRule(
                "a yoki an",
                "Tanlov tovushga qarab, harfga emas: a university (yu- tovushi), an hour (h aytilmaydi, soat). " +
                "a/an faqat sanaladigan birlik otlar bilan ishlatiladi."),
            new CuratedGrammarRule(
                "Qachon «a/an»",
                "Narsa birinchi marta tilga olinganda yoki qaysi biri ekani muhim bo'lmaganda: " +
                "I saw a dog (qandaydir it)."),
            new CuratedGrammarRule(
                "Qachon «the»",
                "Tinglovchi qaysi narsa haqida gapirilayotganini bilganda: ikkinchi marta eslatilganda " +
                "(I saw a dog. The dog was big.), yagona narsalar bilan (the sun, the moon)."),
            new CuratedGrammarRule(
                "Artiklsiz holatlar",
                "Umumiy ma'noda ko'plik yoki sanalmaydigan otlar oldida artikl qo'yilmaydi: " +
                "I like music. Cats are independent."),
        },
        new[]
        {
            new CuratedGrammarExample("I need a pen.", "Menga ruchka kerak (qaysidir bittasi)."),
            new CuratedGrammarExample("She is an engineer.", "U muhandis."),
            new CuratedGrammarExample("Close the door, please.", "Iltimos, eshikni yoping (o'sha aniq eshik)."),
            new CuratedGrammarExample("The sun is very bright today.", "Bugun quyosh juda yorqin."),
            new CuratedGrammarExample("I like coffee.", "Men qahvani yaxshi ko'raman (umuman qahva)."),
        },
        new[]
        {
            "Tovushga qarang, harfga emas: «a hour» emas, «an hour»; «an university» emas, «a university».",
            "Yagona, ma'lum narsalar bilan «the»: «a sun» emas, «the sun».",
            "Umumiy ko'plik oldida artikl shart emas: «I like the dogs» (umuman) emas, «I like dogs».",
        });

    // ───────────────────────────── A1 - Plural Nouns ─────────────────────────────
    private static readonly CuratedGrammarLesson PluralNouns = new(
        "plural-nouns",
        "Ko'plik otlar - -s / -es",
        "Ingliz tilida ot birdan ortiq bo'lsa, oxiriga -s yoki -es qo'shiladi. O'zbekchadagi «-lar» " +
        "qo'shimchasiga o'xshaydi: kitob → kitoblar (book → books).",
        new[]
        {
            "Ko'pchilik otlar: + -s  →  book → books, car → cars",
            "«-s, -ss, -sh, -ch, -x» bilan tugasa: + -es  →  bus → buses, box → boxes",
            "Undosh + «-y»: «-y» → «-ies»  →  city → cities, baby → babies",
            "Noto'g'ri ko'pliklar (yodlanadi)  →  man → men, child → children, foot → feet",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Oddiy qoida",
                "Ko'pchilik otlarga shunchaki -s qo'shiladi: dogs, tables, ideas."),
            new CuratedGrammarRule(
                "-es qo'shiladigan holat",
                "Hushtaksimon tovushlar bilan tugaganda -es: watches, wishes, glasses, tomatoes."),
            new CuratedGrammarRule(
                "«-y» bilan tugaganlar",
                "Undosh + y → ies: country → countries. Lekin unli + y bo'lsa shunchaki -s: boy → boys, " +
                "day → days."),
            new CuratedGrammarRule(
                "Noto'g'ri va o'zgarmas ko'pliklar",
                "Ba'zilari maxsus shaklga ega: woman → women, tooth → teeth, person → people. " +
                "Ba'zilari o'zgarmaydi: one sheep, two sheep; one fish, two fish."),
        },
        new[]
        {
            new CuratedGrammarExample("I have two brothers.", "Mening ikkita akam bor."),
            new CuratedGrammarExample("There are many buses here.", "Bu yerda ko'p avtobuslar bor."),
            new CuratedGrammarExample("Big cities are crowded.", "Katta shaharlar gavjum."),
            new CuratedGrammarExample("Three children are playing.", "Uchta bola o'ynayapti."),
            new CuratedGrammarExample("My feet hurt.", "Oyoqlarim og'riyapti."),
        },
        new[]
        {
            "Noto'g'ri ko'pliklarga -s qo'shmang: «childs» emas, «children»; «mans» emas, «men».",
            "Undosh + y bo'lsa «-ys» emas: «citys» emas, «cities».",
            "Ko'plik otdan oldin «a/an» ishlatmang: «a books» emas, «books» yoki «a book».",
        });

    // ───────────────────────────── A1 - There is / There are ─────────────────────────────
    private static readonly CuratedGrammarLesson ThereIsThereAre = new(
        "there-is-there-are",
        "There is / There are - mavjudlikni bildirish",
        "«There is / There are» biror narsaning mavjudligini yoki qayerdadir borligini bildiradi. " +
        "O'zbekchadagi «bor» so'ziga to'g'ri keladi: «Stolda kitob bor» → There is a book on the table.",
        new[]
        {
            "There is + birlik ot  →  There is a chair in the room.",
            "There are + ko'plik ot  →  There are two windows.",
            "Inkor: There isn't / There aren't  →  There isn't any milk.",
            "So'roq: Is there...? / Are there...?  →  Are there any shops nearby?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "is yoki are",
                "Otning soniga qaragan holda tanlanadi: birlik bo'lsa There is, ko'plik bo'lsa There are. " +
                "Sanalmaydigan otlar bilan There is: There is some water."),
            new CuratedGrammarRule(
                "Inkor",
                "There isn't / There aren't ishlatiladi, ko'pincha «any» bilan: There aren't any chairs " +
                "(Hech qanaqa stul yo'q)."),
            new CuratedGrammarRule(
                "So'roq va qisqa javob",
                "Is there a bank here? - Yes, there is. / No, there isn't. Are there any questions? - " +
                "Yes, there are. / No, there aren't."),
            new CuratedGrammarRule(
                "«there» va «it» farqi",
                "«There is» mavjudlikni e'lon qiladi; «it is» esa allaqachon ma'lum narsani ta'riflaydi: " +
                "There is a car. It is red."),
        },
        new[]
        {
            new CuratedGrammarExample("There is a problem.", "Bir muammo bor."),
            new CuratedGrammarExample("There are many people in the park.", "Bog'da ko'p odam bor."),
            new CuratedGrammarExample("There isn't any bread.", "Hech qanaqa non yo'q."),
            new CuratedGrammarExample("Are there any questions?", "Savollar bormi?"),
            new CuratedGrammarExample("There is some water in the bottle.", "Shishada bir oz suv bor."),
        },
        new[]
        {
            "Otning soniga qarang: «There is two books» emas, «There are two books».",
            "«It is» bilan adashtirmang - mavjudlik uchun «There is» kerak.",
            "Sanalmaydigan ot birlik hisoblanadi: «There are water» emas, «There is water».",
        });

    // ───────────────────────────── A1 - Possessive Adjectives ─────────────────────────────
    private static readonly CuratedGrammarLesson PossessiveAdjectives = new(
        "possessive-adjectives",
        "Egalik sifatlari - my, your, his, her...",
        "Egalik sifatlari biror narsa kimga tegishli ekanini ko'rsatadi va otdan oldin keladi. " +
        "O'zbekchadagi egalik qo'shimchalariga («-im, -ing, -i») to'g'ri keladi: my book - kitobim.",
        new[]
        {
            "I → my,  you → your,  he → his,  she → her",
            "it → its,  we → our,  they → their",
            "Tuzilishi: egalik sifati + ot  →  my name, your house, their car",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Har bir shaxsning shakli",
                "my (mening), your (sening/sizning), his (uning - erkak), her (uning - ayol), " +
                "its (uning - narsa/hayvon), our (bizning), their (ularning)."),
            new CuratedGrammarRule(
                "Doim otdan oldin",
                "Egalik sifati yolg'iz ishlatilmaydi, undan keyin ot keladi: my car, her idea. " +
                "Ot soni o'zgarmaydi: my books (ko'plik bo'lsa ham «my» o'zgarmaydi)."),
            new CuratedGrammarRule(
                "his / her tanlovi",
                "Egasining jinsiga qarab tanlanadi, narsaga emas: Tom loves his sister; Anna loves her brother."),
            new CuratedGrammarRule(
                "its va it's farqi",
                "its - egalik (uning): The dog wags its tail. it's - «it is» qisqartmasi: It's cold. " +
                "Bularni adashtirmang."),
        },
        new[]
        {
            new CuratedGrammarExample("My name is Aziz.", "Mening ismim Aziz."),
            new CuratedGrammarExample("This is her bag.", "Bu uning sumkasi."),
            new CuratedGrammarExample("They love their country.", "Ular o'z mamlakatini sevadi."),
            new CuratedGrammarExample("The cat is licking its paw.", "Mushuk panjasini yalayapti."),
            new CuratedGrammarExample("What is your phone number?", "Telefon raqamingiz nechchi?"),
        },
        new[]
        {
            "Egalik sifatini jinsga qarab tanlang: ayol uchun «his sister» emas, «her sister».",
            "its (egalik) va it's (it is) ni adashtirmang.",
            "Egalik sifatidan keyin ot kerak: yolg'iz «This is my» emas, «This is my book».",
        });

    // ───────────────────────────── A1 - Prepositions of Place ─────────────────────────────
    private static readonly CuratedGrammarLesson PrepositionsOfPlace = new(
        "prepositions-of-place",
        "O'rin predloglari - in, on, under, next to...",
        "O'rin predloglari narsaning qayerda joylashganini bildiradi. O'zbekchadagi «-da, ustida, " +
        "ostida, yonida» kabi so'zlarga to'g'ri keladi.",
        new[]
        {
            "in - ichida  →  in the box, in the room",
            "on - ustida (tegib turadi)  →  on the table, on the wall",
            "under - ostida  →  under the bed",
            "next to / near - yonida  →  next to the door, near the school",
            "between - orasida  →  between the chairs",
        },
        new[]
        {
            new CuratedGrammarRule(
                "in, on, at - uchta asosiy",
                "in - yopiq joy yoki ichida (in the kitchen). on - yuza ustida (on the floor). " +
                "at - aniq nuqta (at the bus stop, at home)."),
            new CuratedGrammarRule(
                "Joylashuvni aniqlash",
                "under (ostida), above / over (tepasida), behind (orqasida), in front of (oldida), " +
                "next to / beside (yonida), between (ikki narsa orasida)."),
            new CuratedGrammarRule(
                "Maxsus iboralar",
                "Ba'zi iboralar yodlanadi: at home, at work, at school, in bed, in hospital, on the left, " +
                "on the right."),
        },
        new[]
        {
            new CuratedGrammarExample("The keys are on the table.", "Kalitlar stol ustida."),
            new CuratedGrammarExample("The cat is under the chair.", "Mushuk stul ostida."),
            new CuratedGrammarExample("She is at home.", "U uyda."),
            new CuratedGrammarExample("The bank is next to the post office.", "Bank pochta yonida."),
            new CuratedGrammarExample("The shop is between the cafe and the bank.", "Do'kon kafe bilan bank orasida."),
        },
        new[]
        {
            "«in» va «on» ni adashtirmang: «on the room» emas, «in the room».",
            "Uy uchun «in home» emas, «at home» ishlatiladi.",
            "«under» (ostida) va «over» (tepasida) ni almashtirmang.",
        });

    // ───────────────────────────── A1 - Can (ability) ─────────────────────────────
    private static readonly CuratedGrammarLesson CanAbility = new(
        "can-ability",
        "«Can» - qobiliyat va imkoniyat",
        "«Can» modal fe'li biror ishni qila olish qobiliyatini yoki imkoniyatini bildiradi. " +
        "O'zbekchadagi «-a olaman/-a oladi» shakliga to'g'ri keladi: I can swim - suza olaman.",
        new[]
        {
            "Tasdiq: ega + can + asosiy fe'l  →  I can swim",
            "Inkor: ega + can't (cannot) + asosiy fe'l  →  She can't drive",
            "So'roq: Can + ega + asosiy fe'l?  →  Can you help me?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Shakli o'zgarmaydi",
                "«can» barcha shaxslar uchun bir xil - -s qo'shilmaydi: «He cans» emas, «He can». " +
                "Undan keyin fe'l «to» siz keladi: «can to swim» emas, «can swim»."),
            new CuratedGrammarRule(
                "Ma'nolari",
                "1) Qobiliyat: I can speak three languages. 2) Imkoniyat: We can meet tomorrow. " +
                "3) Ruxsat so'rash: Can I open the window?"),
            new CuratedGrammarRule(
                "Inkor shakli",
                "cannot = can't. Masalan: I can't come today (Bugun kela olmayman)."),
            new CuratedGrammarRule(
                "Qisqa javoblar",
                "Can you swim? - Yes, I can. / No, I can't."),
        },
        new[]
        {
            new CuratedGrammarExample("I can play the guitar.", "Men gitara chala olaman."),
            new CuratedGrammarExample("She can't come to the party.", "U bazmga kela olmaydi."),
            new CuratedGrammarExample("Can you speak English?", "Ingliz tilida gapira olasizmi?"),
            new CuratedGrammarExample("Birds can fly.", "Qushlar ucha oladi."),
            new CuratedGrammarExample("Can I sit here?", "Bu yerga o'tirsam bo'ladimi?"),
        },
        new[]
        {
            "«can» dan keyin «to» kerak emas: «I can to go» emas, «I can go».",
            "Uchinchi shaxsda -s qo'shmang: «He can swims» emas, «He can swim».",
            "Inkor uchun «don't can» emas, «can't» ishlatiladi.",
        });

    // ───────────────────────────── A1 - Present Continuous ─────────────────────────────
    private static readonly CuratedGrammarLesson PresentContinuous = new(
        "present-continuous",
        "Present Continuous - hozir davom etayotgan zamon (am/is/are + V-ing)",
        "Present Continuous aynan hozir, gapirayotgan paytda davom etayotgan ish-harakatni bildiradi. " +
        "O'zbekchadagi «-yapti» shakliga to'g'ri keladi: She is reading - u o'qiyapti.",
        new[]
        {
            "am / is / are + fe'l-ing  →  I am working, She is reading",
            "Inkor: am/is/are + not + V-ing  →  They aren't sleeping",
            "So'roq: Am/Is/Are + ega + V-ing?  →  Are you listening?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Tuzilishi",
                "«to be» (am/is/are) + asosiy fe'l + -ing. I am, he/she/it is, you/we/they are."),
            new CuratedGrammarRule(
                "-ing qo'shish qoidalari",
                "Ko'pchilik fe'l: + ing (play → playing). «-e» bilan tugasa «-e» tushadi (make → making). " +
                "Qisqa fe'lda oxirgi undosh ikkilanadi (run → running, sit → sitting)."),
            new CuratedGrammarRule(
                "Qachon ishlatiladi",
                "1) Aynan hozir: I am writing now. 2) Vaqtinchalik holat: She is staying with us this week. " +
                "3) Yaqin reja: We are meeting tomorrow."),
            new CuratedGrammarRule(
                "Present Simple bilan farqi",
                "Continuous → aynan hozir / vaqtinchalik (I am cooking now). Simple → odat / doimiy " +
                "(I cook every day). Holat fe'llari (know, like, want) odatda Continuous da kelmaydi."),
        },
        new[]
        {
            new CuratedGrammarExample("I am studying English now.", "Men hozir ingliz tilini o'rganyapman."),
            new CuratedGrammarExample("She is cooking dinner.", "U kechki ovqat tayyorlayapti."),
            new CuratedGrammarExample("They aren't working today.", "Ular bugun ishlamayapti."),
            new CuratedGrammarExample("Are you listening to me?", "Meni eshityapsanmi?"),
            new CuratedGrammarExample("It is raining outside.", "Tashqarida yomg'ir yog'yapti."),
        },
        new[]
        {
            "«to be» fe'lini tushirmang: «I working» emas, «I am working».",
            "Holat fe'llarini odatda -ing da ishlatmang: «I am knowing» emas, «I know».",
            "Odat uchun Continuous emas, Simple kerak: «I am going to school every day» emas, «I go».",
        });

    // ───────────────────────────── A1 - Wh- Questions ─────────────────────────────
    private static readonly CuratedGrammarLesson WhQuestions = new(
        "wh-questions",
        "Wh- so'roqlar - what, where, who, when, why, how",
        "Wh- so'roqlar «ha/yo'q» emas, aniq ma'lumot so'raydi. O'zbekchadagi «nima, qayerda, kim, qachon, " +
        "nega, qanday» so'roqlariga to'g'ri keladi.",
        new[]
        {
            "Wh- so'z + do/does + ega + fe'l?  →  Where do you live?",
            "Wh- so'z + am/is/are + ega?  →  What is your name?",
            "Wh- so'z + can/will + ega + fe'l?  →  When can you come?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Asosiy so'roq so'zlari",
                "what (nima), where (qayerda), who (kim), when (qachon), why (nega), how (qanday). " +
                "Qo'shimcha: which (qaysi), whose (kimning), how much / how many (qancha / nechta)."),
            new CuratedGrammarRule(
                "Tuzilishi",
                "Wh- so'zi gap boshida turadi, keyin so'roq tartibi keladi: yordamchi fe'l (do/does/is/are/can) " +
                "+ ega + asosiy fe'l. Masalan: Why does she cry?"),
            new CuratedGrammarRule(
                "«who» ega bo'lganda",
                "who yoki what ega bo'lsa, do/does kerak emas: Who lives here? What happened?"),
            new CuratedGrammarRule(
                "Javoblar to'liq ma'lumot beradi",
                "Where do you work? - I work in a bank. How old are you? - I am twenty."),
        },
        new[]
        {
            new CuratedGrammarExample("What is your name?", "Ismingiz nima?"),
            new CuratedGrammarExample("Where do you live?", "Qayerda yashaysiz?"),
            new CuratedGrammarExample("Why are you late?", "Nega kechikdingiz?"),
            new CuratedGrammarExample("How do you go to work?", "Ishga qanday borasiz?"),
            new CuratedGrammarExample("Who is that man?", "U odam kim?"),
        },
        new[]
        {
            "So'roq tartibini buzmang: «Where you live?» emas, «Where do you live?».",
            "who/what ega bo'lsa do/does qo'shmang: «Who does live here?» emas, «Who lives here?».",
            "«How many» (sanaladigan) va «How much» (sanalmaydigan) ni almashtirmang.",
        });

    // ───────────────────────────── A2 - Comparatives ─────────────────────────────
    private static readonly CuratedGrammarLesson Comparatives = new(
        "comparatives",
        "Qiyosiy daraja - bigger, faster, more beautiful",
        "Qiyosiy daraja ikki narsani solishtirish uchun ishlatiladi. O'zbekchadagi «-roq» qo'shimchasiga " +
        "to'g'ri keladi: kattaroq, tezroq. Ko'pincha «than» (dan) so'zi bilan keladi.",
        new[]
        {
            "Qisqa sifat: + -er  →  big → bigger, fast → faster",
            "Undosh + «-y»: «-y» → «-ier»  →  happy → happier, easy → easier",
            "Uzun sifat: more + sifat  →  more expensive, more beautiful",
            "Solishtirish: ... + than  →  Tashkent is bigger than Samarkand.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Qisqa va uzun sifatlar",
                "Bir bo'g'inli sifatga -er qo'shiladi (small → smaller). Ikki va undan ko'p bo'g'inli " +
                "sifatlar oldiga «more» qo'yiladi (more careful)."),
            new CuratedGrammarRule(
                "Imlo o'zgarishlari",
                "Qisqa unli + undosh bilan tugasa undosh ikkilanadi: hot → hotter, big → bigger. " +
                "Undosh + y: happy → happier."),
            new CuratedGrammarRule(
                "Noto'g'ri shakllar",
                "Ba'zilari yodlanadi: good → better, bad → worse, far → further/farther."),
            new CuratedGrammarRule(
                "«than» ishlatish",
                "Ikki narsa solishtirilganda «than» keladi: She is taller than me. " +
                "Tenglik uchun «as ... as»: He is as tall as his brother."),
        },
        new[]
        {
            new CuratedGrammarExample("This car is faster than that one.", "Bu mashina anavidan tezroq."),
            new CuratedGrammarExample("Winter is colder than autumn.", "Qish kuzdan sovuqroq."),
            new CuratedGrammarExample("This book is more interesting.", "Bu kitob qiziqroq."),
            new CuratedGrammarExample("My grades are better this year.", "Bu yil baholarim yaxshiroq."),
            new CuratedGrammarExample("She is as tall as her sister.", "U singlisi bilan bir bo'yda."),
        },
        new[]
        {
            "-er va more ni birga ishlatmang: «more bigger» emas, «bigger».",
            "Solishtirishda «then» emas, «than» ishlatiladi (imlo).",
            "Noto'g'ri shakllarni eslang: «gooder» emas, «better».",
        });

    // ───────────────────────────── A2 - Superlatives ─────────────────────────────
    private static readonly CuratedGrammarLesson Superlatives = new(
        "superlatives",
        "Orttirma daraja - the biggest, the most beautiful",
        "Orttirma daraja uch yoki undan ko'p narsa ichidan eng yuqorisini bildiradi. O'zbekchadagi «eng» " +
        "so'ziga to'g'ri keladi: eng katta, eng chiroyli. Doim «the» bilan keladi.",
        new[]
        {
            "Qisqa sifat: the + sifat + -est  →  the biggest, the fastest",
            "Undosh + «-y»: the + ...-iest  →  the happiest, the easiest",
            "Uzun sifat: the most + sifat  →  the most expensive",
        },
        new[]
        {
            new CuratedGrammarRule(
                "«the» majburiy",
                "Orttirma daraja doim «the» bilan keladi, chunki u yagona, eng yuqori narsani bildiradi: " +
                "the tallest building."),
            new CuratedGrammarRule(
                "Qisqa va uzun sifatlar",
                "Bir bo'g'inli: -est (small → the smallest). Ikki va undan ko'p bo'g'inli: the most " +
                "(the most popular)."),
            new CuratedGrammarRule(
                "Noto'g'ri shakllar",
                "good → the best, bad → the worst, far → the furthest/farthest."),
            new CuratedGrammarRule(
                "«in» yoki «of»",
                "Joy yoki guruh bilan: the best student in the class, the tallest of all."),
        },
        new[]
        {
            new CuratedGrammarExample("Everest is the highest mountain.", "Everest eng baland tog'."),
            new CuratedGrammarExample("This is the best day of my life.", "Bu hayotimdagi eng zo'r kun."),
            new CuratedGrammarExample("She is the most talented singer.", "U eng iste'dodli xonanda."),
            new CuratedGrammarExample("It was the worst film I have seen.", "Bu men ko'rgan eng yomon film edi."),
            new CuratedGrammarExample("He is the youngest in the family.", "U oilada eng kichigi."),
        },
        new[]
        {
            "«the» ni tushirmang: «biggest city» emas, «the biggest city».",
            "-est va most ni birga ishlatmang: «the most biggest» emas, «the biggest».",
            "Noto'g'ri shakllarni eslang: «the baddest» emas, «the worst».",
        });

    // ───────────────────────────── A2 - Going to (future) ─────────────────────────────
    private static readonly CuratedGrammarLesson GoingToFuture = new(
        "going-to-future",
        "«Be going to» - kelajak rejasi va bashorat",
        "«Be going to» oldindan rejalashtirilgan ishlarni va hozirgi dalilga asoslangan bashoratlarni " +
        "bildiradi. O'zbekchada «-moqchi» yoki «-adi» orqali ifodalanadi: I am going to study - o'qimoqchiman.",
        new[]
        {
            "am / is / are + going to + asosiy fe'l  →  I am going to travel",
            "Inkor: am/is/are + not + going to  →  She isn't going to come",
            "So'roq: Am/Is/Are + ega + going to...?  →  Are you going to help?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Rejalashtirilgan niyat",
                "Oldindan qaror qilingan reja: We are going to buy a house next year " +
                "(Kelasi yili uy sotib olmoqchimiz)."),
            new CuratedGrammarRule(
                "Dalilga asoslangan bashorat",
                "Hozirgi belgiga qarab kelajakni aytish: Look at those clouds - it's going to rain " +
                "(Anavi bulutlarga qara - yomg'ir yog'adi)."),
            new CuratedGrammarRule(
                "«will» bilan farqi",
                "going to → oldindan o'ylangan reja yoki ko'rinib turgan dalil. will → gapirayotgan paytda " +
                "qaror yoki umumiy taxmin: OK, I'll help you."),
        },
        new[]
        {
            new CuratedGrammarExample("I am going to visit my uncle.", "Men amakimnikiga bormoqchiman."),
            new CuratedGrammarExample("They are going to get married.", "Ular turmush qurmoqchi."),
            new CuratedGrammarExample("It's going to rain.", "Yomg'ir yog'adi."),
            new CuratedGrammarExample("She isn't going to study tonight.", "U bugun kechqurun o'qimaydi."),
            new CuratedGrammarExample("Are you going to call him?", "Unga qo'ng'iroq qilmoqchimisiz?"),
        },
        new[]
        {
            "«going to» dan keyin asosiy fe'l keladi: «going to studying» emas, «going to study».",
            "«to be» ni tushirmang: «I going to go» emas, «I am going to go».",
            "Reja uchun «will» emas, «going to» tabiiyroq: aniq reja bo'lsa «I'm going to...».",
        });

    // ───────────────────────────── A2 - Adverbs of Frequency ─────────────────────────────
    private static readonly CuratedGrammarLesson AdverbsOfFrequency = new(
        "adverbs-of-frequency",
        "Takror ravishlari - always, usually, often, sometimes, never",
        "Takror ravishlari ish-harakat qanchalik tez-tez bo'lishini bildiradi. O'zbekchadagi «doim, odatda, " +
        "ko'pincha, ba'zan, hech qachon» so'zlariga to'g'ri keladi.",
        new[]
        {
            "always (doim) > usually (odatda) > often (ko'pincha) > sometimes (ba'zan) > never (hech qachon)",
            "Oddiy fe'ldan oldin  →  I always drink tea.",
            "«to be» fe'lidan keyin  →  She is never late.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Joylashuvi",
                "Takror ravishi asosiy fe'ldan oldin keladi: He often plays football. Ammo «to be» " +
                "(am/is/are) dan keyin keladi: They are usually busy."),
            new CuratedGrammarRule(
                "Yordamchi fe'l bilan",
                "Yordamchi fe'l bo'lsa, ravish uning orasiga tushadi: I have never been to Paris; " +
                "She doesn't usually eat meat."),
            new CuratedGrammarRule(
                "Tez-tezlikni so'rash",
                "«How often...?» bilan so'raladi: How often do you exercise? - Twice a week."),
            new CuratedGrammarRule(
                "«never» - inkor ma'no",
                "«never» o'zi inkorni bildiradi, shuning uchun yana «not» qo'shilmaydi: " +
                "«I don't never» emas, «I never»."),
        },
        new[]
        {
            new CuratedGrammarExample("I always wake up early.", "Men doim erta turaman."),
            new CuratedGrammarExample("She is usually friendly.", "U odatda samimiy."),
            new CuratedGrammarExample("We sometimes go to the cinema.", "Biz ba'zan kinoga boramiz."),
            new CuratedGrammarExample("He never eats breakfast.", "U hech qachon nonushta qilmaydi."),
            new CuratedGrammarExample("How often do you read?", "Qanchalik tez-tez kitob o'qiysiz?"),
        },
        new[]
        {
            "Ravishni oddiy fe'ldan oldin qo'ying: «I drink always tea» emas, «I always drink tea».",
            "«to be» dan keyin qo'ying: «She never is late» emas, «She is never late».",
            "«never» bilan ikki marta inkor qilmang: «I don't never» emas, «I never».",
        });

    // ───────────────────────────── A2 - Countable / Uncountable ─────────────────────────────
    private static readonly CuratedGrammarLesson CountableUncountable = new(
        "countable-uncountable",
        "Sanaladigan va sanalmaydigan otlar",
        "Sanaladigan otlar (countable) bittalab sanaladi va ko'plikka ega (one apple, two apples). " +
        "Sanalmaydigan otlar (uncountable) sanalmaydi va ko'plikka ega emas (water, money, information).",
        new[]
        {
            "Sanaladigan: a/an + birlik, son + ko'plik  →  a book, three books",
            "Sanalmaydigan: artiklsiz yoki some  →  some water, information",
            "some - tasdiqda,  any - inkor/so'roqda  →  some milk / any milk?",
            "much - sanalmaydigan,  many - sanaladigan  →  much time / many people",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Sanalmaydigan otlar birlik",
                "Ular doim birlik fe'l oladi va «a/an» bilan kelmaydi: «an information» emas, " +
                "«some information» yoki «a piece of information»."),
            new CuratedGrammarRule(
                "some va any",
                "some - tasdiq gaplarda (I have some money) va muloyim taklif/so'rovda (Would you like " +
                "some tea?). any - inkor va so'roqlarda (I don't have any money; Is there any sugar?)."),
            new CuratedGrammarRule(
                "much, many, a lot of",
                "many + sanaladigan ko'plik (many friends). much + sanalmaydigan (much water). " +
                "a lot of - ikkalasi bilan ham (a lot of friends, a lot of water)."),
            new CuratedGrammarRule(
                "O'lchov birliklari",
                "Sanalmaydiganni «sanash» uchun birlik qo'shiladi: a glass of water, a piece of advice, " +
                "two cups of coffee."),
        },
        new[]
        {
            new CuratedGrammarExample("I need some water.", "Menga bir oz suv kerak."),
            new CuratedGrammarExample("There aren't any apples.", "Hech qanaqa olma yo'q."),
            new CuratedGrammarExample("How much money do you have?", "Qancha puling bor?"),
            new CuratedGrammarExample("There are many cars on the road.", "Yo'lda ko'p mashina bor."),
            new CuratedGrammarExample("Can I have a cup of tea?", "Bir piyola choy bersangiz bo'ladimi?"),
        },
        new[]
        {
            "Sanalmaydigan otga «a/an» qo'shmang: «a money» emas, «some money».",
            "much/many ni almashtirmang: «much books» emas, «many books».",
            "Sanalmaydigan ot birlik: «informations» emas, «information».",
        });

    // ───────────────────────────── A2 - Prepositions of Time ─────────────────────────────
    private static readonly CuratedGrammarLesson PrepositionsOfTime = new(
        "prepositions-of-time",
        "Vaqt predloglari - at, on, in",
        "Vaqt predloglari ish-harakat qachon bo'lishini bildiradi. Uchta asosiysi: at (aniq vaqt), " +
        "on (kun va sana), in (oy, yil, fasl, qism). Bularning ishlatilishi yodlanadi.",
        new[]
        {
            "at - aniq vaqt, payt  →  at 7 o'clock, at night, at the weekend",
            "on - kun va sana  →  on Monday, on July 5th, on my birthday",
            "in - oy, yil, fasl, kun qismi  →  in May, in 2024, in summer, in the morning",
        },
        new[]
        {
            new CuratedGrammarRule(
                "at - eng aniq",
                "Soat va aniq paytlar bilan: at 6 p.m., at noon, at midnight, at lunchtime. " +
                "Maxsus iboralar: at night, at the weekend (Britaniya inglizchasi)."),
            new CuratedGrammarRule(
                "on - kunlar",
                "Hafta kunlari va sanalar bilan: on Friday, on 1st January, on New Year's Day, " +
                "on Monday morning."),
            new CuratedGrammarRule(
                "in - kattaroq davrlar",
                "Oylar, yillar, fasllar, asrlar va kun qismlari bilan: in April, in 1999, in winter, " +
                "in the evening."),
            new CuratedGrammarRule(
                "Predlogsiz holatlar",
                "today, tomorrow, yesterday, this week, next year, every day so'zlari oldidan predlog " +
                "qo'yilmaydi: «in tomorrow» emas, «tomorrow»."),
        },
        new[]
        {
            new CuratedGrammarExample("The meeting is at 3 o'clock.", "Yig'ilish soat 3 da."),
            new CuratedGrammarExample("I was born on Monday.", "Men dushanba kuni tug'ilganman."),
            new CuratedGrammarExample("We go on holiday in July.", "Biz iyulda ta'tilga chiqamiz."),
            new CuratedGrammarExample("She gets up early in the morning.", "U ertalab erta turadi."),
            new CuratedGrammarExample("I'll see you tomorrow.", "Ertaga ko'rishamiz."),
        },
        new[]
        {
            "Soat bilan «in» emas, «at» ishlatiladi: «in 5 o'clock» emas, «at 5 o'clock».",
            "Kunlar bilan «in» emas, «on»: «in Monday» emas, «on Monday».",
            "tomorrow/yesterday oldidan predlog qo'ymang: «on tomorrow» emas, «tomorrow».",
        });

    // ───────────────────────────── A2 - Object Pronouns ─────────────────────────────
    private static readonly CuratedGrammarLesson ObjectPronouns = new(
        "object-pronouns",
        "To'ldiruvchi olmoshlar - me, you, him, her, it, us, them",
        "To'ldiruvchi olmoshlar fe'l yoki predlogning to'ldiruvchisi (ob'ekti) bo'lib keladi - ya'ni " +
        "harakat kimga yoki nimaga qaratilganini bildiradi. O'zbekchadagi «meni, uni, bizni» kabi shakllar.",
        new[]
        {
            "I → me,  you → you,  he → him,  she → her",
            "it → it,  we → us,  they → them",
            "Fe'ldan keyin  →  She called me.   Predlogdan keyin  →  Listen to him.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Ega va to'ldiruvchi farqi",
                "Ega (kim qiladi) gap boshida: I see her. To'ldiruvchi (kimga qaratilgan) fe'ldan keyin: " +
                "She sees me. «I» va «me» ni adashtirmang."),
            new CuratedGrammarRule(
                "Predloglardan keyin",
                "to, for, with, about kabi predloglardan keyin to'ldiruvchi shakl keladi: with us, for them, " +
                "about him."),
            new CuratedGrammarRule(
                "Ikki to'ldiruvchi bo'lganda",
                "Ba'zi fe'llar ikki to'ldiruvchi oladi: He gave me a book (menga - kishi, kitobni - narsa)."),
        },
        new[]
        {
            new CuratedGrammarExample("Can you help me?", "Menga yordam bera olasizmi?"),
            new CuratedGrammarExample("I saw him yesterday.", "Men uni kecha ko'rdim."),
            new CuratedGrammarExample("Give it to her.", "Buni unga bering."),
            new CuratedGrammarExample("They invited us to the party.", "Ular bizni bazmga taklif qilishdi."),
            new CuratedGrammarExample("I'm waiting for them.", "Men ularni kutyapman."),
        },
        new[]
        {
            "Fe'ldan keyin ega shaklini ishlatmang: «She called I» emas, «She called me».",
            "Predlogdan keyin to'ldiruvchi shakl kerak: «for I» emas, «for me».",
            "«him» (erkak) va «her» (ayol) ni jinsiga qarab tanlang.",
        });

    // ───────────────────────────── A2 - Possessive Pronouns ─────────────────────────────
    private static readonly CuratedGrammarLesson PossessivePronouns = new(
        "possessive-pronouns",
        "Egalik olmoshlari - mine, yours, his, hers, ours, theirs",
        "Egalik olmoshlari biror narsa kimga tegishli ekanini bildiradi, lekin egalik sifatlaridan farqli " +
        "o'laroq, ulardan keyin ot kelmaydi - ular otning o'rnini bosadi.",
        new[]
        {
            "my book → mine,  your book → yours",
            "his book → his,  her book → hers",
            "our book → ours,  their book → theirs",
            "Ulardan keyin ot KELMAYDI  →  This book is mine.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Sifat va olmosh farqi",
                "Egalik sifati otdan oldin: This is my car. Egalik olmoshi otsiz, otning o'rniga: " +
                "This car is mine. Takrorni oldini oladi."),
            new CuratedGrammarRule(
                "Shakllar",
                "mine, yours, his, hers, ours, theirs. Diqqat: «its» egalik olmoshi sifatida " +
                "deyarli ishlatilmaydi."),
            new CuratedGrammarRule(
                "Apostrof yo'q",
                "Egalik olmoshlarida apostrof ishlatilmaydi: «your's» emas, «yours»; «her's» emas, «hers»."),
            new CuratedGrammarRule(
                "«of» bilan",
                "«a friend of mine» (mening do'stlarimdan biri) kabi iboralarda ham ishlatiladi."),
        },
        new[]
        {
            new CuratedGrammarExample("This pen is mine.", "Bu ruchka meniki."),
            new CuratedGrammarExample("Is this bag yours?", "Bu sumka sizniki(mi)?"),
            new CuratedGrammarExample("The blue car is hers.", "Ko'k mashina uniki (ayol)."),
            new CuratedGrammarExample("Our house is bigger than theirs.", "Bizning uyimiz ularnikidan katta."),
            new CuratedGrammarExample("He's a friend of mine.", "U mening do'stim."),
        },
        new[]
        {
            "Egalik olmoshidan keyin ot qo'ymang: «mine book» emas, «my book» yoki «mine».",
            "Apostrof ishlatmang: «her's» emas, «hers».",
            "«your» (sifat) va «yours» (olmosh) ni adashtirmang.",
        });

    // ───────────────────────────── A2 - Imperatives ─────────────────────────────
    private static readonly CuratedGrammarLesson Imperatives = new(
        "imperatives",
        "Buyruq gaplar - imperative",
        "Buyruq gaplar buyruq, ko'rsatma, taklif yoki ogohlantirish berish uchun ishlatiladi. " +
        "O'zbekchadagi buyruq mayliga to'g'ri keladi: Yop! Kel! Tinglang!",
        new[]
        {
            "Tasdiq: fe'lning asosiy shakli (egasiz)  →  Sit down. Open the window.",
            "Inkor: Don't + asosiy fe'l  →  Don't be late. Don't worry.",
            "Muloyim: Please + buyruq  →  Please wait here.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Ega yo'q",
                "Buyruq gapda ega aytilmaydi - u har doim «you» (tinglovchi) ga qaratilgan: " +
                "Come here. (Bu yerga kel.)"),
            new CuratedGrammarRule(
                "Inkor shakli",
                "«Don't» + fe'l bilan taqiq yoki ogohlantirish: Don't touch that! " +
                "(Unga tegma!)"),
            new CuratedGrammarRule(
                "Muloyimlik",
                "«please» qo'shilsa muloyimroq bo'ladi: Please sit down. «Let's» esa birga ish " +
                "qilishni taklif qiladi: Let's go (Yuringlar / Ketdik)."),
            new CuratedGrammarRule(
                "Qayerda ishlatiladi",
                "Ko'rsatmalar (Turn left), retseptlar (Add salt), ogohlantirishlar (Be careful), " +
                "taklif va iltimoslar."),
        },
        new[]
        {
            new CuratedGrammarExample("Close the door, please.", "Iltimos, eshikni yoping."),
            new CuratedGrammarExample("Don't be afraid.", "Qo'rqma."),
            new CuratedGrammarExample("Turn right at the corner.", "Burchakda o'ngga buriling."),
            new CuratedGrammarExample("Let's have lunch together.", "Keling, birga tushlik qilaylik."),
            new CuratedGrammarExample("Be careful!", "Ehtiyot bo'l!"),
        },
        new[]
        {
            "Buyruqda ega qo'shmang: «You sit down» (buyruq sifatida) emas, «Sit down».",
            "Inkor uchun «Not open» emas, «Don't open» ishlatiladi.",
            "«Let's» dan keyin asosiy fe'l keladi: «Let's to go» emas, «Let's go».",
        });

    // ───────────────────────────── B1 - Past Continuous ─────────────────────────────
    private static readonly CuratedGrammarLesson PastContinuous = new(
        "past-continuous",
        "Past Continuous - o'tgan davom zamon (was/were + V-ing)",
        "Past Continuous o'tmishning ma'lum bir paytida davom etayotgan ish-harakatni bildiradi. Ko'pincha " +
        "uni Past Simple bilan birga ishlatib, bir harakat davom etayotganda boshqasi sodir bo'lganini ko'rsatadi.",
        new[]
        {
            "was / were + fe'l-ing  →  I was sleeping, They were working",
            "Inkor: was/were + not + V-ing  →  She wasn't listening",
            "So'roq: Was/Were + ega + V-ing?  →  Were you waiting?",
            "Uzilish: when + Past Simple  →  I was cooking when he called.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "was yoki were",
                "I / he / she / it bilan «was». You / we / they bilan «were»."),
            new CuratedGrammarRule(
                "Asosiy ma'no",
                "O'tmishning aniq bir paytida jarayon davom etayotgan edi: At 8 p.m. I was watching TV " +
                "(Soat 8 da men televizor ko'rayotgan edim)."),
            new CuratedGrammarRule(
                "when va while bilan",
                "Uzunroq harakat (Continuous) davom etayotganda qisqa harakat (Simple) sodir bo'ladi: " +
                "While I was reading, the phone rang. (when - qisqa harakatni, while - davom etayotganni " +
                "kiritadi.)"),
            new CuratedGrammarRule(
                "Holat fe'llari bilan emas",
                "know, want, like kabi holat fe'llari odatda Continuous da kelmaydi: «I was knowing» emas, " +
                "«I knew»."),
        },
        new[]
        {
            new CuratedGrammarExample("I was reading when you called.", "Sen qo'ng'iroq qilganingda men o'qiyotgan edim."),
            new CuratedGrammarExample("They were playing in the garden.", "Ular bog'da o'ynayotgan edi."),
            new CuratedGrammarExample("What were you doing at noon?", "Tushda nima qilayotgan eding?"),
            new CuratedGrammarExample("She wasn't sleeping at that time.", "U o'sha paytda uxlamayotgan edi."),
            new CuratedGrammarExample("While we were eating, it started to rain.", "Biz ovqatlanayotganimizda yomg'ir boshlandi."),
        },
        new[]
        {
            "was/were ni to'g'ri tanlang: «They was playing» emas, «They were playing».",
            "«been» bu zamonga kerak emas: «I was been working» emas, «I was working».",
            "Holat fe'llarini -ing da ishlatmang: «I was wanting» emas, «I wanted».",
        });

    // ───────────────────────────── B1 - First Conditional ─────────────────────────────
    private static readonly CuratedGrammarLesson FirstConditional = new(
        "first-conditional",
        "Birinchi shart gap - real kelajak sharti",
        "Birinchi shart gap kelajakda yuz berishi mumkin bo'lgan real shart va uning natijasini bildiradi. " +
        "O'zbekchadagi «Agar ... bo'lsa, ... bo'ladi» qolipiga to'g'ri keladi.",
        new[]
        {
            "If + present simple, ... will + asosiy fe'l",
            "    If it rains, we will stay at home.",
            "Tartibni almashtirsa, vergul tushadi: We will stay at home if it rains.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Tuzilishi",
                "Shart qismida (if) hozirgi zamon, natija qismida «will» ishlatiladi. " +
                "Shart real va ehtimoli yuqori bo'lgan kelajakni bildiradi."),
            new CuratedGrammarRule(
                "«if» qismida «will» yo'q",
                "Shart qismida kelajak «will» bilan emas, present simple bilan beriladi: " +
                "«If it will rain» emas, «If it rains»."),
            new CuratedGrammarRule(
                "will o'rniga boshqalar",
                "Natijada «will» o'rniga can, may, must yoki buyruq ham kelishi mumkin: " +
                "If you finish early, you can go home."),
            new CuratedGrammarRule(
                "when va if farqi",
                "if - bo'lishi noaniq (If I see her...). when - albatta bo'ladigan (When I get home, " +
                "I'll call you)."),
        },
        new[]
        {
            new CuratedGrammarExample("If you study, you will pass the exam.", "Agar o'qisang, imtihondan o'tasan."),
            new CuratedGrammarExample("If it rains, we will cancel the trip.", "Agar yomg'ir yog'sa, sayohatni bekor qilamiz."),
            new CuratedGrammarExample("She will be happy if you call her.", "Agar unga qo'ng'iroq qilsang, u xursand bo'ladi."),
            new CuratedGrammarExample("If you don't hurry, you'll miss the bus.", "Shoshmasang, avtobusga kechikasan."),
            new CuratedGrammarExample("If I have time, I'll help you.", "Vaqtim bo'lsa, senga yordam beraman."),
        },
        new[]
        {
            "«if» qismida «will» ishlatmang: «If it will rain» emas, «If it rains».",
            "Vergulni to'g'ri qo'ying: if qismi oldinda bo'lsa vergul, keyin bo'lsa vergul yo'q.",
            "Real kelajak uchun past simple kerak emas: «If you studied» (bu 2-conditional).",
        });

    // ───────────────────────────── B1 - Second Conditional ─────────────────────────────
    private static readonly CuratedGrammarLesson SecondConditional = new(
        "second-conditional",
        "Ikkinchi shart gap - real bo'lmagan hozirgi/kelajak",
        "Ikkinchi shart gap hozir yoki kelajakda real bo'lmagan, xayoliy yoki ehtimoli kam vaziyatni " +
        "bildiradi. O'zbekchada «Agar ... bo'lsa edi, ... bo'lardi» qolipiga to'g'ri keladi.",
        new[]
        {
            "If + past simple, ... would + asosiy fe'l",
            "    If I had a lot of money, I would travel the world.",
            "«be» fe'li uchun barcha shaxslarda «were»: If I were you, I would rest.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Xayoliy ma'no",
                "Shart real emas yoki ehtimoli juda kam: If I won the lottery... (lotereyada yutib " +
                "olmasligim aniq). Past simple bu yerda o'tmishni emas, farazni bildiradi."),
            new CuratedGrammarRule(
                "«were» - barcha shaxslar",
                "Rasmiy va to'g'ri shaklda «was» o'rniga «were»: If he were here, he would help. " +
                "Mashhur ibora: If I were you (Sening o'rningda bo'lsam)."),
            new CuratedGrammarRule(
                "Birinchi shart bilan farqi",
                "1st conditional - real, ehtimol yuqori (If it rains, I'll stay). " +
                "2nd conditional - xayoliy, ehtimol past (If it snowed in summer, I would be surprised)."),
            new CuratedGrammarRule(
                "Maslahat berish",
                "«If I were you, I would...» maslahat berishning keng tarqalgan usuli."),
        },
        new[]
        {
            new CuratedGrammarExample("If I were rich, I would buy a big house.", "Boy bo'lsam, katta uy sotib olardim."),
            new CuratedGrammarExample("If I were you, I would apologize.", "Sening o'rningda bo'lsam, kechirim so'rardim."),
            new CuratedGrammarExample("What would you do if you lost your job?", "Ishingdan ayrilsang, nima qilarding?"),
            new CuratedGrammarExample("If she knew the truth, she would be upset.", "Haqiqatni bilsa, u xafa bo'lardi."),
            new CuratedGrammarExample("If we had a car, we would drive there.", "Mashinamiz bo'lsa, u yerga haydab borardik."),
        },
        new[]
        {
            "«if» qismida «would» ishlatmang: «If I would have» emas, «If I had».",
            "Xayoliy shart uchun «I am» emas, «I were» tabiiyroq: «If I were you».",
            "Real kelajak bo'lsa 1st conditional kerak: «If it rains tomorrow, I will...».",
        });

    // ───────────────────────────── B1 - Gerunds and Infinitives ─────────────────────────────
    private static readonly CuratedGrammarLesson GerundsAndInfinitives = new(
        "gerunds-and-infinitives",
        "Gerundiy va infinitiv - -ing / to + fe'l",
        "Bir fe'ldan keyin ikkinchi fe'l kelganda u yo gerundiy (-ing), yo infinitiv (to + fe'l) shaklida " +
        "bo'ladi. Qaysi shakl kelishi birinchi fe'lga bog'liq va ko'pincha yodlanadi.",
        new[]
        {
            "Gerundiy (-ing): enjoy, finish, avoid, mind, suggest  →  I enjoy reading.",
            "Infinitiv (to +): want, decide, hope, promise, need  →  I want to go.",
            "Predloglardan keyin doim gerundiy  →  good at swimming, interested in learning",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Gerundiy keladigan fe'llar",
                "enjoy, finish, avoid, mind, suggest, keep, practise, miss kabi fe'llardan keyin -ing: " +
                "She finished writing the report."),
            new CuratedGrammarRule(
                "Infinitiv keladigan fe'llar",
                "want, would like, decide, hope, plan, promise, need, agree, learn kabilardan keyin " +
                "to + fe'l: They decided to leave."),
            new CuratedGrammarRule(
                "Predlog va gap boshida",
                "Barcha predloglardan keyin gerundiy keladi: I'm tired of waiting. Gerundiy gapda ega " +
                "ham bo'la oladi: Swimming is good for you."),
            new CuratedGrammarRule(
                "Ma'no o'zgaradigan fe'llar",
                "Ba'zilarida ma'no farq qiladi: stop smoking (chekishni tashlamoq) ≠ stop to smoke " +
                "(chekish uchun to'xtamoq); remember to lock (eslab qulflamoq) ≠ remember locking " +
                "(qulflaganini eslamoq)."),
        },
        new[]
        {
            new CuratedGrammarExample("I enjoy playing football.", "Men futbol o'ynashni yoqtiraman."),
            new CuratedGrammarExample("She wants to learn Spanish.", "U ispan tilini o'rganmoqchi."),
            new CuratedGrammarExample("He is good at drawing.", "U rasm chizishga usta."),
            new CuratedGrammarExample("They decided to buy a house.", "Ular uy sotib olishga qaror qilishdi."),
            new CuratedGrammarExample("Remember to call your mother.", "Onangga qo'ng'iroq qilishni unutma."),
        },
        new[]
        {
            "Predlogdan keyin infinitiv emas, gerundiy: «good at to swim» emas, «good at swimming».",
            "«enjoy» dan keyin infinitiv qo'ymang: «enjoy to read» emas, «enjoy reading».",
            "«want» dan keyin gerundiy emas, infinitiv: «want going» emas, «want to go».",
        });

    // ───────────────────────────── B1 - Will (future) ─────────────────────────────
    private static readonly CuratedGrammarLesson WillFuture = new(
        "will-future",
        "«Will» - kelajak zamon",
        "«Will» kelajakdagi ish-harakatni bildiradi: gapirayotgan paytda qabul qilingan qaror, bashorat, " +
        "va'da yoki taklif. O'zbekchadagi «-aman/-adi» kelasi zamoniga to'g'ri keladi.",
        new[]
        {
            "Tasdiq: ega + will + asosiy fe'l  →  I will help (qisqasi: I'll help)",
            "Inkor: ega + won't (will not) + fe'l  →  She won't come",
            "So'roq: Will + ega + fe'l?  →  Will you join us?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Shakli o'zgarmaydi",
                "«will» barcha shaxslar uchun bir xil va undan keyin fe'lning asosiy shakli keladi: " +
                "He will go (not «will goes», not «will to go»)."),
            new CuratedGrammarRule(
                "Ma'nolari",
                "1) Tezkor qaror: I'll answer the phone. 2) Bashorat: It will be cold tomorrow. " +
                "3) Va'da: I'll always love you. 4) Taklif: I'll carry that for you."),
            new CuratedGrammarRule(
                "going to bilan farqi",
                "will - gapirayotganda qaror yoki umumiy taxmin. going to - oldindan reja yoki ko'rinib " +
                "turgan dalil. Ko'pincha «I think / probably» bilan will keladi."),
            new CuratedGrammarRule(
                "Qisqartmalar",
                "I'll, you'll, he'll, won't keng ishlatiladi: I'll be there; She won't agree."),
        },
        new[]
        {
            new CuratedGrammarExample("I think it will rain tomorrow.", "Menimcha, ertaga yomg'ir yog'adi."),
            new CuratedGrammarExample("I'll call you later.", "Keyinroq qo'ng'iroq qilaman."),
            new CuratedGrammarExample("She won't be late.", "U kechikmaydi."),
            new CuratedGrammarExample("Will you help me with this?", "Bunda menga yordam berasizmi?"),
            new CuratedGrammarExample("Don't worry, everything will be fine.", "Xavotir olma, hammasi yaxshi bo'ladi."),
        },
        new[]
        {
            "«will» dan keyin «to» kerak emas: «I will to go» emas, «I will go».",
            "Uchinchi shaxsda -s qo'shmang: «He will comes» emas, «He will come».",
            "Inkor uchun «will not» yoki «won't»: «doesn't will» emas.",
        });

    // ───────────────────────────── B1 - Modals of Obligation ─────────────────────────────
    private static readonly CuratedGrammarLesson ModalsOfObligation = new(
        "modals-of-obligation",
        "Majburiyat modallari - must, have to, should, mustn't",
        "Bu modallar majburiyat, zarurat, taqiq yoki maslahatni bildiradi. O'zbekchadagi «kerak, shart, " +
        "majbur» kabi ma'nolarga to'g'ri keladi.",
        new[]
        {
            "must / have to - majburiyat (kerak/shart)  →  You must wear a seatbelt.",
            "mustn't - qat'iy taqiq (mumkin emas)  →  You mustn't smoke here.",
            "don't have to - zarur emas (lekin mumkin)  →  You don't have to come.",
            "should / shouldn't - maslahat (kerak/kerak emas)  →  You should rest.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "must va have to",
                "Ikkalasi ham majburiyat, lekin: must - ko'pincha ichki yoki shaxsiy majburiyat va " +
                "qoidalar (I must finish this). have to - tashqi qoida yoki holat (I have to wear " +
                "a uniform). have to shaxsga moslashadi: he has to."),
            new CuratedGrammarRule(
                "mustn't va don't have to - katta farq",
                "mustn't - taqiq, qilmaslik shart (You mustn't be late = kechikish mumkin emas). " +
                "don't have to - majburiyat yo'q, ixtiyoriy (You don't have to be late = kechikishingiz " +
                "shart emas). Bularni aralashtirmang."),
            new CuratedGrammarRule(
                "should - maslahat",
                "should / shouldn't kuchsizroq - maslahat yoki tavsiya beradi: You should see a doctor."),
            new CuratedGrammarRule(
                "O'tmish shakli",
                "must ning o'tmishi yo'q - «had to» ishlatiladi: I had to work yesterday."),
        },
        new[]
        {
            new CuratedGrammarExample("You must show your passport.", "Pasportingizni ko'rsatishingiz shart."),
            new CuratedGrammarExample("I have to get up early tomorrow.", "Ertaga erta turishim kerak."),
            new CuratedGrammarExample("You mustn't touch the wires.", "Simlarga tegmaslik kerak (taqiq)."),
            new CuratedGrammarExample("You don't have to pay now.", "Hozir to'lashingiz shart emas."),
            new CuratedGrammarExample("You should drink more water.", "Ko'proq suv ichishingiz kerak."),
        },
        new[]
        {
            "mustn't va don't have to ni adashtirmang - biri taqiq, biri ixtiyoriylik.",
            "must dan keyin «to» yo'q: «must to go» emas, «must go».",
            "must ning o'tmishi «musted» emas, «had to».",
        });

    // ───────────────────────────── B1 - Defining Relative Clauses ─────────────────────────────
    private static readonly CuratedGrammarLesson DefiningRelativeClauses = new(
        "defining-relative-clauses",
        "Aniqlovchi ergash gaplar - who, which, that",
        "Aniqlovchi ergash gaplar otni aniqlab, qaysi aniq narsa yoki shaxs haqida gapirilayotganini " +
        "ko'rsatadi. Bu ma'lumot zarur - uni olib tashlasa, gap ma'nosi yo'qoladi.",
        new[]
        {
            "who - odamlar uchun  →  the man who lives next door",
            "which - narsalar uchun  →  the book which I bought",
            "that - odam yoki narsa (ko'pincha which/who o'rniga)  →  the car that broke down",
            "where (joy), whose (egalik), when (vaqt)  →  the city where I was born",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Olmoshlarning tanlovi",
                "who - odamlar, which - narsa/hayvon, that - ikkalasi uchun (so'zlashuvda keng tarqalgan). " +
                "whose - egalik (the girl whose bag was stolen), where - joy, when - vaqt."),
            new CuratedGrammarRule(
                "Vergul yo'q",
                "Aniqlovchi (defining) ergash gapda vergul ishlatilmaydi, chunki ma'lumot zarur: " +
                "The woman who called you is here."),
            new CuratedGrammarRule(
                "Olmoshni tushirish mumkin",
                "Agar olmosh ergash gapning to'ldiruvchisi bo'lsa, uni tushirib qoldirish mumkin: " +
                "the book (that) I read. Lekin ega bo'lsa tushirib bo'lmaydi: the man who lives here."),
            new CuratedGrammarRule(
                "Takror ega yo'q",
                "Ergash gapda otni yana olmosh bilan takrorlamang: «the man who he lives» emas, " +
                "«the man who lives»."),
        },
        new[]
        {
            new CuratedGrammarExample("The doctor who treated me was very kind.", "Meni davolagan shifokor juda mehribon edi."),
            new CuratedGrammarExample("This is the phone that I want.", "Bu men xohlagan telefon."),
            new CuratedGrammarExample("The house where I grew up is gone.", "Men o'sgan uy endi yo'q."),
            new CuratedGrammarExample("She's the girl whose father is a pilot.", "U otasi uchuvchi bo'lgan qiz."),
            new CuratedGrammarExample("The film we watched was boring.", "Biz ko'rgan film zerikarli edi."),
        },
        new[]
        {
            "Odam uchun «which» emas, «who» (yoki «that»): «the man which» emas.",
            "Aniqlovchi gapda vergul qo'ymang.",
            "Otni olmosh bilan takrorlamang: «the book that I bought it» emas, «the book that I bought».",
        });

    // ───────────────────────────── B1 - Used to ─────────────────────────────
    private static readonly CuratedGrammarLesson UsedTo = new(
        "used-to",
        "«Used to» - o'tmishdagi odat yoki holat",
        "«Used to» o'tmishda muntazam bo'lgan, lekin hozir bo'lmaydigan odat yoki holatni bildiradi. " +
        "O'zbekchada «ilgari ... -ardi» yoki «avval ... edi» orqali ifodalanadi.",
        new[]
        {
            "Tasdiq: ega + used to + asosiy fe'l  →  I used to play tennis.",
            "Inkor: ega + didn't use to + fe'l  →  He didn't use to like coffee.",
            "So'roq: Did + ega + use to + fe'l?  →  Did you use to live here?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Asosiy ma'no",
                "O'tmishda takrorlangan odat (I used to smoke) yoki uzoq davom etgan holat " +
                "(We used to live in a village) - endi tugagan."),
            new CuratedGrammarRule(
                "Inkor va so'roqda «use to»",
                "did/didn't bilan «d» tushadi: «didn't used to» emas, «didn't use to»; " +
                "«Did you used to?» emas, «Did you use to?»."),
            new CuratedGrammarRule(
                "Faqat o'tmish uchun",
                "«used to» ning hozirgi shakli yo'q. Hozirgi odat uchun present simple + always/usually: " +
                "I usually drink tea."),
            new CuratedGrammarRule(
                "«be used to» bilan adashtirmang",
                "«used to + fe'l» = o'tmishdagi odat. «be used to + -ing» = biror narsaga ko'nikkan " +
                "(I am used to waking up early). Ma'nolari butunlay boshqacha."),
        },
        new[]
        {
            new CuratedGrammarExample("I used to play football every day.", "Ilgari har kuni futbol o'ynardim."),
            new CuratedGrammarExample("She used to have long hair.", "Avval uning sochi uzun edi."),
            new CuratedGrammarExample("We didn't use to have a car.", "Bizda ilgari mashina yo'q edi."),
            new CuratedGrammarExample("Did you use to live in Tashkent?", "Ilgari Toshkentda yashaganmisan?"),
            new CuratedGrammarExample("There used to be a cinema here.", "Bu yerda ilgari kinoteatr bor edi."),
        },
        new[]
        {
            "Inkor/so'roqda «use to» yozing: «didn't used to» emas, «didn't use to».",
            "Hozirgi odat uchun «use to» yo'q: «I use to go now» emas, «I usually go».",
            "«be used to + -ing» (ko'nikma) bilan aralashtirmang.",
        });

    // ───────────────────────────── B1 - Comparative Adverbs ─────────────────────────────
    private static readonly CuratedGrammarLesson ComparativeAdverbs = new(
        "comparative-adverbs",
        "Qiyosiy ravishlar - more quickly, faster, better",
        "Qiyosiy ravishlar ikki harakatni qanday bajarilishi bo'yicha solishtiradi. Sifatlardagi kabi " +
        "qisqalariga -er, uzunlariga «more» qo'shiladi va ko'pincha «than» bilan keladi.",
        new[]
        {
            "Qisqa ravish (= sifat): + -er  →  fast → faster, hard → harder",
            "«-ly» bilan tugagan ravish: more + ravish  →  quickly → more quickly",
            "Noto'g'ri: well → better, badly → worse, far → further",
            "Solishtirish: ... + than  →  He runs faster than me.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Qisqa ravishlar",
                "fast, hard, late, early kabi sifat shaklidagi ravishlarga -er qo'shiladi: She works harder " +
                "than her brother."),
            new CuratedGrammarRule(
                "«-ly» ravishlari",
                "carefully, slowly, quickly kabi -ly bilan tugaganlar oldiga «more» qo'yiladi: " +
                "Please drive more slowly."),
            new CuratedGrammarRule(
                "Noto'g'ri ravishlar",
                "well → better, badly → worse, far → further/farther. Masalan: She sings better than me."),
            new CuratedGrammarRule(
                "Sifat va ravish farqi",
                "Sifat otni ta'riflaydi (a fast car), ravish fe'lni ta'riflaydi (he drives fast). " +
                "Solishtirishda ham shu farq saqlanadi."),
        },
        new[]
        {
            new CuratedGrammarExample("She runs faster than her brother.", "U akasidan tezroq yuguradi."),
            new CuratedGrammarExample("Please speak more slowly.", "Iltimos, sekinroq gapiring."),
            new CuratedGrammarExample("He plays the piano better than me.", "U mendan yaxshiroq pianino chaladi."),
            new CuratedGrammarExample("I did worse in the second test.", "Ikkinchi testda yomonroq natija ko'rsatdim."),
            new CuratedGrammarExample("Can you explain it more clearly?", "Buni aniqroq tushuntira olasizmi?"),
        },
        new[]
        {
            "-ly ravishiga -er qo'shmang: «quicklier» emas, «more quickly».",
            "«more» va -er ni birga ishlatmang: «more faster» emas, «faster».",
            "Noto'g'ri shakllarni eslang: «more well» emas, «better».",
        });

    // ───────────────────────────── B2 - Past Perfect ─────────────────────────────
    private static readonly CuratedGrammarLesson PastPerfect = new(
        "past-perfect",
        "Past Perfect - o'tmishdan oldingi zamon (had + V3)",
        "Past Perfect o'tmishdagi ikki voqeadan oldinroq bo'lganini bildiradi: bir ish boshqasidan oldin " +
        "tugagan. O'zbekchada «... -gan edi» orqali ifodalanadi.",
        new[]
        {
            "had + past participle (V3)  →  I had finished",
            "Inkor: had + not (hadn't) + V3  →  She hadn't arrived",
            "So'roq: Had + ega + V3?  →  Had you met before?",
            "Ko'pincha Past Simple bilan: ... had + V3 before/when + past simple",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Ikki o'tmish voqeasini tartiblash",
                "Avval bo'lgan voqea Past Perfect da, keyin bo'lgani Past Simple da: When I arrived, " +
                "the train had already left (Men yetib kelganimda, poyezd allaqachon ketgan edi)."),
            new CuratedGrammarRule(
                "Barcha shaxslar uchun «had»",
                "«had» o'zgarmaydi: I/you/he/she/we/they had. Undan keyin fe'lning 3-shakli (V3) keladi."),
            new CuratedGrammarRule(
                "already, just, never, before bilan",
                "Bu so'zlar ko'p uchraydi: She had just left; I had never seen such a thing; " +
                "We had met before."),
            new CuratedGrammarRule(
                "Past Simple bilan farqi",
                "Agar voqealar tartibi aniq bo'lsa (after, before bilan), ba'zan oddiy Past Simple ham " +
                "yetarli. Past Perfect ayniqsa oldinligini ta'kidlash kerak bo'lganda ishlatiladi."),
        },
        new[]
        {
            new CuratedGrammarExample("The film had already started when we arrived.", "Biz yetib kelganimizda film allaqachon boshlangan edi."),
            new CuratedGrammarExample("She had never flown before that trip.", "U o'sha sayohatgacha hech qachon samolyotda uchmagan edi."),
            new CuratedGrammarExample("I realized I had forgotten my keys.", "Kalitlarimni unutganimni angladim."),
            new CuratedGrammarExample("After he had eaten, he went out.", "U ovqatlangach, tashqariga chiqdi."),
            new CuratedGrammarExample("Had you finished before the deadline?", "Muddatdan oldin tugatgan edingmi?"),
        },
        new[]
        {
            "«had» dan keyin 3-shakl kerak: «I had went» emas, «I had gone».",
            "Bitta o'tmish voqeasi uchun Past Perfect shart emas - Past Simple yetarli.",
            "«had» ni barcha shaxslarda bir xil ishlating: «he have» emas, «he had».",
        });

    // ───────────────────────────── B2 - Third Conditional ─────────────────────────────
    private static readonly CuratedGrammarLesson ThirdConditional = new(
        "third-conditional",
        "Uchinchi shart gap - real bo'lmagan o'tmish",
        "Uchinchi shart gap o'tmishda bo'lib o'tmagan, ya'ni endi o'zgartirib bo'lmaydigan xayoliy " +
        "vaziyatni va uning natijasini bildiradi. O'zbekchada «Agar ... bo'lganida, ... bo'lardi» qolipi.",
        new[]
        {
            "If + past perfect, ... would have + V3",
            "    If I had studied, I would have passed the exam.",
            "would o'rniga could / might ham: ... could have / might have + V3",
        },
        new[]
        {
            new CuratedGrammarRule(
                "O'tmishga afsus yoki taxmin",
                "Bo'lib o'tgan (yoki bo'lmagan) ishni xayolan o'zgartiradi: If she had left earlier, she " +
                "wouldn't have missed the bus (Agar ertaroq chiqqanida, avtobusga kechikmagan bo'lardi)."),
            new CuratedGrammarRule(
                "Tuzilishi",
                "Shart qismida past perfect (had + V3), natija qismida would have + V3. " +
                "Ikkala qism ham o'tmishga tegishli."),
            new CuratedGrammarRule(
                "could/might bilan yumshatish",
                "Natija aniq bo'lmasa would o'rniga could have (imkon bo'lardi) yoki might have (ehtimol " +
                "bo'lardi): I might have helped if you had asked."),
            new CuratedGrammarRule(
                "2nd conditional bilan farqi",
                "2nd - hozir/kelajakdagi xayol (If I had money, I would buy...). 3rd - o'tmishdagi xayol " +
                "(If I had had money, I would have bought...)."),
        },
        new[]
        {
            new CuratedGrammarExample("If I had known, I would have told you.", "Bilganimda, senga aytgan bo'lardim."),
            new CuratedGrammarExample("She would have come if you had invited her.", "Agar taklif qilganingda, u kelgan bo'lardi."),
            new CuratedGrammarExample("If we had left earlier, we wouldn't have missed the train.", "Ertaroq chiqqanimizda, poyezdga kechikmagan bo'lardik."),
            new CuratedGrammarExample("They might have won if they had practised more.", "Ko'proq mashq qilganlarida, ehtimol yutgan bo'lardilar."),
            new CuratedGrammarExample("What would you have done in my place?", "Mening o'rnimda nima qilgan bo'larding?"),
        },
        new[]
        {
            "«if» qismida «would have» ishlatmang: «If I would have known» emas, «If I had known».",
            "Natija qismida «had» emas, «would have + V3» kerak.",
            "O'tmish uchun 2nd conditional yetarli emas: «If I had money» (o'tmish emas).",
        });

    // ───────────────────────────── B2 - Passive Voice ─────────────────────────────
    private static readonly CuratedGrammarLesson PassiveVoice = new(
        "passive-voice",
        "Majhul nisbat - passive voice (be + V3)",
        "Majhul nisbat harakatni kim bajargani emas, harakatning o'zi yoki uning natijasiga urg'u beradi. " +
        "Ish-harakat ob'ekti gap egasiga aylanadi. O'zbekchadagi «-ildi/-ldi» (majhul) shakliga o'xshaydi.",
        new[]
        {
            "to be (mos zamonda) + past participle (V3)",
            "Present: is/are + V3  →  English is spoken here.",
            "Past: was/were + V3  →  The house was built in 1990.",
            "Bajaruvchini ko'rsatish: ... by + bajaruvchi  →  written by Tolstoy",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Qachon ishlatiladi",
                "1) Bajaruvchi noma'lum yoki muhim emas: My car was stolen. 2) Bajaruvchi aniq " +
                "(hamma biladi): The criminals were arrested. 3) Rasmiy va ilmiy uslubda."),
            new CuratedGrammarRule(
                "Faol gapni majhulga aylantirish",
                "Faol gapning to'ldiruvchisi majhul gapning egasiga aylanadi: " +
                "They build houses → Houses are built. Fe'l «be + V3» shaklini oladi, zamon saqlanadi."),
            new CuratedGrammarRule(
                "«by» bilan bajaruvchi",
                "Agar bajaruvchini ko'rsatish kerak bo'lsa «by» ishlatiladi: The book was written by " +
                "a famous author. Ko'pincha bu qism tushiriladi."),
            new CuratedGrammarRule(
                "Turli zamonlarda",
                "Modal: can be done. Present perfect: has been done. Future: will be done. " +
                "Continuous: is being done. Asosiy o'zgaruvchi qism - «be» fe'li."),
        },
        new[]
        {
            new CuratedGrammarExample("This bridge was built in 1905.", "Bu ko'prik 1905-yilda qurilgan."),
            new CuratedGrammarExample("English is spoken all over the world.", "Ingliz tilida butun dunyoda gapiriladi."),
            new CuratedGrammarExample("The letter has been sent.", "Xat yuborildi."),
            new CuratedGrammarExample("My phone was stolen yesterday.", "Telefonim kecha o'g'irlandi."),
            new CuratedGrammarExample("The work will be finished tomorrow.", "Ish ertaga tugatiladi."),
        },
        new[]
        {
            "«be» fe'lini tushirmang: «The house built» emas, «The house was built».",
            "«be» dan keyin 3-shakl kerak: «was build» emas, «was built».",
            "Zamonni «be» orqali bering, asosiy fe'l doim V3 da qoladi.",
        });

    // ───────────────────────────── B2 - Reported Speech ─────────────────────────────
    private static readonly CuratedGrammarLesson ReportedSpeech = new(
        "reported-speech",
        "O'zlashtirma gap - reported speech",
        "O'zlashtirma gap birovning so'zlarini to'g'ridan-to'g'ri keltirmasdan, o'z so'zlaring bilan " +
        "yetkazadi. Bunda zamonlar ko'pincha bir pog'ona orqaga suriladi va olmosh/vaqt so'zlari o'zgaradi.",
        new[]
        {
            "To'g'ridan: He said, \"I am tired.\"  →  O'zlashtirma: He said (that) he was tired.",
            "Zamon orqaga suriladi: present → past, will → would, can → could",
            "So'roq: He asked if/whether ... yoki wh- so'z + to'g'ri tartib",
            "Buyruq: told/asked + somebody + to + fe'l",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Zamonlarning orqaga surilishi",
                "say/tell o'tmishda bo'lsa: am/is → was, are → were, do → did, have done → had done, " +
                "will → would, can → could, must → had to. Masalan: \"I will call\" → He said he would call."),
            new CuratedGrammarRule(
                "Olmosh va vaqt so'zlari",
                "I → he/she, my → his/her, now → then, today → that day, tomorrow → the next day, " +
                "here → there, this → that."),
            new CuratedGrammarRule(
                "So'roqlarni yetkazish",
                "Yes/no so'roq: ask + if/whether (He asked if I was ready). Wh- so'roq: ask + wh- " +
                "so'z, lekin so'roq tartibisiz (She asked where I lived - «where did I live» emas)."),
            new CuratedGrammarRule(
                "say va tell farqi",
                "tell dan keyin kishi keladi (He told me...), say dan keyin to'g'ridan kishi kelmaydi " +
                "(He said that...). «He said me» emas."),
        },
        new[]
        {
            new CuratedGrammarExample("She said that she was busy.", "U band ekanini aytdi."),
            new CuratedGrammarExample("He told me he would help.", "U menga yordam berishini aytdi."),
            new CuratedGrammarExample("They asked if I was coming.", "Ular kelyaptimanmi deb so'rashdi."),
            new CuratedGrammarExample("She asked where I lived.", "U qayerda yashashimni so'radi."),
            new CuratedGrammarExample("He told me to wait.", "U menga kutishni aytdi."),
        },
        new[]
        {
            "say dan keyin kishini to'g'ridan qo'ymang: «He said me» emas, «He told me» yoki «He said to me».",
            "O'zlashtirma so'roqda so'roq tartibini ishlatmang: «She asked where did I live» emas.",
            "Zamonni orqaga surishni unutmang: «He said he is tired» o'rniga ko'pincha «he was tired».",
        });

    // ───────────────────────────── B2 - Modals of Deduction ─────────────────────────────
    private static readonly CuratedGrammarLesson ModalsOfDeduction = new(
        "modals-of-deduction",
        "Taxmin modallari - must be, can't be, might be",
        "Bu modallar dalillarga asoslanib biror narsa haqida xulosa yoki taxmin qilishni bildiradi: " +
        "ishonchli xulosa (must), imkonsizlik (can't) yoki ehtimol (might/could/may).",
        new[]
        {
            "must - ishonchli ijobiy xulosa  →  He must be tired.",
            "can't - ishonchli salbiy xulosa (imkonsiz)  →  She can't be serious.",
            "might / may / could - ehtimol, taxmin  →  They might be at home.",
            "O'tmish: must have / can't have / might have + V3",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Ishonch darajalari",
                "must - deyarli aminman (ijobiy). can't - deyarli aminman (salbiy, imkonsiz). " +
                "might/may/could - ehtimol bor, lekin aniq emas."),
            new CuratedGrammarRule(
                "must va can't - qarama-qarshi",
                "Ijobiy ishonch uchun must (He must be rich), salbiy ishonch uchun «mustn't» emas, " +
                "«can't» (He can't be poor)."),
            new CuratedGrammarRule(
                "O'tmish haqida taxmin",
                "must have + V3 (She must have left - ketgan bo'lsa kerak), can't have + V3 " +
                "(He can't have known - bilgan bo'lishi mumkin emas), might have + V3 (They might have " +
                "forgotten)."),
            new CuratedGrammarRule(
                "Majburiyatdan farqi",
                "Bu yerda must majburiyat emas, taxmin bildiradi. Kontekst farqni ko'rsatadi: " +
                "You must be hungry (taxmin) ≠ You must eat (majburiyat)."),
        },
        new[]
        {
            new CuratedGrammarExample("You must be joking!", "Hazillashayotgan bo'lsang kerak!"),
            new CuratedGrammarExample("She can't be at home; her car isn't here.", "U uyda bo'lishi mumkin emas; mashinasi yo'q."),
            new CuratedGrammarExample("They might be on holiday.", "Ular ta'tilda bo'lsa kerak."),
            new CuratedGrammarExample("He must have missed the bus.", "U avtobusga kechikkan bo'lsa kerak."),
            new CuratedGrammarExample("I can't have lost my keys again!", "Kalitlarimni yana yo'qotgan bo'lishim mumkin emas!"),
        },
        new[]
        {
            "Salbiy ishonchli xulosa uchun «mustn't» emas, «can't be» ishlatiladi.",
            "O'tmish taxmini uchun «must had» emas, «must have + V3».",
            "Taxminni majburiyat bilan aralashtirmang - kontekstga qarang.",
        });

    // ───────────────────────────── B2 - Non-defining Relative Clauses ─────────────────────────────
    private static readonly CuratedGrammarLesson NonDefiningRelativeClauses = new(
        "non-defining-relative-clauses",
        "Izohlovchi ergash gaplar - , which / , who ...",
        "Izohlovchi ergash gaplar otga qo'shimcha, zarur bo'lmagan ma'lumot beradi. Ularni olib tashlasa " +
        "ham asosiy gap to'liq bo'lib qoladi. Ikki tomondan vergul (yoki tire) bilan ajratiladi.",
        new[]
        {
            "..., who ..., - odamlar uchun  →  My brother, who lives in London, is a doctor.",
            "..., which ..., - narsalar uchun  →  This book, which I love, is old.",
            "«that» ISHLATILMAYDI; olmoshni tushirib bo'lmaydi.",
            "Butun gapga izoh: ..., which ...  →  He was late, which annoyed me.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Vergul majburiy",
                "Qo'shimcha ma'lumot vergul bilan ajratiladi: Mr Karimov, who is our teacher, is very kind. " +
                "Vergul ichidagi qism olib tashlansa ham gap ma'noli qoladi."),
            new CuratedGrammarRule(
                "«that» ishlatilmaydi",
                "Aniqlovchidan (defining) farqli, bu yerda «that» qo'llanmaydi: «My car, that is red» emas, " +
                "«My car, which is red»."),
            new CuratedGrammarRule(
                "Olmoshni tushirib bo'lmaydi",
                "Aniqlovchidan farqli, bu yerda who/which ni tushirib qoldirib bo'lmaydi: " +
                "The film, which I saw yesterday, was great."),
            new CuratedGrammarRule(
                "Butun gapga izoh",
                "«, which» butun oldingi fikrga izoh bera oladi: She passed the exam, which made her happy."),
        },
        new[]
        {
            new CuratedGrammarExample("My sister, who is a nurse, works at night.", "Hamshira bo'lgan opam tunda ishlaydi."),
            new CuratedGrammarExample("Tashkent, which is the capital, is very big.", "Poytaxt bo'lgan Toshkent juda katta."),
            new CuratedGrammarExample("This phone, which I bought last year, still works well.", "O'tgan yili sotib olgan bu telefon hali ham yaxshi ishlaydi."),
            new CuratedGrammarExample("He arrived late, which surprised everyone.", "U kech keldi, bu hammani hayron qoldirdi."),
            new CuratedGrammarExample("Our teacher, whose name is Aziz, is patient.", "Ismi Aziz bo'lgan o'qituvchimiz sabrli."),
        },
        new[]
        {
            "Izohlovchi gapda «that» ishlatmang: «, that is...» emas, «, which is...».",
            "Vergullarni qo'ying - qo'shimcha ma'lumot ajratiladi.",
            "who/which ni tushirib qoldirmang (aniqlovchidan farqli).",
        });

    // ───────────────────────────── B2 - Future Continuous ─────────────────────────────
    private static readonly CuratedGrammarLesson FutureContinuous = new(
        "future-continuous",
        "Future Continuous - kelajakda davom etadigan zamon (will be + V-ing)",
        "Future Continuous kelajakning ma'lum bir paytida davom etayotgan ish-harakatni bildiradi. " +
        "O'zbekchada «... -yotgan bo'laman» orqali ifodalanadi.",
        new[]
        {
            "will be + fe'l-ing  →  I will be working at 9.",
            "Inkor: won't be + V-ing  →  She won't be sleeping.",
            "So'roq: Will + ega + be + V-ing?  →  Will you be using the car?",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Kelajakdagi aniq paytda jarayon",
                "Kelajakning bir nuqtasida harakat davom etayotgan bo'ladi: This time tomorrow, I will be " +
                "flying to London (Ertaga shu payt men Londonga uchayotgan bo'laman)."),
            new CuratedGrammarRule(
                "Rejalashtirilgan, tabiiy ravishda bo'ladigan",
                "Oldindan ma'lum, odatiy ravishda kechadigan ish: I'll be seeing him at the meeting " +
                "(uni baribir uchrataman)."),
            new CuratedGrammarRule(
                "Muloyim so'rash",
                "Birovning rejasini bilish uchun muloyim usul: Will you be using the printer? " +
                "(Printerdan foydalanasizmi?) - bosim o'tkazmaydi."),
            new CuratedGrammarRule(
                "Future Simple bilan farqi",
                "will + fe'l - butun harakat yoki qaror (I'll call him). will be + V-ing - kelajakdagi " +
                "paytda davom etayotgan jarayon (At 8 I'll be having dinner)."),
        },
        new[]
        {
            new CuratedGrammarExample("This time next week, I'll be relaxing on the beach.", "Kelasi hafta shu payt men plyajda dam olayotgan bo'laman."),
            new CuratedGrammarExample("She will be studying all evening.", "U butun kechqurun o'qiyotgan bo'ladi."),
            new CuratedGrammarExample("Don't call at 7 - we'll be having dinner.", "Soat 7 da qo'ng'iroq qilma - biz ovqatlanayotgan bo'lamiz."),
            new CuratedGrammarExample("Will you be working tomorrow?", "Ertaga ishlaysizmi?"),
            new CuratedGrammarExample("They won't be waiting for us.", "Ular bizni kutib turmaydi."),
        },
        new[]
        {
            "«be» ni tushirmang: «I will working» emas, «I will be working».",
            "«will» dan keyin ikkala fe'l ham to'g'ri shaklda: «will be work» emas, «will be working».",
            "Bir martalik qaror uchun Future Simple yetarli: «I'll be calling him now» o'rniga «I'll call».",
        });

    // ───────────────────────────── B2 - Wish Clauses ─────────────────────────────
    private static readonly CuratedGrammarLesson WishClauses = new(
        "wish-clauses",
        "«Wish» gaplari - afsus va istak",
        "«Wish» (va «if only») real bo'lmagan holatga afsus yoki istakni bildiradi. Bunda fe'l zamoni " +
        "bir pog'ona orqaga suriladi: hozir uchun past, o'tmish uchun past perfect ishlatiladi.",
        new[]
        {
            "wish + past simple - hozirgi holatga afsus  →  I wish I knew the answer.",
            "wish + past perfect - o'tmishga afsus  →  I wish I had studied.",
            "wish + would - boshqaning harakatidan bezovtalik  →  I wish he would stop.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Hozirgi holatga afsus",
                "wish + past simple hozirgi real bo'lmagan istakni bildiradi: I wish I had a car " +
                "(hozir mashinam yo'q - afsus). «be» uchun barcha shaxslarda «were»: I wish I were taller."),
            new CuratedGrammarRule(
                "O'tmishga afsus",
                "wish + past perfect (had + V3) o'tmishdagi ishga afsus bildiradi: I wish I had listened " +
                "to you (sizni eshitmaganimga afsus)."),
            new CuratedGrammarRule(
                "wish + would",
                "Boshqa shaxs yoki holatning xatti-harakati o'zgarishini istash (ko'pincha g'ashlik): " +
                "I wish it would stop raining; I wish you would be quiet. O'z holating uchun would " +
                "ishlatilmaydi."),
            new CuratedGrammarRule(
                "if only - kuchliroq",
                "«if only» ham xuddi wish kabi, lekin his-tuyg'u kuchliroq: If only I had more time!"),
        },
        new[]
        {
            new CuratedGrammarExample("I wish I had more free time.", "Koshki ko'proq bo'sh vaqtim bo'lsa edi."),
            new CuratedGrammarExample("She wishes she were taller.", "U bo'yi balandroq bo'lishini istaydi."),
            new CuratedGrammarExample("I wish I had studied harder.", "Qattiqroq o'qiganimda edi."),
            new CuratedGrammarExample("I wish you would listen to me.", "Qaniydi meni eshitsang."),
            new CuratedGrammarExample("If only we had known earlier!", "Qaniydi ertaroq bilganimizda edi!"),
        },
        new[]
        {
            "Hozirgi afsus uchun present emas, past: «I wish I know» emas, «I wish I knew».",
            "O'tmish afsusi uchun «I wish I studied» emas, «I wish I had studied».",
            "O'z harakating uchun «I wish I would» ishlatmang.",
        });

    // ───────────────────────────── B2 - Causative Have ─────────────────────────────
    private static readonly CuratedGrammarLesson CausativeHave = new(
        "causative-have",
        "Causative «have» - have/get something done",
        "Causative tuzilma biror ishni o'zing emas, boshqa birovga qildirishingni bildiradi. " +
        "O'zbekchada «-tirmoq/-dirmoq» (qildirmoq) ma'nosiga to'g'ri keladi: sochimni oldirdim.",
        new[]
        {
            "have + narsa (ob'ekt) + past participle (V3)  →  I had my car repaired.",
            "get + narsa + V3 (norasmiyroq)  →  I got my hair cut.",
            "Turli zamonlarda: have/has/had + ob'ekt + V3",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Asosiy ma'no",
                "Ishni mutaxassis yoki boshqa odam bajaradi, sen tashabbuskor yoki to'lovchisan: " +
                "I had my house painted (uyimni bo'yatdim - o'zim bo'yamadim)."),
            new CuratedGrammarRule(
                "So'z tartibi",
                "have + ob'ekt + V3 - ob'ekt fe'l 3-shakli oldida turadi: She had her photo taken " +
                "(«had taken her photo» emas)."),
            new CuratedGrammarRule(
                "have va get",
                "Ma'no bir xil; «get» so'zlashuvda ko'proq: I'm getting my teeth checked. " +
                "Rasmiyroq matnlarda «have»."),
            new CuratedGrammarRule(
                "Yoqimsiz voqea ma'nosi",
                "Ba'zan o'zing xohlamagan, boshingga tushgan ishni ham bildiradi: " +
                "He had his wallet stolen (hamyoni o'g'irlandi)."),
        },
        new[]
        {
            new CuratedGrammarExample("I had my car repaired yesterday.", "Kecha mashinamni tuzattirdim."),
            new CuratedGrammarExample("She is having her hair cut.", "U sochini oldirayapti."),
            new CuratedGrammarExample("We had our house painted.", "Uyimizni bo'yatdik."),
            new CuratedGrammarExample("He had his wallet stolen.", "Uning hamyoni o'g'irlandi."),
            new CuratedGrammarExample("You should get your eyes tested.", "Ko'zingni tekshirtirishing kerak."),
        },
        new[]
        {
            "So'z tartibiga e'tibor bering: «I had repaired my car» (o'zim tuzatdim) ≠ «I had my car repaired» (tuzattirdim).",
            "Asosiy fe'l 3-shaklida bo'lsin: «have my car repair» emas, «have my car repaired».",
            "Ob'ektni V3 dan oldin qo'ying.",
        });

    // ───────────────────────────── C1 - Inversion ─────────────────────────────
    private static readonly CuratedGrammarLesson Inversion = new(
        "inversion",
        "Inversiya - so'z tartibining o'zgarishi",
        "Inversiya - odatdagi «ega + fe'l» tartibini «yordamchi fe'l + ega» ga aylantirib, urg'u yoki " +
        "rasmiy ohang berish usuli. Ko'pincha salbiy yoki cheklovchi ravishlar gap boshiga chiqqanda yuz beradi.",
        new[]
        {
            "Salbiy ravish + yordamchi fe'l + ega  →  Never have I seen such a thing.",
            "Not only ... but also  →  Not only did he apologize, but he also paid.",
            "Shart gapda «if» tushib: Had I known... = If I had known...",
            "So + sifat / Such + ot ...  →  So beautiful was the view that we stayed.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Salbiy/cheklovchi ravishlar bilan",
                "never, rarely, seldom, hardly, no sooner, not only, little, only then gap boshiga chiqsa, " +
                "so'roq tartibi keladi: Rarely do we see such talent."),
            new CuratedGrammarRule(
                "Shart gaplarda «if» siz",
                "Rasmiy uslubda «if» tushiriladi va had/were/should oldinga chiqadi: " +
                "Had I known (= If I had known); Were I you (= If I were you); Should you need help..."),
            new CuratedGrammarRule(
                "«so» va «such» bilan",
                "So + sifat ... that yoki Such + ot ... that gap boshida inversiya bilan: " +
                "So great was the demand that they reprinted the book."),
            new CuratedGrammarRule(
                "Maqsadi - urg'u va uslub",
                "Inversiya ma'noni o'zgartirmaydi, ammo ohangni rasmiy va ta'sirli qiladi; " +
                "asosan yozma va adabiy uslubda ishlatiladi."),
        },
        new[]
        {
            new CuratedGrammarExample("Never have I been so embarrassed.", "Hech qachon bunchalik uyalmaganman."),
            new CuratedGrammarExample("Not only did she sing, but she also danced.", "U nafaqat kuyladi, balki raqsga ham tushdi."),
            new CuratedGrammarExample("Rarely do we get such an opportunity.", "Bunday imkoniyat kamdan-kam bo'ladi."),
            new CuratedGrammarExample("Had I known, I would have come earlier.", "Bilganimda, ertaroq kelardim."),
            new CuratedGrammarExample("No sooner had we arrived than it started to rain.", "Biz yetib kelishimiz bilanoq yomg'ir boshlandi."),
        },
        new[]
        {
            "Salbiy ravish gap boshida bo'lsa so'roq tartibini ishlating: «Never I have» emas, «Never have I».",
            "Inversiyali shartda «if» qaytarmang: «Had if I known» emas, «Had I known».",
            "Inversiyani har gapda emas, faqat urg'u/rasmiylik kerak bo'lganda ishlating.",
        });

    // ───────────────────────────── C1 - Cleft Sentences ─────────────────────────────
    private static readonly CuratedGrammarLesson CleftSentences = new(
        "cleft-sentences",
        "Ajratma gaplar - It is... / What... clauses",
        "Ajratma (cleft) gaplar bir oddiy gapni ikkiga bo'lib, ma'lum bir qismga kuchli urg'u beradi. " +
        "Eng keng tarqalgan ikki turi: «It is ... that ...» va «What ... is ...».",
        new[]
        {
            "It-cleft: It + be + urg'uli qism + that/who ...",
            "    It was John who broke the window.",
            "Wh-cleft: What + gap + be + urg'uli qism",
            "    What I need is a holiday.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "It-cleft tuzilishi",
                "Urg'u bermoqchi bo'lgan bo'lakni «It is/was ... that ...» orasiga qo'yiladi: " +
                "It was yesterday that I met her (aynan kecha - vaqtga urg'u)."),
            new CuratedGrammarRule(
                "Wh-cleft (What-cleft)",
                "«What ...» bilan boshlanib, urg'uni gap oxiriga suradi: What surprised me was his honesty " +
                "(meni hayron qoldirgani - uning halolligi)."),
            new CuratedGrammarRule(
                "Qaysi qismga urg'u",
                "Ega, to'ldiruvchi, vaqt yoki o'rinni ajratish mumkin: It's the manager who decides; " +
                "It was in Paris that they met."),
            new CuratedGrammarRule(
                "Maqsadi",
                "Ma'lumotni qarama-qarshi qo'yish yoki tuzatish uchun foydali: " +
                "It wasn't me who called - it was Tom."),
        },
        new[]
        {
            new CuratedGrammarExample("It was Maria who solved the problem.", "Muammoni hal qilgan aynan Mariya edi."),
            new CuratedGrammarExample("It's your help that I need.", "Menga aynan sening yordaming kerak."),
            new CuratedGrammarExample("What I want is a clear answer.", "Menga kerak bo'lgan narsa - aniq javob."),
            new CuratedGrammarExample("It was in 2010 that we first met.", "Biz birinchi marta aynan 2010-yilda uchrashganmiz."),
            new CuratedGrammarExample("What she did was call the police.", "U qilgan ish - politsiyaga qo'ng'iroq qilish edi."),
        },
        new[]
        {
            "It-cleftda «that/who» ni tushirmang: «It was John broke it» emas.",
            "Wh-cleftda «be» fe'lini to'g'ri moslang: «What I need are a holiday» emas, «is a holiday».",
            "Cleftni har gapda emas, urg'u yoki qarama-qarshilik kerak bo'lganda ishlating.",
        });

    // ───────────────────────────── C1 - Participle Clauses ─────────────────────────────
    private static readonly CuratedGrammarLesson ParticipleClauses = new(
        "participle-clauses",
        "Sifatdosh gaplar - participle clauses",
        "Sifatdosh gaplar fe'lning -ing yoki -ed (V3) shaklidan foydalanib, ikki gapni qisqa va ixcham " +
        "qilib bog'laydi. Ular sabab, vaqt yoki qo'shimcha ma'lumotni rasmiy uslubda ifodalaydi.",
        new[]
        {
            "-ing (faol ma'no)  →  Feeling tired, she went to bed.",
            "Past participle / -ed (majhul ma'no)  →  Built in 1900, the house is now a museum.",
            "Having + V3 (oldin bo'lgan ish)  →  Having finished, he left.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "-ing - faol va bir vaqtda",
                "Ega bir vaqtda ikki ish qilganda yoki sabab bildirganda: Walking home, I saw an accident " +
                "(uyga ketayotib...); Being ill, she stayed home (kasal bo'lgani uchun...)."),
            new CuratedGrammarRule(
                "Past participle - majhul",
                "Ega harakatni boshqa biror narsadan «olganda» (majhul ma'no): Written in simple English, " +
                "the book is easy to read."),
            new CuratedGrammarRule(
                "Having + V3 - oldinroq tugagan",
                "Bir ish ikkinchisidan oldin tugaganini ko'rsatadi: Having eaten, we went for a walk " +
                "(ovqatlanib bo'lgach...)."),
            new CuratedGrammarRule(
                "Ega bir xil bo'lishi shart",
                "Sifatdosh gapning yashirin egasi bosh gap egasi bilan bir xil bo'lishi kerak, aks holda " +
                "«dangling participle» xatosi yuzaga keladi."),
        },
        new[]
        {
            new CuratedGrammarExample("Feeling tired, I went to bed early.", "Charchaganimni his qilib, erta yotdim."),
            new CuratedGrammarExample("Built in 1500, the mosque is still standing.", "1500-yilda qurilgan masjid hali ham turibdi."),
            new CuratedGrammarExample("Having finished her work, she relaxed.", "Ishini tugatgach, u dam oldi."),
            new CuratedGrammarExample("Not knowing what to say, he remained silent.", "Nima deyishini bilmay, u jim qoldi."),
            new CuratedGrammarExample("Surrounded by friends, she felt happy.", "Do'stlari qurshovida u o'zini baxtli his qildi."),
        },
        new[]
        {
            "Egalar bir xil bo'lsin (dangling participle): «Walking home, the rain started» noto'g'ri.",
            "Faol uchun -ing, majhul uchun V3: «Writing in English» (faol) ≠ «Written in English» (majhul).",
            "Oldin tugagan ish uchun «Having + V3» ishlating, oddiy -ing emas.",
        });

    // ───────────────────────────── C1 - Advanced Passive ─────────────────────────────
    private static readonly CuratedGrammarLesson AdvancedPassive = new(
        "advanced-passive",
        "Murakkab majhul nisbat - advanced passive",
        "Murakkab majhul tuzilmalar fikr va xabarlarni shaxssiz, rasmiy va ehtiyotkor uslubda yetkazadi: " +
        "«It is said that ...», «He is believed to ...», hamda noaniq «get»-passive shakllari.",
        new[]
        {
            "It + be + V3 + that ...  →  It is believed that the plan will work.",
            "Subject + be + V3 + to-infinitive  →  He is thought to be very rich.",
            "Past haqida: ... to have + V3  →  She is said to have left the country.",
            "get-passive (norasmiy/kutilmagan)  →  He got promoted.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Shaxssiz xabar - «It is said that»",
                "Manba noma'lum yoki umumiy fikrni yetkazganda: It is reported that... , It is known that... " +
                "Bu uslub jurnalistika va ilmiy matnda keng tarqalgan."),
            new CuratedGrammarRule(
                "Shaxsiy passive - «He is believed to»",
                "Xuddi shu ma'noni egani gap boshiga chiqarib beradi: People believe he is rich → " +
                "He is believed to be rich. O'tmish uchun: ... to have + V3."),
            new CuratedGrammarRule(
                "get-passive",
                "So'zlashuv va kutilmagan/nojo'ya hodisalar uchun «be» o'rniga «get»: " +
                "He got fired; The window got broken. Rasmiy matnda kam ishlatiladi."),
            new CuratedGrammarRule(
                "Maqsadi - ob'ektivlik va ehtiyot",
                "Bu tuzilmalar javobgarlikni ko'rsatmasdan, ehtiyotkor va xolis ohang beradi: " +
                "Mistakes were made (kim qilgani aytilmaydi)."),
        },
        new[]
        {
            new CuratedGrammarExample("It is said that he is the best in his field.", "Aytishlaricha, u o'z sohasida eng yaxshisi."),
            new CuratedGrammarExample("The president is believed to be ill.", "Prezident kasal deb hisoblanadi."),
            new CuratedGrammarExample("She is said to have made a fortune.", "Aytishlaricha, u katta boylik orttirgan."),
            new CuratedGrammarExample("Several people are reported to have been injured.", "Bir necha kishi jarohatlangani xabar qilinmoqda."),
            new CuratedGrammarExample("He got promoted last month.", "U o'tgan oy lavozimi ko'tarildi."),
        },
        new[]
        {
            "«It is said that» dan keyin to'liq gap kerak: «It is said to he is...» emas.",
            "O'tmish uchun «to have + V3»: «is said to left» emas, «is said to have left».",
            "get-passive ni rasmiy matnda ortiqcha ishlatmang.",
        });

    // ───────────────────────────── C1 - Subjunctive ─────────────────────────────
    private static readonly CuratedGrammarLesson Subjunctive = new(
        "subjunctive",
        "Subjunktiv mayl - subjunctive",
        "Subjunktiv mayl talab, taklif, zarurat yoki istakni rasmiy uslubda bildiradi. Bunda fe'l shaxsdan " +
        "qat'i nazar asosiy (o'zak) shaklda qoladi: he be, she go, it not happen.",
        new[]
        {
            "suggest / recommend / insist / demand + that + ega + asosiy fe'l",
            "    I suggest that he be present. / They demanded that she leave.",
            "It is essential / important / vital + that + ega + asosiy fe'l",
            "    It is essential that everyone arrive on time.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "O'zak (asosiy) fe'l shakli",
                "that-gapdagi fe'l shaxsga moslashmaydi va -s olmaydi: It is important that he be ready " +
                "(«is» emas, «be»); I suggest that she go (« goes» emas)."),
            new CuratedGrammarRule(
                "Qaysi fe'l/sifatlardan keyin",
                "suggest, recommend, propose, insist, demand, request, require + that. " +
                "Sifatlar: essential, important, vital, necessary, crucial + that."),
            new CuratedGrammarRule(
                "Inkor shakli",
                "Inkorda «not» to'g'ridan-to'g'ri fe'l oldiga qo'yiladi, do/does ishlatilmaydi: " +
                "We insist that he not be late."),
            new CuratedGrammarRule(
                "Norasmiy muqobil",
                "So'zlashuvda ko'pincha «should» ishlatiladi: I suggest that he should be present. " +
                "Subjunktiv esa rasmiy va Amerika inglizchasida keng tarqalgan."),
        },
        new[]
        {
            new CuratedGrammarExample("I suggest that he apply for the job.", "Men u ishga ariza berishini taklif qilaman."),
            new CuratedGrammarExample("It is essential that everyone be on time.", "Hammaning o'z vaqtida kelishi muhim."),
            new CuratedGrammarExample("They demanded that she leave immediately.", "Ular uning darhol ketishini talab qilishdi."),
            new CuratedGrammarExample("The doctor recommended that he rest.", "Shifokor unga dam olishni tavsiya qildi."),
            new CuratedGrammarExample("We insist that the report not be delayed.", "Hisobot kechiktirilmasligini talab qilamiz."),
        },
        new[]
        {
            "Fe'lga -s qo'shmang: «I suggest that he applies» emas, «that he apply».",
            "Inkorda do/does ishlatmang: «that he doesn't be late» emas, «that he not be late».",
            "«be» fe'lini moslashtirmang: «that he is present» (subjunktivda) emas, «that he be present».",
        });

    // ───────────────────────────── C1 - Future Perfect ─────────────────────────────
    private static readonly CuratedGrammarLesson FuturePerfect = new(
        "future-perfect",
        "Future Perfect - kelajakda tugallangan zamon (will have + V3)",
        "Future Perfect kelajakning ma'lum bir paytigacha tugab bo'lgan ish-harakatni bildiradi. " +
        "O'zbekchada «... -gan bo'laman» orqali ifodalanadi: By June I will have finished - iyungacha tugatgan bo'laman.",
        new[]
        {
            "will have + past participle (V3)  →  I will have finished by 6.",
            "Inkor: won't have + V3  →  They won't have arrived yet.",
            "So'roq: Will + ega + have + V3?  →  Will you have eaten by then?",
            "Ko'pincha «by» yoki «by the time» bilan keladi.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Kelajakdagi muddatgacha tugash",
                "Biror vaqtgacha tugaydigan ishni bildiradi: By next year, I will have graduated " +
                "(Kelasi yilga kelib, men bitirgan bo'laman)."),
            new CuratedGrammarRule(
                "«by» va «by the time»",
                "by + vaqt nuqtasi (by Friday), by the time + present gap (By the time you arrive, " +
                "I will have left). by the time dan keyin kelajak «will» ishlatilmaydi."),
            new CuratedGrammarRule(
                "Tuzilishi",
                "will + have + V3, barcha shaxslar uchun bir xil: She will have completed the course."),
            new CuratedGrammarRule(
                "Future Continuous bilan farqi",
                "Future Perfect → tugagan natija (I'll have written the report by 5). " +
                "Future Continuous → davom etayotgan jarayon (At 5 I'll be writing)."),
        },
        new[]
        {
            new CuratedGrammarExample("By 2030, I will have finished university.", "2030-yilga kelib, men universitetni bitirgan bo'laman."),
            new CuratedGrammarExample("She will have left by the time you call.", "Sen qo'ng'iroq qilganingda u ketgan bo'ladi."),
            new CuratedGrammarExample("They won't have finished the project yet.", "Ular loyihani hali tugatmagan bo'ladi."),
            new CuratedGrammarExample("Will you have eaten before we meet?", "Biz uchrashguncha ovqatlangan bo'lasizmi?"),
            new CuratedGrammarExample("In an hour, the train will have arrived.", "Bir soatdan keyin poyezd yetib kelgan bo'ladi."),
        },
        new[]
        {
            "«have» va V3 ni unutmang: «I will finished» emas, «I will have finished».",
            "«by the time» dan keyin kelajak ishlatmang: «By the time you will arrive» emas, «you arrive».",
            "Tugagan natija uchun Continuous emas, Perfect: «will be finished» o'rniga «will have finished».",
        });

    // ───────────────────────────── C1 - Ellipsis and Substitution ─────────────────────────────
    private static readonly CuratedGrammarLesson EllipsisAndSubstitution = new(
        "ellipsis-and-substitution",
        "Ellipsis va o'rin almashtirish",
        "Ellipsis - takrorlanadigan so'zlarni tushirib qoldirish; substitution - ularni «one, do, so, not» " +
        "kabi so'zlar bilan almashtirish. Ikkalasi ham takrorni kamaytirib, nutqni tabiiy va ixcham qiladi.",
        new[]
        {
            "Ellipsis: takror so'zni tushirish  →  She can sing and (she can) dance.",
            "one/ones - ot o'rnida  →  I prefer the red one.",
            "do/does/did - fe'l o'rnida  →  He works harder than I do.",
            "so / not - butun gap o'rnida  →  I think so. / I hope not.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Ellipsis - so'z tushirish",
                "Kontekstdan tushunarli takror so'zlar tushiriladi: I'll have coffee and she'll have tea → " +
                "I'll have coffee and she (will have) tea. Ko'pincha bog'lovchidan keyin."),
            new CuratedGrammarRule(
                "one / ones",
                "Sanaladigan otni takrorlamaslik uchun: Which shirt? - The blue one. " +
                "Ko'plik uchun «ones»: I like the small ones."),
            new CuratedGrammarRule(
                "do / so / not bilan almashtirish",
                "do/does/did fe'l guruhini almashtiradi (She runs faster than he does). «so» ijobiy " +
                "gapni (I think so), «not» salbiy gapni (I'm afraid not) almashtiradi."),
            new CuratedGrammarRule(
                "Maqsadi - ixchamlik",
                "Bu vositalar ortiqcha takrorni yo'qotib, nutqni ravon qiladi; ayniqsa savol-javob va " +
                "qiyoslarda foydali."),
        },
        new[]
        {
            new CuratedGrammarExample("She can play the piano, and he can too.", "U pianino chala oladi, u ham (chala oladi)."),
            new CuratedGrammarExample("I have a black pen and a red one.", "Menda qora va qizil ruchka bor."),
            new CuratedGrammarExample("He earns more than I do.", "U mendan ko'proq topadi."),
            new CuratedGrammarExample("Is it going to rain? - I think so.", "Yomg'ir yog'adimi? - Menimcha, ha."),
            new CuratedGrammarExample("Will they come? - I hope not.", "Ular keladimi? - Umid qilamanki, yo'q."),
        },
        new[]
        {
            "Otni takrorlamang - «one/ones» ishlating: «I prefer the red shirt one» emas.",
            "«so» va «not» ni teskari ishlatmang: ijobiy uchun «so», salbiy uchun «not».",
            "Ellipsisni faqat ma'no aniq bo'lganda qo'llang, aks holda gap noaniq bo'ladi.",
        });

    // ───────────────────────────── C1 - Nominalisation ─────────────────────────────
    private static readonly CuratedGrammarLesson Nominalisation = new(
        "nominalisation",
        "Nominalizatsiya - fe'l/sifatni otga aylantirish",
        "Nominalizatsiya - fe'l yoki sifatni ot shakliga aylantirib, fikrni rasmiy, zich va akademik " +
        "uslubda ifodalash. Masalan «to decide» → «decision», «to grow» → «growth».",
        new[]
        {
            "Fe'l → ot: decide → decision, grow → growth, analyse → analysis",
            "Sifat → ot: able → ability, important → importance, happy → happiness",
            "Gap qayta qurish: We decided quickly → Our quick decision ...",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Maqsadi - akademik uslub",
                "Harakatni ot sifatida ifodalash matnni rasmiy va xolis qiladi: «The economy grew rapidly» " +
                "→ «The rapid growth of the economy». Ilmiy va rasmiy yozuvda keng tarqalgan."),
            new CuratedGrammarRule(
                "Keng tarqalgan qo'shimchalar",
                "-tion/-sion (decision, expansion), -ment (development), -ness (happiness), " +
                "-ity (ability), -th (growth), -ance/-ence (importance, difference)."),
            new CuratedGrammarRule(
                "Ma'lumot zichligi",
                "Nominalizatsiya ko'p ma'lumotni qisqa qilib jamlaydi, lekin haddan ortig'i matnni og'ir " +
                "va tushunish qiyin qiladi - me'yorni saqlash kerak."),
            new CuratedGrammarRule(
                "Gapni qayta qurish",
                "Fe'lli gapni otli iboraga aylantirganda ega va to'ldiruvchi «of», «in» kabi predloglar " +
                "bilan bog'lanadi: They failed → Their failure to ..."),
        },
        new[]
        {
            new CuratedGrammarExample("The discovery of penicillin changed medicine.", "Penitsillinning kashf etilishi tibbiyotni o'zgartirdi."),
            new CuratedGrammarExample("There has been a rapid growth in demand.", "Talabning tez o'sishi kuzatildi."),
            new CuratedGrammarExample("His refusal surprised everyone.", "Uning rad etishi hammani hayron qoldirdi."),
            new CuratedGrammarExample("The importance of education cannot be ignored.", "Ta'limning ahamiyatini e'tiborsiz qoldirib bo'lmaydi."),
            new CuratedGrammarExample("Their analysis revealed several problems.", "Ularning tahlili bir nechta muammoni ochib berdi."),
        },
        new[]
        {
            "Ot shaklini to'g'ri tanlang: «the decide» emas, «the decision».",
            "Haddan ortiq nominalizatsiya matnni og'irlashtiradi - me'yorni saqlang.",
            "Otli iborada predloglarni to'g'ri bog'lang: «the growth of...», «the failure to...».",
        });

    // ───────────────────────────── C1 - Discourse Markers ─────────────────────────────
    private static readonly CuratedGrammarLesson DiscourseMarkers = new(
        "discourse-markers",
        "Bog'lovchi iboralar - however, therefore, moreover...",
        "Bog'lovchi iboralar (discourse markers) fikrlarni mantiqan bog'laydi: qarama-qarshilik, sabab-natija, " +
        "qo'shimcha yoki misol munosabatlarini ko'rsatib, matnni ravon va izchil qiladi.",
        new[]
        {
            "Qarama-qarshilik: however, nevertheless, on the other hand, despite this",
            "Sabab-natija: therefore, thus, consequently, as a result",
            "Qo'shimcha: moreover, furthermore, in addition, besides",
            "Misol/xulosa: for instance, in other words, to sum up",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Qarama-qarshilik bildiruvchilar",
                "however, nevertheless, on the contrary oldingi fikrga zid g'oyani kiritadi: " +
                "The plan was risky. However, it succeeded."),
            new CuratedGrammarRule(
                "Sabab va natija",
                "therefore, thus, consequently, as a result natijani ko'rsatadi: Sales fell; " +
                "therefore, prices were cut."),
            new CuratedGrammarRule(
                "Tinish belgilari",
                "Bog'lovchi ibora gap boshida ko'pincha vergul bilan ajratiladi: " +
                "Moreover, the cost was too high. Ikki gapni bog'laganda nuqtali vergul: " +
                "It rained; nevertheless, we went out."),
            new CuratedGrammarRule(
                "Bog'lovchi va bog'lovchi-ravish farqi",
                "however/therefore - ravish, ikki mustaqil gapni bog'laydi (and/but kabi emas). " +
                "Shuning uchun ular oddiy vergul bilan ikki gapni qo'sha olmaydi."),
        },
        new[]
        {
            new CuratedGrammarExample("The test was hard; however, she passed.", "Test qiyin edi; biroq u o'tdi."),
            new CuratedGrammarExample("He was ill. Therefore, he stayed home.", "U kasal edi. Shu sababli, u uyda qoldi."),
            new CuratedGrammarExample("Moreover, the price includes delivery.", "Bundan tashqari, narxga yetkazib berish kiradi."),
            new CuratedGrammarExample("The food was great. On the other hand, the service was slow.", "Ovqat zo'r edi. Boshqa tomondan, xizmat sekin edi."),
            new CuratedGrammarExample("To sum up, the project was a success.", "Xulosa qilib aytganda, loyiha muvaffaqiyatli bo'ldi."),
        },
        new[]
        {
            "however/therefore ikki gapni oddiy vergul bilan bog'lay olmaydi - nuqta yoki nuqtali vergul kerak.",
            "«however» (biroq) va «moreover» (bundan tashqari) ni ma'nosiga qarab tanlang.",
            "Bog'lovchi iborani gap boshida vergul bilan ajrating.",
        });

    // ───────────────────────────── C2 - Advanced Inversion ─────────────────────────────
    private static readonly CuratedGrammarLesson AdvancedInversion = new(
        "advanced-inversion",
        "Murakkab inversiya - advanced inversion",
        "C2 darajada inversiya nafaqat urg'u, balki nozik uslubiy va ritorik effekt uchun ishlatiladi: " +
        "shartli inversiya, «not until», «only by», «so/such ... that» va adabiy o'rin/holat inversiyasi.",
        new[]
        {
            "Only + ... + yordamchi + ega  →  Only later did I realize the truth.",
            "Not until ... + yordamchi + ega  →  Not until midnight did she return.",
            "So/Such ... that (gap boshida)  →  So rarely does it happen that ...",
            "Shartli: Were it not for ... / Had it not been for ...",
        },
        new[]
        {
            new CuratedGrammarRule(
                "«Only» iboralari bilan",
                "Only after, only when, only by, only then gap boshida bo'lsa, asosiy gapda inversiya yuz " +
                "beradi: Only by working hard will you succeed (urg'u - only by qismida)."),
            new CuratedGrammarRule(
                "«Not until / Not since»",
                "Vaqt cheklovini ta'kidlaydi: Not until I saw it myself did I believe it " +
                "(O'z ko'zim bilan ko'rmagunimcha ishonmadim)."),
            new CuratedGrammarRule(
                "Shartli inversiya - rasmiy",
                "If tushirilib, were/had/should oldinga chiqadi: Were it not for your help, we would have " +
                "failed (= If it were not for...); Had it not been for the rain..."),
            new CuratedGrammarRule(
                "Uslubiy maqsad",
                "C2 da bu tuzilmalar adabiy, notiqlik va rasmiy yozuvda ohang, dramatiklik va " +
                "ravonlik beradi - ortiqcha ishlatilsa sun'iy tuyuladi."),
        },
        new[]
        {
            new CuratedGrammarExample("Only after the meeting did I understand the plan.", "Faqat yig'ilishdan keyingina rejani tushundim."),
            new CuratedGrammarExample("Not until she apologized did he forgive her.", "U kechirim so'ramaguncha, u uni kechirmadi."),
            new CuratedGrammarExample("Were it not for you, I would be lost.", "Sen bo'lmaganingda, men adashib qolardim."),
            new CuratedGrammarExample("Had it not been for the delay, we would have arrived on time.", "Kechikish bo'lmaganida, o'z vaqtida yetib kelardik."),
            new CuratedGrammarExample("So complex was the problem that no one could solve it.", "Muammo shu qadar murakkab ediki, uni hech kim hal qila olmadi."),
        },
        new[]
        {
            "«Only when» dan keyin asosiy gapda inversiya kerak: «Only when he left I relaxed» emas, «...did I relax».",
            "Shartli inversiyada «if» qaytarmang: «Were it not if...» emas, «Were it not for...».",
            "Inversiyani me'yorida ishlating - har gapda emas, uslubiy zarurat bo'lganda.",
        });

    // ───────────────────────────── C2 - Fronting and Emphasis ─────────────────────────────
    private static readonly CuratedGrammarLesson FrontingAndEmphasis = new(
        "fronting-and-emphasis",
        "Old o'ringa chiqarish va urg'u - fronting",
        "Fronting - odatda gap o'rtasida yoki oxirida keladigan bo'lakni gap boshiga chiqarib, unga urg'u " +
        "berish yoki fikrlar oqimini bog'lash usuli. Bu uslubiy tanlov bo'lib, ma'noni kuchaytiradi.",
        new[]
        {
            "To'ldiruvchini oldinga: That idea I really like.",
            "Holatni oldinga: In the corner stood an old clock. (o'rin inversiyasi bilan)",
            "Sifatdosh/sifatni oldinga: Exhausted, she collapsed.",
            "So ... / Such ... bilan urg'u",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Nima uchun fronting",
                "Yangi yoki muhim ma'lumotni ta'kidlash va matnni oldingi fikrga bog'lash uchun: " +
                "His behaviour I cannot accept (uning xatti-harakatini - aynan shunga urg'u)."),
            new CuratedGrammarRule(
                "O'rin/holat fronting va inversiya",
                "O'rin holati oldinga chiqsa, ko'pincha ega va fe'l o'rin almashadi: Down the street came " +
                "a parade; Here comes the bus. (olmosh ega bo'lsa inversiya bo'lmaydi: Here it comes.)"),
            new CuratedGrammarRule(
                "Sifatdosh va sifat fronting",
                "Holat yoki sababni ixcham ifodalaydi: Tired but happy, they went home; " +
                "More important is the question of cost."),
            new CuratedGrammarRule(
                "Inversiyadan farqi",
                "Har fronting inversiya talab qilmaydi: oddiy to'ldiruvchi fronting da tartib o'zgarmaydi " +
                "(That film I loved), faqat salbiy/o'rin holatlarida inversiya bo'ladi."),
        },
        new[]
        {
            new CuratedGrammarExample("That kind of behaviour I will not tolerate.", "Bunday xatti-harakatga toqat qilmayman."),
            new CuratedGrammarExample("In the distance stood a lonely house.", "Uzoqda yolg'iz uy turardi."),
            new CuratedGrammarExample("Exhausted, the runners crossed the finish line.", "Holdan toygan yuguruvchilar marra chizig'ini kesib o'tishdi."),
            new CuratedGrammarExample("Here comes the train.", "Mana, poyezd kelyapti."),
            new CuratedGrammarExample("Strange as it may seem, he refused.", "G'alati tuyulsa-da, u rad etdi."),
        },
        new[]
        {
            "Olmosh ega bo'lsa o'rin inversiyasi bo'lmaydi: «Here comes it» emas, «Here it comes».",
            "Fronting urg'u uchun - ortiqcha ishlatilsa nutq sun'iy tuyuladi.",
            "To'ldiruvchi fronting da odatda inversiya kerak emas: «That film loved I» emas.",
        });

    // ───────────────────────────── C2 - Cohesion Devices ─────────────────────────────
    private static readonly CuratedGrammarLesson CohesionDevices = new(
        "cohesion-devices",
        "Matnni bog'lash vositalari - cohesion devices",
        "Bog'lash vositalari matn bo'laklarini bir-biriga ulab, uni yaxlit va izchil qiladi: olmoshlar bilan " +
        "havola (reference), so'z almashtirish (substitution), tushirish (ellipsis) va leksik bog'lanish.",
        new[]
        {
            "Reference: it, this, that, these, such - oldingi fikrga ishora",
            "Substitution: one, do so, the same - takrorni almashtirish",
            "Lexical cohesion: sinonim, takror, umumiy so'z bilan bog'lash",
            "Conjunction: bog'lovchi iboralar bilan mantiqiy aloqa",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Reference (havola)",
                "Olmosh va ko'rsatkichlar oldin aytilganga ishora qiladi va takrorni oldini oladi: " +
                "The government introduced a new law. This caused protests. («This» butun oldingi fikrga)."),
            new CuratedGrammarRule(
                "Substitution va ellipsis",
                "one/do so/the same so'zlar bilan almashtirish yoki tushirish nutqni ixcham qiladi: " +
                "Some people support the plan; others do not (= do not support the plan)."),
            new CuratedGrammarRule(
                "Lexical cohesion",
                "Bir mavzuni sinonim, umumlashtiruvchi so'z yoki takror orqali bog'lash: car ... vehicle ... " +
                "it. Bu matnni mavzu atrofida ushlab turadi."),
            new CuratedGrammarRule(
                "Maqsadi - yaxlitlik",
                "Bu vositalar gaplar to'plamini izchil matnga aylantiradi; ularsiz matn uzuq-yuluq " +
                "va kitobxonga og'ir tuyuladi."),
        },
        new[]
        {
            new CuratedGrammarExample("Prices rose sharply. This worried investors.", "Narxlar keskin ko'tarildi. Bu investorlarni xavotirga soldi."),
            new CuratedGrammarExample("She offered to help, and he did the same.", "U yordam taklif qildi, u ham xuddi shunday qildi."),
            new CuratedGrammarExample("Some agreed; others did not.", "Ba'zilar rozi bo'ldi; boshqalari yo'q."),
            new CuratedGrammarExample("The results were poor. Such an outcome was unexpected.", "Natijalar yomon edi. Bunday yakun kutilmagan edi."),
            new CuratedGrammarExample("He bought a new phone. The device was expensive.", "U yangi telefon sotib oldi. Qurilma qimmat edi."),
        },
        new[]
        {
            "Havola so'zi (this/it) aniq nimaga ishora qilayotgani noaniq bo'lmasligi kerak.",
            "Substitution ni to'g'ri ishlating: «others do not support» o'rniga «others do not» yetarli.",
            "Bog'lash vositalarisiz matn uzuq bo'ladi - ularni izchil qo'llang.",
        });

    // ───────────────────────────── C2 - Hedging Language ─────────────────────────────
    private static readonly CuratedGrammarLesson HedgingLanguage = new(
        "hedging-language",
        "Ehtiyotkor til - hedging",
        "Hedging - fikrni ehtiyotkorlik bilan, qat'iy da'vo qilmasdan ifodalash. Akademik va rasmiy " +
        "uslubda muhim: u mualliflni haddan ortiq qat'iylikdan va keyinchalik xato chiqishidan saqlaydi.",
        new[]
        {
            "Modallar: may, might, could, would  →  This may suggest that ...",
            "Fe'llar: seem, appear, tend to, suggest, indicate",
            "Ravishlar: possibly, perhaps, arguably, apparently, relatively",
            "Iboralar: It is likely that ... / to some extent / in general",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Nima uchun hedging",
                "Da'voni yumshatib, uni ehtimol darajasida bayon qiladi: «Smoking causes cancer» o'rniga " +
                "«Smoking may contribute to cancer». Bu akademik xolislikni ko'rsatadi."),
            new CuratedGrammarRule(
                "Vositalar turlari",
                "Modal fe'llar (may, might, could), ehtiyot fe'llari (seem, appear, tend to), " +
                "ehtimol ravishlari (perhaps, possibly, arguably) va miqdor cheklovchilari (some, many, " +
                "often) ishlatiladi."),
            new CuratedGrammarRule(
                "Daraja va miqdor cheklovi",
                "«to some extent», «relatively», «in most cases» kabi iboralar umumlashtirishni " +
                "yumshatadi: Results were, to some extent, positive."),
            new CuratedGrammarRule(
                "Me'yor muhim",
                "Haddan ortiq hedging fikrni kuchsiz va ishonchsiz qiladi; yetarli hedging esa ilmiy va " +
                "professional ohang beradi - muvozanat kerak."),
        },
        new[]
        {
            new CuratedGrammarExample("The data may suggest a link between the two.", "Ma'lumotlar ikkalasi o'rtasida bog'liqlik borligini ko'rsatishi mumkin."),
            new CuratedGrammarExample("This appears to be the main cause.", "Bu asosiy sababga o'xshaydi."),
            new CuratedGrammarExample("It is likely that prices will rise.", "Narxlar ko'tarilishi ehtimoli yuqori."),
            new CuratedGrammarExample("The results are, to some extent, inconclusive.", "Natijalar ma'lum darajada noaniq."),
            new CuratedGrammarExample("Such behaviour tends to occur in groups.", "Bunday xatti-harakat ko'pincha guruhlarda kuzatiladi."),
        },
        new[]
        {
            "Qat'iy da'vodan saqlaning: «This proves...» o'rniga «This may suggest...».",
            "Haddan ortiq hedging fikrni kuchsizlantiradi - me'yorni saqlang.",
            "Modal va ehtiyot fe'llarini birga ortiqcha yig'maganga harakat qiling: «may possibly seem» og'ir.",
        });

    // ───────────────────────────── C2 - Emphatic Structures ─────────────────────────────
    private static readonly CuratedGrammarLesson EmphaticStructures = new(
        "emphatic-structures",
        "Urg'uli tuzilmalar - do/did emphasis",
        "Urg'uli tuzilmalar gapdagi ma'lum bir fikrni kuchaytiradi: yordamchi «do/does/did», «it is ... that» " +
        "cleft, takror va «the very / at all» kabi kuchaytiruvchi so'zlar orqali.",
        new[]
        {
            "do/does/did + asosiy fe'l (tasdiqni kuchaytirish)  →  I do believe you.",
            "It is/was ... that ... (cleft urg'u)  →  It was you that I trusted.",
            "Kuchaytiruvchilar: the very, at all, whatsoever, indeed",
            "Takror va so/such ... that",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Emphatic «do»",
                "Oddiy tasdiq gapga «do/does/did» qo'shib, fikrni qat'iy ta'kidlaydi yoki shubhaga javob " +
                "beradi: I did lock the door (rostdan ham qulfladim - ishonmagan odamga). Asosiy fe'l " +
                "o'zak shaklda qoladi: «did locked» emas."),
            new CuratedGrammarRule(
                "Cleft bilan urg'u",
                "It is/was ... that/who ... aniq bir bo'lakni ajratib ta'kidlaydi: It was on Monday that " +
                "she arrived (aynan dushanba kuni)."),
            new CuratedGrammarRule(
                "Kuchaytiruvchi so'zlar",
                "the very (the very thought - aynan o'sha fikr), at all (inkorda: not at all), " +
                "whatsoever (no doubt whatsoever), indeed (very good indeed)."),
            new CuratedGrammarRule(
                "Maqsadi va ohang",
                "Bu tuzilmalar his-tuyg'u, ishonch yoki qarama-qarshilikni ifodalaydi; nutq va " +
                "yozuvda kuchli ta'sir beradi."),
        },
        new[]
        {
            new CuratedGrammarExample("I do appreciate your help.", "Yordamingizni chindan ham qadrlayman."),
            new CuratedGrammarExample("She did warn us, but we didn't listen.", "U bizni rostdan ogohlantirgan edi, lekin biz quloq solmadik."),
            new CuratedGrammarExample("It was his honesty that impressed me.", "Meni hayratlantirgani uning halolligi edi."),
            new CuratedGrammarExample("The very idea makes me nervous.", "Shu fikrning o'zi meni asabiylashtiradi."),
            new CuratedGrammarExample("I have no doubt whatsoever.", "Menda hech qanday shubha yo'q."),
        },
        new[]
        {
            "Emphatic «do» dan keyin o'zak fe'l: «I did locked» emas, «I did lock».",
            "«do» ni faqat oddiy zamonlarda ishlating - «I do am» yoki «I do can» noto'g'ri.",
            "Urg'uli tuzilmalarni ortiqcha ishlatmang - ta'siri kamayadi.",
        });

    // ───────────────────────────── C2 - Complex Reporting ─────────────────────────────
    private static readonly CuratedGrammarLesson ComplexReporting = new(
        "complex-reporting",
        "Murakkab o'zlashtirma gap - complex reporting",
        "C2 darajada o'zlashtirma gap shunchaki zamon surishdan ko'ra ko'proq narsa: turli reporting fe'llari " +
        "nutqning maqsadi va ohangini (tan olish, ogohlantirish, taklif) aniq yetkazadi va nozik ma'no beradi.",
        new[]
        {
            "Fe'l + that-gap: claim, admit, deny, suggest, point out",
            "Fe'l + somebody + to-infinitive: advise, warn, urge, encourage",
            "Fe'l + -ing: admit, deny, suggest, recommend  →  He admitted cheating.",
            "Fe'l + preposition + -ing: accuse of, apologize for, insist on",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Reporting fe'lini tanlash",
                "Oddiy «say/tell» o'rniga maqsadni aniqlaydigan fe'l: claim (asossiz da'vo), admit " +
                "(tan olish), deny (rad etish), warn (ogohlantirish), insist (qat'iy turish). " +
                "Bu fe'l nutq maqsadini bitta so'zda beradi."),
            new CuratedGrammarRule(
                "Har fe'lning tuzilishi",
                "Har reporting fe'li o'z naqshini oladi: suggest + that/-ing (suggest that we go / suggest " +
                "going), advise + somebody + to (advised me to wait), accuse + of + -ing (accused him of " +
                "lying). Naqsh yodlanadi."),
            new CuratedGrammarRule(
                "Zamon surilishi nozikliklari",
                "Umumiy haqiqat yoki hali ham amaldagi holat saqlanishi mumkin: He said the Earth is round; " +
                "She said she works here (hali ishlaydi). Surilish majburiy emas, mantiqdan kelib chiqadi."),
            new CuratedGrammarRule(
                "Ohang va munosabat",
                "Reporting fe'li so'zlovchining munosabatini ham ko'rsatadi: «claimed» shubha, «admitted» " +
                "ayb, «pointed out» asoslangan dalil ohangini beradi."),
        },
        new[]
        {
            new CuratedGrammarExample("He admitted making a mistake.", "U xato qilganini tan oldi."),
            new CuratedGrammarExample("She denied taking the money.", "U pulni olganini rad etdi."),
            new CuratedGrammarExample("They accused him of cheating.", "Ular uni aldovda aybladilar."),
            new CuratedGrammarExample("The doctor advised me to rest.", "Shifokor menga dam olishni maslahat berdi."),
            new CuratedGrammarExample("He pointed out that the data was incomplete.", "U ma'lumotlar to'liq emasligini ta'kidladi."),
        },
        new[]
        {
            "Har reporting fe'lining naqshini to'g'ri ishlating: «suggest me to go» emas, «suggest that I go».",
            "«accuse» predlog oladi: «accused him to lie» emas, «accused him of lying».",
            "Doimiy haqiqatda zamonni majburan surmang: «He said the Earth was round» shart emas.",
        });

    // ───────────────────────────── C2 - Concessive Clauses ─────────────────────────────
    private static readonly CuratedGrammarLesson ConcessiveClauses = new(
        "concessive-clauses",
        "Qarama-qarshilik gaplari - although, despite, however...",
        "Konsessiv (yon berish) gaplar kutilgan natijaga zid holatni ifodalaydi: bir narsaga qaramay, " +
        "ikkinchisi baribir yuz beradi. O'zbekchadagi «-ga qaramay, bo'lsa-da» ma'nosiga to'g'ri keladi.",
        new[]
        {
            "although / though / even though + gap  →  Although it rained, we went out.",
            "despite / in spite of + ot/-ing  →  Despite the rain, we went out.",
            "however / no matter how + sifat/ravish  →  However hard he tried, he failed.",
            "Inversiyali: Much as I like it ... / Try as he might ...",
        },
        new[]
        {
            new CuratedGrammarRule(
                "although va despite farqi",
                "although/though + to'liq gap (ega + fe'l): Although he was tired... " +
                "despite/in spite of + ot yoki -ing: Despite being tired... / Despite his tiredness... " +
                "«despite of» noto'g'ri."),
            new CuratedGrammarRule(
                "however / no matter",
                "«however + sifat/ravish» yoki «no matter how/what/who» kutilgan natijani inkor qiladi: " +
                "However rich you are, money can't buy happiness; No matter what happens, I'll support you."),
            new CuratedGrammarRule(
                "even though - kuchaytirilgan",
                "«even though» «although» dan kuchliroq qarama-qarshilikni ta'kidlaydi: " +
                "Even though she apologized, he was still angry."),
            new CuratedGrammarRule(
                "Adabiy inversiyali shakl",
                "C2 da nozik uslub: Much as I respect him, I disagree; Try as she might, she couldn't open " +
                "it; Strange though it may seem..."),
        },
        new[]
        {
            new CuratedGrammarExample("Although she was tired, she kept working.", "Charchagan bo'lsa-da, u ishlashda davom etdi."),
            new CuratedGrammarExample("Despite the heavy traffic, we arrived on time.", "Tirbandlikka qaramay, o'z vaqtida yetib keldik."),
            new CuratedGrammarExample("However hard I try, I can't convince him.", "Qanchalik urinmay, uni ishontira olmayman."),
            new CuratedGrammarExample("No matter what you say, I won't change my mind.", "Nima desangiz ham, fikrimni o'zgartirmayman."),
            new CuratedGrammarExample("Much as I love the city, I need a break.", "Shaharni qanchalik sevsam-da, menga dam kerak."),
        },
        new[]
        {
            "«despite» dan keyin «of» qo'ymang: «despite of the rain» emas, «despite the rain».",
            "«although» (gap) va «despite» (ot/-ing) ni almashtirmang: «although the rain» emas.",
            "«however» (qarama-qarshilik) va «however» (bog'lovchi ravish) ni kontekstda ajrating.",
        });

    // ───────────────────────────── C2 - Idiomatic Modality ─────────────────────────────
    private static readonly CuratedGrammarLesson IdiomaticModality = new(
        "idiomatic-modality",
        "Idiomatik modallik - idiomatic modality",
        "Idiomatik modallik - modal ma'noni an'anaviy modal fe'llardan tashqari, idiomatik ibora va " +
        "tuzilmalar bilan ifodalash: «be bound to», «be supposed to», «had better», «may well», «would sooner».",
        new[]
        {
            "be bound to - muqarrar  →  Prices are bound to rise.",
            "be supposed to - kutilgan/majbur  →  You're supposed to wear a tie.",
            "had better - maslahat/ogohlantirish  →  You'd better leave now.",
            "may/might (just) as well - boshqa yaxshiroq variant yo'q  →  We may as well start.",
        },
        new[]
        {
            new CuratedGrammarRule(
                "Muqarrarlik va kutilma",
                "be bound to - deyarli muqarrar natija (He's bound to win). be supposed to - qoida yoki " +
                "umumiy kutilma (You're supposed to check in early); ko'pincha bu kutilma bajarilmaganini " +
                "bildiradi (I was supposed to call him - qilmadim)."),
            new CuratedGrammarRule(
                "Maslahat va afzallik",
                "had better - kuchli maslahat yoki ogohlantirish (You'd better see a doctor - aks holda " +
                "yomon bo'ladi). would rather / would sooner - afzallik (I'd sooner stay home)."),
            new CuratedGrammarRule(
                "«may well» va «as well»",
                "may/might well - yuqori ehtimol (You may well be right). may/might as well - yaxshiroq " +
                "variant yo'qligidan biror ishni qilish (There's nothing on TV; we might as well go out)."),
            new CuratedGrammarRule(
                "Tuzilish nozikliklari",
                "had better dan keyin o'zak fe'l (had better go, not «to go»); be supposed/bound to dan " +
                "keyin to-infinitive. Bu naqshlar yodlanadi."),
        },
        new[]
        {
            new CuratedGrammarExample("With his talent, he's bound to succeed.", "Iste'dodi bilan u albatta muvaffaqiyatga erishadi."),
            new CuratedGrammarExample("You're supposed to register before Friday.", "Juma kunigacha ro'yxatdan o'tishingiz kerak edi."),
            new CuratedGrammarExample("You'd better apologize before it's too late.", "Kech bo'lmasidan kechirim so'raganing ma'qul."),
            new CuratedGrammarExample("It's raining, so we might as well stay in.", "Yomg'ir yog'yapti, shu sababli uyda qolganimiz ma'qul."),
            new CuratedGrammarExample("You may well be right about that.", "Bu borada haq bo'lishingiz ehtimoli katta."),
        },
        new[]
        {
            "«had better» dan keyin «to» qo'ymang: «had better to go» emas, «had better go».",
            "«be supposed to» dagi «to» ni tushirmang: «supposed wear» emas, «supposed to wear».",
            "«may well» (ehtimol) va «may as well» (ma'qul) ni ma'nosiga qarab ajrating.",
        });

    // ───────────────────────────── C2 - Register and Formality ─────────────────────────────
    private static readonly CuratedGrammarLesson RegisterAndFormality = new(
        "register-and-formality",
        "Uslub va rasmiylik darajasi - register and formality",
        "Register - vaziyatga mos til tanlash: rasmiy yoki norasmiy, yozma yoki og'zaki. C2 darajada " +
        "so'z tanlash, grammatik tuzilma va ohangni auditoriya va maqsadga moslash mahorati talab qilinadi.",
        new[]
        {
            "Rasmiy ≈ lotin asosli so'z: obtain, require, assist, commence",
            "Norasmiy ≈ phrasal verb / oddiy so'z: get, need, help, start",
            "Rasmiy: to'liq shakl, passive, nominalizatsiya, hedging",
            "Norasmiy: qisqartma, phrasal verb, contraction, to'g'ridan murojaat",
        },
        new[]
        {
            new CuratedGrammarRule(
                "So'z tanlash (lexis)",
                "Rasmiy uslub lotin/fransuz asosli so'zlarni afzal ko'radi (purchase, assistance, " +
                "regarding), norasmiy uslub esa qisqa, anglo-sakson so'zlar va phrasal verblarni " +
                "(buy, help, about) ishlatadi."),
            new CuratedGrammarRule(
                "Grammatik belgilar",
                "Rasmiy: contraction yo'q (do not, cannot), passive va nominalizatsiya ko'p, " +
                "shaxssiz tuzilma (It is recommended that...). Norasmiy: contraction (don't), " +
                "to'g'ridan «you», elliptik gaplar."),
            new CuratedGrammarRule(
                "Ohang va murojaat",
                "Rasmiy yozuvda muloyim va xolis ohang (I would be grateful if you could...), " +
                "norasmiyda samimiy va to'g'ridan (Can you...? Thanks!). Auditoriyaga moslash kerak."),
            new CuratedGrammarRule(
                "Izchillik muhim",
                "Bitta matnda registerni aralashtirmaslik kerak: rasmiy xatda slang yoki kutilmagan " +
                "norasmiy ibora g'aliz tuyuladi va aksincha."),
        },
        new[]
        {
            new CuratedGrammarExample("We would be grateful if you could provide further details.", "Qo'shimcha tafsilotlar bersangiz, minnatdor bo'lardik."),
            new CuratedGrammarExample("Could you give me a hand with this?", "Bunda menga yordam bera olasizmi?"),
            new CuratedGrammarExample("Please find the report attached.", "Hisobot ilova qilingani bilan tanishib chiqing."),
            new CuratedGrammarExample("I'm really sorry, but I can't make it.", "Juda uzr, lekin kela olmayman."),
            new CuratedGrammarExample("The matter requires immediate attention.", "Bu masala zudlik bilan e'tibor talab qiladi."),
        },
        new[]
        {
            "Bitta matnda registerni aralashtirmang - rasmiy xatda slang g'aliz tuyuladi.",
            "Rasmiy uslubda contraction ishlatmang: «don't» o'rniga «do not».",
            "Auditoriya va maqsadni hisobga oling - register tanlovi shularga bog'liq.",
        });

    // Declared after the lessons so the static field initializers above have run (textual order),
    // and the analyzer sees each lesson as the non-null value it is.
    private static readonly IReadOnlyDictionary<string, CuratedGrammarLesson> Lessons =
        new[]
        {
            ToBe, PresentSimple, Articles, PluralNouns, ThereIsThereAre, PossessiveAdjectives,
            PrepositionsOfPlace, CanAbility, PresentContinuous, WhQuestions,
            PastSimple, Comparatives, Superlatives, GoingToFuture, AdverbsOfFrequency, CountableUncountable,
            PrepositionsOfTime, ObjectPronouns, PossessivePronouns, Imperatives,
            PresentPerfect, PastContinuous, FirstConditional, SecondConditional, GerundsAndInfinitives,
            WillFuture, ModalsOfObligation, DefiningRelativeClauses, UsedTo, ComparativeAdverbs,
            PresentPerfectContinuous, PastPerfect, ThirdConditional, PassiveVoice, ReportedSpeech,
            ModalsOfDeduction, NonDefiningRelativeClauses, FutureContinuous, WishClauses, CausativeHave,
            MixedConditionals, Inversion, CleftSentences, ParticipleClauses, AdvancedPassive,
            Subjunctive, FuturePerfect, EllipsisAndSubstitution, Nominalisation, DiscourseMarkers,
            HypotheticalMeaning, AdvancedInversion, FrontingAndEmphasis, CohesionDevices, HedgingLanguage,
            EmphaticStructures, ComplexReporting, ConcessiveClauses, IdiomaticModality, RegisterAndFormality,
        }
            .ToDictionary(l => l.FocusCode, l => l);
}
