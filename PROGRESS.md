# PROGRESS

## 2026-09-14 — Development hujjatlari va verification output

- Arxitektura, xavfsizlik va delivery qoidalari `docs/development-guide.md` da
  birlashtirildi; koddagi texnik havolalar shu qo'llanmaga yangilandi.
- Mobil migratsiya va lesson runner talablari alohida implementation/requirements
  hujjatlari sifatida tartiblandi. Mahsulot mantig'i va aktivlar atributsiyasi saqlandi.
- Playwright output papkalari `.artifacts/` ga ko'chirildi; Git, lint va Docker
  ignore qoidalari moslashtirildi.
- Tekshiruvlar: frontend typecheck, lint, production build; backend solution build
  va 1374 ta Architecture/Domain/Application testi; Playwright konfiguratsiyalari
  discovery tekshiruvi va bitta Chromium smoke testi o'tdi.
- Frontend: 1011 test o'tdi, 4 ta mavjud dizayn tekshiruvi yiqildi
  (`DesignGuard.test.ts` — 3 ta, `responsive-bands.test.ts` — 1 ta).
  O'zgarishdan oldingi commitning alohida nusxasida aynan shu 4 xato qayta tasdiqlandi.

---

## 2026-09-13 — Internet aloqasi yo'q modal oynasi (pen 81 WEB/MOBILE)

Branch: `feat/energy-refill-and-balance-modal`

### Nima uchun

`englishai.pen` dagi "81 • WEB • /home • Internet aloqasi yo'q" va uning MOBILE juftligi
kodga ko'chirilmagan edi. Ilovada umuman global oflayn aniqlash yo'q edi: `navigator.onLine`
ham, `online`/`offline` listener ham hech qayerda ishlatilmagan. Internet uzilganda
foydalanuvchi faqat muvaffaqiyatsiz `fetch` dan keyin, har bir sahifaning o'z xato matni
orqali xabar topardi.

### Bajarildi

- `src/lib/useOnline.ts` — `online`/`offline` hodisalari avtomatik ochilish uchun.
  `navigator.onLine` faqat qurilmada tarmoq interfeysi borligini bildirgani uchun
  "Qayta urinish" tugmasi `/health` ga haqiqiy so'rov yuboradi (6 s timeout,
  `cache: "no-store"`); javob bergan har qanday status ulanish tirikligini isbotlaydi.
  SSR (`entry-prerender.tsx`) uchun `navigator` yo'qligi hisobga olingan.
- `src/components/OfflineModal.tsx` + `.css` — dizayn 1:1: 520px/32px (web) va
  342px/24px (mobil) karta, eyebrow + yopish chipi, 64px wifi-off plitka, markazlashgan
  sarlavha, "Energiya sarflanmadi" tinchlantiruvchi bloki, qayta urinish tugmasi,
  "Hozir emas" va pastki izoh. `DesignModal` ustiga qurilgan (DesignGuard shuni talab
  qiladi) — kanonik header faqat accessible name uchun qoladi, ko'rinmas qilinadi.
- Pen dagi `Internet / Checking`, `Still offline`, `Reconnected` komponentlari qayta
  urinish holati sifatida shu modal ichida ko'rsatiladi.
- `src/content/uz.ts` — `uz.offline` namespace (11-qoida: hech qanday hardcode matn).
- `src/components/ui/iconMap.ts` — `wifi` (Reconnected holati uchun) qo'shildi.
- `src/main.tsx` — modal router ustiga o'rnatildi, shuning uchun barcha sahifalarda
  (o'quvchi shell, admin shell, login, ochiq sahifalar) ishlaydi.

### Qanday test qilindi

- `src/components/OfflineModal.test.tsx` — 7 ta test: onlayn holatda ko'rinmasligi,
  uzilishda ochilishi, oflayn boot, probe muvaffaqiyatsiz → "Hali ham...", probe
  muvaffaqiyatli → "Ulanish tiklandi" → yopilishi, `online` hodisasida yopilishi,
  "Hozir emas" dan keyin keyingi uzilishda qayta chiqishi. Hammasi yashil.
- Brauzerda (Playwright, 1440px va 390px): karta 520×500 (dizayn 520×496) va mobilda
  342px markazlashgan — mobil uchun `overlays.responsive.css` ning 700px dan pastdagi
  bottom-sheet ko'rinishi shu modal uchun bekor qilindi, chunki dizayn markazlashgan
  kartani ko'rsatadi.
- `npx tsc -b` va `eslint` toza.

### Ochiq

- Dizayndagi tugma soyasi (`0 3px 0 #5126B8`) DesignGuard da taqiqlangan (offset shadow),
  shuning uchun `.vocabulary-primary` kabi `border-bottom: 3px solid` bilan berildi.
- Butun test suite da 4 ta yiqilish bor (`DesignGuard` ×3, `responsive-bands` ×1), lekin
  ularning hech biri bu ishga tegishli emas — ro'yxatdagi fayllar `SpeakingPage.css`,
  `VideoPlaylistPlayerPage.css`, `GrammarCatalogPage.css` va h.k., ya'ni daraxtdagi
  boshqa commit qilinmagan ishlar.

---

## 2026-08-17 — Onboarding: anti-cheat foydalanuvchilarni darajasiz qoldirayotgani

Branch: `main`

### Muammo

Prod'da ko'p hisob "Boshlanmagan" darajada qolgan. Prod DB va backend loglari o'lchandi
(2026-08-17 11:51 UTC):

| Ko'rsatkich | Qiymat |
|---|---|
| Ro'yxatdan o'tgan | 113 |
| `LearnerProfile` bor | 86 |
| Darajasiz ("Boshlanmagan") | **27** |
| `PlacementStarted` → `PlacementCompleted` | **48 → 22** |
| Test boshlab, profil olmaganlar | **21** |
| Oxirgi muvaffaqiyatli test | 2026-08-14 |

Ya'ni darajasiz qolganlarning 78% i testni **boshlagan**, lekin tugatolmagan.

### Ildiz sabab — bitta sessiya loglaridan tiklandi

`shohruxabdumalikov060@gmail.com` (07:06:54 da ro'yxatdan o'tgan):

```
07:07:31  /placement/start
07:07:46 → 07:08:23   5 javob
07:08:25  /placement/integrity-violation      ← 1-zarba
07:09:29 → 07:12:21   yana 16 javob = 21 (6+6+5+4, barcha MCQ bosqichlari tugadi)
          22-item = Writing …3 daqiqa 23 soniya…
07:15:44  /placement/integrity-violation      ← 2-zarba → sessiya bekor
```

`finalize` hech qachon chaqirilmadi → `LearnerProfile` yo'q → daraja "Boshlanmagan".

Uch mustaqil kamchilik birga ishlagan:

1. `RecordIntegrityViolation` da chegara **2** edi - 21 ta javob bir zumda yo'q bo'lardi.
2. Kuzatiladigan signallar aldashni emas, oddiy telefon xatti-harakatini ushlaydi.
   `window blur` allaqachon sensorli qurilmalarda o'chirilgan edi, lekin `fullscreen_exit`
   emas: Android Chrome fullscreen'dan **klaviatura ochilganda** (Writing) va **mikrofon
   ruxsati so'ralganda** (Speaking) o'zi chiqadi. Ya'ni testning oxirgi ikki savoli har biri
   bepul zarba berardi - roppa-rosa ikkitasi.
3. Bekor qilingan ekranda yagona tugma `/welcome` ga olib borardi va sessiya id
   `sessionStorage` da saqlangani uchun (mobil brauzer fondagi tabni tozalaydi) davom
   ettirib ham bo'lmasdi. Chiqish yo'li yo'q edi.

Buning ustiga violation `reason` hech qayerda loglanmagan, shuning uchun muammo ko'rinmagan:
testni tashlab ketgan foydalanuvchi bilan tizim o'ldirgan foydalanuvchi bir xil ko'rinardi.

### Bajarildi

- `PlacementTestSession.ViolationsBeforeInvalidation = 4` (avval qattiq yozilgan `2`).
  Ataylab tab almashtirishni baribir to'xtatadi, lekin klaviatura/mikrofon zarbalarini
  yutib yuboradi.
- `secureAssessment.ts` - `fullscreen_exit` endi faqat pointer qurilmalarda hisoblanadi
  (`isTouchPrimaryDevice()` bo'yicha, `window blur` bilan bir xil qoida). Fon rejimi
  (`visibilitychange`/`pagehide`) telefonlarda o'z kuchida qoladi.
- `withoutFullscreenWatch(action)` - `getUserMedia` chaqiruvi shu oyna ichida bajariladi,
  chunki Chrome mikrofon so'rovini fullscreen'dan chiqarib ko'rsatadi. So'rovdan keyin
  fullscreen qaytmasa, kiosk qulfi yoqiladi va kuzatuv yolg'on zarba bermaydi.
- Bekor qilingan ekran: "Testni qayta boshlash" va "A1 darajadan boshlash" (`/learning/start`).
  Endi muvaffaqiyatsiz test hisobni darajasiz qoldirmaydi.
- Sessiya id `sessionStorage` → `localStorage` (eski kalit o'qishda hurmat qilinadi), shuning
  uchun fonga tushgan mobil tab yarim tugagan testni yo'qotmaydi.
- `POST /api/placement/integrity-violation` endi sabab, bosqich, savol raqami va hisobni
  loglaydi (natijaga `Stage`/`ItemNumber`/`TotalItems` qo'shildi).
- Ogohlantirish modalidagi qattiq yozilgan o'zbekcha matn kontent qatlamiga (`uz.placement.secure`)
  o'tkazildi; matnlar yangi chegaraga moslab yangilandi.

### Test

`dotnet build` 0 xato / 0 ogohlantirish. `dotnet test`: 2000 ta test yashil
(Domain 568, Application 738, Integration 691, Architecture 3). Web: `tsc --noEmit` toza,
`eslint` toza, `vitest run` 139 fayl / 760 test yashil. Yangi testlar: chegaragacha bo'lgan
har bir hodisa sessiyani o'ldirmasligi, sensorli qurilmada `fullscreen_exit` e'tiborsiz
qolishi, ruxsat so'rovi oynasida zarba berilmasligi va oyna fullscreen'da qolsa kuzatuv
davom etishi.

### Ochiq

- Testni tugatolmagan 21 ta foydalanuvchiga hozircha tegilmadi (foydalanuvchi qarori). Ular
  keyingi kirishda `/welcome` dan "A1 dan boshlash" orqali kira oladi.
- `/hubs/notifications` va `/api/learning/{id}/events` da "CORS policy execution failed"
  loglanmoqda - alohida, darajaga aloqasi yo'q, tekshirilmadi.

---

## 2026-08-17 — Mobil: xavfsiz test kiosk rejimi va video kartadan chiqib ketgan player

Branch: `feat/cost-governance-and-free-tier`

### Nima uchun

Ikki mobil xato bir sessiyada hal qilindi.

1. **"Darajani aniqlash" testi telefonda umuman boshlanmasdi.** `lib/secureAssessment.ts` faqat
   prefikssiz Fullscreen API'ga tayanardi (`document.fullscreenEnabled &&
   documentElement.requestFullscreen`). iOS Safari/WKWebView'da (Capacitor shell ham) bu ikkalasi
   ham yo'q, ba'zi Android WebView'larda faqat `webkit*` bor. Natijada `beginSecureTest` darhol
   xatoga tushib, foydalanuvchiga "Chrome, Edge yoki Firefox'dan foydalaning" deb yozilardi —
   ya'ni iPhone egasi platformaning kirish testini o'ta olmasdi.
2. **Video sahifasida mobilda video karta chegarasidan o'ngga chiqib ketardi** va shu sabab
   "AI" hamda "To'liq ekran" tugmalari ekrandan tashqarida qolardi.

### Ildiz sabab (2-xato) — o'lchab tasdiqlangan

`.video-player-stage` — `display:grid`, lekin `grid-template-columns` **berilmagan** edi. Yagona
implicit ustun `auto`, ya'ni **max-content** bo'yicha o'lchanadi va YouTube iframe'ning o'z piksel
kengligi ustunni kartadan kengroq qilib yuborardi. Pixel 5 / 390px emulyatsiyasida o'lchandi:

| | oldin | keyin |
|---|---|---|
| `.video-player-stage` | x 8 → 382 (374px) | x 8 → 382 |
| `.video-player-frame` | x 14 → **450** (436px) | x 14 → 376 (362px) |
| AI tugmasi o'ng qirrasi | **403** (ekran 390) | 329 |
| "To'liq ekran" o'ng qirrasi | **441** | 367 |

Grid ustuni 436px bo'lgani uchun sarlavha paneli ham 436px'ga cho'zilardi va uning o'ng
tomonidagi tugmalar ekrandan chiqib ketardi. Tuzatish — `grid-template-columns: minmax(0, 1fr)`.

### Bajarildi

**Xavfsiz test (fullscreen + kiosk)**

- `lib/fullscreen.ts` (yangi) — prefiksli Fullscreen API qatlami: `fullscreenElement`,
  `fullscreenSupported`, `requestFullscreenOn` (`navigationUI:"hide"` → bare → `webkit*`),
  `exitFullscreenIfActive`, `onFullscreenChange` (ikkala event nomi).
- `lib/kioskLock.ts` (yangi) + `index.css` bloki — Fullscreen API yo'q brauzerlar uchun kiosk
  qulfi: html/body `position:fixed; 100dvh`, skroll/pinch/tanlash/context-menu o'chiriladi,
  viewport meta `user-scalable=no` ga almashadi va release'da aniq tiklanadi.
  `[data-secure-assessment-surface]` — test yuzasi yagona skroll konteyneri bo'ladi, shuning
  uchun uzun reading/writing itemlari baribir skroll qilinadi.
- `lib/secureAssessment.ts` — `SecureAssessmentMode = "fullscreen" | "kiosk"`.
  `enterSecureAssessment()` endi `boolean` emas, rejim qaytaradi va **hech qachon fail
  qilmaydi**. Signal matritsasi: `visibilitychange` + `pagehide` doim; `window blur` **faqat
  touch bo'lmagan qurilmada** (telefonda ekran klaviaturasi va bildirishnoma pardasi soxta blur
  beradi va halol o'quvchining testini bekor qilardi); `fullscreenchange` faqat fullscreen
  rejimida. Hook unmount'da kiosk qulfini bo'shatadi — busiz test o'rtasida Back bosilsa butun
  ilova `position:fixed` bo'lib qotib qolardi.
- Backend tekshirildi: `ReportPlacementIntegrityViolationCommandValidator` `Reason` ni qattiq
  allowlist bilan tekshiradi (`visibility_hidden|window_blur|fullscreen_exit`), handler esa uni
  saqlamaydi. Shuning uchun **yangi reason qo'shilmadi**, backend o'zgarmadi.
- `PlacementTestPage`, `ExitTestPage`, `AdaptiveTestRunner` — test har qanday qurilmada
  boshlanadi; fullscreen yo'q brauzerda kiosk haqida qisqa izoh ko'rsatiladi.
- **11-qoida:** shu ekranlardagi 10 ta hardcode qilingan o'zbekcha matn `uz.placement.secure.*`
  ga ko'chirildi (`SecureAssessmentWarning` ichidagilar ham).

**Video player**

- `VideoPlayerPage.css` — `.video-player-stage` ga `grid-template-columns: minmax(0, 1fr)`
  (asosiy tuzatish, yuqoridagi jadval).
- Mobil boshqaruvlar (ovoz / sozlama / **AI** / **To'liq ekran**) video kadrining ichidan
  chiqarilib, karta sarlavha paneliga ko'chirildi — endi ularni na video sirti, na 3 soniyalik
  avto-yashirish taymeri yopa olmaydi. Fullscreen'da klaster kadr ichida qoladi (fullscreen
  elementi faqat o'z avlodlarini ko'rsata oladi).
- `lib/videoViewport.ts` (yangi) — `visualViewport` o'lchovi layout viewport'ga clamp qilinadi.
  `--video-player-viewport-*` endi **faqat fullscreen'da** yoziladi va `visualViewport scroll`
  listeneri olib tashlandi (u faqat offsetni o'zgartiradi, o'lchamni emas — har skroll kadrida
  4 ta shell-wide `max-width` qayta yozilardi).
- `lib/useYouTubePlayer.ts` — konteyner har fit'da kech yechiladi (ilgari effekt boshida bir
  marta o'qilib, `null` bo'lsa ResizeObserver umuman ulanmasdi), `fitIframeToContainer` endi
  **hech qachon `"100%"` yozmaydi** (aynan shu Android WebView'ning 640px sirtini qayta
  yoqadi), va `videoId` bo'lmasa player umuman qurilmaydi.
- `toggleFullscreen` prefiksli API'ga o'tkazildi; Fullscreen API umuman yo'q bo'lsa (iPhone
  Safari) CSS `position:fixed` fallback'i ishlaydi va Escape bilan chiqiladi — tugma endi jim
  o'lik emas.

### Qanday test qilindi

- `npm test` — **139 fayl / 757 test yashil**. Yangi: `lib/fullscreen.test.ts` (8),
  `lib/kioskLock.test.ts` (5), `lib/videoViewport.test.ts` (6), `useYouTubePlayer.test.tsx` ga
  `fitIframeToContainer` testlari, `secureAssessment.test.tsx` ga kiosk/pagehide/touch-blur/
  unmount testlari, `ExitTestPage.test.tsx` ga "Fullscreen API'siz ham boshlanadi",
  `VideoPlayerPage.test.tsx` ga "AI va To'liq ekran kadr ichida emas".
- `npm run build` (`tsc -b` + vite + prerender) toza.
- Qo'lda: mock-server + dev server ustida Playwright bilan Pixel 5 (390×844, DPR 3, isMobile)
  emulyatsiyasi — yuqoridagi o'lchovlar jadvali, hamda "To'liq ekran" bosilganda native
  fullscreen + portret uchun CSS rotatsiya, `--video-player-viewport-width: 390px` faqat
  fullscreen'da, klaster kadr ichiga ko'chishi tasdiqlandi.

### Ochiq

- Haqiqiy iPhone/iPad va Capacitor Android build'ida kiosk rejimini qo'lda tekshirish kerak
  (emulyatsiya WKWebView xatti-harakatini to'liq takrorlamaydi).

## 2026-08-16 — Xarajat boshqaruvi, bepul tarif limitlari va monetizatsiyaga tayyorgarlik

Branch: `feat/cost-governance-and-free-tier`

### Nima uchun

Prod jonli (108 ro'yxatdan o'tgan, 58 faol/30 kun), lekin biznes qatlami ishlamas edi:
to'lov o'chiq, narx hech qayerda ko'rsatilmagan, har akkaunt 30 kunlik bepul Pro trial olardi,
va faol foydalanuvchining AI xarajati obuna narxidan yuqori edi. Bundan tashqari AI byudjeti
global (foydalanuvchi boshiga emas) edi, eng qimmat operatsiya — AI yordamchi — esa umuman
cheklanmagan edi.

### Bajarildi

**Bosqich 1 — xarajat**

- Talaffuz baholash endi har turn'da emas, namuna asosida ishlaydi (1, 2, keyin har 4-turn).
  Ilgari STT'ga yuborilgan ayni audio ikkinchi marta hisoblanardi (docs/development-guide.md 10-qoida buzilishi).
  `ConversationSession.ShouldAssessPronunciation`.
- Baholanmagan sessiya endi mavzuga **nol ball yozmaydi** — soxta ball o'rniga mavzu ochiq qoladi.
- LLM marshrutlash: `FeatureRoutedLlmCompletion` `ResilientHermesGatewayLlmCompletion` **ostida**.
  Faqat `WritingAssessment` va `Assistant` pullik modelga boradi; 96 tokenlik speaking tutor va
  barcha backfill self-hosted gateway'da. Ikki tomonlama fallback.
- TTS keshi: `CachedTextToSpeechService` + Redis/in-memory ikki qavat. Butun `SynthesizedSpeech`
  (audio + vizemalar + so'z taymingi) round-trip qiladi. Kesh hit'i `text_to_speech_cached`
  kategoriyasida nol xarajat bilan yoziladi, ya'ni panelda ko'rinadi.
- `IVoicedTextToSpeechService` qo'shildi — aksent tutorlari endi concrete tipni tekshirmaydi,
  shuning uchun dekorator ularni jimgina default ovozga tushirmaydi.
- VAD padding qisqartirildi (720→400 ms pre-roll, 1200→900 ms redemption) — har turn'ga
  ~2 soniya ortiqcha hisoblangan audio qo'shilardi.

**Bosqich 2 — kvota**

- `IMeteredAllowanceStore` (kasrli kunlik hisoblagich). `RedisVoiceLiveSessionStore` shu store
  ustiga qayta yozildi, ya'ni bitta implementatsiya qoldi.
- Kun chegarasi birlashtirildi: `UsagePeriodKey` orqali hamma joyda UTC+5 (ilgari Voice Live
  UTC'da, entitlement UTC+5'da edi).
- Kunlik speaking daqiqa kvotasi: bepul 5, Premium 20. Transkripsiyadan **oldin** tekshiriladi,
  qabul qilingandan **keyin** yoziladi (rad etilgan klip foydalanuvchi hisobidan yechilmaydi).
  402 + `speaking_minutes_exhausted` → frontend paywall'ni ochadi; qolgan daqiqa UI'da ko'rinadi.
- Yangi bepul limitlar: `AssistantQuestion` 10/kun, `PronunciationDrill` 3/kun,
  `WritingAssessment` 3/oy → 1/kun, `SpeakingSession` 1 → 5/kun (endi asosiy nazorat daqiqada).
- **AI yordamchi endi cheklangan** — ilgari `/api/assistant/*` da hech qanday gate yo'q edi,
  holbuki bu 2 pass × 1400 token bilan ilovadagi eng qimmat chaqiruv.
- Per-learner kunlik USD chegarasi (`AiAdmission__*DailyBudgetPerLearnerUsd`), **har ikkala tier**
  uchun va global byudjetdan oldin. Ilgari bitta foydalanuvchi $25 ni tugatib, qolgan barcha bepul
  foydalanuvchilarni kun oxirigacha bloklashi mumkin edi.

**Bosqich 3 — Whisper sidecar**

- `services/speech-stt/` (FastAPI + faster-whisper `base.en`, `image-moderation` naqshi bo'yicha).
- `WhisperSttService` + `TieredSpeechToTextService`: bepul → self-hosted, Premium → Azure,
  sidecar yiqilsa Azure'ga fallback (log bilan, jim emas).
- `DependencyInjection.cs` dagi qattiq downcast tuzatildi — dekorator endi `InvalidCastException`
  bermaydi.
- Compose servisi + `.env.prod.example` + CI image build. **`WHISPER_ENABLED=false`** —
  VPS'da kechikish o'lchanmaguncha yoqilmasin (`base.en` 2 yadroda ~0.3–0.6× realtime).

**Bosqich 4 — narx va monetizatsiyaga tayyorgarlik**

- Narxlar: 99 000 / 249 000 / 449 000 / 799 000 so'm. `PricePerMonthUzs` va `SavePercent`
  hisoblab chiqariladi, qo'lda yozilmaydi.
- `GET /api/subscription/plans` — narxning yagona manbasi, `paymentsEnabled` bilan.
  Frontend'dagi to'rtta qo'lda yozilgan nusxa bitta `content/pricing.ts` ga birlashtirildi va
  `pricing.test.ts` uni C# konstantalariga bog'ladi.
- `LandingPage` dagi qattiq "tez orada" holati olib tashlandi — endi server bayrog'iga bog'liq.
- `/pricing` sahifasi: haqiqiy narxlar + waitlist formasi (SEO maqolasi pastda saqlandi).
- Waitlist: `PremiumWaitlistEntry` domain + migratsiya + `POST /api/subscription/waitlist`
  (anonim, idempotent, normallashtirilgan kontakt bo'yicha unique). O'zbek telefon raqamlari
  qanday yozilishidan qat'i nazar qabul qilinadi.
- Trial 30 → 7 kun. Mavjud akkauntlar tegilmaydi (muddat ro'yxatdan o'tishda yoziladi).
  `LegacyAccessOptions` — to'lov o'chiq bo'lganda eski foydalanuvchilar to'liq huquqda qoladi,
  Click ulangan kuni shart o'zini o'chiradi.
- `TrialExpiryJob` (Hangfire, kuniga 11:00 Toshkent) + o'zbekcha shablonlar. To'lov o'chiq
  bo'lsa waitlist matni, yoqilgan bo'lsa obuna matni yuboriladi.

**Bosqich 5 — qisman**

- Speaking feedback shablonlari 23 → 50. Takrorlanuvchi javoblar uchun variant tizimi
  (`pron.mispronunciation.1..4`), tanlov **deterministik** — bir so'z doim bir xil matn oladi.
- 12 ta yangi fonema maslahati (`ŋ`, `ə`, `ʌ`, `z`, `ʃ`, `tʃ`, `dʒ`, `uː`/`ʊ`, `h`, `l`) —
  o'zbek tilida so'zlashuvchilar eng ko'p adashadigan tovushlar endi o'z maslahatiga ega.
- `PointsService` dagi o'lik `IProAccessService` chaqiruvi olib tashlandi (har XP berishda
  ortiqcha 3 ta DB o'qish).
- `data/curriculum/topics.yml` o'chirildi — bu curriculum emas, xato commit qilingan Playwright
  accessibility snapshot edi.

### Testlar

Backend: Domain 567, Application 738, Integration 691, Architecture 3 — 1 999 test, barchasi yashil.
Frontend: 725 test / 136 fayl. Build 0 xato, 0 ogohlantirish. `docker compose config` yashil.

### P1 ma'lumot yaxlitligi (2026-08-16, tugadi)

`docs/audits/critical-flow-risk-report.md` dagi ochiq P1 oltita o'quv submit oqimi uchun yopildi.

**Muammo.** Har bir handler o'quvchi profilini va topic completion yozuvini **alohida** commit
qilardi, keyin Redis'dagi mukofotni ishga tushirardi. Ikki commit orasidagi xatolik profilni
yangilangan, ballni yo'qolgan holda qoldirardi; mukofotdagi xatolik esa allaqachon saqlangan
natija uchun HTTP 500 qaytarardi va o'quvchi tushgan ishni qayta yuborardi.

**Yechim.**

- `ITopicCompletionStore` va `ITopicSpeakingProgressStore` ga `TrackAsync` (faqat stage) qo'shildi;
  `SaveAsync` (stage + commit) o'zgarmadi. Default interface implementatsiyasi `SaveAsync` ga
  delegat qiladi, shuning uchun in-memory adapterlar va ~15 ta bitta-agregat chaqiruvchi tegilmadi.
- Reading, Listening, Grammar, Writing va Speaking endi hamma narsani stage qilib **bitta**
  `CommitAsync` chiqaradi. Speaking turn butun turn uchun bir marta commit qiladi — gapirilgan vaqt
  endi u qozongan modul ballisiz hisoblanmaydi.
- Mukofot, analitika hodisasi, SRS enrollment va practice-word yig'ish
  `LearningRewards.AwardBestEffortAsync` orqali o'tadi: idempotent, halokatli emas, cancellation
  yutilmaydi.
- Writing'ning kvota yechimi ataylab commitdan **oldin** qoladi — u AI chaqiruvi uchun to'laydi, u
  esa allaqachon sodir bo'lgan va pul turgan.

**Regressiya gate'lari.** Har oqim uchun handler testlari (bir marta commit, mukofot xatosi,
cancellation), `tests/Integration.Tests/Vocabulary/TopicCompletionPersistenceTests.cs` (real
PostgreSQL: commitgacha hech narsa ko'rinmaydi, keyin hammasi), va
`tests/Integration.Tests/LearningRewardFailureFlowTests.cs` (mukofot store yiqilganda HTTP 200 va
ball saqlangan).

Auditda hali ochiq: placement/level-exit finalizatsiya va subscription confirm/discount redemption.

### Ochiq qolgan ishlar

1. **11-qoida: AI yordamchining erkin o'zbekchasi.** Eng ko'p uchraydigan javob turlari uchun
   shablonlashtirilgan skelet kerak (LLM faqat slotlarni to'ldiradi). Bu arxitektura o'zgarishi,
   alohida reja bilan.

### Kurrikulum backfill

Prod'da **to'liq bajarilgan** - 2026-08-16 da serverda tekshirildi:

| Nima | Holat |
|---|---|
| VocabularyTopics | 300/300 to'ldirilgan, hammasi `ContentVersion = 4` (joriy), 0 ta eskirgan |
| Mavzu so'zlari | 300 mavzu, 6 000 so'z (aynan 20/mavzu) |
| GrammarLessons / ReadingPassages / WritingTasks | har biri 300, `Status = 1` (Filled) |
| ListeningExercises | 312 (300 mavzuga bog'langan + 12 ta mustaqil seed kontenti) |
| To'rt skillning birortasi yetishmayotgan mavzu | **0** |
| Generatsiya run | 1 ta, 2026-07-30, `edubase` / `gpt-5.6-sol`, published |

Ya'ni dars ochilganda tirik LLM chaqiruvi ketmaydi va hech kim `Pending` dars ko'rmaydi.
Kontent `gpt-5.6-sol` orqali yaratilgan (Bosqich 1.2 dan oldin) - bu sarflangan pul, qayta
ishlatilmaydi; keyingi backfill arzon modelga boradi.

Diqqat: `VocabularyTopic.CurrentContentVersion` (hozir 4) oshirilsa,
`NeedsContentRefresh => !IsFilled || ContentVersion < CurrentContentVersion` bo'yicha **barcha 300 ta
vocabulary mavzusi eskirgan deb belgilanadi** va birinchi ochilishda yoki keyingi backfillda qayta
generatsiya qilinadi. Ya'ni bitta konstantani oshirish 300 ta LLM chaqiruvini keltirib chiqaradi -
buni bilib qiling va backfillni oldindan ishga tushiring, tirik trafikda emas. Qolgan to'rt skill
(grammar/listening/reading/writing) faqat `IsFilled` ga qaraydi, shuning uchun ular o'z-o'zidan
eskirmaydi.

### Deploydan oldin diqqat qilinadigan narsalar

- `WHISPER_ENABLED` — VPS sig'imi tekshirilmaguncha `false`. Whisper ~2 yadro talab qiladi,
  VPS'da allaqachon 2× Hermes (1.5 CPU har biri) ishlaydi.
- `LegacyAccess__GrantedForAccountsCreatedBefore` — mavjud 108 foydalanuvchini saqlab qolish
  uchun sana qo'yilishi kerak (masalan deploy sanasi).
- `WritingAssessment` oylik→kunlik o'tgani uchun eski `M:` Redis kalitlari yetim qoladi va
  40 kunlik TTL bilan yo'qoladi — deploy kunida hamma yangi allowance oladi.
- Voice Live kunlik daqiqa limiti endi UTC+5 kunida — hisoblagichlar bir marta nolga tushadi
  (2 kunlik TTL o'zini tuzatadi).

### Energiya balansi — 76–79 (2026-09-14)

- Energiya endi 5 birlikdan iborat va har **36 daqiqada +1** tiklanadi; 0 → 5 to'liq
  tiklanishi 3 soat. To'liq balansda `nextRefillAt` va `fullRefillAt` `null`, ya'ni vaqt
  bank qilinmaydi.
- `LearnerEnergySpends` (learner/action/reference) sarf daftari qo'shildi. Video YouTube ID,
  Speaking esa oldindan yaratilgan session ID bilan idempotent sarflanadi; natija
  `None | Consumed | AlreadyStarted | Insufficient` orqali qaytadi.
- Vocabulary topic ochish energiya sarflamaydi. Video katalog/deep-link/playlist epizodi va
  Speaking conversation/roleplay boshlanishi sarf gate'idan o'tadi. Empty Speaking boshlashi
  HTTP 409 `energy_exhausted` qaytaradi.
- Headerlardagi ⚡ chiplar va bloklangan amal `EnergyModal`ni ochadi. Modal 0–5 holatlari,
  1-second backend timestamp countdownlari, 520px WEB va 342px MOBILE markaziy yorug' kartasi
  bilan berildi; 5/5 taymerni umuman rejalashtirmaydi.
- Tekshirildi: `dotnet build --no-restore` (0 xato/0 warning), Energy Domain 11/11,
  Energy + Speaking Application 15/15, `npx tsc -b --pretty false`, scoped ESLint va
  EnergyModal + VideoPlayer frontend 17/17.
