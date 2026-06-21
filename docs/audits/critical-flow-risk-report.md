# Critical Flow Risk Audit

Updated: 2026-08-06

## P0 — Library quiz completion

- **Symptom:** finishing a library section can show the generic retry message.
- **Risk path:** `SubmitBookQuizCommandHandler` persists BookProgress, LearnerProfile activity/errors, Redis daily progress, referral qualification, points, and leaderboard effects in one request.
- **Confirmed design defect:** BookProgress and LearnerProfile previously called separate `SaveChangesAsync` operations. A later failure could return HTTP 500 after earlier progress had already committed.
- **Remediation:** defer both EF aggregate mutations and commit them together; treat daily reward/gamification as an idempotent best-effort side effect after the core result is durable.
- **Regression gates:** application handler tests, real PostgreSQL persistence test, frontend retry/correlation test, and middleware error-contract test.
- **Production evidence:** pending because the configured `englishai` SSH alias could not resolve from the implementation environment. Production verification must collect `/api/books/quiz` status, correlation ID, backend exception, and persisted synthetic learner progress without modifying real learner data.

## P1 — Similar multi-persistence submit flows

**Resolved 2026-08-16** for the six learning-submit flows.

- **Confirmed defect:** each handler committed the learner profile and the topic completion record
  separately, then ran a Redis-backed reward. A failure between the two commits left the profile
  updated and the score lost; a failure in the reward returned HTTP 500 for a result that was already
  durable, and the learner re-submitted work that had landed.
- **Remediation:**
  - `ITopicCompletionStore` and `ITopicSpeakingProgressStore` gained `TrackAsync` (stage) alongside
    `SaveAsync` (stage + commit), matching `ILearnerProfileRepository.TrackAsync` and
    `IBookProgressStore`. Default interface implementations delegate to `SaveAsync`, so the
    immediately-consistent in-memory adapters and the ~15 single-aggregate callers are unchanged.
  - Reading, Listening, Grammar, Writing and Speaking now stage every aggregate and issue exactly one
    `CommitAsync`. The speaking turn commits once for the whole turn, so spoken-time progress can no
    longer be credited without the module score it earned.
  - Rewards, analytics events, SRS enrollment and practice-word harvesting run through
    `LearningRewards.AwardBestEffortAsync`: idempotent, non-fatal, and cancellation is re-thrown
    rather than swallowed.
  - Writing's quota charge deliberately stays BEFORE the commit - it pays for the AI call, which has
    already happened and already cost money.
- **Regression gates:** per-flow handler tests (commit-once, reward-failure, cancellation),
  `tests/Integration.Tests/Vocabulary/TopicCompletionPersistenceTests.cs` (real PostgreSQL: nothing
  visible before the commit, everything after), and
  `tests/Integration.Tests/LearningRewardFailureFlowTests.cs` (HTTP 200 with the score persisted
  while the reward store is down).

Still open, same review required:

- Placement and level-exit finalization.
- Subscription confirmation and discount redemption.

Acceptance for each remaining flow: core learner result is durable exactly once, auxiliary rewards cannot turn a successful learning result into a generic 500, retries are idempotent, and a real HTTP/persistence test covers the boundary.

## P2 — Observability and user recovery

- Standard error responses now include a machine `code` and `correlationId`.
- Library submit failures retain answers, show a specific Uzbek recovery message, expose the support code, and allow resubmission without restarting the quiz.
- Remaining product-wide work: replace generic catches on critical finalize/submit routes with typed messages and add correlation IDs to the admin support workflow.

## Audit Command

Run `ops/scripts/audit/critical-flow-risk-audit.sh` locally and in CI. It verifies that the P0 regression coverage and report remain present; behavioral correctness is enforced by the referenced test suites.
