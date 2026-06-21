# EnglishAI Android Play Market Checklist

## Artifactlar

- `uz.englishai.app`, `versionName 1.0.0`, `versionCode 1`.
- Play Console uchun signed `app-release.aab`.
- Real qurilma testi uchun signed `app-release.apk`.
- Upload keystore va parollar repository tashqarisida saqlanadi.

## Store listing

- Ilova nomi: EnglishAI.uz.
- Asosiy til: Uzbek.
- Qisqa tavsif: AI yordamida 6 ko‘nikma bo‘yicha ingliz tilini o‘rganing.
- Telefon screenshotlari: Login, Home 6 skill, Speaking, Progress, Leaderboard va widgetlar.
- Feature graphic va 512×512 Play icon.
- Privacy policy URL va support email.

## Data Safety

- Google account identity, learning progress, microphone audio va notification tokenlari ko‘rsatiladi.
- Microphone faqat Speaking va placement speaking mashqlari uchun ishlatiladi.
- Audio yuborilishi, saqlanishi va o‘chirish siyosati privacy policy bilan mos bo‘lishi kerak.
- Push notification va analytics maqsadlari deklaratsiya qilinadi.

## Device acceptance

- Android 7/API 24, Android 13 notification permission va target SDK qurilmasida install/open.
- Google-only sign-in, cold-start callback va logout/login.
- Androidda Video, YouTube, Pricing va Checkout yo‘qligi.
- 4 widgetning render, refresh, offline snapshot va deep-link sinovi.
- Native notification sound, status-bar icon va `/home` deep-link sinovi.
- Launcher alias: normal, 24 soat qizil, 72 soat kulrang, reopen normal.
- Speaking microphone permission va recording smoke test.

## Rollout

- Avval Play Internal testing track.
- Crash/ANR, login failure, push registration va widget deep-link monitoring.
- Internal testdan keyin closed testing, keyin staged production rollout.
