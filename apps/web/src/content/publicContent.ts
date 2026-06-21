export interface GuideSection {
  heading: string;
  paragraphs: string[];
  steps?: string[];
  example?: string;
}

export interface Faq {
  question: string;
  answer: string;
}

export interface Source {
  label: string;
  url: string;
}

export interface PublicPage {
  path: string;
  slug: string;
  kind: "hub" | "guide" | "trust";
  title: string;
  description: string;
  summary: string;
  intentCluster: string;
  aliases: string[];
  sections: GuideSection[];
  faq: Faq[];
  author: string;
  reviewer: string;
  publishedAt: string;
  reviewedAt: string;
  sources: Source[];
  relatedPaths: string[];
  cta: { label: string; href: string };
  schemaType: "Article" | "WebPage";
  published: true;
}

const editorial = {
  author: "EnglishAI tahririyati",
  reviewer: "EnglishAI metodika jamoasi",
  publishedAt: "2026-08-06",
  reviewedAt: "2026-08-06",
} as const;

const cefrSource: Source = {
  label: "Council of Europe — CEFR Companion Volume",
  url: "https://www.coe.int/en/web/common-european-framework-reference-languages/cefr-descriptors",
};

const openAiPrivacySource: Source = {
  label: "OpenAI — Enterprise privacy",
  url: "https://openai.com/enterprise-privacy/",
};

const googleAiSource: Source = {
  label: "Google — Responsible AI practices",
  url: "https://ai.google/responsibility/responsible-ai-practices/",
};

export const guidePages: PublicPage[] = [
  {
    path: "/learn/ai-bilan-ingliz-tilini-organish",
    slug: "ai-bilan-ingliz-tilini-organish",
    kind: "guide",
    title: "AI bilan ingliz tilini o‘rganish: amaliy va xavfsiz yo‘l xaritasi",
    description: "AI tutor yordamida speaking, listening, reading, vocabulary, grammar va writing ko‘nikmalarini reja asosida mashq qilish bo‘yicha qo‘llanma.",
    summary: "Sun’iy intellekt ustozni to‘liq almashtiradigan sehrli vosita emas. U to‘g‘ri topshiriq, muntazam takrorlash va natijani tekshirish bilan birga ishlatilganda shaxsiy mashq hamkori bo‘la oladi.",
    intentCluster: "ai yordamida ingliz tili o‘rganish",
    aliases: ["AI bilan ingliz tili", "sun’iy intellekt orqali ingliz tili", "AI tutor"],
    sections: [
      {
        heading: "AI tutor nimaga foydali?",
        paragraphs: [
          "AI tutor istalgan vaqtda savol-javob, vaziyatli suhbat va matn tahriri uchun mashq muhiti yaratadi. Ayniqsa gapirishdan uyaladigan boshlovchi bir xil vazifani bir necha marta, bosimsiz qaytarishi mumkin.",
          "Foyda vositaning o‘zidan emas, topshiriqning aniqligidan keladi. “Ingliz tilini o‘rgat” o‘rniga daraja, mavzu, mashq turi va javob formatini ayting. Muhim ma’lumotni lug‘at, darslik yoki ishonchli manba bilan tekshiring: AI ham xato qilishi mumkin.",
        ],
        steps: [
          "Maqsadni belgilang: masalan, A2 darajada kundalik suhbat.",
          "AI tutor rolini va mashq chegarasini yozing.",
          "Javobdan keyin bitta xato, uning sababi va tuzatilgan variantni so‘rang.",
          "Yangi iboralarni 3/7/21 kunlik takrorlash rejasiga qo‘shing.",
        ],
        example: "Prompt: “Men A2 darajadaman. Kafeda buyurtma berish bo‘yicha 8 replikali suhbat qil. Har safar faqat bitta savol ber; oxirida uchta eng muhim xatomni o‘zbekcha tushuntir.”",
      },
      {
        heading: "Olti ko‘nikmani bir haftada qanday uyg‘unlashtirish kerak?",
        paragraphs: [
          "Speaking va writing faol ishlab chiqarish ko‘nikmalari; listening va reading esa tushunish uchun kirish materialini beradi. Vocabulary va grammar ularning tayanchidir. Birini uzoq vaqt ajratib mashq qilish o‘rniga, bitta mavzuni olti ko‘nikma bo‘ylab aylantiring.",
          "Masalan, sayohat mavzusida qisqa audio tinglang, matn o‘qing, foydali iboralarni ajrating, bir grammatik qolipni kuzating, ovozli javob bering va yakunda kichik xat yozing. EnglishAI ichidagi tegishli modullar shu siklni alohida mashq qilishga yordam beradi.",
        ],
        steps: ["Dushanba: level test va maqsad", "Seshanba–payshanba: 20–30 daqiqalik aralash mashq", "Juma: AI bilan vaziyatli suhbat", "Yakshanba: xatolar va 3/7/21 takrorlash ro‘yxati"],
      },
      {
        heading: "AI javobini tekshirish odati",
        paragraphs: [
          "Talaffuz bahosi, grammatik izoh yoki tarjimani mutlaq haqiqat deb qabul qilmang. Avval AI’dan qoida va misolni ajratib berishni so‘rang, keyin shubhali joyni ishonchli lug‘at, rasmiy til korpusi yoki o‘qituvchi bilan tekshiring.",
          "Shaxsiy, maxfiy yoki ishga oid sirli matnlarni promptga kiritmang. Xizmatning maxfiylik shartlarini o‘qing va mashq uchun ismlar hamda tafsilotlarni umumiylashtiring.",
        ],
        steps: ["Da’voni ajrating", "Ikkinchi manbadan tekshiring", "Xatoni qayd eting", "To‘g‘ri variant bilan qayta mashq qiling"],
      },
      {
        heading: "30 kunlik sodda tizim",
        paragraphs: [
          "Birinchi kuni level test orqali taxminiy CEFR bosqichingizni aniqlang va bitta o‘lchanadigan vazifa tanlang: masalan, o‘zingiz haqingizda ikki daqiqa to‘xtamasdan gapirish. Har kuni qisqa, lekin yakunlanadigan mashq qiling.",
          "Har yetti kunda bir xil topshiriqni qaytaring va faqat o‘zingizning oldingi natijangiz bilan solishtiring. Platforma yoki AI bergan fikrni dalil sifatida saqlang, ammo kafolatlangan natija deb qaramang.",
        ],
        example: "Kunlik blok: 5 daqiqa takrorlash + 10 daqiqa input + 10 daqiqa speaking/writing + 5 daqiqa xatolar jurnali.",
      },
    ],
    faq: [
      { question: "AI bilan ingliz tilini noldan o‘rganish mumkinmi?", answer: "Ha, AI mashq va tushuntirishda yordam beradi, lekin boshlovchiga aniq dastur, tekshirilgan material va izchil takrorlash ham kerak. Level testdan boshlash ma’qul." },
      { question: "AI tutor o‘qituvchini almashtiradimi?", answer: "To‘liq emas. U ko‘p mashq va tezkor fikr beradi; murakkab xato, motivatsiya va individual pedagogik qarorlar uchun malakali o‘qituvchi foydali." },
      { question: "Har kuni qancha vaqt ajratish kerak?", answer: "Barqaror 20–30 daqiqalik blok ko‘pincha uzoq, ammo kamdan-kam darsdan qulayroq. Vaqtni shaxsiy jadvalingizga moslang." },
      { question: "AI xato javob bersa nima qilaman?", answer: "Shubhali da’voni alohida yozib oling, ishonchli manbadan tekshiring va tuzatilgan variant bilan yangi misol tuzing." },
      { question: "EnglishAI qaysi ko‘nikmalarni qamrab oladi?", answer: "AI tutor bilan birga speaking, listening, reading, vocabulary, grammar va writing yo‘nalishlari hamda CEFR A1–C2 level testi mavjud." },
    ],
    ...editorial,
    sources: [cefrSource, openAiPrivacySource, googleAiSource],
    relatedPaths: ["/learn", "/learn/ingliz-tilida-gapirishni-organish", "/learn/soz-yodlash", "/methodology"],
    cta: { label: "Darajani aniqlash", href: "/level-test" },
    schemaType: "Article",
    published: true,
  },
  {
    path: "/learn/ingliz-tilida-gapirishni-organish",
    slug: "ingliz-tilida-gapirishni-organish",
    kind: "guide",
    title: "Ingliz tilida gapirishni o‘rganish: sukutdan suhbatgacha",
    description: "Speaking ko‘nikmasini shadowing, vaziyatli dialog, ovozli qayd va maqsadli feedback orqali rivojlantirish uchun bosqichma-bosqich reja.",
    summary: "Ravonlik ko‘proq qoida bilish emas, bilgan til birliklarini real vaqt ichida chaqira olishdir. Buning uchun qisqa, takrorlanuvchi va mazmunli nutq mashqi kerak.",
    intentCluster: "ingliz tilida gapirishni o‘rganish",
    aliases: ["speaking o‘rganish", "inglizcha ravon gapirish", "gapirish mashqlari"],
    sections: [
      {
        heading: "Gapirishga to‘sqinlik qiladigan uch muammo",
        paragraphs: [
          "Ko‘pchilikda so‘z yetishmasligi, gapni o‘zbekchadan ichida tarjima qilish va xato qilishdan qo‘rqish birga uchraydi. Ularni alohida mashq bilan yechish kerak: tayyor iboralar, vaqt cheklangan javob va xavfsiz takrorlash.",
          "Mukammal talaffuzni kutib sukut saqlash samarasiz. Avval tushunarli va qisqa gap, keyin aniqlik, bog‘lovchilar va tabiiy tezlik ustida ishlang.",
        ],
        example: "“I think … because … For example …” qolipi bilan bitta savolga 30, keyin 60 soniya javob bering.",
      },
      {
        heading: "Kunlik speaking sikli",
        paragraphs: [
          "Quloq eshitgan qolipni og‘iz tezroq o‘zlashtiradi. Shuning uchun qisqa audio yoki namunaviy gapdan boshlang, matn bilan tekshiring, so‘ng ovozga taqlid qilib shadowing qiling.",
          "Keyingi bosqichda matnni yopib, mazmunni o‘z so‘zingiz bilan ayting. Ovozli qaydni bir marta tinglab, faqat bitta ustuvor xatoni tanlang; bir urinishda hammasini tuzatish nutqni muzlatadi.",
        ],
        steps: ["1–2 daqiqa audio tinglang", "3 marta shadowing qiling", "Mazmunni 60 soniyada qayta ayting", "Bitta xatoni tuzating", "Yana bir marta yozib oling"],
      },
      {
        heading: "AI tutor bilan vaziyatli suhbat",
        paragraphs: [
          "AI tutor suhbatdosh, mijoz, intervyuer yoki hamkasb rolini bajarishi mumkin. Daraja va vaziyatni aniq belgilang, undan bir vaqtda bitta savol berishni hamda suhbatni bo‘lmasdan oxirida feedback berishni so‘rang.",
          "Feedbackni uch ustunga ajrating: tushunarlilik, grammatik aniqlik va foydali yangi ibora. Talaffuz bo‘yicha avtomatik baho taxminiy bo‘lishi mumkin; tushunarsiz joyni inson tinglovchi bilan ham sinash foydali.",
        ],
        example: "“B1 darajadagi ish intervyusini o‘tkaz. 6 savol ber. Javoblarim tugagach, eng ko‘p takrorlangan ikki xato va tabiiyroq uchta iborani ko‘rsat.”",
      },
      {
        heading: "Haftalik o‘sishni o‘lchash",
        paragraphs: [
          "Bir xil savolga har hafta ikki daqiqalik javob yozib oling. Pauza soni, fikrni yakunlash, mavzuga mos so‘zlar va tushunarlilikni kuzating. Faqat tezlikka qarash noto‘g‘ri: sekin, ammo ravshan nutq ham yaxshi natija.",
          "Yangi iboralarni alohida so‘z emas, butun birikma sifatida 3/7/21 kunlarda qaytaring va har safar yangi vaziyatda ishlating.",
        ],
        steps: ["Haftalik bitta mavzu", "2 daqiqalik qayd", "4 mezon bo‘yicha o‘zini baholash", "Keyingi hafta uchun bitta fokus"],
      },
    ],
    faq: [
      { question: "Inglizcha gapirishni qayerdan boshlash kerak?", answer: "O‘zingiz, kundalik reja va tanish joylar haqidagi 5–10 tayyor qolipdan boshlang; keyin ularni qisqa savol-javobda ishlating." },
      { question: "Grammatikam mukammal bo‘lmasa gapirsam bo‘ladimi?", answer: "Ha. Tushunarli oddiy gaplardan boshlang, suhbatdan keyin takroriy xatolarni bittalab tuzating." },
      { question: "Shadowing nima?", answer: "Audio ortidan juda qisqa kechikish bilan ohang va talaffuzga taqlid qilib qaytarish mashqi." },
      { question: "Suhbatdosh topolmasam nima qilaman?", answer: "Ovozli kundalik, rasm tasviri, qayta hikoya va AI tutor bilan rol o‘ynash mashqlaridan foydalaning." },
      { question: "Ravonlikni qanday o‘lchayman?", answer: "Bir xil vazifadagi pauza, tugallangan fikr, tushunarlilik va mavzuga mos iboralarni vaqt bo‘yicha solishtiring." },
    ],
    ...editorial,
    sources: [cefrSource, { label: "British Council — Speaking practice", url: "https://learnenglish.britishcouncil.org/skills/speaking" }],
    relatedPaths: ["/learn", "/learn/ai-bilan-ingliz-tilini-organish", "/learn/soz-yodlash", "/learn/cefr-darajalari"],
    cta: { label: "Speaking mashqini boshlash", href: "/app/speaking" },
    schemaType: "Article",
    published: true,
  },
  {
    path: "/learn/ingliz-tilini-mustaqil-organish",
    slug: "ingliz-tilini-mustaqil-organish",
    kind: "guide",
    title: "Ingliz tilini mustaqil o‘rganish: reja, resurs va nazorat",
    description: "Mustaqil o‘rganuvchi uchun CEFRga tayangan maqsad, haftalik tizim, ko‘nikmalar balansi va o‘zini tekshirish usullari.",
    summary: "Mustaqil ta’limda eng katta xavf resurs kamligi emas, tartibsiz sakrashdir. Bitta aniq maqsad, cheklangan resurslar va muntazam qayta aloqa barqaror yo‘l yaratadi.",
    intentCluster: "ingliz tilini mustaqil o‘rganish",
    aliases: ["uyda ingliz tili o‘rganish", "self study english", "mustaqil ingliz tili rejasi"],
    sections: [
      {
        heading: "Boshlang‘ich nuqtani va maqsadni aniqlash",
        paragraphs: [
          "Avval level testdan o‘ting va natijani taxminiy yo‘nalish deb qabul qiling. CEFR darajasi faqat grammatik test emas; tinglash, o‘qish, gapirish va yozish qobiliyati bir xil bo‘lmasligi mumkin.",
          "Maqsadni vazifa bilan yozing: “B1 bo‘laman”dan ko‘ra “uch oy davomida haftasiga besh kun mashq qilib, tanish mavzuda ikki daqiqalik fikr ayta olaman” boshqariladiganroq.",
        ],
        steps: ["Level test", "Kuchli va zaif ko‘nikmalar ro‘yxati", "12 haftalik bitta asosiy maqsad", "Haftalik tekshiruv vazifasi"],
      },
      {
        heading: "Minimal resurslar to‘plami",
        paragraphs: [
          "Bir vaqtning o‘zida ko‘p ilova va kurs yig‘mang. Sizga darajaga mos asosiy dars yo‘li, tinglash/o‘qish materiali, takrorlash tizimi va speaking yoki writing uchun feedback kanali yetadi.",
          "EnglishAI’dagi speaking, listening, reading, vocabulary, grammar va writing modullarini haftalik rejaning bo‘laklari sifatida ishlatish mumkin. AI tutor savol berish va mashqni moslashtirishga xizmat qiladi, lekin manbani tekshirish mas’uliyati o‘rganuvchida qoladi.",
        ],
        example: "Resurs qoidasi: 1 asosiy kurs + 1 input manbasi + 1 takrorlash ro‘yxati + 1 feedback usuli.",
      },
      {
        heading: "Haftalik o‘quv jadvali",
        paragraphs: [
          "Har bir darsda eski materialni chaqirish, yangi input va faol qo‘llash bo‘lsin. Faqat video ko‘rish tanishlik hissini beradi, lekin mustaqil gap tuzish qobiliyatini kafolatlamaydi.",
          "Band kunlarda minimal 15 daqiqalik blokni saqlang. Bo‘sh kunda esa chuqurroq reading yoki writing qiling. Bir kunni xatolar jurnali va takrorlashga ajrating.",
        ],
        steps: ["5 daqiqa: 3/7/21 takrorlash", "10 daqiqa: listening yoki reading", "10 daqiqa: grammar/vocabulary", "10 daqiqa: speaking yoki writing", "2 daqiqa: natijani qayd etish"],
      },
      {
        heading: "O‘zini nazorat qilish va rejani tuzatish",
        paragraphs: [
          "Har hafta bajarilgan daqiqadan tashqari real mahsulotni ham ko‘ring: yozilgan matn, ovozli qayd, tushunilgan audio yoki qayta ishlatilgan ibora. Bu faoliyat bilan natijani ajratadi.",
          "Ikki hafta ketma-ket reja bajarilmasa, irodani ayblashdan oldin hajmni kamaytiring yoki vaqtni almashtiring. Oson davom etadigan reja mukammal, ammo bajarilmaydigan jadvaldan foydaliroq.",
        ],
        example: "Yakshanba savoli: “Bu hafta qaysi vazifani kechagidan mustaqilroq bajardim va kelasi haftada nimani bittaga soddalashtiraman?”",
      },
    ],
    faq: [
      { question: "Mustaqil o‘rganishda kurs shartmi?", answer: "Shart emas, ammo darajaga mos ketma-ket yo‘l va feedback zarur. Kurs shu tuzilmani berishi mumkin." },
      { question: "Qaysi ko‘nikmadan boshlayman?", answer: "Asosiy maqsadingizga yaqin ko‘nikmadan boshlang, biroq haftada barcha asosiy ko‘nikmalarga vaqt ajrating." },
      { question: "Rejani bajara olmasam nima qilaman?", answer: "Kunlik minimumni kamaytiring, aniq vaqt va joy belgilang, resurslar sonini qisqartiring." },
      { question: "Natijani qachon tekshiraman?", answer: "Haftalik kichik vazifa va davriy level test bilan. Testni juda tez-tez takrorlash o‘rniga amaliy namunalaringizni saqlang." },
      { question: "Xatolar jurnaliga nima yoziladi?", answer: "Xato gap, to‘g‘ri variant, qisqa sabab va o‘zingiz tuzgan yangi misol." },
    ],
    ...editorial,
    sources: [cefrSource, { label: "Cambridge English — Learning English", url: "https://www.cambridgeenglish.org/learning-english/" }],
    relatedPaths: ["/learn", "/learn/boshlovchilar-uchun-ingliz-tili", "/learn/cefr-darajalari", "/methodology"],
    cta: { label: "Shaxsiy yo‘lni boshlash", href: "/level-test" },
    schemaType: "Article",
    published: true,
  },
  {
    path: "/learn/boshlovchilar-uchun-ingliz-tili",
    slug: "boshlovchilar-uchun-ingliz-tili",
    kind: "guide",
    title: "Boshlovchilar uchun ingliz tili: A1 poydevorini qurish",
    description: "Ingliz tilini noldan boshlayotganlar uchun talaffuz, asosiy iboralar, sodda grammatika va kundalik mashq bo‘yicha tushunarli yo‘l.",
    summary: "Boshlovchiga minglab so‘z emas, tez-tez uchraydigan iboralar va ularni tanish vaziyatda ishlatish kerak. Kichik qadamlar talaffuz, tushunish va gap tuzishni birga rivojlantiradi.",
    intentCluster: "boshlovchilar uchun ingliz tili",
    aliases: ["ingliz tili 0 dan", "A1 ingliz tili", "beginner english uzbek"],
    sections: [
      {
        heading: "Birinchi maqsad: tanish vaziyatda muloqot",
        paragraphs: [
          "A1 bosqichida salomlashish, o‘zini tanishtirish, raqam-vaqt, oila, kundalik ishlar, oziq-ovqat va yo‘l so‘rash kabi mavzular ustuvor. Har mavzu uchun oz sonli so‘z va tayyor gap qolipini tanlang.",
          "Alifboni yodlashning o‘zi talaffuz emas. So‘zni audio bilan eshiting, urg‘usini kuzating va butun ibora sifatida qaytaring.",
        ],
        example: "Qoliplar: “My name is …”, “I live in …”, “I usually …”, “Could I have …, please?”",
      },
      {
        heading: "So‘z, ibora va sodda grammatika",
        paragraphs: [
          "So‘zni tarjimasi bilan yolg‘iz yodlamang. Uning talaffuzi, bitta oddiy birikmasi va o‘zingizga tegishli gapini yozing. Shu usul vocabulary’ni speaking bilan bog‘laydi.",
          "Dastlab be fe’li, hozirgi oddiy zamon, olmoshlar, birlik-ko‘plik va sodda savollarga e’tibor bering. Qoidani uzoq ta’rifdan ko‘ra uchta kontrast misol bilan ko‘rish osonroq.",
        ],
        steps: ["Kuniga 5–8 yangi birlik", "Har biri uchun audio", "Bitta shaxsiy gap", "3/7/21 kunlarda qaytarish"],
      },
      {
        heading: "Boshlovchining 25 daqiqalik darsi",
        paragraphs: [
          "Qisqa darsning har qismi faol bo‘lsin: eshitish, qaytarish, tanlash va gap tuzish. Tushunmagan materialni cheksiz ko‘rish o‘rniga, darajaga mos qisqa parchani takrorlang.",
          "AI tutor’dan sodda til, qisqa savollar va o‘zbekcha izoh so‘rashingiz mumkin. Bir suhbatda yangi birliklar sonini cheklash kognitiv yukni kamaytiradi.",
        ],
        steps: ["5 daqiqa eski iboralar", "7 daqiqa audio va shadowing", "5 daqiqa grammatika misollari", "5 daqiqa savol-javob", "3 daqiqa qayd"],
      },
      {
        heading: "A1 dan keyingi qadam",
        paragraphs: [
          "Bir mavzudagi yodlangan dialogni emas, o‘zgargan savolga mos javob bera olishni tekshiring. Masalan, o‘zingizni tanishtirgach, boshqa odam yoki oila a’zosini ham tanishtiring.",
          "CEFR bosqichlari qat’iy dars ro‘yxati emas, kommunikativ qobiliyat tavsifidir. Keyingi bosqichga o‘tishda listening, reading, speaking va writing namunalariga birga qarang.",
        ],
        example: "Mini-tekshiruv: 60 soniya o‘zingiz haqingizda gapiring, 80 so‘zli profil yozing va sekin kundalik dialogning asosiy mazmunini ayting.",
      },
    ],
    faq: [
      { question: "Ingliz tilini noldan qaysi mavzudan boshlayman?", answer: "Salomlashish va o‘zingizni tanishtirishdan; keyin raqam, vaqt, oila va kundalik faoliyatga o‘ting." },
      { question: "Kuniga nechta so‘z yodlash kerak?", answer: "Qat’iy son yo‘q. Boshlovchi uchun 5–8 foydali birlikni gapda ishlatish, ko‘p so‘zni yuzaki ko‘rishdan ma’qul." },
      { question: "Avval grammatika yoki speakingmi?", answer: "Ikkalasini bog‘lang: kichik qoidani o‘rganib, darhol savol-javobda qo‘llang." },
      { question: "Talaffuzni qanday boshlayman?", answer: "Qisqa audio, matn va shadowing bilan; o‘zingizni yozib, tushunarlilikni tekshiring." },
      { question: "A1 qancha vaqtda tugaydi?", answer: "Bu boshlang‘ich daraja, avvalgi bilim, vaqt va mashq sifatiga bog‘liq. EnglishAI aniq muddat yoki natijani kafolatlamaydi." },
    ],
    ...editorial,
    sources: [cefrSource, { label: "British Council — A1 English level", url: "https://learnenglish.britishcouncil.org/english-levels/understand-your-english-level/a1-elementary" }],
    relatedPaths: ["/learn", "/learn/ingliz-tilini-mustaqil-organish", "/learn/soz-yodlash", "/learn/cefr-darajalari"],
    cta: { label: "Boshlang‘ich darajani tekshirish", href: "/level-test" },
    schemaType: "Article",
    published: true,
  },
  {
    path: "/learn/soz-yodlash",
    slug: "soz-yodlash",
    kind: "guide",
    title: "Inglizcha so‘z yodlash: 3/7/21 takrorlash va faol qo‘llash",
    description: "Yangi inglizcha so‘zlarni kontekst, faol eslash va 3/7/21 kunlik takrorlash orqali uzoqroq saqlash bo‘yicha amaliy qo‘llanma.",
    summary: "So‘zni qayta-qayta o‘qish tanishlik yaratadi, ammo kerak paytda eslashni kafolatlamaydi. Kuchli lug‘at ma’no, tovush, birikma va shaxsiy misolni faol chaqirish orqali quriladi.",
    intentCluster: "inglizcha so‘z yodlash",
    aliases: ["vocabulary yodlash", "so‘zlarni tez yodlash", "3 7 30 takrorlash"],
    sections: [
      {
        heading: "Nimani yodlash kerak?",
        paragraphs: [
          "Maqsadingizda tez-tez uchraydigan so‘z va iboralarni tanlang. Har bir yangi birlik uchun qisqa ta’rif yoki tarjima, talaffuz, odatiy birikma va haqiqiy misol saqlang.",
          "Alohida so‘zdan ko‘ra “make a decision”, “interested in” kabi birikma gap tuzishni tezlashtiradi. Bir so‘zning barcha ma’nosini birdan olish shart emas; hozirgi matndagi ma’nodan boshlang.",
        ],
        example: "decision — qaror; make a decision; “I need to make a decision today.”",
      },
      {
        heading: "3/7/21 takrorlash tizimi",
        paragraphs: [
          "Yangi birlikni o‘rganganingizdan keyin 3-, 7- va 30-kunlarda qayta chaqirish oddiy jadval beradi. Bu sanalar mutlaq ilmiy formula emas; qiyin so‘zga ko‘proq, osoniga kamroq interval kerak bo‘lishi mumkin.",
          "Takrorlashda kartani faqat o‘qimang. Tarjimani yopib eslang, audioni eshitib yozing, bo‘sh joyni to‘ldiring yoki yangi gap tuzing. Xato qilish eslash chegarasini ko‘rsatadi.",
        ],
        steps: ["0-kun: kontekst bilan o‘rganish", "3-kun: ma’noni faol eslash", "7-kun: yangi gapda ishlatish", "30-kun: aralash test va speaking"],
      },
      {
        heading: "Faol va passiv lug‘at",
        paragraphs: [
          "O‘qiganda tushunadigan so‘z passiv lug‘atda, gapirganda mustaqil ishlata oladigani faol lug‘atda turadi. Har bir so‘zni faol qilish shart emas; maqsadingiz uchun muhimlarini tanlang.",
          "Faollashtirish uchun savolga javob, mini-hikoya yoki writing topshirig‘ida 3–5 nishon iborani ishlating. AI tutor’dan iboralarni majburan emas, tabiiy qo‘llash bo‘yicha feedback so‘rang.",
        ],
        example: "Nishon so‘zlar: reliable, schedule, improve. Vazifa: shu uchalasini ishlatib haftalik o‘qish rejangiz haqida 5 gap yozing.",
      },
      {
        heading: "Ro‘yxat nega ishlamay qoladi?",
        paragraphs: [
          "Juda katta kunlik norma, kontekstsiz tarjima va eski kartalarni tashlab ketish tizimni buzadi. Eslay olmagan birliklarni “qobiliyatsizlik” emas, jadvalni moslashtirish signali deb ko‘ring.",
          "Haftada bir marta keraksiz yoki haddan tashqari maxsus so‘zlarni arxivlang. Kichik, dolzarb va qayta ishlatiladigan to‘plam o‘qishni davom ettirishni osonlashtiradi.",
        ],
        steps: ["Yangi so‘z sonini kamaytiring", "Birikma qo‘shing", "Ovoz va gap bilan tekshiring", "Takroriy xatoni yaqinroq intervalga qaytaring"],
      },
    ],
    faq: [
      { question: "3/7/21 usuli nima?", answer: "Yangi birlikni taxminan 3-, 7- va 30-kunlarda faol eslash orqali takrorlash uchun sodda jadval." },
      { question: "Kuniga nechta so‘z yaxshi?", answer: "Universal son yo‘q. Takrorlash qarzini oshirmaydigan va gapda ishlata oladigan miqdorni tanlang." },
      { question: "So‘zni tarjima bilan yodlash noto‘g‘rimi?", answer: "Tarjima boshlang‘ich tayanch bo‘lishi mumkin, ammo talaffuz, birikma va kontekst bilan to‘ldirilsa foydaliroq." },
      { question: "Yodlagan so‘zimni nega gapda ishlata olmayman?", answer: "U passiv tanish bo‘lishi mumkin. Savol-javob, writing va mini-hikoyada bir necha bor faol chaqiring." },
      { question: "Unutilgan kartani o‘chiramanmi?", answer: "Avval misolini soddalashtiring va intervalni qisqartiring; maqsadingizga aloqasiz bo‘lsa arxivlang." },
    ],
    ...editorial,
    sources: [
      { label: "Dunlosky et al. — Effective learning techniques", url: "https://journals.sagepub.com/doi/10.1177/1529100612453266" },
      { label: "The Learning Scientists — Retrieval practice", url: "https://www.learningscientists.org/retrieval-practice" },
    ],
    relatedPaths: ["/learn", "/learn/boshlovchilar-uchun-ingliz-tili", "/learn/ingliz-tilida-gapirishni-organish", "/methodology"],
    cta: { label: "Vocabulary mashqiga o‘tish", href: "/app/vocabulary" },
    schemaType: "Article",
    published: true,
  },
  {
    path: "/learn/cefr-darajalari",
    slug: "cefr-darajalari",
    kind: "guide",
    title: "CEFR darajalari: A1 dan C2 gacha nimani anglatadi?",
    description: "CEFR A1, A2, B1, B2, C1 va C2 darajalarining amaliy ma’nosi, ko‘nikmalar tafovuti va level test natijasini talqin qilish qo‘llanmasi.",
    summary: "CEFR til qobiliyatini A1–C2 oralig‘ida kommunikativ tavsiflar bilan ifodalaydi. Daraja sertifikat va kurs nomidan ko‘ra, muayyan vazifani qay darajada bajara olishingizni tushuntirish uchun foydali.",
    intentCluster: "CEFR darajalari",
    aliases: ["A1 A2 B1 B2 C1 C2", "ingliz tili darajalari", "CEFR level"],
    sections: [
      {
        heading: "A1 va A2: asosiy foydalanuvchi",
        paragraphs: [
          "A1 darajadagi o‘rganuvchi juda tanish mavzuda sodda iboralarni tushunishi va ishlatishi, o‘zini tanishtirishi hamda suhbatdosh yordam bersa oddiy muloqot qilishi mumkin.",
          "A2 bosqichida kundalik vazifalar, yaqin atrof, oila va ishga oid tez-tez ishlatiladigan ifodalar kengayadi. Bu “hamma grammatikani bilish” emas, cheklangan vaziyatlarda oddiy almashinuv qila olishdir.",
        ],
        example: "A1 vazifa: o‘zingizni tanishtirish. A2 vazifa: kechagi kun yoki xarid haqida qisqa, bog‘langan gaplar aytish.",
      },
      {
        heading: "B1 va B2: mustaqil foydalanuvchi",
        paragraphs: [
          "B1 darajada tanish mavzudagi aniq nutqning asosiy mazmunini tushunish, sayohatda ko‘p vaziyatni hal qilish va tajriba yoki rejani sodda bog‘langan tilda bayon qilish kutiladi.",
          "B2 darajada murakkabroq matnlarning asosiy g‘oyasini tushunish, suhbatni nisbatan erkin olib borish va turli mavzularda batafsil fikr bildirish imkoniyati ortadi. Barcha vaziyatda xatosiz gapirish talab qilinmaydi.",
        ],
        example: "B1 vazifa: sayohat muammosini tushuntirish. B2 vazifa: masofaviy ishning afzallik va kamchiliklarini dalillar bilan muhokama qilish.",
      },
      {
        heading: "C1 va C2: malakali foydalanuvchi",
        paragraphs: [
          "C1 keng hajmdagi murakkab matnni anglash, yashirin ma’noni ilg‘ash va tilni ijtimoiy, akademik yoki kasbiy maqsadda moslashuvchan ishlatishni bildiradi.",
          "C2 turli og‘zaki va yozma manbalarni oson anglash, ma’lumotni uyg‘unlashtirish va nozik ma’no farqlarini juda ravon ifodalash bilan tavsiflanadi. C2 ham “har bir so‘zni bilish” degani emas.",
        ],
      },
      {
        heading: "Level test natijasini to‘g‘ri o‘qish",
        paragraphs: [
          "Qisqa onlayn test odatda tilning ayrim qismlarini o‘lchaydi va taxminiy daraja beradi. Speaking, listening, reading, writing, vocabulary va grammar natijalari o‘zaro farq qilishi tabiiy.",
          "EnglishAI level testidan o‘quv yo‘lini boshlash uchun foydalaning, keyin real topshiriqlar orqali profilni aniqlashtiring. Darajani shaxsiy qiymat yoki kafolatlangan imtihon natijasi sifatida qabul qilmang.",
        ],
        steps: ["Testni mustaqil bajaring", "Ko‘nikmalar kesimidagi zaif joyni toping", "Darajaga mos material tanlang", "Davriy amaliy namuna bilan tekshiring"],
      },
    ],
    faq: [
      { question: "CEFR nimaning qisqartmasi?", answer: "Common European Framework of Reference for Languages — tillar bo‘yicha umumiy Yevropa tavsiflar tizimi." },
      { question: "B1 va B2 orasidagi asosiy farq nima?", answer: "B2’da murakkabroq matn va muhokama, batafsil dalillash hamda nisbatan erkin muloqot ko‘lami B1’dan kengroq." },
      { question: "Level test aniq darajani kafolatlaydimi?", answer: "Yo‘q. Qisqa test taxminiy yo‘nalish beradi; to‘liq baho bir nechta ko‘nikma va real vazifani qamrashi kerak." },
      { question: "Barcha ko‘nikmam bir darajada bo‘lishi shartmi?", answer: "Yo‘q. Masalan, reading B2, speaking B1 bo‘lishi mumkin; reja shu tafovutni hisobga oladi." },
      { question: "C2 ona tili darajasi deganimi?", answer: "CEFR C2 juda yuqori kommunikativ mahoratni tavsiflaydi, ammo u insonning ona tilida so‘zlashuvchiga aylanganini yoki barcha so‘zni bilishini anglatmaydi." },
    ],
    ...editorial,
    sources: [cefrSource, { label: "Council of Europe — Global scale table", url: "https://www.coe.int/en/web/common-european-framework-reference-languages/table-1-cefr-3.3-common-reference-levels-global-scale" }],
    relatedPaths: ["/learn", "/learn/boshlovchilar-uchun-ingliz-tili", "/learn/ingliz-tilini-mustaqil-organish", "/methodology"],
    cta: { label: "Level testdan o‘tish", href: "/level-test" },
    schemaType: "Article",
    published: true,
  },
];

const learnHub: PublicPage = {
  path: "/learn",
  slug: "learn",
  kind: "hub",
  title: "Ingliz tilini o‘rganish markazi",
  description: "Ingliz tilini AI bilan, mustaqil va CEFR asosida o‘rganish uchun professional o‘zbekcha qo‘llanmalar markazi.",
  summary: "Maqsadingizga mos yo‘lni tanlang: boshlang‘ich poydevor, speaking, mustaqil reja, so‘z yodlash, CEFR yoki AI tutor bilan xavfsiz mashq.",
  intentCluster: "ingliz tilini o‘rganish",
  aliases: ["EnglishAI learn", "ingliz tili qo‘llanmalari", "ingliz tili o‘rganish markazi"],
  sections: [
    {
      heading: "Qaysi qo‘llanmadan boshlash kerak?",
      paragraphs: [
        "Noldan boshlayotgan bo‘lsangiz, boshlovchilar qo‘llanmasi va level testdan boshlang. Ma’lum bazangiz bo‘lsa, hozir eng ko‘p to‘sqinlik qilayotgan ko‘nikmani tanlang: speaking, vocabulary yoki umumiy mustaqil reja.",
        "Har bir qo‘llanma mustaqil amaliy qiymatga ega. Ularni tartib bilan o‘qish shart emas; maslahatni o‘zingizning daraja, vaqt va maqsadingizga moslashtiring.",
      ],
      steps: ["Taxminiy CEFR darajani aniqlang", "Bitta ustuvor ko‘nikmani tanlang", "7 kunlik kichik tajriba qiling", "Natijaga qarab rejani yangilang"],
    },
    {
      heading: "EnglishAI o‘quv vositalari",
      paragraphs: [
        "Platforma AI tutor, level test hamda speaking, listening, reading, vocabulary, grammar va writing yo‘nalishlarini bir joyga jamlaydi. Qo‘llanmalar bu vositalardan maqsadli foydalanish usulini tushuntiradi.",
        "EnglishAI muayyan muddatda daraja, sertifikat yoki ish natijasini kafolatlamaydi. O‘sish muntazam mashq, avvalgi bilim va individual sharoitga bog‘liq.",
      ],
    },
    {
      heading: "Tahririy yondashuv",
      paragraphs: [
        "Kontent kommunikativ vazifa, CEFR tavsiflari va tekshiriladigan manbalarga tayangan holda yoziladi. Mahsulot haqidagi da’volar mavjud funksiyalar bilan cheklanadi.",
        "Sana va manbalarni ko‘rsatamiz, materiallarni davriy ko‘rib chiqamiz. Xato yoki eskirgan ma’lumot topsangiz, aloqa sahifasi orqali yuborishingiz mumkin.",
      ],
    },
  ],
  faq: [
    { question: "Qo‘llanmalar bepul o‘qiladimi?", answer: "Public Learn markazidagi ushbu tahririy qo‘llanmalar ochiq o‘qish uchun mo‘ljallangan; mahsulot funksiyalarining amaldagi rejalarini pricing sahifasidan tekshiring." },
    { question: "Qaysi sahifadan boshlayman?", answer: "Noldan boshlasangiz boshlovchilar qo‘llanmasidan, maqsad noaniq bo‘lsa CEFR va level testdan boshlang." },
    { question: "Materiallar kim uchun?", answer: "O‘zbek tilida yo‘l-yo‘riq izlayotgan A1–C2 oralig‘idagi mustaqil o‘rganuvchilar uchun." },
    { question: "AI maslahatlari xatosizmi?", answer: "Yo‘q. Muhim til yoki fakt da’volarini ishonchli manba bilan tekshirish kerak." },
  ],
  ...editorial,
  sources: [cefrSource, googleAiSource],
  relatedPaths: ["/learn/ai-bilan-ingliz-tilini-organish", "/learn/boshlovchilar-uchun-ingliz-tili", "/learn/cefr-darajalari", "/methodology"],
  cta: { label: "Darajani aniqlash", href: "/level-test" },
  schemaType: "WebPage",
  published: true,
};

export const trustPages: PublicPage[] = [
  {
    path: "/methodology",
    slug: "methodology",
    kind: "trust",
    title: "EnglishAI metodologiyasi",
    description: "EnglishAI o‘quv tajribasi CEFR, ko‘nikmalar integratsiyasi, faol eslash va 3/7/21 takrorlashdan qanday foydalanishini tushuntiradi.",
    summary: "Metodologiyamiz testdan boshlab real til vazifasiga, qisqa feedback va rejalashtirilgan takrorlashga o‘tadigan siklga asoslanadi.",
    intentCluster: "EnglishAI metodologiyasi",
    aliases: ["o‘qitish usuli", "EnglishAI qanday ishlaydi", "ta’lim metodikasi"],
    sections: [
      { heading: "Darajadan vazifaga", paragraphs: ["CEFR A1–C2 darajalaridan material murakkabligini yo‘naltirish uchun foydalanamiz. Level test boshlang‘ich taxmin beradi; keyingi mashqlar ko‘nikmalar profilini aniqlashtiradi.", "Maqsad faqat test savoliga javob berish emas, tinglash, o‘qish, gapirish va yozish orqali ma’noli vazifani bajarishdir."], steps: ["Darajani taxmin qilish", "Ko‘nikma bo‘shlig‘ini tanlash", "Qisqa mashq", "Feedback", "3/7/21 takrorlash"] },
      { heading: "Olti ko‘nikma va AI tutor", paragraphs: ["Speaking, listening, reading, vocabulary, grammar va writing bir-birini qo‘llab-quvvatlaydi. Mavzuni modullar orasida qayta ishlatish bilimni yangi vaziyatga ko‘chirishga yordam beradi.", "AI tutor mashq, savol va izoh beradi. Uning chiqishi xato bo‘lishi mumkinligi sababli muhim da’volar tekshiriladi; avtomatik feedback rasmiy baholash yoki inson ekspertizasining o‘rnini bosmaydi."] },
      { heading: "Takrorlash va o‘sishni ko‘rish", paragraphs: ["3/7/21 jadvali yangi birlikni uch nuqtada qayta chaqirish uchun sodda asosdir; individual qiyinchilikka qarab interval moslashtiriladi.", "O‘sishni faqat sarflangan vaqt bilan emas, oldingi namunaga nisbatan mustaqillik, tushunarlilik va aniqlik bilan ko‘rish tavsiya etiladi. Hech bir metod individual natijani kafolatlamaydi."] },
    ],
    faq: [
      { question: "EnglishAI qaysi standartga tayanadi?", answer: "Daraja tilida Council of Europe CEFR A1–C2 tavsiflaridan foydalaniladi." },
      { question: "3/7/21 qat’iy jadvalmi?", answer: "Yo‘q. Bu amaliy boshlang‘ich ritm; oson yoki qiyin materialga qarab o‘zgarishi mumkin." },
      { question: "AI feedback rasmiy bahomi?", answer: "Yo‘q. U mashq uchun tezkor ko‘rsatma, rasmiy imtihon yoki malakali ekspert bahosi emas." },
      { question: "Barcha ko‘nikmalar qamrab olinadimi?", answer: "Platformada speaking, listening, reading, vocabulary, grammar va writing yo‘nalishlari mavjud." },
    ],
    ...editorial,
    sources: [cefrSource, { label: "The Learning Scientists — Spaced practice", url: "https://www.learningscientists.org/spaced-practice" }, googleAiSource],
    relatedPaths: ["/learn", "/learn/cefr-darajalari", "/editorial-policy", "/product"],
    cta: { label: "O‘quv yo‘lini boshlash", href: "/level-test" },
    schemaType: "WebPage",
    published: true,
  },
  {
    path: "/about",
    slug: "about",
    kind: "trust",
    title: "EnglishAI haqida",
    description: "EnglishAI’ning vazifasi, mahsulot chegaralari va o‘zbek tilida ingliz tili o‘rganishga yondashuvi.",
    summary: "EnglishAI — o‘zbek tilida yo‘l-yo‘riq beradigan, AI tutor va asosiy til ko‘nikmalarini bitta o‘quv tajribasida birlashtiruvchi ingliz tili platformasi.",
    intentCluster: "EnglishAI haqida",
    aliases: ["biz haqimizda", "EnglishAI nima", "EnglishAI Uzbekistan"],
    sections: [
      { heading: "Vazifamiz", paragraphs: ["Maqsadimiz ingliz tilini o‘rganish yo‘lini o‘zbek foydalanuvchisi uchun tushunarli va amaliy qilishdir. Daraja, mashq va feedback orasidagi uzilishni kamaytirishga intilamiz.", "Platforma AI tutor, level test hamda speaking, listening, reading, vocabulary, grammar va writing mashqlarini taklif etadi."] },
      { heading: "Nimani va’da qilmaymiz", paragraphs: ["Til o‘rganish shaxsiy vaqt, avvalgi bilim va muntazam amaliyotga bog‘liq. Biz kafolatlangan CEFR o‘sishi, imtihon bali, ish yoki migratsiya natijasini va’da qilmaymiz.", "AI javobi mutlaq to‘g‘ri deb qaralmasligi kerak. Muhim qarorlar uchun rasmiy manba yoki malakali mutaxassisga murojaat qiling."] },
      { heading: "Ochiqlik va aloqa", paragraphs: ["Public qo‘llanmalarda mualliflik, ko‘rib chiqish sanasi va manbalarni ko‘rsatamiz. Mahsulot imkoniyatlarini mavjud funksiyalar bilan cheklab tasvirlaymiz.", "Savol, xato yoki hamkorlik taklifini contact sahifasi orqali yuborishingiz mumkin."] },
    ],
    faq: [
      { question: "EnglishAI kimlar uchun?", answer: "O‘zbek tilidagi yo‘l-yo‘riq bilan ingliz tilini A1–C2 oralig‘ida rivojlantirmoqchi bo‘lgan o‘rganuvchilar uchun." },
      { question: "EnglishAI maktabmi?", answer: "Bu raqamli til o‘rganish platformasi; mahsulot rasmiy ta’lim muassasasi yoki imtihon organi sifatida taqdim etilmaydi." },
      { question: "Platformada nimalar bor?", answer: "AI tutor, level test va oltita yo‘nalish: speaking, listening, reading, vocabulary, grammar, writing." },
      { question: "Xatoni qanday bildirsam bo‘ladi?", answer: "Contact sahifasidagi kanal orqali sahifa manzili va xato tafsilotini yuboring." },
    ],
    ...editorial,
    sources: [cefrSource, googleAiSource],
    relatedPaths: ["/product", "/methodology", "/editorial-policy", "/contact"],
    cta: { label: "Mahsulotni ko‘rish", href: "/product" },
    schemaType: "WebPage",
    published: true,
  },
  {
    path: "/pricing",
    slug: "pricing",
    kind: "trust",
    title: "EnglishAI narxlari va rejalar haqida",
    description: "EnglishAI rejalarini tanlashda funksiyalar, cheklovlar va amaldagi checkout ma’lumotlarini qanday tekshirish haqida ochiq izoh.",
    summary: "Narx va reja tarkibi o‘zgarishi mumkin; yakuniy qiymat, davr va mavjud funksiyalar checkout yoki ilova ichidagi amaldagi taklifda ko‘rsatiladi.",
    intentCluster: "EnglishAI narxlari",
    aliases: ["EnglishAI pricing", "obuna narxi", "EnglishAI tariflari"],
    sections: [
      { heading: "Rejani qanday tanlash kerak?", paragraphs: ["Avval o‘quv maqsadingiz va haftalik foydalanish ritmini aniqlang. So‘ng AI tutor, level test yoki muayyan ko‘nikma modullaridan qaysilari rejangizga kerakligini amaldagi taklif bilan solishtiring.", "Faqat ko‘p funksiya emas, amalda ishlatadigan funksiya qiymat yaratadi. Xariddan oldin davr, avtomatik yangilanish, cheklov va bekor qilish shartlarini o‘qing."] },
      { heading: "Narx bo‘yicha aniqlik", paragraphs: ["Ushbu tahririy sahifa o‘zgaruvchan summalarni qotirib qo‘ymaydi. Sizga tatbiq etiladigan yakuniy narx va valyuta checkout oynasida ko‘rsatilgan ma’lumotdir.", "Chegirma bo‘lsa, uning muddati va qaysi reja uchun ekanini tekshiring. EnglishAI o‘quv natijasini xarid bilan kafolatlamaydi."] },
      { heading: "Savol tug‘ilsa", paragraphs: ["To‘lovdan oldin noaniq band bo‘lsa contact sahifasi orqali savol yuboring. Hisobga bog‘liq murojaatda maxfiy karta ma’lumotini jo‘natmang.", "Platforma ichidagi amaldagi reja tavsifi ushbu umumiy ma’lumotdan ustun turadi."] },
    ],
    faq: [
      { question: "EnglishAI narxi qancha?", answer: "Amaldagi summa va valyutani ilova ichidagi pricing yoki checkout oynasidan tekshiring; bu sahifa eskirib qolishi mumkin bo‘lgan narxni keltirmaydi." },
      { question: "Qaysi funksiya qaysi rejada?", answer: "Eng so‘nggi reja taqqoslashini xarid oynasida ko‘ring, chunki tarkib vaqt o‘tishi bilan yangilanishi mumkin." },
      { question: "Obuna natijani kafolatlaydimi?", answer: "Yo‘q. Obuna vositalarga kirishni beradi; o‘quv natijasi individual mashq va sharoitga bog‘liq." },
      { question: "To‘lov bo‘yicha yordamni qayerdan olaman?", answer: "Contact sahifasi orqali murojaat qiling va maxfiy to‘lov rekvizitlarini yubormang." },
    ],
    ...editorial,
    sources: [{ label: "EnglishAI — mahsulot haqida", url: "/product" }, { label: "EnglishAI — aloqa", url: "/contact" }],
    relatedPaths: ["/product", "/about", "/contact", "/methodology"],
    cta: { label: "Bepul boshlash", href: "/login" },
    schemaType: "WebPage",
    published: true,
  },
  {
    path: "/product",
    slug: "product",
    kind: "trust",
    title: "EnglishAI mahsuloti",
    description: "EnglishAI’dagi AI tutor, level test va speaking, listening, reading, vocabulary, grammar, writing yo‘nalishlari haqida aniq tavsif.",
    summary: "EnglishAI darajani aniqlash, turli til ko‘nikmalarini mashq qilish va AI tutor bilan qayta aloqa olishni yagona oqimga birlashtiradi.",
    intentCluster: "EnglishAI mahsuloti",
    aliases: ["EnglishAI funksiyalari", "EnglishAI ilovasi", "ingliz tili AI platforma"],
    sections: [
      { heading: "Boshlanish: level test va yo‘nalish", paragraphs: ["Level test CEFR A1–C2 oralig‘ida boshlang‘ich yo‘nalish beradi. Natija o‘quv kontentini tanlashga yordam beradi, ammo rasmiy sertifikat yoki to‘liq diagnostika emas.", "O‘rganuvchi speaking, listening, reading, vocabulary, grammar va writing yo‘nalishlarida maqsadli mashq qilishi mumkin."] },
      { heading: "AI tutor bilan mashq", paragraphs: ["AI tutor savol-javob, vaziyatli suhbat, tushuntirish va yozuv bo‘yicha mashq hamkori bo‘la oladi. Foydalanuvchi daraja va vazifani qanchalik aniq bersa, mashq shunchalik boshqariladigan bo‘ladi.", "AI xato qilishi mumkin. Muhim fakt, tarjima yoki til izohini ishonchli manbadan tekshirish va maxfiy ma’lumotni kiritmaslik kerak."] },
      { heading: "Takrorlash va ko‘nikmalar bog‘lanishi", paragraphs: ["Vocabulary uchun 3/7/21 takrorlash ritmi yangi birlikni qayta chaqirishga yordam beradi. Bu qat’iy formula emas va o‘rganuvchiga moslashtirilishi mumkin.", "Bir mavzuni tinglash, o‘qish, gapirish va yozishda qayta ishlatish alohida mashqlarni amaliy kommunikatsiyaga bog‘laydi."] },
    ],
    faq: [
      { question: "EnglishAI’da qaysi modullar bor?", answer: "Speaking, listening, reading, vocabulary, grammar va writing, shuningdek AI tutor va level test." },
      { question: "Qaysi CEFR darajalari qo‘llanadi?", answer: "A1, A2, B1, B2, C1 va C2." },
      { question: "Level test sertifikat beradimi?", answer: "U boshlang‘ich yo‘nalish uchun; rasmiy sertifikat deb taqdim etilmaydi." },
      { question: "AI javobiga ishonish mumkinmi?", answer: "Uni mashq feedbacki sifatida ishlating, ammo muhim da’volarni tekshiring; generativ AI xato qilishi mumkin." },
      { question: "3/7/21 nima uchun?", answer: "Yangi lug‘at birliklarini 3-, 7- va 30-kunlarda faol qaytarish uchun amaliy ritm." },
    ],
    ...editorial,
    sources: [cefrSource, openAiPrivacySource, googleAiSource],
    relatedPaths: ["/methodology", "/learn", "/pricing", "/about"],
    cta: { label: "Level testdan boshlash", href: "/login" },
    schemaType: "WebPage",
    published: true,
  },
  {
    path: "/contact",
    slug: "contact",
    kind: "trust",
    title: "EnglishAI bilan bog‘lanish",
    description: "Mahsulot yordami, kontent xatosi, hamkorlik va umumiy savollar uchun EnglishAI bilan bog‘lanish yo‘riqnomasi.",
    summary: "Murojaatingiz tezroq tushunilishi uchun mavzu, sahifa manzili, qurilma va muammoni takrorlash qadamlarini yozing; maxfiy ma’lumot yubormang.",
    intentCluster: "EnglishAI aloqa",
    aliases: ["EnglishAI contact", "qo‘llab-quvvatlash", "EnglishAI yordam"],
    sections: [
      { heading: "Qanday murojaat yuborish kerak?", paragraphs: ["Mahsulot muammosida foydalanilgan sahifa, kutilgan natija, ko‘rilgan xato va uni takrorlash qadamlarini yozing. Imkon bo‘lsa qurilma va brauzer turini qo‘shing.", "Kontent xatosida aniq jumla, sahifa URL’i va taklif qilayotgan tuzatishni yuboring. Bu tahririyatga manbani qayta tekshirishga yordam beradi."], steps: ["Murojaat turini tanlang", "Qisqa mavzu yozing", "Takrorlash qadamlarini qo‘shing", "Maxfiy ma’lumotni olib tashlang", "Contact formasini yuboring"] },
      { heading: "Nimani yubormaslik kerak?", paragraphs: ["Parol, karta raqami, maxfiy hujjat, API kalit yoki boshqa shaxsning shaxsiy ma’lumotini yubormang. Hisobni aniqlash zarur bo‘lsa ham, faqat forma so‘ragan minimal ma’lumotdan foydalaning.", "AI tutor promptiga ham ish siri yoki shaxsiy nozik ma’lumotni kiritmang."] },
      { heading: "Murojaat turlari", paragraphs: ["Mahsulot yordami, billing savoli, kontent tuzatishi, xavfsizlik xabari va hamkorlik taklifini mavzuda ajrating. Har biri tegishli jarayon orqali ko‘rib chiqiladi.", "Javob vaqti bo‘yicha ushbu sahifa qat’iy kafolat bermaydi; murojaatning murakkabligi va hajmiga qarab muddat farq qiladi."] },
    ],
    faq: [
      { question: "Texnik xatoni qanday yuboraman?", answer: "Contact formasida URL, qurilma/brauzer, kutilgan va amaldagi natija hamda takrorlash qadamlarini yozing." },
      { question: "Kontentdagi xatoni bildirsam bo‘ladimi?", answer: "Ha. Sahifa manzili, aniq jumla va iloji bo‘lsa ishonchli manbani qo‘shing." },
      { question: "Karta ma’lumotini yuboraymi?", answer: "Yo‘q. To‘liq karta raqami, CVV, parol yoki boshqa sirni hech qachon yubormang." },
      { question: "Javob qachon keladi?", answer: "Qat’iy muddat kafolatlanmaydi; murojaat murakkabligi va navbatga qarab farq qiladi." },
    ],
    ...editorial,
    sources: [{ label: "Google Safety Center — Security tips", url: "https://safety.google/security/security-tips/" }, openAiPrivacySource],
    relatedPaths: ["/about", "/pricing", "/product", "/editorial-policy"],
    cta: { label: "Murojaat yuborish", href: "/contact#form" },
    schemaType: "WebPage",
    published: true,
  },
  {
    path: "/editorial-policy",
    slug: "editorial-policy",
    kind: "trust",
    title: "EnglishAI tahririy siyosati",
    description: "Public o‘quv kontenti qanday yozilishi, manbalar qanday tanlanishi, AI qanday cheklanishi va xatolar qanday tuzatilishini tushuntiradi.",
    summary: "Foydali, tekshiriladigan va mahsulot faktlariga sodiq o‘zbekcha material yaratamiz; AI yordamidan foydalanilsa ham, u avtomatik haqiqat manbasi hisoblanmaydi.",
    intentCluster: "EnglishAI tahririy siyosati",
    aliases: ["editorial policy", "kontent siyosati", "manbalar va tuzatishlar"],
    sections: [
      { heading: "Yozish tamoyillari", paragraphs: ["Har bir guide aniq qidiruv niyatiga javob berishi va mahsulotdan mustaqil foyda berishi kerak. Til professional, tushunarli va asossiz va’dalardan xoli bo‘ladi.", "Mahsulot da’volari faqat mavjud AI tutor, level test, speaking, listening, reading, vocabulary, grammar, writing, CEFR A1–C2 va 3/7/21 takrorlash kabi tasdiqlangan imkoniyatlar bilan cheklanadi."] },
      { heading: "Manba va AI siyosati", paragraphs: ["CEFR haqidagi da’volar Council of Europe kabi birlamchi manbaga tayanadi. AI xizmatlari bo‘yicha tegishli rasmiy hujjat, til o‘rganish bo‘yicha esa ishonchli ta’lim yoki ilmiy manba tanlanadi.", "Generativ AI g‘oya, tuzilma yoki til tahririda yordam berishi mumkin, ammo manba sifatida ko‘rsatilmaydi. Fakt, havola va mahsulot da’vosi inson ko‘rib chiqishidan o‘tishi kerak."] },
      { heading: "Yangilash va tuzatish", paragraphs: ["Sahifada nashr va ko‘rib chiqish sanasi saqlanadi. Standart, mahsulot yoki ishonchli manba o‘zgarsa, tegishli material qayta ko‘rib chiqiladi.", "Xato tasdiqlansa, imkon qadar aniq tuzatamiz. Tuzatish takliflari contact sahifasi orqali yuborilishi mumkin; tanqid yoki murojaat natijani oldindan kafolatlamaydi."] },
      { heading: "Tijoriy aniqlik", paragraphs: ["O‘quv kontenti mahsulotga havola qilishi mumkin, biroq soxta review, uydirma foydalanuvchi natijasi yoki kafolatlangan o‘sish ishlatilmaydi.", "Narx va reja tarkibi kabi o‘zgaruvchan ma’lumot uchun amaldagi checkout yoki ilova ichidagi tavsif asosiy manba hisoblanadi."] },
    ],
    faq: [
      { question: "Kontentni AI yozadimi?", answer: "AI tahririy jarayonda yordamchi bo‘lishi mumkin, ammo fakt va manbalar inson ko‘rib chiqishidan o‘tadi; AI manbaning o‘rnini bosmaydi." },
      { question: "Qaysi manbalar ustuvor?", answer: "Rasmiy birlamchi hujjatlar, tan olingan ta’lim tashkilotlari va tegishli ilmiy manbalar." },
      { question: "Sahifalar qachon yangilanadi?", answer: "Manba, standart yoki mahsulotdagi muhim o‘zgarish aniqlanganda va davriy tahririy ko‘rib chiqishda." },
      { question: "Xatoni qanday tuzatish mumkin?", answer: "Contact orqali URL, xato jumla va asoslovchi manbani yuboring; tahririyat tekshiradi." },
      { question: "EnglishAI natijani kafolatlaydimi?", answer: "Yo‘q. Kontent va mahsulot o‘rganishga yordam beradi, lekin individual daraja yoki imtihon natijasini kafolatlamaydi." },
    ],
    ...editorial,
    sources: [cefrSource, googleAiSource, openAiPrivacySource],
    relatedPaths: ["/methodology", "/about", "/contact", "/learn"],
    cta: { label: "Tuzatish yuborish", href: "/contact" },
    schemaType: "WebPage",
    published: true,
  },
];

export const publicPages: PublicPage[] = [learnHub, ...guidePages, ...trustPages];

export const getPublicPage = (path: string): PublicPage | undefined => {
  const normalizedPath = path !== "/" && path.endsWith("/") ? path.slice(0, -1) : path;
  return publicPages.find(
    (page) => page.path === normalizedPath || page.aliases.includes(normalizedPath),
  );
};

export const publicPaths: string[] = publicPages.map((page) => page.path);
