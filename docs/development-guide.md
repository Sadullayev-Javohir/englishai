# EnglishAI.uz — ishlab chiqish qo'llanmasi

Ushbu hujjat loyiha arxitekturasi, kod standartlari, testlash, xavfsizlik va
o'zgarishlarni yetkazish talablarini belgilaydi. Raqamlangan bo'limlar koddagi
texnik izohlar uchun barqaror havola hisoblanadi.

---

## 0. O'zgarishdan oldingi tekshiruv

1. Repository rootidagi `README.md` va ushbu qo'llanma bilan tanishish.
2. `git status --short` orqali mavjud o'zgarishlarni tekshirish.
3. Vazifa chegaralarini belgilash va unga aloqasiz o'zgarishlarni saqlash.
4. Katta o'zgarishlar uchun implementatsiya va tekshiruv rejasini yozish.

---

## 1. Loyiha maqsadi (mission)

**Loyiha nomi:** EnglishAI.uz
**Logo/maskot:** To'tiqush - ikki til (qush tili + inson tili) ramzi, ona tildan yangi
tilga o'tish va takrorlash orqali o'rganishni ifodalaydi. Speaking modulida AI tutor
sifatida ham ishlatilishi mumkin (rasm/3D mavjud bo'lsa).

O'zbek tilida so'zlashuvchilar uchun ingliz tilini **haqiqatda gapira oladigan** darajaga
yetkazadigan, ko'p metodli, adaptiv platforma. Yadro: tushunarli kirish (input) + majburiy
ishlab chiqarish (output) + AI fikr-mulohaza, daraja oshgani sayin hamma narsa qiyinlashadi.

**Har bir qaror shu mezon bilan o'lchanadi:** "Bu o'zgarish foydalanuvchini gapirishga
yaqinlashtiradimi?"

---

## 2. Asosiy tamoyillar (har doim amal qil)

1. **Clean Architecture qatlamlarini buzma** - Domain hech narsaga bog'liq emas; Infrastructure
   tashqariga qaramaydi.
2. **CQRS/MediatR** - har bir amal alohida Command yoki Query + Handler.
3. **Har bir o'zgarishdan keyin build + test** - yashil bo'lmaguncha keyingi vazifaga o'tma.
4. **Kichik, atom commitlar** - bitta mantiqiy o'zgarish = bitta commit.
5. **Sirlarni hech qachon kodga yozma** - `appsettings`/`.env`/secret manager (qoida 13).
6. **O'zbek matnni AI erkin yaratmaydi** - faqat tekshirilgan shablonlar (qoida 11).
7. **Xarajat ongli** - arzon LLM + caching default (qoida 10).
8. **Hech qanday placeholder/TODO qoldirma** - funksiya to'liq ishlashi kerak yoki yozilmaydi.
9. **Mavjud konventsiyalarga ergash** - yangi pattern joriy qilishdan oldin mavjudini ishlat.

---

## 3. Texnik stack (canonical - o'zgartirma)

| Qatlam | Texnologiya |
|---|---|
| Til / runtime | C# / .NET 8 (`global.json` bilan qulflangan) |
| Arxitektura | Clean Architecture + CQRS/MediatR |
| ORM / DB | EF Core + PostgreSQL |
| Kesh / leaderboard / streak | Redis (Sorted Sets) |
| Real vaqt | SignalR (WebSocket) |
| Fon ishlari | Hangfire |
| Mapping | Mapster (AutoMapper EMAS) |
| Validatsiya | FluentValidation |
| Auth | Google OAuth 2.0 + JWT (HttpOnly cookie) |
| Speech (STT/TTS/talaffuz) | Azure Speech SDK |
| LLM | API (budjet model default) |
| Test | xUnit + FluentAssertions + Moq/NSubstitute + Testcontainers |

---

## 4. Arxitektura konventsiyalari

**Qatlamlar (loyiha tuzilmasi):**

```
apps/
  api/              # API endpoints, SignalR Hubs, middleware va composition root.
  worker/           # Background worker composition root.
  web/              # React + TypeScript client.
src/
  Domain/           # Entity, ValueObject, Enum, Domain Event. Hech narsaga bog'liq emas.
  Application/      # Command/Query + Handler, Validator, DTO, interfeyslar (ports).
  Infrastructure/   # EF Core, Redis, Azure Speech, LLM, tashqi xizmatlar (adapters).
services/            # Mustaqil transcript va image moderation sidecarlar.
ops/                 # Deploy, maintenance va developer automation.
tests/
  Domain.Tests/
  Application.Tests/
  Integration.Tests/
```

**Qoidalar:**
- Domain → hech kimga bog'liq emas.
- Application → faqat Domain'ga; tashqi xizmatlar interfeys (port) orqali.
- Infrastructure → interfeyslarni implement qiladi.
- API → MediatR orqali Application'ga murojaat; biznes mantiq endpointda EMAS.
- Har bir use-case: `Feature/{Module}/{Action}Command.cs` + `{Action}CommandHandler.cs` +
  `{Action}CommandValidator.cs`.
- `apps/` faqat executable host va UI applicationlarni saqlaydi; hostlar biznes
  mantiq saqlamaydi.
- `Infrastructure` Application portlarini implement qiladi, host-specific kod saqlamaydi.
- `services/` ichidagi servislar mustaqil deploy qilinadi, o'z Docker build contexti
  va health endpointiga ega bo'ladi; backend database'iga to'g'ridan-to'g'ri ulanmaydi.
- `ops/` automation uchun; product source code bu yerga qo'yilmaydi.
- Yangi root-level fayl yoki papka qo'shilmaydi; mavjud repository chegaralari ishlatiladi.

---

## 5. Kod standartlari

- Nullable reference types yoqilgan; warning'larni e'tiborsiz qoldirma.
- Async/await hamma I/O da; `async void` ishlatma (event handler'dan tashqari).
- Har bir public method'ga maqsad aniq nom; magic number'larni const'ga chiqar.
- FluentValidation har bir Command/Query uchun.
- Mapster mapping profillari `Infrastructure`/`Application`da markazlashgan.
- Exception'lar: domain xatolari uchun maxsus exception turlari; global error middleware.
- Logging: structured logging (Serilog yoki built-in), sezgir ma'lumotni log qilma.
- Hech qachon `TaskStatus` kabi BCL nomlari bilan to'qnashma - domain enum'larga prefiks ber
  (masalan `TaskItemStatus`).

---

## 6. Ishlab chiqish va tekshirish tartibi

Har bir vazifa uchun shu tsiklni bajar:

1. **Rejala** - vazifani kichik qadamlarga bo'l (TODO).
2. **Yoz** - bitta qadamni implement qil.
3. **Build** - `dotnet build`. Xato bo'lsa → tuzat → qayta build.
4. **Test** - `dotnet test`. Yiqilsa → 8-qoida bo'yicha tahlil qil.
5. **Verifikatsiya** - qabul mezonlariga (Definition of Done) mos kelishini tekshir.
6. **Commit** - atom commit, aniq xabar.
7. **Hisobot** - `PROGRESS.md` ni yangila.
8. Keyingi qadam.

**Hech qachon** ishlamaydigan yoki test qilinmagan kodni "tugadi" deb belgilama.

---

## 7. Testlash strategiyasi

- **TDD afzal:** murakkab mantiq uchun avval test yoz, keyin kod.
- **Unit testlar:** Domain mantiqi, Handler'lar (tashqi bog'liqliklar mock).
- **Integration testlar:** EF Core (Testcontainers bilan real PostgreSQL), Redis, Hangfire.
- **Tashqi xizmatlar (Azure Speech, LLM):** interfeys orqali mock qil; real API'ni testda chaqirma
  (xarajat + flakiness). Faqat alohida "smoke test"da real chaqiruv (ixtiyoriy, belgilangan).
- **Har bir Command/Query:** kamida happy-path + 1 validatsiya xatosi + 1 chekka holat.
- **Coverage maqsadi:** Domain va Application'da yuqori; muhim biznes mantiq 100% qamralsin.
- **Har commit oldidan barcha testlar yashil** bo'lishi shart.

---

## 8. Xatolarni tahlil qilish va tuzatish

Build yoki test yiqilsa:

1. Xato xabarini to'liq o'qi (faqat birinchi qatorni emas).
2. Asl sababni aniqla (symptom emas).
3. Eng kichik tuzatishni qil.
4. Qayta build + test.
5. Yashil bo'lguncha takrorla - lekin **bir xil tuzatishni 3 martadan ortiq sinama**.
6. Agar 3 urinishdan keyin ham yiqilsa: muammoni `PROGRESS.md` ga yoz, alternativ yondashuvni
   sina yoki (faqat zarur bo'lsa) foydalanuvchidan so'ra.
7. Hech qachon testni **o'chirib** yoki **soxta o'tkazib** muammoni "yashirma".

---

## 9. Sifat darvozalari (Definition of Done)

Bir vazifa faqat shularning hammasi bajarilsa "tugadi":
- [ ] Kod build bo'ladi (0 xato, 0 ogohlantirish).
- [ ] Barcha testlar yashil.
- [ ] Yangi mantiq uchun testlar yozilgan.
- [ ] FluentValidation mavjud (Command/Query uchun).
- [ ] Sirlar kodda yo'q.
- [ ] Xarajat qoidalariga (10) rioya qilingan.
- [ ] O'zbek matn shablon orqali (11).
- [ ] `PROGRESS.md` yangilangan.

---

## 10. AI / Speech integratsiya qoidalari (xarajat nazorati)

- **LLM default:** eng arzon mos model (Gemini Flash / GPT-4.1 mini darajasi). Murakkab
  tahlilgagina qimmatroq model.
- **Prompt caching** yoq (system prompt'lar uchun) - takroriy kirishga 90% chegirma.
- **Batch API** real vaqt kerak bo'lmagan ishlarga (50% chegirma).
- **Talaffuz baholash (Azure):** faqat qisqa maxsus mashqlarga; butun suhbatga EMAS.
  Suhbat STT uchun arzonroq endpoint.
- **TTS keshlash:** umumiy/takroriy iboralar bir marta sintez qilinib saqlanadi.
- **Bepul tier:** har Azure resursdan 5 soat STT + 0.5M TTS bepul - to'liq foydalan.
- Barcha tashqi AI chaqiruvlari interfeys (port) orqali - provayderni almashtirish oson bo'lsin.
- Har chaqiruvda `max_output_tokens` cheklab qo'y (kutilmagan xarajatning oldini ol).

---

## 11. O'zbek tili sifat qoidasi (KRITIK)

- AI o'zbek tilini **erkin yozmaydi**. LLM inglizcha fikrlaydi, natijani **strukturalangan
  JSON (kodlar)** qaytaradi.
- Foydalanuvchiga ko'rsatiladigan har bir o'zbekcha matn - **oldindan yozilgan, tekshirilgan
  shablon** (content store'dan).
- Misol: `{ "error_type": "missing_article" }` → "Artikl ('the') tushib qolgan".
- Barcha tushuntirish, UI matni, fikr-mulohaza, grammatika izohi shablonlarda saqlanadi
  (`Resources/uz/*.json` yoki DB'da).
- Agar dinamik o'zbek matn muqarrar bo'lsa: glossary + few-shot + ikkinchi tekshiruv passi;
  imkon qadar bundan qoch.
- Adaptiv til balansi: A1–A2 ko'p o'zbek; B1–B2 kamayadi; C1+ deyarli ingliz.

---

## 12. Rasmlar strategiyasi (qayerdan olinadi / yaratiladi)

**Qoida:** Hech qachon internetdan tasodifiy/mualliflik huquqi himoyalangan rasm olma.
Faqat litsenziyalangan, ochiq yoki yaratilgan rasm. Atributsiyani saqla. Rasmlarni lokal
kesh/storage'ga yuklab qo'y (har safar tashqaridan tortma).

| Maqsad | Manba | Izoh |
|---|---|---|
| UI ikonkalar | Lucide / Heroicons (ochiq litsenziya) | To'g'ridan-to'g'ri loyihaga |
| Illyustratsiyalar | unDraw, Storyset (bepul) | Atributsiya talab bo'lishi mumkin |
| Lug'at/kontent uchun real foto | Unsplash API yoki Pexels API (bepul tier) | Kalit so'z bo'yicha tortib olinadi |
| Video thumbnaillar | Video provayderdan (masalan YouTube thumbnail) | Asl manbadan |
| Maxsus/abstrakt tushuncha rasmlari | AI image generation API | Faqat zarur bo'lsa (xarajat + izchillik) |

**Tavsiya:** `IImageService` interfeysi qil - manba (Unsplash / AI-gen / lokal) almashtirilsin.
Lug'at kartochkalari uchun rasm-so'z assotsiatsiyasi o'rganishni kuchaytiradi: real foto uchun
Unsplash/Pexels, abstrakt tushuncha uchun AI-generatsiya. Har bir rasmga manba + litsenziya
metama'lumotini saqla.

---

## 13. Xavfsizlik (oldingi muammolarni takrorlama)

- Sirlar: User Secrets (dev), environment variables / secret manager (prod). Kodda/gitda yo'q.
- JWT HttpOnly cookie'da; `SameSite` va `SecurePolicy` to'g'ri (mos kelmaslikka yo'l qo'yma).
- Rate limiting: Nginx/reverse-proxy ortida bo'lsa `UseForwardedHeaders` yoq - aks holda
  IP-asoslangan limit ishlamaydi.
- CORS, CSRF, security headers to'g'ri sozlangan.
- Foydalanuvchi audio/matnini saxlashda maxfiylikni hisobga ol.

---

## 14. Progress va commit konventsiyalari

- `PROGRESS.md`: har vazifadan keyin yangila - bajarilgan, keyingi, ochiq muammolar.
- Commit xabarlari: `feat:`, `fix:`, `test:`, `refactor:`, `chore:` prefiks bilan, qisqa va aniq.
- Katta faza tugaganda: mavjud `docs/` ichida qisqa hisobot - nima qilindi, qanday test qilindi.
- Faqat vazifaga tegishli fayllar path-scoped staging bilan commit qilinadi.
- Generated output, PID, browser snapshot, secret va local cache commit qilinmaydi.
- Path o'zgarsa solution, Compose, CI, deploy testlari va hujjatlar bir xil commit
  seriyasida yangilanadi.
- Backend uchun focused tests; frontend uchun typecheck, lint, test va build bajariladi.

---

## 15. Kelishishni talab qiladigan qarorlar

**Alohida kelishish talab qilinadi:**
- Spec'da yo'q, qaytarib bo'lmaydigan arxitektura qarori kerak bo'lsa.
- Tashqi hisob/kredit/maxfiy ma'lumot (API key, to'lov) kerak bo'lsa.
- Ikki yo'l ham jiddiy va spec aniqlik bermasa.

**Mavjud qoidalar doirasida hal qilinadi:**
- Oddiy implementatsiya tafsilotlari.
- Test yiqilishi (8-qoida bo'yicha tuzat).
- Konvention tanlovi (mavjudga ergash).
- Refactoring va kod sifati.

---

## 16. Muhim komandalar

```bash
dotnet build                      # build
dotnet test                       # barcha testlar
dotnet ef migrations add <Name>   # migratsiya qo'shish
dotnet ef database update         # DB yangilash
dotnet run --project apps/api/Api.csproj      # ishga tushirish
docker compose up -d              # PostgreSQL + Redis (lokal)
```

> PostgreSQL/Redis portlari `docker-compose.yml` da; konfliktdan qochish uchun standart bo'lmagan
> portlarni ishlat (masalan PostgreSQL 5433, Redis 6380) va `appsettings`da mos sozla.

---

## 17. Build tartibi (fazalar - PROJECT-SPEC v2 bilan mos)

1. **Faza 0 - Scaffolding + CEFR Placement Test:** solution, qatlamlar, Docker
   (PostgreSQL+Redis), CI, base test setup, adaptiv daraja aniqlash testi mantig'i.
2. **Faza 1 - Speaking yadro:** STT + talaffuz baholash + viseme servisi + LLM suhbat + TTS + SignalR.
3. **Faza 2 - O'quvchi modeli + analitika:** xato/ball yig'ish, grafiklar, tavsiya,
   ko'nikma balli va daraja o'tish formulasi.
4. **Faza 3 - Lug'at (SRS) + Bildirishnoma:** 3/7/30 kun jadval, Hangfire job, mini-testlar.
5. **Faza 4 - Video/Listening:** YouTube ingestion, CEFR darajalash, interaktiv transkript.
6. **Faza 5 - Majburlash, gamifikatsiya va Obuna/To'lov:** kunlik maqsad, streak,
   gating, Free/Premium tier, Click/Payme integratsiyasi.
7. **Faza 6 - O'qish + Grammatika + Writing:** 5-bosqichli grammatika dars formati,
   4-o'lchovli Writing baholash.
8. **Faza 7 - Retention va o'sish (MVP'dan keyin, ixtiyoriy):** churn signallari,
   win-back ketma-ketligi, feature flag, D1/D7/D30 metrikalar.
9. **Parallel:** O'zbek kontent qatlami (shablon, glossary, xato katalogi).

### 17.1 Viseme (og'iz/yuz animatsiyasi) - qo'shimcha texnik qoidalar
- `Infrastructure/Speech/VisemeService.cs` - Azure TTS dan viseme ID + audio ofset oladi.
- `PhonemeVisualLibrary` - IPA fonema -> SVG ID xaritasi, content store'da, koddan ajratilgan.
- 2D SVG yondashuvi default; 3D/blend-shape keyingi fazaga qoldiriladi.
- Test: viseme ketma-ketligi audio uzunligi bilan mos kelishini tasdiqlovchi unit test.

### 17.2 SRS bildirishnoma - qo'shimcha texnik qoidalar
- `ReviewSchedule` entity: `LearnedAt`, `NextReviewAt`, `ReviewStage` (Day3/Day7/Day30/Mastered), `FailCount`.
- Hangfire recurring job kuniga bir marta `due` yozuvlarni qidiradi.
- Bildirishnoma matni faqat shablon orqali (11-qoida bilan bir xil tamoyil).
- Test: vaqtni mock qilib (`IClock`/`TimeProvider` abstraktsiyasi orqali), 3/7/30 kunlik
  o'tish to'g'ri ishlashini unit test bilan tasdiqlang - real `DateTime.Now` ishlatmang.

### 17.3 Video ingestion - qo'shimcha texnik qoidalar
- Video fayllar hech qachon o'z serveringizga yuklanmaydi/saqlanmaydi - faqat YouTube
  embed + metadata (CEFR daraja, transkript, teglar) DB'da saqlanadi.
- YouTube API kvotasi cheklangan - chaqiruvlarni keshlang, Hangfire orqali fon ishida
  bajaring (real vaqt emas).

Har faza: mustaqil ishlaydi, testlar yashil, `PROGRESS.md` yangilangan, qisqa hisobot yozilgan.

### 17.4 Dizayn importi - qo'shimcha texnik qoidalar

- Dizayn manbai `design-export/` papkasi (`DESIGN.md` Qism 3-4 ga qarang), uch qismdan
  iborat:
  - `design-export/style-guide/EnglishAI-design-system.md` - rasmiy dizayn tizimi
    (rang tokenlari, shrift, komponent qoidalari). **Bu asosiy/ustun manba** - boshqa
    fayllar bilan ziddiyat bo'lsa, shu fayl ustun turadi.
  - `design-export/brand/parrot-mascot.png` va `app-icon.png` - tayyor aktivlar, qayta
    chizilmaydi, to'g'ridan-to'g'ri frontend `assets/`/`public/` papkasiga ko'chiriladi.
  - `design-export/screens/*.html` - 13 ta ekranning rasmiy dizayni (Welcome, Level
    Assessment Intro, Placement Test Question, Home Dashboard, AI Speaking Conversation,
    Pronunciation Detail, Video Lesson Preview, Video Player, Video Lesson Quiz,
    Notifications, Vocabulary Review, Progress Dashboard, Profile & Settings).
- Har faza boshlanishidan oldin (ayniqsa Faza 0 va frontend ishlari boshlanganda):
  1. Avval `.md` style guide faylni o'qing - rang (hex), shrift, masofa, komponent
     qoidalarini chiqarib oling.
  2. Bu qiymatlarni qayta ishlatiladigan dizayn tokenlariga (CSS variables yoki
     Tailwind config) aylantiring - markazlashtirilgan joyda saqlang, har sahifada
     qayta yozmang.
  3. Tegishli `.html` faylni o'qib, shu ekranning komponent tuzilmasini (layout,
     tugmalar, kartalar, navigatsiya) tushunib oling, shunga mos quring.
- **Matn tuzatish (imlo/grammatika):** eksport fayllaridagi o'zbekcha matnlarda
  imlo yoki grammatik xato, yoki notabiiy ibora
  bo'lishi mumkin. Bunday matnlarni **tuzating**, lekin faqat matnni - vizual dizaynni
  (rang, joylashuv, komponent tuzilmasi) o'zgartirmang. Tuzatilgan matn 11-qoida
  bo'yicha kontent qatlamiga (shablon sifatida) qo'yiladi, kodga hardcode qilinmaydi.
- Agar bir xil tushuncha turli ekranlarda turlicha nomlangan bo'lsa (masalan
  "Boshlash" va "Start qilish"), bitta izchil atamaga keltiring va shu atamani butun
  loyihada izchil ishlating.
- Agar matnning **mazmuni** (nafaqat imlosi) noto'g'ri yoki teskari tuyulsa (masalan
  tugma "Tugatish" deb yozilgan, lekin mantiqan "Boshlash" bo'lishi kerak), buni
  o'zingiz tuzatmang - `PROGRESS.md` ga qayd qiling va foydalanuvchidan tasdiq so'rang
  (15-qoida bo'yicha "haqiqiy noaniqlik" holati).

---

## 18. Mutlaq qoidalar (qisqa eslatma)

- ✅ Har o'zgarishdan keyin build + test, yashil bo'lguncha o'zing tuzat.
- ✅ Clean Architecture + CQRS buzilmaydi.
- ✅ O'zbek matn faqat shablon orqali.
- ✅ Arzon LLM + caching default.
- ✅ Litsenziyalangan/yaratilgan rasm, atributsiya saqlanadi.
- ❌ Sir kodda yo'q.
- ❌ Placeholder/TODO/ishlamaydigan kod "tugadi" deb belgilanmaydi.
- ❌ Test o'chirib muammo yashirilmaydi.
