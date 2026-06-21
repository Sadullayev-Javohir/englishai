# EnglishAI.uz Flutter Android/iOS — master implementation plan

Status: implementation blueprint
Prepared: 2026-08-13
Target app: `apps/mobile`
Package / bundle ID: `uz.englishai.app`

## 0. Document contract

This document is the source of truth for replacing the current Capacitor mobile shell with a native Flutter application while preserving the React web application.

Non-negotiable outcomes:

- Flutter is a new executable app under `apps/mobile`; no new repository root folder is introduced.
- React web remains in `apps/web` and continues to own all web routes, including `/video`, `/pricing`, and `/admin`.
- Capacitor stays in `apps/web` until Flutter passes the release gates in section 18. It is removed only in a separate cleanup change.
- Flutter V1 contains no payment, subscription, pricing, external payment link, Play Billing, or Apple IAP surface.
- Flutter contains no admin surface and no video feature in any form.
- Backend remains the source of truth for XP, energy, scores, entitlements, progress, and learning content.
- Android supports API 24+, compiles and targets API 36. iOS deployment target is 15.0+.
- Target audience is 13+ and the product is not positioned as a child-directed app.
- Initial storefront availability is Uzbekistan and United States.

## 1. Verified policy baseline

Recheck store policy and SDK requirements immediately before submission.

- Starting 2026-08-31, Google Play requires new apps and updates to target Android 16 / API 36 or later.
- Google Play Data Safety answers must match the app and every included SDK's actual data behavior.
- If users can create an account, Google Play requires deletion inside the app and through a web resource.
- AI-generating apps need an in-app reporting/flagging mechanism that does not require leaving the app.
- Apple requires apps that support account creation to let users initiate account deletion in-app.
- Because Google is a primary third-party login on iOS, Sign in with Apple must be offered unless a current App Review exception applies.
- `PrivacyInfo.xcprivacy` must truthfully declare collected data and required-reason API usage. Included third-party SDK manifests/signatures must be audited.
- Check Flutter's supported-platform matrix before pinning the release toolchain. This project intentionally sets iOS 15.0 even if Flutter supports older versions.

Official references are in section 20.

## 2. Repository baseline and migration constraints

- Web lives in `apps/web`; existing Capacitor Android code is under `apps/web/android`.
- Current bridge uses `/api/auth/mobile-google*` and `/api/auth/google`, but has no durable rotating mobile refresh session.
- `UserAccount` currently owns a required unique `GoogleSubject`; external identities do not exist yet.
- Account deletion exists at `DELETE /api/auth/account`; mobile session revocation must join the same deletion flow.
- Preferences contain `PushNotifications`, but not morning/evening times or time zone.
- Existing Android widgets/push are migration inputs, not Flutter-verified deliverables.
- Preserve unrelated dirty work. Use path-scoped edits and staging; never reset unrelated files.

## 3. Target structure and dependency rules

Create the requested tree under `apps/mobile`: `android`, `ios`, `assets`, `integration_test`, `test`, and `lib`. Under `lib`, keep `app`, `core`, `design_system`, `features`, and `shared`. Every feature uses `data`, `domain`, and `presentation` layers with the supplied subfolders.

Rules:

- Presentation depends on domain abstractions; data implements domain repositories.
- Features may use `core` and `shared`, but cannot import another feature's data/presentation internals.
- No backend business rule is reimplemented in Dart.
- Add architecture/static-import tests for these boundaries.

## 4. Technology decisions

- Riverpod for state and DI; GoRouter for navigation; Dio for HTTP.
- `flutter_secure_storage` for tokens; Drift/SQLite for structured local data; SharedPreferences only for non-sensitive settings.
- Freezed plus `json_serializable` for immutable models.
- Firebase Cloud Messaging for push after privacy declarations and consent UX are ready.
- Google login on Android; Google and Apple login on iOS.
- Select maintained audio player/recorder packages only after an API 24/iOS 15 spike.
- Implement Android AppWidget and iOS WidgetKit with small native adapters; extensions read only sanitized cached snapshots.
- Crash reporting stays disabled until an explicit privacy/SDK approval enables it.
- Pin a stable Flutter SDK and dependency versions; commit lockfiles and avoid unconstrained `any` dependencies.

## 5. Configuration and platform baseline

Use `dev`, `staging`, and `prod` compile-time environments. `AppConfig` may contain only public configuration: API URL, environment, OAuth public client IDs, link hosts, presentation flags, and log level. Never embed keys, signing material, service accounts, or tokens.

Build invariants:

- package/bundle ID `uz.englishai.app` and display name `EnglishAI.uz`;
- Android `minSdk=24`, `compileSdk=36`, `targetSdk=36`;
- iOS deployment target `15.0` for Runner and extensions;
- production release rejects non-production API URLs;
- Firebase/Apple/Google environment files are separate and secrets remain outside source control.

## 6. Router allowlist and total `/video` exclusion

Use one typed route/destination registry. Allowed top-level routes are `/home`, `/onboarding`, `/levels`, approved children of `/vocabulary`, `/grammar`, `/reading`, `/writing`, `/listening`, `/speaking`, `/books`, plus `/progress`, `/leaderboard`, `/profile`, `/notifications`, `/support`, and `/not-found`.

Block `/video`, `/pricing`, `/admin`, payment/checkout/subscription destinations, malformed links, and unknown destinations. Authenticated fallback is `/home` with optional “Sahifa mavjud emas”; unauthenticated fallback goes through login then `/home`.

A forbidden-surface test must scan router, navigation, home cards, search, notification destinations, analytics names, widgets, App/Universal Links, remote config, API clients, store text/screenshots, reviewer data, debug routes, and integration tests. Web `/video` and backend `/api/video` remain unchanged.

## 7. Mobile authentication and session security

Endpoints:

```http
POST /api/mobile/auth/google
POST /api/mobile/auth/apple
POST /api/mobile/auth/refresh
POST /api/mobile/auth/logout
GET  /api/mobile/auth/session
```

Login/refresh returns `user`, `accessToken`, `accessTokenExpiresAt`, `refreshToken`, and `refreshTokenExpiresAt`.

Server rules:

- Access JWT lifetime: recommended 10–15 minutes.
- Refresh token: at least 256 random bits, opaque, never logged; store only a keyed/strong hash.
- Rotate atomically on each refresh; reused consumed token revokes the session family.
- One installation/device equals one independently revocable session.
- Logout revokes current device; account deletion revokes all sessions.
- Store family/replacement/revocation/expiry/timestamps and minimal device metadata.
- Rate-limit and safely audit login, refresh, reuse, and logout.

Flutter rules:

- Store tokens only through Keychain/Keystore-backed secure storage.
- Serialize refresh to one in-flight operation; retry a request at most once.
- On expiry/revocation/reuse, clear tokens, authenticated jobs, user database/queue/cache, and return to login.
- Never log headers, provider credentials, refresh tokens, or full auth payloads.
- Widget extensions never read tokens.

## 8. External identity migration and linking

Add `UserExternalIdentity`: `Id`, `UserAccountId`, `Provider`, `ProviderSubject`, `VerifiedEmail`, `CreatedAt`, `LastUsedAt`. Enforce unique `(Provider, ProviderSubject)` and index `UserAccountId`.

Safe sequence:

1. Add table while retaining `GoogleSubject`.
2. Idempotently backfill Google identities.
3. Resolve Google auth via external identity with temporary compatibility fallback.
4. Add Apple validation and explicit linking.
5. Verify duplicates/orphans/missing identities.
6. Retire `GoogleSubject` only in a later reversible migration.

Validate provider signature, issuer, audience, expiry, nonce/state, subject, and verified-email claim server-side. Never link by email alone. Link only after authenticated proof or explicit reauthentication. In ambiguous cases return `account_link_required`. Persist Apple's first-login email/name because later responses may omit them; support private relay and credential revocation.

## 9. Free V1 entitlement mode

Add `Features:AllLearnersEntitled`.

- `true`: all learners pass premium feature gates, while usage/cost telemetry continues.
- `false`: existing entitlement rules apply unchanged.
- Keep payment endpoints for web compatibility; Flutter has no payment DTO, repository, route, CTA, analytics name, or link.
- Enforce in backend entitlement service and test both values and rollback.

## 10. Notification preferences

Contract:

```json
{
  "pushNotifications": true,
  "morningReminderEnabled": true,
  "morningReminderTime": "09:00",
  "eveningReminderEnabled": true,
  "eveningReminderTime": "20:00",
  "timeZone": "Asia/Tashkent"
}
```

Validate `HH:mm` and IANA zones; support DST/zone changes, independent enablement, retries, and deduplication. FCM/APNs tokens are installation-scoped and removed on logout/deletion. Ask permission contextually, not as an onboarding blocker. Payloads contain typed/versioned destinations, never arbitrary URLs; blocked/unknown destinations go to `/home`.

## 11. Widget snapshot

Add `GET /api/mobile/widgets/snapshot` with `schemaVersion`, `generatedAt`, `dailyPlan`, `streak`, `skillProgress`, and leaderboard rank/league.

Return only home/lock-screen-safe data. Flutter atomically writes a sanitized shared cache. Widgets render last success plus stale/empty states. Unsupported/malformed snapshots never erase last valid data. Logout/deletion clears cache and timelines. Allowlisted links: daily plan/streak → home, skills → progress, leaderboard → leaderboard. Test offline, stale, account switch, large text, dark mode, and resize.

## 12. Offline mutation queue

Queue only lesson progress, completion, analytics event, and preference update. Never queue AI generation, live Speaking, audio streaming, account deletion, auth, payment, or entitlement changes.

Record `operationId`, `idempotencyKey`, type, timestamps, attempts, next attempt, payload version, payload, and user ID. Backend deduplicates by user + operation type + key and rejects same key with a different payload hash. Use bounded exponential backoff with jitter. Preserve ordering where needed. Logout/account switch clears or quarantines the prior user's work. Do not endlessly retry validation/auth failures.

## 13. Design system and accessibility

Use web brand tokens, not WebView or copied desktop DOM. Extract semantic colors, type, spacing, radius, elevation, motion, icons, and states. Build native buttons, cards, fields, sheets, dialogs, bars, navigation, progress, reporting, loading, empty, error, and offline components.

Use platform navigation/gesture/text-selection/permission/photo-picker conventions. Do not request broad photo/storage access. Verify touch targets, semantics, focus, screen readers, reduced motion, contrast, keyboard/safe area, 200% text scale, and light/dark mode.

Device matrix: compact API 24 Android, API 36 Android, small iPhone/iOS 15, current iPhone/iOS, tablets, slow network, and offline.

## 14. Feature order

1. Shell: auth, onboarding, home, profile, support, navigation, design system/errors.
2. Read-oriented: levels, progress, leaderboard, books and skill catalogs.
3. Interactive: vocabulary, grammar, reading, writing, listening runners.
4. Speaking/audio with interruption and resource cleanup.
5. Push, offline queue, widgets, and deep links.
6. Store/privacy hardening and later Capacitor retirement.

For every feature, inventory current API/web behavior, define DTO/domain/error/offline contracts, implement native interaction, add tests, exercise staging APIs, and prove no blocked-surface leakage.

## 15. AI reporting, support, and deletion

Every user-visible AI result has an accessible in-app Report action. Reports include content reference/type, category, optional comment, and privacy-safe moderation metadata; endpoints are authenticated and rate-limited. Do not claim guaranteed outcomes or replacement of human teachers.

Profile/settings exposes easy-to-find account deletion. It uses deliberate confirmation and suitable reauthentication, explains consequences, handles pending server state, and never queues offline. Keep a public web deletion URL. Success clears tokens, user Drift/SharedPreferences data, queued work, push association, widget snapshot, and timelines.

## 16. Permissions and exact purpose copy

Android V1 should normally contain only `INTERNET`, point-of-use `RECORD_AUDIO`, API 33+ `POST_NOTIFICATIONS`, and any audio capability proven necessary by the chosen library. Do not add SMS, call log, contacts, location, broad storage/media, camera, Bluetooth, advertising ID, or package-query permissions without a reviewed feature/policy change.

iOS Uzbek microphone copy:

```text
EnglishAI.uz Speaking mashqlarida talaffuzingizni yozib olish va baholash uchun mikrofondan foydalanadi. Mikrofon faqat mashqni o‘zingiz boshlaganingizda ishlatiladi.
```

Notification rationale shown in-app:

```text
EnglishAI.uz o‘qish rejangiz, kundalik eslatmalar va muhim hisob xabarlari haqida bildirishnoma yuboradi. Eslatmalarni Sozlamalarda o‘zgartirishingiz yoki o‘chirishingiz mumkin.
```

Do not add photo-library purpose strings if the system picker requires no broad permission. Add purpose strings only for resources actually accessed.

## 17. Privacy and store inventory

Maintain an SDK/data inventory: vendor/version, data, purpose, linkage/tracking, retention, transmission, permissions, privacy manifest/signature, consent/opt-out, and Data Safety/App Privacy mapping.

Audit account/profile, learning progress, user text/audio, diagnostics, notification token, device/app metadata, support content, reports, and any location/storefront inference.

Required artifacts: privacy/terms URLs, public deletion page, Data Safety worksheet, App Privacy worksheet, `PrivacyInfo.xcprivacy` audit, AI/reporting explanation, reviewer notes/test data, honest store text/screenshots, and consistent 13+ content-rating declarations.

## 18. Tests, release gates, and Capacitor removal

Backend gates: focused Domain/Application tests; integration tests for all mobile endpoints, ownership, concurrency, migrations, rate limits, and rollback; architecture tests.

Flutter gates:

- `dart format --set-exit-if-changed .`, `flutter analyze`, unit/controller/repository/mapper/router/golden/integration tests;
- Android debug/release builds and API 24/API 36 device tests;
- iOS simulator tests plus signed archive/device smoke on iOS 15+;
- dependency/license/vulnerability/secret scans;
- forbidden video/admin/pricing/payment scan.

Manual matrix covers fresh install/upgrade, auth/refresh/reuse/logout/deletion, every feature state, microphone denied/allowed/interrupted, push/token rotation/links, widget states, allowed/blocked links, accessibility, localization, safe areas, startup/jank/network/battery, and absence of secret logs.

Remove Capacitor only after Flutter Android+iOS acceptance, production-like auth/push/links/widgets/offline/deletion, policy artifact completion, staged rollout approval, rollback plan, and explicit owner approval. Remove Capacitor dependencies/scripts/native folders in a separate scoped change; re-run web typecheck/lint/test/build and confirm React web routes remain unchanged.

## 19. Definition of done

Done means signed installable Android and iOS artifacts from exact source SHA, passing CI, device matrix, backend migration/rollback proof, privacy/store checklists, reviewer flow, production readiness, and no blocked surfaces. Source code alone is not completion.

## 20. Official references checked on 2026-08-13

- Google Play target API: https://support.google.com/googleplay/android-developer/answer/11926878
- Google Play Data Safety: https://support.google.com/googleplay/android-developer/answer/10787469
- Google Play account deletion: https://support.google.com/googleplay/android-developer/answer/13327111
- Google Play target audience: https://support.google.com/googleplay/android-developer/answer/9893335
- Google Play AI-generated content: https://support.google.com/googleplay/android-developer/answer/13985936
- Google Play photo/video permissions: https://support.google.com/googleplay/android-developer/answer/14115180
- Apple App Review Guidelines: https://developer.apple.com/app-store/review/guidelines/
- Apple account deletion: https://developer.apple.com/support/offering-account-deletion-in-your-app/
- Apple App Privacy: https://developer.apple.com/app-store/app-privacy-details/
- Apple privacy manifests: https://developer.apple.com/documentation/bundleresources/privacy-manifest-files
- Apple third-party SDK requirements: https://developer.apple.com/support/third-party-SDK-requirements/
- Flutter supported platforms: https://docs.flutter.dev/reference/supported-platforms
