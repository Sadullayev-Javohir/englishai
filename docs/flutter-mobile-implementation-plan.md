# EnglishAI.uz Flutter migration — 30 implementation phases

Complete these phases in order. Each phase depends on the preceding phase and its passing tests. Follow `docs/development-guide.md` and `docs/flutter-mobile-master-plan.md`.

## Implementation and acceptance requirements

- Run repository commands from the checkout root.
- First read `docs/development-guide.md`, `docs/flutter-mobile-master-plan.md`, current `git status --short`, and the exact affected code. Do not rely on assumptions.
- Preserve unrelated dirty work. Never reset, clean, stash, overwrite, stage, or commit unrelated files. Use path-scoped changes only.
- Do not add root-level files/folders. Flutter belongs in `apps/mobile`; backend code stays in existing `src`, `apps/api`, `apps/worker`, `tests`, and `ops` boundaries.
- Do not remove Capacitor until phase 30 and only if every gate is satisfied.
- Flutter must contain no `/video`, `/pricing`, `/admin`, payment, subscription, Play Billing, Apple IAP, or external payment-link surface.
- Never put secrets, credentials, service accounts, signing keys, tokens, generated output, PIDs, browser snapshots, or local caches in Git.
- Backend remains source of truth for XP, energy, score, entitlement, progress, and content.
- Add/update focused tests with each change. Backend work runs focused .NET tests; Flutter work runs format/analyze/tests/builds relevant to the phase.
- If a required credential, Apple signing identity, physical device, or external console setting is unavailable, finish all local work, write the exact blocker and manual step, and do not claim that external verification passed.
- Record changed files, migrations/configuration changes, validation commands and outcomes, and unresolved blockers for each phase.

---

## Phase 01 — Baseline inventory and executable migration ledger

Audit the current repository against `docs/flutter-mobile-master-plan.md`. Inspect current web routes/API clients, Capacitor Android/iOS/plugins/scripts, auth/session model, `GoogleSubject`, entitlement implementation, preferences, notification jobs/device tokens, widgets, account deletion, deep links, analytics, and CI/deploy files. Create `docs/flutter-mobile-gap-analysis.md` with: current implementation evidence by path, target state, gap, dependency, risk, migration/rollback, owner type, verification command, and which later phase closes it. Include an endpoint/feature matrix for all requested mobile features and explicitly enumerate every current video/admin/pricing/payment reference that must never enter Flutter. Do not implement product code in this phase. Validate every path named in the document exists. Show clean path-scoped diff.

## Phase 02 — Scaffold `apps/mobile` and pin the toolchain

Create the Flutter application at `apps/mobile` with package/bundle ID `uz.englishai.app`, display name `EnglishAI.uz`, Android min API 24, compile/target API 36, and iOS deployment target 15.0. Build the full requested folder structure, minimal `main.dart`/`bootstrap.dart`/app shell, dev/staging/prod public configuration, pinned Flutter/dependencies, analysis rules, codegen scripts, and `.gitignore` additions for Flutter/generated/signing artifacts. Add Riverpod, GoRouter, Dio, secure storage, Drift, SharedPreferences, Freezed/json serialization, FCM, Google sign-in, Apple sign-in, and test dependencies only at compatible pinned versions. Do not configure real credentials. Add a smoke widget test. Run Flutter doctor evidence, pub get, code generation, format, analyze, tests, Android debug build if SDK is available, and iOS simulator build only if macOS/Xcode is available. Report exact environment blockers.

## Phase 03 — Architecture guardrails and app bootstrap

Implement typed app configuration, bootstrap error handling, provider containers/overrides, lifecycle hooks, logging redaction, environment validation, and stable app initialization. Add import-boundary tests/lints enforcing feature `data/domain/presentation` separation and preventing cross-feature internals. Add a release assertion that production cannot use local/staging HTTP URLs. Add a secret/log redaction test. No feature UI beyond a diagnostic-safe bootstrap screen. Run codegen, format, analyze, and tests.

## Phase 04 — Design-token extraction and native component foundation

Inventory current web semantic tokens and reusable visual contracts without changing `apps/web`. Implement Flutter semantic tokens, light/dark themes, typography, spacing, radii, elevation, motion, and platform adaptations. Build accessible primitives for buttons, cards, text fields, app bars, sheets, dialogs, loading, empty, error, offline, progress, and AI Report action. Add component/golden tests at compact phone, tablet, light/dark, and 200% text scale. Do not copy desktop layouts or use WebView. Document token provenance in `apps/mobile/README.md`.

## Phase 05 — Closed GoRouter allowlist and forbidden-surface guard

Implement the typed route registry and GoRouter shell for only approved mobile routes. Add safe auth redirects and `/not-found` behavior. Explicitly block `/video`, `/pricing`, `/admin`, payment/checkout/subscription links, malformed links, and unknown destinations. Add a repository test/script that scans all `apps/mobile` source, route data, nav, search, notifications, analytics, widgets, links, remote config defaults, fixtures, and store metadata for forbidden surfaces, while excluding test assertions/documentation as appropriate. Prove blocked links reach `/home` or safe not-found. Do not modify web `/video` or backend video APIs.

## Phase 06 — External identity domain model and additive migration

Add `UserExternalIdentity` in Domain, ports/use cases in Application, EF implementation/configuration in Infrastructure, DbSet, and an additive migration. Fields and unique/index constraints must match the master plan. Retain `UserAccount.GoogleSubject` for compatibility. Add domain, application, architecture, migration, and integration tests. Ensure Domain has no new dependencies. Do not alter login behavior yet.

## Phase 07 — Idempotent Google identity backfill and compatibility read path

Implement an idempotent backfill that creates one Google external identity for every existing account, with duplicate/orphan detection and operational metrics/logging that never exposes provider tokens. Update Google account lookup to prefer external identity and use a narrowly documented temporary `GoogleSubject` fallback. Add concurrency and migration tests, rollback instructions, and a verification query/script under existing `ops` boundaries. Do not remove `GoogleSubject`.

## Phase 08 — Mobile session aggregate, persistence, and secure token primitives

Implement a mobile session/family model and ports: device-scoped session, opaque refresh-token hash, created/last-used/expiry, replacement chain, revocation reason/time, and minimal device metadata. Generate at least 256 random bits for refresh tokens; store no raw refresh token. Implement constant-time validation where applicable, atomic rotation transaction boundaries, family-reuse revocation, cleanup job/service, EF migration, and tests for hashing, expiry, rotation, race/concurrency, replay, and revocation. Keep existing web cookie auth working.

## Phase 09 — Google mobile auth endpoints

Implement `POST /api/mobile/auth/google`, `POST /api/mobile/auth/refresh`, `POST /api/mobile/auth/logout`, and `GET /api/mobile/auth/session`. Validate Google ID tokens server-side using the configured mobile audiences; issue short access JWTs and rotating refresh tokens; resolve ownership only from authenticated claims/session. Add request validation, sanitized device metadata, rate limits, no-store headers, safe audit events, and OpenAPI contract tests. Preserve `/api/auth/google` web behavior. Add integration tests for success, invalid audience/signature/expiry, refresh rotation, replay family revocation, concurrent refresh, logout, expired/revoked session, and isolation between devices.

## Phase 10 — Apple identity validation and safe account linking

Implement Apple token/authorization-code validation through an Application port and Infrastructure adapter. Validate issuer, audience, signature/JWKS, expiry, nonce/state, subject, and provider-verified email. Add `POST /api/mobile/auth/apple`. Implement explicit safe linking: never link by email alone; require an authenticated account plus provider proof or a reauthentication flow; return `account_link_required` for ambiguity. Persist first-login Apple email/name metadata and support private relay/credential revocation state. Add tests for first/subsequent login, missing email, private relay, wrong audience/nonce, duplicate subject, ambiguous email, explicit linking, takeover prevention, and disabled/missing Apple config.

## Phase 11 — Flutter secure authentication client

Implement Flutter Google auth on Android and Google + Apple buttons on iOS. Create auth repository/use cases/controllers, secure token storage, Dio bearer interceptor, one-flight refresh, one retry maximum, logout, session restoration, and complete local user-data clearing on revoked/expired/reused refresh. Never log credentials. Handle cancellation, offline, provider errors, `account_link_required`, and account switching. Add unit/provider/widget/integration tests with fake providers and HTTP adapters. No real credentials in source.

## Phase 12 — Account deletion and all-session revocation

Integrate mobile session revocation into existing account deletion. Add/adjust a mobile-safe deletion API/flow if needed without weakening the existing endpoint. Ensure deletion cannot be queued offline, requires deliberate confirmation and suitable reauthentication, revokes every device session, disassociates push tokens, and causes Flutter to clear secure storage, Drift, SharedPreferences user state, offline queue, and widget cache. Add easy-to-find Profile/Settings UI and a configurable public web deletion URL. Test partial failures, retry/pending UI, already-deleted account, all-session revocation, and no cross-user deletion.

## Phase 13 — Entitlement override with rollback

Implement `Features:AllLearnersEntitled` in the single backend entitlement decision path. `true` allows all learners while preserving usage/cost telemetry; `false` preserves previous behavior. Add configuration validation, focused tests for both modes, integration coverage across representative gated features, operational metrics/alarms documentation, and rollback instructions. Flutter must consume normal feature APIs and must not contain payment/pricing/subscription clients or UI.

## Phase 14 — Notification preference model and scheduling semantics

Extend `UserPreferences`, DTOs, commands, endpoints, persistence, and migrations with push, morning/evening enabled flags/times, and IANA time zone. Defaults: 09:00 and 20:00, `Asia/Tashkent` only as initial fallback. Validate `HH:mm` and zones. Update scheduling to handle DST/zone changes, disabled reminders, deduplication, retries, and account deletion. Preserve existing preferences. Add domain/application/integration/time-zone tests and migration defaults.

## Phase 15 — Flutter profile, settings, permissions, and system photo picker

Implement profile/settings using existing backend contracts plus new reminder preferences and account deletion. Add contextual notification permission rationale and point-of-use microphone permission service. Use system photo picker for avatar; request no broad photo/storage permission. Add exact Uzbek microphone copy from the master plan and clear denied/permanently-denied settings guidance. Add tests for preference save rollback, permission states, large text, screen reader labels, and deletion discoverability. Verify Android/iOS manifests contain no forbidden permissions.

## Phase 16 — Device push registration and allowlisted notification routing

Implement installation-scoped FCM/APNs registration, token rotation, logout/deletion cleanup, foreground/background handling, and versioned typed notification payloads. Flutter parser must accept only allowlisted destinations and route blocked/unknown/video/admin/pricing/payment payloads to `/home`. Configure Android channels and iOS capabilities without real secrets. Add backend and Flutter tests for duplicate tokens, account switch, denied permission, token refresh, malformed payload, killed-app launch, and safe fallback. Do not enable crash reporting.

## Phase 17 — Widget snapshot backend

Implement `GET /api/mobile/widgets/snapshot` with schema version 1, generated time, daily plan, streak, skill progress, rank, and league. Resolve user from auth claims, not request IDs. Return only widget-safe data, add no-store/private caching semantics as appropriate, bounded response size, and stable empty states. Add Application use case/port, Infrastructure queries, endpoint, authorization tests, schema-contract tests, query-performance checks, and no-video/no-secret assertions.

## Phase 18 — Shared widget cache plus Android AppWidget and iOS WidgetKit

Implement atomic sanitized snapshot caching from Flutter into platform shared storage and native Android/iOS widgets. Widgets never hold tokens or call authenticated APIs directly. Support last-good/stale/empty/logged-out/account-changed states and clear/reload on logout/deletion. Implement only allowlisted deep links: home, progress, leaderboard. Test schema mismatch, malformed cache, resize, dark mode, large text, offline, account switch, and all widget taps. If macOS/iOS signing is unavailable, complete source/tests and document the exact external validation blocker.

## Phase 19 — Drift database and offline idempotent mutation queue

Implement Drift schema/migrations and the queue only for lesson progress, completion, analytics event, and preference update. Include all required fields/versioning, user isolation, bounded exponential backoff with jitter, terminal/dead-letter states, ordering where needed, lifecycle/network triggers, and logout/account-switch quarantine/clear behavior. Explicitly reject AI, Speaking live, audio streaming, account deletion, auth, entitlement, and payment operations. Add deterministic clock/network tests and migration tests.

## Phase 20 — Backend idempotency middleware/store

Implement durable `Idempotency-Key` handling for the exact replay-safe mobile mutations. Scope by authenticated user and operation type; store payload hash and response/result; reject key reuse with a different payload; handle concurrent duplicates atomically; set retention/cleanup; never apply to streaming, AI generation, auth, deletion, or payment. Add integration/load/concurrency tests for duplicates, crash/retry boundaries, 4xx, 429, 5xx, and expiration. Wire Flutter queue request mapping and test end-to-end replay.

## Phase 21 — Onboarding, home shell, and capability-safe navigation

Implement native onboarding and home using current backend state and web product semantics, not copied desktop layout. Include username/demographics/learning-goal flows as required by current APIs, loading/empty/error/offline states, bottom navigation/drawer appropriate to phone/tablet, and accessibility. Home cards must include only approved mobile features. Add widget/golden/controller/integration tests and prove no video/admin/pricing/payment references.

## Phase 22 — Levels, progress, and leaderboard

Implement levels map/exit-test entry where supported, progress dashboards, and leaderboard using existing backend contracts. Keep score/XP/energy calculations server-owned. Add pagination/refresh, empty/error/offline cache behavior, account isolation, accessibility, and widget deep-link landing behavior. Add DTO mapper, repository, use-case, controller, widget/golden, and API-contract tests.

## Phase 23 — Vocabulary and grammar native flows

Implement vocabulary and grammar catalogs, detail/guidance, exercises/quizzes, completion/progress mutation, and safe offline replay only for allowed progress/completion actions. Do not queue generated passage/AI actions. Preserve backend grading and scoring. Add pronunciation entry permission gating where used. Test long content, Uzbek/English text, retries/idempotency, resume state, accessibility, compact phone/tablet, and no blocked-route leakage.

## Phase 24 — Reading and writing native flows with AI Report

Implement reading catalog/detail/quiz and writing catalog/editor/submission/result using existing APIs. All AI-generated/explained/evaluated results must expose in-app Report. Add report domain/API contract if not already implemented, with authenticated rate-limited moderation submission and privacy-safe metadata. Never claim guaranteed scores/results. Test long text, keyboard/autosave, network interruption, report categories/submission/failure, accessibility, and offline boundary (submissions/generation are not blindly queued).

## Phase 25 — Listening native flow and audio playback lifecycle

Implement listening catalog/detail/audio/answers/quiz with a maintained audio player compatible with API 24/iOS 15. Handle focus, interruptions, route changes, background/foreground, headphone disconnect, buffering, retry, cleanup, and accessibility. Do not request microphone for listening playback. Do not persist protected audio beyond allowed caching rules. Add unit/widget/integration tests and device-smoke instructions.

## Phase 26 — Speaking recorder, pronunciation, and live-session safety

Implement Speaking catalogs/roleplay/free-talk/pronunciation flows using native recorder/player and point-of-use microphone permission. Never queue live sessions, AI turns, or audio streaming. Handle denied/permanently denied permission, interruptions, cancellation, app background, route close, network loss, duplicate submit prevention, and complete recorder/socket/player disposal. Add AI Report to generated/evaluated output. Test with fakes plus API 24/API 36 and iOS 15+ device matrix where available; clearly separate simulator/unit proof from physical microphone proof.

## Phase 27 — Books, notifications inbox, support, search, and final feature parity

Implement books catalog/detail/sections/quizzes, notifications inbox/read state, learner support conversation/attachments, and mobile search limited to approved features. Reuse system pickers and safe attachment constraints. Search must never return video/admin/pricing/payment. Add report/support handoff where appropriate, pagination, caching, offline/error states, accessibility, and contract tests. Update the gap ledger with remaining parity gaps.

## Phase 28 — Privacy manifests, data inventory, store assets, and policy checklist

Create production-ready privacy/store documentation under existing `docs`/`ops` boundaries: SDK/data inventory, Google Data Safety worksheet, Apple App Privacy worksheet, `PrivacyInfo.xcprivacy`, required-reason API audit, third-party SDK manifest/signature audit, privacy/terms/deletion URLs, AI report/reviewer instructions, 13+ target-audience checklist, Uzbekistan/US storefront plan, permission inventory, and honest listing copy. Ensure screenshots/descriptions contain no video, pricing, admin, payment, child-directed claims, human-teacher replacement, or guaranteed outcomes. Do not invent declarations: derive them from actual code/SDK behavior and mark external-console steps precisely.

## Phase 29 — Full CI, security, performance, accessibility, and release-candidate verification

Add/update CI for Flutter format/analyze/codegen-drift, unit/widget/golden/integration tests, Android debug/release/AAB, backend focused/integration/architecture tests, forbidden-surface scan, dependency/license/vulnerability/secret scans, and artifact metadata tied to exact SHA. Execute the complete locally available matrix. Run Android API 24/API 36 tests, iOS simulator/archive/device tests where available, auth replay/concurrency, offline queue, push/deep links/widgets, account deletion, accessibility/text scale/dark mode, startup/jank/network/battery checks, and token-log audit. Produce `docs/flutter-mobile-release-readiness.md` with evidence and explicit unverified items. Do not claim signed/device/store readiness without evidence.

## Phase 30 — Guarded production rollout and separate Capacitor retirement

First audit phase 29 evidence. If any required Flutter Android/iOS, auth/session, push, links, widgets, offline, deletion, privacy/store, signing, device, rollback, or owner-approval gate is missing, do not remove Capacitor; only write the exact remaining checklist and stop blocked. If all gates are proven, create a separate path-scoped Capacitor retirement change: remove only Capacitor dependencies/scripts/config/native folders from `apps/web`, retain React web and all web routes including `/video`, `/pricing`, and `/admin`, update CI/docs/deploy references, and remove obsolete bridge code only after proving no web dependency. Run web typecheck/lint/test/build, Flutter full checks/builds, backend focused tests, exact-SHA artifact verification, production readiness (`/health/ready`), allowed/blocked deep-link smoke, and rollback drill. Report exact artifact versions, SHA, store rollout stages, and production evidence. Never combine unrelated dirty files.
