# EnglishAI Learning Runner UI — 7 modul uchun talablar

Ushbu talablar `apps/web/src/pages/VocabularyTopicPage.tsx` va `VocabularyTopicPage.css` dagi tasdiqlangan responsive lesson runner xatti-harakatlarini boshqa learning sahifalariga xavfsiz ko‘chirish uchun yozilgan.

## Barcha modullar uchun umumiy qabul mezonlari

- Mavjud API, DTO, auth, route, progress/resume, hearts, XP, stop/reset, retry va result xatti-harakatlarini saqla.
- Route-local CSS ishlat; boshqa sahifalarga ta’sir qiladigan global selector kiritma.
- Lesson sahifasi mobile va tabletda `100dvh`, fallback `100vh`; asosiy card viewportning mavjud balandligini yuqoridan pastgacha to‘ldirsin.
- Outer page scroll emas, lesson card body ichki `overflow-y:auto` bo‘lsin. `min-height:0`, `minmax(0,1fr)`, `overscroll-behavior:contain`, `-webkit-overflow-scrolling:touch` va safe-area’larni to‘g‘ri qo‘lla.
- Card grid’i `auto minmax(0,1fr) auto`: header va footer fixed, body scrollable.
- Header elementlari bitta qatorda vertikal markazda bo‘lsin: exit/back, progress, `current/total`, hearts/XP/status, stop. Bir element tepaga, boshqasi pastga tushmasin.
- Header kichik ekranga sig‘masa labelni ixchamlashtir, lekin touch target kamida `44px` qolsin.
- Button matni haqiqiy geometrik markazda bo‘lsin. Leading icon bo‘lsa, qarama-qarshi tomonda teng o‘lchamli bo‘sh slot yarat; icon labelni chapga surmasin.
- Test variantlari `<768px` da 1 ustun, `>=768px` tablet va desktopda 2 ustun. Route-local media query bilan kafolatla; base selector breakpoint qoidasini bosib ketmasin.
- Har bir flashcard/test/question uchun `current/total`, masalan `1/20`, ko‘rsat.
- Feedback va `Keyingi` action uchun oldindan doimiy joy rezerv qil. Javob tanlanganda savol, variantlar va card koordinatalari siljimasin.
- Feedback reveal’da height animatsiyasi ishlatma; faqat opacity/transform. `Tekshirish` buttonini feedback slot balandligiga cho‘zma; u `48–56px` normal balandlikda qolsin.
- Kontent rezerv joy bilan birga viewportga sig‘masa body ichida scrollbar ishlasin.
- Loading, empty, error, retry, stopped, completed va result holatlarini buzma.
- Validation: focused tests, `npx tsc -b --pretty false`, scoped ESLint, `git diff --check`, so‘ng browser geometry tekshiruvi `390x844`, `820x1180`, `1440x900`.
- Browser’da feedbackdan oldin/keyin card, savol, variant grid va feedback slot `getBoundingClientRect()` qiymatlarini solishtir. Feedback slot balandligi va yuqoridagi elementlar `y` koordinatasi o‘zgarmasligi shart.

---

## Modul 1 — Grammar Lesson Runner

EnglishAI repositoryda Grammar lesson runnerni VocabularyTopicPage’dagi tasdiqlangan responsive lesson contract asosida qayta joylashtir.

TARGET:
- Route: /app/grammar/topic/:topicId
- Primary files: apps/web/src/pages/GrammarLessonPage.tsx va GrammarLessonPage.css
- Shared primitives: LessonStageFrame, LessonFeedbackSlot, LessonAdvanceAction va mavjud Grammar header/action komponentlari

Vazifa:
1. Avval GrammarLessonPage’ning barcha stage va state’larini xaritala: loading/error, intro/rule, explanation, choice/type/fill exercises, feedback, retry, completion/result. Mavjud biznes logikani o‘zgartirma.
2. Lesson root va asosiy cardni mobile/tabletda 100dvh/100vh ga yoy. Root grid qatori `minmax(0,1fr)` bo‘lsin; `align-content:center` sabab card kontent balandligida markazda qolib ketmasin.
3. LessonStageFrame’ni `auto minmax(0,1fr) auto` qilib, faqat body’ni scrollable qil. Header va footer viewport ichida qolishi shart.
4. Headerni bitta qatorda markazla: back/exit, progress bar, `current/total`, hearts, XP va stop. Kichik ekranda touch target 44px dan kamaymasin.
5. Har bir Grammar mashqida aniq `1/N` hisoblagich ko‘rsat. Stage progress bilan exercise progressni adashtirma.
6. Choice variantlarini mobile’da 1 ustun, 768px+ da 2 ustun qil. Type/fill input va `Tekshirish` buttoni full-width, lekin normal 48–56px balandlikda bo‘lsin.
7. Feedback message va `Keyingi` uchun oldindan doimiy slot rezerv qil. Javob berilganda savol, input/variant va card siljimasin. Reveal faqat opacity bilan ishlasin.
8. Button label’larini geometrik center qil; leading icon bo‘lgan CTA’larda qarama-qarshi ghost slot ishlat.
9. Long rule/explanation va keyboard-open holatlarda body ichki scroll qilsin; footer va actionlar kesilmasin.
10. Existing validation, score, mastery, hearts decrement, XP, retry, stop/reset, persistence va navigationni o‘zgartirma.

Acceptance:
- 390x844: full-height card, 1-column options, inner scroll, safe area.
- 820x1180 va 1440x900: 2-column options, single centered header row.
- Feedbackdan oldin/keyin question/options `y` koordinatasi va card height o‘zgarmaydi.
- Focused Grammar tests, TypeScript, scoped ESLint, diff-check va browser geometry pass.

---

## Modul 2 — Listening Topic Runner

EnglishAI repositoryda Listening topic runnerni VocabularyTopicPage’dagi responsive lesson contractga moslashtir.

TARGET:
- Route: /listening/topic/:topicId
- Files: apps/web/src/pages/ListeningTopicPage.tsx va ListeningTopicPage.css
- Audio playback, retry, question grading va result logikasini saqla.

Vazifa:
1. Audio intro/playback, transcript/guidance, question, feedback, hearts-empty, retry va result stage’larini xaritala.
2. Root va asosiy LessonStageFrame cardini mobile/tabletda 100dvh/100vh ga yoy; root grid row `minmax(0,1fr)` va stretch bo‘lsin.
3. Headerni bitta markazlangan qatorda saqla: exit, progress, `current/total`, hearts/XP, stop. Top/bottom ikki qatorli header qolmasin.
4. Audio player/control hududini barqaror saqla. Uzun transcript yoki savol matni body ichida scroll qilsin; audio control scroll ortida yo‘qolmasin.
5. Har savolda `1/N` hisoblagich ko‘rsat. Audio bosqich hisoblagichi va test savol hisoblagichini to‘g‘ri ajrat.
6. Javob variantlari mobile’da 1 ustun, 768px+ da 2 ustun.
7. Feedback + `Keyingi` uchun oldindan fixed-height slot rezerv qil. Variant tanlanganda audio player, savol va variant grid siljimasin; height animation ishlatma.
8. `Tekshirish`, replay, retry va next buttonlari normal 48–56px balandlikda bo‘lsin. Iconli label’lar ghost trailing slot bilan center qilinsin.
9. Android WebView, safe-area, touch scrolling va audio playback interactionlarini saqla.
10. Existing API, audio caching/loading, answer grading, hearts, XP, persistence, retry va result behaviorini o‘zgartirma.

Acceptance:
- Mobile’da 1-column, tablet/desktopda 2-column.
- Audio controls, header va footer doim viewport ichida.
- Feedbackdan oldin/keyin yuqoridagi layout geometriyasi o‘zgarmaydi.
- Listening focused tests, TypeScript, ESLint, diff-check va real browser geometry pass.

---

## Modul 3 — Reading Topic Runner

EnglishAI repositoryda Reading topic runnerni VocabularyTopicPage’dagi responsive lesson contract bilan birxillashtir.

TARGET:
- Route: /reading/topic/:topicId
- Files: apps/web/src/pages/ReadingTopicPage.tsx va ReadingTopicPage.css

Vazifa:
1. Intro, reading text, guidance, questions, feedback, hearts-empty, retry va result stage’larini xaritala. Reading content va grading logikasini saqla.
2. Lesson root/cardni mobile/tabletda 100dvh/100vh ga yoy. Card viewport markazida kichik bo‘lib qolmasin; root track stretch bo‘lsin.
3. Header bitta qatorda center: back/exit, progress, `current/total`, hearts/XP, stop.
4. Reading matni uzun bo‘lsa kesilmasin. Header/footer fixed, body ichki scrollable bo‘lsin; nested competing scroller yaratma.
5. Savollarda `1/N` hisoblagich qo‘llanilsin. Reading stage progress va quiz question progress semantik jihatdan to‘g‘ri ko‘rsatilsin.
6. Choice variantlari mobile’da 1 ustun, 768px+ tablet/desktopda 2 ustun.
7. Feedback va `Keyingi` uchun oldindan band slot yarat. Javob tanlanganda savol va variantlar joyi o‘zgarmasin. Feedback faqat opacity bilan ochilsin.
8. Button label’lari, shu jumladan iconli next/back CTA’lar geometrik center bo‘lsin.
9. Result va empty/error holatlarda uzun content kerak bo‘lsa page/body ichki scroll bilan to‘liq ko‘rinsin.
10. Existing API, topic gating, answer evaluation, hearts, XP, progress persistence, retry va navigationni saqla.

Acceptance:
- 390x844’da matn va test content kesilmaydi; bir savol variantlari bitta ustunda.
- 820x1180 va desktopda variantlar ikki ustunda.
- Feedback reveal layout shift bermaydi.
- Focused Reading tests, TypeScript, ESLint, diff-check va browser geometry pass.

---

## Modul 4 — Writing Topic va Task Runner

EnglishAI repositoryda Writing learning runnerlarini VocabularyTopicPage’dagi responsive lesson contractga moslashtir.

TARGET:
- Routes: /writing/topic/:topicId va /writing/task/:topicId
- Primary files: WritingTopicPage.tsx/.css va WritingTaskPage.tsx/.css
- Har route uchun route-local CSS saqla; bir sahifa selectorini boshqasiga global qo‘llama.

Vazifa:
1. Topic intro/instruction, writing task, textarea/input, submit, checking/loading, AI feedback, retry va result stage’larini ikkala route bo‘yicha xaritala.
2. Root va runner cardni mobile/tabletda 100dvh/100vh ga yoy; header/body/footer grid ishlat.
3. Headerni bitta center qatorda joylashtir: back/exit, progress, `current/total` yoki stage count, status, stop.
4. Textarea keyboard ochilganda viewportdan chiqib ketmasin. Body scrollable bo‘lsin, active input `scrollIntoView` orqali ko‘rinadigan hududda qolsin; footer keyboard bilan to‘qnashmasin.
5. Bir nechta prompt/question bo‘lsa `1/N` ko‘rsat. Bitta task bo‘lsa stage progressionni aniq va yolg‘on bo‘lmagan formatda ko‘rsat.
6. Submitdan keyingi correctness/AI feedback va `Keyingi`/retry action uchun oldindan doimiy slot rezerv qil. Loading yoki feedback paydo bo‘lganda textarea va prompt siljimasin.
7. `Tekshirish`/`Yuborish` normal 48–56px balandlikda qolsin; feedback slotni to‘liq egallab semizlashmasin.
8. Button labels iconlardan qat’i nazar geometrik center bo‘lsin.
9. Long AI feedback slot ichida yoki body ichida scroll qilsin; butun card height oshib viewportdan chiqmasin.
10. Existing API payload, autosave/progress, rate-limit/error handling, retry, result va navigationni o‘zgartirma.

Acceptance:
- Mobile keyboard-open holati real browser/emulation bilan tekshiriladi.
- Feedbackdan oldin/keyin prompt/textarea koordinatasi o‘zgarmaydi.
- Full-height card, inner scroll va centered header barcha target o‘lchamlarda pass.
- Ikkala Writing route uchun focused tests, TypeScript, ESLint va diff-check pass.

---

## Modul 5 — Speaking Lesson Runner

EnglishAI repositoryda Speaking lesson runnerlarini VocabularyTopicPage’dagi full-height responsive contractga moslashtir.

TARGET ROUTES:
- /app/speaking/topic/:topicId
- /app/speaking/free
- /app/speaking/free-talk/:topicCode
- /app/speaking/role-talk/:scenarioCode
- Primary files: SpeakingPage.tsx va SpeakingPage.css

Vazifa:
1. Session start, tutor message, recording, STT/transcript, pronunciation/assessment, feedback, retry, session-ended va result holatlarini xaritala.
2. Faqat lessonFrame route’larda runner root/cardni mobile/tabletda 100dvh/100vh ga yoy. Standard `/app/speaking` catalog/entry ko‘rinishini buzma.
3. Headerni bitta markazlangan qatorda joylashtir: exit/back, conversation progress yoki turn `current/total`, session status, stop. Top va bottomga ajralgan header qolmasin.
4. Main conversation card yuqoridan pastgacha mavjud heightni egallasin. Messages/transcript uzun bo‘lsa faqat body scroll qilsin; recording controls ko‘rinadigan hududda qolsin.
5. Finite exercise/word/turn mavjud bo‘lsa `1/N` hisoblagich ko‘rsat. Cheksiz free-talk uchun yolg‘on total yaratma; turn count yoki session status ishlat.
6. Assessment feedback va next/retry action uchun oldindan rezerv slot yarat. Recording natijasi kelganda transcript, prompt va microphone control siljimasin.
7. Microphone, stop, retry va next buttonlari kamida 44px touch target, label geometrik center. Icon-only control aria-label saqlasin.
8. Keyboard/tutor-name modal, audio permission, recording cancellation, RequestAborted, STT fallback va streamed tutor response behaviorini o‘zgartirma.
9. Native APK safe-area, keyboard-open va touch interactionlarni alohida tekshir.
10. Existing API/session persistence, SSE/cancellation, score, pronunciation queue, stop/reset va navigationni saqla.

Acceptance:
- Topic/free-talk/role-talk lesson route’lari full-height; catalog route o‘zgarmaydi.
- Feedback yoki transcript kelganda control geometriyasi sakramaydi.
- Mobile/tablet/desktop visual geometry, focused Speaking tests, TypeScript, ESLint va diff-check pass.

---

## Modul 6 — Video Player va Video Quiz

EnglishAI repositoryda Video player va Video quiz flowlarini VocabularyTopicPage’dagi responsive lesson contractga moslashtir, lekin playerning media xususiyatlarini saqla.

TARGET:
- Routes: /video/:id/play va /video/:id/quiz
- Files: VideoPlayerPage.tsx/.css va VideoQuizPage.tsx/.css

Vazifa:
1. Player loading/error, playback, subtitles/translation, assistant/overlay, fullscreen/rotation hamda quiz question/feedback/result state’larini xaritala.
2. Player sahifasi mobile/tabletda 100dvh/100vh bo‘lsin. Video aspect ratio, landscape rotation, fullscreen va native capability behaviorini buzma.
3. Non-fullscreen layoutda header/controls bitta markazlangan qatorda bo‘lsin. Video surface mavjud space’ga moslashsin; qolgan content inner scroll orqali ko‘rinsin.
4. Quiz runner cardni viewport heightga yoy va `auto minmax(0,1fr) auto` qil. Header: exit/back, progress, `1/N`, status — bitta center qatorda.
5. Quiz answer variantlari mobile’da 1 ustun, 768px+ tablet/desktopda 2 ustun.
6. Feedback va `Keyingi` uchun oldindan fixed slot rezerv qil. Variant tanlanganda video preview, savol va variantlar siljimasin.
7. `Tekshirish` normal heightda qolsin. Iconli player/quiz button label’larini geometrik center qil; media controlsning accessibility label’larini saqla.
8. Subtitles va overlay uzun bo‘lsa o‘z hududida scroll/wrap qilsin; video frame heightni kutilmaganda oshirmasin.
9. Existing playback position, quiz progress persistence, API, captions, fullscreen, orientation, native capability va result/navigationni o‘zgartirma.
10. Player va quizni alohida browser acceptance bilan tekshir.

Acceptance:
- Portrait mobile, tablet va desktopda full-height shell.
- Landscape/fullscreen player regressiyasiz.
- Quiz mobile 1-column, tablet/desktop 2-column; feedback layout shift yo‘q.
- Focused Video tests, TypeScript, ESLint, diff-check va browser geometry pass.

---

## Modul 7 — Kutubxona: Book Detail, Reading va Quiz

EnglishAI repositoryda Book detail learning flowini VocabularyTopicPage’dagi responsive lesson contractga moslashtir.

TARGET:
- Route: /books/:bookId
- Primary files: apps/web/src/pages/BookDetailPage.tsx va BookDetailPage.css
- Scope: intro, section reading, section question/quiz, feedback, section result va book completion.
- Books catalog scope’dan tashqarida.

Vazifa:
1. Intro, section summary/reading, question, feedback, retry, section result va completion stage’larini xaritala. RunnerHeader va LessonStageFrame contractini ko‘rib chiq.
2. Book runner cardni mobile/tabletda 100dvh/100vh ga yoy. Root grid track stretch bo‘lsin; card content balandligida centerda qolmasin.
3. Header elementlarini bitta center qatorda joylashtir: back/exit, progress, section yoki question `current/total`, status. Title uzun bo‘lsa ellipsis, touch targetlar 44px+.
4. Reading content uzun bo‘lsa header/footer joyida qoladi va body ichki scroll qiladi. Section content kesilmasin.
5. Quiz savollarida `1/N`, section reading’da section `current/total` ko‘rsat.
6. Quiz variantlari mobile’da 1 ustun, 768px+ tablet/desktopda 2 ustun.
7. Feedback va next/retry uchun oldindan band slot yarat. Javob tanlanganda savol va variant grid siljimasin; feedback faqat opacity bilan ochilsin.
8. `Tekshirish` normal 48–56px balandlikda qolsin. Footer CTA va iconli button label’lari geometrik center bo‘lsin.
9. Result/completion content viewportdan uzun bo‘lsa ichki scroll bilan to‘liq ko‘rinsin; redundant nested cards yaratma.
10. Existing section progress, resume, API, answer grading, rewards, retry, continue, book completion va navigationni saqla.

Acceptance:
- 390x844’da reading scrollable, quiz 1-column, safe-area to‘g‘ri.
- 820x1180 va 1440x900’da quiz 2-column, header bitta center qatorda.
- Feedbackdan oldin/keyin card/question/options geometriyasi o‘zgarmaydi.
- Book detail focused tests, TypeScript, ESLint, diff-check va browser geometry pass.
