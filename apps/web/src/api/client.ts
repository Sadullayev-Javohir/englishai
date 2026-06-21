// Thin typed fetch wrapper over the .NET minimal API. In dev, Vite proxies /api and
// /hubs to the backend (see vite.config.ts); in production the SPA is served from the
// same origin, so relative URLs work without configuration.
import type {
  AdminAccessDto,
  SupportConversationDto,
  SupportConversationSummaryDto,
  SupportMessageDto,
  AdminUserDetailDto,
  AdminUsersDto,
  AdminVocabularyTopicDto,
  AdminVocabularyTopicUpsertDto,
  AdminVocabularyImageDto,
  AdminCurriculumRunDto,
  AdminCurriculumItemDto,
  AdminBookDto,
  AdminGrammarLessonDto,
  AdminGrammarLessonDetailDto,
  AdminGrammarLessonFullUpdateDto,
  AdminGrammarLessonUpsertDto,
  AdminListeningExerciseDto,
  AdminListeningExerciseDetailDto,
  AdminListeningExerciseFullUpdateDto,
  AdminListeningExerciseUpsertDto,
  AdminReadingPassageDto,
  AdminReadingPassageDetailDto,
  AdminReadingPassageFullUpsertDto,
  AdminReadingPassageUpsertDto,
  AdminWritingTaskDto,
  AdminWritingTaskUpsertDto,
  FounderMetricsDto,
  VariableCostSnapshotDto,
  PublicSocialProofMetricsDto,
  ServerDiagnosticsDto,
  ServerTelemetryHistoryDto,
  ForceDueReviewsResultDto,
  SetUserAdminResultDto,
  AdminBroadcastDto,
  DispatchResultDto,
  AuthenticatedUserDto,
  AuthConfigDto,
  GoogleSignInResponse,
  DeveloperApiKeyDto,
  CreatedDeveloperApiKeyDto,
  BookSummaryDto,
  BookDetailDto,
  BookSectionDto,
  BookQuizAnswer,
  BookQuestionOutcomeDto,
  BookQuizResultDto,
  ChurnAssessmentDto,
  DueReviewDto,
  MandatoryReviewStatusDto,
  GamificationStatusDto,
  LeaderboardDto,
  PointsBalanceDto,
  EnergyDto,
  EnergyAction,
  RedemptionDto,
  GrammarExerciseAnswer,
  GrammarExerciseCheckDto,
  GrammarExerciseResultDto,
  GrammarLessonDto,
  GrammarTopicSummaryDto,
  TopicImageManifestDto,
  TranslationDto,
  AssistantReplyDto,
  AssistantTurnDto,
  UsernameAvailabilityDto,
  UserPreferencesDto,
  GrowthPointDto,
  ProgressDashboardDto,
  ProgressInsightDto,
  PublicLearnerProgressDto,
  FinalizeLevelExitTestResult,
  LearnerOverviewDto,
  LevelMapDto,
  NotificationDto,
  PlacementResultDto,
  PlacementIntegrityViolationResult,
  ListeningAnswerCheckDto,
  ListeningExerciseDto,
  ListeningQuizAnswer,
  ListeningQuizResultDto,
  ListeningSummaryDto,
  ReadingAnswerCheckDto,
  ReadingPassageDto,
  ReadingQuizAnswer,
  ReadingQuizResultDto,
  ReadingSummaryDto,
  RecommendationDto,
  ReviewResultDto,
  RecommendedTopicsDto,
  ResumePlacementTestResult,
  StudyStatsDto,
  StartConversationResult,
  AccentTutorMessageDto,
  AccentTutorStartResult,
  AccentTutorTurnResult,
  AccentTutorAttemptScoreDto,
  AccentTutorEvaluationResult,
  IdeaCardsResult,
  StartLevelExitTestResult,
  StartPlacementTestResult,
  FreeTalkTopicDto,
  RoleplayScenarioDto,
  StartRoleplayResult,
  RoleplayEvaluationResult,
  PaymentProviderDto,
  StartSubscriptionResultDto,
  SubmitAnswerResult,
  SubmitSpeakingResult,
  SubmitWritingResult,
  SubmitUtteranceResult,
  SpeakingUtteranceStreamHandlers,
  SubscriptionDto,
  VideoFeedDto,
  VideoFeedItemDto,
  VideoPlaylistDto,
  VideoPlaylistSearchDto,
  VideoLessonDto,
  VideoQuizResultDto,
  GeneratedVideoQuizDto,
  VideoSummaryDto,
  VocabularyItemDto,
  VocabularyTopicDetailDto,
  VocabularyTopicPassageTranslationDto,
  VocabularyTopicSummaryDto,
  TopicQuizResultDto,
  TopicCompletionDto,
  RecordTopicModuleScoreResult,
  WordPronunciationCheckDto,
  JoinPremiumWaitlistRequest,
  PlanCatalogDto,
  SpeakingQuotaStatusDto,
  WordPronunciationDetailDto,
  WritingAssessmentDto,
  WritingTaskDto,
  WritingTaskSummaryDto,
  PronunciationResultDto,
  SpeakingPracticeAttemptResult,
  SpeakingPracticeWordDto,
  ChatTurn,
  ChatReplyDto,
} from "./types";
import {
  CefrLevel,
  Gender,
  AcquisitionSource,
  LearningGoal,
  PaymentProvider,
  ProductEventType,
  SkillType,
  SubscriptionPlan,
  VideoDifficultyRating,
  ReferralStatusDto,
} from "./types";
import type {
  CompetitionSettingsDto,
  CompetitionDto,
  SelectableTopicDto,
  CompetitionResultDto,
} from "./types";
import { apiUrl } from "./config";
import {
  getAuthToken,
  isNativePlatform,
  setAuthToken,
  clearAuthToken,
} from "./nativeAuth";
import { getStoredReferralCode, clearStoredReferralCode } from "@/lib/referral";
import { MANDATORY_REVIEW_REQUIRED_EVENT } from "@/lib/mandatoryReview";

export class ApiError extends Error {
  constructor(public status: number, message: string, public body?: unknown) {
    super(message);
    this.name = "ApiError";
  }
}

export interface ApiErrorBody {
  status?: number;
  code?: string | null;
  message?: string;
  errors?: string[] | null;
  correlationId?: string;
}

export function apiErrorDetails(error: unknown): ApiErrorBody | null {
  if (!(error instanceof ApiError) || typeof error.body !== "object" || error.body === null) return null;
  return error.body as ApiErrorBody;
}

export interface RateLimitErrorBody {
  code: string;
  message: string;
  retryAfterSeconds: number;
  correlationId: string;
}

export function rateLimitDetails(error: unknown): RateLimitErrorBody | null {
  if (!(error instanceof ApiError) || (error.status !== 429 && error.status !== 503)) return null;
  if (typeof error.body !== "object" || error.body === null) return null;
  const body = error.body as Partial<RateLimitErrorBody>;
  return typeof body.code === "string" && typeof body.message === "string"
    && typeof body.retryAfterSeconds === "number" && typeof body.correlationId === "string"
    ? body as RateLimitErrorBody
    : null;
}

interface ServerSentEvent {
  type: string;
  data: string;
}

function readServerSentEvents(buffer: string, flush = false): { events: ServerSentEvent[]; remainder: string } {
  const normalized = buffer.replace(/\r\n/g, "\n");
  const parts = normalized.split("\n\n");
  const remainder = flush ? "" : parts.pop() ?? "";
  const complete = flush ? parts.filter(Boolean) : parts;
  const events = complete.map((event) => {
    const lines = event.split("\n");
    const type = lines.find((line) => line.startsWith("event:"))?.slice(6).trim() ?? "message";
    const data = lines.filter((line) => line.startsWith("data:"))
      .map((line) => line.slice(5).trimStart())
      .join("\n");
    return { type, data };
  });
  return { events, remainder };
}

const DEFAULT_TIMEOUT_MS = 15_000;
const LONG_OPERATION_TIMEOUT_MS = 90_000;
const GET_CACHE_TTL_MS = 2_000;
const getCache = new Map<string, { expiresAt: number; promise: Promise<unknown> }>();
const LONG_OPERATION_PATHS = [
  "/api/placement/answer/writing",
  "/api/placement/answer/speaking",
  "/api/speaking/start",
  "/api/speaking/roleplay/start",
  "/api/speaking/utterance",
  "/api/speaking/segment-pronunciation",
  "/api/speaking/idea-cards",
  "/api/speaking/roleplay/evaluate",
  "/api/speaking/accent-tutors",
  "/api/vocabulary/pronounce",
  "/api/video/ingest",
  "/api/video/open",
  "/api/video/open-url",
  "/api/video/explain",
  "/api/assistant/ask",
  "/api/assistant/project",
  "/api/writing/submit",
];

const LONG_OPERATION_PATTERNS = [
  /^\/api\/video\/[^/]+\/quiz$/,
  /^\/api\/books\/[^/]+\/sections\/[^/]+\/learner\/[^/]+$/,
];

function requestTimeout(path: string): number {
  return LONG_OPERATION_PATHS.some((prefix) => path.startsWith(prefix)) ||
    LONG_OPERATION_PATTERNS.some((pattern) => pattern.test(path))
    ? LONG_OPERATION_TIMEOUT_MS
    : DEFAULT_TIMEOUT_MS;
}

function combineSignals(timeoutSignal: AbortSignal, callerSignal?: AbortSignal) {
  if (!callerSignal) return { signal: timeoutSignal, dispose: () => undefined };

  const controller = new AbortController();
  const abortFromTimeout = () => controller.abort(timeoutSignal.reason);
  const abortFromCaller = () => controller.abort(callerSignal.reason);

  if (timeoutSignal.aborted) abortFromTimeout();
  else timeoutSignal.addEventListener("abort", abortFromTimeout, { once: true });

  if (callerSignal.aborted) abortFromCaller();
  else callerSignal.addEventListener("abort", abortFromCaller, { once: true });

  return {
    signal: controller.signal,
    dispose: () => {
      timeoutSignal.removeEventListener("abort", abortFromTimeout);
      callerSignal.removeEventListener("abort", abortFromCaller);
    },
  };
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
  signal?: AbortSignal
): Promise<T> {
  const headers: Record<string, string> = {};
  if (body !== undefined) headers["Content-Type"] = "application/json";

  // Native shell authenticates with a Bearer token (the cross-origin cookie is unreliable);
  // the web build attaches nothing here and rides the HttpOnly cookie via credentials:"include".
  if (isNativePlatform()) {
    const token = getAuthToken();
    if (token) headers["Authorization"] = `Bearer ${token}`;
  }

  const timeoutController = new AbortController();
  const timeout = window.setTimeout(
    () => timeoutController.abort(new DOMException("Request timed out", "TimeoutError")),
    requestTimeout(path),
  );
  const combinedSignal = combineSignals(timeoutController.signal, signal);

  let res: Response;
  try {
    res = await fetch(apiUrl(path), {
      method,
      headers: Object.keys(headers).length > 0 ? headers : undefined,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      credentials: "include",
      signal: combinedSignal.signal,
    });
  } catch (error) {
    if (timeoutController.signal.aborted && !signal?.aborted) {
      throw new ApiError(408, `${method} ${path} → timeout`, { code: "request_timeout" });
    }
    throw error;
  } finally {
    window.clearTimeout(timeout);
    combinedSignal.dispose();
  }

  if (!res.ok) {
    // Session gone/expired: let the app drop back to the sign-in screen. Decoupled from
    // routing via a window event the AuthProvider listens for. The sign-in exchange itself
    // is exempt so a failed login doesn't trigger a redirect loop.
    if (res.status === 401 && !path.startsWith("/api/auth/")) {
      window.dispatchEvent(new CustomEvent("auth:unauthorized"));
    }

    let parsed: unknown;
    const text = await res.text();
    try {
      parsed = text ? JSON.parse(text) : undefined;
    } catch {
      parsed = text;
    }

    // Trial paywall (H.1): the server returns 402 with code "subscription_required" when a learner
    // reaches a topic past their free allowance. Surface the upgrade paywall globally (decoupled from
    // routing, like auth:unauthorized) so it works no matter which content request triggered it.
    if (
      res.status === 402 &&
      typeof parsed === "object" &&
      parsed !== null &&
      ((parsed as { code?: string }).code === "subscription_required" ||
        // The daily speaking budget is an upgrade prompt too, so it opens the same paywall rather
        // than surfacing as a generic error the learner cannot act on.
        (parsed as { code?: string }).code === "speaking_minutes_exhausted")
    ) {
      window.dispatchEvent(new CustomEvent("paywall:required"));
    }

    // Energy is a temporary availability state, never a purchase prompt. Keep this global so a
    // server-side Speaking rejection opens the same balance modal as a blocked Video tap.
    if (
      res.status === 409 &&
      typeof parsed === "object" &&
      parsed !== null &&
      (parsed as { code?: string }).code === "energy_exhausted"
    ) {
      window.dispatchEvent(new CustomEvent("energy:exhausted"));
    }

    if (
      res.status === 423 &&
      typeof parsed === "object" &&
      parsed !== null &&
      (parsed as { code?: string }).code === "review_required"
    ) {
      window.dispatchEvent(new CustomEvent(MANDATORY_REVIEW_REQUIRED_EVENT));
    }

    throw new ApiError(res.status, `${method} ${path} → ${res.status}`, parsed);
  }

  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

const get = <T>(path: string): Promise<T> => {
  const now = Date.now();
  const cached = getCache.get(path);
  if (cached && cached.expiresAt > now) return cached.promise as Promise<T>;

  const promise = request<T>("GET", path);
  getCache.set(path, { expiresAt: now + GET_CACHE_TTL_MS, promise });
  void promise.catch(() => {
    if (getCache.get(path)?.promise === promise) getCache.delete(path);
  });
  return promise;
};

async function streamServerSentEvents(
  path: string,
  handlers: Record<string, (payload: unknown) => void>,
  signal: AbortSignal,
) {
  const headers: Record<string, string> = { Accept: "text/event-stream" };
  if (isNativePlatform()) {
    const token = getAuthToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }
  const response = await fetch(apiUrl(path), {
    headers,
    credentials: "include",
    signal,
  });
  if (!response.ok || !response.body) throw new ApiError(response.status, `GET ${path} → ${response.status}`);

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";
  while (!signal.aborted) {
    const result = await reader.read();
    buffer += decoder.decode(result.value, { stream: !result.done });
    const parsed = readServerSentEvents(buffer, result.done);
    buffer = parsed.remainder;
    parsed.events.forEach((event) => {
      const handler = handlers[event.type];
      if (handler && event.data) handler(JSON.parse(event.data));
    });
    if (result.done) break;
  }
}

function mutate<T>(method: string, path: string, body?: unknown) {
  getCache.clear();
  return request<T>(method, path, body);
}

async function upload<T>(path: string, formData: FormData): Promise<T> {
  getCache.clear();
  const headers: Record<string, string> = {};
  if (isNativePlatform()) {
    const token = getAuthToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }
  const response = await fetch(apiUrl(path), {
    method: "PUT",
    headers: Object.keys(headers).length > 0 ? headers : undefined,
    body: formData,
    credentials: "include",
  });
  if (!response.ok) {
    const text = await response.text();
    let body: unknown = text;
    try { body = text ? JSON.parse(text) : undefined; } catch { /* keep text */ }
    throw new ApiError(response.status, `PUT ${path} → ${response.status}`, body);
  }
  return response.json() as Promise<T>;
}

async function uploadPost<T>(path: string, formData: FormData): Promise<T> {
  getCache.clear();
  const headers: Record<string, string> = {};
  if (isNativePlatform()) {
    const token = getAuthToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }
  const response = await fetch(apiUrl(path), {
    method: "POST",
    headers: Object.keys(headers).length > 0 ? headers : undefined,
    body: formData,
    credentials: "include",
  });
  if (!response.ok) {
    const text = await response.text();
    let body: unknown = text;
    try { body = text ? JSON.parse(text) : undefined; } catch { /* keep text */ }
    throw new ApiError(response.status, `POST ${path} → ${response.status}`, body);
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>;
}

const post = <T>(path: string, body?: unknown) => mutate<T>("POST", path, body);
const put = <T>(path: string, body?: unknown) => mutate<T>("PUT", path, body);
const del = <T>(path: string, body?: unknown) => mutate<T>("DELETE", path, body);

export const api = {
  health: () => get<{ status: string }>("/health"),

  publicMetrics: () => get<PublicSocialProofMetricsDto>("/api/public/metrics"),

  support: {
    conversation: () => get<SupportConversationDto>("/api/support/conversation"),
    send: (text: string, files: File[]) => {
      const form = new FormData();
      if (text.trim()) form.append("text", text.trim());
      files.forEach((file) => form.append("files", file));
      return uploadPost<SupportMessageDto>("/api/support/messages", form);
    },
    markRead: (conversationId: string) => post<void>("/api/support/read", { conversationId }),
  },

  // Native push: register (or refresh) this device's FCM token so the server can deliver
  // status-bar notifications + app badge even when the app is closed. Ownership is enforced
  // server-side against the JWT, so the learnerId must be the signed-in learner.
  registerDevice: (learnerId: string, token: string, platform: string) =>
    post<void>(`/api/notifications/${learnerId}/register-device`, {
      token,
      platform,
    }),

  analytics: {
    track: (learnerId: string, eventType: ProductEventType, source?: string) =>
      post<void>(`/api/learning/${learnerId}/events`, { eventType, source }),
  },

  // Google sign-in (the only auth method). The browser holds the session in an HttpOnly
  // cookie; these calls rely on `credentials: "include"` above.
  auth: {
    config: () => get<AuthConfigDto>("/api/auth/config"),
    // Returns the user and (for native) stores the session token so subsequent calls can send
    // it as a Bearer header. The web build ignores the stored token and uses its cookie.
    google: async (idToken: string) => {
      // Attach any captured ?ref= code so a first-time sign-up is credited to the referrer.
      // Cleared afterwards; the server only honours it for brand-new accounts anyway.
      const referralCode = getStoredReferralCode() ?? undefined;
      const res = await post<GoogleSignInResponse>("/api/auth/google", {
        idToken,
        referralCode,
      });
      await setAuthToken(res.token);
      clearStoredReferralCode();
      return res.user;
    },
    me: () => get<AuthenticatedUserDto>("/api/auth/me"),
    logout: async () => {
      await post<void>("/api/auth/logout");
      await clearAuthToken();
    },
    // Development-only local sign-in (no Google OAuth). The backend only exposes
    // /api/auth/dev-login when running in the Development environment, and the SPA only
    // shows the button under its dev build, so this never reaches Production.
    dev: async () => {
      const res = await post<GoogleSignInResponse>("/api/auth/dev-login", {});
      await setAuthToken(res.token);
      return res.user;
    },
    // Live availability check for the handle-setup and profile-edit screens.
    usernameAvailable: (username: string) =>
      get<UsernameAvailabilityDto>(
        `/api/auth/username-available?username=${encodeURIComponent(username)}`
      ),
    // Save the editable profile fields (display name + username). Returns the refreshed user.
    updateProfile: (displayName: string, username: string) =>
      put<AuthenticatedUserDto>("/api/auth/profile", { displayName, username }),
    setDemographics: (
      birthDate: string,
      gender: Gender,
      acquisitionSource: AcquisitionSource,
      acquisitionSourceOther?: string | null,
    ) => put<AuthenticatedUserDto>("/api/auth/demographics", {
      birthDate,
      gender,
      acquisitionSource,
      acquisitionSourceOther,
    }),
    updateAvatar: (file: File) => {
      const formData = new FormData();
      formData.append("file", file);
      return upload<AuthenticatedUserDto>("/api/auth/avatar", formData);
    },
    deleteAvatar: () => del<AuthenticatedUserDto>("/api/auth/avatar"),
    // Save the name the AI tutor should use (the one-time "what should I call you?" answer).
    setPreferredName: (preferredName: string) =>
      put<AuthenticatedUserDto>("/api/auth/preferred-name", { preferredName }),
    // Save the learner's onboarding goal (goal-based onboarding). Returns the refreshed user so
    // the goal gate stops showing the goal screen.
    setLearningGoal: (goal: LearningGoal) =>
      put<AuthenticatedUserDto>("/api/auth/learning-goal", { goal }),
    preferences: () => get<UserPreferencesDto>("/api/auth/preferences"),
    updatePreferences: (preferences: UserPreferencesDto) =>
      put<UserPreferencesDto>("/api/auth/preferences", preferences),
    // Permanently delete the account. The re-typed email is re-verified server-side.
    deleteAccount: (confirmationEmail: string) =>
      del<void>("/api/auth/account", { confirmationEmail }),
  },

  developer: {
    keys: () => get<DeveloperApiKeyDto[]>("/api/developer/keys"),
    createKey: (name: string) =>
      post<CreatedDeveloperApiKeyDto>("/api/developer/keys", { name }),
    revokeKey: (id: string) => del<void>(`/api/developer/keys/${id}`),
  },

  placement: {
    start: (learnerId: string, includeSpeaking = true) =>
      post<StartPlacementTestResult>("/api/placement/start", {
        learnerId,
        includeSpeaking,
      }),
    resume: (sessionId: string) =>
      // Recovery must read the latest saved item, never the generic short GET cache.
      request<ResumePlacementTestResult>("GET", `/api/placement/session/${sessionId}`),
    answer: (
      sessionId: string,
      questionId: string,
      selectedOptionIndex: number
    ) =>
      post<SubmitAnswerResult>("/api/placement/answer", {
        sessionId,
        questionId,
        selectedOptionIndex,
      }),
    answerWriting: (sessionId: string, taskId: string, text: string) =>
      post<SubmitWritingResult>("/api/placement/answer/writing", {
        sessionId,
        taskId,
        text,
      }),
    // audioContent is a base64-encoded clip - the backend command binds it to a byte[].
    answerSpeaking: (sessionId: string, taskId: string, audioContent: string) =>
      post<SubmitSpeakingResult>("/api/placement/answer/speaking", {
        sessionId,
        taskId,
        audioContent,
      }),
    finalize: (sessionId: string) =>
      post<PlacementResultDto>("/api/placement/finalize", { sessionId }),
    reportIntegrityViolation: (sessionId: string, incidentId: string, reason: string) =>
      post<PlacementIntegrityViolationResult>("/api/placement/integrity-violation", {
        sessionId,
        incidentId,
        reason,
      }),
    // URL of a listening item's spoken clip (played by an <audio> element).
    audioUrl: (questionId: string) =>
      apiUrl(`/api/placement/audio/${questionId}`),
  },

  gamification: {
    status: (learnerId: string) =>
      get<GamificationStatusDto>(`/api/gamification/${learnerId}`),
    completeTask: (learnerId: string) =>
      post<GamificationStatusDto>(
        `/api/gamification/${learnerId}/complete-task`
      ),
    // `level` defaults server-side to the learner's own current CEFR level when omitted.
    leaderboard: (learnerId: string, level?: CefrLevel) =>
      get<LeaderboardDto>(
        `/api/gamification/${learnerId}/leaderboard${
          level !== undefined ? `?level=${level}` : ""
        }`
      ),
    points: (learnerId: string) =>
      get<PointsBalanceDto>(`/api/gamification/${learnerId}/points`),
    energy: (learnerId: string) =>
      get<EnergyDto>(`/api/gamification/${learnerId}/energy`),
    consumeEnergy: (learnerId: string, action: EnergyAction, referenceId: string) =>
      post<EnergyDto>(`/api/gamification/${learnerId}/energy/consume`, { action, referenceId }),
    redeemDiscount: (learnerId: string, coinsCost: number) =>
      post<RedemptionDto>(
        `/api/gamification/${learnerId}/redeem-discount`,
        { coinsCost }
      ),
  },

  referral: {
    // The signed-in learner's referral standing (code, share stats, earned bonus). The learner
    // is resolved from the session server-side, so no id is passed.
    status: () => get<ReferralStatusDto>("/api/referral"),
  },

  learning: {
    // Onboarding: create the learner's profile at a chosen starting level (skip placement).
    start: (learnerId: string, level: CefrLevel) =>
      post<void>("/api/learning/start", { learnerId, level }),
    overview: (learnerId: string) =>
      get<LearnerOverviewDto>(`/api/learning/${learnerId}/overview`),
    recommendations: (learnerId: string) =>
      get<RecommendationDto[]>(`/api/learning/${learnerId}/recommendations`),
    // Goal-tailored topic suggestions for the home "for your goal" strip (goal-based onboarding).
    recommendedTopics: (learnerId: string, count = 6) =>
      get<RecommendedTopicsDto>(
        `/api/learning/${learnerId}/recommended-topics?count=${count}`
      ),
    growth: (learnerId: string, weeks = 8) =>
      get<GrowthPointDto[]>(`/api/learning/${learnerId}/growth?weeks=${weeks}`),
    progressInsight: (learnerId: string, today: string) =>
      get<ProgressInsightDto>(
        `/api/learning/${learnerId}/progress-insight?today=${today}`
      ),
    progressDashboard: (learnerId: string, today: string) =>
      get<ProgressDashboardDto>(
        `/api/learning/${learnerId}/progress-dashboard?today=${today}`
      ),
    publicProgress: (learnerId: string, today: string) =>
      get<PublicLearnerProgressDto>(
        `/api/learning/${learnerId}/public-progress?today=${today}`
      ),

    // Aggregated study-time statistics for the progress dashboard. `today` is the learner's
    // local calendar day (YYYY-MM-DD) so week/month/year boundaries match their own clock.
    studyStats: (learnerId: string, today: string) =>
      get<StudyStatsDto>(
        `/api/learning/${learnerId}/study-stats?today=${today}`
      ),

    // A study-time heartbeat (active learning page → elapsed seconds for a skill on the local day).
    recordStudyTime: (
      learnerId: string,
      skill: SkillType,
      seconds: number,
      localDate: string
    ) =>
      post<void>(`/api/learning/${learnerId}/study-time`, {
        skill,
        seconds,
        localDate,
      }),

    // Fire-and-forget variant used when the page is hidden/closed: sendBeacon survives unload and
    // includes the session cookie. Returns whether the beacon was queued.
    studyTimeBeacon: (
      learnerId: string,
      skill: SkillType,
      seconds: number,
      localDate: string
    ): boolean => {
      if (typeof navigator === "undefined" || !navigator.sendBeacon)
        return false;
      const blob = new Blob([JSON.stringify({ skill, seconds, localDate })], {
        type: "application/json",
      });
      return navigator.sendBeacon(
        `/api/learning/${learnerId}/study-time`,
        blob
      );
    },
  },

  speaking: {
    accentTutor: {
      start: (tutorId: string) =>
        post<AccentTutorStartResult>(`/api/speaking/accent-tutors/${encodeURIComponent(tutorId)}/start`, {}),
      nudge: (tutorId: string, history: AccentTutorMessageDto[]) =>
        post<AccentTutorStartResult>(`/api/speaking/accent-tutors/${encodeURIComponent(tutorId)}/nudge`, { history }),
      turn: (tutorId: string, audioContent: string, history: AccentTutorMessageDto[]) =>
        post<AccentTutorTurnResult>(`/api/speaking/accent-tutors/${encodeURIComponent(tutorId)}/turn`, {
          audioContent,
          history,
        }),
      turnStream: async (
        tutorId: string,
        audioContent: string,
        history: AccentTutorMessageDto[],
        handlers: {
          onRecognized: (payload: { transcript: string }) => void;
          onTutor: (payload: { tutorText: string }) => void;
          onPronunciation: (payload: { pronunciation: import("./types").PronunciationResultDto }) => void;
          onAudio: (payload: Pick<AccentTutorTurnResult, "tutorAudioBase64" | "isNaturalVoice" | "wordTimings">) => void;
          onStatus?: (payload: { phase: string }) => void;
        },
        signal?: AbortSignal,
        isInterruption = false,
      ) => {
        const headers: Record<string, string> = {
          "Content-Type": "application/json",
          Accept: "text/event-stream",
        };
        if (isNativePlatform()) {
          const token = getAuthToken();
          if (token) headers.Authorization = `Bearer ${token}`;
        }
        const response = await fetch(apiUrl(`/api/speaking/accent-tutors/${encodeURIComponent(tutorId)}/turn/stream`), {
          method: "POST",
          headers,
          credentials: "include",
          body: JSON.stringify({ audioContent, history, isInterruption }),
          signal,
        });
        if (!response.ok || !response.body) {
          throw new ApiError(response.status, `Accent tutor stream → ${response.status}`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = "";
        for (;;) {
          const result = await reader.read();
          buffer += decoder.decode(result.value, { stream: !result.done });
          const parsed = readServerSentEvents(buffer, result.done);
          buffer = parsed.remainder;
          for (const event of parsed.events) {
            if (event.type === "done") return;
            if (!event.data) continue;
            const payload = JSON.parse(event.data);
            if (event.type === "error") throw new ApiError(503, payload.message ?? "Live tutor unavailable", payload);
            if (event.type === "recognized") handlers.onRecognized(payload);
            else if (event.type === "tutor") handlers.onTutor(payload);
            else if (event.type === "pronunciation") handlers.onPronunciation(payload);
            else if (event.type === "audio") handlers.onAudio(payload);
            else if (event.type === "status") handlers.onStatus?.(payload);
          }
          if (result.done) return;
        }
      },
      evaluate: (tutorId: string, history: AccentTutorMessageDto[], scores: AccentTutorAttemptScoreDto[]) =>
        post<AccentTutorEvaluationResult>(`/api/speaking/accent-tutors/${encodeURIComponent(tutorId)}/evaluate`, {
          history,
          scores,
        }),
    },
    // Topic selection (both optional → open conversation): `topic` is a curated code
    // (e.g. "travel"); `vocabularyTopicId` links the chat to a studied vocabulary topic,
    // which the server resolves to that topic's title + words for the tutor to reinforce.
    start: (
      learnerId: string,
      level: CefrLevel,
      topic?: string,
      vocabularyTopicId?: string
    ) =>
      post<StartConversationResult>("/api/speaking/start", {
        learnerId,
        level,
        topic,
        vocabularyTopicId,
      }),
    liveCapabilities: () =>
      get<import("./types").SpeakingLiveCapabilities>("/api/speaking/live/capabilities"),
    // audioContent is a base64-encoded clip - the backend command binds it to a byte[].
    utterance: (
      sessionId: string,
      audioContent: string,
      confirmation?: { transcript: string; outcome: import("./types").SpeakingTranscriptConfirmationOutcome },
    ) =>
      post<SubmitUtteranceResult>("/api/speaking/utterance", {
        sessionId,
        audioContent,
        confirmedTranscript: confirmation?.transcript ?? null,
        confirmationOutcome: confirmation?.outcome ?? null,
      }),
    utteranceStream: async (
      sessionId: string,
      audioContent: string,
      handlers: SpeakingUtteranceStreamHandlers,
      confirmation?: { transcript: string; outcome: import("./types").SpeakingTranscriptConfirmationOutcome },
    ) => {
      const headers: Record<string, string> = {
        "Content-Type": "application/json",
        Accept: "text/event-stream",
      };
      if (isNativePlatform()) {
        const token = getAuthToken();
        if (token) headers.Authorization = `Bearer ${token}`;
      }
      const response = await fetch(apiUrl("/api/speaking/utterance/stream"), {
        method: "POST",
        headers,
        credentials: "include",
        body: JSON.stringify({
          sessionId,
          audioContent,
          confirmedTranscript: confirmation?.transcript ?? null,
          confirmationOutcome: confirmation?.outcome ?? null,
        }),
      });
      if (!response.ok || !response.body) {
        throw new ApiError(response.status, "Speaking stream failed");
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";
      let ended = false;
      while (!ended) {
        const result = await reader.read();
        ended = result.done;
        buffer += decoder.decode(result.value, { stream: !ended });
        const parsed = readServerSentEvents(buffer, ended);
        buffer = parsed.remainder;
        for (const event of parsed.events) {
          if (event.type === "done") return;
          if (event.type === "error") {
            throw new ApiError(503, "Speaking stream unavailable", event.data);
          }
          if (!event.data) continue;
          const payload = JSON.parse(event.data);
          if (event.type === "recognized") handlers.onRecognized(payload);
          else if (event.type === "tutor") handlers.onTutor(payload);
          else if (event.type === "pronunciation") handlers.onPronunciation(payload);
          else if (event.type === "audio") handlers.onAudio(payload);
          else if (event.type === "progress") handlers.onProgress(payload);
          else if (event.type === "unrecognized") handlers.onUnrecognized(payload);
          else if (event.type === "transcript-confirmation-required") handlers.onTranscriptConfirmationRequired?.(payload);
          else if (event.type === "tutor-unavailable") handlers.onTutorUnavailable(payload);
        }
      }
    },
    /** Today's remaining speaking minutes, read before entering the conversation room. */
    quota: (learnerId: string) =>
      get<SpeakingQuotaStatusDto>(`/api/speaking/quota/${learnerId}`),
    wordDetail: (word: string) =>
      get<WordPronunciationDetailDto>(
        `/api/speaking/word/${encodeURIComponent(word)}`
      ),
    practiceWords: (learnerId: string) =>
      get<SpeakingPracticeWordDto[]>(`/api/speaking/practice-words/${learnerId}`),
    practiceAttempt: (practiceWordId: string, audioContent: string) =>
      post<SpeakingPracticeAttemptResult>(
        `/api/speaking/practice-words/${practiceWordId}/attempt`,
        { audioContent },
      ),

    // Shadowing: scores one video transcript line the learner spoke along with (a slice of one
    // continuous recording), against that line's own known text. audioContent is base64.
    assessSegment: (referenceText: string, audioContent: string, signal?: AbortSignal) =>
      request<PronunciationResultDto>("POST", "/api/speaking/segment-pronunciation", {
        referenceText,
        audioContent,
      }, signal),

    // "I need an idea" helper for a live conversation: concrete talking-point cards keyed by the
    // active session so they fit its topic/level/recent turns. Always resolves to usable cards.
    ideaCards: (sessionId: string) =>
      post<IdeaCardsResult>("/api/speaking/idea-cards", { sessionId }),

    // Free-talk topics ("Erkin suhbat"): the curated catalog of conversation subjects, 20 per CEFR
    // level. `level` narrows to that level's 20 topics; `allLevels` returns the whole A1→C2 catalog.
    // Picking one starts a conversation via `start` with the topic's code.
    freeTalkTopics: (level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<FreeTalkTopicDto[]>(
        `/api/speaking/free-talk-topics${q ? `?${q}` : ""}`
      );
    },

    // Roleplay: list the scenarios (20 per CEFR level, 120 total - `level` narrows to one level,
    // `allLevels` browses the whole A1→C2 catalog), start one (persona greeting), and score the
    // finished sitting. Turns run through the shared `utterance` method above; only the scenario
    // code is ever sent.
    roleplayScenarios: (level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<RoleplayScenarioDto[]>(
        `/api/speaking/roleplay/scenarios${q ? `?${q}` : ""}`
      );
    },
    roleplayStart: (
      learnerId: string,
      level: CefrLevel,
      scenarioCode: string
    ) =>
      post<StartRoleplayResult>("/api/speaking/roleplay/start", {
        learnerId,
        level,
        scenarioCode,
      }),
    roleplayEvaluate: (sessionId: string) =>
      post<RoleplayEvaluationResult>("/api/speaking/roleplay/evaluate", {
        sessionId,
      }),
  },

  vocabulary: {
    list: (learnerId: string) =>
      get<VocabularyItemDto[]>(`/api/vocabulary/${learnerId}`),
    due: (learnerId: string) =>
      get<DueReviewDto[]>(`/api/vocabulary/${learnerId}/due`),
    reviewStatus: (learnerId: string) =>
      get<MandatoryReviewStatusDto>(`/api/vocabulary/${learnerId}/review-status`),
    learn: (
      learnerId: string,
      word: string,
      translation: string,
      exampleSentence?: string
    ) =>
      post<VocabularyItemDto>("/api/vocabulary/learn", {
        learnerId,
        word,
        translation,
        exampleSentence,
      }),
    // Submits one SRS review. Which of the two answer fields to send depends on the due item's
    // miniTestType (Application.Vocabulary.SubmitReview.SubmitReviewCommand): ClozeChoice/
    // WrittenUsage send `submittedAnswer` (the server verifies it - the client's own opinion of
    // "passed" is never trusted for these), the rest send `selfRatedPassed` (flip-card recall).
    review: (
      vocabularyItemId: string,
      answer: { submittedAnswer?: string; selfRatedPassed?: boolean }
    ) =>
      post<ReviewResultDto>("/api/vocabulary/review", {
        vocabularyItemId,
        submittedAnswer: answer.submittedAnswer,
        selfRatedPassed: answer.selfRatedPassed,
      }),
    notifications: (learnerId: string, cursor?: string, pageSize = 30) => {
      const params = new URLSearchParams({ pageSize: String(pageSize) });
      if (cursor) params.set("cursor", cursor);
      return get<import("./types").CursorPageDto<NotificationDto>>(`/api/vocabulary/${learnerId}/notifications?${params}`);
    },
    markRead: (learnerId: string) =>
      post<void>(`/api/vocabulary/${learnerId}/notifications/read`),
    // Dismiss one tapped notification: it drops out of the feed while everything else stays.
    dismissNotification: (learnerId: string, notificationId: string) =>
      post<void>(`/api/vocabulary/${learnerId}/notifications/${notificationId}/read`),

    // Module 4 - vocabulary in context. Topics for a level (defaults to the learner's level);
    // `allLevels` returns the whole A1→C2 catalog easiest-first.
    topics: (learnerId: string, level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<VocabularyTopicSummaryDto[]>(
        `/api/vocabulary/topics/${learnerId}${q ? `?${q}` : ""}`
      );
    },
    // Full topic detail; the passage + 15 words are generated on first open and cached.
    topic: (topicId: string) =>
      get<VocabularyTopicDetailDto>(`/api/vocabulary/topic/${topicId}`),
    // The passage split into sentences, each paired with its Uzbek translation (the "MATN" card).
    passageTranslation: (topicId: string) =>
      post<VocabularyTopicPassageTranslationDto>(
        `/api/vocabulary/topic/${topicId}/passage-translation`,
        {}
      ),
    // Grade the cloze quiz: answers map a question index to the chosen option index. Passing a
    // learnerId records the quiz percentage as the topic's Vocabulary module score (K.5).
    submitTopicQuiz: (
      topicId: string,
      answers: Record<number, number>,
      learnerId?: string
    ) =>
      post<TopicQuizResultDto>("/api/vocabulary/topic/quiz", {
        topicId,
        answers,
        learnerId,
      }),
    // Check the learner's spoken pronunciation of a target word (audioContent is base64).
    pronounce: (word: string, audioContent: string, signal?: AbortSignal) =>
      request<WordPronunciationCheckDto>("POST", "/api/vocabulary/pronounce", {
        word,
        audioContent,
      }, signal),

    // K.5 topic mastery across all six lesson modules. `module` is the numeric SkillType code
    // (Speaking=1, Listening=2, Reading=3, Writing=4, Grammar=5, Vocabulary=6) - enums bind
    // numerically over the wire, the same as GrammarExerciseType.
    topicCompletion: (topicId: string, learnerId: string) =>
      get<TopicCompletionDto>(
        `/api/vocabulary/topic/${topicId}/completion/${learnerId}`
      ),
    recordTopicModuleScore: (
      learnerId: string,
      topicId: string,
      module: SkillType,
      score: number
    ) =>
      post<RecordTopicModuleScoreResult>("/api/vocabulary/topic/completion", {
        learnerId,
        topicId,
        module,
        score,
      }),
    resetTopicModuleScore: (
      learnerId: string,
      topicId: string,
      module: SkillType
    ) =>
      post<TopicCompletionDto>("/api/vocabulary/topic/completion/reset", {
        learnerId,
        topicId,
        module,
      }),
  },

  levels: {
    // The Level Map for a CEFR level (defaults to the learner's level): can-do statements,
    // ordered topics, progress and exit-test readiness (PROJECT-SPEC M.3).
    map: (learnerId: string, level?: CefrLevel) => {
      const q = level ? `?level=${level}` : "";
      return get<LevelMapDto>(`/api/levels/map/${learnerId}${q}`);
    },

    // Level Exit Test (PROJECT-SPEC M.5). Start pins the adaptive engine to the learner's
    // current level; the questions/audio in between reuse the shared placement endpoints
    // (`api.placement.answer*` / `audioUrl`) since the session lives in the same store.
    exitTest: {
      start: (learnerId: string, includeSpeaking = true, testLevel?: CefrLevel) =>
        post<StartLevelExitTestResult>("/api/levels/exit-test/start", {
          learnerId,
          includeSpeaking,
          testLevel,
        }),
      finalize: (sessionId: string) =>
        post<FinalizeLevelExitTestResult>("/api/levels/exit-test/finalize", {
          sessionId,
        }),
    },
  },

  video: {
    catalog: (learnerId: string) =>
      get<VideoSummaryDto[]>(`/api/video/catalog/${learnerId}`),
    // Infinite, level-adaptive feed (B.3 Bosqich 2). Pass the previous page's nextCursor to
    // load the next page as the learner scrolls down.
    feed: (learnerId: string, cursor?: string | null, pageSize = 12, visitSeed?: string) => {
      const params = new URLSearchParams({ pageSize: String(pageSize) });
      if (cursor) params.set("cursor", cursor);
      if (!cursor && visitSeed) params.set("visitSeed", visitSeed);
      return get<VideoFeedDto>(
        `/api/video/feed/${learnerId}?${params.toString()}`
      );
    },
    // Learner-typed search: English-only, safe-search-strict YouTube results for any query
    // (plus an opaque cursor for "load more"). Adult/porn results are filtered out server-side.
    search: (query: string, cursor?: string | null, pageSize = 12) => {
      const params = new URLSearchParams({ q: query, pageSize: String(pageSize) });
      if (cursor) params.set("cursor", cursor);
      return get<VideoFeedDto>(
        `/api/video/search?${params.toString()}`
      );
    },
    playlistSearch: (query: string) =>
      get<VideoPlaylistSearchDto>(`/api/video/playlist/search?q=${encodeURIComponent(query)}`),
    featuredPlaylist: () => get<VideoPlaylistDto | null>("/api/video/playlist/featured"),
    playlist: (playlistId: string) =>
      get<VideoPlaylistDto | null>(`/api/video/playlist/${encodeURIComponent(playlistId)}`),
    // Opens a feed video into a playable lesson (created on first open); returns the lesson
    // so the client can navigate to its preview.
    open: (item: VideoFeedItemDto) =>
      post<VideoLessonDto>("/api/video/open", {
        youTubeVideoId: item.youTubeVideoId,
        title: item.title,
        channel: item.channel,
        durationSeconds: item.durationSeconds,
        topic: item.topic,
        level: item.level,
      }),
    // Opens an arbitrary YouTube video the learner pasted (its extracted 11-char video id)
    // into a playable lesson; returns the lesson so the client can navigate to the player.
    openUrl: (youTubeVideoId: string) =>
      post<VideoLessonDto>("/api/video/open-url", { youTubeVideoId }),
    lesson: (videoLessonId: string) =>
      get<VideoLessonDto>(`/api/video/${videoLessonId}`),
    generateQuiz: (videoLessonId: string, learnerId: string) =>
      post<GeneratedVideoQuizDto>(`/api/video/${videoLessonId}/quiz`, { learnerId }),
    quizSession: (quizId: string, learnerId: string) =>
      get<GeneratedVideoQuizDto>(`/api/video/quiz/${quizId}?learnerId=${encodeURIComponent(learnerId)}`),
    quiz: (videoLessonId: string, learnerId: string, answers: Record<string, number>, quizId?: string) =>
      post<VideoQuizResultDto>("/api/video/quiz", {
        videoLessonId, learnerId, quizId,
        answers: Object.entries(answers).map(([questionId, selectedOptionIndex]) => ({ questionId, selectedOptionIndex })),
      }),
    rate: (
      videoLessonId: string,
      learnerId: string,
      rating: VideoDifficultyRating
    ) => post<unknown>("/api/video/rate", { videoLessonId, learnerId, rating }),
    // Save a word/phrase met in a video into the SRS as a Video-sourced item, whose
    // first mini-test is listening recognition (PROJECT-SPEC Faza 4 ↔ Faza 3 / B.1).
    saveWord: (
      learnerId: string,
      word: string,
      translation: string,
      exampleSentence?: string | null
    ) =>
      post<VocabularyItemDto>("/api/video/word", {
        learnerId,
        word,
        translation,
        exampleSentence,
      }),
    // Admin/curation: ingest a YouTube video into the curated catalog (PROJECT-SPEC Faza 4,
    // rule 17.3). When Hangfire is enabled the server runs it in the background and returns
    // 202 (no body → undefined); otherwise it returns the ingested lesson synchronously.
    ingest: (youTubeVideoId: string, topic: string) =>
      post<VideoLessonDto | undefined>("/api/video/ingest", {
        youTubeVideoId,
        topic,
      }),
    // Explain-chat: the server resolves the lesson's complete real transcript by id; `focusText`
    // carries the currently visible line for word/sentence questions, while video-level questions
    // use the full transcript. History remains client-side and is resent for follow-ups.
    explain: (videoLessonId: string, focusText: string, userMessage: string, history: ChatTurn[]) =>
      post<ChatReplyDto>("/api/video/explain", {
        videoLessonId,
        focusText,
        userMessage,
        history,
      }),
    explainStream: async (
      videoLessonId: string,
      focusText: string,
      userMessage: string,
      history: ChatTurn[],
      onChunk: (chunk: string) => void,
    ) => {
      const headers: Record<string, string> = { "Content-Type": "application/json", Accept: "text/event-stream" };
      if (isNativePlatform()) {
        const token = getAuthToken();
        if (token) headers.Authorization = `Bearer ${token}`;
      }
      let response: Response;
      try {
        response = await fetch(apiUrl("/api/video/explain/stream"), {
          method: "POST",
          headers,
          credentials: "include",
          body: JSON.stringify({ videoLessonId, focusText, userMessage, history }),
        });
      } catch (error) {
        try {
          return await api.video.explain(videoLessonId, focusText, userMessage, history);
        } catch (fallbackError) {
          throw fallbackError instanceof ApiError
            ? fallbackError
            : new ApiError(0, "Video AI network failed", error);
        }
      }
      if (!response.ok || !response.body) {
        if (response.status >= 500 || response.status === 404 || response.status === 405) {
          return api.video.explain(videoLessonId, focusText, userMessage, history);
        }
        throw new ApiError(response.status, "Video AI stream failed");
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";
      let reply = "";
      let done = false;
      while (!done) {
        const result = await reader.read();
        done = result.done;
        const { value } = result;
        buffer += decoder.decode(value, { stream: !done });
        const events = buffer.split("\n\n");
        buffer = events.pop() ?? "";
        for (const event of events) {
          const eventType = event.match(/^event:\s*(.+)$/m)?.[1];
          const data = event.match(/^data:\s*(.+)$/m)?.[1];
          if (eventType === "chunk" && data) {
            const chunk = (JSON.parse(data) as { text: string }).text;
            reply += chunk;
            onChunk(chunk);
          } else if (eventType === "error") {
            let body: unknown = data;
            try {
              body = data ? JSON.parse(data) : data;
            } catch {
              // Preserve plain-text error events such as "unavailable".
            }
            throw new ApiError(503, "Video AI unavailable", body);
          }
        }
      }
      return reply
        ? { replyUz: reply } satisfies ChatReplyDto
        : api.video.explain(videoLessonId, focusText, userMessage, history);
    },
  },

  reading: {
    // No filter → adapted to the learner's level. `level` browses one CEFR band; `allLevels`
    // returns the whole catalog easiest-first (a progressively harder list across every level).
    // Each row is a learning-spine topic; the lesson is generated and cached when the topic opens.
    catalog: (learnerId: string, level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<ReadingSummaryDto[]>(
        `/api/reading/catalog/${learnerId}${q ? `?${q}` : ""}`
      );
    },
    // Keyed by the learning-spine topic id; the lesson is generated and cached on first open.
    passage: (topicId: string) =>
      get<ReadingPassageDto>(`/api/reading/topic/${topicId}`),
    checkAnswer: (topicId: string, questionId: string, selectedOptionIndex: number) =>
      post<ReadingAnswerCheckDto>("/api/reading/quiz/check", {
        topicId,
        questionId,
        selectedOptionIndex,
      }),
    submit: (
      topicId: string,
      learnerId: string,
      answers: ReadingQuizAnswer[]
    ) =>
      post<ReadingQuizResultDto>("/api/reading/quiz", {
        topicId,
        learnerId,
        answers,
      }),
  },

  // Books library (Home → Books): multi-section graded readers. No filter adapts to the learner's
  // level; `level` browses one CEFR band; `allLevels` returns the whole library easiest-first. A
  // section's body + ten-question quiz are generated and cached when the section is first opened.
  books: {
    catalog: (learnerId: string, level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<BookSummaryDto[]>(
        `/api/books/catalog/${learnerId}${q ? `?${q}` : ""}`
      );
    },
    detail: (bookId: string, learnerId: string) =>
      get<BookDetailDto>(`/api/books/${bookId}/learner/${learnerId}`),
    section: (bookId: string, sectionId: string, learnerId: string) =>
      get<BookSectionDto>(
        `/api/books/${bookId}/sections/${sectionId}/learner/${learnerId}`
      ),
    checkAnswer: (
      bookId: string,
      sectionId: string,
      questionId: string,
      selectedOptionIndex: number
    ) =>
      post<BookQuestionOutcomeDto>("/api/books/answers/check", {
        bookId,
        sectionId,
        questionId,
        selectedOptionIndex,
      }),
    submit: (
      bookId: string,
      sectionId: string,
      learnerId: string,
      answers: BookQuizAnswer[]
    ) =>
      post<BookQuizResultDto>("/api/books/quiz", {
        bookId,
        sectionId,
        learnerId,
        answers,
      }),
  },

  listening: {
    // No filter → adapted to the learner's level. `level` browses one CEFR band; `allLevels`
    // returns the whole catalog easiest-first (a progressively harder list across every level).
    // Each row is a learning-spine topic; the exercise is generated and cached when the topic opens.
    catalog: (learnerId: string, level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<ListeningSummaryDto[]>(
        `/api/listening/catalog/${learnerId}${q ? `?${q}` : ""}`
      );
    },
    // Keyed by the learning-spine topic id; the exercise is generated and cached on first open.
    exercise: (topicId: string) =>
      get<ListeningExerciseDto>(`/api/listening/topic/${topicId}`),
    // URL of the synthesized clip (played by an <audio> element); cached server-side (rule 10).
    audioUrl: (topicId: string) =>
      apiUrl(`/api/listening/topic/${topicId}/audio`),
    checkAnswer: (
      topicId: string,
      questionId: string,
      selectedOptionIndex: number
    ) =>
      post<ListeningAnswerCheckDto>("/api/listening/answers/check", {
        topicId,
        questionId,
        selectedOptionIndex,
      }),
    // A passing score is credited toward the topic's Listening module in its mastery checklist (K.5).
    submit: (
      topicId: string,
      learnerId: string,
      answers: ListeningQuizAnswer[]
    ) =>
      post<ListeningQuizResultDto>("/api/listening/quiz", {
        topicId,
        learnerId,
        answers,
      }),
  },

  // On-demand English→Uzbek sentence translation (click/hover a sentence to see its meaning).
  // Cached server-side, so repeated sentences are translated only once.
  translate: (
    text: string,
    level?: CefrLevel,
    context?: { speaker?: "learner" | "tutor"; topic?: string; previousTurns?: string[] },
  ) => post<TranslationDto>("/api/translate", {
    text,
    level: level ?? null,
    speaker: context?.speaker ?? null,
    topic: context?.topic ?? null,
    previousTurns: context?.previousTurns ?? null,
  }),

  assistant: {
    ask: (question: string, history: AssistantTurnDto[]) =>
      post<AssistantReplyDto>("/api/assistant/ask", { question, history }),
    askProject: (question: string, history: AssistantTurnDto[], locale: "uz" | "en" = "uz") =>
      post<AssistantReplyDto>("/api/assistant/project", { question, history, locale }),
    askContextStream: async (
      area: string,
      title: string,
      context: string,
      focusText: string,
      question: string,
      history: AssistantTurnDto[],
      onChunk: (chunk: string) => void,
    ) => {
      const headers: Record<string, string> = { "Content-Type": "application/json", Accept: "text/event-stream" };
      if (isNativePlatform()) {
        const token = getAuthToken();
        if (token) headers.Authorization = `Bearer ${token}`;
      }
      const response = await fetch(apiUrl("/api/assistant/context/stream"), {
        method: "POST",
        headers,
        credentials: "include",
        body: JSON.stringify({ area, title, context, focusText, question, history }),
      });
      if (!response.ok || !response.body) throw new ApiError(response.status, "Contextual AI stream failed");
      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";
      let reply = "";
      let done = false;
      while (!done) {
        const result = await reader.read();
        done = result.done;
        const { value } = result;
        buffer += decoder.decode(value, { stream: !done });
        const events = buffer.split("\n\n");
        buffer = events.pop() ?? "";
        for (const event of events) {
          const type = event.match(/^event:\s*(.+)$/m)?.[1];
          const data = event.match(/^data:\s*(.+)$/m)?.[1];
          if (type === "chunk" && data) {
            const chunk = (JSON.parse(data) as { text: string }).text;
            reply += chunk;
            onChunk(chunk);
          } else if (type === "error") {
            throw new ApiError(503, "Contextual AI unavailable", data);
          }
        }
      }
      return { reply };
    },
    sessions: {
      list: (cursor?: string, pageSize = 20) => {
        const params = new URLSearchParams({ pageSize: String(pageSize) });
        if (cursor) params.set("cursor", cursor);
        return get<import("./types").AssistantSessionPageDto>(`/api/assistant/sessions?${params}`);
      },
      create: (input: { skill: import("./types").AssistantSkill; resourceType: import("./types").AssistantResourceType; resourceId?: string | null; title: string }) =>
        post<import("./types").AssistantSessionDto>("/api/assistant/sessions", input),
      get: (sessionId: string) => get<import("./types").AssistantSessionDto>(`/api/assistant/sessions/${sessionId}`),
      rename: (sessionId: string, title: string) =>
        mutate<import("./types").AssistantSessionDto>("PATCH", `/api/assistant/sessions/${sessionId}`, { title }),
      delete: (sessionId: string) => del<void>(`/api/assistant/sessions/${sessionId}`),
      send: async (
        sessionId: string,
        question: string,
        context: string,
        focusText: string,
        clientRequestId: string,
        route?: string,
        stage?: string,
        onChunk?: (text: string) => void,
      ) => {
        const headers: Record<string, string> = { "Content-Type": "application/json", Accept: "text/event-stream" };
        if (isNativePlatform()) {
          const token = getAuthToken();
          if (token) headers.Authorization = `Bearer ${token}`;
        }
        const response = await fetch(apiUrl(`/api/assistant/sessions/${sessionId}/messages/stream`), {
          method: "POST",
          headers,
          credentials: "include",
          body: JSON.stringify({ question, context, focusText, clientRequestId, route, stage }),
        });
        if (!response.ok || !response.body) {
          throw new ApiError(response.status, "Assistant session stream failed");
        }
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = "";
        let streamEnded = false;
        const parseEvents = (flush = false) => {
          const parsed = readServerSentEvents(buffer, flush);
          buffer = parsed.remainder;
          for (const event of parsed.events) {
            if (event.type === "chunk" && event.data) {
              onChunk?.((JSON.parse(event.data) as { text: string }).text);
            }
            if (event.type === "done" && event.data) {
              return (JSON.parse(event.data) as { message: import("./types").AssistantMessageDto }).message;
            }
            if (event.type === "error") {
              const body = event.data ? JSON.parse(event.data) as RateLimitErrorBody : undefined;
              throw new ApiError(body?.code === "rate_limited" ? 429 : 503, "Assistant session unavailable", body);
            }
          }
          return null;
        };
        while (!streamEnded) {
          const result = await reader.read();
          streamEnded = result.done;
          buffer += decoder.decode(result.value, { stream: !streamEnded });
          const message = parseEvents(streamEnded);
          if (message) return message;
        }
        throw new ApiError(503, "Assistant session returned no message");
      },
    },
  },

  grammar: {
    // An optional level lets the learner browse the grammar topics of any CEFR level;
    // `allLevels` returns the whole A1→C2 catalog easiest-first.
    catalog: (learnerId: string, level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<GrammarTopicSummaryDto[]>(
        `/api/grammar/catalog/${learnerId}${q ? `?${q}` : ""}`
      );
    },
    // Keyed by the learning-spine topic id; the lesson is generated and cached on first open.
    lesson: (topicId: string) =>
      get<GrammarLessonDto>(`/api/grammar/topic/${topicId}`),
    checkExercise: (topicId: string, exerciseId: string, selectedOptionIndex: number, textAnswer?: string) =>
      post<GrammarExerciseCheckDto>("/api/grammar/exercises/check", {
        topicId,
        exerciseId,
        selectedOptionIndex,
        ...(textAnswer !== undefined ? { textAnswer } : {}),
      }),
    // The score is credited toward the topic's Grammar module in its mastery checklist (K.5).
    submitExercises: (
      topicId: string,
      learnerId: string,
      answers: GrammarExerciseAnswer[]
    ) =>
      post<GrammarExerciseResultDto>("/api/grammar/exercises", {
        topicId,
        learnerId,
        answers,
      }),
  },

  writing: {
    // No filter → adapted to the learner's level. `level` browses one CEFR band; `allLevels`
    // returns the whole catalog easiest-first (a progressively harder list across every level).
    // Each row is a learning-spine topic; the task is generated and cached when the topic opens.
    catalog: (learnerId: string, level?: CefrLevel, allLevels = false) => {
      const params = new URLSearchParams();
      if (level) params.set("level", String(level));
      if (allLevels) params.set("all", "true");
      const q = params.toString();
      return get<WritingTaskSummaryDto[]>(
        `/api/writing/catalog/${learnerId}${q ? `?${q}` : ""}`
      );
    },
    // Keyed by the learning-spine topic id; the task is generated and cached on first open.
    task: (topicId: string) =>
      get<WritingTaskDto>(`/api/writing/topic/${topicId}`),
    // The overall score is credited toward the topic's Writing module (K.5).
    submit: (learnerId: string, topicId: string, text: string) =>
      post<WritingAssessmentDto>("/api/writing/submit", {
        learnerId,
        topicId,
        text,
      }),
  },

  subscription: {
    /** Plans, prices and whether checkout is open. The only source the UI should read prices from. */
    plans: () => get<PlanCatalogDto>("/api/subscription/plans"),
    /**
     * Records interest while checkout is closed. Always resolves for a well-formed contact - the
     * server never reveals whether it was already on the list.
     */
    joinWaitlist: (request: JoinPremiumWaitlistRequest) =>
      post<void>("/api/subscription/waitlist", request),
    get: (learnerId: string) =>
      get<SubscriptionDto>(`/api/subscription/${learnerId}`),
    providers: () => get<PaymentProviderDto[]>("/api/subscription/payments/providers"),
    start: (learnerId: string, plan: SubscriptionPlan, provider: PaymentProvider, discountCode?: string) =>
      post<StartSubscriptionResultDto>(`/api/subscription/${learnerId}/start`, {
        plan,
        provider,
        returnUrl: `${window.location.origin}/profile?payment=success`,
        discountCode,
      }),
    cancel: (learnerId: string) =>
      post<SubscriptionDto>(`/api/subscription/${learnerId}/cancel`),
  },

  retention: {
    churn: (learnerId: string) =>
      get<ChurnAssessmentDto>(`/api/retention/${learnerId}/churn`),
  },

  // Operator admin panel. `access` is callable by any signed-in user (it just reports their own
  // standing); `users` requires an admin (403 otherwise) and `setAdmin` requires the super-admin.
  admin: {
    access: () => get<AdminAccessDto>("/api/admin/access"),
    supportConversations: (filter = "open", limit = 50) =>
      get<SupportConversationSummaryDto[]>(`/api/admin/support/conversations?${new URLSearchParams({ filter, limit: String(limit) })}`),
    supportConversation: (conversationId: string) =>
      get<SupportConversationDto>(`/api/admin/support/conversations/${conversationId}`),
    sendSupportMessage: (conversationId: string, text: string, files: File[]) => {
      const form = new FormData();
      if (text.trim()) form.append("text", text.trim());
      files.forEach((file) => form.append("files", file));
      return uploadPost<SupportMessageDto>(`/api/admin/support/conversations/${conversationId}/messages`, form);
    },
    assignSupportConversation: (conversationId: string) =>
      post<SupportConversationDto>(`/api/admin/support/conversations/${conversationId}/assign`),
    setSupportStatus: (conversationId: string, closed: boolean) =>
      post<SupportConversationDto>(`/api/admin/support/conversations/${conversationId}/status`, { closed }),
    markSupportRead: (conversationId: string) =>
      post<void>(`/api/admin/support/conversations/${conversationId}/read`),
    users: (cursor?: string, pageSize = 50) => {
      const params = new URLSearchParams({ pageSize: String(pageSize) });
      if (cursor) params.set("cursor", cursor);
      return get<AdminUsersDto>(`/api/admin/users?${params}`);
    },
    user: (targetId: string) => get<AdminUserDetailDto>(`/api/admin/users/${targetId}`),
    metrics: (from?: string, to?: string) => {
      const params = new URLSearchParams();
      if (from) params.set("from", from);
      if (to) params.set("to", to);
      const query = params.toString();
      return get<FounderMetricsDto>(`/api/admin/metrics${query ? `?${query}` : ""}`);
    },
    setVariableCostBudget: (dailyBudgetUsd: number) =>
      put<VariableCostSnapshotDto>("/api/admin/metrics/cost-budget", { dailyBudgetUsd }),
    metricsStream: (from: string, to: string, onMetrics: (data: FounderMetricsDto) => void, signal: AbortSignal) => {
      const params = new URLSearchParams({ from, to });
      return streamServerSentEvents(
        `/api/admin/metrics/stream?${params}`,
        { metrics: (payload) => onMetrics(payload as FounderMetricsDto) },
        signal,
      );
    },
    // Super-admin only: live server-operations snapshot (runtime, dependency health, recent errors).
    server: () => get<ServerDiagnosticsDto>("/api/admin/server"),
    serverStream: (
      onHistory: (data: ServerTelemetryHistoryDto) => void,
      onSnapshot: (data: ServerDiagnosticsDto) => void,
      signal: AbortSignal,
    ) => streamServerSentEvents(
      "/api/admin/server/stream",
      {
        history: (payload) => onHistory(payload as ServerTelemetryHistoryDto),
        snapshot: (payload) => onSnapshot(payload as ServerDiagnosticsDto),
      },
      signal,
    ),
    // Super-admin only: dismiss one recent warning/error line, or clear the whole buffer.
    deleteServerLog: (logId: string) =>
      del<{ deleted: boolean }>(`/api/admin/server/logs/${logId}`),
    clearServerLogs: () => del<{ removed: number }>("/api/admin/server/logs"),
    setAdmin: (targetId: string, isAdmin: boolean) =>
      put<SetUserAdminResultDto>(`/api/admin/users/${targetId}/admin`, {
        isAdmin,
      }),
    // Testing aid: makes every word in the admin's own "Mening so'zlarim" due for SRS review now.
    forceDueReviews: () =>
      post<ForceDueReviewsResultDto>("/api/admin/force-due-reviews"),
    // Super-admin only: the recently sent broadcasts, and composing/sending a new one to all learners.
    broadcasts: (cursor?: string, pageSize = 20) => {
      const params = new URLSearchParams({ pageSize: String(pageSize) });
      if (cursor) params.set("cursor", cursor);
      return get<import("./types").CursorPageDto<AdminBroadcastDto>>(`/api/admin/broadcasts?${params}`);
    },
    sendBroadcast: (title: string, body: string, linkUrl: string | null) =>
      post<AdminBroadcastDto>("/api/admin/broadcasts", {
        title,
        body,
        linkUrl,
      }),
    // Super-admin only: remove a previously sent broadcast from the history.
    deleteBroadcast: (id: string) =>
      del<{ deleted: boolean }>(`/api/admin/broadcasts/${id}`),
    // Super-admin only: manual "send now" fallback for the daily SRS reminders.
    dispatchDaily: () =>
      post<DispatchResultDto>("/api/admin/notifications/dispatch-daily"),
    // Vocabulary topic administration (Application.Admin.Dtos).
    vocabulary: {
      list: () => get<AdminVocabularyTopicDto[]>("/api/admin/vocabulary"),
      get: (id: string) =>
        get<AdminVocabularyTopicDto>(`/api/admin/vocabulary/${id}`),
      create: (dto: AdminVocabularyTopicUpsertDto) =>
        post<AdminVocabularyTopicDto>("/api/admin/vocabulary", dto),
      update: (id: string, dto: AdminVocabularyTopicUpsertDto) =>
        put<AdminVocabularyTopicDto>(`/api/admin/vocabulary/${id}`, dto),
      remove: (id: string) =>
        del<void>(`/api/admin/vocabulary/${id}`),
    },
    vocabularyImages: {
      list: () => get<AdminVocabularyImageDto[]>("/api/admin/vocabulary-images"),
      replace: (topicId: string, imageId: string) =>
        post<AdminVocabularyImageDto>(`/api/admin/vocabulary-images/${topicId}/${imageId}/replace`),
    },
    curriculum: {
      runs: () => get<AdminCurriculumRunDto[]>("/api/admin/curriculum/runs"),
      items: (runId: string, status?: string, module?: string) => {
        const params = new URLSearchParams();
        if (status) params.set("status", status);
        if (module) params.set("module", module);
        return get<AdminCurriculumItemDto[]>(`/api/admin/curriculum/runs/${runId}/items${params.size ? `?${params}` : ""}`);
      },
    },
    books: {
      list: () => get<AdminBookDto[]>("/api/admin/books"),
      get: (id: string) => get<AdminBookDto>(`/api/admin/books/${id}`),
      update: (id: string, dto: AdminBookDto) => put<AdminBookDto>(`/api/admin/books/${id}`, dto),
    },
    // Grammar lesson administration (Application.Grammar.Admin).
    grammar: {
      list: () => get<AdminGrammarLessonDto[]>("/api/admin/grammar"),
      get: (id: string) =>
        get<AdminGrammarLessonDetailDto>(`/api/admin/grammar/${id}`),
      create: (dto: AdminGrammarLessonUpsertDto) =>
        post<AdminGrammarLessonDto>("/api/admin/grammar", dto),
      update: (id: string, dto: AdminGrammarLessonUpsertDto) =>
        put<AdminGrammarLessonDto>(`/api/admin/grammar/${id}`, dto),
      updateFull: (id: string, dto: AdminGrammarLessonFullUpdateDto) =>
        put<AdminGrammarLessonDetailDto>(`/api/admin/grammar/${id}/full`, dto),
      remove: (id: string) => del<void>(`/api/admin/grammar/${id}`),
    },
    // Listening exercise administration (Application.Listening.Admin).
    listening: {
      list: () => get<AdminListeningExerciseDto[]>("/api/admin/listening"),
      get: (id: string) =>
        get<AdminListeningExerciseDetailDto>(`/api/admin/listening/${id}`),
      create: (dto: AdminListeningExerciseUpsertDto) =>
        post<AdminListeningExerciseDto>("/api/admin/listening", dto),
      update: (id: string, dto: AdminListeningExerciseUpsertDto) =>
        put<AdminListeningExerciseDto>(`/api/admin/listening/${id}`, dto),
      updateFull: (id: string, dto: AdminListeningExerciseFullUpdateDto) =>
        put<AdminListeningExerciseDetailDto>(`/api/admin/listening/${id}/full`, dto),
      remove: (id: string) => del<void>(`/api/admin/listening/${id}`),
    },
    // Reading passage administration (Application.Reading.Admin).
    reading: {
      list: () => get<AdminReadingPassageDto[]>("/api/admin/reading"),
      get: (id: string) =>
        get<AdminReadingPassageDetailDto>(`/api/admin/reading/${id}`),
      create: (dto: AdminReadingPassageUpsertDto) =>
        post<AdminReadingPassageDto>("/api/admin/reading", dto),
      update: (id: string, dto: AdminReadingPassageUpsertDto) =>
        put<AdminReadingPassageDto>(`/api/admin/reading/${id}`, dto),
      updateFull: (id: string, dto: AdminReadingPassageFullUpsertDto) =>
        put<AdminReadingPassageDetailDto>(`/api/admin/reading/${id}/full`, dto),
      remove: (id: string) => del<void>(`/api/admin/reading/${id}`),
    },
    // Writing task administration (Application.Writing.Admin).
    writing: {
      list: () => get<AdminWritingTaskDto[]>("/api/admin/writing"),
      get: (id: string) =>
        get<AdminWritingTaskDto>(`/api/admin/writing/${id}`),
      create: (dto: AdminWritingTaskUpsertDto) =>
        post<AdminWritingTaskDto>("/api/admin/writing", dto),
      update: (id: string, dto: AdminWritingTaskUpsertDto) =>
        put<AdminWritingTaskDto>(`/api/admin/writing/${id}`, dto),
      remove: (id: string) => del<void>(`/api/admin/writing/${id}`),
    },
  },

  images: {
    // The URL of one gallery image for a topic (slot 0 = cover). Pointed at by <img src>.
    topicUrl: (topicId: string, slot = 0) =>
      apiUrl(
        slot === 0
          ? `/api/images/topics/${topicId}`
          : `/api/images/topics/${topicId}/slots/${slot}`
      ),
    // The gallery manifest: which slots exist + their attribution (rule 12).
    topicManifest: (topicId: string) =>
      get<TopicImageManifestDto>(`/api/images/topics/${topicId}/manifest`),
  },

  // Live multiplayer quiz competitions ("Musobaqa"). The REST surface covers creation,
  // lobby/topic browsing, join, host-driven lifecycle (start/advance/finish) and answering;
  // the real-time flow (broadcasts + low-latency invoke) runs through CompetitionHubClient.
  competitions: {
    // Host creates a competition; returns the CompetitionDto carrying the accessCode.
    create: (body: {
      hostLearnerId: string;
      hostDisplayName: string;
      title: string;
      settings: CompetitionSettingsDto;
      topicIds: string[];
    }) => post<CompetitionDto>("/api/competitions", body),

    // Topics the host can pick slides from when configuring a competition.
    listTopics: () => get<SelectableTopicDto[]>("/api/competitions/topics"),

    // Full competition state for a viewer (the viewerId scoping lets the server omit other
    // participants' in-flight answers / show the accessCode only to the host).
    get: (competitionId: string, viewerLearnerId: string) =>
      get<CompetitionDto>(
        `/api/competitions/${competitionId}?viewerLearnerId=${encodeURIComponent(
          viewerLearnerId
        )}`
      ),

    // A learner joins an existing competition (accessCode required when the host set one).
    join: (
      competitionId: string,
      body: { learnerId: string; displayName: string; accessCode?: string }
    ) =>
      post<CompetitionDto>(`/api/competitions/${competitionId}/join`, body),

    // Host starts the competition; slides are generated and the lobby closes.
    start: (competitionId: string, hostLearnerId: string) =>
      post<CompetitionDto>(`/api/competitions/${competitionId}/start`, {
        hostLearnerId,
      }),

    // A participant submits their answer for the current slide.
    submitAnswer: (
      competitionId: string,
      body: {
        participantId: string;
        selectedOptionIndex: number;
        timeRatioRemaining: number;
      }
    ) =>
      post<CompetitionDto>(
        `/api/competitions/${competitionId}/answer`,
        body
      ),

    // Host advances to the next slide.
    advance: (competitionId: string, hostLearnerId: string) =>
      post<CompetitionDto>(`/api/competitions/${competitionId}/advance`, {
        hostLearnerId,
      }),

    // Host ends the competition; returns the final ranking + reward.
    finish: (competitionId: string, hostLearnerId: string) =>
      post<CompetitionResultDto>(`/api/competitions/${competitionId}/finish`, {
        hostLearnerId,
      }),
  },
};
