import {
  MONTHLY_PRICE_UZS,
  QUARTERLY_DURATION_DAYS,
  QUARTERLY_PRICE_UZS,
  SEMI_ANNUAL_DURATION_DAYS,
  SEMI_ANNUAL_PRICE_UZS,
  YEARLY_DURATION_DAYS,
  YEARLY_PRICE_UZS,
  formatUzs,
  pricePerMonthUzs,
  savePercent,
} from "./pricing";

// Landing / startup explainer page content (uzcombinator.uz pitch + public marketing).
// docs/development-guide.md rule 11 / 17.4: every user-facing Uzbek string is a vetted, hand-written
// template here - never AI-generated at runtime. Module names (Speaking, Listening,
// Reading, Vocabulary, Grammar, Writing) stay in English exactly as the design system
// and PROJECT-SPEC use them. Facts (modules, architecture, pricing, moat, roadmap,
// metrics) are kept as reviewed product facts so the page stays truthful.

export const landing = {
  nav: {
    links: [
      { id: "muammo", label: "Muammo" },
      { id: "qanday", label: "Qanday ishlaydi" },
      { id: "modullar", label: "Modullar" },
      { id: "biznes", label: "Narxlar" },
      { id: "savol", label: "Savol-javob" },
    ],
    // Always "Kirish" - a signed-in user is still routed to /home, but the label stays
    // consistent for every visitor (no "Dashboardga o'tish" variant).
    cta: "Kirish",
    ctaAuthed: "Kirish",
  },

  hero: {
    badge: "AI bilan ingliz tili",
    title: "O'zbeklar uchun ingliz tilida haqiqatda gapira oladigan platforma",
    subtitle:
      "Tushunarli kirish, majburiy ishlab chiqarish va AI fikr-mulohaza " +
      "asosida qurilgan adaptiv platforma. Maqsad bitta: foydalanuvchini bilim to'plashdan " +
      "emas, gapirishdan to'xtatmaslik.",
    primaryCta: "Bepul boshlash",
    secondaryCta: "Qanday ishlashini ko'rish",
    stats: [
      { value: "6", label: "o'zaro bog'langan ko'nikma moduli" },
      { value: "A1–C2", label: "adaptiv CEFR darajalari" },
      { value: "3/7/21", label: "kunlik takrorlash jadvali" },
    ],
    mascotAlt: "To'tiqush maskoti",
  },

  // Why the product exists - the core insight behind the startup.
  problem: {
    eyebrow: "Nega bu kerak",
    title: "O'zbekistonda yillab o'rganadi, lekin gapira olmaydi",
    body:
      "Aksariyat o'rganuvchilar grammatika va lug'atni alohida-alohida yodlaydi. Bilim " +
      "parchalangan holda qoladi - test yechishga yetadi, lekin og'zaki nutqqa aylanmaydi. " +
      "Mavjud ilovalarning har biri bitta narsani yaxshi qiladi, qolganida zaif.",
    points: [
      {
        icon: "link_off",
        title: "Bog'lanmagan bilim",
        text: "Grammatika, lug'at va tinglash bir-biridan uzilgan - natija: passiv bilim, faol nutq yo'q.",
      },
      {
        icon: "translate",
        title: "Til ko'prigi yo'qoladi",
        text: "Xalqaro ilovalar ona tilini hisobga olmaydi; o'zbek o'rganuvchining aniq xatolari e'tibordan chetda qoladi.",
      },
      {
        icon: "sentiment_dissatisfied",
        title: "Motivatsiya so'nadi",
        text: "Gapirish amaliyoti qo'rqinchli va qimmat; tezda ko'rinadigan natija bo'lmasa, foydalanuvchi tashlab ketadi.",
      },
    ],
  },

  audience: {
    eyebrow: "Kim uchun",
    title: "Aniq auditoriya, aniq ehtiyoj",
    items: [
      {
        icon: "school",
        title: "Talabalar va o'quvchilar",
        text: "IELTS/CEFR ga tayyorlanayotgan, gapirish amaliyoti yetishmayotgan yoshlar.",
      },
      {
        icon: "work",
        title: "Mutaxassislar",
        text: "IT, eksport va xizmat sohasidagi, ish uchun ingliz tili talab qilinadigan kishilar.",
      },
      {
        icon: "groups",
        title: "Til markazlari (B2B)",
        text: "O'quvchilariga darsdan tashqari mashq vositasi sifatida taklif qiladigan markazlar.",
      },
      {
        icon: "self_improvement",
        title: "Mustaqil o'rganuvchilar",
        text: "O'z sur'atida, har kuni 10–15 daqiqada barqaror odat shakllantirmoqchi bo'lganlar.",
      },
    ],
  },

  // The pedagogy: input -> output -> feedback -> spaced repetition, topic-centric.
  how: {
    eyebrow: "Qanday ishlaydi",
    title: "Bitta mavzu atrofida birlashgan o'rganish sikli",
    body:
      "Har bir mavzu (Topic) va uning lug'ati atrofida olti ko'nikma qattiq bog'lanadi. " +
      "Foydalanuvchi bir mavzuda so'z o'rgansa, keyingi har bir modulda aynan o'sha so'zlarni " +
      "yangi kontekstda qayta ko'radi va ishlatadi - bu i+1 (tushunarli kirish + bitta yangilik) " +
      "tamoyiliga asoslanadi.",
    steps: [
      {
        n: "01",
        icon: "input",
        title: "Tushunarli kirish",
        text: "Matn yoki audioning 80–90% tanish, 10–20% yangi - miya kontekstdan taxmin qilib o'rganadi.",
      },
      {
        n: "02",
        icon: "record_voice_over",
        title: "Majburiy ishlab chiqarish",
        text: "Gapirish va yozish orqali so'z faol ishlatiladi - passiv tanishdan faol nutqqa o'tadi.",
      },
      {
        n: "03",
        icon: "rate_review",
        title: "Darhol fikr-mulohaza",
        text: "AI talaffuz, grammatika va lug'at qamrovini baholaydi; o'zbekcha izoh shablon orqali beriladi.",
      },
      {
        n: "04",
        icon: "event_repeat",
        title: "Qat'iy takrorlash",
        text: "3/7/21 kun jadvali bo'yicha so'z unutilishdan oldin yangi kontekstda qayta sinaladi.",
      },
    ],
    flow: {
      title: "Topic-markazli yo'l",
      text: "Tartib tavsiya etiladi, lekin qulflanmaydi - foydalanuvchi xohlagan modulga o'tishi mumkin.",
      chain: ["Vocabulary", "Reading", "Writing", "Speaking", "Listening"],
    },
  },

  modules: {
    eyebrow: "Modullar",
    title: "Bitta ekotizimda barcha ko'nikmalar",
    items: [
      {
        icon: "forum",
        title: "Speaking",
        image: "/assets/modules/speaking.svg",
        text: "AI tutor bilan suhbat + Azure talaffuz baholash + viseme (og'iz harakati) vizual fikr-mulohaza.",
      },
      {
        icon: "hearing",
        title: "Listening",
        image: "/assets/modules/listening.svg",
        text: "CEFR darajalangan audio/video, interaktiv transkript va tushunish testlari.",
      },
      {
        icon: "menu_book",
        title: "Reading",
        image: "/assets/modules/reading.svg",
        text: "Darajalangan matn va graded readers - har biri tushunarlilik darajasi bo'yicha tekshirilgan.",
      },
      {
        icon: "style",
        title: "Vocabulary (SRS)",
        image: "/assets/modules/vocabulary.svg",
        text: "Kontekstdagi so'zlar + 3/7/21 kun faol takrorlash testlari, har safar yangi mashq turi.",
      },
      {
        icon: "rule",
        title: "Grammar",
        image: "/assets/modules/grammar.svg",
        text: "5 bosqichli kontekstli dars; o'zbeklarga xos xato chastotasi bo'yicha tartiblangan syllabus.",
      },
      {
        icon: "edit_note",
        title: "Writing",
        image: "/assets/modules/writing.svg",
        text: "4 + 1 o'lchovli AI baholash: vazifa, mantiq, lug'at, grammatika va mavzu lug'at qamrovi.",
      },
    ],
    extra: {
      title: "Qo'shimcha tizimlar",
      items: [
        { icon: "fact_check", text: "Adaptiv CEFR daraja aniqlash testi (Computer Adaptive Testing)" },
        { icon: "insights", text: "O'quvchi modeli: xato heatmap, ko'nikma balli, o'sish grafiklari" },
        { icon: "local_fire_department", text: "Gamifikatsiya: kunlik maqsad, streak va ixtiyoriy pul-garov mexanizmi" },
        { icon: "notifications_active", text: "Bildirishnoma tizimi va faolsiz foydalanuvchini qaytarish ketma-ketligi" },
      ],
    },
  },

  business: {
    eyebrow: "Biznes model",
    title: "Mintaqaga moslangan Freemium + B2B",
    tiers: [
      {
        name: "Free",
        price: "0 so'm",
        period: "doimiy",
        highlight: false,
        features: [
          "Birinchi 2 ta mavzu to'liq ochiq",
          "Daraja aniqlash testi",
          "Kuniga 1 ta Speaking sessiya (10 daqiqa)",
          "Kuniga 10 ta yangi SRS so'z",
          "Reklama bilan",
        ],
      },
      {
        name: "Premium",
        price: `${formatUzs(MONTHLY_PRICE_UZS)} so'mdan`,
        period: "oyiga",
        highlight: true,
        features: [
          "Barcha mavzular ochiq (A1–C2)",
          "Reklamasiz",
          "Cheksiz Speaking sessiyalar va SRS so'z",
          "Cheksiz Writing AI-baholash + to'liq viseme kutubxonasi",
          "Batafsil analitika va offline rejim",
          "Oylik, 3 oylik, 6 oylik yoki 1 yillik - quyida tanlang",
        ],
      },
      {
        name: "B2B",
        price: "Kelishuv asosida",
        period: "markaz / kompaniya",
        highlight: false,
        features: [
          "Til markazlari uchun guruh obunasi",
          "Korporativ (IT/eksport) paketlar",
          "O'qituvchi paneli (kelajakda)",
          "O'quvchi soniga ko'ra darajalangan narx",
          "Talaba chegirmasi (30–40%)",
        ],
      },
    ],
    note:
      `Premium: oyiga ${formatUzs(MONTHLY_PRICE_UZS)} so'm. Uzoqroq rejalar arzonroq - 1 yillik obunada ` +
      `oyiga ~${formatUzs(pricePerMonthUzs(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS))} so'mga tushadi ` +
      `(${savePercent(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS)}% chegirma). Narx O'zbekiston xarid qobiliyatiga moslangan - ` +
      "xalqaro ilovalardan past, AI xarajatini qoplaydigan darajada. To'lov: Click va Payme.",
    premium: {
      eyebrow: "Premium rejalar",
      title: "Premium rejani tanlang",
      subtitle:
        "Barcha rejalar bir xil to'liq imkoniyatni beradi - barcha mavzular, reklamasiz, " +
        "cheksiz Speaking va Writing. Qancha uzoq muddat tanlasangiz, oylik narx shuncha arzon.",
      plans: [
        {
          name: "Oylik",
          price: formatUzs(MONTHLY_PRICE_UZS),
          unit: "so'm",
          period: "1 oy",
          perMonth: `${formatUzs(MONTHLY_PRICE_UZS)} so'm / oy`,
          badge: "",
          save: "",
          highlight: false,
        },
        {
          name: "3 oylik",
          price: formatUzs(QUARTERLY_PRICE_UZS),
          unit: "so'm",
          period: "3 oy",
          perMonth: `≈ ${formatUzs(pricePerMonthUzs(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS))} so'm / oy`,
          badge: "",
          save: `${savePercent(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS)}% tejaysiz`,
          highlight: false,
        },
        {
          name: "6 oylik",
          price: formatUzs(SEMI_ANNUAL_PRICE_UZS),
          unit: "so'm",
          period: "6 oy",
          perMonth: `≈ ${formatUzs(pricePerMonthUzs(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS))} so'm / oy`,
          badge: "Ommabop",
          save: `${savePercent(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS)}% tejaysiz`,
          highlight: true,
        },
        {
          name: "1 yillik",
          price: formatUzs(YEARLY_PRICE_UZS),
          unit: "so'm",
          period: "12 oy",
          perMonth: `≈ ${formatUzs(pricePerMonthUzs(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS))} so'm / oy`,
          badge: "Eng foydali",
          save: `${savePercent(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS)}% tejaysiz`,
          highlight: false,
        },
      ],
      footnote: "To'lov Click yoki Payme orqali. Istalgan vaqtda bekor qilish mumkin.",
    },
  },

  roadmap: {
    eyebrow: "Yo'l xaritasi",
    title: "Bosqichma-bosqich qurilish rejasi",
    phases: [
      { n: "0", title: "Scaffolding + CEFR daraja testi", done: true },
      { n: "1", title: "Speaking yadro (STT + talaffuz + viseme)", done: true },
      { n: "2", title: "O'quvchi modeli + analitika", done: true },
      { n: "3", title: "Lug'at (SRS) + bildirishnoma", done: true },
      { n: "4", title: "Video / Listening", done: true },
      { n: "5", title: "Gamifikatsiya + Obuna/To'lov", done: false },
      { n: "6", title: "Reading + Grammar + Writing", done: true },
      { n: "7", title: "Retention va o'sish (MVP'dan keyin)", done: true },
    ],
  },

  metrics: {
    eyebrow: "O'lchovlar",
    title: "Muvaffaqiyatni qanday o'lchaymiz",
    items: [
      { value: "D1 / D7 / D30", label: "retention - odat shakllanishi va product-market fit signali" },
      { value: "CEFR o'sish", label: "foydalanuvchi necha oyda bir daraja oshadi - eng muhim metrika" },
      { value: "Streak mediani", label: "majburlash mexanizmi qanchalik ishlayotgani" },
      { value: "Premium %", label: "monetizatsiya konversiya samaradorligi" },
    ],
  },

  // Public FAQ - mirrors the FAQPage JSON-LD in index.html so the visible page and the
  // structured data stay in sync. Every answer is a vetted template (docs/development-guide.md rule 11).
  faq: {
    eyebrow: "Savol-javob",
    title: "Ko'p beriladigan savollar",
    body: "Boshlashdan oldin ko'p so'raladigan savollarga qisqa javoblar.",
    items: [
      {
        q: "Bu qanday platforma?",
        a: "O'zbeklar uchun sun'iy intellektga asoslangan ingliz tili platformasi. AI tutor bilan gapirishni mashq qilasiz; CEFR A1–C2 darajalari, lug'at, grammatika, tinglash, o'qish va yozish bir joyda.",
      },
      {
        q: "Bepul boshlash mumkinmi?",
        a: "Ha. Birinchi 2 ta mavzu, daraja aniqlash testi va kunlik mashq bepul. Barcha mavzular va cheksiz sessiyalar uchun Premium obuna mavjud.",
      },
      {
        q: "Platforma qanday ishlaydi?",
        a: "Tushunarli kirish, majburiy ishlab chiqarish (gapirish va yozish) va darhol AI fikr-mulohaza asosida. Har bir mavzu atrofida oltita ko'nikma o'zaro bog'lanadi.",
      },
      {
        q: "Qaysi darajalar uchun mos?",
        a: "Boshlang'ich (A1) dan ilg'or (C2) gacha. Daraja aniqlash testi sizni to'g'ri darajaga joylashtiradi, keyin kontent siz o'sgan sari qiyinlashadi.",
      },
      {
        q: "Telefonda ishlaydimi?",
        a: "Ha. Brauzerda ochiladi va Android ilovasi mavjud - mikrofon orqali gapirish mashqlari telefonda ham to'liq ishlaydi.",
      },
      {
        q: "To'lov qanday amalga oshiriladi?",
        a: "Premium obuna Click yoki Payme orqali to'lanadi. Oylik, 3 oylik, 6 oylik yoki 1 yillik rejalardan birini tanlaysiz; istalgan vaqtda bekor qilish mumkin.",
      },
    ],
  },

  cta: {
    title: "Ingliz tilida gapirishni bugun boshlang",
    subtitle: "Daraja aniqlash testidan o'ting va birinchi mavzuingizni AI bilan suhbatda mustahkamlang.",
    primary: "Bepul boshlash",
    secondary: "Ilovaga kirish",
  },

  // Android ilovasini yuklab olish bo'limi. Fayl `public/downloads/englishai.apk` da joylashadi
  // va to'g'ridan-to'g'ri yuklab olinadi (Google Play talab qilinmaydi).
  androidApp: {
    eyebrow: "Android ilovasi",
    title: "Telefoningizga o'rnating",
    body: "Ilovani Android telefoningizga bevosita yuklab oling - barcha modullar, bildirishnomalar va AI suhbat cho'ntagingizda.",
    download: "APK yuklab olish",
    hint: "Yuklab olgach, faylni oching va \"Noma'lum manbalar\"ga ruxsat bering.",
    version: "So'nggi versiya",
  },

  footer: {
    tagline: "O'zbeklar uchun ingliz tilini o'zlashtirish platformasi.",
    rights: "Barcha huquqlar himoyalangan.",
  },
} as const;
