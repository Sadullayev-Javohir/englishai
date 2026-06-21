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
import seo from "./seo.json";

// Uzbek content store (docs/development-guide.md rule 11 / 17.4): every user-facing Uzbek string lives
// here as a vetted template, never hardcoded in components. Text was lifted from the
// Stitch design exports (design-export/screens/*.html) and corrected for spelling,
// grammar and consistent terminology. Module names (Speaking, Video, Vocabulary,
// Reading, Grammar, Writing) are kept in English exactly as the design system uses them.
//
// Backend-provided Uzbek text (feedback, hints, notification messages, recommendations)
// already comes from the server-side content layer (Resources/uz/*.json) - those strings
// are rendered as received and are not duplicated here.

export const uz = {
  brand: "EnglishAI.uz",
  // Shared logout label (AdminShell header menu). The profile page has its own copy at
  // profile.logout for its settings-list phrasing.
  logout: "Chiqish",

  common: {
    start: "Boshlash",
    next: "Keyingi",
    continue: "Davom etish",
    back: "Orqaga",
    backHome: "Bosh sahifaga qaytish",
    skip: "O'tkazib yuborish",
    loading: "Yuklanmoqda...",
    retry: "Qayta urinish",
    error: "Xatolik yuz berdi. Birozdan so'ng qayta urinib ko'ring.",
    close: "Yopish",
    seeAll: "Hammasini ko'rish",
    minutesShort: "daqiqa",
  },

  // Global "no internet" dialog (design: englishai.pen, screens 81 WEB/MOBILE).
  // The dialog reassures the learner that nothing was consumed, because losing
  // the connection mid-tap must never look like a lost attempt or lost energy.
  offline: {
    eyebrow: "ULANISH UZILDI",
    title: "Internet aloqasi yo'q",
    text: "Wi-Fi yoki mobil internetni tekshirib, qayta urinib ko'ring.",
    noticeTitle: "Mashq boshlanmadi.",
    noticeText: "Energiya sarflanmadi.",
    retry: "Qayta urinish",
    dismiss: "Hozir emas",
    hint: "Ulanish tiklanganda xabar beramiz.",
    close: "Oynani yopish",
    checking: {
      title: "Tekshirilmoqda...",
      text: "Qayta urinish tugmasi vaqtincha faol emas.",
    },
    stillOffline: {
      title: "Hali ham internetga ulanib bo'lmadi.",
      text: "Ulanishni tekshiring va yana urinib ko'ring.",
    },
    reconnected: {
      title: "Ulanish tiklandi",
      text: "Mashqni boshlashingiz mumkin.",
    },
  },

  lessonGuidance: {
    title: "Bu bosqichda nima qilasiz?",
    close: "Ko'rsatmalarni yopish",
    open: "Ko'rsatmalarni ochish",
    vocabulary: {
      text: [
        "Matnni diqqat bilan o'qing va yangi so'zlarning gapda qanday ishlatilganini kuzating.",
        "Ajratilgan so'z ustiga kompyuterda sichqonchani olib boring yoki telefon va planshetda bosing — tarjima va talaffuz yordami ochiladi.",
        "So'zni bossangiz, misol va batafsil ma'lumot oynasi ochiladi.",
      ],
      words: [
        "Kartani bosib aylantiring: bir tomonida tarjima, ikkinchi tomonida inglizcha so'z, talaffuz va misol bor.",
        "So'zni ovoz chiqarib ayting yoki talaffuz mashqini oching.",
        "Tayyor bo'lsangiz, keyingi so'zga o'ting.",
      ],
      practice: [
        "Savolga mos javobni tanlang yoki so'zni yozing.",
        "Javob tekshirilgach, natija va izohni ko'rib chiqing.",
        "Xato bo'lsa, to'g'ri javobni eslab keyingi mashqqa o'ting.",
      ],
    },
    grammar: {
      context: [
        "Matnni o'qing va grammatika qoidasi qayerda ishlatilganini kuzating.",
        "Ajratilgan so'zni kompyuterda hover qiling yoki touch qurilmada bosing — lug'at yordami ochiladi.",
      ],
      rule: [
        "Qoida tuzilishini va qachon ishlatilishini o'qing.",
        "Misollarni qoida bilan solishtiring, keyin keyingi bosqichga o'ting.",
      ],
      example: [
        "Misollarda qoida qanday ishlatilganini toping.",
        "Gap tuzilishiga e'tibor bering va o'zingiz bir misol o'ylab ko'ring.",
      ],
      quiz: [
        "Savol uchun bitta javobni tanlang.",
        "Natija va izoh chiqqach, keyingi savolga o'ting.",
      ],
    },
    listening: {
      audio: [
        "Audioni diqqat bilan tinglang; tushunmagan joyingizni qayta eshitishingiz mumkin.",
        "Audio tinglangach test tugmasi faollashadi.",
      ],
      practice: [
        "Javobni audioda eshitgan ma'lumotga asoslanib tanlang.",
        "Tekshiruvdan keyin izohni ko'rib, keyingi savolga o'ting.",
      ],
    },
    reading: {
      text: [
        "Avval matnning umumiy mazmunini tushunib o'qing.",
        "Ajratilgan so'zni kompyuterda hover qiling yoki touch qurilmada bosing — tarjima va batafsil lug'at yordami ochiladi.",
      ],
      translate: [
        "Inglizcha matnni so'zlar tarjimasi bilan solishtiring.",
        "Tushunilmagan gaplarni qayta o'qib, keyin savollarga o'ting.",
      ],
      practice: [
        "Javobni faqat o'qilgan matndagi ma'lumotga asoslab tanlang.",
        "Feedbackni o'qing va keyingi savolga o'ting.",
      ],
    },
    writing: {
      guidance: [
        "Topshiriq va yozuvda bo'lishi kerak bo'lgan fikrlarni o'qing.",
        "Tayyor matnni ko'chirmang; ko'rsatmalardan reja sifatida foydalaning.",
      ],
      editor: [
        "Javobni ingliz tilida yozing va tavsiya etilgan so'z oralig'iga rioya qiling.",
        "Tayyor bo'lgach yuboring — AI ball, xatolar va yaxshilash tavsiyalarini ko'rsatadi.",
      ],
      result: [
        "Umumiy ball, xatolar va tavsiyalarni ko'rib chiqing.",
        "Kerak bo'lsa topshiriqni qayta yozib, natijani yaxshilang.",
      ],
    },
    speaking: {
      live: [
        "AI savolini tinglang, mikrofonni bosib ingliz tilida javob bering.",
        "Gapirishni tugatgach yozishni to'xtating va AI javobini kuting.",
        "Fikr kartalari yordam beradi, lekin ularni aynan takrorlash shart emas.",
      ],
    },
    books: {
      intro: [
        "Bo'limni o'qing, keyin qisqa savollarga javob bering.",
        "Natija saqlangach, navbatdagi bo'lim ochiladi.",
      ],
      read: [
        "Matnni oxirigacha o'qing va muhim voqealarni eslab qoling.",
        "Tayyor bo'lsangiz, savollar bosqichiga o'ting.",
      ],
      quiz: [
        "Har savolga bittadan javob bering.",
        "Feedbackni ko'rib chiqing; barcha savollar tugagach bo'lim natijasi saqlanadi.",
      ],
    },
    video: {
      player: [
        "Markazdagi tugma bilan videoni boshlating yoki pauza qiling.",
        "Subtitrdagi inglizcha so'zni bossangiz, tarjima va batafsil ma'lumot ochiladi.",
        "Transcript satrini bossangiz, video shu joyga o'tadi.",
      ],
      quiz: [
        "Javobni videoda ko'rgan va eshitgan ma'lumotga asoslanib tanlang.",
        "Izoh chiqqach keyingi savolga o'ting; yakunda natija saqlanadi.",
      ],
    },
  },

  notFound: {
    title: "Sahifa topilmadi",
    text: "Manzil noto'g'ri yoki sahifa ko'chirilgan bo'lishi mumkin.",
    back: "Oldingi sahifaga qaytish",
    home: "Asosiy sahifaga o'tish",
  },

  // Day/night (light/dark) theme toggle.
  theme: {
    switchToDark: "Tungi rejim",
    switchToLight: "Kunduzgi rejim",
    label: "Ko'rinish",
    light: "Kunduzgi",
    dark: "Tungi",
    system: "Tizim bo'yicha",
  },

  // On-demand sentence translation modal (click/hover an English sentence to see its meaning).
  // The Uzbek translation itself is produced by translating the real English source (rule 11).
  translate: {
    title: "O'zbekcha tarjima",
    hint: "Tarjima uchun bosing",
    unavailable: "Tarjima hozircha mavjud emas.",
    close: "Yopish",
  },

  assistant: {
    title: "AI Yordamchi",
    open: "AI Yordamchi",
    close: "Yopish",
    kicker: "EnglishAI yordamchisi",
    online: "Onlayn",
    triggerHint: "Savolingizni yozing",
    welcomeTitle: "Bugun nimani o'rganamiz?",
    intro: "So'z tarjimasi, grammatika yoki gapdagi qo'llanish bo'yicha savolingizni yozing.",
    empty: "Ingliz tili bo'yicha savolingizni yozing.",
    placeholder: "Ingliz tili bo'yicha savolingiz...",
    send: "Yuborish",
    thinking: "Javob tayyorlanmoqda...",
    unavailable: "Javob olinmadi. Savolingiz chatda saqlandi, qayta urinib ko'ring.",
    retry: "Qayta urinish",
    charsLabel: "belgi",
    capabilitiesLabel: "Yordamchi imkoniyatlari",
    capabilityTranslate: "Tarjima",
    capabilityGrammar: "Grammatika",
    capabilityExamples: "Misollar",
    keyboardHint: "Enter — yuborish, Shift + Enter — yangi qator",
    resize: "Yordamchi oynasi balandligini o'zgartirish",
    videoMode: "Video yordamchisi",
    writingMode: "Writing yordamchisi",
    speakingMode: "Speaking yordamchisi",
  },

  nav: {
    home: "Asosiy",
    levels: "Darajalar",
    speaking: "Speaking",
    video: "Video",
    vocabulary: "Vocabulary",
    reading: "Reading",
    grammar: "Grammar",
    writing: "Writing",
    listening: "Listening",
    progress: "Progress",
    leaderboard: "Reyting",
    profile: "Profil",
    admin: "Admin",
    notifications: "Bildirishnomalar",
    saved: "Mening so'zlarim",
    // Compact mobile labels
    learn: "O'rganish",
    practice: "Mashq",
    lessons: "Darslar",
    // Menu button aria-label (hamburger / nav menu)
    menu: "Menyu",
    // Left sidebar (desktop, only on /home per the redesign brief): the primary
    // sections - Home, Yo'l xaritasi (level roadmap), Leaderboard.
    sidebar: {
      heading: "O'quv bo'limlari",
      home: "Asosiy",
      roadmap: "Yo'l xaritasi",
      competition: "Musobaqa",
      leaderboard: "Reyting",
      books: "Kitoblar",
      speaking: "Gapirish",
      growth: "O'sish jarayoni",
      // Admin entry - shown only to admins; links to the operator panel hub.
      admin: "Admin",
      // Footer: the signed-in user's username.
      signedInAs: "Tizimga kirgan",
    },
  },

  // Mobile "Darslar" bottom-sheet: the central tab opens this grid of skill modules. Each
  // entry pairs the English module name (kept per the design system) with a short Uzbek hint.
  lessonsSheet: {
    title: "Darslar",
    subtitle: "Mavzu bo'yicha barcha ko'nikmalar",
    items: {
      vocabulary: "So'z boyligi - kontekstda yangi so'zlar",
      grammar: "Grammatika - qoidalar va mashqlar",
      writing: "Yozuv - insho va baholash",
      speaking: "Nutq - AI bilan suhbat",
      listening: "Tinglash - audio mashqlar",
      reading: "O'qish - matnlar va savollar",
      video: "Video - interaktiv transkript bilan",
    },
  },

  welcome: {
    eyebrow: "2-bosqich · Daraja",
    title: "O'quv yo'lingizni to'g'ri nuqtadan boshlang",
    tagline: "Ingliz tilini oson va qiziqarli o'rganing",
    chooseHint: "Qayerdan boshlamoqchisiz?",
    determineLevel: "Darajamni aniqlash",
    determineLevelHint: "Qisqa test orqali darajangizni aniqlaymiz",
    startFromA1: "A1 darajadan boshlash",
    startFromA1Hint: "Boshlang'ich darajadan boshlab o'rganaman",
    startingA1: "A1 daraja sozlanmoqda...",
    startError: "Darajani saqlab bo'lmadi. Internetni tekshirib, qayta urinib ko'ring.",
    featureTutorTitle: "Aqlli AI Tutor",
    featureTutorText: "Shaxsiy o'quv dasturi faqat siz uchun.",
    featureStreakTitle: "Kunlik streak",
    featureStreakText: "Har kuni o'rganing va natijaga erishing.",
    trust: "o'quvchi bizga ishonadi",
  },

  // Global HUD stats (jungle gamification redesign - rule 11: all copy here).
  hud: {
    streak: "Ketma-ket kun",
    xp: "Tajriba balli (XP)",
    energy: "Energiya balansi - Video yoki Speaking boshlanganda 1 ta sarflanadi, har 36 daqiqada 1 ta tiklanadi",
    coins: "Chegirma tangasi - Pro chegirma kuponiga almashtiriladi",
    gems: "Chegirma tangasi",
    league: "Liga",
    hearts: "Yuraklar",
    heartsLabel: "Yuraklar",
    profile: "Profil",
  },

  energy: {
    eyebrow: "ENERGIYA",
    emptyTitle: "Energiya tugadi",
    restoredTitle: (current: number) => `${current} ta energiya tiklandi`,
    fullTitle: "Energiya to'liq tiklandi!",
    emptyText: "Video va Speakingni boshlash uchun energiya kerak. To'liq tiklanish — 3 soat, har 36 daqiqada +1 energiya.",
    restoredText: (current: number) => `Kutish shart emas: hozir ${current} ta Video yoki Speaking mashqini boshlashingiz mumkin. Qolgan energiya ham tiklanishda davom etadi.`,
    fullText: "Barcha 5 ta energiya tayyor. Video va Speaking mashqlarini davom ettiring. Har bir yangi boshlash −1 energiya.",
    oneAtATime: "Bir donadan tiklanadi. 5/5 da to'xtaydi.",
    nextRefill: "Keyingi +1 energiya",
    fullRefill: "To'liq 5/5 bo'lishiga",
    restorationFinished: "Tiklanish yakunlandi",
    startNew: "Yangi mashqni boshlang",
    ready: "Tayyor",
    resumeVideo: "Videoni ochish · −1 energiya",
    resumeSpeaking: "Suhbatni boshlash · −1 energiya",
    alternative: "Boshqa mashqlarni bajarish",
    gotIt: "Tushunarli",
    close: "Yopish",
    hint: "Ilovadan chiqsangiz ham tiklanish davom etadi.",
  },

  // League / leaderboard tiers (jungle redesign).
  league: {
    title: "Liga",
    short: "Bronza",
    bronze: "Bronza",
    silver: "Kumush",
    gold: "Oltin",
    platinum: "Platina",
    diamond: "Olmos",
  },

  // Public marketing landing page - mirrors the marketing site at englishai.uz.
  marketing: {
    nav: {
      problem: "Muammo",
      how: "Qanday ishlaydi",
      modules: "Modullar",
      pricing: "Narxlar",
      faq: "Savol-javob",
      cta: "Bepul boshlash",
    },
    heroBadge: "O'yin kabi qiziqarli, real natija bilan",
    heroTitle: "O'zbeklar uchun ingliz tilida haqiqatda gapira oladigan platforma",
    heroSubtitle:
      "Tushunarli kirish, majburiy ishlab chiqarish va AI fikr-mulohaza asosida qurilgan adaptiv platforma. Maqsad bitta: foydalanuvchini bilim to'plashdan emas, gapirishdan to'xtatmaslik.",
    cta: "Bepul boshlash",
    ctaSecondary: "Qanday ishlashini ko'rish",
    ctaReturning: "Davom etish",
    loopAriaLabel: "Olti ko'nikmali o'rganish sikli",
    loopEyebrow: "Bitta mavzu - olti faol ko'nikma",
    loopTitle: "Yangi so'zni bilib qolmaysiz - uni gapda ishlatasiz",
    loopLead:
      "Har bir mavzudagi so'zlar keyingi mashqlarda qayta ishlatiladi. Shu sabab lug'at, grammatika va gapirish alohida darslar emas, bitta uzluksiz yo'l bo'ladi.",
    loopSteps: [
      { icon: "menu_book", title: "Lug'at", text: "Mavzuga kerakli so'zlarni kontekstda o'rganing." },
      { icon: "rule", title: "Grammatika", text: "Shu so'zlar bilan to'g'ri gap tuzing." },
      { icon: "auto_stories", title: "O'qish", text: "Ularni tabiiy matnda yana uchrating." },
      { icon: "edit", title: "Yozish", text: "Fikringizni mustaqil yozib ifodalang." },
      { icon: "mic", title: "Gapirish", text: "AI tutor bilan ovozli suhbat qiling." },
      { icon: "headphones", title: "Tinglash", text: "Tirik nutqda so'zlarni taniy boshlang." },
    ],
    proofEyebrow: "Platformaning yuragi",
    proofTitle: "Natija beradigan uchta mexanizm",
    proofLead: "Ko'p funksiya emas, foydalanuvchini faol nutqqa olib boradigan aniq tizim.",
    proofCards: [
      { icon: "forum", title: "AI bilan haqiqiy suhbat", text: "Mavzu bo'yicha erkin gapiring, javob va tuzatishni darhol oling." },
      { icon: "mic", title: "Talaffuzni ko'rib tuzating", text: "So'z bahosi, tovush va og'iz harakati orqali xatoni tushuning." },
      { icon: "repeat", title: "Unutishdan oldin takrorlang", text: "3/7/21 kunlik faol testlar so'zni uzoq muddatli xotiraga o'tkazadi." },
    ],
    stats: [
      { value: "6", label: "o'zaro bog'langan ko'nikma moduli" },
      { value: "A1–C2", label: "adaptiv CEFR darajalari" },
      { value: "3/7/21", label: "kunlik takrorlash jadvali" },
    ],
    socialProof: {
      registeredUsers: "ro'yxatdan o'tgan o'quvchi",
      activePremiumUsers: "faol Premium foydalanuvchi",
      activeLearners30d: "oxirgi 30 kundagi faol o'quvchi",
      totalStudyMinutes: "daqiqa o'qish va mashq",
    },

    // ── Problem section ──
    problemEyebrow: "Nega bu kerak",
    problemTitle: "O'zbekistonda yillab o'rganadi, lekin gapira olmaydi",
    problemLead:
      "Aksariyat o'rganuvchilar grammatika va lug'atni alohida-alohida yodlaydi. Bilim parchalangan holda qoladi - test yechishga yetadi, lekin og'zaki nutqqa aylanmaydi. Mavjud ilovalarning har biri bitta narsani yaxshi qiladi, qolganida zaif.",
    problems: [
      {
        icon: "extension",
        title: "Bog'lanmagan bilim",
        text: "Grammatika, lug'at va tinglash bir-biridan uzilgan - natija: passiv bilim, faol nutq yo'q.",
      },
      {
        icon: "translate",
        title: "Til ko'prigi yo'qoladi",
        text: "Xalqaro ilovalar ona tilini hisobga olmaydi; o'zbek o'rganuvchining aniq xatolari e'tibordan chetda qoladi.",
      },
      {
        icon: "flame",
        title: "Motivatsiya so'nadi",
        text: "Gapirish amaliyoti qo'rqinchli va qimmat; tezda ko'rinadigan natija bo'lmasa, foydalanuvchi tashlab ketadi.",
      },
    ],

    // ── Audience section ──
    audienceEyebrow: "Kim uchun",
    audienceTitle: "Aniq auditoriya, aniq ehtiyoj",
    audiences: [
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
        icon: "hub",
        title: "Til markazlari (B2B)",
        text: "O'quvchilariga darsdan tashqari mashq vositasi sifatida taklif qiladigan markazlar.",
      },
      {
        icon: "trending_up",
        title: "Mustaqil o'rganuvchilar",
        text: "O'z sur'atida, har kuni 10–15 daqiqada barqaror odat shakllantirmoqchi bo'lganlar.",
      },
    ],

    // ── How it works ──
    howEyebrow: "Qanday ishlaydi",
    howTitle: "Bitta mavzu atrofida birlashgan o'rganish sikli",
    howLead:
      "Har bir mavzu (Topic) va uning lug'ati atrofida olti ko'nikma qattiq bog'lanadi. Foydalanuvchi bir mavzuda so'z o'rgansa, keyingi har bir modulda aynan o'sha so'zlarni yangi kontekstda qayta ko'radi va ishlatadi - bu i+1 (tushunarli kirish + bitta yangilik) tamoyiliga asoslanadi.",
    steps: [
      {
        num: "01",
        icon: "menu_book",
        title: "Tushunarli kirish",
        text: "Matn yoki audioning 80–90% tanish, 10–20% yangi - miya kontekstdan taxmin qilib o'rganadi.",
      },
      {
        num: "02",
        icon: "edit",
        title: "Majburiy ishlab chiqarish",
        text: "Gapirish va yozish orqali so'z faol ishlatiladi - passiv tanishdan faol nutqqa o'tadi.",
      },
      {
        num: "03",
        icon: "sparkles",
        title: "Darhol fikr-mulohaza",
        text: "AI talaffuz, grammatika va lug'at qamrovini baholaydi; o'zbekcha izoh shablon orqali beriladi.",
      },
      {
        num: "04",
        icon: "repeat",
        title: "Qat'iy takrorlash",
        text: "3/7/21 kun jadvali bo'yicha so'z unutilishdan oldin yangi kontekstda qayta sinaladi.",
      },
    ],
    topicNote:
      "Topic-markazli yo'l: tartib tavsiya etiladi, lekin qulflanmaydi - foydalanuvchi xohlagan modulga o'tishi mumkin.",

    // ── Modules ──
    modulesEyebrow: "Modullar",
    modulesTitle: "Bitta ekotizimda barcha ko'nikmalar",
    modules: [
      {
        icon: "mic",
        title: "Speaking",
        text: "AI tutor bilan suhbat + Azure talaffuz baholash + viseme (og'iz harakati) vizual fikr-mulohaza.",
        accent: "green",
      },
      {
        icon: "headphones",
        title: "Listening",
        text: "CEFR darajalangan audio/video, interaktiv transkript va tushunish testlari.",
        accent: "blue",
      },
      {
        icon: "menu_book",
        title: "Reading",
        text: "Darajalangan matn va graded readers - har biri tushunarlilik darajasi bo'yicha tekshirilgan.",
        accent: "purple",
      },
      {
        icon: "auto_stories",
        title: "Vocabulary (SRS)",
        text: "Kontekstdagi so'zlar + 3/7/21 kun faol takrorlash testlari, har safar yangi mashq turi.",
        accent: "teal",
      },
      {
        icon: "rule",
        title: "Grammar",
        text: "5 bosqichli kontekstli dars; o'zbeklarga xos xato chastotasi bo'yicha tartiblangan syllabus.",
        accent: "amber",
      },
      {
        icon: "edit",
        title: "Writing",
        text: "4 + 1 o'lchovli AI baholash: vazifa, mantiq, lug'at, grammatika va mavzu lug'at qamrovi.",
        accent: "red",
      },
    ],
    systemsTitle: "Qo'shimcha tizimlar",
    systems: [
      "Adaptiv CEFR daraja aniqlash testi (Computer Adaptive Testing)",
      "O'quvchi modeli: xato heatmap, ko'nikma balli, o'sish grafiklari",
      "Gamifikatsiya: kunlik maqsad, streak va ixtiyoriy pul-garov mexanizmi",
      "Bildirishnoma tizimi va faolsiz foydalanuvchini qaytarish ketma-ketligi",
    ],

    // ── Pricing ──
    pricingEyebrow: "Biznes model",
    pricingTitle: "Mintaqaga moslangan Freemium + B2B",
    pricingLead:
      "Bepul reja hozir ishlaydi. Premium imkoniyatlari tayyor, Click va Payme orqali to'lov esa ulanish jarayonida.",
    pricingAvailable: "Hozir faol",
    pricingComingSoon: "Tez orada",
    pricingFreeCta: "Bepul boshlash",
    pricingPremiumCta: "Premium haqida bilish",
    pricingBusinessCta: "Bog'lanish uchun kirish",
    // Narxlar `content/pricing.ts` dan keladi - u yagona frontend manbasi va test orqali
    // backenddagi SubscriptionPricing.cs konstantalariga bog'langan. Bu yerga raqam yozmang.
    plans: [
      {
        name: "Free",
        price: "0 so'm",
        cadence: "doimiy",
        accent: "green",
        highlight: false,
        badge: "",
        features: [
          "Birinchi 2 ta mavzu to'liq ochiq",
          "Daraja aniqlash testi",
          "Kuniga 5 daqiqa AI tutor bilan gapirish",
          "Kuniga 10 ta AI yordamchi savoli",
          "Kuniga 10 ta yangi SRS so'z va 1 ta Writing baholash",
          "Lug'at, grammatika, o'qish, tinglash - cheksiz",
        ],
      },
      {
        name: "Premium",
        price: `${formatUzs(MONTHLY_PRICE_UZS)} so'mdan`,
        cadence: "oyiga",
        accent: "purple",
        highlight: true,
        badge: "Eng foydali",
        features: [
          "Barcha mavzular ochiq (A1–C2)",
          "Kuniga 20 daqiqa AI tutor bilan gapirish",
          "Azure talaffuz baholash - har bir tovush bo'yicha",
          "Cheksiz SRS so'z va Writing AI-baholash",
          "Batafsil analitika va to'liq viseme kutubxonasi",
          "Oylik, 3 oylik, 6 oylik yoki 1 yillik - quyida tanlang",
        ],
      },
    ],
    premiumNote:
      `Premium: oyiga ${formatUzs(MONTHLY_PRICE_UZS)} so'm - til markazidan bir necha barobar arzon. ` +
      `Uzoqroq rejalar arzonroq: 1 yillik obunada oyiga ~${formatUzs(pricePerMonthUzs(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS))} so'mga tushadi ` +
      `(${savePercent(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS)}% chegirma).`,
    premiumPlansTitle: "Premium rejani tanlang",
    premiumPlansLead:
      "Barcha rejalar bir xil to'liq imkoniyatni beradi - barcha mavzular, kuniga 20 daqiqa gapirish, talaffuz baholash, cheksiz SRS va Writing. Qancha uzoq muddat tanlasangiz, oylik narx shuncha arzon.",
    premiumPlans: [
      {
        name: "Oylik",
        price: formatUzs(MONTHLY_PRICE_UZS),
        unit: "so'm / 1 oy",
        perMonth: `${formatUzs(MONTHLY_PRICE_UZS)} so'm / oy`,
        save: "",
        tag: "",
      },
      {
        name: "3 oylik",
        price: formatUzs(QUARTERLY_PRICE_UZS),
        unit: "so'm / 3 oy",
        perMonth: `≈ ${formatUzs(pricePerMonthUzs(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS))} so'm / oy`,
        save: `${savePercent(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS)}% tejaysiz`,
        tag: "",
      },
      {
        name: "6 oylik",
        price: formatUzs(SEMI_ANNUAL_PRICE_UZS),
        unit: "so'm / 6 oy",
        perMonth: `≈ ${formatUzs(pricePerMonthUzs(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS))} so'm / oy`,
        save: `${savePercent(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS)}% tejaysiz`,
        tag: "Ommabop",
      },
      {
        name: "1 yillik",
        price: formatUzs(YEARLY_PRICE_UZS),
        unit: "so'm / 12 oy",
        perMonth: `≈ ${formatUzs(pricePerMonthUzs(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS))} so'm / oy`,
        save: `${savePercent(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS)}% tejaysiz`,
        tag: "Eng foydali",
      },
    ],
    premiumPayNote: "Premium rejalar narxi oldindan ko'rsatilgan. Click va Payme checkout ishga tushgach tanlash mumkin bo'ladi.",

    // ── Mobile apps / final CTA ──
    appsTitle: "Mobil ilovalar",
    appsText:
      "Ilovani telefoningizga o'rnating - barcha modullar, bildirishnomalar va AI suhbat cho'ntagingizda.",
    androidButton: "Android uchun · Play Market",
    androidSoon: "Tez orada",
    androidHint: "Tez orada Play Market orqali mavjud bo'ladi.",
    iosButton: "iOS uchun · App Store",
    iosSoon: "Tez orada",
    iosHint: "Tez orada App Store orqali mavjud bo'ladi.",
    appsDownloadNote:
      "Hozircha dasturni bevosita APK fayli orqali yuklab olishingiz mumkin.",
    appsDownloadButton: "APK yuklab olish",
    appsDownloadHint:
      "APK faylini yuklab olgach, uni oching va \"Noma'lum manbalar\"ga ruxsat bering.",

    // ── FAQ ──
    faqEyebrow: "Savol-javob",
    faqTitle: "Ko'p beriladigan savollar",
    faqLead: "Boshlashdan oldin ko'p so'raladigan savollarga qisqa javoblar.",
    faqs: seo.faq.map(({ question, answer }) => ({ q: question, a: answer })),

    // ── Closing CTA ──
    closingTitle: "Ingliz tilida gapirishni bugun boshlang",
    closingText:
      "Daraja aniqlash testidan o'ting va birinchi mavzuingizni AI bilan suhbatda mustahkamlang.",
    closingPrimary: "Bepul boshlash",
    closingSecondary: "Ilovaga kirish",

    // ── Footer ──
    footerTagline:
      "O'zbeklar uchun ingliz tilini o'zlashtirish platformasi.",
    footerRights: "Barcha huquqlar himoyalangan.",
  },

  heroCampaign: {
    homeAria: "EnglishAI.uz bosh sahifasi",
    eyebrow: "O'zbeklar uchun AI speaking platformasi",
    titlePrefix: "Bilishdan",
    titleAccent: "gapirishga",
    titleSuffix: "o'ting",
    lead:
      "Kursda bilim olasiz. EnglishAI'da esa o'sha bilimni AI tutor bilan real suhbatda ishlatasiz - xatodan qo'rqmasdan, har kuni.",
    start: "Bepul boshlash",
    continue: "Darsni davom ettirish",
    secondary: "Nega ishlashini ko'ring",
    proofAria: "EnglishAI asosiy imkoniyatlari",
    proofs: [
      { icon: "record_voice_over", title: "AI bilan gaplashing", text: "24/7 sabrli tutor bilan ovozli suhbat." },
      { icon: "graphic_eq", title: "Talaffuzni tuzating", text: "Har bir so'zga aniq baho va o'zbekcha izoh." },
      { icon: "menu_book", title: "So'zni unutmang", text: "3/7/21 SRS takrori bilimni xotirada saqlaydi." },
      { icon: "school", title: "A1 dan C2 gacha", text: "Darajangizga mos adaptiv o'quv yo'li." },
    ],
    whyEyebrow: "Muammo bilimda emas",
    whyTitle: "Gapirish faqat gapirish orqali o'rganiladi",
    whyLead:
      "EnglishAI passiv bilimni faol nutqqa aylantirish uchun uchta mexanizmni bitta oqimga birlashtiradi.",
    reasons: [
      { icon: "forum", number: "01", title: "Haqiqiy suhbat", text: "AI tutor savol beradi, javobingizni tinglaydi va suhbatni tabiiy davom ettiradi." },
      { icon: "mic", number: "02", title: "Darhol feedback", text: "Talaffuz va gap tuzilishidagi xatoni ayni paytda ko'rasiz va qayta aytasiz." },
      { icon: "translate", number: "03", title: "O'zbekcha ko'prik", text: "Tushuntirishlar o'zbek o'rganuvchining odatiy xatolariga moslashtirilgan." },
    ],
    stats: [
      { value: "A1–C2", label: "barcha darajalar" },
      { value: "10 daqiqa", label: "kunlik faol mashq" },
      { value: "24/7", label: "AI tutor mavjud" },
    ],
    finalTitle: "Birinchi suhbatni bugun boshlang",
    finalText: "Darajangizni aniqlang va yangi so'zlarni darhol AI suhbatida ishlating.",
  },

  // Goal-based onboarding: one screen asking why the learner is studying English. Shown once,
  // right after handle setup and before the level-choice flow.
  onboardingGoal: {
    title: "Ingliz tilini nima uchun o'rganyapsiz?",
    subtitle: "Maqsadingizga qarab sizga mos mavzularni tanlab beramiz",
    skip: "Hozircha o'tkazib turaman",
    saving: "Saqlanmoqda...",
    error: "Saqlashda xatolik. Qayta urinib ko'ring.",
    goals: {
      ieltsCefr: {
        title: "IELTS / CEFR imtihoni",
        hint: "Rasmiy imtihon yoki sertifikatga tayyorgarlik",
      },
      work: {
        title: "Ish / karyera uchun",
        hint: "Ish joyida va suhbatlarda ishonchli ingliz tili",
      },
      migration: {
        title: "Migratsiya / work visa",
        hint: "Chet elga ko'chish va rasmiy hujjatlar uchun",
      },
      travel: {
        title: "Sayohat uchun",
        hint: "Sayohatda erkin muloqot qilish",
      },
      generalSpeaking: {
        title: "Umumiy speaking",
        hint: "Kundalik suhbatlarda ravon gapirish",
      },
      school: {
        title: "Maktab / universitet",
        hint: "O'quv dasturi va imtihonlarda muvaffaqiyat",
      },
    },
    // Short labels reused on the home strip heading and the profile goal section.
    labels: {
      unspecified: "Belgilanmagan",
      ieltsCefr: "IELTS / CEFR",
      work: "Ish / karyera",
      migration: "Migratsiya",
      travel: "Sayohat",
      generalSpeaking: "Umumiy speaking",
      school: "Maktab / universitet",
    },
  },

  // Home "for your goal" recommended-topics strip (goal-based onboarding).
  goalTopics: {
    heading: "Maqsadingiz uchun",
    subheading: "Sizning maqsadingizga mos mavzular",
    relevant: "Maqsadingizga mos",
    empty: "Tavsiya etilgan mavzular topilmadi",
  },

  // Google-only sign-in screen (the single entry point to the app).
  auth: {
    homeAria: "EnglishAI.uz bosh sahifasi",
    eyebrow: "Xavfsiz va tez kirish",
    title: "Xush kelibsiz",
    subtitle: "Davom etish uchun Google hisobingiz bilan kiring",
    benefitsAria: "Kirishdan keyingi imkoniyatlar",
    benefitsTitle: "Bir hisob - butun o'quv yo'lingiz",
    benefits: [
      { icon: "map", title: "Darajangiz saqlanadi", text: "A1 dan C2 gacha progress barcha qurilmalarda davom etadi." },
      { icon: "record_voice_over", title: "AI tutor tayyor", text: "Speaking, talaffuz va yozish feedbacki bitta profilda." },
      { icon: "shield", title: "Google orqali xavfsiz", text: "Parol yaratmaysiz; kirish Google hisobingiz orqali himoyalanadi." },
    ],
    trustNote: "Emailingiz reklama uchun sotilmaydi. Istalgan vaqtda profilingizni o'chirishingiz mumkin.",
    continueWithGoogle: "Google bilan kirish",
    signingIn: "Kirilmoqda...",
    error: "Kirishda xatolik yuz berdi. Qayta urinib ko'ring.",
    networkError: "Internet aloqasini tekshirib, qayta urinib ko'ring.",
    googleRejected: "Google hisobini tasdiqlab bo'lmadi. Hisobni qayta tanlab ko'ring.",
    googleCancelled: "Google orqali kirish bekor qilindi.",
    googleTimeout: "Google orqali kirish vaqti tugadi. Qayta urinib ko‘ring.",
    googleSecurityError: "Google kirish javobi xavfsizlik tekshiruvidan o‘tmadi. Qayta urinib ko‘ring.",
    googleExchangeError: "Google hisobini ilovaga ulab bo‘lmadi. Internetni tekshirib, qayta urinib ko‘ring.",
    tooManyAttempts: "Juda ko'p urinish bo'ldi. Biroz kutib, qayta urinib ko'ring.",
    googleBlocked:
      "Google bilan kirish yuklanmadi. Brave Shields yoki kontent bloklagichda bu sayt uchun Google kirishiga ruxsat bering, so'ng qayta urinib ko'ring.",
    emailTaken:
      "Bu email manzili boshqa Google hisobiga bog'langan. Avval ro'yxatdan o'tgan Google hisobingiz bilan kiring.",
    notConfigured:
      "Google bilan kirish hozircha sozlanmagan. Administrator bilan bog'laning.",
    privacy:
      "Kirish orqali siz Google hisobingizdagi ism va elektron pochtangizdan foydalanishga rozilik bildirasiz.",
    googleAccount: "Google hisobi",
    referralToggle: "Taklif kodingiz bormi?",
    referralPrompt: "Do'stingiz yuborgan taklif kodini kiriting",
    referralPlaceholder: "Taklif kodi (masalan: AB12CD)",
    referralApply: "Qo'llash",
    referralApplied: "Kod qo'llanildi - kirgach bonus ikkalangizga beriladi",
    referralInvalid: "Kod noto'g'ri. 4–12 ta harf yoki raqam bo'lishi kerak.",
  },

  // Post-sign-up username (public handle) setup + validation messages (rule 11).
  username: {
    step: "1-bosqich · Profil",
    benefitsAria: "Foydalanuvchi nomi afzalliklari",
    setupTitle: "Foydalanuvchi nomingizni tanlang",
    setupSubtitle:
      "Bu sizning communitydagi noyob nomingiz. Keyinroq profildan o'zgartira olasiz.",
    benefits: [
      { icon: "badge", title: "Noyob nom", text: "Communitydagi shaxsiy manzilingiz - faqat sizniki." },
      { icon: "group", title: "Do'stlaringiz topadi", text: "Reyting va natijalaringiz shu nom bilan ko'rsatiladi." },
      { icon: "edit", title: "Keyin o'zgartirasiz", text: "Xohlagan paytingiz profil sozlamalaridan yangilang." },
    ],
    label: "Foydalanuvchi nomi",
    placeholder: "masalan: aziz_karimov",
    hint: "3–30 belgi. Faqat kichik harf, raqam, pastki chiziq (_) va nuqta (.).",
    checking: "Tekshirilmoqda...",
    available: "Bu nom bo'sh - egallashingiz mumkin",
    taken: "Bu nom band. Boshqasini tanlang.",
    tooShort: (min: number) => `Kamida ${min} ta belgi bo'lishi kerak.`,
    tooLong: (max: number) => `Ko'pi bilan ${max} ta belgi bo'lishi kerak.`,
    invalidChars: "Faqat kichik harf, raqam, _ va . ishlatish mumkin.",
    empty: "Foydalanuvchi nomini kiriting.",
    continue: "Davom etish",
    saving: "Saqlanmoqda...",
    saveError: "Saqlashda xatolik. Qayta urinib ko'ring.",
  },

  assessmentIntro: {
    eyebrow: "3-bosqich · Boshlang'ich daraja",
    title: "Darajangizni aniqlaymiz",
    subtitle: "O'quv dasturini sizga moslashtirishimiz uchun testdan o'ting.",
    pointQuickTitle: "12–18 daqiqa",
    pointQuickText: "Olti ko'nikmani xotirjam va to'liq tekshirasiz",
    pointAdaptiveTitle: "Moslashuvchan",
    pointAdaptiveText:
      "Savollar javoblaringizga qarab qiyinlashadi yoki osonlashadi",
    pointSkillTitle: "6 ta ko'nikma",
    pointSkillText: "Lug'at, grammatika, tinglash, o'qish, yozish va gapirish alohida baholanadi",
    startTest: "Testni boshlash",
    chooseA1: "A1 darajani tanlash",
    choosingA1: "A1 daraja sozlanmoqda...",
    startError: "Darajani saqlab bo'lmadi. Internetni tekshirib, qayta urinib ko'ring.",
    later: "A1 darajadan boshlayman",
  },

  placement: {
    questionProgress: (n: number, total: number) => `${n}-savol / ${total}`,
    stageProgress: (n: number, total: number) => `${n}/${total} ko'nikma`,
    checkAnswer: "Javobni tekshirish",
    submitAnswer: "Javobni yuborish",
    skipQuestion: "Savolni o'tkazib yuborish",
    correct: "To'g'ri!",
    incorrect: "Noto'g'ri",
    finishing: "Natija hisoblanmoqda...",
    // Xavfsiz test rejimi (fullscreen yoki kiosk) matnlari - qoida 11 bo'yicha hammasi shu yerda.
    secure: {
      gateTitle: "Darajani aniqlash",
      notice: "Test haqqoniy bo‘lishi uchun xavfsiz rejimda o‘tadi: ekran faqat testga qulflanadi. Boshqa tab yoki ilovaga o‘tsangiz ogohlantirilasiz - to‘rt marta takrorlansa test bekor qilinadi.",
      kioskHint: "Test tugaguncha boshqa ilovaga o‘tmaslikka harakat qiling. Klaviatura ochilishi va mikrofon so‘rovi ogohlantirish hisoblanmaydi.",
      startCta: "Testni boshlash",
      continueCta: "Testni davom ettirish",
      startError: "Xavfsiz rejimni yoqib bo‘lmadi. Sahifani yangilab, qayta urinib ko‘ring.",
      warningTitle: "Test oynasidan chiqdingiz",
      warningBody: "Bu ogohlantirish. Boshqa tab yoki ilovaga qayta-qayta o‘tsangiz test bekor qilinadi.",
      warningCta: "Testga qaytish",
      invalidatedTitle: "Test bekor qilindi",
      invalidatedBody: "Xavfsiz test rejimi bir necha marta buzildi. Testni yangidan boshlashingiz yoki hozircha A1 darajadan o‘qishni boshlab, testni keyinroq topshirishingiz mumkin.",
      invalidatedCta: "Ortga qaytish",
      invalidatedA1Cta: "A1 darajadan boshlash",
    },
    sessionExpiredTitle: "Test sessiyasi tugagan",
    sessionExpiredMessage: "Saqlangan test sessiyasini tiklab bo'lmadi. Javoblaringiz PostgreSQL'dagi asosiy ma'lumotlarga ta'sir qilmagan. Testni yangidan boshlang.",
    restartTest: "Testni qayta boshlash",
    resultTitle: "Sizning darajangiz",
    resultSubtitle: "Har bir ko'nikma bo'yicha o'quv dasturi natijangizga moslashtirildi.",
    overallScore: (score: number) => `Umumiy natija: ${score}%`,
    skillAverage: (score: number) => `Ko'nikmalar o'rtachasi: ${score}%`,
    estimatedWeeks: (weeks: number) => `Keyingi bosqichgacha taxminan ${weeks} hafta`,
    goToDashboard: "Bosh sahifaga o'tish",
    stageScores: "Bosqichlar bo'yicha natija",
    stageLabels: {
      vocabulary: "Lug'at",
      grammar: "Grammatika",
      listening: "Tinglab tushunish",
      reading: "O'qib tushunish",
      writing: "Yozish",
      speaking: "Gapirish",
    },
    listen: {
      instruction: "Audioni tinglang va savolga javob bering.",
      play: "Tinglash",
      pause: "To'xtatib turish",
      replay: "Qayta tinglash",
      loading: "Audio yuklanmoqda...",
      error: "Audioni yuklab bo'lmadi. Qayta urinib ko'ring.",
    },
    read: {
      passageLabel: "Matnni o'qing",
    },
    write: {
      instruction: "Quyidagi mavzuda qisqa matn yozing.",
      placeholder: "Javobingizni shu yerga yozing...",
      wordCount: (n: number) => `${n} so'z`,
      recommended: (min: number, max: number) => `Tavsiya: ${min}–${max} so'z`,
      wordLimit: (min: number, max: number | null) =>
        max == null ? `Kamida ${min} so'z yozing.` : `${min}–${max} so'z oralig'ida yozing.`,
      submit: "Yuborish va davom etish",
      tooShort: "Iltimos, kamida bir nechta gap yozing.",
      checking: "Baholanmoqda...",
    },
    speak: {
      instruction: "Mavzu bo'yicha ovoz yozib, gapiring.",
      record: "Yozishni boshlash",
      recording: "Yozilmoqda... to'xtatish uchun bosing",
      recordingTime: (seconds: number) => `Yozilmoqda: ${seconds} soniya. To'xtatish uchun bosing.`,
      ready: "Yozuv tayyor. Eshitib ko'ring yoki yuboring.",
      preparing: "Yozuv tayyorlanmoqda...",
      recordAgain: "Qayta yozish",
      checking: "Baholanmoqda...",
      micPermission:
        "Mikrofonga ruxsat berilmadi. Brauzer sozlamalarini tekshiring.",
      unsupported: "Bu brauzer ovoz yozishni qo'llab-quvvatlamaydi.",
      emptyAudio: "Ovoz yozilmadi. Mikrofonni tekshirib, qayta gapiring.",
      invalidAudio: "Audio tayyorlanmadi. Qayta yozib ko'ring.",
      noSpeech: "Ovoz aniqlanmadi. Mikrofonni yaqinroq tutib, qayta gapiring.",
      lowConfidence: "Gap yetarlicha aniq tanilmadi. Sekinroq va ravshanroq qayta gapiring.",
      serviceUnavailable: "Ovozni baholash xizmati vaqtincha ishlamayapti. Yozuv saqlandi, qayta yuboring.",
      submit: "Davom etish",
    },
  },

  home: {
    greeting: (name: string) => `Salom, ${name}!`,
    // No real profile name until auth lands; a warm, neutral term keeps the greeting
    // natural without inventing personal data.
    defaultName: "do'stim",
    // Small line above the greeting on the home dashboard.
    greetingKicker: "Bugun ham o'rganishda davom etamiz",
    levelPrefix: "Daraja",
    dailyPlanTitle: "Kunlik reja",
    dailyPlanText:
      "Bugun oltita ko'nikmani mashq qiling - har biri sizni gapirishga yaqinlashtiradi.",
    // 6-skill daily plan checklist.
    skillsLabel: "ko'nikma",
    dailyPlanProgress: (done: number, total: number) =>
      `${done}/${total} ko'nikma bajarildi`,
    allSkillsDone: "Bugungi reja bajarildi!",
    skillsLeft: (n: number) =>
      n === 1
        ? "Rejani yakunlash uchun yana 1 ta ko'nikma qoldi"
        : `Rejani yakunlash uchun yana ${n} ta ko'nikma qoldi`,
    topicSkillsDone: "Joriy mavzuning barcha ko'nikmalari bajarildi",
    topicSkillsLeft: (n: number) =>
      n === 1
        ? "mavzuni yakunlash uchun yana 1 ta ko'nikma qoldi"
        : `mavzuni yakunlash uchun yana ${n} ta ko'nikma qoldi`,
    chooseTopic: "O‘rganishni boshlash uchun roadmapdan mavzu tanlang",
    skillDone: "Bajarildi",
    celebrate: {
      title: "Ajoyib!",
      text: "Kunlik rejangizni tugatdingiz. Davom eting!",
    },
    goingWell: "Yaxshi ketyapsiz!",
    tasksLabel: "vazifa",
    tasksLeft: (n: number) =>
      n === 1
        ? "Maqsadga yetish uchun yana 1 ta vazifa qoldi"
        : `Maqsadga yetish uchun yana ${n} ta vazifa qoldi`,
    continueCta: "Mashqni davom ettirish",
    streakRiskTitle: "Ketma-ketlik xavf ostida",
    streakRiskText: "Bugun bitta mashqni bajaring va streakni saqlab qoling.",
    streakRiskCta: "Mashq qilish",
    dueReviewsTitle: "Takrorlash vaqti keldi",
    dueReviewsText: (n: number) =>
      n === 1
        ? "1 ta so'z takrorlashni kutmoqda"
        : `${n} ta so'z takrorlashni kutmoqda`,
    dueReviewsCta: "Takrorlash",
    modulesTitle: "O'quv modullari",
    // Lug'at vositalari - vocabulary sahifasidan ko'chirilgan doimiy kirish nuqtalari (saqlangan
    // so'zlar va SRS takrorlash) home sahifasida turadi.
    vocabToolsTitle: "Lug'atim",
    savedWordsCard: { title: "Mening so'zlarim", text: "Saqlangan so'zlar" },
    reviewCard: {
      title: "Talaffuz mashqi",
      text: "Speakingda xato aytilgan so'zlar",
    },
    // Small affordance label on the tappable vocabulary cards.
    openLabel: "Ochish",
    streakDays: (n: number) => `${n} kun`,
    streakLabel: "Ketma-ketlik",
    modules: {
      speaking: {
        title: "Speaking",
        text: "Gapirish mahoratingizni AI bilan mashq qiling.",
        cta: "Boshlash",
      },
      video: {
        title: "Video",
        text: "Qiziqarli videolardan yangi iboralarni o'rganing.",
        cta: "Ko'rish",
      },
      vocabulary: {
        title: "Vocabulary",
        text: "So'z boyligingizni oshiring.",
        cta: "Yodlash",
      },
      reading: {
        title: "Reading",
        text: "Matnlarni o'qing va tushunishni tekshiring.",
        cta: "O'qish",
      },
      books: {
        title: "Kutubxona",
        text: "Darajangizga mos kitoblarni o'qing va tushunishni tekshiring.",
        cta: "Kitob o'qish",
      },
      grammar: {
        title: "Grammar",
        text: "O'rgangan so'zlaringiz bilan grammatikani mustahkamlang.",
        cta: "Mashq qilish",
      },
      writing: {
        title: "Writing",
        text: "O'rgangan so'zlaringiz bilan insho yozing va AI fikrini oling.",
        cta: "Yozish",
      },
      listening: {
        title: "Listening",
        text: "Mavzu so'zlarini tinglab, tushunishni mashq qiling.",
        cta: "Tinglash",
      },
      // Speaking modulining ikki kirish nuqtasi endi home modullarida ham turadi: erkin suhbat
      // mavzulari va rolli suhbat (vaziyatli). Har biri /speaking'ning tegishli katalogiga olib boradi.
      roleplay: {
        title: "Rolli suhbat",
        text: "Vaziyat tanlang - AI bilan rol o'ynab suhbatlashing.",
        cta: "Rol o'ynash",
      },
    },
  },

  homeConceptLab: {
    navigatorLabel: "Bosh sahifa dizaynlarini solishtirish",
    directLabel: "Dizayn variantlari",
    previous: "Oldingi dizayn",
    next: "Keyingi dizayn",
    concept: (current: number, total: number) => `${current}/${total}-dizayn`,
    names: [
      "Fokusli o'quv paneli",
      "O'quv sayohati xaritasi",
      "Kunlik AI murabbiy",
      "Modulli bento",
      "Tahririy ta'lim",
      "Missiya boshqaruvi",
      "Tinch o'quv muhiti",
      "Yutuqlar sur'ati",
      "Ko'nikmalar g'ildiragi",
      "Moslashuvchan keyingi qadam",
    ],
    all: "Barchasi",
    loadingTitle: "Bugungi reja tayyorlanmoqda",
    loadingText: "Progress va tavsiyalar yuklanmoqda.",
    errorTitle: "Ma'lumotlarni yuklab bo'lmadi",
    errorText: "Aloqani tekshirib, qayta urinib ko'ring.",
    retry: "Qayta urinish",
    nextBest: "Eng yaxshi keyingi qadam",
    weakestSkill: "E'tibor talab qiladigan ko'nikma",
    weekly: "Haftalik ritm",
    achievements: "Yutuqlar",
    skillBalance: "Ko'nikmalar muvozanati",
    dailyPlan: "Kunlik reja",
    modulesTitle: "Barcha yo'nalishlar",
    reviewQueue: "Takrorlash navbati",
    savedWords: "Saqlangan so'zlar",
    minutesToday: "Bugungi fokus",
    days: ["Du", "Se", "Cho", "Pa", "Ju", "Sha", "Ya"],
    badges: {
      consistency: "Barqaror ritm",
      firstStep: "Birinchi qadam",
      weekly: "Hafta davomiyligi",
    },
    reminderLabel: "Bugungi eslatma",
    coachTitle: "Murabbiy tavsiyasi",
    editorialEyebrow: "Bugungi o'quv qaydnomangiz",
    journeyEyebrow: "Keyingi qadam doim aniq",
    studioEyebrow: "Gapirish orqali o'rganing",
    workspaceEyebrow: "Bugungi o'quv maydoningiz",
    pathEyebrow: "Har bir qadam - yangi ko'nikma",
    worldEyebrow: "Ingliz tili olamiga kiring",
    coachingEyebrow: "Natijangizga mos tavsiya",
    academyEyebrow: "EnglishAI akademiyasi",
    focusEyebrow: "Bugun faqat bitta muhim qadam",
    socialEyebrow: "Birga mashq qilishga tayyormisiz?",
    today: "Bugun",
    nextActions: "Keyingi harakatlar",
    yourRoute: "Bugungi yo'lingiz",
    livePractice: "Jonli mashq",
    todayWorkspace: "Bugungi vazifalar",
    todayProgress: "Bugungi progress",
    recommended: "Siz uchun tavsiya",
    curriculum: "O'quv yo'nalishlari",
    oneThing: "Eng katta natija uchun hozir shu mashqni bajaring.",
    morePractice: "Boshqa mashqlarni ko'rish",
    practiceRooms: "Mashq yo'nalishlari",
  },

  // Topic-centric core path: vocabulary → grammar → reading → writing → speaking → listening, all
  // using the SAME topic's words.
  // Har bir skill katalogida o'zlashtirilgan mavzular va navbatdagi uchta mavzu ochiq turadi.
  // Tugagan mavzu yashil "tugatildi" bilan
  // belgilanadi. 11-qoida: matn shu yerda, kodda hardcode emas.
  skillTopic: {
    done: "Tugatildi",
    lockedHint: "Ochiq 3 mavzudan birini o'zlashtiring",
    proBadge: "Pro",
    proHint: "Davom etish uchun Pro",
  },
  // To'lov tizimi ulanmaguncha qiziqishni yig'ish. Bu ro'yxat checkout ochilgan kuni birinchi
  // xabar yuboriladigan odamlar - narxni ko'rib, kontakt qoldirgan foydalanuvchi eng kuchli signal.
  waitlist: {
    title: "Premium tez orada",
    lead:
      "To'lov tizimi ulanmoqda. Kontaktingizni qoldiring - Premium ochilgan kuni birinchi bo'lib " +
      "sizga xabar beramiz.",
    label: "Email yoki telefon raqamingiz",
    placeholder: "email@example.com yoki +998901234567",
    submit: "Xabar bering",
    sending: "Yuborilmoqda...",
    thanks: "Rahmat! Premium ochilganda birinchi bo'lib sizga xabar beramiz.",
    hint: "Faqat Premium ishga tushgani haqida bir marta xabar yuboramiz. Boshqa hech narsa uchun ishlatmaymiz.",
    error: "Yuborib bo'lmadi. Email yoki telefon raqamini tekshirib, qayta urinib ko'ring.",
  },

  paywall: {
    title: "Bepul darslar tugadi",
    body: "Dastlabki 3 ta mavzu bepul. Keyingi barcha mavzular va darslarni ochish uchun Pro obunaga o'ting.",
    benefits: [
      "Barcha mavzular va 6 ko'nikma to'liq ochiq",
      "AI bilan cheksiz jonli muloqot",
      "Reklamasiz, to'xtovsiz o'rganish",
    ],
    // Subscription tiers (H.1). `key` maps to the SubscriptionPlan enum in the component;
    // longer commitments give a lower effective monthly rate.
    chooseTitle: "Rejani tanlang",
    bestLabel: "Eng foydali",
    plans: [
      {
        key: "monthly",
        label: "1 oy",
        price: `${formatUzs(MONTHLY_PRICE_UZS)} so'm`,
        perMonth: `oyiga ${formatUzs(MONTHLY_PRICE_UZS)} so'm`,
      },
      {
        key: "quarterly",
        label: "3 oy",
        price: `${formatUzs(QUARTERLY_PRICE_UZS)} so'm`,
        perMonth: `oyiga ~${formatUzs(pricePerMonthUzs(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS))} so'm`,
        badge: `${savePercent(QUARTERLY_PRICE_UZS, QUARTERLY_DURATION_DAYS)}% chegirma`,
      },
      {
        key: "semiannual",
        label: "6 oy",
        price: `${formatUzs(SEMI_ANNUAL_PRICE_UZS)} so'm`,
        perMonth: `oyiga ~${formatUzs(pricePerMonthUzs(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS))} so'm`,
        badge: `${savePercent(SEMI_ANNUAL_PRICE_UZS, SEMI_ANNUAL_DURATION_DAYS)}% chegirma`,
      },
      {
        key: "yearly",
        label: "1 yil",
        price: `${formatUzs(YEARLY_PRICE_UZS)} so'm`,
        perMonth: `oyiga ~${formatUzs(pricePerMonthUzs(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS))} so'm`,
        badge: `${savePercent(YEARLY_PRICE_UZS, YEARLY_DURATION_DAYS)}% chegirma`,
        best: true,
      },
    ],
    cta: "Pro'ga o'tish",
    starting: "Ochilmoqda…",
    later: "Keyinroq",
    // Until Click/Payme are wired up, Premium can't be purchased yet - the modal explains this
    // instead of starting a (free) checkout. Only the free plan (first 2 topics) is active.
    comingSoonTitle: "Premium tez orada",
    comingSoonBody:
      "To'lov tizimi (Click va Payme) ulanmoqda. U tayyor bo'lgach, barcha mavzularni ochish uchun Premiumga o'tishingiz mumkin bo'ladi. Hozircha dastlabki 2 ta mavzu bepul.",
    gotIt: "Tushunarli",
  },
  skillPath: {
    title: "Bu mavzu bo'yicha mashqlar",
    hint: "Dars ketma-ketligi: Lug'at, Grammar, Reading, Writing, Speaking va Listening. Har bir bosqichda kamida 75% ball to'plang.",
    ready: "Tayyor",
    comingSoon: "Tez orada",
    here: "Shu yerda",
    // Per-skill completion states (driven by the topic's K.5 module scores).
    done: "Bajarildi",
    current: "Hozir shu",
    next: "Keyingisi",
    todo: "Boshlanmagan",
    // Topic mastery (K.5): every skill must reach the same minimum score.
    locked: "Qulflangan",
    lockedHint:
      "Bu ko'nikmada kamida 75% natija to'plang.",
    progress: (passed: number, total: number) =>
      `${total} bosqichdan ${passed} tasi bajarildi`,
    // Shown when all six stages are passed - the topic is complete.
    moduleCompleteTitle: "Mavzu to'liq o'rganildi! 🎉",
    moduleCompleteHint:
      "Bu bo'limning barcha 6 bosqichini kamida 75% bilan bajardingiz. Keyingi bo'limga o'tishingiz mumkin.",
    nextTopic: "Keyingi mavzu",
    backToRoadmap: "Yo'lga qaytish",
    // Shown on a result screen - the full six-stage status for this topic.
    overviewTitle: "Bu mavzu ko'nikmalari",
    overviewHint:
      "Qaysi ko'nikma bajarildi, qaysi biri qolganini ko'ring. Keyingisini shu yerdan oching.",
    continueNext: "Keyingi ko'nikmaga o'tish",
    // Bottom-of-page action on every skill page: advance to the next topic, enabled only once all
    // all six stages of this topic are passed (matches the sequential-topic lock).
    nextModule: "Keyingi mavzuga o'tish",
    nextModuleFinish: "Yo'l xaritasiga qaytish",
    nextModuleLockedHint:
      "Keyingi bo'limga o'tish uchun barcha 6 ko'nikmada kamida 75% natija oling.",
    steps: {
      vocabulary: { title: "Lug'at", text: "Mavzu so'zlarini o'rganing." },
      grammar: {
        title: "Grammar",
        text: "Grammatika darsini bajaring - bali shu mavzuga qo'shiladi.",
      },
      writing: { title: "Writing", text: "Shu so'zlar bilan insho yozing." },
      speaking: { title: "Speaking", text: "Shu mavzuda AI bilan gapiring." },
      listening: {
        title: "Listening",
        text: "Shu so'zlarni tinglab tushuning.",
      },
      reading: { title: "Reading", text: "Shu mavzudagi matnni o'qing." },
    },
  },

  // Bottom-of-page "continue" CTA on every skill page, plus the section-finished celebration
  // modal shown once all six stages (vocabulary/grammar/reading/writing/speaking/listening) of a
  // topic are passed. See LessonFlowAction.tsx.
  lessonFlow: {
    finishSection: "Bo'limni yakunlash",
    continueTo: (label: string) => `${label} ga o'tamiz`,
    nextStepFallback: "Keyingi bosqich",
    retryHint: "75% natija bilan qayta urinib ko'ring",
    completedTitle: "Dars muvaffaqiyatli tugadi",
    sectionComplete: (n: number) => `${n}-bo'limni tugatdingiz!`,
    sectionCompleteFallback: "Bo'limni tugatdingiz!",
    completedBody:
      "Lug'at, Grammar, Reading, Writing, Speaking va Listening bosqichlarining barchasi bajarildi.",
    nextSectionDetecting: "Keyingi bo'lim aniqlanmoqda",
    nextSectionLabel: "Keyingi bo'lim",
    levelFinishLabel: "Daraja yakuni",
    backToRoadmap: "Yo'l xaritasiga qaytish",
    roadmap: "Yo'l xaritasi",
    startNextSection: "Keyingi bo'limni boshlash",
    continueAction: "Davom etish",
    steps: {
      vocabulary: "Lug'at",
      grammar: "Grammar",
      reading: "Reading",
      writing: "Writing",
      speaking: "Speaking",
      listening: "Listening",
    },
  },

  // Shared chrome for the topic-organised skill modules (Grammar, Writing, Listening). Each
  // module is organised BY topic so the learner practises it with words they already studied.
  skillModule: {
    grammar: {
      title: "Grammar - mavzu bo'yicha",
      subtitle:
        "Mavzuni tanlang. Grammatika mashqlari shu mavzuning so'zlari bilan quriladi.",
    },
    writing: {
      title: "Writing - mavzu bo'yicha",
      subtitle: "Mavzuni tanlang. O'rgangan so'zlaringiz bilan insho yozasiz.",
    },
    listening: {
      title: "Listening - mavzu bo'yicha",
      subtitle:
        "Mavzuni tanlang. Shu mavzuning so'zlarini tinglab tushunishni mashq qiling.",
    },
    // Honest banner: the interactive exercise backend is still being built (rule 8 - no fake
    // functionality). The learning path is wired now; the exercise itself follows in a later phase.
    pendingTitle: "Bu modul tayyorlanmoqda",
    pendingText:
      "Hozircha mavzuni oching: so'zlarni o'rganing, Speaking'da mashq qiling. Bu modulning interaktiv mashqi keyingi bosqichda ishga tushadi.",
    openTopic: "Mavzuni ochish",
  },

  // Grammar module (PROJECT-SPEC G.2 - topic-scoped 5-step lessons). Each topic's grammar focus
  // is taught in the topic's own context; the lesson is generated and cached on first open. All
  // learner-facing Uzbek is a vetted template (rule 11); the teaching content itself is English.
  grammar: {
    title: "Grammatika",
    subtitle:
      "Mavzuni tanlang - har mavzuning grammatikasi shu mavzu kontekstida o'rgatiladi.",
    catalogEmpty:
      "Sizning darajangiz uchun grammatika mavzulari hali tayyor emas.",
    // Level selector above the topic grid - lets the learner browse any CEFR level's grammar.
    levelLabel: "Daraja:",
    allLevels: "Hammasi",
    // Hint under the rule: each English sentence can be tapped for its Uzbek meaning.
    tapToTranslate:
      "Har bir inglizcha gapni ustiga bosib o'zbekcha tarjimasini ko'ring.",
    // Step 1 context: a single button gives the exact, full Uzbek translation on demand (no
    // accidental hover translation) - the sanctioned translate-a-real-source path (rule 11).
    showTranslation: "O'zbekcha tarjimasini ko'rish",
    hideTranslation: "Tarjimani yashirish",
    translating: "Tarjima qilinmoqda...",
    translationUnavailable:
      "Tarjima hozircha mavjud emas. Qayta urinib ko'ring.",
    // Shown while the lesson is still being generated (rule 8 - no fake content).
    preparing: "Grammatika darsi tayyorlanmoqda...",
    retry: "Qayta urinish",
    open: "Darsni ochish",
    back: "Grammatika mavzulari",
    // Five-step lesson sections.
    stepContext: "1. Kontekstda ko'rish",
    stepRule: "2. Qoida",
    // After the lesson (steps 1-2) the exercises are grouped under one clearly separated heading so
    // learning and practice don't blur together (like the vocabulary topic quiz).
    exercisesTitle: "Bu mavzu bo'yicha mashqlar",
    exercisesHint:
      "Qoidani o'rganib bo'lgach, shu mavzu bo'yicha mashqlarni bajaring. 70% va undan yuqori ball to'plansa, grammatika moduli yakunlanadi.",
    stepRecognition: "3. Tanib olish",
    stepActiveUse: "4. Faol ishlatish",
    stepApplication: "5. Qo'llash",
    recognitionHint: "To'g'ri shaklni tanlang.",
    activeUseHint: "Bo'sh joyni to'ldiring yoki gapni qayta tuzing.",
    applicationHint:
      "O'rgangan qoidani Speaking yoki Writing modulida ishlating.",
    exerciseTypes: {
      recognition: "Tanib olish",
      fillInBlank: "Bo'sh joyni to'ldirish",
      rephrase: "Qayta tuzish",
    },
    answered: (a: number, total: number) => `${a}/${total} javob berildi`,
    check: "Tekshirish",
    again: "Qayta urinish",
    heartsEmpty: "Yuraklar tugadi. Qoidani takrorlab, testni boshidan boshlang.",
    correct: "To'g'ri",
    incorrect: "Noto'g'ri",
    correctAnswer: "To'g'ri javob:",
    score: (correct: number, total: number) => `${correct}/${total} to'g'ri`,
    passed: "Ajoyib! Bu mavzuni o'zlashtirdingiz.",
    notPassed: "Yaxshi urinish - qoidani ko'rib chiqib, qayta sinab ko'ring.",
    practiceIn: "Bu yerda mashq qilish",
    // Shown when grammar is opened from a vocabulary topic's learning path (K.5).
    forTopic: (title: string) => `"${title}" mavzusi uchun`,
    topicModuleProgress: (passed: number, total: number) =>
      `Mavzu bo'yicha: ${total} modulning ${passed} tasi yakunlandi`,
    grammarModulePassed: "Grammatika moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
    // Shown on each catalog card and the lesson header so the learner sees which grammar point the
    // topic teaches (rule 11 - vetted labels). Grammar term names stay in English (standard terms).
    focusLabel: "Grammatika:",
    // Curated-lesson section headings (shown when a vetted Uzbek lesson is available).
    curatedFormula: "Tuzilishi (formula)",
    curatedExamples: "Misollar",
    curatedMistakes: "Ko'p uchraydigan xatolar",
    // Short Uzbek-facing name for every grammar focus code in the learning spine (10 per level).
    // Standard grammar term names are kept in English; an Uzbek gloss is added where it helps.
    focusNames: {
      // A1
      "to-be": "«To be» (am/is/are)",
      "present-simple": "Present Simple (hozirgi oddiy zamon)",
      articles: "Artikllar (a / an / the)",
      "plural-nouns": "Ko'plik otlar (-s/-es)",
      "there-is-there-are": "There is / There are",
      "possessive-adjectives": "Egalik sifatlari (my, your, his...)",
      "prepositions-of-place": "O'rin predloglari (in, on, under...)",
      "can-ability": "«Can» - qobiliyat",
      "present-continuous": "Present Continuous (hozirgi davom zamon)",
      "wh-questions": "Wh- so'roqlar (what, where, who...)",
      // A2
      "past-simple": "Past Simple (oddiy o'tgan zamon)",
      comparatives: "Qiyosiy daraja (bigger, faster...)",
      superlatives: "Orttirma daraja (the biggest...)",
      "going-to-future": "«Be going to» (kelajak rejasi)",
      "adverbs-of-frequency": "Takror ravishlari (always, often...)",
      "countable-uncountable": "Sanaladigan/sanalmaydigan otlar",
      "prepositions-of-time": "Vaqt predloglari (at, on, in)",
      "object-pronouns": "To'ldiruvchi olmoshlar (me, him, us...)",
      "possessive-pronouns": "Egalik olmoshlari (mine, yours...)",
      imperatives: "Buyruq gaplar (imperative)",
      // B1
      "present-perfect": "Present Perfect (have/has + V3)",
      "past-continuous": "Past Continuous (o'tgan davom zamon)",
      "first-conditional": "Birinchi shart gap (1st conditional)",
      "second-conditional": "Ikkinchi shart gap (2nd conditional)",
      "gerunds-and-infinitives": "Gerundiy va infinitiv (-ing / to + fe'l)",
      "will-future": "«Will» - kelajak zamon",
      "modals-of-obligation": "Majburiyat modallari (must, have to...)",
      "defining-relative-clauses":
        "Aniqlovchi ergash gaplar (who, which, that)",
      "used-to": "«Used to» (o'tmishdagi odat)",
      "comparative-adverbs": "Qiyosiy ravishlar (more quickly...)",
      // B2
      "present-perfect-continuous":
        "Present Perfect Continuous (have been + V-ing)",
      "past-perfect": "Past Perfect (o'tmishdan oldingi zamon)",
      "third-conditional": "Uchinchi shart gap (3rd conditional)",
      "passive-voice": "Majhul nisbat (passive voice)",
      "reported-speech": "O'zlashtirma gap (reported speech)",
      "modals-of-deduction": "Taxmin modallari (must be, can't be...)",
      "non-defining-relative-clauses": "Izohlovchi ergash gaplar (, which...)",
      "future-continuous": "Future Continuous (will be + V-ing)",
      "wish-clauses": "«Wish» gaplari (afsus/istak)",
      "causative-have": "Causative «have» (have something done)",
      // C1
      "mixed-conditionals": "Aralash shart gaplar (mixed conditionals)",
      inversion: "Inversiya (so'z tartibi o'zgarishi)",
      "cleft-sentences": "Ajratma gaplar (It is... / What... clauses)",
      "participle-clauses": "Sifatdosh gaplar (participle clauses)",
      "advanced-passive": "Murakkab majhul nisbat",
      subjunctive: "Subjunktiv mayl (subjunctive)",
      "future-perfect": "Future Perfect (will have + V3)",
      "ellipsis-and-substitution": "Ellipsis va o'rin almashtirish",
      nominalisation: "Nominalizatsiya (fe'lni otga aylantirish)",
      "discourse-markers": "Bog'lovchi iboralar (however, therefore...)",
      // C2
      "hypothetical-meaning": "Xayoliy ma'no (hypothetical meaning)",
      "advanced-inversion": "Murakkab inversiya",
      "fronting-and-emphasis": "Old o'ringa chiqarish va urg'u",
      "cohesion-devices": "Matnni bog'lash vositalari",
      "hedging-language": "Ehtiyotkor til (hedging)",
      "emphatic-structures": "Urg'uli tuzilmalar (do/did emphasis)",
      "complex-reporting": "Murakkab o'zlashtirma gap",
      "concessive-clauses": "Qarama-qarshilik gaplari (although, despite...)",
      "idiomatic-modality": "Idiomatik modallik",
      "register-and-formality": "Uslub va rasmiylik darajasi",
    } as Record<string, string>,
  },

  // Writing module (PROJECT-SPEC G.3 - curated tasks + 4-dimension AI assessment). All
  // learner-facing Uzbek is a vetted template (rule 11); the AI returns structured scores and
  // issue codes, and the issue explanations themselves are resolved server-side.
  writing: {
    title: "Yozish",
    subtitle:
      "Darajangizdagi mavzular bo'yicha insho yozing - AI 4 o'lcham bo'yicha baholaydi.",
    // Level selector above the task grid - browse any CEFR level or the full progression.
    levelLabel: "Daraja:",
    allLevels: "Hammasi",
    catalogEmpty: "Sizning darajangiz uchun yozish mavzulari hali tayyor emas.",
    catalogLoadError: "Yozish mavzularini yuklab bo'lmadi. Qayta urinib ko'ring.",
    // Shown when writing is opened from a vocabulary topic's learning path (K.5).
    forTopic: (title: string) => `"${title}" mavzusi uchun`,
    back: "Yozish mavzulari",
    // Honest "preparing" state while the prompt is being generated (rules 8, 11).
    preparing:
      "Topshiriq tayyorlanmoqda… Bir lahzadan so'ng qayta urinib ko'ring.",
    taskNotFound: "Bu yozish mavzusi endi mavjud emas.",
    taskNotFoundHint: "Katalogga qaytib, mavjud topshiriqlardan birini tanlang.",
    taskLoadError: "Topshiriqni hozir yuklab bo'lmadi. Qayta urinib ko'ring.",
    retry: "Qayta urinish",
    start: "Yozishni boshlash",
    guidanceTitle: "Nimalarni yozish kerak",
    wordRange: (min: number, max: number) => `${min}-${max} so'z`,
    wordTarget: (min: number, max: number) => `Tavsiya: ${min}-${max} so'z`,
    wordCount: (n: number) => `${n} so'z yozildi`,
    // Per-level hard limit (rule 10): the AI assessment is a paid call, so a submission longer than
    // the level's max is blocked here before it reaches the server.
    overLimit: (max: number) =>
      `So'z chegarasidan oshib ketdingiz - eng ko'pi ${max} so'z. Baholashdan oldin qisqartiring.`,
    yourAnswer: "Sizning javobingiz",
    placeholder: "Inshongizni shu yerga yozing...",
    submit: "Baholashga yuborish",
    assessing: "Natijalar chiqmoqda...",
    assessingHint: "Yozuvingiz darajangizga mos mezonlar bo'yicha tekshirilmoqda.",
    assessmentError: "Yozuvingizni hozir tekshirib bo'lmadi. Matningiz saqlandi - qayta urinib ko'ring.",
    assessmentTimeout: "AI belgilangan vaqtda javob bermadi. Matningiz saqlandi - qayta urinib ko'ring.",
    assessmentUnavailable: "AI xizmati hozir javob bermayapti. Matningiz saqlandi - qayta urinib ko'ring.",
    correlationId: (id: string) => `Tekshiruv kodi: ${id}.`,
    again: "Qayta yozish",
    // Assessment result.
    overallScore: (percent: number) => `Umumiy ball: ${percent}%`,
    bandLabel: (band: number) => `O'rtacha: ${band.toFixed(1)}/5`,
    estimatedLevel: "Taxminiy daraja:",
    dimensionsTitle: "O'lchamlar bo'yicha ball",
    dimensions: {
      taskAchievement: "Topshiriqni bajarish",
      coherence: "Bog'lanish va izchillik",
      lexicalResource: "So'z boyligi",
      grammaticalAccuracy: "Grammatik aniqlik",
    },
    issuesTitle: "Tuzatishlar",
    noIssues: "Jiddiy xato topilmadi - ajoyib ish!",
    // Freemium gating (H.1): Free learners get 3 AI assessments per month (HTTP 402).
    gatedTitle: "Oylik limit tugadi",
    gatedText:
      "Bepul rejada oyiga 3 ta AI baholash mavjud. Cheksiz baholash uchun Premium'ga o'ting.",
    // Topic mastery (K.5) - shown only when the task was opened from a topic.
    topicModuleProgress: (passed: number, total: number) =>
      `Mavzu bo'yicha: ${total} modulning ${passed} tasi yakunlandi`,
    writingModulePassed: "Yozish moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
    taskDonePraise: "Ajoyib!",
  },

  speaking: {
    title: "Speaking",
    // Mode-picker hub (jungle redesign): the landing screen for /speaking before a
    // conversation starts, offering free / topic-based / roleplay conversation modes.
    hub: {
      heroTitle: "Qanday suhbatlashamiz?",
      heroSubtitle: "AI tutor bilan gaplashish uslubini tanlang.",
      freeMode: {
        title: "Erkin suhbat",
        hint: "Mavzusiz, istalgan mavzuda AI bilan erkin gaplashing.",
        cta: "Boshlash",
      },
      topicMode: {
        title: "Mavzuli suhbat",
        hint: "O'rgangan so'zlaringiz bo'yicha AI bilan suhbat quring.",
        cta: "Boshlash",
      },
      roleMode: {
        title: "Rolli suhbat",
        hint: "Haqiqiy vaziyatlarni simulyatsiya qiling - restoran, aeroport va boshqalar.",
        cta: "Boshlash",
      },
    },
    greeting: "Salom! Bugun nima haqida gaplashamiz?",
    thinking: "AI tutor javobi kutilmoqda...",
    pressToSpeak: "Tugmani bosing va gapiring",
    micPreparing: "Mikrofon tayyorlanmoqda...",
    recording: "Yozilmoqda...",
    stopRecording: "To'xtatish",
    pronunciation: "Talaffuz",
    realScoreUnavailable: "Real baholash ulanmagan",
    details: "Batafsil",
    // Shown above the chips of every mispronounced word so the learner can open each one.
    practiceWordsLabel:
      "Xato qilingan so'zlar - batafsil ko'rish uchun bosing:",
    you: "Siz",
    tutor: "AI Tutor",
    connecting: "Tayyorlanmoqda...",
    liveConnecting: "Live suhbat ulanmoqda...",
    liveActive: "Live tinglash faol",
    micPermission:
      "Mikrofonga ruxsat berilmadi. Brauzer sozlamalarini tekshiring.",
    notRecognized:
      "Ovoz aniqlanmadi. Iltimos, balandroq va aniqroq qayta gapiring.",
    invalidAudio:
      "Audio yozuvini o‘qib bo‘lmadi. Brauzerni yangilab, qayta urinib ko‘ring.",
    lowConfidence:
      "Gapingiz eshitildi, ammo yetarlicha aniq tanilmadi. Biroz sekinroq qayta gapiring.",
    speechServiceError:
      "Ovozni aniqlash xizmati vaqtincha ishlamadi. Qayta urinib ko‘ring.",
    utteranceError: "AI tutor hozir javob bera olmadi. Gapingiz va yozuv saqlandi - qayta urinishingiz mumkin.",
    // Kunlik gapirish limiti. Bu xato emas - limit ertaga yangilanadi, Premium esa uni kengaytiradi.
    quotaRemaining: "Bugun yana {minutes} daqiqa gapira olasiz",
    quotaAlmostGone: "Bugungi gapirish vaqtingiz tugay deb qoldi",
    quotaExhausted:
      "Bugungi gapirish vaqtingiz tugadi. Ertaga yangilanadi yoki Premium bilan ko‘proq gapirasiz.",
    // Jonli (Voice Live) aksent tutorini ishga tushirishdagi xatolar. Server mashina o'qiy
    // oladigan kod qaytaradi (budget_exhausted / voice_live_daily_limit), sahifa esa shu
    // shablonlardan mosini ko'rsatadi.
    liveTutorErrors: {
      micPermission:
        "Mikrofonga ruxsat berilmadi. Brauzer sozlamalaridan mikrofonni yoqing va qayta urinib ko'ring.",
      dailyLimit:
        "Bugungi jonli suhbat vaqtingiz tugadi. Ertaga yangi limit ochiladi - shu vaqtgacha boshqa mashqlarni davom ettiring.",
      budgetExhausted:
        "Jonli suhbat hozir band. Biroz kuting yoki bugun boshqa mashqlar bilan davom eting.",
      generic:
        "Jonli tutorga ulanib bo'lmadi. Internet aloqasini tekshirib, qayta urinib ko'ring.",
    },
    retryUtterance: "Qayta yuborish",
    endSession: "Suhbatni yakunlash",
    speakingNow: "AI gapirmoqda...",
    // "Gapirishga g'oya kelmay qolganda" yordamchisi (blank-page yechimi). Kartochkalardagi
    // inglizcha matn (savol + gap boshlanmasi) - o'rganilayotgan til, uni AI yaratadi; bu yerdagi
    // barcha o'zbekcha yorliqlar esa oldindan yozilgan shablon (rule 11).
    ideas: {
      button: "G'oya kerakmi?",
      sheetTitle: "Gapirish uchun g'oyalar",
      sheetSubtitle:
        "Nima deyishni bilmayapsizmi? Quyidagilardan birini tanlang va o'z so'zingiz bilan davom eting.",
      starterLabel: "Shunday boshlang:",
      loading: "G'oyalar tayyorlanmoqda...",
      error: "G'oyalarni yuklab bo'lmadi. Qayta urinib ko'ring.",
      refresh: "Boshqa g'oyalar",
      close: "Yopish",
      empty: "Hozircha g'oya yo'q.",
      // Suhbat boshida tayyorgarlik paneli - planning time (TBLT): gapirishdan oldin bir necha
      // soniya fikrni yig'ib olish qotib qolishni keskin kamaytiradi.
      planning: {
        title: "Bir oz tayyorlaning",
        subtitle:
          "Gapirishdan oldin fikringizni yig'ib oling. Quyidagi g'oyalar yordam beradi.",
        timer: (s: number) => `${s} soniya`,
        ready: "Tayyorman, gapiraman",
        skip: "O'tkazib yuborish",
      },
    },
    // One-time "what should I call you?" prompt: the tutor never invents a name, so the learner
    // provides one here. Shown only until a name is saved (rule 11 - all wording lives here).
    nameModal: {
      title: "AI sizni qanday chaqirsin?",
      subtitle:
        "Ismingizni bir marta kiriting - AI tutor sizni shu nom bilan eslab qoladi.",
      placeholder: "Ismingiz",
      save: "Saqlash",
      skip: "Hozircha emas",
      error: "Ism juda uzun yoki noto'g'ri. Qisqaroq kiriting.",
    },
    // Suhbat ichida ko'rinadigan ism so'rovi: ovozli tanishtiruvni STT to'g'ri tushunmasa,
    // foydalanuvchi ismini yozib beradi va AI shu nom bilan chaqiradi (rule 11).
    namePrompt: {
      heading: "Ismingizni to'g'ri eshitmadim",
      hint: "Ismingizni shu yerga yozing - AI tutor sizni shu nom bilan chaqiradi.",
      placeholder: "Ismingiz",
      save: "Saqlash",
      dismiss: "Yopish",
      error: "Ism juda uzun yoki noto'g'ri. Qisqaroq kiriting.",
      // Mikrofon ostidagi doimiy havola - istalgan paytda ismni kiritish/o'zgartirish.
      open: "AI sizni qanday chaqirsin?",
    },
    // Mavzu bo'yicha "o'rganildi" maqsadi: 5 daqiqa gapirish (rule 11).
    topicGoal: "Mavzuni o'rganish uchun gapiring",
    topicProgress: (spoken: number, goal: number) =>
      `${spoken}/${goal} daq gaplashildi`,
    topicLearned: "Bu mavzu o'rganildi! 🎉",
    topicLearnedHint:
      "Siz bu mavzuda 5 daqiqa gaplashing - endi u o'rganilgan deb belgilandi.",
    // Topic picker - the conversation subjects are the vocabulary topics. The learner studies
    // a topic's words, then practices speaking about it. Only topic ids are sent to the
    // backend; all labels stay in this content store (rule 11).
    topicTitle: "Bugun nima haqida gaplashamiz?",
    topicSubtitle:
      "O'rgangan mavzungizni tanlang yoki erkin suhbatni boshlang.",
    levelLabel: "Daraja",
    allLevels: "Hammasi",
    freeConversation: "Erkin suhbat",
    freeConversationHint: "Mavzusiz, ixtiyoriy mavzuda suhbatlashing.",
    topicsEmpty: "Bu daraja uchun mavzular topilmadi.",
    // Backend faqat mavzu kodini oladi; barcha o'zbekcha yorliqlar shu yerda (rule 11).
    freeTalkHeading: "Erkin suhbat mavzulari",
    freeTalkSubheading: "Suhbatni boshlash uchun mavzu tanlang",
    freeTalkEmpty: "Bu daraja uchun mavzular topilmadi.",
    // "Erkin suhbat mavzulari" endi alohida sahifada ochiladi: speaking sahifasidagi tugma
    // mavzular sahifasiga o'tkazadi, u yerda mavzu tanlansa AI shu mavzuda dialogni boshlaydi.
    freeTalkEntryHint:
      "Mavzu tanlang - AI aynan shu mavzuda siz bilan suhbatlashadi.",
    freeTalkOpen: "Mavzularni ochish",
    freeTalkPageSubtitle:
      "Mavzu tanlang - AI aynan shu mavzuda siz bilan dialog qiladi.",
    freeTalkTopics: {
      // A1
      my_family: "Mening oilam",
      my_daily_routine: "Kunlik tartibim",
      favorite_food: "Sevimli taomim",
      my_house: "Mening uyim",
      colors_and_numbers: "Ranglar va sonlar",
      my_pet: "Uy hayvonim",
      days_of_the_week: "Hafta kunlari",
      the_weather_today: "Bugungi ob-havo",
      my_friends: "Do'stlarim",
      clothes_i_wear: "Kiyimlarim",
      my_school: "Maktabim",
      fruits_and_vegetables: "Meva va sabzavotlar",
      my_hobbies: "Mashg'ulotlarim",
      body_parts: "Tana a'zolari",
      my_hometown: "Tug'ilgan shahrim",
      shopping_for_food: "Oziq-ovqat xaridi",
      my_morning: "Ertalabim",
      telling_the_time: "Vaqtni aytish",
      my_favorite_animal: "Sevimli hayvonim",
      greetings_and_names: "Salomlashish va ismlar",
      // A2
      weekend_plans: "Dam olish kunlari rejalari",
      my_last_holiday: "So'nggi ta'tilim",
      going_to_the_doctor: "Shifokorga borish",
      my_favorite_movie: "Sevimli filmim",
      describing_a_friend: "Do'stni tavsiflash",
      shopping_for_clothes: "Kiyim xaridi",
      my_neighborhood: "Mahallam",
      getting_around_town: "Shahar bo'ylab yurish",
      my_favorite_season: "Sevimli faslim",
      cooking_a_meal: "Ovqat pishirish",
      birthday_celebrations: "Tug'ilgan kun bayrami",
      my_free_time: "Bo'sh vaqtim",
      making_plans_with_friends: "Do'stlar bilan reja tuzish",
      describing_your_town: "Shahringizni tavsiflash",
      healthy_habits: "Sog'lom odatlar",
      my_favorite_sport: "Sevimli sportim",
      a_typical_workday: "Oddiy ish kuni",
      using_the_phone: "Telefondan foydalanish",
      asking_for_directions: "Yo'l so'rash",
      my_dream_vacation: "Orzuimdagi ta'til",
      // B1
      social_media_habits: "Ijtimoiy tarmoq odatlari",
      learning_a_language: "Til o'rganish",
      my_future_goals: "Kelajak maqsadlarim",
      work_life_balance: "Ish va hayot muvozanati",
      favorite_books: "Sevimli kitoblar",
      travel_experiences: "Sayohat tajribalari",
      technology_in_daily_life: "Kundalik hayotda texnologiya",
      music_and_concerts: "Musiqa va konsertlar",
      environmental_habits: "Ekologik odatlar",
      staying_healthy: "Sog'liqni saqlash",
      memorable_events: "Esda qolarli voqealar",
      city_vs_countryside: "Shahar va qishloq",
      shopping_online: "Onlayn xarid",
      celebrations_and_traditions: "Bayramlar va an'analar",
      a_person_you_admire: "Siz hurmat qiladigan inson",
      part_time_jobs: "Yarim kunlik ishlar",
      food_and_culture: "Taom va madaniyat",
      weekend_getaways: "Qisqa sayohatlar",
      my_daily_challenges: "Kundalik qiyinchiliklarim",
      hobbies_and_interests: "Qiziqishlar va mashg'ulotlar",
      // B2
      remote_work: "Masofaviy ish",
      the_role_of_technology: "Texnologiyaning o'rni",
      climate_change: "Iqlim o'zgarishi",
      education_systems: "Ta'lim tizimlari",
      social_media_influence: "Ijtimoiy tarmoq ta'siri",
      cultural_differences: "Madaniy farqlar",
      healthy_lifestyle_choices: "Sog'lom turmush tarzi",
      career_ambitions: "Karyera intilishlari",
      traveling_the_world: "Dunyo bo'ylab sayohat",
      money_and_saving: "Pul va jamg'arma",
      news_and_media: "Yangiliklar va ommaviy axborot",
      art_and_creativity: "San'at va ijod",
      urban_life: "Shahar hayoti",
      volunteering: "Ko'ngillilik faoliyati",
      work_stress: "Ish stressi",
      the_future_of_jobs: "Kasblarning kelajagi",
      relationships_and_friendship: "Munosabatlar va do'stlik",
      personal_development: "Shaxsiy rivojlanish",
      living_abroad: "Chet elda yashash",
      entertainment_and_hobbies: "Ko'ngilochar va mashg'ulotlar",
      // C1
      globalization: "Globallashuv",
      artificial_intelligence: "Sun'iy intellekt",
      ethics_in_business: "Biznesda axloq",
      the_economy: "Iqtisodiyot",
      mental_health_awareness: "Ruhiy salomatlik",
      the_media_and_truth: "OAV va haqiqat",
      sustainable_living: "Barqaror turmush",
      leadership_qualities: "Yetakchilik fazilatlari",
      innovation_and_society: "Innovatsiya va jamiyat",
      cultural_identity: "Madaniy o'ziga xoslik",
      the_value_of_education: "Ta'lim qadri",
      privacy_in_the_digital_age: "Raqamli davrda maxfiylik",
      work_and_automation: "Ish va avtomatlashtirish",
      social_inequality: "Ijtimoiy tengsizlik",
      the_arts_in_society: "Jamiyatda san'at",
      science_and_progress: "Fan va taraqqiyot",
      consumerism: "Iste'molchilik",
      the_meaning_of_success: "Muvaffaqiyat ma'nosi",
      urbanization: "Urbanizatsiya",
      lifelong_learning: "Umrbod o'rganish",
      // C2
      the_philosophy_of_happiness: "Baxt falsafasi",
      free_will_and_determinism: "Iroda erkinligi va determinizm",
      the_ethics_of_ai: "Sun'iy intellekt axloqi",
      geopolitics: "Geosiyosat",
      the_nature_of_creativity: "Ijod tabiati",
      economic_globalization: "Iqtisodiy globallashuv",
      the_future_of_humanity: "Insoniyat kelajagi",
      language_and_thought: "Til va tafakkur",
      moral_dilemmas: "Axloqiy muammolar",
      the_role_of_government: "Hukumatning roli",
      scientific_ethics: "Ilmiy axloq",
      art_and_meaning: "San'at va ma'no",
      justice_and_law: "Adolat va qonun",
      the_information_age: "Axborot asri",
      cultural_relativism: "Madaniy nisbiylik",
      the_psychology_of_decisions: "Qaror qabul qilish psixologiyasi",
      power_and_responsibility: "Hokimiyat va mas'uliyat",
      the_limits_of_knowledge: "Bilim chegaralari",
      technology_and_ethics: "Texnologiya va axloq",
      the_pursuit_of_wisdom: "Donolikka intilish",
    } as Record<string, string>,
    // Kunlik bepul limit tugaganda (backend 402 Payment Required) ko'rsatiladi.
    limit: {
      title: "Bugungi bepul sessiya tugadi",
      text: "Bepul tarifda kuniga 1 ta Speaking sessiya mavjud. Cheksiz suhbat uchun Premium'ga o'ting yoki ertaga qayta urinib ko'ring.",
      upgrade: "Premium'ga o'tish",
      home: "Bosh sahifa",
    },
    // Bir suhbatda 10 daqiqa gaplashilganda (server cheklovi, rule 10) ko'rsatiladi.
    timeUp: {
      title: "Bugun 10 daqiqa gaplashdingiz",
      text: "Zo'r mashq bo'ldi! Bir suhbat 10 daqiqa bilan cheklangan. Keyinroq yangi suhbat boshlashingiz mumkin.",
      ok: "OK",
    },
    // Speaking sessiyasini boshlashdagi texnik xatolar.
    error: {
      title: "Suhbatni boshlab bo'lmadi",
      networkText: "Internet aloqangiz uzildi. Ulanishni tekshirib, qayta urinib ko'ring.",
      unavailableText: "AI tutor vaqtincha band. Birozdan so'ng qayta urinib ko'ring.",
      rateLimitedText: "Juda ko'p urinish bo'ldi. Biroz kutib, qayta urinib ko'ring.",
      retry: "Qayta urinish",
    },
    // Session summary
    summary: {
      title: "Suhbat yakunlandi",
      subtitlePraise: "Ajoyib mashq bo'ldi! Davom eting.",
      avgPronunciation: "O'rtacha talaffuz",
      turnsSpoken: (n: number) => `Siz ${n} marta gapirdingiz`,
      focusWords: "Mashq qilish kerak bo'lgan so'zlar",
      noFocusWords: "Barcha so'zlar yaxshi talaffuz qilindi - zo'r!",
      focusWordsSaved: "Bu so'zlar bosh sahifadagi Talaffuz mashqi kartasiga saqlandi.",
      practiceAgain: "Yana mashq qilish",
      backHome: "Bosh sahifaga qaytish",
    },

    // Rolli suhbat - AI tutor belgilangan personajni (suhbatdosh, ofitsiant, shifokor…) o'ynaydi
    // va foydalanuvchi haqiqiy hayotdagi vaziyatni mashq qiladi. Sahna oxirida baho beriladi.
    // Barcha o'zbekcha matn shu yerda (rule 11); backend faqat scenario kodini oladi.
    roleplay: {
      // Suhbat va rolli suhbat orasida o'tish uchun havolalar (ikkala tanlash ekranida ham).
      switchToRoleplay: "Rolli suhbat",
      switchToConversation: "Erkin suhbat",
      // Senariy tanlash ekrani.
      pickTitle: "Qaysi vaziyatni mashq qilamiz?",
      pickSubtitle:
        "Haqiqiy hayotdagi vaziyatni tanlang va AI bilan rol o'ynang.",
      // "Rolli suhbat" endi alohida sahifada (/speaking/role-talk): speaking sahifasidagi tugma
      // senariylar sahifasiga o'tkazadi, u yerda vaziyat tanlansa AI shu rolda suhbatni boshlaydi.
      entryHint: "Vaziyat tanlang - AI bilan rol o'ynab suhbatlashing.",
      recommendedLevel: "Tavsiya etilgan daraja",
      start: "Boshlash",
      loadError: "Senariylarni yuklab bo'lmadi. Qayta urinib ko'ring.",
      empty: "Hozircha senariylar mavjud emas.",
      // Sahnani yakunlash tugmasi (suhbat davomida).
      endScene: "Sahnani yakunlash",
      scoring: "Natija hisoblanmoqda...",
      scoreError: "Natijani hisoblab bo'lmadi. Qayta urinib ko'ring.",
      // Senariy yorliqlari - backenddagi scenario kodi bo'yicha (rule 11). Har bir CEFR darajasi
      // uchun 20 tadan, jami 120 ta rolli suhbat: A1/A2 - oddiy kundalik vaziyatlar, B1/B2 -
      // muammo hal qilish va muzokara, C1/C2 - yuqori bosqich professional sahnalar.
      scenarios: {
        // A1
        airport: {
          title: "Aeroportda",
          description:
            "Reysga ro'yxatdan o'ting: ismingizni ayting, pasportingizni bering, o'rindiq tanlang va yukingiz haqida so'rang.",
        },
        restaurant: {
          title: "Restoranda",
          description:
            "Ovqat va ichimlik buyurtma qiling, menyu bo'yicha savol bering va hisobni so'rang.",
        },
        shop: {
          title: "Do'konda",
          description:
            "Yoqqan narsangizni toping, o'lcham, rang va narxini so'rang va xarid qiling.",
        },
        cafe: {
          title: "Kafeda",
          description:
            "Ichimlik va yengil taom buyurtma qiling, narxini so'rang va to'lang.",
        },
        taxi: {
          title: "Taksida",
          description:
            "Haydovchiga qayerga borishni ayting, qancha vaqt va pul ketishini so'rang, oxirida to'lang.",
        },
        hotel_check_in: {
          title: "Mehmonxonaga joylashish",
          description:
            "Ismingizni ayting, bronni tasdiqlang, nonushta va Wi-Fi haqida so'rang.",
        },
        bus_ticket: {
          title: "Avtobus chiptasi",
          description:
            "Qayerga borishingizni ayting, jo'nash vaqti va narxini so'rang.",
        },
        bakery: {
          title: "Nonvoyxonada",
          description:
            "Non va shirinlik sotib oling, bugun nima yangi pishganini so'rang va to'lang.",
        },
        fruit_market: {
          title: "Meva bozorida",
          description:
            "Meva sotib oling: kilosi narxini so'rang, miqdorini tanlang va to'lang.",
        },
        asking_directions: {
          title: "Yo'l so'rash",
          description:
            "Vokzalga qanday borishni so'rang va yo'nalishni to'g'ri tushunganingizga ishonch hosil qiling.",
        },
        new_neighbor: {
          title: "Yangi qo'shni bilan tanishish",
          description:
            "O'zingizni tanishtiring, qayerdan ekaningizni ayting va qisqa suhbat qiling.",
        },
        pharmacy: {
          title: "Dorixonada",
          description:
            "Bosh og'rig'iga dori so'rang, uni qanday ichishni so'rang va to'lang.",
        },
        post_office: {
          title: "Pochtada",
          description:
            "Chet elga xat jo'nating: qayerga ketishini ayting, narxini so'rang va marka sotib oling.",
        },
        supermarket: {
          title: "Supermarketda",
          description:
            "Sut, non va tuxum qayerdaligini so'rang va bir mahsulot narxi bilan qiziqing.",
        },
        pizza_order: {
          title: "Telefonda pitsa buyurtma qilish",
          description:
            "O'lcham va qo'shimchalarni tanlang, manzilingizni ayting, qachon yetib kelishini so'rang.",
        },
        lost_bag: {
          title: "Yo'qolgan sumka",
          description:
            "Sumkangiz yo'qolganini tushuntiring, uni tasvirlab bering va oddiy savollarga javob bering.",
        },
        zoo_tickets: {
          title: "Hayvonot bog'iga chipta",
          description:
            "Oilangiz uchun chipta oling, bolalar narxini va bog' qachon yopilishini so'rang.",
        },
        library_card: {
          title: "Kutubxona kartasi",
          description:
            "Karta olish uchun ism va manzilingizni ayting, nechta kitob olish mumkinligini so'rang.",
        },
        ice_cream_stand: {
          title: "Muzqaymoq do'konchasida",
          description:
            "O'zingiz va do'stingizga ta'm tanlang, o'lcham va narxlarni so'rang, to'lang.",
        },
        new_classmate: {
          title: "Yangi sinfdosh bilan tanishish",
          description:
            "O'zingizni tanishtiring, uning ismi va qiziqishlarini so'rang, birga o'qishga kelishing.",
        },
        // A2
        job_interview: {
          title: "Ish suhbati",
          description:
            "O'zingizni tanishtiring, tajriba va kuchli tomonlaringiz haqida gapiring va nega bu ishni xohlayotganingizni tushuntiring.",
        },
        doctor: {
          title: "Shifokorda",
          description:
            "Shikoyatlaringizni tushuntiring, shifokorning savollariga javob bering va maslahatni aniq tushunib oling.",
        },
        hairdresser: {
          title: "Sartaroshxonada",
          description:
            "Qanday soch turmagi xohlashingizni tushuntiring, savollarga javob bering va suhbat quring.",
        },
        bank_account: {
          title: "Bank hisobi ochish",
          description:
            "Ma'lumotlaringizni bering, qanday hujjat kerakligini va bank kartasi haqida so'rang.",
        },
        lost_luggage: {
          title: "Yo'qolgan yuk",
          description:
            "Yo'qolgan chamadoningizni tasvirlang, qaysi reysda kelganingizni ayting va yetkazish manzilini qoldiring.",
        },
        train_station: {
          title: "Temir yo'l vokzalida",
          description:
            "Borish-kelish chiptasini oling, platforma va jo'nash vaqtini, chegirma bor-yo'qligini so'rang.",
        },
        bike_rental: {
          title: "Velosiped ijarasi",
          description:
            "Bir soatlik narx, garov puli va yopilish vaqtini so'rab, velosiped ijaraga oling.",
        },
        hotel_problem: {
          title: "Mehmonxonadagi muammo",
          description:
            "Xona sovuq va shovqinli ekanidan muloyim shikoyat qiling va xonani almashtirishni so'rang.",
        },
        return_item: {
          title: "Mahsulotni qaytarish",
          description:
            "O'lchami to'g'ri kelmagan ko'ylakni qaytaring: muammoni tushuntiring, chekni ko'rsating, pul yoki almashtirish so'rang.",
        },
        clinic_call: {
          title: "Shifokorga yozilish",
          description:
            "Telefonda qabulga yoziling: muammoni qisqacha ayting, kun va vaqt tanlang, nima olib kelishni so'rang.",
        },
        gym_signup: {
          title: "Sport zalga a'zo bo'lish",
          description:
            "Narxlar va ish vaqtini so'rang, bitta mashg'ulotni bepul sinab ko'rish mumkinligini bilib oling.",
        },
        food_delivery: {
          title: "Ovqat yetkazib berish buyurtmasi",
          description:
            "Ikki kishilik kechki ovqat buyurtma qiling, manzil bering, yetkazish vaqti va to'lovni so'rang.",
        },
        phone_shop: {
          title: "Telefon sotib olish",
          description:
            "Nima kerakligini tasvirlang, ikkita modelni solishtiring va kafolat haqida so'rang.",
        },
        dentist: {
          title: "Tish shifokorida",
          description:
            "Tish og'rig'i qachon boshlanganini ayting, savollarga javob bering va davolashni tushunib oling.",
        },
        tourist_info: {
          title: "Sayyohlik ma'lumot markazida",
          description:
            "Bir kunda nimalarni ko'rish mumkinligini so'rang, xarita oling va shahar avtobus sayohati bilan qiziqing.",
        },
        birthday_invitation: {
          title: "Do'stni taklif qilish",
          description:
            "Do'stingizni tug'ilgan kuningizga taklif qiling: qachon va qayerdaligini ayting, savollariga javob bering.",
        },
        cinema_tickets: {
          title: "Kinoteatrda",
          description:
            "Film seanslarini so'rang, o'rindiq tanlang va gazaklar narxi bilan qiziqing.",
        },
        flower_shop: {
          title: "Gul do'konida",
          description:
            "Do'stingizning tug'ilgan kuniga gul oling: nimani yoqtirishini ayting, rang tanlang, narxga kelishing.",
        },
        talking_to_teacher: {
          title: "O'qituvchi bilan suhbat",
          description:
            "Uy vazifangiz haqida so'rang: nimani yaxshilash kerak va uyda qanday mashq qilish mumkin.",
        },
        lost_wallet: {
          title: "Yo'qolgan hamyon",
          description:
            "Hamyoningiz yo'qolganini ayting: ichida nima borligini, qayerda yo'qotganingizni tushuntiring va aloqa ma'lumotlaringizni qoldiring.",
        },
        // B1
        apartment_viewing: {
          title: "Kvartira ko'rish",
          description:
            "Ijara haqi, kommunal to'lovlar va uy qoidalarini so'rang, keyingi qadamni kelishib oling.",
        },
        internship_interview: {
          title: "Amaliyot suhbati",
          description:
            "O'qishingiz, ko'nikmalaringiz va bo'sh vaqtingiz haqida gapiring, lavozim haqida savollar bering.",
        },
        bank_card_issue: {
          title: "Karta bilan muammo",
          description:
            "Kartangiz bloklanib, pul ikki marta yechilganini tushuntiring, shaxsingizni tasdiqlang va yechimni kelishing.",
        },
        missed_flight: {
          title: "Reysga ulgurmaslik",
          description:
            "Nima bo'lganini tushuntiring, keyingi reysga qayta joylashtirishni va mehmonxona yoki ovqat talonini so'rang.",
        },
        restaurant_complaint: {
          title: "Restoranda shikoyat",
          description:
            "Taom uzoq kutilib sovuq kelganidan muloyim shikoyat qiling va adolatli yechimga kelishing.",
        },
        travel_agency: {
          title: "Sayohat agentligida",
          description:
            "Bir haftalik ta'til rejalashtiring: budjet, sana va xohishlaringizni ayting, ikkita taklifni solishtiring.",
        },
        new_coworker: {
          title: "Ishdagi birinchi kun",
          description:
            "O'zingizni tanishtiring, jamoa va kun tartibi haqida so'rang, yozilmagan qoidalarni bilib oling.",
        },
        market_bargaining: {
          title: "Bozorda savdolashish",
          description:
            "Qo'lda to'qilgan gilam uchun savdolashing: narx so'rang, pastroq taklif qiling va kelishuvga erishing.",
        },
        car_rental: {
          title: "Avtomobil ijarasi",
          description:
            "Sug'urta, yoqilg'i qoidasi va ikkinchi haydovchi qo'shishni so'rang, umumiy narxni tekshiring.",
        },
        tech_support: {
          title: "Texnik yordamga qo'ng'iroq",
          description:
            "Noutbukdagi muammoni aniq tasvirlang, mutaxassis aytgan qadamlarni bajaring va ta'mirlashni kelishing.",
        },
        hotel_booking_change: {
          title: "Bronni o'zgartirish",
          description:
            "Bronni boshqa sanalarga ko'chiring, to'lovlar haqida so'rang va yangi ma'lumotlarni tasdiqlang.",
        },
        gym_cancellation: {
          title: "A'zolikni bekor qilish",
          description:
            "A'zolikni muloyim bekor qiling, qolishga ko'ndirishlariga berilmang va shartlarni tasdiqlang.",
        },
        university_admissions: {
          title: "Qabul bo'limida",
          description:
            "Hujjat topshirish haqida so'rang: qanday hujjatlar kerak, muddatlar va til talablari qanday.",
        },
        insurance_claim: {
          title: "Sug'urta da'vosi",
          description:
            "Oshxonadagi suv oqishi haqida xabar bering: nima bo'lgani va nima zarar ko'rganini ayting, keyingi qadamlarni so'rang.",
        },
        cake_order: {
          title: "Maxsus tort buyurtma qilish",
          description:
            "Tug'ilgan kun torti buyurtma qiling: o'lcham, dizayn va ta'mlarni kelishing, olib ketish kunini belgilang.",
        },
        car_mechanic: {
          title: "Avtoustada",
          description:
            "Mashinangizdagi g'alati ovozni tasvirlang, nima bo'lishi mumkinligini so'rang, narx va muddatni kelishing.",
        },
        parent_teacher_meeting: {
          title: "Ota-onalar uchrashuvi",
          description:
            "Farzandingizning o'zlashtirishini muhokama qiling: kuchli tomonlar va muammolarni so'rang, uyda yordam rejasini tuzing.",
        },
        career_advisor: {
          title: "Kasb maslahatchisida",
          description:
            "Ish variantlaringizni muhokama qiling: tajriba va qiziqishlaringizni ayting, CV bo'yicha maslahat so'rang.",
        },
        trip_with_friend: {
          title: "Do'st bilan sayohat rejasi",
          description:
            "Manzil, transport va budjetni kelishing, reja bo'yicha kelishmovchilikni muloyim hal qiling.",
        },
        mobile_plan: {
          title: "Tarif tanlash",
          description:
            "Ikki tarifni solishtiring, yashirin to'lovlarni so'rang va keraksiz qo'shimchalardan muloyim voz keching.",
        },
        // B2
        salary_negotiation: {
          title: "Maosh muzokarasi",
          description:
            "Maosh va shartlarni kelishing: so'ragan summangizni asoslang, imtiyozlarni muhokama qiling.",
        },
        competency_interview: {
          title: "Chuqur ish intervyusi",
          description:
            "Xulq-atvor savollariga tajribangizdan aniq misollar bilan javob bering, lavozim haqida o'tkir savollar bering.",
        },
        performance_review: {
          title: "Yillik baholash suhbati",
          description:
            "Yutuqlaringizni taqdim eting, tanqidga xotirjam javob bering, rivojlanish maqsadlarini kelishing.",
        },
        lease_negotiation: {
          title: "Ijara shartnomasi muzokarasi",
          description:
            "Ijara haqi va muddatini kelishing, shartnomaga o'zgartirish so'rang, ta'mirni kim to'lashini aniqlashtiring.",
        },
        bank_loan: {
          title: "Kredit olish",
          description:
            "Kredit nimaga kerakligini tushuntiring, foiz va to'lov shartlarini muhokama qiling, daromad haqidagi savollarga javob bering.",
        },
        visa_interview: {
          title: "Viza intervyusi",
          description:
            "Safaringiz haqidagi savollarga aniq va izchil javob bering, faktlarni ishonch bilan keltiring.",
        },
        pitching_an_idea: {
          title: "Ishda g'oya taqdim etish",
          description:
            "Yaxshilash g'oyangizni taqdim eting, e'tirozlarga dalil bilan javob bering va qo'llab-quvvatlashga erishing.",
        },
        complaint_escalation: {
          title: "Shikoyatni yuqoriga ko'tarish",
          description:
            "Muammo tarixini bayon qiling, qat'iy lekin muloyim turing va munosib kompensatsiyani kelishing.",
        },
        networking_event: {
          title: "Networking kechasi",
          description:
            "O'zingizni professional tanishtiring, ishingizni qiziqarli tasvirlang va aloqada qolishga kelishing.",
        },
        medical_results: {
          title: "Tahlil natijalarini muhokama qilish",
          description:
            "Natijalarni tushunib oling, variantlar haqida so'rang va davolash rejasini tanqidiy muhokama qiling.",
        },
        cancelled_flight: {
          title: "Bekor qilingan reys",
          description:
            "Qayta joylashtirilishga erishing: huquqlaringizni xotirjam ayting, kompensatsiya so'rang, muqobillarni solishtiring.",
        },
        seminar_debate: {
          title: "Universitet seminari",
          description:
            "Belgilangan mavzu bo'yicha nuqtai nazaringizni himoya qiling va qarshi dalillarga javob bering.",
        },
        buying_a_car: {
          title: "Ishlatilgan mashina olish",
          description:
            "Mashinaning tarixini so'rang, bosim taktikalariga berilmang va narxni pastga tushiring.",
        },
        client_meeting: {
          title: "Qiyin mijoz uchrashuvi",
          description:
            "Kechikishni halol tushuntiring, tiklanish rejasini taklif qiling va mijoz ishonchini saqlang.",
        },
        roommate_conflict: {
          title: "Ijaradosh bilan kelishmovchilik",
          description:
            "Uy yumushlari, to'lovlar va shovqin bo'yicha kelishmovchilikni hal qiling, ikkalangizga mos murosani toping.",
        },
        deadline_pushback: {
          title: "Muddatga e'tiroz",
          description:
            "Xavflarni tushuntirib, imkonsiz muddatga e'tiroz bildiring, muqobil hajm yoki sana taklif qiling.",
        },
        house_purchase: {
          title: "Uy ko'rigi",
          description:
            "Sotuv gaplaridan nariga o'ting: uyning holati, mahalla va narx tarixini so'rang, savdolashishni boshlang.",
        },
        promotion_case: {
          title: "Lavozim oshirish masalasi",
          description:
            "Dalillar bilan lavozim oshirishga asos keltiring, mezonlarni so'rang, aniq muddatni kelishing.",
        },
        friendly_debate: {
          title: "Do'st bilan bahs",
          description:
            "Dolzarb mavzuda bahslashing: fikringizni asoslang, muloyim qarshilik ko'rsating, mushtarak nuqta toping.",
        },
        claim_dispute: {
          title: "Rad etilgan da'voga e'tiroz",
          description:
            "Rad javobiga e'tiroz bildiring: polis shartlarini keltiring, sabablarga raddiya bering, rasmiy qayta ko'rishni so'rang.",
        },
        // C1
        panel_interview: {
          title: "Panel intervyu",
          description:
            "Har tomondan yog'ilgan savollarga tartibli javob bering va xotirjam yetakchilikni namoyon qiling.",
        },
        board_presentation: {
          title: "Kengash oldida taqdimot",
          description:
            "Strategiyani qisqa taqdim eting, raqamlarni qattiq savollar ostida himoya qiling va tasdiqqa erishing.",
        },
        investor_pitch: {
          title: "Investorga taqdimot",
          description:
            "Biznesingizni taqdim eting: bozor, model va natijalar - qiyin tekshiruv savollariga dosh bering.",
        },
        contract_negotiation: {
          title: "Shartnoma muzokarasi",
          description:
            "Narx, javobgarlik va muddatlarni kelishing, pozitsiyangizni boy bermay o'zaro yon berishlar qiling.",
        },
        media_crisis_interview: {
          title: "Inqiroz paytidagi intervyu",
          description:
            "Bosim ostida gapiring: muammoni tan oling, asosiy xabarlaringizga qaytaring, tuzoq savollardan qoching.",
        },
        conference_qna: {
          title: "Konferensiya savol-javobi",
          description:
            "Metod va xulosalaringizni himoya qiling, cheklovlarni ishingizni yiqitmasdan tan oling.",
        },
        lawyer_consultation: {
          title: "Advokat bilan maslahat",
          description:
            "Nizoni aniq tushuntiring, variantlar, xavflar va xarajatlarni tushunib oling va yo'l tanlang.",
        },
        policy_debate: {
          title: "Siyosat bo'yicha bahs",
          description:
            "Pozitsiyangizni dalillar bilan asoslang, raqibning eng kuchli fikrlariga raddiya bering va ishonarli yakunlang.",
        },
        partnership_negotiation: {
          title: "Hamkorlik muzokarasi",
          description:
            "Manfaatlarni muvofiqlashtiring, hamkorlik tuzilmasini taklif qiling, pul va nazorat haqidagi nozik savollarni boshqaring.",
        },
        giving_feedback: {
          title: "Qiyin fikr bildirish",
          description:
            "Menejer sifatida aniq misollar va hamdardlik bilan qattiq fikr bildiring, yaxshilanish rejasini kelishing.",
        },
        diplomatic_reception: {
          title: "Rasmiy qabul marosimi",
          description:
            "Nafis suhbatni davom ettiring, madaniy nozik mavzularni ehtiyotkorlik bilan aylanib o'ting, samimiy aloqa o'rnating.",
        },
        medical_second_opinion: {
          title: "Ikkinchi shifokor fikri",
          description:
            "Tashxisni tanqidiy muhokama qiling: dalillar, muqobillar va har bir davolash usulining afzallik-kamchiliklari.",
        },
        mediation_session: {
          title: "Mediatsiya sessiyasi",
          description:
            "E'tirozlaringizni konstruktiv bayon qiling, ikkinchi tomon talqiniga javob bering, kelishuv sari harakatlaning.",
        },
        thesis_defense: {
          title: "Dissertatsiya himoyasi",
          description:
            "Tadqiqot tanlovlaringizni asoslang, o'tkir savollarga dosh bering va ishingiz hissasini himoya qiling.",
        },
        press_briefing: {
          title: "Matbuot brifingi",
          description:
            "Asosiy xabarlarni yetkazing, shubha bilan berilgan qo'shimcha savollarni boshqaring va noto'g'ri gapirib yubormang.",
        },
        supplier_negotiation: {
          title: "Yetkazib beruvchi bilan muzokara",
          description:
            "Shartlarni qayta muvozanatlang: hajm va muqobillardan foydalaning, narx himoyasini qo'lga kiriting.",
        },
        expert_panel: {
          title: "Ekspertlar davrasida",
          description:
            "Qisqa va aniq ekspert javoblarini bering, hamkasblar bilan konstruktiv bahslashing.",
        },
        key_client_rescue: {
          title: "Asosiy mijozni saqlab qolish",
          description:
            "Vaziyatni yumshating, ortiqcha yon bermasdan mas'uliyatni zimmangizga oling va mijozni saqlab qoling.",
        },
        town_hall: {
          title: "Umumiy yig'ilishni boshqarish",
          description:
            "Ommaga yoqmaydigan o'zgarishni e'lon qiling, o'tkir savollarga halol javob bering, jamoa ishonchini saqlang.",
        },
        radio_interview: {
          title: "Jonli radio intervyu",
          description:
            "Murakkab g'oyalarni sodda tushuntiring, hazil va gap bo'linishlariga yo'nalishni yo'qotmasdan javob bering.",
        },
        // C2
        keynote_qna: {
          title: "Katta ma'ruza savol-javobi",
          description:
            "Falsafiy, dushmanona va chalkash savollarga birdek aniqlik, zukkolik va nazokat bilan javob bering.",
        },
        live_tv_debate: {
          title: "Jonli teledebat",
          description:
            "Auditoriyani o'zingizga og'diring: ritorikani ongli qo'llang, keskin raddiya bering, shaxsiy hujum ostida sovuqqonlikni saqlang.",
        },
        parliamentary_hearing: {
          title: "Parlament eshituvi",
          description:
            "Guvohlik bering: aniq javoblar bering, noto'g'ri talqinlarni rasman to'g'rilang, maxfiy masalalarni qonuniy himoya qiling.",
        },
        summit_negotiation: {
          title: "Xalqaro sammit muzokarasi",
          description:
            "Ko'p tomonlama kelishuvni kelishing: ittifoqlar tuzing, qizil chiziqlaringizni boshqaring, yakuniy matnni shakllantiring.",
        },
        crisis_press_conference: {
          title: "Inqiroz matbuot anjumani",
          description:
            "Janjalni boshqaring: ishonarli uzr so'rang, voqea talqinini o'z qo'lingizga oling, tuzoqlarga tushmang.",
        },
        philosophy_seminar: {
          title: "Falsafa seminari",
          description:
            "Iroda erkinligi va axloq haqida qat'iy dalillashuvni davom ettiring, mantiqiy xatolarni fosh qiling.",
        },
        expert_witness: {
          title: "Ekspert guvoh",
          description:
            "Sudda ekspert ko'rsatmasi bering: o'zaro so'roq ostida mutlaq aniqlik saqlang, so'zlaringizni buzib talqin qilishlariga yo'l qo'ymang.",
        },
        emergency_board_meeting: {
          title: "Favqulodda kengash yig'ilishi",
          description:
            "Jamoani inqirozdan olib chiqing: variantlarni yoyib bering, noaniqlikda qaror qabul qiling, guruhlarni birlashtiring.",
        },
        diplomatic_negotiation: {
          title: "Diplomatik muzokara",
          description:
            "Nozik bitimni kelishing: gap ostidagi ma'noni o'qing, konstruktiv noaniqlikdan foydalaning, manfaatlarni himoya qiling.",
        },
        adversarial_interview: {
          title: "Qarama-qarshi intervyu",
          description:
            "Tanlab iqtibos olish, gap bo'lish va tuzoq savollarni boshqarib, baribir fikringizni yetkazing.",
        },
        peer_review_defense: {
          title: "Taqrizga javob",
          description:
            "Metodologiyangizni ekspert tanqididan himoya qiling, tezisdan voz kechmasdan tuzatishlarni kelishing.",
        },
        merger_negotiation: {
          title: "Kompaniyalar qo'shilishi muzokarasi",
          description:
            "Baholash, nazorat va madaniyatni kelishing, boshi berk holatlarda ham bitimni saqlab qoling.",
        },
        policy_advisory: {
          title: "Siyosat bo'yicha maslahat",
          description:
            "Murakkab islohot bo'yicha maslahat bering: dalillarni jamlang, afzallik-kamchiliklarni o'lchang, tavsiyangizni himoya qiling.",
        },
        literary_panel: {
          title: "Adabiyot davrasi",
          description:
            "Talqin, uslub va mumtoz merosni zukkolik va teranlik bilan muhokama qiling.",
        },
        ethics_committee: {
          title: "Axloq qo'mitasi eshituvi",
          description:
            "Nozik axloqiy ishni dalillang: pretsedentlar, tamoyillar va yengillashtiruvchi holatlar - quruq gaplarsiz.",
        },
        final_investment_round: {
          title: "Hal qiluvchi investitsiya bosqichi",
          description:
            "Baholash va qarashingizni ekspert shubhalaridan himoya qiling, shartnoma shartlarini kelishing.",
        },
        moderating_a_panel: {
          title: "Qizigan davrani boshqarish",
          description:
            "Moderatorlik qiling: muvozanatni ta'minlang, dushmanlikni nafislik bilan yumshating, shovqindan mazmun ajratib oling.",
        },
        ambassador_briefing: {
          title: "Elchiga brifing",
          description:
            "Beqaror vaziyat haqida qisqa brifing bering, tezkor strategik savollarga yozuvsiz javob bering.",
        },
        labor_negotiation: {
          title: "Mehnat nizosi muzokarasi",
          description:
            "Ish tashlashning oldini oling: maosh va shartlarni kelishing, ikki tomon tazyiqini boshqaring.",
        },
        legacy_interview: {
          title: "Faoliyatga yakuniy nazar",
          description:
            "Xatolaringiz, qadriyatlaringiz va merosingiz haqida teran mulohaza yuriting - qolip gaplardan ko'ra nozik tahlil.",
        },
      } as Record<string, { title: string; description: string }>,
      // Sahna oxiridagi baho kartochkasi.
      score: {
        title: "Sahna yakunlandi",
        overall: "Umumiy ball",
        // Baho o'lchovlari - backenddagi RoleplayDimension kodi bo'yicha.
        dimensions: {
          task_completion: "Vazifa bajarilishi",
          fluency: "Ravonlik",
          grammar: "Grammatika",
          appropriateness: "Muomala",
        } as Record<string, string>,
        playAgain: "Yana mashq qilish",
        chooseScenario: "Boshqa senariy",
        backSpeaking: "Speaking sahifasiga qaytish",
        backHome: "Bosh sahifaga qaytish",
      },
    },
  },

  pronunciation: {
    dojoTitle: "Talaffuz dojosi",
    nativePronunciation: "Asl talaffuz",
    slow: "Sekin",
    hardHint: "O'zbeklar uchun qiyin tovush",
    tapToHear: "Eshitish uchun bosing",
    watchMouth: "Og'iz harakatini ko'rish",
    mouthPosition: "Og'iz holati",
    comparison: "Talaffuz qiyoslovi",
    yourAttempt: "Sizning urinishingiz",
    similarity: (pct: number) => `${pct}% o'xshashlik`,
    correctPronunciation: "To'g'ri talaffuz",
    tip: "Maslahat",
    tryAgain: "Yana bir bor urinib ko'rish",
    // O'zingiz aytib ko'rish (yozib olib, talaffuzni tekshirish) bo'limi.
    practiceTitle: "O'zingiz ayting",
    practiceHint:
      "Mikrofonni bosing va so'zni ayting - to'g'ri talaffuz qilganingizni tekshiramiz.",
    recordStart: "Yozib ko'rish",
    recording: "Yozilmoqda... to'xtatish uchun bosing",
    checking: "Tekshirilmoqda...",
    correct: "To'g'ri aytdingiz!",
    incorrect: "Yana bir bor urinib ko'ring",
    // O'z ovozini eshitib ko'rish (yozib olingan urinishni qayta ijro etish).
    listenMine: "O'zingizni eshitish",
    improved: (xp: number) => `+${xp} XP`,
    phonemePass: "To'g'ri talaffuz qilindi",
    phonemeFail: "Qayta mashq qiling",
    scoreLabel: "ball",
    bestScore: (score: number) => `Eng yaxshi natija: ${score} ball`,
    notRecognizedAttempt:
      "Ovoz aniqlanmadi. Balandroq va aniqroq qayta ayting.",
    authenticScoreUnavailable:
      "Haqiqiy talaffuz bahosi olinmadi. Ovoz aniq yozilganini tekshirib, qayta urinib ko'ring.",
    assessmentError:
      "Ovozni tekshirib bo'lmadi. Yozuvni qayta yuborish uchun yana urinib ko'ring.",
    assessmentTimeout:
      "Talaffuzni tekshirish vaqti tugadi. Internet aloqasini tekshirib, qayta urinib ko'ring.",
    phonemeScore: (score: number) => `${score} ball`,
    micError: "Mikrofonga ruxsat berilmadi. Brauzer sozlamalarini tekshiring.",
    notFoundTitle: "Bu so'z uchun talaffuz tafsiloti topilmadi",
    notFoundBody: "Bu so'zning fonetik tahlili hozircha mavjud emas.",
    errorTitle: "Talaffuz tafsilotini yuklab bo'lmadi",
    errorBody: "Nimadir xato ketdi. Qayta urinib ko'ring.",
    retry: "Qayta urinish",
    back: "Suhbatga qaytish",
    backHome: "Bosh sahifaga qaytish",
  },

  videoCatalog: {
    loadingMore: "Yana videolar yuklanmoqda...",
    endOfFeed:
      "Hozircha shu - pastga tushib yana yuklang yoki keyinroq qayting.",
    loadMore: "Yana yuklash",
    opening: "Tayyorlanmoqda...",
    error:
      "Videolarni yuklab bo'lmadi. Internetni tekshirib, qayta urinib ko'ring.",
    // Paste-a-link panel: open any YouTube video by its URL.
    pasteTitle: "YouTube havolasini joylang",
    pasteSubtitle:
      "Istalgan YouTube videoni shu yerga joylab, transkript bilan oching.",
    pastePlaceholder: "https://www.youtube.com/watch?v=...",
    pasteOpen: "Ochish",
    pasteInvalid:
      "Bu to'g'ri YouTube havolasi emas. Tekshirib, qayta urinib ko'ring.",
    pasteError: "Videoni ochib bo'lmadi. Qayta urinib ko'ring.",
    // Search box: type a topic, get live English-only video recommendations in a modal.
    searchTitle: "Qidirish",
    searchSubtitle: "Qidiruv bo'limiga yozing - ingliz tilidagi videolar tavsiya etiladi.",
    searchPlaceholder: "Mavzu yozing (masalan: speaking practice)...",
    searchButton: "Qidirish",
    searchEmpty: "Videolarni ko'rish uchun qidiring.",
    searchLoading: "Qidirilmoqda...",
    searchNoResults: "Hech qanday video topilmadi. Boshqa so'z bilan urinib ko'ring.",
    searchError: "Qidirib bo'lmadi. Internetni tekshirib, qayta urinib ko'ring.",
    searchHint: "Faqat ingliz tilidagi va subtitrli videolar. Kattalarga mo'ljallangan kontent yashiriladi.",
    closedCaptionsAvailable: "Subtitr mavjud",
    selectedVideo: "Oldin ochilgan video",
  },

  videoPlayer: {
    practiceCaptions: "MASHQ UCHUN SUBTITRLAR",
    quizLink: "Video testiga o‘tish",
    openYouTube: "YouTube’da ochish",
    showTranslation: "Tarjimani ko‘rsatish",
    hideTranslation: "Tarjimani yashirish",
    translationPending: "Bu gapning tarjimasi hali tayyor emas.",
    videoUnavailable: "Video ochilmadi. Video katalogiga qaytib, qayta urinib ko‘ring.",
    transcript: "Transkript",
    showFullTranscript: "To'liq transkriptni ko'rish",
    fullTranscript: "To'liq transkript",
    hideTranscript: "Transkriptni yashirish",
    showTranscript: "Transkriptni ko'rsatish",
    fullSize: "To'liq ekran",
    exitFullSize: "To'liq ekrandan chiqish",
    close: "Yopish",
    play: "Ijro etish",
    pause: "To'xtatish",
    skipBack: "5 soniya orqaga",
    skipForward: "5 soniya oldinga",
    holdForDoubleSpeed: "2x tezlik uchun bosib turing",
    seekLabel: "Videoni kerakli joyga suring",
    takeQuiz: "Testni boshlash",
    waitingToStart: "Videoni ijro eting - gaplar shu yerda ketma-ket chiqadi.",
    transcriptPending:
      "Transkript bu video uchun hali tayyorlanmoqda. Hozircha pleyerning o'z subtitr (CC) tugmasini yoqishingiz mumkin.",
    // Shown under the transcript of a long video while the remaining lines are still streaming in.
    transcriptLoadingMore: "Transkriptning davomi yuklanmoqda…",
    transcriptUnavailable:
      "Bu video uchun interaktiv transkript mavjud emas. Videoni pleyerning o'z subtitr (CC) tugmasi bilan tomosha qilishingiz mumkin.",
    settings: "Sozlamalar",
    quality: "Video sifati",
    captions: "Subtitr",
    captionsOn: "Yoqilgan",
    captionsOff: "O'chirilgan",
    textColor: "Matn rangi",
    backgroundColor: "Orqa fon rangi",
    highlightColor: "Ajratish rangi",
    font: "Shrift",
    textSize: "Matn o'lchami",
    fontStandard: "Standart",
    fontClean: "Sodda",
    fontMono: "Mono",
    volume: "Ovoz",
    mute: "Ovozni o'chirish",
    unmute: "Ovozni yoqish",
  },

  videoWord: {
    inThisVideo: "Bu videoda:",
    // Same word-detail sheet, but opened from a text-based skill (vocabulary, reading, …) where
    // the example sentence comes from a passage rather than a video.
    inThisText: "Bu matnda:",
    listen: "Tinglash",
    listenSlow: "Sekin tinglash",
    mouthPosition: "Og'iz holati",
    tip: "Maslahat",
    notFound: "Bu so'z uchun talaffuz ma'lumoti topilmadi.",
    error: "Talaffuzni yuklab bo'lmadi. Qayta urinib ko'ring.",
    close: "Yopish",
    hint: "So'z ustiga olib boring yoki bosing - tarjima va talaffuzini ko'ring",
    translation: "Tarjima",
    details: "Batafsil",
    askAi: "AI'dan so'rash",
  },

  videoChat: {
    title: "AI'dan so'rash",
    subtitle: "Video haqida savol bering - nima uchun bu so'z ishlatilgan, iboralar nimani anglatadi.",
    placeholder: "Savolingizni yozing...",
    send: "Yuborish",
    thinking: "Javob tayyorlanmoqda...",
    unavailable: "AI hozir javob bera olmadi. Qayta yuboring.",
    networkError: "Internet aloqasi uzildi. Ulanishni tekshirib, qayta yuboring.",
    unauthorized: "AI yordamchidan foydalanish uchun qayta tizimga kiring.",
    rateLimited: "Juda ko'p savol yuborildi. Biroz kutib, qayta urinib ko'ring.",
    invalidQuestion: "Savol juda uzun yoki noto'g'ri. Qisqartirib, qayta yuboring.",
    empty: "Video haqida istalgan savolni bering - masalan, \"bu so'z nima uchun ishlatilgan?\"",
    openButton: "AI'dan so'rash",
    close: "Yopish",
    contextLabel: "Shu gap haqida:",
    explainSentence: (sentence: string) => `"${sentence}" gapini tushuntirib bering.`,
  },

  videoQuiz: {
    questionProgress: (n: number, total: number) => `Savol ${n} / ${total}`,
    title: "Bilimingizni tekshiring",
    loadErrorTitle: "Video testi topilmadi",
    loadErrorText: "Bu video mavjud emas yoki uning testi yuklanmadi. Videolar ro'yxatidan boshqa darsni tanlang.",
    submitError: "Javoblarni tekshirib bo'lmadi. Natijani saqlash uchun qayta urinib ko'ring.",
    tip: "Maslahat",
    nextQuestion: "Keyingi savol",
    finish: "Yakunlash",
    resultTitle: "Test natijasi",
    correctOf: (correct: number, total: number) =>
      `${total} tadan ${correct} ta to'g'ri`,
    passed: "Tabriklaymiz, testdan o'tdingiz!",
    failed: "Yana bir bor urinib ko'ring.",
    backToCatalog: "Videolar ro'yxatiga qaytish",
    ratePrompt: "Bu video siz uchun qanday edi?",
    rateTooEasy: "Oson edi",
    rateJustRight: "Aynan mos",
    rateTooHard: "Qiyin edi",
    rateThanks: "Rahmat! Tavsiyalarni shunga moslaymiz.",
  },

  reading: {
    title: "Reading",
    subtitle:
      "Darajangizdagi mavzular bo'yicha matnlarni o'qing va tushunishni tekshiring.",
    // Level selector above the lesson grid - browse any CEFR level or the full progression.
    levelLabel: "Daraja:",
    allLevels: "Hammasi",
    words: (n: number) => `${n} so'z`,
    read: "O'qish",
    empty: "Hozircha matnlar yo'q. Tez orada qo'shamiz!",
    forTopic: (title: string) => `"${title}" mavzusi uchun`,
    preparing: "Dars tayyorlanmoqda… Bir lahzadan so'ng qayta urinib ko'ring.",
    retry: "Qayta urinish",
    // Advance to the next slide of the guided reading flow (text → glossary → quiz → result).
    continue: "Davom etish",
    glossaryTitle: "Lug'at",
    quizTitle: "Tushunishni tekshirish",
    quizHint: "Matnni o'qib bo'lgach, savollarga javob bering.",
    check: "Tekshirish",
    answered: (n: number, total: number) => `${n}/${total} javob berildi`,
    score: (n: number, total: number) => `${n}/${total} to'g'ri`,
    passed: "Ajoyib! Matnni yaxshi tushundingiz.",
    notPassed: "Yana bir bor o'qib, qayta urinib ko'ring.",
    correct: "To'g'ri",
    incorrect: "Noto'g'ri",
    correctAnswer: "To'g'ri javob:",
    again: "Qayta",
    // Topic mastery (K.5) - shown after the quiz when reached from a topic's learning path.
    topicModuleProgress: (passed: number, total: number) =>
      `Mavzu progressi: ${passed}/${total} modul yakunlandi`,
    readingModulePassed: "O'qish moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
  },

  // Books library (Home → Books): multi-section graded readers per CEFR level. Each section is read
  // once at least 70% of its quiz is correct; the whole book is confirmed read once every
  // section is passed. Rule 11: all learner-facing Uzbek text lives here, not hardcoded in pages.
  books: {
    title: "Kutubxona",
    subtitle:
      "Darajangizga mos kitoblarni o'qing. Har bo'limdan so'ng tushunganingizni tekshiring.",
    levelLabel: "Daraja:",
    allLevels: "Hammasi",
    empty: "Hozircha kitoblar yo'q. Tez orada qo'shamiz!",
    by: (author: string) => `Muallif: ${author}`,
    sectionsLabel: (n: number) => `${n} ta bo'lim`,
    progress: (read: number, total: number) =>
      `${total} bo'limdan ${read} tasi o'qildi`,
    completed: "O'qib tugatildi",
    startReading: "O'qishni boshlash",
    continueReading: "Davom etish",
    // Detail page (table of contents).
    contentsTitle: "Bo'limlar",
    synopsisTitle: "Qisqacha mazmun",
    sectionRead: "O'qildi",
    sectionLocked: "Qulflangan",
    sectionLockedHint: "Avval oldingi bo'limni tugating",
    readSection: "O'qish",
    reReadSection: "Qayta o'qish",
    bookCompleteBanner: "Tabriklaymiz! Bu kitobni to'liq o'qib tugatdingiz. 🎉",
    backToBooks: "Kutubxonaga qaytish",
    backToContents: "Bo'limlarga qaytish",
    // Reader + quiz.
    words: (n: number) => `${n} so'z`,
    preparing:
      "Bo'lim tayyorlanmoqda… Bir lahzadan so'ng qayta urinib ko'ring.",
    preparingTimeout:
      "Bo'limni tayyorlash odatdagidan uzoqroq davom etdi. Qayta urinib ko'ring — tayyor bo'lgan bo'lim darhol ochiladi.",
    retry: "Qayta urinish",
    quizTitle: "Tushunganingizni tekshiring",
    quizHint:
      "Bo'limni o'qib bo'lgach, savollarga javob bering. O'tish uchun kamida 70% to'g'ri javob kerak.",
    check: "Tekshirish",
    answerCheckError: "Javobni tekshirib bo'lmadi. Internet aloqasini tekshirib, qayta tanlang.",
    submitError: "Natijani saqlab bo'lmadi. Javoblaringiz saqlandi — qayta urinib ko'ring.",
    submitTimeout: "Natijani saqlash uzoq davom etdi. Javoblaringiz saqlandi — qayta urinib ko'ring.",
    retrySubmit: "Natijani qayta yuborish",
    errorCode: (correlationId: string) => `Yordam kodi: ${correlationId}`,
    answered: (n: number, total: number) => `${n}/${total} javob berildi`,
    score: (n: number, total: number) => `${n}/${total} to'g'ri`,
    requiredHint: (need: number, total: number) =>
      `O'tish uchun ${total} tadan kamida ${need} ta to'g'ri javob kerak.`,
    correct: "To'g'ri",
    incorrect: "Noto'g'ri",
    correctAnswer: "To'g'ri javob:",
    again: "Qayta urinish",
    // Result states.
    sectionPassed: "Ajoyib! Bu bo'limni o'qiganingiz tasdiqlandi.",
    sectionFailed:
      "70% dan kam to'g'ri javob. Yana bir bor o'qib, qayta urinib ko'ring.",
    bookComplete: "Tabriklaymiz! Kitobni to'liq o'qib tugatdingiz. 🎉",
    nextSection: "Keyingi bo'lim",
  },

  listening: {
    title: "Listening",
    subtitle:
      "Har darajadagi audio matnlarni tinglang va tushunishni tekshiring - pastdan yuqoriga qiyinlashib boradi.",
    words: (n: number) => `${n} so'z`,
    empty: "Hozircha audio mashqlar yo'q. Tez orada qo'shamiz!",
    // Level selector above the exercise grid - browse any CEFR level or the full progression.
    levelLabel: "Daraja:",
    allLevels: "Hammasi",
    play: "Tinglash",
    replay: "Qayta tinglash",
    pause: "To'xtatish",
    resume: "Davom etish",
    // Accessible label for the seek slider that shows how far the clip has played.
    seekLabel: "Audio holati",
    // Playback-speed control: slow the clip down to catch every word (rule 17.1 - slow, clear audio).
    speedLabel: "Tezlik:",
    speedNormal: "Oddiy",
    speedSlow: "Sekin",
    speedSlower: "Juda sekin",
    audioError: "Audioni yuklab bo'lmadi. Qayta urinib ko'ring.",
    // Shown while the exercise (transcript + quiz) is still being generated (rules 8, 11).
    preparing: "Audio mashq tayyorlanmoqda...",
    retry: "Qayta urinish",
    listenFirst: "Avval audioni tinglang, so'ng savollarga javob bering.",
    quizTitle: "Tushunishni tekshirish",
    showTranscript: "Matnni ko'rsatish",
    hideTranscript: "Matnni yashirish",
    transcriptTitle: "Audio matni",
    check: "Tekshirish",
    answered: (n: number, total: number) => `${n}/${total} javob berildi`,
    score: (n: number, total: number) => `${n}/${total} to'g'ri`,
    passed: "Ajoyib! Audioni yaxshi tushundingiz.",
    notPassed: "Yana bir bor tinglab, qayta urinib ko'ring.",
    correct: "To'g'ri",
    incorrect: "Noto'g'ri",
    correctAnswer: "To'g'ri javob:",
    again: "Qayta",
    back: "Orqaga",
  },

  // Guided topic lesson flow for the Listening skill (mirrors vocabularyTopics.* but the content
  // is audio → transcript → comprehension quiz). All copy lives here (rule 11); pages never hardcode.
  listeningTopics: {
    back: "Mavzularga qaytish",
    pending: "Audio mashq tayyorlanmoqda. Birozdan so'ng qayta urinib ko'ring.",
    retry: "Qayta urinish",
    loadError: "Tinglash ma'lumotlarini yuklab bo'lmadi. Internet aloqasini tekshirib, qayta urinib ko'ring.",
    locked: "Bu tinglash darsi hali ochilmagan. Avvalgi o'quv bosqichlarini yakunlang.",
    subscriptionRequired: "Bu tinglash darsi Premium obunani talab qiladi.",
    // Stage 0 - hub.
    hubStart: "Darsni boshlash",
    // Stage 1 - audio + transcript.
    audioTitle: "Tinglash",
    showTranscript: "Matnni ko'rsatish",
    hideTranscript: "Matnni yashirish",
    transcriptTitle: "Audio matni",
    toQuiz: "Tushunishni tekshirish",
    // Stage 2 - quiz (practice).
    quizTitle: "Tushunishni tekshirish",
    quizHint: "Audioni tinglab, savollarga javob bering.",
    nextExercise: "Keyingi savol",
    practiceResult: (correct: number, total: number) =>
      `Natija: ${total} tadan ${correct} ta to'g'ri`,
    moduleProgress: (passed: number, total: number) =>
      `Mavzu bo'yicha: ${total} modulning ${passed} tasi yakunlandi`,
    listeningModulePassed: "Tinglash moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
    audioHint: "Audioni diqqat bilan tinglang, so'ng tushunish testini boshlang.",
    checkError: "Javobni tekshirib bo'lmadi. Internet aloqasini tekshirib, yana tanlang.",
    submitError: "Test natijasini saqlab bo'lmadi. Qayta urinib ko'ring.",
    incompleteAttempt: "Urinish to'liq saqlanmagan. Test 1-savoldan qayta boshlandi.",
    noQuestions: "Bu mavzu uchun test savollari topilmadi.",
    passHint: "Keyingi urinishda kamida 75% natija oling. Audioni qayta tinglab, testni yana bajaring.",
    retryLesson: "Qayta tinglash va test",
    heartsEmpty: "Yuraklar tugadi. Audioni qayta tinglab, testni boshidan boshlang.",
    noTranscript: "Hozircha ushbu audio uchun matn mavjud emas.",
    wordsTitle: "Yangi so'zlar",
    wordsListen: "Tinglash",
    wordsFlip: "Aylantirish",
    wordsFlipHint: "So'z matnini ko'rish uchun aylantiring",
    wordsNext: "Keyingi so'z",
    wordsDone: "So'zlar tugadi",
  },

  // Guided topic lesson flow for the Reading skill (mirrors vocabularyTopics.* but the content
  // is text → translate toggle → comprehension questions). All copy lives here (rule 11).
  readingTopics: {
    back: "Mavzularga qaytish",
    pending: "Matn tayyorlanmoqda. Birozdan so'ng qayta urinib ko'ring.",
    retry: "Qayta urinish",
    // Stage 0 - hub.
    hubStart: "Darsni boshlash",
    // Stage 1 - text with translate toggle.
    textTitle: "Matn",
    textTranslate: "Keyingisi",
    textHideTranslation: "Tarjimani yashirish",
    textTranslating: "Tarjima qilinmoqda...",
    textTranslationError: "Tarjima hozircha mavjud emas.",
    toQuestions: "Tushunishni tekshirish",
    // Stage 2 - questions (practice).
    quizTitle: "Tushunishni tekshirish",
    quizHint: "Matnni o'qib bo'lgach, savollarga javob bering.",
    nextExercise: "Keyingi savol",
    checking: "Tekshirilmoqda...",
    checkError: "Javobni tekshirib bo'lmadi. Internet aloqasini tekshirib, yana tanlang.",
    submitError: "Test natijasini saqlab bo'lmadi. Qayta urinib ko'ring.",
    noQuestions: "Bu mavzu uchun test savollari topilmadi.",
    retryLesson: "Mavzuni qaytadan boshlash",
    practiceResult: (correct: number, total: number) =>
      `Natija: ${total} tadan ${correct} ta to'g'ri`,
    moduleProgress: (passed: number, total: number) =>
      `Mavzu bo'yicha: ${total} modulning ${passed} tasi yakunlandi`,
    readingModulePassed: "O'qish moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
  },

  // Guided topic lesson flow for the Writing skill (mirrors vocabularyTopics.* but the content
  // is sample/instruction → writing task → AI assessment). All copy lives here (rule 11).
  writingTopics: {
    back: "Mavzularga qaytish",
    pending: "Topshiriq tayyorlanmoqda. Birozdan so'ng qayta urinib ko'ring.",
    retry: "Qayta urinish",
    // Stage 0 - hub.
    hubStart: "Darsni boshlash",
    // Stage 1 - sample / instruction.
    sampleTitle: "Namuna va ko'rsatma",
    guidanceTitle: "Nimalarni yozish kerak",
    toTask: "Yozishni boshlash",
    // Stage 2 - writing task (practice).
    taskTitle: "Yozish topshirig'i",
    wordTarget: (min: number, max: number) => `Tavsiya: ${min}-${max} so'z`,
    wordCount: (n: number) => `${n} so'z yozildi`,
    yourAnswer: "Sizning javobingiz",
    placeholder: "Inshongizni shu yerga yozing...",
    submit: "Baholashga yuborish",
    submitting: "Baholanmoqda...",
    overLimit: (max: number) =>
      `So'z chegarasidan oshib ketdingiz - eng ko'pi ${max} so'z. Baholashdan oldin qisqartiring.`,
    nextExercise: "Davom etish",
    practiceResult: (correct: number, total: number) =>
      `Natija: ${total} o'lchamdan ${correct} tasi yakin`,
    moduleProgress: (passed: number, total: number) =>
      `Mavzu bo'yicha: ${total} modulning ${passed} tasi yakunlandi`,
    writingModulePassed: "Yozish moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
    assessmentTitle: "Baholash natijasi",
    wordsTitle: "Yangi so'zlar",
    wordsListen: "Tinglash",
    wordsFlip: "Aylantirish",
    wordsFlipHint: "So'z matnini ko'rish uchun aylantiring",
    wordsNext: "Keyingi so'z",
    wordsDone: "So'zlar tugadi",
  },

  notifications: {
    title: "Bildirishnomalar",
    back: "Orqaga",
    close: "Bildirishnomalarni yopish",
    markAllRead: "Hammasini o'qish",
    unreadCount: (n: number) => `${n} ta yangi bildirishnoma`,
    empty: "Hozircha bildirishnoma yo'q.",
    review: "Takrorlash",
    open: "Ochish",
    openLink: "Havolani ochish",
    allNotifications: "Barcha bildirishnomalar",
    // The section each notification opens when tapped, keyed by its in-app destination. Shown as a
    // chip so the learner knows where a notification leads before tapping it.
    destinations: {
      "/home": "Bosh sahifa",
      "/app/vocabulary/topics": "Lug'at",
      "/app/vocabulary/review": "Takrorlash",
      "/app/vocabulary/saved": "Mening so'zlarim",
      "/app/grammar": "Grammatika",
      "/writing": "Yozuv",
      "/app/speaking": "Suhbat",
      "/listening": "Tinglash",
      "/reading": "O'qish",
    } as Record<string, string>,
  },

  vocabulary: {
    progress: (n: number, total: number) => `${n}-so'z / ${total}`,
    percentComplete: (pct: number) => `${pct}% bajarildi`,
    dueIn: (label: string) => label,
    sessionDone: "Bugungi takrorlash yakunlandi!",
    nothingDue: "Hozircha takrorlash uchun so'z yo'q. Ajoyib!",
    stageDay3: "3 kundan keyin",
    stageDay7: "7 kundan keyin",
    stageDay21: "21 kundan keyin",
    stageMastered: "O'zlashtirildi",

    // Review mode picker + the four ways to revise a due word (test + game + write + dictation).
    review: {
      pickTitle: "Qanday takrorlaymiz?",
      pickHint: (n: number) =>
        `Takrorlash uchun ${n} ta so'z tayyor. Usulni tanlang.`,
      dueCountLabel: (n: number) => `${n} ta so'z`,
      listenTitle: "Tinglab yozish",
      listenDesc: "So'zni tinglang va eshitganingizni inglizcha yozing.",
      listenPrompt:
        "Tinglang va eshitgan so'zingizni yozing. Qayta eshitish uchun tugmani bosing.",
      listenReplay: "Qayta tinglash",
      testTitle: "Test",
      testDesc:
        "Rasmga qarab to'g'ri tarjimani toping (o'zbekcha ↔ inglizcha).",
      matchTitle: "Moslashtirish o'yini",
      matchDesc:
        "Inglizcha so'zlarni o'zbekcha ma'nolari bilan rasm orqali ulang.",
      writeTitle: "Yozish",
      writeDesc: "O'zbekcha ma'no va rasmga qarab inglizcha so'zni yozing.",
      back: "Usulni o'zgartirish",
      next: "Keyingi",
      finish: "Yakunlash",
      // Multiple-choice (test) mode.
      chooseTranslation: "To'g'ri tarjimani tanlang",
      chooseWord: "To'g'ri inglizcha so'zni tanlang",
      correct: "To'g'ri!",
      incorrect: "Noto'g'ri",
      correctAnswerWas: (answer: string) => `To'g'ri javob: ${answer}`,
      // Write mode.
      writePrompt: "Bu so'zni inglizcha yozing",
      writePlaceholder: "Inglizcha so'z...",
      check: "Tekshirish",
      reveal: "Ko'rsatish",

      // SRS session (VocabularyReviewPage): the real server-graded mini-tests
      // (MiniTestType.ClozeChoice / WrittenUsage), plus the self-rated flip-card fallback for the
      // modes the server can't verify yet (spoken/listening - docs/development-guide.md B.1 scope note).
      mandatory: {
        eyebrow: "So'zlarni mustahkamlash",
        pageTitle: "Bugungi takrorlash",
        pageSubtitle: "Xotiradan chiqib ketishi mumkin bo'lgan so'zlarni qisqa mashqlar bilan eslab qoling.",
        purposeTitle: "Bu sahifa nima uchun kerak?",
        purposeText: "EnglishAI o'rgangan so'zlaringizni kerakli vaqtda qayta ko'rsatadi. Javobingizga qarab so'z keyinroq yoki tezroq yana takrorlanadi.",
        dueLeft: (n: number) => `Qolgan: ${n}`,
        dueToday: (n: number) => `Bugungi navbat: ${n} ta`,
        nothingDueTitle: "Barcha takrorlashlar bajarildi!",
        nothingDueText: "Hozir takrorlash vaqti kelgan so'z yo'q. O'qishni davom ettirishingiz mumkin.",
        emptyCta: "Mening so'zlarim",
        back: "Orqaga",
        later: "Keyinroq",
        ratingEyebrow: "Eslab qolish darajasi",
        ratingTitle: "Bu so'zni qanchalik yaxshi esladingiz?",
        ratingHint: "Halol baholang. Natijaga qarab so'zning keyingi takrorlash vaqti belgilanadi.",
        rateHard: "Qiyin",
        rateHardHint: "Tezroq yana chiqadi",
        rateGood: "Esladim",
        rateGoodHint: "Odatdagi vaqtda chiqadi",
        rateEasy: "Oson",
        rateEasyHint: "Keyinroq yana chiqadi",
        sessionDoneTitle: "Bugungi takrorlash yakunlandi!",
        sessionDoneContinue: "Bosh sahifaga",
        loadErrorTitle: "Takrorlashlarni yuklab bo'lmadi",
        loadErrorText: "Internet aloqasini tekshirib, qayta urinib ko'ring.",
        retry: "Qayta urinish",
        submitError: "Javob saqlanmadi. Qayta urinib ko'ring.",
        statusError: "Natijani tekshirib bo'lmadi. Qayta urinib ko'ring.",
        statusLoadErrorTitle: "Takrorlash holatini tekshirib bo'lmadi",
        statusLoadErrorText: "Internet aloqasini tekshirib, qayta urinib ko'ring.",
      },
      session: {
        // ClozeChoice - pick the correct English word for the shown Uzbek meaning.
        clozePrompt: "Mos inglizcha so'zni tanlang",
        // WrittenUsage - write one sentence that uses the target word; the server (AI) grades it.
        writeSentencePrompt: (word: string) => `"${word}" so'zini ishlatib bitta gap yozing`,
        writeSentencePlaceholder: "Ingliz tilida gap yozing...",
        writeSentenceSubmit: "Yuborish",
        grading: "Tekshirilmoqda...",
        // Feedback reason codes from the server-side grader (Domain.Vocabulary.WordUsageReasonCode)
        // - resolved to a vetted Uzbek explanation here rather than shown as free LLM text
        // (docs/development-guide.md rule 11).
        reasonWordNotUsed: "Gapingizda bu so'z ishlatilmagan.",
        reasonWrongMeaning: "So'z noto'g'ri ma'noda ishlatilgan.",
        reasonTooShort: "Juda qisqa - to'liq gap yozing.",
        reasonNotEnglish: "Iltimos, ingliz tilida yozing.",
      },
      // Match game.
      matchPrompt: "Juftlarni ulang",
      matchRemaining: (n: number) => `${n} ta juft qoldi`,
      matchDone: "Hammasi to'g'ri ulandi!",
      // Session summary.
      sessionSummary: (correct: number, total: number) =>
        `${total} tadan ${correct} tasini bildingiz`,
      reviewAgain: "Yana takrorlash",

      // Four mandatory sequential sections. The learner revises the SAME due words four ways (test →
      // moslashtirish → yozish → tinglab yozish); grading is applied once at the very end, so finishing
      // a single section no longer empties the queue. Every section must be completed.
      sectionsIntroTitle: "Takrorlash - 4 bo'lim",
      sectionsIntroHint: (n: number) =>
        `${n} ta so'zni 4 xil usulda takrorlaysiz: test, moslashtirish, yozish va tinglab yozish. To'rttala bo'limni ham tugatishingiz shart.`,
      start: "Boshlash",
      // Resuming an unfinished session: completed sections are kept, the run continues from the
      // first unfinished section.
      resume: "Davom etish",
      resumeHint: (next: number) =>
        `Oldingi mashg'ulotingiz saqlab qo'yilgan. ${next}-bo'limdan davom etasiz.`,
      mustFinishNote:
        "Chiqib ketsangiz, tugallangan bo'limlaringiz saqlanadi - qaytganingizda qolgan bo'limdan davom etasiz. Yakuniy natija to'rttala bo'lim tugagandan keyingina qo'yiladi.",
      sectionLabel: (n: number, total: number) => `Bo'lim ${n}/${total}`,
      sectionStep: (title: string, n: number, total: number) =>
        `${title} - ${n}/${total}-bo'lim`,
    },

    // Vocabulary hub ("Lug'at dunyosi") landing - the /vocabulary screen.
    hub: {
      title: "Lug'at dunyosi",
      greeting: (name?: string) =>
        name ? `${name}, keling, yangi so'zlarni o'rganamiz!` : "Keling, yangi so'zlarni o'rganamiz!",
      subtitle:
        "Mavzularni o'rganing, eslab qolgan so'zlaringizni takrorlang va shaxsiy lug'atingizni to'ldiring.",
      wordsLearned: "so'z",
      dueToday: "takrorlash",
      streak: "ketma-ketlik",
      topicsTitle: "Mavzular",
      topicsText: "Qiziqarli mavzulardan yangi so'zlarni o'rganing.",
      topicsCta: "Boshlash",
      reviewTitle: "Takrorlash",
      reviewText: "Eslab qolgan so'zlaringizni mustahkamlab o'ting.",
      reviewCta: "Takrorlash",
      savedTitle: "Mening so'zlarim",
      savedText: "Saqlangan va o'rganilgan so'zlaringiz.",
      savedCta: "Ko'rish",
      recentTitle: "Yaqinda o'rganilganlar",
      recentAll: "Barchasi",
      recentLoading: "Yuklanmoqda...",
      recentEmpty: "Hozircha so'zlar yo'q. Birinchi mavzuni o'rganib ko'ring!",
    },
  },

  // Module 4 - vocabulary in context (passage + 20 words + quiz + pronunciation practice).
  vocabularyTopics: {
    title: "Lug'at - matn ichida",
    subtitle: "Mavzuni tanlang. Har bir matnda 20 ta yangi so'z bor.",
    listen: "Tinglash",
    reviewCta: "Takrorlash (SRS)",
    // FlipCard hint under the word face.
    tapToFlip: "Bosib, ma'nosini ko'ring",
    levelLabel: "Daraja",
    allLevels: "Hammasi",
    topicCount: (n: number) => `${n} ta mavzu`,
    ready: "Tayyor",
    learned: "O'rganilgan",
    // Rolling topic-window gating (K.5): three unmastered topics stay open.
    locked: "Qulflangan",
    lockedHint: "Ochiq 3 mavzudan birini to'liq o'zlashtiring (6/6).",
    empty: "Bu daraja uchun mavzular topilmadi.",
    back: "Mavzularga qaytish",
    pending: "Matn tayyorlanmoqda. Birozdan so'ng qayta urinib ko'ring.",
    retry: "Qayta urinish",
    galleryTitle: "Mavzu rasmlari",
    galleryHint:
      "Bu mavzuga oid rasmlar - so'zlarni rasm bilan bog'lab eslab qoling.",
    // New guided-lesson flow (hub → text → new words → practice).
    hubTitle: "Mening oilam",
    hubStart: "Darsni boshlash",
    textTitle: "Matn",
    textTranslate: "Tarjimasi",
    textHideTranslation: "Tarjimani yashirish",
    textTranslating: "Tarjima qilinmoqda...",
    textTranslationError: "Tarjima hozircha mavjud emas.",
    textToWords: "Yangi so'zlar",
    wordsListen: "Tinglash",
    wordsFlip: "Aylantirish",
    wordsNext: "Keyingi so'z",
    wordsDone: "So'zlar tugadi",
    wordsPronounceTitle: "Talaffuz",
    wordsAttention: "E'tibor bering",
    wordsHowToSay: "Qanday aytiladi",
    wordsLetterFocus: "Qaysi harfga e'tibor bering",
    wordsLetterFocusHint: "O'zbek tilida farqli talaffuz qilinadigan tovushlar",
    wordsTipTitle: "Tavsiya",
    wordsFlipHint: "So'z matnini ko'rish uchun aylantiring",
    pronunciationCta: "Talaffuzni mashq qilish",
    pronunciationEyebrow: "Vocabulary talaffuz mashqi",
    pronunciationReference: "Qanday talaffuz qilinadi",
    pronunciationListen: "Asl talaffuzni tinglash",
    pronunciationAudioFallback: "Asl audio topilmasa brauzer ovozi ishlatiladi.",
    pronunciationRecordTitle: "O'zingiz aytib ko'ring",
    pronunciationRecordHint: "Mikrofonni bosing, so'zni ayting va yozuvni to'xtating.",
    pronunciationStart: "Ovozni yozish",
    pronunciationStop: "Yozishni to'xtatish",
    pronunciationRecording: "Yozilmoqda...",
    pronunciationChecking: "Talaffuz tekshirilmoqda...",
    pronunciationListenMine: "O'z ovozimni tinglash",
    pronunciationBack: "Flashcardga qaytish",
    pronunciationNoIpa: "Talaffuz belgisi mavjud emas",
    pronunciationDefaultFeedback: "Natijani yaxshilash uchun so'zni yana bir bor aniq ayting.",
    pronunciationLoadError: "Talaffuz mashqini ochib bo'lmadi",
    pronunciationLoadHint: "So'z topilmadi yoki mavzu hali tayyor emas. Flashcardga qaytib, qayta urinib ko'ring.",
    wordsEmptyTitle: "Bu mavzuda yangi so'zlar topilmadi",
    wordsEmptyHint: "Mavzu ma'lumotlari hali to'liq tayyor emas. Keyinroq qayta urinib ko'ring.",
    backToTopics: "Mavzular ro'yxatiga qaytish",
    practiceTitle: "Mashq",
    practiceWordPlace: "So'zni qo'ying",
    practiceTest: "Test",
    practiceWrite: "So'zni yozing",
    practiceMatch: "Juftlang",
    nextExercise: "Keyingi mashq",
    practiceResult: (correct: number, total: number) =>
      `Natija: ${total} tadan ${correct} ta to'g'ri`,
    practiceNotPassed: "Bu safar o'tish bali yetmadi",
    heartsEmpty: "Yuraklar tugadi. Mashqni boshidan boshlang.",
    retryLesson: "Mavzuni qaytadan boshlash",
    rewardXp: "Tajriba bali",
    rewardCoins: "Sarflanadigan tanga",
    rewardAlreadyCollected: "Bugungi Vocabulary mukofoti avval berilgan.",
    rewardRequiresPass: "Mukofot olish uchun kamida 70% natija kerak.",
    rewardSaveError: "Natijani saqlab bo'lmadi. Internet aloqasini tekshirib, qayta urinib ko'ring.",
    rewardPremium: "Pro bonusi qo'llandi: barcha mukofotlar 1,2 baravar.",
    rewardPolicy: "XP bir ko'nikmaning kundagi birinchi muvaffaqiyatli mashqi uchun beriladi. Kunlik faollik, 6 ko'nikma va streak bonuslari alohida qo'shiladi.",
    rewardBonuses: (daily: number, allModules: number, streak: number) =>
      [
        daily > 0 ? `kunlik faollik +${daily}` : null,
        allModules > 0 ? `6 ko'nikma bonusi +${allModules}` : null,
        streak > 0 ? `streak bonusi +${streak}` : null,
      ].filter(Boolean).join(" · "),
    continue: "Davom etish",
    // The rest of the vocabularyTopics keys (passageTitle, wordsTitle, etc.) are
    // defined below in the original catalog block.
    // The 12 lexical categories a topic teaches: emoji + vetted Uzbek word-class name (rule 11: Uzbek
    // text comes from vetted templates, not the LLM). Keys match the server's lowercased enum name.
    wordCategory: (pos: string): { emoji: string; label: string } | null =>
      ((
        {
          noun: { emoji: "📘", label: "Otlar" },
          verb: { emoji: "⚡", label: "Fe'llar" },
          phrasalverb: { emoji: "🔥", label: "Frazali fe'llar" },
          adjective: { emoji: "🎨", label: "Sifatlar" },
          adverb: { emoji: "🚀", label: "Ravishlar" },
          preposition: { emoji: "📍", label: "Ko'makchilar" },
          conjunction: { emoji: "🔗", label: "Bog'lovchilar" },
          pronoun: { emoji: "👤", label: "Olmoshlar" },
          determiner: { emoji: "📝", label: "Aniqlovchilar va artikllar" },
          expression: { emoji: "💬", label: "Iboralar" },
          idiom: { emoji: "💡", label: "Idiomalar" },
          collocation: { emoji: "🤝", label: "So'z birikmalari" },
        } as Record<string, { emoji: string; label: string }>
      )[pos] ?? null),
    // The display order of the category groups in the words list.
    wordCategoryOrder: [
      "noun",
      "verb",
      "phrasalverb",
      "adjective",
      "adverb",
      "preposition",
      "conjunction",
      "pronoun",
      "determiner",
      "expression",
      "idiom",
      "collocation",
    ] as string[],
    // Singular chip label shown next to a single word (e.g. "fe'l").
    partOfSpeech: (pos: string): string | null =>
      ((
        {
          noun: "ot",
          verb: "fe'l",
          phrasalverb: "frazali fe'l",
          adjective: "sifat",
          adverb: "ravish",
          preposition: "ko'makchi",
          conjunction: "bog'lovchi",
          pronoun: "olmosh",
          determiner: "aniqlovchi",
          expression: "ibora",
          idiom: "idioma",
          collocation: "so'z birikmasi",
        } as Record<string, string>
      )[pos] ?? null),
    // Hover/click hint for the highlighted word cards (same affordance as the passage).
    wordCardHint:
      "Har bir so'z ustiga olib boring - tarjima, talaffuz va og'iz harakatini ko'rasiz.",
    // Where the words go - they are saved automatically once the learner finishes the topic's
    // exercises (no manual "add" button), then feed the 3/7/21-kun SRS review.
    savedHint:
      "Mashqni yakunlaganingizda bu so'zlar avtomatik \"Mening so'zlarim\"ga tushadi va takrorlash jadvaliga (3/7/21 kun) ulanadi.",
    quizTitle: "Mashq - so'zni qo'ying",
    quizHint: "Bo'sh joyga to'g'ri so'zni tanlang.",
    quizProgress: (n: number, total: number) => `${n} / ${total}`,
    checkAnswers: "Tekshirish",
    quizScore: (correct: number, total: number) =>
      `${total} tadan ${correct} ta to'g'ri`,
    quizAgain: "Qaytadan",
    moduleProgress: (passed: number, total: number) =>
      `Mavzu bo'yicha: ${total} modulning ${passed} tasi yakunlandi`,
    vocabularyModulePassed: "Lug'at moduli yakunlandi ✓",
    topicMastered: "Tabriklaymiz! Bu mavzu to'liq egallandi.",
    correct: "To'g'ri",
    incorrect: "Noto'g'ri",
    speakTitle: "Talaffuzni mashq qiling",
    speakHint:
      "So'zni tanlang, mikrofonni bosing va ovoz chiqarib ayting. AI to'g'ri aytayotganingizni tekshiradi.",
    speakPrompt: (word: string) => `"${word}" so'zini ayting`,
    record: "Yozib olish",
    recording: "Tinglanmoqda...",
    checking: "Tekshirilmoqda...",
    micError: "Mikrofonga ruxsat berilmadi. Brauzer sozlamalarini tekshiring.",
    tryAgain: "Yana urinib ko'ring",
    chooseWord: "Mashq qilish uchun so'zni tanlang",
    speakAboutTitle: "Endi shu mavzuda gapiring",
    speakAboutHint:
      "O'rgangan so'zlaringizni AI tutor bilan jonli suhbatda ishlating.",
    speakAboutCta: "Bu mavzuda gapirish",
  },

  // "Mening so'zlarim" - every word the learner added via "Lug'atga qo'shish". They go straight
  // into SRS (first review after 3 kun), so without this list they were invisible until then.
  mySavedWords: {
    title: "Mening so'zlarim",
    subtitle:
      "Lug'atga qo'shgan so'zlaringiz. Har biri 3 / 7 / 21 kun jadvali bo'yicha takrorlanadi.",
    cta: "Mening so'zlarim",
    back: "Lug'atga qaytish",
    count: (n: number) => `${n} ta so'z`,
    empty: "Hali so'z qo'shmagansiz.",
    emptyHint:
      "Biror mavzuni oching va so'z yonidagi \"Lug'atga qo'shish\" tugmasini bosing - so'z shu yerda paydo bo'ladi.",
    browseTopics: "Mavzularni ko'rish",
    flipHint: "O'girish",
    dueNow: "Takrorlashga tayyor",
    reviewNowCta: "Hozir takrorlash",
    // Human date for the next scheduled review of a not-yet-due word.
    nextReview: (date: string) => `Keyingi takrorlash: ${date}`,
    mastered: "O'zlashtirildi",
    searchPlaceholder: "Mavzu yoki so'zni qidiring...",
    topicsTitle: "O'rganilgan mavzular",
    topicsHint: "Mavzuni tanlang va undagi so'zlarni yozish hamda tinglash orqali mustahkamlang.",
    otherWordsTitle: "Boshqa saqlangan so'zlar",
    otherWordsHint: "Video, suhbat yoki qo'lda saqlangan so'zlar.",
    unknownTopic: "Saqlangan mavzu",
    masteryProgress: (mastered: number, total: number) => `${total} tadan ${mastered} tasi o'zlashtirilgan`,
    openTopicPractice: "Mashqlarni boshlash",
    loadError: "So'zlarni yuklab bo'lmadi.",
    loadErrorHint: "Internet aloqasini tekshirib, qayta urinib ko'ring.",
    noSearchResults: "Hech narsa topilmadi.",
    searchAgain: "Boshqa mavzu yoki so'z nomini qidirib ko'ring.",
    clearSearch: "Qidiruvni tozalash",
    practiceSubtitle: (count: number) => `${count} ta so'z · yozish va tinglash`,
    questionProgress: (current: number, total: number) => `${current} / ${total} savol`,
    writeSection: "Yozish",
    writePrompt: "O'zbekcha ma'noga mos inglizcha so'zni yozing",
    writePlaceholder: "Inglizcha so'z...",
    checkAnswer: "Tekshirish",
    listeningSection: "Tinglab topish",
    listeningPrompt: "So'zni tinglang va to'g'ri javobni tanlang",
    listenAgain: "Qayta tinglash",
    correct: "To'g'ri!",
    correctAnswer: (answer: string) => `To'g'ri javob: ${answer}`,
    nextQuestion: "Keyingi savol",
    practiceDone: "Mashq yakunlandi!",
    practiceResult: (correct: number, total: number) => `${total} ta savoldan ${correct} tasiga to'g'ri javob berdingiz.`,
    resultLabel: "Natija",
    practiceAgain: "Yana ishlash",
    backToSaved: "Mening so'zlarim",
    practiceLoadError: "Mavzu mashqlarini yuklab bo'lmadi.",
    practiceLoadErrorHint: "Saqlangan so'zlar yoki mavzu ma'lumoti hozir mavjud emas.",
    practiceEmpty: "Bu mavzuda saqlangan so'z yo'q.",
    practiceEmptyHint: "Avval mavzuning Vocabulary mashqini yakunlang - so'zlar avtomatik shu yerga keladi.",

    // 3 / 7 / 21 kunlik takrorlash zinapoyasi (MilestoneTrack).
    milestoneCaption: "3 / 7 / 21 kun jadvali",
    milestoneDay: (days: number) => `${days}-kun`,
    milestoneAria: (done: number, total: number) => `${total} bosqichdan ${done} tasi bajarilgan`,
    dueBadge: (n: number) => `${n} ta takrorlash`,
    topicMastered: "Mavzu o'zlashtirildi",

    // Practice sahifasi - takrorlash (SRS) rejimi jadvalga yoziladi; erkin mashq yozilmaydi.
    practiceReviewEyebrow: "Bugungi takrorlash",
    practiceReviewNote: "Natija jadvalga yoziladi - 3 / 7 / 21 progress oldinga siljiydi.",
    practiceFreeEyebrow: "Erkin mashq",
    practiceFreeNote: "Bu mashq jadvalga ta'sir qilmaydi - shunchaki takrorlash uchun.",
    practiceReviewCta: (n: number) => `${n} ta so'zni takrorlash`,
    practiceFreeCta: "Erkin mashq qilish",
    practiceNothingDueTitle: "Hozircha takrorlash vaqti kelmagan",
    practiceNothingDueHint: "Keyingi takrorlashgacha so'zlarni erkin mashq bilan mustahkamlashingiz mumkin.",
    practiceSwitchToFree: "Erkin mashqqa o'tish",
    submitError: "Javob saqlanmadi. Qayta urinib ko'ring.",
    // Self-rated (spoken/listening) mini-tests - server hali baholay olmaydi (docs/development-guide.md B.1).
    selfRateTitle: "Bu so'zni esladingizmi?",
    selfRateYes: "Ha, esladim",
    selfRateNo: "Yo'q, unutdim",
    reviewDoneTitle: "Takrorlash yakunlandi!",
    reviewDoneNote: "So'zlaringizning keyingi takrorlash vaqti yangilandi.",
    reviewPassLabel: (correct: number, total: number) => `${total} tadan ${correct} tasi eslab qolindi.`,
  },

progress: {
    title: "Mening progressim",
    subtitle:
      "Sizning ingliz tili o'rganishdagi so'nggi yutuqlaringiz va tahlilingiz.",
    actionSubtitle:
      "Bugungi holatingizni ko'ring va eng foydali keyingi mashqdan davom eting.",
    dashboardLabel: "O'quv analitikasi",
    continueLearning: "O'qishni davom ettirish",
    analyticsHeading: "Asosiy ko'rsatkichlar",
    analyticsSubheading: "O'sishingiz, lug'at boyligingiz va xatolaringiz bir joyda.",
    loadError: "Progress ma'lumotlarini yuklab bo'lmadi. Birozdan so'ng qayta urinib ko'ring.",
    ai: {
      heading: "AI progress tahlili",
      subheading: "Ko'rsatkichlaringiz asosida kuchli tomonlar, xatolar va keyingi qadam.",
      poweredBy: "Hermes AI tahlili",
      fallback: "Tasdiqlangan statistik tavsiya",
      generatedAt: (date: string) => `Yangilandi: ${date}`,
      achievements: "Yutuqlaringiz",
      focusSkills: "Kuchaytirish kerak",
      recurringErrors: "Takrorlanayotgan xatolar",
      studyHabit: "O'qish odati",
      nextStep: "Eng yaxshi keyingi qadam",
      startAction: "Mashqni boshlash",
      openSkill: "Mashqqa o'tish",
      weeklyRhythm: "Haftalik ritm",
      yourAnswer: "Sizning javobingiz",
      correctAnswer: "To'g'ri javob",
      showDetails: "Batafsil tahlil",
      hideDetails: "Tahlilni yopish",
      score: (value: number) => `${Math.round(value)} ball`,
      trendUp: (value: number) => `8 haftada +${Math.round(value)} ball`,
      trendDown: (value: number) => `8 haftada ${Math.round(value)} ball`,
      trendFlat: "8 haftada o'zgarish kam",
      samples: (count: number) => `${count} ta natija asosida`,
      baseline: "Boshlang'ich baho - ko'proq mashqdan keyin aniqlashadi",
      errorCount: (count: number) => `Oxirgi 30 kunda ${count} marta`,
      errorTimes: (count: number) => `${count} marta`,
      confidence: {
        low: "Dastlabki xulosa",
        medium: "Yetarli ma'lumot",
        high: "Ishonchli tahlil",
      },
      overall: (code: string) =>
        ({
          no_data: "Tahlil uchun hali ma'lumot yetarli emas. Birinchi mashqni boshlang.",
          getting_started: "Boshlang'ich natijalaringiz tayyor. Muntazam mashq AI tahlilini yanada aniq qiladi.",
          strong_progress: "Natijalaringiz juda yaxshi. Kuchli ko'nikmalarni saqlab, eng zaif yo'nalishga e'tibor qarating.",
          steady_progress: "Barqaror o'syapsiz. Quyidagi ustuvor mashqlar sizni keyingi bosqichga tezroq olib boradi.",
          needs_focus: "O'sish uchun aniq imkoniyat bor. Avval eng ko'p xato va eng past ko'nikmaga ishlang.",
        } as Record<string, string>)[code] ?? "Statistikalaringiz tahlil qilindi. Quyidagi ustuvor qadamdan boshlang.",
      achievement: (code: string) =>
        ({
          strong_skill: "Kamida bitta ko'nikmangiz 80 balldan yuqori.",
          positive_trend: "So'nggi haftalarda ko'nikma ballingiz o'sgan.",
          daily_goal_met: "Bugungi o'quv maqsadingiz bajarilgan.",
          vocabulary_growth: "Bu hafta faol lug'atingizga yangi so'zlar qo'shilgan.",
        } as Record<string, string>)[code] ?? "O'quv ko'rsatkichlaringizda ijobiy natija bor.",
      evidence: (code: string) =>
        ({
          placement_baseline: "Bu hozircha daraja testidagi boshlang'ich baho.",
          negative_trend: "So'nggi 8 haftalik yo'nalish pasaygan.",
          lowest_score: "Joriy ko'nikmalar orasida bu yo'nalish ko'proq e'tibor talab qiladi.",
        } as Record<string, string>)[code] ?? "Joriy ball va mashq natijalari shu yo'nalishni ustuvor ko'rsatmoqda.",
      habit: (code: string) =>
        ({
          no_study_data: "Hali o'qish vaqti qayd etilmagan.",
          start_habit: "Qisqa kundalik mashq bilan muntazam odatni boshlang.",
          habit_at_risk: "Bugun mashq qilmasangiz, joriy seriyangiz uzilishi mumkin.",
          habit_consistent: "Kamida 7 kunlik muntazam o'qish odatini shakllantirgansiz.",
          habit_building: "O'qish odatingiz shakllanmoqda - bugungi rejani davom ettiring.",
        } as Record<string, string>)[code] ?? "Muntazam qisqa mashqlar uzoq muddatli o'sishni kuchaytiradi.",
    },
    topicsProgress: "Mavzular bo'yicha progress",
    vocabularyInsight: "Har bir yangi so'z faol lug'atingizni kengaytiradi.",
    pronunciationLevel: "Talaffuz darajasi",
    last30Avg: "Oxirgi 30 kundagi o'rtacha ball",
    vocabularySize: "Lug'at boyligi",
    newWordsLearned: "Yangi o'rganilgan so'zlar",
    errorTypes: "Xatolik turlari",
    errorTypesSub: "Eng ko'p yo'l qo'yilgan xatolar",
    heatmapHint: "Uzunroq ustun ko'proq uchragan xatoni anglatadi.",
    errorChartSummary: "Xatolar soni eng ko'p uchragan turdan boshlab kamayish tartibida ko'rsatilgan.",
    weeklyTip: "Haftalik tavsiya",
    startExercises: "Mashqlarni boshlash",
    dailyGoal: "Bugungi reja",
    heroTitle: (level: string) => `${level} darajaga yo'lingiz davom etmoqda`,
    heroSubtitle: (remaining: string) =>
      `Keyingi bosqichga yaqinlashish uchun yana ${remaining} ta mavzuni yakunlang.`,
    weekStudyDetail: "Oxirgi 7 kundagi jami o'qish vaqti",
    streakRisk: "Bugun mashq qilinmasa seriya uzilishi mumkin",
    streakSafe: "Muntazamlikni saqlab turibsiz",
    wordsAdded: (count: number) => `Bu hafta +${count} yangi so'z`,
    focusSkill: "Hozirgi fokus",
    scoreDetail: (score: number) => `Joriy natija: ${score}/100`,
    showStatistics: "Ko'proq statistika",
    hideStatistics: "Statistikani yopish",
    skills: "Ko'nikmalar",
    today: "Bugun",
    // Skills analysis block
    skillsHeading: "Ko'nikmalar tahlili",
    skillsSubheading: "Har bir ko'nikma bo'yicha darajangiz va o'sishingiz.",
    skillBalance: "Ko'nikma balansi",
    skillBalanceSub: "Har bir ko'nikmadagi joriy ball (0–100)",
    skillScores: "Ko'nikma ballari",
    skillTrend: "Umumiy o'sish",
    skillTrendSub: "Haftalik o'rtacha ball (barcha ko'nikmalar)",
    currentScore: "Joriy ball",
    skillBaseline: "Boshlang'ich baho",
    skillBaselineHint: "Bu ko'nikma hali mashq qilinmagan - ball placement testidan olingan taxminiy baho.",
    errorsHeading: "Xatolar tahlili",
    noErrors: "Ajoyib! Hozircha qayd etilgan xatolar yo'q.",
    noSkillData: "Hozircha ko'nikma ma'lumoti yo'q - mashqlarni boshlang.",
    noTrendData:
      "Grafik uchun yetarli ma'lumot yo'q - bir necha kun mashq qiling.",
    // Time-statistics block (today/week/month/year/all-time + charts).
    stats: {
      heading: "Vaqt statistikasi",
      subheading: "Ingliz tili o'rganishga sarflagan vaqtingiz.",
      today: "Bugun",
      thisWeek: "Bu hafta",
      thisMonth: "Bu oy",
      thisYear: "Bu yil",
      allTime: "Jami",
      last7Days: "Oxirgi 7 kun",
      last12Months: "Oxirgi 12 oy",
      yearActivity: "Yillik faollik",
      bySkill: "Ko'nikma bo'yicha vaqt",
      activeDays: "Faol kunlar",
      dayStreak: "Kunlik seriya",
      avgPerDay: "Kunlik o'rtacha",
      longestDay: "Eng samarali kun",
      noData: "Hozircha ma'lumot yo'q",
      less: "kam",
      more: "ko'p",
      heatmapHint: "To'qroq rang - ko'proq mashq qilingan kun.",
      heatmapSummary: (activeDays: number, totalSeconds: number) =>
        `Yillik faollik: ${activeDays} faol kun, jami ${Math.round(totalSeconds / 3600)} soat mashq.`,
      hour: "soat",
      minute: "daqiqa",
      second: "soniya",
      zero: "0 daqiqa",
      todayDetail: "Bugun qayd etilgan o'qish vaqti",
      monthDetail: "Joriy oy davomida",
      avgDetail: "Faol kunlardagi o'rtacha vaqt",
      days: (n: number) => `${n} kun`,
      months: [
        "Yan",
        "Fev",
        "Mar",
        "Apr",
        "May",
        "Iyn",
        "Iyl",
        "Avg",
        "Sen",
        "Okt",
        "Noy",
        "Dek",
      ],
      weekdays: ["Du", "Se", "Ch", "Pa", "Ju", "Sh", "Ya"],
    },
    // Level progress
    levelProgressTitle: "Daraja progressi",
    levelProgressSub: "Joriy darajangiz va keyingi bosqichga yaqinlashingiz",
    currentLevelLabel: "Joriy daraja",
    targetLevelLabel: "Keyingi daraja",
    topicsLearned: "O'rganilgan mavzular",
    skillsMastered: "Egallangan ko'nikmalar",
    exitTest: "Chiqish testi",
    passed: "O'tildi",
    pending: "Kutilmoqda",
    cefrLevel: "CEFR darajasi",
    progressToNext: "Keyingi darajaga progress",
    topicsRemaining: (count: string) => `${count} ta mavzu qoldi`,
    maxLevelReached: "Maksimal daraja yetib bordingiz!",
    maxLevelSub: "Siz eng yuqori C2 darajasidasiz. Davom eting!",
    // Topics
    topicsHeading: "Mavzular progressi",
    topicsSubheading: "Har bir bo'lim 6 ta asosiy bosqichdan iborat. Barchasini o'ting!",
    topicsEmptyTitle: "Mavzular yo'q",
    topicsEmptyText: "Bu daraja uchun hali mavzular yuklanmagan.",
    topicLocked: "Bu mavzu hali ochilmagan",
    modulesPassed: "modul o'tildi",
    topicMastered: "Mavzu to'liq o'rganildi!",
    // Gamification
    gamificationTitle: "Gamifikatsiya va seriya",
    gamificationSub: "Kunlik maqsad, seriya va bugungi reja",
    currentStreak: "Joriy seriya",
    longestStreak: "Eng uzun seriya",
    streakAtRisk: "Seriya xavfda!",
    dailyGoalMet: "Kunlik maqsad bajarildi!",
    dailyGoalPending: "Kunlik maqsad hali bajarilmagan",
    todayPlan: "Bugungi reja (6 ko'nikma)",
  },

  leaderboard: {
    title: "Reyting",
    subtitle: "O'z darajangizdagi eng faol o'quvchilar bilan taqqoslang.",
    heroCopy:
      "Umumiy natijangizni kuzating, ligangizni oshiring va to'plangan ballarni chegirmalarga almashtiring.",
    yourRank: (rank: number) => `Sizning o'rningiz: ${rank}`,
    thisWeek: (rank: number) => `Bu hafta: ${rank}-o'rinda`,
    outOfTop: (rank: number) => `${rank}-o'rindasiz`,
    overallRank: (rank: number) => `Umumiy reyting: #${rank}`,
    points: "ball",
    you: "Siz",
    board: {
      kicker: "Reyting jadvali",
      title: "Eng kuchli o'quvchilar",
      copy: "XP to'plang, yuqori o'rinlarga chiqing va o'z ligangizda yetakchilik qiling.",
      participants: (count: number) => `${count} ishtirokchi`,
      overallRanking: "Umumiy reyting",
      colRank: "O'rin",
      colLearner: "O'quvchi",
      colScore: "Jami XP",
    },
    empty: "Bu darajada hali reyting ma'lumoti yo'q - birinchi bo'lib boshlang!",
    loadError: "Reytingni yuklab bo'lmadi. Birozdan so'ng qayta urinib ko'ring.",
    proBadge: "Pro",
    rulesTitle: "Qoidalar",
    rulesButton: "Qoidalarni ko'rish",
    practiceMore: "Yana mashq qilish",
    rules: [
      "Har hafta yangi liga boshlanadi.",
      "Top 3 - Oltin ligaga chiqadi.",
      "Top 10 - Kumush ligaga chiqadi.",
      "Qolganlar - Bronza ligada qoladi.",
    ],
    pointsCard: {
      title: "Ballaringiz",
      subtitle: "Har bir mashqni yakunlaganingizda ball to'planadi.",
      lifetimeXp: "Jami ball",
      spendableCoins: "Sarflash mumkin bo'lgan ball",
      discountsHeading: "Pro obunaga chegirma",
      discountsSubheading: "To'plangan ballarni Pro obunaga chegirmaga almashtiring.",
      redeem: "Almashtirish",
      redeeming: "Almashtirilmoqda...",
      notEnough: "Yetarli ball yo'q",
      redeemSuccess: (percent: number, code: string) =>
        `${percent}% chegirma kodi olindi: ${code}`,
      redeemError: "Chegirma olishda xatolik yuz berdi. Qayta urinib ko'ring.",
      activeCodesHeading: "Faol chegirma kodlaringiz",
      noActiveCodes: "Hozircha faol chegirma kodingiz yo'q.",
      expiresOn: (date: string) => `${date} gacha amal qiladi`,
      discountOff: (percent: number) => `${percent}% chegirma`,
    },
  },

  profile: {
    levelLabel: (level: string) => `Daraja: ${level}`,
    learningSince: (date: string) => `Ingliz tilini ${date} beri o'rganmoqda`,
    share: "Ulashish",
    history: "Tarix",
    dailyStreak: "Kunlik streak",
    totalStudyTime: "Jami o'rganish vaqti",
    hours: (n: number) => `${n} soat`,
    days: (n: number) => `${n} kun`,
    longestStreak: "Eng uzun streak",
    premiumActive: "Premium faol",
    premiumText:
      "Cheksiz darslar, AI bilan jonli muloqot va reklamasiz o'rganish.",
    settings: "Sozlamalar",
    notificationsSection: "Bildirishnomalar",
    emailNotifications: "Email bildirishnomalar",
    emailNotificationsText: "Yangi darslar va haftalik natijalar haqida",
    pushNotifications: "Push bildirishnomalar",
    pushNotificationsText: "Kunlik eslatmalar va mashqlar",
    dailyGoal: "Kunlik maqsad",
    goalCalm: "Sokin (10 daqiqa)",
    goalNormal: "Normal (20 daqiqa)",
    goalIntense: "Intensiv (40 daqiqa)",
    languageBalance: "Til balansi",
    langUzbek: "Faqat o'zbekcha",
    langBilingual: "Ikki tilli",
    langEnglish: "Faqat inglizcha",
    accountSettings: "Hisob sozlamalari",
    developerApi: {
      title: "Dasturchilar uchun API",
      endpointLabel: "OpenAI bilan mos endpoint:",
      defaultName: "Mening ilovam",
      namePlaceholder: "Kalit nomi",
      create: "Yangi API kalit",
      waiting: "Kutilmoqda...",
      oneTimeWarning: "Bu kalit faqat bir marta ko'rsatiladi. Hozir nusxalang.",
      copy: "Nusxalash",
      active: "Faol",
      revoked: "Bekor qilingan",
      revoke: "Bekor qilish",
      loadError: "API kalitlarini yuklab bo'lmadi.",
      createError: "API kalitini yaratib bo'lmadi.",
      revokeError: "API kalitini bekor qilib bo'lmadi.",
    },
    changePassword: "Parolni o'zgartirish",
    editEmail: "Emailni tahrirlash",
    premiumTitle: "Premium imkoniyatlar",
    goPremium: "Premium'ga o'tish",
    logout: "Chiqish",
    // Profile editing (display name + username)
    usernameLabel: "Foydalanuvchi nomi",
    displayNameLabel: "Ko'rinadigan ism",
    edit: "Tahrirlash",
    save: "Saqlash",
    cancel: "Bekor qilish",
    saving: "Saqlanmoqda...",
    saved: "Saqlandi",
    saveError: "Saqlashda xatolik. Qayta urinib ko'ring.",
    displayNameEmpty: "Ism bo'sh bo'lishi mumkin emas.",
    editProfile: "Profilni tahrirlash",
    settingsSaved: "Sozlamalar saqlandi",
    // Subscription plans (H.1). Premium tiers reuse uz.paywall.plans; freemium is the default
    // tier. Payment gateways (Click/Payme) are not wired yet - hence the "coming soon" note.
    plansTitle: "Obuna rejalari",
    plansSubtitle: "O'zingizga mos rejani tanlang. Uzoqroq reja - arzonroq.",
    planFreemiumLabel: "Bepul",
    planFreemiumPrice: "0 so'm",
    planFreemiumPer: "doimiy",
    planFreemiumDesc: "Dastlabki 3 ta mavzu va kuniga 1 ta AI suhbat.",
    trialTitle: "Bepul Pro faollashtirilgan",
    trialBody: "Barcha Pro imkoniyatlaridan 30 kun bepul foydalanishingiz mumkin.",
    trialUntil: (date: string) => `${date}gacha bepul Pro`,
    currentPlan: "Joriy reja",
    choosePlanCta: "Tanlash",
    // Premium tiers are shown for preview only until a real payment gateway is wired up.
    planComingSoon: "Tez orada",
    expiresInDays: (n: number) => `${n} kun qoldi`,
    paymentSoon:
      'To\'lov tizimi (Click va Payme) tez orada ulanadi. Hozircha faqat "Bepul" reja faol.',
    // Account deletion (danger zone). Multi-step confirmation: enter email → confirm → delete.
    dangerZone: "Xavfli hudud",
    deleteAccountTitle: "Hisobni o'chirish",
    deleteAccountWarning:
      "Hisobingiz va barcha ma'lumotlaringiz (progress, streak, lug'at, obuna) butunlay o'chiriladi. Bu amalni orqaga qaytarib bo'lmaydi.",
    deleteAccountButton: "Hisobni o'chirish",
    deleteEmailPrompt: "Tasdiqlash uchun email manzilingizni kiriting:",
    deleteEmailPlaceholder: "Email manzilingiz",
    deleteEmailMismatch: "Email manzil hisobingizdagi bilan mos kelmadi.",
    deleteContinue: "Davom etish",
    deleteConfirmQuestion:
      "Haqiqatan ham hisobingizni butunlay o'chirmoqchimisiz?",
    deleteConfirmYes: "Ha, davom etish",
    deleteConfirmNo: "Yo'q, bekor qilish",
    deleteFinal: "O'chirish",
    deleting: "O'chirilmoqda...",
    deleteError: "Hisobni o'chirishda xatolik. Qayta urinib ko'ring.",
    cancelDelete: "Bekor qilish",
  },

  // Referral programme (Profile "Invite friends" section). Rewards are cheap + capped: every
  // qualified friend unlocks +2 topics and a small Speaking/Writing bonus for BOTH sides.
  referral: {
    title: "Do'stlarni taklif qiling",
    subtitle:
      "Havolangizni ulashing. Do'stingiz kodingiz bilan ro'yxatdan o'tishi bilanoq - ikkalangizga ham +2 mavzu, speaking va writing bonusi ochiladi.",
    yourCode: "Sizning kodingiz",
    copy: "Nusxa olish",
    copied: "Nusxa olindi!",
    shareLink: "Havolani ulashish",
    shareTelegram: "Telegram",
    invited: (n: number) => `${n} do'st taklif qilingan`,
    qualified: (n: number) => `${n} tasdiqlangan`,
    progress: (used: number, cap: number) => `${used} / ${cap} bonus olindi`,
    bonusTitle: "Ishlagan bonuslaringiz",
    bonusTopics: (n: number) => `+${n} mavzu`,
    bonusSpeaking: (n: number) => `${n} speaking`,
    bonusWriting: (n: number) => `${n} writing`,
    perReferral: (t: number, s: number, w: number) =>
      `Har taklif uchun: +${t} mavzu, +${s} speaking, +${w} writing`,
    capReached: "Siz maksimal bonusga yetdingiz. Rahmat!",
    shareMessage: (code: string, url: string) =>
      `Sun'iy intellekt bilan ingliz tilini o'rgan! Mening taklif kodim: ${code}\n${url}`,
    error: "Ma'lumotni yuklab bo'lmadi.",
  },

  // Operator admin panel. Visible only to admins; the promote/demote controls only to the
  // super-admin. All strings are vetted templates (rule 11).
  admin: {
    title: "Admin panel",
    subtitle: "Foydalanuvchilar va ularning ma'lumotlari",
    loading: "Yuklanmoqda...",
    loadError: "Ma'lumotlarni yuklab bo'lmadi.",
    forbidden: "Bu sahifaga kirish uchun ruxsatingiz yo'q.",
    // Back-link from a sub-page to the admin hub.
    back: "Admin panelga qaytish",
    // AdminShell header: link back to the learner-facing app.
    backToApp: "Ilovaga qaytish",
    // Admin hub: the /admin landing page is now just navigation cards to the separate admin sections.
    hub: {
      metrics: "O'sish paneli",
      metricsHint: "DAU, retention va konversiya ko'rsatkichlari",
      users: "Foydalanuvchilar",
      usersHint: "Ro'yxat, rollar va obunalarni boshqarish",
      support: "Support",
      supportHint: "Foydalanuvchi murojaatlari va suhbatlarini boshqarish",
      vocabulary: "Lug'at mavzulari",
      vocabularyHint: "Barcha vocab mavzularini ko'rish va tahrirlash",
      grammar: "Grammatika darslari",
      grammarHint: "Grammatika darslarini yaratish va tahrirlash",
      listening: "Tinglash mashqlari",
      listeningHint: "Audio mashqlarni boshqarish va yuklash",
      reading: "O'qish matnlari",
      readingHint: "Matnlar va savollarni boshqarish",
      writing: "Yozuv topshiriqlari",
      writingHint: "Yozuv topshiriqlarini yaratish va tahrirlash",
      notifications: "Bildirishnomalar boshqaruvi",
      notificationsHint: "Broadcast yuborish va kunlik eslatmalar",
      sections: "Bo'limlarni sinash",
      sectionsHint: "Foydalanuvchi bo'limlarini o'zingiz sinab ko'ring",
      server: "Server holati",
      serverHint: "Bog'lanishlar, xizmatlar va so'nggi xatoliklar",
    },
    // Listening exercises admin CRUD (open to any admin).
    listening: {
      title: "Tinglash mashqlari",
      subtitle: "Audio mashqlarni boshqarish va yuklash",
      back: "Orqaga",
      loadError: "Mashqlarni yuklashda xatolik",
      forbidden: "Sizda ruxsat yo'q",
      search: "Qidirish",
      empty: "Mashqlar topilmadi",
      createNew: "Yangi mashq",
      edit: "Tahrirlash",
      delete: "O'chirish",
      confirmDelete: "Haqiqatan ham o'chirmoqchimisiz?",
      colTitle: "Sarlavha",
      colTopic: "Mavzu",
      colLevel: "Daraja",
      colStatus: "Holat",
      colQuestions: "Savollar",
      colActions: "Amallar",
      save: "Saqlash",
      cancel: "Bekor qilish",
      creating: "Yaratilmoqda...",
      updating: "Yangilanmoqda...",
      deleting: "O'chirilmoqda...",
      updateError: "Yangilashda xatolik",
      createError: "Yaratishda xatolik",
      deleteError: "O'chirishda xatolik",
      level: "Daraja",
      titlePlaceholder: "Sarlavhani kiriting",
      topicPlaceholder: "Mavzu nomini kiriting",
      createTitle: "Yangi tinglash mashqi",
      editTitle: "Tinglash mashqini tahrirlash",
    },
    // Reading passages admin CRUD (open to any admin).
    reading: {
      title: "O'qish matnlari",
      subtitle: "Matnlar va savollarni boshqarish",
      back: "Orqaga",
      loadError: "Matnlarni yuklashda xatolik",
      forbidden: "Sizda ruxsat yo'q",
      search: "Qidirish",
      empty: "Matnlar topilmadi",
      createNew: "Yangi matn",
      edit: "Tahrirlash",
      delete: "O'chirish",
      confirmDelete: "Haqiqatan ham o'chirmoqchimisiz?",
      colTitle: "Sarlavha",
      colTopic: "Mavzu",
      colLevel: "Daraja",
      colStatus: "Holat",
      colQuestions: "Savollar",
      colActions: "Amallar",
      save: "Saqlash",
      cancel: "Bekor qilish",
      creating: "Yaratilmoqda...",
      updating: "Yangilanmoqda...",
      deleting: "O'chirilmoqda...",
      updateError: "Yangilashda xatolik",
      createError: "Yaratishda xatolik",
      deleteError: "O'chirishda xatolik",
      level: "Daraja",
      titlePlaceholder: "Sarlavhani kiriting",
      createTitle: "Yangi o'qish matni",
      editTitle: "O'qish matnini tahrirlash",
    },
    // Writing tasks admin CRUD (open to any admin). Only the CEFR level is editable - the word
    // range and prompt are derived/generated server-side.
    writing: {
      title: "Yozuv topshiriqlari",
      subtitle: "Yozuv topshiriqlarini yaratish va tahrirlash",
      back: "Orqaga",
      loadError: "Topshiriqlarni yuklashda xatolik",
      retry: "Qayta urinish",
      forbidden: "Sizda ruxsat yo'q",
      search: "Qidirish",
      empty: "Topshiriqlar topilmadi",
      createNew: "Yangi topshiriq",
      edit: "Tahrirlash",
      delete: "O'chirish",
      confirmDelete: "Haqiqatan ham o'chirmoqchimisiz?",
      colLevel: "Daraja",
      colStatus: "Holat",
      colWords: "So'zlar",
      colPrompt: "Prompt",
      colActions: "Amallar",
      save: "Saqlash",
      cancel: "Bekor qilish",
      creating: "Yaratilmoqda...",
      updating: "Yangilanmoqda...",
      deleting: "O'chirilmoqda...",
      updateError: "Yangilashda xatolik",
      createError: "Yaratishda xatolik",
      deleteError: "O'chirishda xatolik",
      level: "Daraja",
      createTitle: "Yangi yozuv topshiriqi",
      editTitle: "Yozuv topshiriqini tahrirlash",
    },
    // Vocabulary topics admin CRUD (open to any admin).
    vocabulary: {
      title: "Lug'at mavzulari",
      subtitle: "Barcha mavzularni boshqaring",
      back: "Orqaga",
      loadError: "Mavzularni yuklashda xatolik",
      retry: "Qayta urinish",
      forbidden: "Sizda ruxsat yo'q",
      search: "Qidirish",
      empty: "Mavzular topilmadi",
      createNew: "Yangi mavzu",
      edit: "Tahrirlash",
      delete: "O'chirish",
      confirmDelete: "Haqiqatan ham o'chirmoqchimisiz?",
      colTitle: "Sarlavha",
      colTitleUz: "Sarlavha (uz)",
      colLevel: "Daraja",
      colCategory: "Kategoriya",
      colStatus: "Holat",
      colWords: "So'zlar",
      colSequence: "Tartib",
      save: "Saqlash",
      cancel: "Bekor qilish",
      creating: "Yaratilmoqda...",
      updating: "Yangilanmoqda...",
      deleting: "O'chirilmoqda...",
      updateError: "Yangilashda xatolik",
      createError: "Yaratishda xatolik",
      deleteError: "O'chirishda xatolik",
      slug: "Slug",
      grammarFocus: "Grammatik yo'nalish",
      level: "Daraja",
      sequence: "Tartib",
      titlePlaceholder: "Sarlavhani kiriting",
      titleUzPlaceholder: "Sarlavhani o'zbekcha kiriting",
      categoryPlaceholder: "Kategoriyani kiriting",
      grammarFocusPlaceholder: "Grammatik yo'nalishni kiriting",
      createTitle: "Yangi mavzu yaratish",
      editTitle: "Mavzuni tahrirlash",
      colActions: "Amallar",
    },
    // Grammar lessons admin CRUD (open to any admin).
    grammar: {
      title: "Grammatika darslari",
      subtitle: "Barcha darslarni boshqaring",
      back: "Orqaga",
      loadError: "Darslarni yuklashda xatolik",
      retry: "Qayta urinish",
      search: "Qidirish",
      empty: "Darslar topilmadi",
      createNew: "Yangi dars",
      edit: "Tahrirlash",
      delete: "O'chirish",
      confirmDelete: "Haqiqatan ham o'chirmoqchimisiz?",
      colTitle: "Sarlavha",
      colLevel: "Daraja",
      colCategory: "Kategoriya",
      colStatus: "Holat",
      colExercises: "Mashqlar",
      colActions: "Amallar",
      save: "Saqlash",
      cancel: "Bekor qilish",
      creating: "Yaratilmoqda...",
      updating: "Yangilanmoqda...",
      deleting: "O'chirilmoqda...",
      updateError: "Yangilashda xatolik",
      createError: "Yaratishda xatolik",
      deleteError: "O'chirishda xatolik",
      level: "Daraja",
      titlePlaceholder: "Sarlavhani kiriting",
      createTitle: "Yangi dars yaratish",
      editTitle: "Darsni tahrirlash",
    },
    // Dedicated users page (was the table on the old admin panel).
    usersTitle: "Foydalanuvchilar",
    usersSubtitle: "Foydalanuvchilar va ularning ma'lumotlari",
    // Super-admin preview: open the learner-facing SRS review and saved-words sections to test them.
    previewTitle: "Bo'limlarni sinash",
    previewHint: "Foydalanuvchi bo'limlarini o'zingiz sinab ko'ring.",
    previewReview: "Takrorlash (SRS)",
    previewReviewHint: "4 bo'limli takrorlash sessiyasi",
    previewSaved: "Mening so'zlarim",
    previewSavedHint: "Saqlangan so'zlar ro'yxati",
    forceDue: "So'zlarni takrorlashga chaqirish",
    forceDueHint:
      "Mening so'zlarimdagi barcha so'zlar darhol takrorlash navbatiga qo'yiladi (sinov uchun)",
    forceDueRunning: "Bajarilmoqda...",
    forceDueDone: (dueNow: number) =>
      `${dueNow} ta so'z takrorlash navbatida. Endi "Takrorlash (SRS)" sahifasini oching.`,
    forceDueEmpty: "Mening so'zlarimda hali so'z yo'q. Avval so'z saqlang.",
    forceDueError: "Xatolik yuz berdi. Qayta urinib ko'ring.",
    // Super-admin notification management: compose a broadcast to all learners + manual daily dispatch.
    notifTitle: "Bildirishnomalar boshqaruvi",
    notifHint:
      "Foydalanuvchilarga bildirishnoma yuboring yoki kunlik eslatmalarni qo'lda ishga tushiring.",
    broadcastTitleLabel: "Sarlavha",
    broadcastTitlePlaceholder: "Masalan: Yangi imkoniyat!",
    broadcastBodyLabel: "Matn",
    broadcastBodyPlaceholder: "Bildirishnoma matnini yozing...",
    broadcastDestLabel: "Ilova bo'limi (ixtiyoriy)",
    broadcastDestNone: "Yo'nalishsiz",
    broadcastUrlLabel: "Tashqi havola (ixtiyoriy)",
    broadcastUrlPlaceholder: "https://...",
    broadcastUrlHint:
      "To'ldirilsa, bildirishnomada tugma bo'lib chiqadi va bosilganda shu sahifani ochadi (ilova bo'limidan ustun turadi).",
    broadcastUrlInvalid:
      "Havola https:// yoki http:// bilan boshlanishi kerak.",
    broadcastSend: "Yuborish",
    broadcastSending: "Yuborilmoqda...",
    broadcastSent: "Bildirishnoma barcha foydalanuvchilarga yuborildi.",
    broadcastError: "Yuborib bo'lmadi. Qayta urinib ko'ring.",
    broadcastValidation: "Sarlavha va matnni to'ldiring.",
    broadcastRecent: "Yaqinda yuborilganlar",
    broadcastEmpty: "Hali bildirishnoma yuborilmagan.",
    broadcastDelete: "O'chirish",
    broadcastDeleteConfirm: "Bu bildirishnomani ro'yxatdan o'chirasizmi?",
    broadcastDeleteError: "O'chirib bo'lmadi. Qayta urinib ko'ring.",
    dispatchDaily: "Kunlik eslatmalarni yuborish",
    dispatchDailyHint:
      "Rejalashtirilgan kunlik SRS eslatmalari yuborilmagan bo'lsa, qo'lda ishga tushiring.",
    dispatchDailyRunning: "Yuborilmoqda...",
    dispatchDailyDone: (n: number) =>
      `${n} ta foydalanuvchiga eslatma yuborildi.`,
    dispatchDailyError: "Yuborib bo'lmadi. Qayta urinib ko'ring.",
    statTotalSent: "Jami yuborilgan",
    statWithLink: "Havolali",
    statLastSent: "So'nggi yuborish",
    statLastSentNever: "Hali yo'q",
    // Summary cards
    totalUsers: "Jami foydalanuvchilar",
    admins: "Adminlar",
    onboarded: "Boshlagan",
    premium: "Premium",
    // Table headers
    colUser: "Foydalanuvchi",
    colRole: "Rol",
    colLevel: "Daraja",
    colSubscription: "Obuna",
    colRegistered: "Ro'yxatdan o'tgan",
    colLastLogin: "Oxirgi kirish",
    colLastActivity: "Oxirgi faollik",
    colActions: "Amallar",
    // Roles
    roleSuperAdmin: "Super admin",
    roleAdmin: "Admin",
    roleLearner: "O'quvchi",
    // Subscription statuses (server sends the English status code)
    subFree: "Bepul",
    subPremium: "Premium",
    subCancelled: "Bekor qilingan",
    subExpired: "Muddati tugagan",
    // Misc values
    notOnboarded: "Boshlamagan",
    never: "-",
    noUsername: "-",
    search: "Qidirish (ism yoki email)",
    empty: "Foydalanuvchilar topilmadi.",
    // Promote / demote
    makeAdmin: "Admin qilish",
    removeAdmin: "Adminlikdan olish",
    updating: "Saqlanmoqda...",
    updateError: "O'zgartirishni saqlab bo'lmadi.",
    confirmMakeAdmin: (name: string) => `${name}ni admin qilasizmi?`,
    confirmRemoveAdmin: (name: string) => `${name}ni adminlikdan olasizmi?`,
    youBadge: "Siz",
    // Super-admin → server-operations page (ServerHealthPage). Only the super-admin sees this link.
    openServer: "Server holati",
    server: {
      title: "Server holati",
      subtitle:
        "Server qanday ishlayapti - bog'lanishlar, xizmatlar va so'nggi xatoliklar.",
      forbidden: "Bu sahifani faqat super-admin ko'ra oladi.",
      loadError: "Server ma'lumotlarini yuklab bo'lmadi.",
      refresh: "Yangilash",
      autoRefresh: "Har 8 soniyada avtomatik yangilanadi",
      updatedAt: (time: string) => `Yangilangan: ${time}`,
      // Overall banner
      statusHealthy: "Hammasi joyida",
      statusDegraded: "Qisman ishlamoqda",
      statusUnhealthy: "Muammo bor",
      statusHealthyHint: "Barcha bog'lanishlar sog'lom.",
      statusDegradedHint: "Ba'zi bog'lanishlar sozlanmagan.",
      statusUnhealthyHint: "Bir yoki bir nechta bog'lanish ishlamayapti.",
      // Runtime section
      runtimeTitle: "Ish holati",
      uptime: "Ishlash vaqti",
      environment: "Muhit",
      version: "Versiya",
      framework: "Platforma",
      os: "Operatsion tizim",
      machine: "Server nomi",
      startedAt: "Ishga tushgan",
      cpuCores: "Protsessor yadrolari",
      managedMemory: "Xotira (GC)",
      workingSet: "Umumiy xotira",
      threads: "Oqimlar",
      gc: "GC yig'ishlari (0/1/2)",
      // Dependencies section
      dependenciesTitle: "Bog'lanishlar",
      depHealthy: "Sog'lom",
      depUnhealthy: "Ishlamayapti",
      depNotConfigured: "Sozlanmagan",
      latency: (ms: number) => `${ms} ms`,
      // Services section
      servicesTitle: "Tashqi xizmatlar",
      svcConfigured: "Ulangan",
      svcNotConfigured: "Ulanmagan",
      // Logs section
      logsTitle: "So'nggi ogohlantirish va xatoliklar",
      logsEmpty: "Hozircha ogohlantirish yoki xatolik yo'q.",
      logsSummary: (errors: number, warnings: number) =>
        `${errors} ta xatolik, ${warnings} ta ogohlantirish`,
      errorsButton: (count: number) => `Xatoliklar (${count})`,
      warningsButton: (count: number) => `Ogohlantirishlar (${count})`,
      logLevelError: "Xatolik",
      logLevelWarning: "Ogohlantirish",
      logLevelFatal: "Jiddiy",
      showException: "Batafsil (stack trace)",
      logDelete: "O'chirish",
      logDeleteError: "O'chirib bo'lmadi. Qayta urinib ko'ring.",
      logsClear: "Hammasini tozalash",
      logsClearConfirm:
        "Barcha ogohlantirish va xatoliklarni ro'yxatdan tozalaysizmi?",
    },
    // Admin → video ingestion screen (AdminVideoPage)
    videoIngestTitle: "Video qo'shish (ingestion)",
    videoIngestSubtitle:
      "YouTube video ID va mavzusini kiriting - katalogga qo'shiladi, CEFR darajasi va transkript avtomatik aniqlanadi.",
    videoIdLabel: "YouTube video ID",
    videoIdPlaceholder: "masalan: dQw4w9WgXcQ",
    videoIdHint: "URL'dagi v= dan keyingi qism (to'liq havola emas).",
    topicLabel: "Mavzu",
    topicPlaceholder: "masalan: Sayohat, Texnologiya, Kundalik suhbat",
    submit: "Katalogga qo'shish",
    queued:
      "Qabul qilindi - video fonda qayta ishlanmoqda. Tayyor bo'lgach katalogda paydo bo'ladi.",
    ingested: "Video katalogga qo'shildi!",
    error: "Qo'shishda xatolik yuz berdi. Video ID to'g'riligini tekshiring.",
    openVideo: "Videolar katalogini ochish",
  },

  // Founder growth dashboard (DAU / retention / conversion). Visible only to admins. All strings
  // are vetted templates (rule 11); the server sends only numbers and dates.
  founder: {
    title: "O'sish paneli",
    subtitle:
      "Faollik, ushlab qolish va konversiya - asoschi uchun asosiy metrikalar.",
    asOf: (date: string) => `Holat: ${date}`,
    backToUsers: "Foydalanuvchilar ro'yxati",
    openDashboard: "O'sish paneli",
    loading: "Yuklanmoqda...",
    loadError: "Metrikalarni yuklab bo'lmadi.",
    loadErrorTitle: "Xatolik",
    forbidden: "Bu sahifaga kirish uchun ruxsatingiz yo'q.",
    forbiddenTitle: "Ruxsat yo'q",

    // Section headings
    sectionEngagement: "Faollik",
    sectionGrowth: "O'sish",
    sectionActivationFunnel: "Activation Funnel",
    sectionRetention: "Ushlab qolish (retention)",
    sectionConversion: "Konversiya va daromad",
    sectionFunnel: "Konversiya voronkasi",
    sectionPlans: "Tariflar bo'yicha",

    // Overview cards
    dau: "Kunlik faol (DAU)",
    wau: "Haftalik faol (WAU)",
    mau: "Oylik faol (MAU)",
    stickiness: "Yopishqoqlik (DAU/MAU)",
    totalUsers: "Jami foydalanuvchilar",
    onboarded: "Boshlagan",
    premium: "Premium",
    free: "Bepul",
    conversion: "Konversiya (bepul→premium)",
    newToday: "Bugun yangi",
    new7d: "7 kunda yangi",
    mrr: "Oylik daromad (MRR)",
    arppu: "To'lovchi boshiga (ARPPU)",

    // Charts
    activeTrend: "Kunlik faol foydalanuvchilar (30 kun)",
    signupsTrend: "Yangi ro'yxatdan o'tishlar (30 kun)",
    retentionChart: "Ushlab qolish darajasi (%)",
    chartUsers: "foydalanuvchi",
    chartSignups: "ro'yxatdan o'tgan",
    noData: "Hozircha ma'lumot yo'q.",

    // Activation funnel
    activationHint:
      "Ro'yxatdan o'tishdan monetizatsiyagacha bo'lgan real activation yo'li.",
    activationPitch: (
      registered: number,
      placement: number,
      speaking: number,
      d7: number
    ) =>
      `${registered} odam kirsa, ${placement} tasi placement tugatyapti, ${speaking} tasi birinchi speaking sessiya qilyapti, ${d7} tasi 7 kunda 3 marta gapiryapti.`,
    activationFromStart: (pct: string) => `Startdan: ${pct}`,
    activationFromPrevious: (pct: string) => `Oldingi: ${pct}`,
    activationColStart: "Startdan",
    activationColDropoff: "Pasayish",
    activationColCount: "Soni",
    activationColStep: "Bosqich",
    activationRegistered: "Ro'yxatdan o'tdi",
    activationUsername: "Username o'rnatdi",
    activationPlacementStarted: "Placement boshladi",
    activationPlacementCompleted: "Placement tugatdi",
    activationTopicOpened: "Birinchi mavzuni ochdi",
    activationSpeakingCompleted: "Birinchi speaking sessiya",
    activationFeedbackViewed: "Talaffuz tahlilini ko'rdi",
    activationDay1Returned: "Ertasiga qaytdi (D1)",
    activationDay7Active: "7 kunda 3 marta gapirdi",
    activationMonetization: "Paywall / upgrade / to'lov",

    // Retention brackets
    retentionHint: "Ro'yxatdan o'tgandan N kun keyin qaytib kelganlar ulushi.",
    d1: "1-kun",
    d7: "7-kun",
    d30: "30-kun",
    cohort: (n: number) => `Kogorta: ${n}`,
    retainedOf: (retained: number, size: number) => `${retained} / ${size}`,
    noCohort: "Kogorta hali yetarli emas",

    // Funnel steps
    funnelRegistered: "Ro'yxatdan o'tgan",
    funnelOnboarded: "Darajani aniqlagan",
    funnelActive: "Faol (7 kun)",
    funnelPremium: "Premium",

    // Plans
    planName: "Tarif",
    planUsers: "Obunachilar",
    planMrr: "MRR",
    noPlans: "Faol premium obuna yo'q.",
    // Segments by goal (goal-based onboarding)
    sectionGoals: "Maqsad bo'yicha segmentlar",
    goalsHint: "Qaysi segment ko'proq to'laydi va yaxshiroq qoladi",
    goalColGoal: "Maqsad",
    goalColUsers: "Foydalanuvchi",
    goalColPremium: "Premium",
    goalColConversion: "Konversiya",
    goalColMrr: "MRR",
    // Plan codes (server sends the English plan name)
    planMonthly: "Oylik",
    planQuarterly: "3 oylik",
    planSemiAnnual: "6 oylik",
    planYearly: "Yillik",
  },

  // Level Map (PROJECT-SPEC M.3). All Uzbek text here is a vetted template (rule 11); the
  // can-do statements are keyed by the server's statementCode (e.g. "level.b1.speaking").
  levelMap: {
    title: "O'quv yo'li",
    subtitle:
      "Mavzudan mavzuga bosqichma-bosqich yuring. Har mavzuni 6 ko'nikma bo'yicha bajarib, to'liq o'zlashtiring.",
    levelLabel: "Daraja",
    current: "Joriy darajangiz",
    levelLocked: "Bu daraja qulflangan",
    levelLockedHint: "Faqat joriy darajangiz ochiq. Keyingi daraja chiqish testidan so'ng ochiladi.",
    fullAccess: "Barcha darajalar ochiq",
    activeRoad: "Faol yo'l",
    canDoTitle: "Bu darajada nimaga erishasiz",
    canDoToggle: "Bu daraja nimani beradi?",
    skillScoresTitle: "Ko'nikma ballaringiz",
    noScores:
      "Hali ball yo'q. Mashqlarni boshlang - ballaringiz shu yerda ko'rinadi.",
    topicsTitle: "Mavzular",
    progress: (learned: number, total: number) =>
      `${total} mavzudan ${learned} tasi to'liq o'zlashtirildi`,
    topicCount: (n: number) => `${n} ta mavzu`,
    empty: "Bu daraja uchun mavzular topilmadi.",
    learned: "O'rganilgan",
    ready: "Tayyor",
    // Chapters (bo'limlar): every CEFR level ships exactly 10 categories x 5 topics, and the API
    // returns them in learning order, so each contiguous category run is one chapter of the road.
    chapter: {
      countLabel: (index: number, total: number) => `${total} bo'limdan ${index}-si`,
      progress: (done: number, total: number) => `${total} mavzudan ${done} tasi`,
      done: "Bo'lim yakunlandi",
      active: "Shu bo'limdasiz",
      locked: "Bu bo'lim hali qulflangan",
      upcoming: "Keyingi bo'lim",
      expand: "Bo'limni ochish",
      collapse: "Bo'limni yig'ish",
    },
    // Chapter names keyed by the server's Category code (rule 11: no AI-generated Uzbek).
    // Falls back to the prettified English code when a key is missing.
    chapters: {
      // A1
      family_people: "Oila va odamlar",
      home_routine: "Uy va kun tartibi",
      food_drink: "Ovqat va ichimlik",
      school: "Maktab",
      animals_pets: "Hayvonlar va uy hayvonlari",
      body_health: "Tana va salomatlik",
      clothes_weather: "Kiyim va ob-havo",
      places_town: "Shahar va joylar",
      free_time_toys: "Bo'sh vaqt va o'yinchoqlar",
      time_numbers: "Vaqt va sonlar",
      // A2
      daily_life: "Kundalik hayot",
      home_technology: "Uy va texnika",
      food_eating_out: "Ovqat va tashqarida ovqatlanish",
      shopping: "Xarid qilish",
      town_directions: "Shahar va yo'l ko'rsatish",
      travel_transport: "Sayohat va transport",
      weather_seasons: "Ob-havo va fasllar",
      health_body: "Salomatlik va tana",
      hobbies_free_time: "Sevimli mashg'ulot va bo'sh vaqt",
      work_jobs: "Ish va kasblar",
      // B1
      city_life: "Shahar hayoti",
      education_learning: "Ta'lim va o'rganish",
      environment_nature: "Atrof-muhit va tabiat",
      health_lifestyle: "Salomatlik va turmush tarzi",
      media_entertainment: "Media va ko'ngilochar",
      money_shopping: "Pul va xaridlar",
      relationships_society: "Munosabatlar va jamiyat",
      technology_internet: "Texnologiya va internet",
      travel_culture: "Sayohat va madaniyat",
      work_career: "Ish va karyera",
      // B2
      culture_arts: "Madaniyat va san'at",
      education_knowledge: "Ta'lim va bilim",
      environment_sustainability: "Atrof-muhit va barqarorlik",
      global_issues: "Global muammolar",
      health_psychology: "Salomatlik va psixologiya",
      media_communication: "Media va kommunikatsiya",
      science_innovation: "Fan va innovatsiya",
      society_issues: "Jamiyat muammolari",
      technology_future: "Texnologiya va kelajak",
      work_economy: "Ish va iqtisodiyot",
      // C1
      business_leadership: "Biznes va yetakchilik",
      culture_identity: "Madaniyat va o'ziga xoslik",
      economics_markets: "Iqtisodiyot va bozorlar",
      environment_policy: "Atrof-muhit siyosati",
      globalisation_society: "Globallashuv va jamiyat",
      law_justice: "Huquq va adolat",
      philosophy_ethics: "Falsafa va axloq",
      politics_governance: "Siyosat va boshqaruv",
      science_research: "Fan va tadqiqot",
      technology_ai: "Texnologiya va sun'iy intellekt",
      // C2
      aesthetics_theory: "Estetika va nazariya",
      bioethics_medicine: "Bioetika va tibbiyot",
      epistemology_philosophy: "Epistemologiya va falsafa",
      geopolitics_diplomacy: "Geosiyosat va diplomatiya",
      innovation_disruption: "Innovatsiya va tub o'zgarishlar",
      jurisprudence_rights: "Huquqshunoslik va inson huquqlari",
      linguistics_language: "Tilshunoslik va til",
      macroeconomics_finance: "Makroiqtisodiyot va moliya",
      neuroscience_cognition: "Neyrofan va idrok",
      sustainability_climate: "Barqarorlik va iqlim",
    } as Record<string, string>,
    energyTitle: "Energiya kerak",
    energyEmpty: (time: string) => `Chaqmoq tugadi. Keyingi 5 ta chaqmoq ${time} da to'ladi.`,
    energyError: "Chaqmoq holatini tekshirib bo'lmadi. Qayta urinib ko'ring.",
    // Roadmap (winding topic path).
    roadmap: {
      continueTitle: "Davom etamizmi?",
      startTitle: "O'qishni boshlaymizmi?",
      continueCta: "Davom etish",
      startCta: "Boshlash",
      reviewCta: "Takrorlash",
      activeHint: "Shu mavzudan davom eting",
      nextHint: "Keyingi mavzu",
      mastered: "O'zlashtirildi",
      inProgress: (passed: number, total: number) =>
        `${total} ko'nikmadan ${passed} tasi`,
      notStarted: "Boshlanmagan",
      moduleCount: (passed: number, total: number) => `${passed}/${total}`,
      // Rolling topic-window gating (K.5): three unmastered topics stay open.
      locked: "Qulflangan",
      lockedHint:
        "Ochiq 3 mavzudan birini to'liq o'zlashtiring (6/6) - keyin bu ochiladi.",
      // Per-topic lesson dropdown: all six stages' done/not-done state.
      skillsTitle: "6 ta bosqich",
      skillsHint:
        "Qaysi ko'nikma bajarildi, qaysi biri qolgan. Ustiga bosib oching.",
      openTopic: "Mavzuni to'liq ochish",
      finishTitle: "Daraja yakuni",
      allMasteredHint:
        "Barcha mavzular o'zlashtirildi - chiqish testiga tayyorsiz!",
    },
    // Level Exit Test readiness (M.5).
    exitTitle: "Daraja chiqish testi",
    // Level Exit Test intro (hero) screen - matches the Welcome / AssessmentIntro
    // 3D design language. All copy vetted here (rule 11).
    exitIntroTitle: "Bu darajani yakunlash vaqti!",
    exitIntroSubtitle:
      "So'nggi tasdiqlash testini topshiring - o'tsangiz, keyingi CEFR darajasiga o'tasiz.",
    exitStakesTitle: "Nima kutmoqda?",
    exitCurrentLabel: "Joriy darajangiz",
    exitNextLabel: "Keyingi daraja",
    exitPointTimeTitle: "Qisqa test",
    exitPointTimeText: "Taxminan 10-15 daqiqada yakunlanadi - o'zingizga qulay vaqtda.",
    exitPointSkillTitle: "6 ta ko'nikma",
    exitPointSkillText:
      "Lug'at, grammatika, tinglash, o'qish, yozish va gapirish baholanadi.",
    exitPointLevelTitle: "Yangi daraja",
    exitPointLevelText: "Muvaffaqiyatli topshirsangiz - darajangiz avtomatik oshadi.",
    exitReady:
      "Tayyorsiz! Kundalik ko'nikma talablari bajarildi. Endi 6 ko'nikmali chiqish testini topshiring.",
    exitNotReady: (mastered: number, required: number) =>
      `Daraja chiqish testi uchun kamida ${required} ko'nikma yuqori ballga yetishi kerak (hozir ${mastered} ta). Mavzularda mashq qilishda davom eting.`,
    exitAtMax:
      "Siz eng yuqori darajadasiz (C2). Bundan keyin daraja yo'q - tabriklaymiz!",
    exitMaxTitle: "Eng yuqori daraja",
    exitCompleted:
      "Bu darajaning chiqish testidan muvaffaqiyatli o'tgansiz. Keyingi darajaga o'tdingiz.",
    exitLocked:
      "Bu daraja hali qulflangan. Avval joriy darajangizni yakunlab, uning chiqish testidan o'ting.",
    exitStartCta: "Chiqish testini boshlash",
    exitTakeAnyway: "Baribir testni topshirish",
    // Level Exit Test result screen (M.5).
    exitResult: {
      passedTitle: "Test muvaffaqiyatli topshirildi!",
      failedTitle: "Hali bir oz mashq kerak",
      advancedSubtitle: (level: string) =>
        `Tabriklaymiz! Siz ${level} darajasiga o'tdingiz.`,
      passedNotAdvancedSubtitle: (mastered: number, required: number) =>
        `Testdan o'tdingiz. Darajani oshirish uchun kamida ${required} ko'nikma yuqori ballga yetishi kerak (hozir ${mastered} ta). Mashqda davom eting - natijangiz saqlandi.`,
      failedSubtitle:
        "Bu safar yetarli bo'lmadi. Mavzularda mashq qilib, keyinroq qayta urinib ko'ring.",
      breakdownTitle: "Ko'nikma bo'yicha natija",
      backToLevels: "Darajalarga qaytish",
      retry: "Qayta urinish",
      missingTitle: "Test natijasi topilmadi",
      missingText:
        "Natijani ko'rish uchun daraja chiqish testini yakunlang. Hech qanday daraja o'zgarishi saqlanmadi.",
      missingBack: "Darajalar xaritasiga qaytish",
    },
    canDo: {
      "level.a1.listening":
        "Odamlar sekin va aniq gapirsa, tanish kundalik so'zlar va juda oddiy iboralarni tushuna olaman.",
      "level.a1.reading":
        "Tanish ismlar, so'zlar va juda oddiy gaplarni - masalan, e'lon va lavhalarda - tushuna olaman.",
      "level.a1.speaking":
        "O'zimni va boshqalarni tanishtira olaman, shaxsiy ma'lumotlar haqida oddiy savol berib, javob bera olaman.",
      "level.a1.writing":
        "Qisqa, oddiy xabar yoza olaman va shaxsiy ma'lumotli shakllarni to'ldira olaman.",
      "level.a2.listening":
        "Bevosita shaxsiy ahamiyatga ega mavzularda iboralar va eng ko'p uchraydigan so'zlarni tushuna olaman.",
      "level.a2.reading":
        "Qisqa, oddiy matnlarni o'qiy olaman va menyu, jadval kabi kundalik materiallardan kerakli ma'lumotni topa olaman.",
      "level.a2.speaking":
        "Oddiy, kundalik vazifalarda muloqot qila olaman hamda o'z kelib chiqishim va atrofimni tasvirlay olaman.",
      "level.a2.writing":
        "Qisqa oddiy eslatmalar va oddiy shaxsiy xat - masalan, kimgadir minnatdorchilik - yoza olaman.",
      "level.b1.listening":
        "Ish, maktab va bo'sh vaqt kabi tanish mavzulardagi aniq, odatiy nutqning asosiy nuqtalarini tushuna olaman.",
      "level.b1.reading":
        "Asosan kundalik yoki ish bilan bog'liq tildan iborat matnlarni tushuna olaman.",
      "level.b1.speaking":
        "Sayohat paytida uchraydigan ko'p vaziyatlarni hal qila olaman va iboralarni bog'lab tajriba hamda voqealarni tasvirlay olaman.",
      "level.b1.writing":
        "Tanish mavzularda bog'langan oddiy matn va tajribani tasvirlovchi shaxsiy xatlar yoza olaman.",
      "level.b2.listening":
        "Uzoq nutqni va televideniedagi ko'pchilik yangilik hamda dolzarb mavzudagi ko'rsatuvlarni tushuna olaman.",
      "level.b2.reading":
        "Mualliflar muayyan nuqtai nazar bildirgan zamonaviy muammolar haqidagi maqola va hisobotlarni o'qiy olaman.",
      "level.b2.speaking":
        "Ravon va erkin muloqot qila olaman hamda dolzarb masala bo'yicha o'z fikrimni tushuntira olaman.",
      "level.b2.writing":
        "Keng doiradagi mavzularda aniq, batafsil matn va ma'lumot beruvchi insholar yoza olaman.",
      "level.c1.listening":
        "Nutq aniq tuzilmagan va munosabatlar yashirin ifodalangan bo'lsa ham, uzoq nutqni tushuna olaman.",
      "level.c1.reading":
        "Uzun va murakkab faktik hamda badiiy matnlarni tushuna olaman va uslubdagi farqlarni anglay olaman.",
      "level.c1.speaking":
        "Fikrlarimni ravon va erkin ifodalay olaman hamda tilni ijtimoiy va kasbiy maqsadlarda moslab ishlata olaman.",
      "level.c1.writing":
        "O'zimni aniq, yaxshi tuzilgan matnda ifodalay olaman va murakkab mavzular haqida batafsil yoza olaman.",
      "level.c2.listening":
        "Tez ona tilidagi nutq ham bo'lsa, har qanday og'zaki tilni qiyinchiliksiz tushuna olaman.",
      "level.c2.reading":
        "Mavhum va murakkab matnlar ham bo'lsa, deyarli barcha yozma til shakllarini bemalol o'qiy olaman.",
      "level.c2.speaking":
        "Har qanday suhbatda bemalol ishtirok eta olaman va murakkab vaziyatlarda o'zimni aniq ifodalay olaman.",
      "level.c2.writing":
        "Mos uslubdagi aniq, ravon matn va murakkab xat, hisobot yoki maqolalar yoza olaman.",
    },
  },

  // Yo'l xaritasi (level map) uchun to'tiqush yo'riqnomasi. Barcha matnlar
  // tekshirilgan shablonlar (rule 11) - kod orqali generatsiya qilinmaydi.
  mascot: {
    hero: "Salom! Men seni o'quv yo'lingda kuzatib boraman.",
    levelIntro: (level: string) => `${level} darajasi - shu yerda turibsiz.`,
    pickLevel: (level: string) => `${level} tanladingiz. O'sha darajadagi mavzularni ko'ramiz.`,
    progress: (pct: number) =>
      `Ajoyib! Darajangizning ${pct}% ini o'zlashtirib bo'ldingiz.`,
    active: "Bu mavzu faol - davom eting, birorta ko'nikma qolmagan bo'lsin!",
    locked: "Bu mavzu qulflangan. Ochiq 3 mavzudan birini to'liq o'zlashtiring.",
    mastered: "Bu mavzu mukammal o'zlashtirildi! Keyingisiga o'tamiz.",
    skillHints: {
      vocabulary: "Yangi so'zlarni o'rganing va esda saqlang.",
      grammar: "Grammatika qoidalarini mashq qiling.",
      reading: "Matnlarni o'qing va tushunib chiqing.",
      writing: "Yozuv mashqlari orqali o'z fikringizni ifodalang.",
      speaking: "AI tutor bilan suhbatlashib gapirishni mashq qiling.",
      listening: "Tinglash mashqlari orqali eshitish qobiliyatingizni rivojlantiring.",
    },
    finishDone: "Barcha darajalar yakunlandi - tabriklaymiz!",
    finishReady: "Chiqish testiga tayyorsiz! Sinab ko'ring.",
    finishLocked: "Chiqish testi hali qulflangan. Davom eting.",
  },

  cefrNames: {
    A1: "A1 - Boshlang'ich",
    A2: "A2 - Elementar",
    B1: "B1 - O'rta",
    B2: "B2 - O'rtadan yuqori",
    C1: "C1 - Yuqori",
    C2: "C2 - Mukammal",
  },

  skills: {
    Speaking: "Speaking",
    Listening: "Listening",
    Reading: "Reading",
    Writing: "Writing",
    Grammar: "Grammar",
    Vocabulary: "Vocabulary",
  },

  errorCategories: {
    Articles: "Artikllar",
    VerbTense: "Fe'l zamonlari",
    Prepositions: "Predloglar",
    GerundInfinitive: "Gerund / Infinitiv",
    Modals: "Modal fe'llar",
    SubjectVerbAgreement: "Ega-kesim mosligi",
    WordOrder: "So'z tartibi",
    Pronunciation: "Talaffuz",
    Vocabulary: "Lug'at",
    Spelling: "Imlo",
    Other: "Boshqa",
  },

  // Gamifikatsiya (3D) sahifasi - barcha matnlar tekshirilgan shablonlar (rule 11).
  gamification: {
    title: "Gamifikatsiya",
    subtitle: "Yutuqlaringiz, ko'nikmalar va liga - hammasi bitta 3D o'yin maydonchasida.",
    loading: "Yuklanmoqda...",
    // Aylanishda (tumbling info-cube) ko'rsatiladigan 4 ta asosiy ko'rsatkich.
    cube: {
      streak: "Kunlik seriya",
      streakHint: "Har kuni mashq qilib, rekordini uzishma!",
      xp: "Tajriba ballari",
      xpHint: "Mashqlarni bajarib XP to'plang.",
      gems: "Tamchilar",
      gemsHint: "Tomonizni oching va kodlarga almashtiring.",
      league: "Liga",
      leagueHint: "Boshqalar bilan bellashib, yuqoriga chiqing.",
    },
    // Ko'nikma aylanish halqasi (progress ring) sarlavhasi va bo'sh holat.
    skillRing: {
      title: "Ko'nikma aylanishi",
      subtitle: "Har bir ko'nikmangiz qanchalik rivojlanganini aylanishda kuzating.",
      empty: "Hali ball yo'q - mashqlarni boshlang.",
    },
    // Orqaga ag'dariladigan (flip) yutuq kartalari.
    achievements: {
      title: "Yutuqlaringiz",
      subtitle: "Orqaga ag'darib, qaysi yutuqlarga erishganingizni ko'ring.",
      streakTitle: "Bir hafta ketma-ket",
      streakText: "7 kun davomida har kuni mashq qildingiz.",
      perfectTitle: "Mukammal dars",
      perfectText: "Xatosiz yakunlangan birorta mavzu.",
      explorerTitle: "O'rganuvchi",
      explorerText: "Barcha 6 ko'nikmani sinab ko'rdmngiz.",
      gemTitle: "Tamonchi",
      gemText: "Birinchi chegirma kodingizni oldingiz.",
      locked: "Qulflangan",
    },
    // Quyi qismdagi amaliy tugmalar.
    actions: {
      leaderboard: "Reytingni ko'rish",
      levels: "Yo'l xaritasiga o'tish",
    },
  },

  // Trophy shelf (achievement badges) + progress charts + the level-up ring on the analytics screens.
  trophy: {
    achievementFirstLesson: "Birinchi dars",
    achievementWeekStreak: "7 kunlik seriya",
    achievementMonthStreak: "30 kunlik seriya",
    achievementLevel: (level: number) => `${level}-daraja`,
    achievementVocab: (n: number) => `${n} so'z`,
    achievementSkills: "Barcha ko'nikmalar",
    achievementTopics: "Mavzular",
    achievementMastered: "O'zlashtirilgan",
    capStudy7: "Oxirgi 7 kunlik mashq vaqti",
    capStudy12: "Oxirgi 12 haftalik mashq vaqti",
    capSkillTime: "Ko'nikmalar bo'yicha mashq vaqti",
    capRadar: "Ko'nikmalar profili",
    capTrend: "Umumiy o'sish dinamikasi",
    capErrors: "Ko'p uchraydigan xatolar",
    capVocab: "Lug'atdagi so'zlar",
    nextLevelTitle: "Keyingi darajaga",
    nextLevelHint: (delta: number) => `Keyingi darajagacha ${delta} XP qoldi`,
    practiceCta: "Mashq qilish",
  },

} as const;

export type UzContent = typeof uz;
