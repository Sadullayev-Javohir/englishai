# EnglishAI Android va Play Market Release Rejasi

## Maqsad

EnglishAI web ilovasidan `uz.englishai.app` Android ilovasini tayyorlash, Play Market uchun signed AAB va qurilma sinovi uchun signed APK chiqarish. Android buildda YouTube/video va payment interfeyslari bo‘lmaydi; Google-only auth, native Uzbek notification, to‘rtta widget, inactivity launcher iconlari va retention funksiyalari bo‘ladi.

## Ishlash modeli

- 15 ta mustaqil Hermes prompt, 3 ta wave, har wave’da ko‘pi bilan 5 parallel sessiya.
- Har sessiya alohida Git worktree va branchda ishlaydi.
- Agent faqat o‘z promptidagi write-scope’ni o‘zgartiradi.
- Integrator commitlarni navbat bilan cherry-pick qiladi va har qadamda focused test ishlatadi.
- Generated APK/AAB, keystore, signing parollari va service-account secretlari Gitga kiritilmaydi.

## Wave 1 — Foundation

1. Baseline va eski Android artifact audit.
2. Capacitor Android shellni qayta generatsiya qilish.
3. Native capability qatlamini yaratish; video/payment route’larini bloklash.
4. Android navigatsiya va home UI’dan video/payment entry-pointlarini olib tashlash.
5. Google-only native authenticationni tiklash va testlash.

## Wave 2 — Native funksiyalar

6. FCM backend payload va deep-link contractini mustahkamlash.
7. `englishai_learning_v2` notification channel, Uzbek ayol ovozi va logo iconlarini qo‘shish.
8. Bugungi 6 skill va Streak/Comeback widgetlari.
9. Leaderboard va Progress tahlili widgetlari.
10. Normal, 24 soatdan keyin qizil, 72 soatdan keyin kulrang launcher icon aliaslari.

## Wave 3 — Retention va release

11. Androidda paymentni yopib, learning Premium accessni vaqtincha ochish.
12. Daily quest va weekly challenge.
13. Streak rescue, comeback reward va personal reminder.
14. Branding, versioning, signed APK/AAB release automation.
15. Play Console readiness, privacy/data-safety va real-device QA.

## Qabul mezonlari

- Androidda `/video*`, `/pricing`, checkout va payment UI ochilmaydi; web xatti-harakati saqlanadi.
- Login/register faqat Google orqali ishlaydi.
- Notification matni va ovozi: “EnglishAI.uzda ingliz tilini o‘rganing”.
- Status-barda monochrome logo, notification panelida brand logo ko‘rinadi.
- To‘rtta widget offline snapshot va to‘g‘ri deep-link bilan ishlaydi.
- Launcher icon 24 soatda qizil, 72 soatda kulrang, app ochilganda normal bo‘ladi.
- Android learning kontenti payment ulanmaguncha ochiq, checkout esa yopiq bo‘ladi.
- Play uchun signed AAB, sinov uchun signed APK package/version/signature tekshiruvidan o‘tadi.
