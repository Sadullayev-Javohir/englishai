# EnglishAI: manbalar va moliyaviy farazlar

Taqdimot sanasi: 2026-yil 10-sentabr. Bu hujjat 12 slaydga qo‘shimcha,
PDF tarkibidagi 13-sahifa emas. Og‘zaki nutq va savol-javob izohlari
`EnglishAI_StartupGarage_Slayd_Matnlari_UZ.md` hujjatida berilgan.

## Raqamlarning maqomi

| Tur | Misol | Talqin |
| --- | --- | --- |
| Tarixiy ichki ko‘rsatkich | 113 hisob, 86 profil | 2026-08-17 hisoboti, joriy faol yoki pullik auditoriya emas |
| Mahsulot tarifi | 99 000 so‘m / 30 kun | Koddagi narx, o‘lchangan ARPU emas |
| Rasmiy tashqi ma’lumot | 9 630 556 yoshlar | 2025-01-01 dagi 14-30 yosh aholisi, ingliz tiliga talab emas |
| Pilot maqsadi | 100 ishtirokchi, 40% / 25% / 15% | Hali erishilmagan maqsad |
| Moliyaviy taxmin | 89 000 ARPU, 180 000 CAC | Haqiqiy to‘lov va xarajatlardan o‘lchanmagan |
| Moliyalashtirish so‘rovi | 40 000 USD | Asoschining taklifi, tasdiqlangan investitsiya emas |

## Tashqi manbalar

### S1. Yoshlar soni

Milliy statistika qo‘mitasi, "O‘zbekistonda yoshlar soni qancha?", 2025-08-04.
2025-yil 1-yanvarda 14-30 yoshdagi 9 630 556 kishi.
2026-yil 10-sentabrda sahifadagi mazmun qayta tekshirildi.

https://stat.uz/uz/matbuot-markazi/qo-mita-yangiliklar/63005-o-zbekistonda-yoshlar-soni-qancha

### S2. Valyuta kursi

Markaziy bank. USD/UZS, 2026-09-09: 1 USD = 11 813,21 so‘m.
API javobidagi sana va kurs 2026-09-10 kuni tekshirildi.
Tarixiy kurs hisob modeli bilan bir xil bo‘lishi uchun saqlandi.

https://cbu.uz/uz/arkhiv-kursov-valyut/json/USD/2026-09-09/

### S3. Duolingo

"Video Call lets you have real life conversations with Lily", 2024-09-24.
AI suhbat imkoniyati bo‘yicha rasmiy manba. Mazmuni 2026-09-10 kuni tekshirildi.

https://blog.duolingo.com/video-call/

### S4. Speak

Rasmiy mahsulot sahifasi. Suhbat mashqlari va AI bilan moslashtirilgan
o‘quv dasturi. Mazmuni 2026-09-10 kuni tekshirildi.

https://www.speak.com/

### S5. Anki

Rasmiy mahsulot sahifasi. Kartochka va takrorlashni rejalashtirish.
Mazmuni 2026-09-10 kuni tekshirildi.

https://apps.ankiweb.net/

## Ichki manbalar

### I1. Haqiqiy logo va aloqa

- Joriy komponent: [ParrotLogo.tsx](../../apps/web/src/components/ParrotLogo.tsx).
- Asl oq belgi: [englishai-mark-white.png](../../apps/web/public/assets/englishai-mark-white.png).
- Muqovadagi mavjud ilova logosi: [app-icon-20260815.png](../../apps/web/public/assets/app-icon-20260815.png).
- Logo testi: [ParrotLogo.test.tsx](../../apps/web/src/components/ParrotLogo.test.tsx).
- Aloqa: [officialContacts.ts](../../apps/web/src/content/officialContacts.ts).

`parrot-mascot.png` dekorativ maskot bo‘lgani uchun logo o‘rnida ishlatilmadi.
Eski `data/brand-logo/README.md`dagi oq plitkali tasvir joriy komponentga
mos kelmagani uchun yangi logotip deb talqin qilinmadi.

### I2. O‘quv mexanizmi

- [TopicCompletionRecord.cs](../../src/Domain/Vocabulary/TopicCompletionRecord.cs):
  oltita modul va mavzuni o‘zlashtirish mezonlari.
- [ReviewSchedule.cs](../../src/Domain/Vocabulary/ReviewSchedule.cs):
  dastlabki 3/7/30 kunlik takrorlash.
- [VocabularyTopicCatalog.cs](../../src/Infrastructure/Vocabulary/VocabularyTopicCatalog.cs):
  A1-C2 mavzu katalogi. Kontent sifati va samaradorlikning mustaqil isboti emas.

### I3. Mahsulot tasviri

[shot-speaking.png](../../apps/web/public/assets/landing/shot-speaking.png).
Mavjud mahsulot ekran tasviri, jonli xizmat holati yoki o‘quv natijasi emas.

### I4. Tarixiy foydalanish

[PROGRESS.md](../../PROGRESS.md), 2026-08-17, 11:51 UTC:
113 hisob, 86 profil, 27 daraja belgilanmagan hisob. Mustaqil audit emas.
Joriy faol yoki pullik auditoriya uchun yangi dalil zarur.

### I5. Tarif va nutq limitlari

- [SubscriptionPricing.cs](../../src/Domain/Subscription/SubscriptionPricing.cs):
  30/90/180/365 kun uchun 99/249/449/799 ming so‘m.
- [SpeakingMinuteAllowance.cs](../../src/Domain/Subscription/SpeakingMinuteAllowance.cs):
  bepul 5, Premium 20 daqiqa o‘rganuvchi nutqi / kun.

### I6. Moliyaviy model

[financial-model.json](financial-model.json) va
[financial_model.py](../../ops/tools/startupgarage-pitch/financial_model.py).
Asoschining 40 000 USD so‘rovi va uch ssenariyli 36 oylik reja
qisqartirishda o‘zgartirilmagan. Bazaviy davr: 2026-10 dan 2029-09 gacha.

### I7. Jamoa

Asoschi taqdim etgan 2026-09-09 ma’lumotlari:

- Javohir Sadullayev, Founder & CEO, `javohirsadullayev.jpeg`.
- Diyorbek Pirimqulov, Mobile Developer, `diyorbekpirimqulov.jpg`.
- Abdukarim Qarshiyev, Backend Developer, `abdukarimqarshiyev.jpg`.

Suratlar manbalardan mutanosib kesilgan. Sun’iy yuz, retush,
tasdiqlanmagan tajriba yoki biografiya qo‘shilmagan.

## Asosiy hisob-kitoblar

### Obuna iqtisodiyoti

- ARPU: 89 000 so‘m / oy, faraz.
- Pullik tannarx: 18 000 ovozli AI + 4 000 boshqa AI + 2 000 infratuzilma
  + 3% × 89 000 komissiya = 26 670 so‘m.
- Bepul baza xarajati: `max(300, o‘rtacha pullik baza × 8) × 600`.
- Kengaygan bazada bepul xizmat ulushi: 8 × 600 = 4 800 so‘m.
  O‘rtacha pullik baza 37,5 dan kichik bo‘lsa, bu ulush yuqoriroq.
- Hissa: 89 000 - 26 670 - 4 800 = 57 530 so‘m, 64,6%.
- CAC: 180 000 so‘m, faraz. Oddiy qoplanish: 180 000 / 57 530 = 3,1 oy.
- Ketish: oyiga 6%, faraz. Oddiy LTV: 57 530 / 6% = 958 833 so‘m.
  LTV/CAC = 5,3. Diskont va mijozning real umri hisobga olinmagan.

### Bozor

- TAM: 9 630 556 × 80% × 25% = 1 926 111,2 kishi.
- SAM: TAM × 90% × 20% = 346 700,016 kishi.
- Yillik SAM qiymati: SAM × 89 000 × 12 = 370 275 617 088 so‘m.
- To‘rtta foizning hammasi taxmin, rasmiy statistika emas.
- 14-30 yosh bozor modeli va 18+ talaba piloti bir xil auditoriya emas.

### Moliyalashtirish

- 40 000 USD × 11 813,21 = 472 528 400 so‘m.
- 18 oylik sof operatsion ehtiyoj: 334,1 mln so‘m.
- Bir martalik tayyorgarlik: 30,0 mln so‘m.
- 18-oy bazaviy pul zaxirasi: 108,4 mln so‘m.
- Taqsimot: 70,71% + 6,35% + 22,94% = 100%.
- Tushum o‘sha oyda undirilishi faraz. 18 oy faqat investitsiyaga tayanmaydi.

## Qarorga ta’sir qiladigan cheklovlar

1. Pullik auditoriya, MRR, CAC, ketish va o‘quv samaradorligining tekshirilgan
   joriy ko‘rsatkichlari mavjud hujjatlarda keltirilmagan.
2. Pilot, kanallar, til markazlari va foizlar reja, tasdiqlangan hamkorlik emas.
3. Reja kichik asoschi jamoasi va part-time ijrochilarga tayanadi.
   Maoshlar, bandlik va ulushlar kelishilgan deb ko‘rsatilmagan.
4. AI ovozli xarajati 120 daqiqalik o‘rtacha foydalanishga tayanadi.
   Kunlik 20 daqiqalik limit to‘liq ishlatilsa tannarx keskin oshadi.
5. Bazaviy birinchi musbat oy M18, M19 yana manfiy, uzluksiz musbat natija M20 dan.
   Ehtiyotkor ssenariyda pul M14 da tugaydi.
6. Soliq, refund, qarz, amortizatsiya, inflyatsiya, aylanma kapital va
   yillik oldindan to‘lovlar alohida modellashtirilmagan.
7. 18 oylik qamrov obuna tushumlariga bog‘liq. Past tushumda xarajatni qayta
   rejalash yoki qo‘shimcha moliyalashtirish kerak.
8. Instrument, valuation, ulush va Startup Garage qabul shartlari kelishilgan
   yoki kafolatlangan deb ko‘rsatilmagan.
