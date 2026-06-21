export type LandingAccent = "lime" | "purple" | "blue" | "orange" | "green" | "yellow";

export interface LandingSkillStory {
  id: string;
  icon: string;
  title: string;
  promise: string;
  action: string;
  result: string;
  accent: LandingAccent;
  media: string;
}

export const landingStory = {
  hero: {
    eyebrow: "3 yil o'rgandingiz. Endi gapirish vaqti.",
    title: "Ingliz tilini bilasiz, lekin gapira olmaysizmi?",
    lead: "EnglishAI passiv bilimingizni faol nutqqa aylantiradi. Sizga 24/7 AI o'qituvchi biriktiriladi, oltita ko'nikma esa bitta mavzu atrofida birga ishlaydi.",
    note: "Xato qilishdan qo'rqmang: AI tutor charchamaydi, hukm qilmaydi va har bir javobingizni tushuntirib beradi.",
  },
  problem: {
    eyebrow: "Muammo sizda emas — usulda",
    title: "Nega yillar davomida o'qib ham gap boshlash qiyin?",
    lead: "So'z yodlash, grammatika va listening alohida o'rganilganda miya ularni real suhbat paytida birlashtira olmaydi. EnglishAI bilimni jamlamaydi — uni ishlatishga majbur qiladigan xavfsiz muhit yaratadi.",
    cards: [
      { icon: "menu_book", title: "Bilim passiv qoladi", text: "Qoidani taniysiz, ammo gap tuzish uchun kerakli so'z ayni paytda yodga kelmaydi." },
      { icon: "mic", title: "Gapirish amaliyoti kam", text: "Darsda navbat qisqa, hayotda esa xato qilishdan uyat va qo'rquv gapni to'xtatadi." },
      { icon: "extension", title: "Ko'nikmalar uzilgan", text: "Bugun o'rgangan so'z ertangi reading, writing va speaking mashqida qayta ishlatilmaydi." },
    ],
  },
  ai: {
    eyebrow: "Ikki xil AI yordami",
    title: "O'qituvchi gapirtiradi. Yordamchi savolingizga javob beradi.",
    lead: "EnglishAI'da AI tutor va AI yordamchi bir-birini to'ldiradi: biri nutq amaliyotini boshqaradi, ikkinchisi tushunmagan joyingizni shu zahoti izohlaydi.",
    tutor: { icon: "record_voice_over", title: "24/7 AI o'qituvchi", text: "Darajangiz va mavzuingizga mos savol beradi, javobingizni tinglaydi, talaffuz va gap tuzilishini tuzatadi, so'ng suhbatni tabiiy davom ettiradi.", points: ["Istalgan vaqtda ovozli suhbat", "Shaxsiy daraja va temp", "Darhol talaffuz feedbacki"] },
    helper: { icon: "smart_toy", title: "Har qanday savol uchun AI yordamchi", text: "“Nega bu zamon ishlatildi?”, “Bu so'zning farqi nima?” yoki “Yana sodda misol ber” deb so'rang. Javob o'zbekcha va ayni dars kontekstida keladi.", points: ["Grammatika va so'z izohi", "Tarjima va real misollar", "Darsdan chiqmasdan yordam"] },
  },
  cycle: {
    eyebrow: "Bitta mavzu — oltita ko'nikma",
    title: "Bir marta ko'rgan so'zingiz olti xil vazifada faol ishlaydi",
    lead: "Masalan, “Travel” mavzusidagi so'zlar Vocabulary'dan boshlanadi, Grammar va Reading'da qayta uchraydi, Writing va Speaking'da siz tomondan ishlatiladi, Listening'da esa tirik nutq ichida taniladi.",
  },
  skills: [
    { id: "vocabulary", icon: "menu_book", title: "Vocabulary", promise: "So'zni tarjimasi bilan emas, vaziyat bilan eslab qoling.", action: "Travel mavzusidagi tayanch so'zlarni misol, audio va mini-test orqali o'rganasiz.", result: "Keyingi besh ko'nikma aynan shu lug'atni qayta ishlatadi.", accent: "lime", media: "vocabulary" },
    { id: "grammar", icon: "rule", title: "Grammar", promise: "Qoidani yodlamang — hozirgina o'rgangan so'z bilan gap tuzing.", action: "O'zbeklar ko'p qiladigan xatolar qisqa izoh va kontekstli bosqichlarda tuzatiladi.", result: "Speaking paytida kerak bo'ladigan tayyor gap qoliplari paydo bo'ladi.", accent: "purple", media: "grammar" },
    { id: "reading", icon: "auto_stories", title: "Reading", promise: "Yangi so'zlarni tabiiy hikoya ichida yana uchrating.", action: "CEFR darajangizga mos matn, tezkor so'z izohi va tushunish savollarini bajarasiz.", result: "Miya so'zni alohida kartochka emas, ma'no va vaziyat bilan bog'laydi.", accent: "green", media: "reading" },
    { id: "writing", icon: "edit", title: "Writing", promise: "Fikringizni mustaqil tuzing va aniq AI feedback oling.", action: "AI vazifa, mantiq, lug'at, grammatika va mavzu so'zlari qamrovini baholaydi.", result: "Xatoni faqat ko'rmaysiz — yaxshiroq variantni yozib mustahkamlaysiz.", accent: "orange", media: "writing" },
    { id: "speaking", icon: "mic", title: "Speaking", promise: "Bilimingiz real ovozli suhbatga aylanadigan asosiy bosqich.", action: "AI tutor mavzu bo'yicha gapirtiradi, talaffuzni baholaydi va javobingizga mos savol beradi.", result: "Tayyor jumlani o'qish emas, o'z fikringizni inglizcha aytish odati shakllanadi.", accent: "lime", media: "speaking" },
    { id: "listening", icon: "headphones", title: "Listening", promise: "O'rgangan iboralarni tabiiy tezlikdagi nutqda taniy boshlang.", action: "Audio, interaktiv transkript, qayta eshitish va tushunish testi bitta oqimda ishlaydi.", result: "Eshitish va gapirish bir-birini kuchaytiradigan yopiq sikl hosil qiladi.", accent: "blue", media: "listening" },
  ] satisfies LandingSkillStory[],
  levels: {
    eyebrow: "A1 dan C2 gacha aniq yo'l",
    title: "Har bir darajada 50 ta tartiblangan dars",
    lead: "Daraja aniqlash testi sizni mos nuqtaga joylashtiradi. Har darajadagi 50 mavzu osondan murakkabga o'tadi; har bir mavzu ichida oltita skill bitta topic spine bo'yicha ishlaydi.",
    levels: ["A1", "A2", "B1", "B2", "C1", "C2"],
  },
  libraries: [
    { id: "books", icon: "library_books", title: "Kitoblar kutubxonasi", promise: "Darajangizga mos graded reader'larni qo'rqmasdan o'qing.", action: "Kitob bobma-bob ochiladi, notanish so'z izohi va tushunish testi o'qish jarayonining o'zida beriladi.", result: "Uzoqroq matnni tushunish, diqqat va kontekstdan ma'no topish qobiliyati o'sadi.", accent: "purple", media: "books" },
    { id: "video", icon: "play_circle", title: "Video kutubxonasi", promise: "Real ingliz tilini ko'ring, eshiting va ortidan takrorlang.", action: "Darajalangan video, interaktiv transkript, so'z izohi, shadowing va yakuniy quiz bir joyda.", result: "Tabiiy tezlik, urg'u va jonli iboralarni real vaziyatda taniysiz.", accent: "blue", media: "video" },
  ] satisfies LandingSkillStory[],
  routine: {
    eyebrow: "Har kuni 10–15 daqiqalik aniq yo'l",
    title: "Nimani o'rganishni o'ylamaysiz — tizim keyingi qadamni beradi",
    steps: [
      { icon: "school", title: "1. Darajani aniqlang", text: "Qisqa adaptiv test mos boshlanish nuqtasini topadi." },
      { icon: "route", title: "2. Bitta mavzuni tanlang", text: "Ish, sayohat yoki kundalik hayotga kerakli topicni oching." },
      { icon: "extension", title: "3. Olti skillda ishlating", text: "Bir lug'at reading, writing, speaking va listening bo'ylab yuradi." },
      { icon: "smart_toy", title: "4. AI feedback oling", text: "Xato darhol tushuntiriladi va qayta ishlash imkoniyati beriladi." },
      { icon: "repeat", title: "5. 3/7/21 da qaytaring", text: "So'z unutilishidan oldin yangi kontekstda faol sinaladi." },
    ],
  },
} as const;
