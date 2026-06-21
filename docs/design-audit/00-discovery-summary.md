# EnglishAI monochrome redesign — 1-qism: discovery

Sana: 2026-08-06
Holat: `DISCOVERY COMPLETE` — production UI o‘zgartirilmagan.

## Maqsad

Amaldagi rangli va bir-biriga zid qatlamlarni olib tashlab, keyinchalik boshqariladigan rang qo‘shishga tayyor bo‘lgan, avval oq-qora/neytral, ko‘zni charchatmaydigan Light/Dark/System dizayn poydevorini yaratish.

## O‘lchangan frontend hajmi

- Router path patternlari: **94**
- Dinamik/wildcard route: **27**
- Catch-all route: **1**
- `<Navigate>` alias/redirectlar: **6**
- `*Page.tsx` modullari: **63**
- Router import qilgan page modullari: **61**
- Orphan page modullari: **2** (`AdminVideoPage.tsx`, `NotificationsPage.tsx`)
- Page-local CSS fayllari: **52**
- CSS import occurrence: **70**, unique import: **66**
- Tekshirilgan TS/TSX/CSS source fayllari: **394**

## Dizayn drift o‘lchovlari (raw)

- `--ea-*` token reference: **5,925**
- `--duo-*` reference: **248**
- `--clay-*` reference: **125**
- Material compatibility `--c-*` reference: **97**
- Hex literal: **538**
- `!important`: **1,696**
- Inline `style={{...}}`: **126**
- Arbitrary Tailwind px utility: **1,432**
- Dark selector/utility reference: **21**

Bu raw sonlar avtomatik o‘chirish ro‘yxati emas. Chart/SVG/illustration va canonical token declarationlar keyingi migratsiya trackerida istisno sifatida tasniflanadi.

## Ildiz sabablar

### P0 — Theme runtime Light rejimga majburan qulflangan

- `apps/web/index.html` har load’da `englishai-theme=light` yozadi, `.dark`ni olib tashlaydi va `colorScheme=light` qiladi.
- `apps/web/src/lib/theme.ts` `dark` va `system` so‘rovlarini ham `light`ga normalizatsiya qiladi.
- `ThemeToggle` mavjud, lekin amalda rejimni o‘zgartira olmaydi.
- `index.css`da Dark tokenlar mavjud bo‘lsa-da, runtime ularga yetib bormaydi.

Natija: foydalanuvchi tanlovi, OS System rejimi va CSS dark palette bir-biriga ulanmagan.

### P0 — Bir vaqtda to‘rtta vizual til yuklanadi

`apps/web/src/main.tsx` quyidagilarni global tartibda yuklaydi:

1. `index.css` — Lime + Ink + Material alias + Duo + global animation;
2. `styles/clay.css` — Claymorphism;
3. `styles/reference-lesson-theme.css`;
4. `styles/reference-catalog-theme.css`;
5. `styles/reference-final-theme.css`.

Reference stylesheetlar keng `:where(...)` selector va `!important` orqali page-local stillarni qayta bo‘yaydi. Bu cascade’ni komponent source’dan tushunib bo‘lmaydigan holatga olib kelgan.

### P0 — Global fonning o‘zi rangli

`index.css` `html/body/#root` uchun purple/orange/blue/green radial gradientlarni birga ishlatadi. Shuning uchun hatto neytral component ham rangli muhit ichida qoladi.

### P1 — Saturated legacy palette productionda mavjud

- Duo ranglari standalone va Dark’da re-theme qilinmaydi.
- Clay tintlar Duo ranglariga bog‘langan.
- `surface-3d-*` oilasi 10 xil rangli face/lip beradi.
- Global animationlarda yashil/sariq rangli glow va shimmer literal qiymatlar bor.

### P1 — Cascade qarzi katta

1,696 ta `!important`, 52 ta page-local CSS va 3 ta reference override fayli bir elementni bir necha qatlamdan boshqaradi. Monochrome redesign faqat root tokenlarni almashtirish bilan ishonchli tugamaydi; override qatlamlari bosqichma-bosqich scope qilinishi yoki olib tashlanishi kerak.

## Shell va product zonalari

1. **Public** — Landing, SEO, learn/content, pricing/product/contact, login, 404.
2. **Onboarding** — username, goal, welcome, assessment, placement/result, exit-test/result.
3. **Learner standard** — Home shell, levels, cataloglar, progress, leaderboard, profile.
4. **Immersive lesson** — speaking session, pronunciation, video play/quiz, book section, vocabulary/grammar/listening/reading/writing lessonlari.
5. **Admin/operator** — `/admin/**`, alohida sidebar/topbar/mobile nav.
6. **Native** — Capacitor status bar, safe-area, keyboard, widget va capability cheklovlari.

## KEEP / MERGE / REMOVE-SCOPE qarori

### KEEP

- Semantic `--ea-*` nomlash g‘oyasi, ammo qiymatlar yangi monochrome spec bilan almashtiriladi.
- 421/701/901/1181 responsive bandlari.
- 44px minimum target va safe-area tokenlari.
- `Manrope` body + `Space Grotesk` display juftligi (vizual approvalgacha provisional).
- Public/onboarding/learner/lesson/admin shell chegaralari.
- `prefers-reduced-motion` kontrakti.

### MERGE

- `--c-*` compatibility aliaslari `--ea-*` semantic tokenlariga bir tomonlama ulanadi.
- Card/Button/Input/Dialog/Sheet oilalari bitta foundation contractga yig‘iladi.
- Light/Dark/System runtime va native status-bar mapping bitta theme service orqali ishlaydi.
- Page-local ranglar avval semantic surface/text/border/status rollariga ko‘chiriladi.

### REMOVE yoki SCOPE

- Global Lime + Ink rangli fon gradientlari.
- Global Clay importi; faqat aniq preview/legacy scope bo‘lsa qolishi mumkin.
- Uchta `reference-*-theme.css` global override qatlami.
- Productiondagi dekorativ `duo-*` ranglar va `surface-3d-*` rang sikllari; faqat keyinchalik tasdiqlangan game feedback scope’da qayta kiritiladi.
- Light-only bootstrap va Light-only theme controller.
- Orphan `AdminVideoPage.tsx` va `NotificationsPage.tsx`: keyingi route reconciliation’da merge/delete qarori.

## Monochrome yo‘nalish uchun cheklovlar

- “Oq-qora” degani sof `#fff` va `#000`ni katta yuzalarda ishlatish emas. Ko‘zni charchatmaslik uchun iliq/neytral off-white va yumshoq near-black ishlatiladi.
- Light va Dark faqat rang inverti bo‘lmaydi: surface hierarchy, border, shadow, overlay, media fallback va form focus alohida tekshiriladi.
- Status ranglari birinchi previewda ham monochrome bo‘ladi; error/success faqat icon, copy, border pattern va contrast orqali farqlanadi. Rang keyingi approval bosqichida controlled semantic accent sifatida qo‘shiladi.
- Production page migratsiyasi yozma `DESIGN.md` va Design Lab visual approval’dan oldin boshlanmaydi.

## Tegilmagan production fayllar

Discovery read-only bo‘ldi. `apps/web/src/**`, Android, API, Domain, Application, Infrastructure va mavjud dirty worktree fayllariga o‘zgartirish kiritilmadi.
