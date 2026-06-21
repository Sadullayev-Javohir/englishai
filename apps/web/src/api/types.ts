// Typed contracts mirroring the .NET Application-layer DTOs.
// The minimal API uses System.Text.Json web defaults: camelCase property names and
// enums serialized as their numeric values. These enums therefore use the exact same
// numeric values as the C# domain enums.

/** The signed-in Google user. `id` doubles as the learner id for every scoped endpoint. */
export interface AuthenticatedUserDto {
  id: string;
  email: string;
  displayName: string;
  /** The chosen public handle (lowercase). Null until the user picks one after sign-up. */
  username: string | null;
  pictureUrl: string | null;
  /** Whether the account already has a learner profile (placement done or a level chosen). */
  hasOnboarded: boolean;
  /** The name the AI tutor uses. Null until the learner answers the one-time prompt. */
  preferredName: string | null;
  /** The learner's onboarding goal (goal-based onboarding). Unspecified until chosen. */
  learningGoal: LearningGoal;
  birthDate: string | null;
  gender: Gender | null;
  acquisitionSource: AcquisitionSource | null;
  acquisitionSourceOther: string | null;
  hasCompletedDemographics: boolean;
}

export enum Gender {
  Male = 1,
  Female = 2,
}

export enum AcquisitionSource {
  Telegram = 1,
  Instagram = 2,
  Google = 3,
  AiAssistants = 4,
  FriendReferral = 5,
  Other = 6,
}

/** Response of POST /api/auth/google: the user plus the session token (used by native). */
export interface GoogleSignInResponse {
  user: AuthenticatedUserDto;
  token: string;
  expiresAt: string;
}

export interface AuthConfigDto {
  googleClientId: string;
}

export interface DeveloperApiKeyDto {
  id: string;
  name: string;
  prefix: string;
  createdAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
  isActive: boolean;
}

export interface CreatedDeveloperApiKeyDto {
  id: string;
  name: string;
  prefix: string;
  apiKey: string;
  createdAt: string;
}

/** The signed-in learner's referral standing (GET /api/referral). Mirrors ReferralStatusDto. */
export interface ReferralStatusDto {
  /** The learner's own shareable referral code. */
  code: string;
  /** Friends who signed up with the code (pending + qualified). */
  invitedCount: number;
  /** Friends who engaged and triggered a reward. */
  qualifiedCount: number;
  /** Lifetime cap on rewarded referrals. */
  rewardCap: number;
  /** Extra vocabulary topics earned (permanent). */
  bonusTopicsUnlocked: number;
  /** Bonus Speaking sessions still available. */
  remainingSpeakingCredits: number;
  /** Bonus Writing assessments still available. */
  remainingWritingCredits: number;
  /** Topics granted per qualified referral. */
  topicsPerReferral: number;
  /** Speaking credits granted per qualified referral. */
  speakingCreditsPerReferral: number;
  /** Writing credits granted per qualified referral. */
  writingCreditsPerReferral: number;
}

/** An account's admin standing (Application.Identity.Dtos.AdminRole). Serialized numerically. */
export enum AdminRole {
  None = 0,
  Admin = 1,
  SuperAdmin = 2,
}

/** The signed-in user's admin standing, used to gate the admin panel nav/route. */
export interface AdminAccessDto {
  role: AdminRole;
  isAdmin: boolean;
  canManageAdmins: boolean;
}

export enum SupportConversationStatus { Open = 0, Closed = 1 }
export enum SupportSenderKind { Learner = 0, Admin = 1 }

export interface SupportAttachmentDto {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
}

export interface SupportMessageDto {
  id: string;
  senderId: string;
  senderKind: SupportSenderKind;
  text: string | null;
  createdAt: string;
  isRead: boolean;
  attachments: SupportAttachmentDto[];
}

export interface SupportConversationDto {
  id: string;
  learnerId: string;
  learnerName: string;
  learnerEmail: string;
  learnerPictureUrl: string | null;
  assignedAdminId: string | null;
  assignedAdminName: string | null;
  status: SupportConversationStatus;
  createdAt: string;
  updatedAt: string;
  closedAt: string | null;
  unreadCount: number;
  messages: SupportMessageDto[];
}

export interface SupportConversationSummaryDto {
  id: string;
  learnerId: string;
  learnerName: string;
  learnerEmail: string;
  learnerPictureUrl: string | null;
  assignedAdminId: string | null;
  assignedAdminName: string | null;
  status: SupportConversationStatus;
  updatedAt: string;
  unreadCount: number;
  lastMessagePreview: string;
}

/** One row of the admin panel's user list (Application.Admin.Dtos.AdminUserDto). */
export interface AdminUserDto {
  id: string;
  email: string;
  displayName: string;
  username: string | null;
  pictureUrl: string | null;
  role: AdminRole;
  /** When the account registered. */
  registeredAt: string;
  /** When the account last signed in. */
  lastLoginAt: string;
  hasOnboarded: boolean;
  /** Current CEFR level (e.g. "A2"), or null before onboarding. */
  level: string | null;
  /** Last learning activity, or null if none yet. */
  lastActivityAt: string | null;
  /** "Free" | "Premium" | "Cancelled" | "Expired". */
  subscriptionStatus: string;
  subscriptionPlan: string | null;
  subscriptionExpiresAt: string | null;
}

/** The admin panel's user-list payload (Application.Admin.Dtos.AdminUsersDto). */
export interface AdminUsersDto {
  viewerRole: AdminRole;
  totalUsers: number;
  adminCount: number;
  onboardedCount: number;
  premiumCount: number;
  users: AdminUserDto[];
  nextCursor: string | null;
}

export interface AdminUserDetailDto extends AdminUserDto {
  preferredName: string | null;
  proTrialExpiresAt: string;
  isProTrialActive: boolean;
  learningGoal: string | null;
  birthDate: string | null;
  gender: string | null;
  acquisitionSource: string | null;
  acquisitionSourceOther: string | null;
  profileCreatedAt: string | null;
  profileUpdatedAt: string | null;
  confirmationTestPassedAt: string | null;
  lastWinBackStage: string | null;
  skillSeedCount: number;
  activityCount: number;
  errorObservationCount: number;
  subscriptionCreatedAt: string | null;
  subscriptionUpdatedAt: string | null;
  learning: {
    study: { todaySeconds:number; weekSeconds:number; monthSeconds:number; yearSeconds:number; totalSeconds:number; activeDays:number; currentDayStreak:number; averageSecondsPerActiveDay:number; longestDaySeconds:number; bySkill:Array<{ skill:string; seconds:number }> };
    gamification: { todayCompletedTasks:number; dailyGoalTarget:number; isGoalMet:boolean; currentStreak:number; longestStreak:number; isStreakAtRisk:boolean; skillsCompletedToday:string[] };
    skills: Array<{ skill:string; score:number; sampleCount:number; confidence:string; isPlacementBaseline:boolean; eightWeekDelta:number }>;
    errors: Array<{ category:string; countLast30Days:number; recentExamples:Array<{ id:string; skill:string; occurredAt:string; prompt:string|null; learnerAnswer:string|null; expectedAnswer:string|null; explanation:string|null }> }>;
    topics: { startedTopics:number; masteredTopics:number; passedModules:number };
    vocabulary: { total:number; learning:number; mastered:number; due:number; addedLast7Days:number; addedLast30Days:number; totalFailCount:number; stageBreakdown:Array<{ stage:string; count:number }> };
    churn: { overallRisk:string; isAtRisk:boolean; signals:Array<{ type:string; risk:string }> };
    registeredDeviceCount:number;
    devicePlatforms:string[];
  };
}

/** One vocabulary topic row in the admin panel (Application.Admin.Dtos.AdminVocabularyTopicDto). */
export interface AdminVocabularyTopicDto {
  id: string;
  slug: string;
  title: string;
  titleUz: string;
  category: string;
  grammarFocusCode: string;
  sequence: number;
  level: string;
  status: string;
  wordCount: number;
  passage: string;
  words: AdminVocabularyTopicWordDto[];
  createdAt: string;
}

export interface AdminVocabularyTopicWordDto {
  word: string;
  translation: string;
  exampleSentence: string | null;
  partOfSpeech: string;
  lexicalCategory: string;
  register: string | null;
  usageNote: string | null;
  imageUrl: string | null;
  imageSource: string | null;
  imageAttribution: string | null;
}

export interface AdminVocabularyImageDto {
  topicId: string;
  imageId: string;
  topicTitle: string;
  level: string;
  word: string;
  translation: string;
  imageUrl: string;
  imageSource: string | null;
  imageAttribution: string | null;
  version: number;
}

/** Create/update payload for a vocabulary topic (Application.Admin.Dtos.AdminVocabularyTopicUpsertDto). */
export interface AdminVocabularyTopicUpsertDto {
  slug?: string;
  title: string;
  titleUz: string;
  category: string;
  grammarFocusCode: string;
  level: string;
  sequence?: number;
  passage?: string;
  words?: AdminVocabularyTopicWordDto[];
}

export interface AdminCurriculumRunDto {
  id: string; version: string; provider: string; model: string; status: string;
  createdAt: string; updatedAt: string; completedAt: string | null; publishedAt: string | null;
  totalItems: number; approvedItems: number; publishedItems: number; problemItems: number;
}

export interface AdminCurriculumItemDto {
  id: string; module: string; subjectKey: string; level: string | null; status: string;
  attemptCount: number; errorCode: string | null; errorMessage: string | null; updatedAt: string;
}

export interface AdminBookQuestionDto { id: string; prompt: string; options: string[]; correctOptionIndex: number; explanation: string | null; }
export interface AdminBookSectionDto { id: string; order: number; title: string; status: string; body: string; questions: AdminBookQuestionDto[]; }
export interface AdminBookDto {
  id: string; title: string; titleUz: string; author: string; synopsis: string; topic: string; level: string;
  coverImageQuery: string; coverImageUrl: string | null; coverAttribution: string | null; sections: AdminBookSectionDto[];
}

/** One grammar lesson row in the admin panel (Application.Grammar.Admin.GrammarLessonAdminDto). */
export interface AdminGrammarLessonDto {
  id: string;
  title: string;
  category: string;
  level: string;
  status: string;
  exerciseCount: number;
  vocabularyTopicId: string | null;
  createdAt: string;
}

/** Create/update payload for a grammar lesson (Application.Grammar.Admin.GrammarLessonAdminUpsertDto). */
export interface AdminGrammarLessonUpsertDto {
  title: string;
  category: string;
  level: string;
}

export interface AdminGrammarLessonDetailDto extends AdminGrammarLessonDto {
  grammarFocusCode: string | null;
  contextIntro: string;
  explanation: string | null;
  curatedTitleUz: string | null;
  curatedSummaryUz: string | null;
  curatedFormulas: string[];
  curatedRules: Array<{ headingUz: string; bodyUz: string }>;
  examples: Array<{ english: string; uzbek: string }>;
  commonMistakes: Array<{ text: string }>;
  exercises: Array<{
    type: string;
    prompt: string;
    options: string[];
    correctOptionIndex: number;
    hintCode: string | null;
    explanation: string | null;
  }>;
  applicationTasks: Array<{ targetSkill: string; prompt: string }>;
}

export interface AdminGrammarLessonFullUpdateDto {
  title: string;
  category: string;
  level: string;
  status: string;
  vocabularyTopicId: string | null;
  grammarFocusCode: string | null;
  contextIntro: string;
  explanation: string | null;
  curatedTitleUz: string | null;
  curatedSummaryUz: string | null;
  curatedFormulas: string[];
  curatedRules: Array<{ headingUz: string; bodyUz: string }>;
  examples: AdminGrammarLessonDetailDto["examples"];
  commonMistakes: AdminGrammarLessonDetailDto["commonMistakes"];
  exercises: AdminGrammarLessonDetailDto["exercises"];
  applicationTasks: AdminGrammarLessonDetailDto["applicationTasks"];
}

/** One listening exercise row in the admin panel (Application.Listening.Admin.ListeningExerciseAdminDto). */
export interface AdminListeningExerciseDto {
  id: string;
  title: string;
  topic: string;
  level: string;
  status: string;
  questionCount: number;
  wordCount: number;
  vocabularyTopicId: string | null;
  createdAt: string;
}

/** Create/update payload for a listening exercise (Application.Listening.Admin.ListeningExerciseAdminUpsertDto). */
export interface AdminListeningExerciseUpsertDto {
  title: string;
  topic: string;
  level: string;
}

export interface AdminListeningQuestionDto {
  id: string;
  prompt: string;
  options: string[];
  correctOptionIndex: number;
  hintCode: string | null;
  explanation: string | null;
}

export interface AdminListeningSegmentDto {
  id: string;
  order: number;
  startMs: number;
  endMs: number;
  speaker: string;
  text: string;
}

export interface AdminListeningExerciseDetailDto extends AdminListeningExerciseDto {
  transcript: string;
  audio: {
    streamUrl: string;
    hasCachedAudio: boolean;
    sizeBytes: number | null;
    generatedAt: string | null;
    contentType: string;
  };
  segments: AdminListeningSegmentDto[];
  questions: AdminListeningQuestionDto[];
  vocabularyContext: Array<{
    word: string;
    translation: string;
    exampleSentence: string | null;
  }>;
}

export interface AdminListeningExerciseFullUpdateDto {
  title: string;
  topic: string;
  level: string;
  status: string;
  vocabularyTopicId: string | null;
  transcript: string;
  segments: Array<Omit<AdminListeningSegmentDto, "id"> & { id: string | null }>;
  questions: Array<Omit<AdminListeningQuestionDto, "id"> & { id: string | null }>;
}

/** One reading passage row in the admin panel (Application.Reading.Admin.ReadingPassageAdminDto). */
export interface AdminReadingPassageDto {
  id: string;
  title: string;
  topic: string;
  level: string;
  status: string;
  questionCount: number;
  wordCount: number;
  vocabularyTopicId: string | null;
  createdAt: string;
}

/** Create/update payload for a reading passage (Application.Reading.Admin.ReadingPassageAdminUpsertDto). */
export interface AdminReadingPassageUpsertDto {
  title: string;
  topic: string;
  level: string;
}

export interface AdminReadingVocabularyDto {
  id?: string;
  word: string;
  translation: string;
  exampleSentence: string | null;
}

export interface AdminReadingQuestionDto {
  id?: string;
  prompt: string;
  options: string[];
  correctOptionIndex: number;
  hintCode: string | null;
  explanation: string | null;
}

export interface AdminReadingPassageDetailDto {
  id: string;
  title: string;
  topic: string;
  category: string;
  level: string;
  status: string;
  body: string;
  sections: string[];
  contextGaps: string | null;
  vocabulary: AdminReadingVocabularyDto[];
  questions: AdminReadingQuestionDto[];
  vocabularyTopicId: string | null;
  createdAt: string;
}

export interface AdminReadingPassageFullUpsertDto {
  title: string;
  topic: string;
  category: string;
  level: string;
  status: string;
  sections: string[];
  contextGaps: string | null;
  vocabulary: Omit<AdminReadingVocabularyDto, "id">[];
  questions: Omit<AdminReadingQuestionDto, "id">[];
}

/** One writing task row in the admin panel (Application.Writing.Admin.WritingTaskAdminDto). */
export interface AdminWritingTaskDto {
  id: string;
  level: string;
  status: string;
  minWords: number;
  maxWords: number;
  promptSnippet: string;
  vocabularyTopicId: string | null;
  createdAt: string;
}

/** Create/update payload for a writing task (Application.Writing.Admin.WritingTaskAdminUpsertDto). */
export interface AdminWritingTaskUpsertDto {
  level: string;
}

/** Process/runtime vitals of the server (Application.Admin.Dtos.ServerRuntimeDto). */
export interface ServerRuntimeDto {
  environment: string;
  version: string;
  framework: string;
  operatingSystem: string;
  machineName: string;
  /** ISO timestamp the process started. */
  startedAtUtc: string;
  uptimeSeconds: number;
  processorCount: number;
  managedMemoryMb: number;
  workingSetMb: number;
  threadCount: number;
  gen0Collections: number;
  gen1Collections: number;
  gen2Collections: number;
}

/** One backing-store health probe (Application.Admin.Dtos.DependencyHealthDto). */
export interface DependencyHealthDto {
  name: string;
  /** "Healthy" | "Unhealthy" | "NotConfigured". */
  status: string;
  detail: string;
  latencyMs: number | null;
}

/** Whether an external integration is configured (Application.Admin.Dtos.ExternalServiceDto). */
export interface ExternalServiceDto {
  name: string;
  configured: boolean;
  detail: string;
}

/** One captured log line (Application.Admin.Dtos.LogEntryDto). */
export interface LogEntryDto {
  /** Per-entry handle used to dismiss a single line from the buffer. */
  id: string;
  timestampUtc: string;
  /** "Warning" | "Error" | "Fatal". */
  level: string;
  message: string;
  exception: string | null;
  sourceContext: string | null;
  requestPath: string | null;
  correlationId: string | null;
  traceId: string | null;
}

/** Recent-log rollup (Application.Admin.Dtos.ServerLogSummaryDto). */
export interface ServerLogSummaryDto {
  warningCount: number;
  errorCount: number;
  capacity: number;
  recent: LogEntryDto[];
}

/** The super-admin server-operations snapshot (Application.Admin.Dtos.ServerDiagnosticsDto). */
export interface ServerDiagnosticsDto {
  /** ISO timestamp the snapshot was taken. */
  generatedAtUtc: string;
  /** "Healthy" | "Degraded" | "Unhealthy" - the worst dependency status. */
  overallStatus: string;
  runtime: ServerRuntimeDto;
  dependencies: DependencyHealthDto[];
  services: ExternalServiceDto[];
  logs: ServerLogSummaryDto;
}

export interface ServerTelemetryPointDto {
  timestampUtc: string;
  managedMemoryMb: number;
  workingSetMb: number;
  threadCount: number;
  gen0Collections: number;
  gen1Collections: number;
  gen2Collections: number;
  warningCount: number;
  errorCount: number;
  dependencyLatenciesMs: Record<string, number | null>;
}

export interface ServerTelemetryHistoryDto {
  current: ServerDiagnosticsDto;
  history: ServerTelemetryPointDto[];
  sampleIntervalSeconds: number;
  retentionMinutes: number;
}

/** One point on a daily time-series (Application.Analytics.Dtos.DailyCountDto). */
export interface DailyCountDto {
  /** ISO date "YYYY-MM-DD". */
  day: string;
  count: number;
}

/** A single day-N retention bracket (Application.Analytics.Dtos.RetentionPointDto). */
export interface RetentionPointDto {
  cohortSize: number;
  retained: number;
  ratePct: number;
}

/** Founder dashboard headline cards (Application.Analytics.Dtos.FounderOverviewDto). */
export interface FounderOverviewDto {
  totalUsers: number;
  onboardedUsers: number;
  premiumUsers: number;
  freeUsers: number;
  conversionRatePct: number;
  dau: number;
  wau: number;
  mau: number;
  stickinessPct: number;
  newUsersToday: number;
  newUsers7d: number;
  mrrUzs: number;
  arppuUzs: number;
}

/** One sequential activation funnel milestone (Application.Analytics.Dtos.ActivationFunnelStepDto). */
export interface ActivationFunnelStepDto {
  code: string;
  count: number;
  rateFromRegisteredPct: number;
  rateFromPreviousPct: number;
}

/** Active-premium subscriptions grouped by plan (Application.Analytics.Dtos.PlanBreakdownDto). */
export interface PlanBreakdownDto {
  plan: string;
  count: number;
  mrrUzs: number;
}

/** One onboarding-goal segment (Application.Analytics.Dtos.GoalSegmentDto). */
export interface GoalSegmentDto {
  /** The LearningGoal name, e.g. "IeltsCefr". */
  goal: string;
  users: number;
  premiumUsers: number;
  conversionRatePct: number;
  d7: RetentionPointDto;
  mrrUzs: number;
}

/** The founder growth dashboard payload (Application.Analytics.Dtos.FounderMetricsDto). */
export interface FounderMetricsDto {
  /** ISO date the figures are computed as-of. */
  asOf: string;
  overview: FounderOverviewDto;
  activeUsersTrend: DailyCountDto[];
  signupsTrend: DailyCountDto[];
  retention: {
    d1: RetentionPointDto;
    d7: RetentionPointDto;
    d30: RetentionPointDto;
  };
  funnel: {
    registered: number;
    onboarded: number;
    active7d: number;
    premium: number;
  };
  activationFunnel: ActivationFunnelStepDto[];
  goalSegments: GoalSegmentDto[];
  planBreakdown: PlanBreakdownDto[];
  demographics: {
    completed: number;
    missing: number;
    completionRatePct: number;
    gender: DemographicBreakdownDto[];
    acquisitionSources: DemographicBreakdownDto[];
    ageGroups: DemographicBreakdownDto[];
  };
  variableCosts?: VariableCostSnapshotDto | null;
  canManageVariableCostBudget: boolean;
}

export interface VariableCostSnapshotDto {
  day: string;
  dailyBudgetUsedUsd: number;
  dailyBudgetLimitUsd: number;
  dailyBudgetWarning: boolean;
  freeTierShed: boolean;
  pricingConfigured: boolean;
  categories: VariableCostCategorySnapshotDto[];
  topCallers: VariableCostAttributionSnapshotDto[];
  topEndpoints: VariableCostAttributionSnapshotDto[];
}

export interface VariableCostCategorySnapshotDto {
  category: string;
  requests: number;
  units: number;
  unit: string;
  estimatedCostUsd: number;
}

export interface VariableCostAttributionSnapshotDto {
  key: string;
  requests: number;
  estimatedCostUsd: number;
}

export interface DemographicBreakdownDto {
  label: string;
  count: number;
  ratePct: number;
}

/** Anonymous aggregate figures displayed as landing-page social proof. */
export interface PublicSocialProofMetricsDto {
  asOf: string;
  registeredUsers: number;
  activePremiumUsers: number;
  activeLearners30d: number;
  totalStudyMinutes: number;
  speakingSessions: number;
  speakingMinutes: number;
}

/** Result of a super-admin's promote/demote (Application.Admin.SetUserAdmin.SetUserAdminResultDto). */
export interface SetUserAdminResultDto {
  id: string;
  role: AdminRole;
}

/** Result of the admin "force my saved words due for SRS review now" testing aid
 * (Application.Admin.ForceDueReviews.ForceDueReviewsResultDto). */
export interface ForceDueReviewsResultDto {
  totalWords: number;
  madeDue: number;
  dueNow: number;
  seededWords: number;
}

/** Product activation event codes (Domain.Analytics.ProductEventType). */
export enum ProductEventType {
  Registered = 1,
  SignedIn = 2,
  UsernameSetupCompleted = 3,
  PlacementStarted = 4,
  PlacementCompleted = 5,
  TopicOpened = 6,
  SpeakingSessionCompleted = 7,
  PronunciationFeedbackViewed = 8,
  PaywallHit = 9,
  UpgradeClicked = 10,
  PaymentCompleted = 11,
  OnboardingGoalSelected = 12,
}

/**
 * Why the learner is studying English (goal-based onboarding). Numeric values mirror
 * Domain.Common.LearningGoal - keep in sync; sent numerically over the wire.
 */
export enum LearningGoal {
  Unspecified = 0,
  IeltsCefr = 1,
  Work = 2,
  Migration = 3,
  Travel = 4,
  GeneralSpeaking = 5,
  School = 6,
}

/** One goal-tailored topic suggestion (GET /api/learning/{id}/recommended-topics). */
export interface RecommendedTopicDto {
  id: string;
  title: string;
  titleUz: string;
  category: string;
  level: CefrLevel;
  isGoalRelevant: boolean;
}

/** The learner's goal + goal-ordered topic suggestions for the home strip. */
export interface RecommendedTopicsDto {
  goal: LearningGoal;
  level: CefrLevel;
  topics: RecommendedTopicDto[];
}

/** Mirrors Domain.Identity.UserAccount username rules - keep in sync with UsernameRules.cs. */
export const USERNAME_MIN_LENGTH = 3;
export const USERNAME_MAX_LENGTH = 30;
export const USERNAME_PATTERN = /^[a-z0-9_.]+$/;

/** Result of the live username check (GET /api/auth/username-available). */
export interface UsernameAvailabilityDto {
  isValidFormat: boolean;
  isAvailable: boolean;
}

export enum DailyGoal {
  Calm = 1,
  Normal = 2,
  Intense = 3,
}

export enum LanguageBalance {
  Uzbek = 1,
  Bilingual = 2,
  English = 3,
}

/** Durable account preferences shown/saved on the Profile screen. */
export interface UserPreferencesDto {
  dailyGoal: DailyGoal;
  languageBalance: LanguageBalance;
  emailNotifications: boolean;
  pushNotifications: boolean;
}

export enum CefrLevel {
  A1 = 1,
  A2 = 2,
  B1 = 3,
  B2 = 4,
  C1 = 5,
  C2 = 6,
}

export const CefrLevelName: Record<CefrLevel, string> = {
  [CefrLevel.A1]: "A1",
  [CefrLevel.A2]: "A2",
  [CefrLevel.B1]: "B1",
  [CefrLevel.B2]: "B2",
  [CefrLevel.C1]: "C1",
  [CefrLevel.C2]: "C2",
};

// Values must match the backend Domain.Assessment.TestStage enum (1-based; serialized
// as numbers by the API).
export enum TestStage {
  Vocabulary = 1,
  Grammar = 2,
  Listening = 3,
  Reading = 4,
  Writing = 5,
  Speaking = 6,
}

export enum SkillType {
  Speaking = 1,
  Listening = 2,
  Reading = 3,
  Writing = 4,
  Grammar = 5,
  Vocabulary = 6,
}

export enum ErrorCategory {
  Articles = 1,
  VerbTense = 2,
  Prepositions = 3,
  GerundInfinitive = 4,
  Modals = 5,
  SubjectVerbAgreement = 6,
  WordOrder = 7,
  Pronunciation = 8,
  Vocabulary = 9,
  Spelling = 10,
  Other = 99,
}

export enum ReviewStage {
  Day3 = 0,
  Day7 = 1,
  Day21 = 2,
  Mastered = 3,
}

export enum MiniTestType {
  ClozeChoice = 0,
  WrittenUsage = 1,
  SpokenUsage = 2,
  ListeningRecognition = 3,
}

export enum VocabularySource {
  Manual = 0,
  Speaking = 1,
  Video = 2,
  Reading = 3,
}

export enum PronunciationBand {
  Good = 0,
  NeedsImprovement = 1,
}

export enum PronunciationErrorType {
  None = 0,
  Mispronunciation = 1,
  Omission = 2,
  Insertion = 3,
}

export enum SubscriptionStatus {
  Free = 0,
  Premium = 1,
  Expired = 2,
  Cancelled = 3,
}

export enum SubscriptionPlan {
  Monthly = 0,
  Yearly = 1,
  Quarterly = 2,
  SemiAnnual = 3,
}

export enum PaymentProvider {
  Local = 0,
  Click = 1,
  Payme = 2,
  Uzum = 3,
}

export enum IngestionStatus {
  Pending = 0,
  Leveled = 1,
}

// State of a lesson's interactive transcript fill (mirrors Domain.Video.TranscriptStatus).
// Lets the player tell "still preparing" (poll) from "this video has no transcript" (terminal).
export enum TranscriptStatus {
  Pending = 0,
  Available = 1,
  Unavailable = 2,
  // Some lines are present but the fill is still streaming the rest of a long video's transcript;
  // the player shows what it has and keeps polling for the remainder.
  Partial = 3,
}

export enum GrammarExerciseType {
  Recognition = 1,
  FillInBlank = 2,
  Rephrase = 3,
}

// Values match the backend Domain.Writing.WritingDimension enum (serialized numerically).
export enum WritingDimension {
  TaskAchievement = 1,
  Coherence = 2,
  LexicalResource = 3,
  GrammaticalAccuracy = 4,
}

// Values match the backend Domain.Speaking.RoleplayDimension enum (serialized numerically).
// The four dimensions an end-of-scene roleplay performance is scored on.
export enum RoleplayDimension {
  TaskCompletion = 1,
  Fluency = 2,
  Grammar = 3,
  Appropriateness = 4,
}

export enum ChurnRiskLevel {
  None = 0,
  Medium = 1,
  High = 2,
  VeryHigh = 3,
}

// ── Placement ──────────────────────────────────────────────────────────────
// Matches the backend Application.Assessment.Dtos.PlacementItemKind enum.
export enum PlacementItemKind {
  MultipleChoice = 0,
  Writing = 1,
  Speaking = 2,
}

export interface PlacementItemDto {
  kind: PlacementItemKind;
  id: string;
  stage: TestStage;
  difficulty: CefrLevel;
  // MCQ: the question prompt. Productive: the task instruction (English content).
  prompt: string;
  // MCQ only.
  options: string[] | null;
  // True for listening items: the clip is streamed from the audio endpoint and the
  // spoken script is deliberately not sent as text.
  hasAudio: boolean;
  // Reading MCQ: the passage shown above the question.
  passageText: string | null;
  // Productive tasks: expected word-count guidance.
  minWords: number | null;
  maxWords: number | null;
  // Server-authoritative progress for this exact adaptive session.
  stageNumber: number;
  stageCount: number;
  itemNumberInStage: number;
  itemsInStage: number;
  completedItems: number;
  totalItems: number;
}

export interface StartPlacementTestResult {
  sessionId: string;
  currentStage: TestStage;
  firstItem: PlacementItemDto | null;
}

export interface PlacementIntegrityViolationResult {
  violationCount: number;
  invalidated: boolean;
}

export interface ResumePlacementTestResult {
  sessionId: string;
  isCompleted: boolean;
  currentItem: PlacementItemDto | null;
}

export interface SubmitAnswerResult {
  wasCorrect: boolean;
  isTestCompleted: boolean;
  currentStage: TestStage;
  currentDifficulty: CefrLevel;
  nextItem: PlacementItemDto | null;
}

export interface SubmitWritingResult {
  score: number;
  level: CefrLevel;
  isTestCompleted: boolean;
  currentStage: TestStage;
  currentDifficulty: CefrLevel;
  nextItem: PlacementItemDto | null;
}

export enum PlacementSpeakingOutcome {
  Scored = 0,
  OffTopic = 1,
  InvalidAudio = 2,
  NoSpeech = 3,
  LowConfidence = 4,
  ServiceUnavailable = 5,
}

export interface SubmitSpeakingResult {
  score: number;
  level: CefrLevel;
  outcome: PlacementSpeakingOutcome;
  retryable: boolean;
  isTestCompleted: boolean;
  currentStage: TestStage;
  currentDifficulty: CefrLevel;
  nextItem: PlacementItemDto | null;
}

export interface StageResultDto {
  stage: TestStage;
  level: CefrLevel;
  score: number;
}

export interface PlacementResultDto {
  overallLevel: CefrLevel;
  overallScore: number;
  stageResults: StageResultDto[];
}

// ── Gamification ───────────────────────────────────────────────────────────
export interface GamificationStatusDto {
  todayCompletedTasks: number;
  dailyGoalTarget: number;
  isGoalMet: boolean;
  currentStreak: number;
  longestStreak: number;
  isStreakAtRisk: boolean;
  /** Canonical SkillType names (e.g. "Vocabulary") of the core skills practiced today -
   * drives the Home "kunlik reja" 6-skill plan checklist. */
  skillsCompletedToday: string[];
}

/** A single leaderboard row, ready for display. */
export interface LeaderboardEntryDto {
  rank: number;
  learnerId: string;
  displayName: string;
  pictureUrl: string | null;
  score: number;
  isPremium: boolean;
  isCurrentUser: boolean;
}

/** One CEFR level's leaderboard: top 50 plus the viewer's own row when outside of it. */
export interface LeaderboardDto {
  level: CefrLevel;
  top: LeaderboardEntryDto[];
  currentUserEntry: LeaderboardEntryDto | null;
}

/** One coin-for-discount tier, annotated with whether the learner can afford it now. */
export interface DiscountTierDto {
  coinsCost: number;
  discountPercent: number;
  canAfford: boolean;
}

/** An active (unused, unexpired) discount coupon the learner has already redeemed. */
export interface RedemptionDto {
  code: string;
  discountPercent: number;
  expiresAt: string;
}

/** The learner's points ledger, the discount catalog, and their active coupons. */
export interface PointsBalanceDto {
  lifetimeXp: number;
  spendableCoins: number;
  tiers: DiscountTierDto[];
  activeRedemptions: RedemptionDto[];
}

export interface EnergyDto {
  current: number;
  maximum: number;
  nextRefillAt: string | null;
  fullRefillAt: string | null;
  outcome: EnergyOutcome;
}

/** Mirrors Application.Gamification.Dtos.EnergyOutcome. */
export enum EnergyOutcome {
  None = 0,
  Consumed = 1,
  AlreadyStarted = 2,
  Insufficient = 3,
}

export type EnergyAction = "video" | "speaking";

// ── Learning ───────────────────────────────────────────────────────────────
export interface SkillScoreDto {
  skill: SkillType;
  score: number;
  sampleCount: number;
}

export interface ErrorHeatmapEntryDto {
  category: ErrorCategory;
  count: number;
}

export interface LevelStatusDto {
  isEligible: boolean;
  masteredSkillCount: number;
  requiredMasteredSkills: number;
  confirmationTestPassed: boolean;
  atMaxLevel: boolean;
}

export interface LearnerOverviewDto {
  learnerId: string;
  overallLevel: CefrLevel;
  skillScores: SkillScoreDto[];
  errorHeatmap: ErrorHeatmapEntryDto[];
  levelStatus: LevelStatusDto;
}

export interface RecommendationDto {
  code: string;
  text: string | null;
  skill: SkillType | null;
  category: ErrorCategory | null;
}

export interface GrowthPointDto {
  weekEnding: string;
  skill: SkillType;
  score: number;
}

export enum ProgressDataConfidence {
  Baseline = 0,
  Low = 1,
  Medium = 2,
  High = 3,
}

export enum ProgressInsightSource {
  Hermes = 0,
  Local = 1,
}

export enum WritingAssessmentSource {
  Hermes = 0,
  LocalFallback = 1,
}

export interface ProgressSkillSnapshotDto {
  skill: SkillType;
  score: number;
  sampleCount: number;
  confidence: ProgressDataConfidence;
  isPlacementBaseline: boolean;
  eightWeekDelta: number;
  last8Weeks: GrowthPointDto[];
}

export interface ProgressErrorSnapshotDto {
  category: ErrorCategory;
  countLast30Days: number;
  recentExamples: ProgressErrorDetailDto[];
}

export interface ProgressErrorDetailDto {
  id: string;
  category: ErrorCategory;
  skill: SkillType;
  occurredAt: string;
  source: string | null;
  sourceId: string | null;
  prompt: string | null;
  learnerAnswer: string | null;
  expectedAnswer: string | null;
  explanation: string | null;
}

export interface VocabularyStageCountDto {
  stage: ReviewStage;
  count: number;
}

export interface VocabularySummaryDto {
  total: number;
  learning: number;
  mastered: number;
  due: number;
  addedLast7Days: number;
  addedLast30Days: number;
  totalFailCount: number;
  stageBreakdown: VocabularyStageCountDto[];
}

export interface TopicProgressSummaryDto {
  started: number;
  mastered: number;
  modulesPassed: number;
}

export interface ProgressSnapshotDto {
  learnerId: string;
  asOfDate: string;
  overallLevel: CefrLevel | null;
  levelStatus: LevelStatusDto | null;
  skills: ProgressSkillSnapshotDto[];
  errorsLast30Days: ProgressErrorSnapshotDto[];
  topicProgress: TopicProgressSummaryDto;
  studyTime: StudyStatsDto;
  vocabulary: VocabularySummaryDto;
  gamification: GamificationStatusDto;
  hasLearningProfile: boolean;
}

export interface ProgressSkillInsightDto {
  skill: SkillType;
  priority: number;
  evidenceCode: string;
  actionCode: string;
  score: number;
  eightWeekDelta: number;
  sampleCount: number;
  confidence: ProgressDataConfidence;
}

export interface ProgressErrorInsightDto {
  category: ErrorCategory;
  priority: number;
  actionCode: string;
  countLast30Days: number;
}

export interface ProgressInsightAnalysisDto {
  overallCode: string;
  achievementCodes: string[];
  skillsToStrengthen: ProgressSkillInsightDto[];
  recurringErrors: ProgressErrorInsightDto[];
  habitCode: string;
  nextActionCode: string;
  targetRoute: string;
  source: ProgressInsightSource;
  isFallback: boolean;
}

export interface ProgressInsightDto {
  snapshot: ProgressSnapshotDto;
  insight: ProgressInsightAnalysisDto;
  generatedAt: string;
}

export interface ProgressDashboardDto {
  studyStats: StudyStatsDto;
  progressInsight: ProgressInsightDto;
  overview: LearnerOverviewDto;
  growth: GrowthPointDto[];
  recommendations: RecommendationDto[];
  gamification: GamificationStatusDto;
  levelMap: LevelMapDto;
}

/** Safe learner progress projection shown to authenticated leaderboard viewers. */
export interface PublicLearnerProgressDto {
  learnerId: string;
  displayName: string;
  pictureUrl: string | null;
  isPremium: boolean;
  lifetimeXp: number;
  rank: number | null;
  overallLevel: CefrLevel;
  skillScores: SkillScoreDto[];
  topicsTotal: number;
  topicsLearned: number;
  topicsMastered: number;
  studyStats: StudyStatsDto;
  gamification: GamificationStatusDto;
  growth: GrowthPointDto[];
}

// ── Study-time statistics (progress dashboard) ──────────────────────────────
/** One day's total study seconds (`day` is an ISO date, e.g. "2026-06-25"). */
export interface DailyStudyBucketDto {
  day: string;
  seconds: number;
}

export interface MonthlyStudyBucketDto {
  year: number;
  month: number;
  seconds: number;
}

export interface SkillStudyBucketDto {
  skill: SkillType;
  seconds: number;
}

export interface StudyStatsDto {
  todaySeconds: number;
  weekSeconds: number;
  monthSeconds: number;
  yearSeconds: number;
  totalSeconds: number;
  activeDays: number;
  currentDayStreak: number;
  averageSecondsPerActiveDay: number;
  longestDaySeconds: number;
  last7Days: DailyStudyBucketDto[];
  last12Months: MonthlyStudyBucketDto[];
  bySkill: SkillStudyBucketDto[];
  yearHeatmap: DailyStudyBucketDto[];
}

// ── Speaking ───────────────────────────────────────────────────────────────
export interface VisemeFrameDto {
  visemeId: number;
  offsetMs: number;
}

export interface SpeechWordTimingDto {
  text: string;
  textOffset: number;
  wordLength: number;
  audioOffsetMs: number;
  durationMs: number;
}

export interface PhonemePronunciationDto {
  phoneme: string;
  accuracyScore: number;
}

export interface WordPronunciationDto {
  word: string;
  spokenForm: string | null;
  accuracyScore: number;
  errorType: PronunciationErrorType;
  needsPractice: boolean;
  phonemes: PhonemePronunciationDto[];
}

export interface PronunciationResultDto {
  overallScore: number;
  accuracyScore: number;
  fluencyScore: number;
  completenessScore: number;
  band: PronunciationBand;
  isAuthentic: boolean;
  words: WordPronunciationDto[];
}

export interface AccentTutorMessageDto {
  role: "learner" | "tutor";
  text: string;
}

export interface AccentTutorStartResult {
  text: string;
  tutorAudioBase64: string;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
}

export interface AccentTutorTurnResult {
  transcript: string;
  tutorText: string;
  tutorAudioBase64: string;
  isNaturalVoice: boolean;
  pronunciation: PronunciationResultDto;
  wordTimings: SpeechWordTimingDto[];
}

export interface AccentTutorAttemptScoreDto {
  overallScore: number;
  accuracyScore: number;
  fluencyScore: number;
  completenessScore: number;
}

export interface AccentTutorEvaluationResult {
  evaluable: boolean;
  overallScore: number;
  accuracyScore: number;
  fluencyScore: number;
  completenessScore: number;
  feedback: string;
  strongestSkill: string;
  focusSkill: string;
}

export interface PhonemeVisualDto {
  phoneme: string;
  svgId: string;
  isHardForUzbek: boolean;
}

export interface WordPronunciationDetailDto {
  word: string;
  spokenForm: string | null;
  ipa: string;
  phonemes: PhonemeVisualDto[];
  tipUz: string | null;
  // Viseme ids/offsets for the whole word (timing only).
  visemes: VisemeFrameDto[];
  // The official Azure red-lips SVG for the whole word (synthesized once and cached
  // server-side): one self-contained SVG the renderer injects and plays. Null when the
  // engine produced viseme ids only (e.g. a non-en-US voice, which has no SVG output).
  visemeAnimation: string | null;
  // The Azure reference voice for the whole word as a base64 WAV. Played directly by the
  // detail screen so audio works even where the browser's built-in speech synthesis has no
  // installed voices (common on Linux/Chromium). Null only on the offline fallback.
  audioBase64: string | null;
  // Server-side extras (added by the backend sub-agent). Optional so the frontend
  // compiles even before the backend ships these fields.
  keyWord?: string | null;
  exampleSentenceUz?: string | null;
}

/**
 * Progress toward learning a vocabulary topic by speaking about it (the 5-minute rule).
 * Present only when the conversation is linked to a topic; null for a free conversation.
 */
export interface TopicSpeakingProgressDto {
  /** Cumulative engaged-speaking time for the topic so far, in seconds. */
  spokenSeconds: number;
  /** Speaking time needed to learn the topic (300 = 5 minutes). */
  goalSeconds: number;
  /** Whether the topic is now learned (goal reached). */
  learned: boolean;
  /** True only on the utterance that crossed the goal - for a one-off celebration. */
  justLearned: boolean;
}

export interface StartConversationResult {
  sessionId: string;
  tutorText: string;
  tutorAudioBase64: string;
  visemes: VisemeFrameDto[];
  visemeAnimation: string | null;
  topicProgress: TopicSpeakingProgressDto | null;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
}

/**
 * One "idea card" for the blank-page helper: a concrete talking-point `prompt` and a sentence
 * `starter` (both English - the target language), plus an `emoji` used as the card's picture when the
 * client has no richer topic image. The surrounding Uzbek labels come from the content store (rule 11).
 */
export interface IdeaCardDto {
  prompt: string;
  starter: string;
  emoji: string;
}

export interface IdeaCardsResult {
  cards: IdeaCardDto[];
}

export interface SubmitUtteranceResult {
  /** False when speech-to-text returned nothing; the other fields are then empty. */
  recognized: boolean;
  recognizedText: string;
  pronunciation: PronunciationResultDto | null;
  tutorText: string;
  tutorAudioBase64: string;
  visemes: VisemeFrameDto[];
  visemeAnimation: string | null;
  feedbackUz: string | null;
  focusWord: string | null;
  topicProgress: TopicSpeakingProgressDto | null;
  /** The topic's six-module mastery checklist (K.5) after this turn; null for a free conversation. */
  completion: TopicCompletionDto | null;
  /** True once engaged-speaking time hit the per-conversation cap - the client ends the sitting. */
  sessionLimitReached: boolean;
  /**
   * True when the learner seemed to introduce or spell their name but the tutor still has no name
   * on file. Spoken names (spelled out, or Uzbek) are what STT garbles, so the client shows a small
   * input for the learner to type it; once saved the tutor uses it from the next turn.
   */
  namePrompt: boolean;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
  rejectionCode: SpeakingRejectionCode | null;
  requiresTranscriptConfirmation: boolean;
  transcriptAlternatives: SpeakingTranscriptAlternative[];
}

export interface SpeakingTranscriptAlternative {
  text: string;
  confidence: number;
}

export type SpeakingRejectionCode =
  | "invalid_audio"
  | "no_speech"
  | "low_confidence"
  | "service_failure";

export interface SpeakingRecognizedEvent {
  text: string;
}

export interface SpeakingTutorEvent {
  text: string;
  sessionLimitReached: boolean;
  namePrompt: boolean;
  topicProgress: TopicSpeakingProgressDto | null;
  completion: TopicCompletionDto | null;
}

export interface SpeakingPronunciationEvent {
  pronunciation: PronunciationResultDto;
  feedbackUz: string | null;
  focusWord: string | null;
}

export interface SpeakingAudioEvent {
  audioBase64: string;
  visemes: VisemeFrameDto[];
  visemeAnimation: string | null;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
}

export interface SpeakingUnrecognizedEvent {
  feedbackUz: string | null;
  rejectionCode: SpeakingRejectionCode;
}

export interface SpeakingTranscriptConfirmationEvent {
  suggestedText: string;
  alternatives: SpeakingTranscriptAlternative[];
}

export type SpeakingTranscriptConfirmationOutcome = "candidate_selected" | "edited";

export interface SpeakingTutorUnavailableEvent {
  code: string;
  message: string;
  retryable: boolean;
}

export interface SpeakingUtteranceStreamHandlers {
  onRecognized: (event: SpeakingRecognizedEvent) => void;
  onTutor: (event: SpeakingTutorEvent) => void;
  onPronunciation: (event: SpeakingPronunciationEvent) => void;
  onAudio: (event: SpeakingAudioEvent) => void;
  onProgress: (event: SpeakingTutorEvent) => void;
  onUnrecognized: (event: SpeakingUnrecognizedEvent) => void;
  onTranscriptConfirmationRequired?: (event: SpeakingTranscriptConfirmationEvent) => void;
  onTutorUnavailable: (event: SpeakingTutorUnavailableEvent) => void;
}

export interface SpeakingLiveCapabilities {
  enabled: boolean;
}

/** One free-talk conversation subject shown under "Erkin suhbat" (20 per CEFR level). The Uzbek
 *  label comes from the content store keyed by `code` (rule 11); `englishTitle` is a reference. The
 *  `code` is sent back to /api/speaking/start as the conversation topic. */
export interface FreeTalkTopicDto {
  /** Stable snake_case code (e.g. "my_family") - keys the Uzbek content-store labels and is the
   *  conversation topic sent to the tutor. */
  code: string;
  englishTitle: string;
  /** CEFR level this topic is pitched at. */
  level: CefrLevel;
  /** Keys the topic's stored illustration: /api/images/topics/{imageId} (rule 12). */
  imageId: string;
}

// ── Roleplay (speaking scenarios) ────────────────────────────────────────────
// A roleplay is a Speaking session with a fixed persona (interviewer, waiter, doctor, …). The
// curated catalog holds 20 scenarios per CEFR level (120 total); the client only ever sends a
// scenario's snake_case code. Turns go through the shared /api/speaking/utterance pipeline; the
// sitting is scored at the end.

/** One scenario shown in the roleplay picker. Uzbek title/description come from the content store
 *  keyed by `code` (rule 11); `englishTitle` is only a reference/fallback. */
export interface RoleplayScenarioDto {
  /** Stable snake_case code (e.g. "job_interview") - keys the Uzbek content-store labels and is
   *  what gets sent back to /api/speaking/roleplay/start. */
  code: string;
  englishTitle: string;
  /** The CEFR level this scenario is curated for (20 per level). */
  level: CefrLevel;
  /** Keys the scenario's stored illustration: /api/images/topics/{imageId} (rule 12). */
  imageId: string;
}

/** The tutor's in-character opening turn when a roleplay begins. Mirrors StartConversationResult
 *  but carries the chosen scenario's code instead of topic progress. */
export interface StartRoleplayResult {
  sessionId: string;
  scenarioCode: string;
  tutorText: string;
  tutorAudioBase64: string;
  visemes: VisemeFrameDto[];
  visemeAnimation: string | null;
  isNaturalVoice: boolean;
  wordTimings: SpeechWordTimingDto[];
}

/**
 * The end-of-scene "how did you do" result. Carries a 0-100 score per dimension plus the Uzbek
 * summary/strength/tip strings, which the backend resolves from vetted templates (rule 11).
 */
export interface RoleplayEvaluationResult {
  /** False when the sitting had no spoken learner turns to grade: scores are all zero and only
   *  `summaryUz` is meaningful (a prompt to actually speak before finishing). */
  evaluable: boolean;
  scenarioCode: string;
  overallScore: number;
  taskCompletion: number;
  fluency: number;
  grammar: number;
  appropriateness: number;
  band: PronunciationBand;
  strongestDimension: RoleplayDimension;
  weakestDimension: RoleplayDimension;
  summaryUz: string | null;
  strengthUz: string | null;
  tipUz: string | null;
}

// ── Vocabulary & Notifications ─────────────────────────────────────────────
export interface VocabularyItemDto {
  id: string;
  learnerId: string;
  word: string;
  translation: string;
  exampleSentence: string | null;
  source: VocabularySource;
  stage: ReviewStage;
  failCount: number;
  nextReviewAt: string | null;
  // The vocabulary topic this word was learned from (when known) - for the review picture cards.
  sourceTopicId: string | null;
  // Lowercased word-class name ("noun"/"verb"/"adjective"/"phrasalverb"/…); null when untagged.
  partOfSpeech: string | null;
}

export interface MandatoryReviewStatusDto {
  isRequired: boolean;
  dueItemCount: number;
  dueTopicCount: number;
}

export interface DueReviewDto {
  id: string;
  word: string;
  translation: string;
  exampleSentence: string | null;
  stage: ReviewStage;
  miniTestType: MiniTestType;
  // The vocabulary topic this word was learned from (when known), so the review can show its
  // Uzbek↔English picture card. Null for words from other sources.
  sourceTopicId: string | null;
  // Lowercased word-class name ("noun"/"verb"/"adjective"/"phrasalverb"/…); null when untagged.
  partOfSpeech: string | null;
  // Multiple-choice options for miniTestType === MiniTestType.ClozeChoice (the correct word plus a
  // few distractors from the learner's own vocabulary, shuffled). Null for every other mini-test
  // type - the server still verifies whatever is submitted, this is only what to render.
  options: string[] | null;
}

/** Why a WrittenUsage mini-test passed/failed (Domain.Vocabulary.WordUsageReasonCode). Resolved to
 *  a vetted Uzbek string via uz.vocabulary.review.session (docs/development-guide.md rule 11), never shown raw. */
export enum WordUsageReasonCode {
  Correct = 0,
  WordNotUsed = 1,
  WrongMeaning = 2,
  TooShort = 3,
  NotEnglish = 4,
}

/** Outcome of POST /api/vocabulary/review. */
export interface ReviewResultDto {
  id: string;
  stage: ReviewStage;
  mastered: boolean;
  failCount: number;
  nextReviewAt: string | null;
  // Whether THIS submission passed (server-verified for ClozeChoice/WrittenUsage, self-rated
  // otherwise).
  passed: boolean;
  // Set only when passed is false and the mode was WrittenUsage - resolve via
  // uz.vocabulary.review.session (docs/development-guide.md rule 11), never show raw.
  reasonCode: WordUsageReasonCode | null;
}

// ── Vocabulary topics (module 4: words in context - passage + 15 words + quiz) ──

export interface VocabularyTopicSummaryDto {
  id: string;
  title: string;
  titleUz: string;
  /** Optional catalog metadata; omitted by older API versions. Never infer a word count. */
  wordCount?: number;
  category: string;
  level: CefrLevel;
  isFilled: boolean;
  /** True once the learner has practiced speaking about the topic for 5+ minutes. */
  learned: boolean;
  /** True once the learner has created progress in any module for this topic. */
  isStarted: boolean;
  /** How many of the six skill modules the learner has passed for this topic (K.5). */
  passedModuleCount: number;
  /** Total modules required to master the topic (always six). */
  requiredModuleCount: number;
  /** True once all six lesson modules are passed - the topic is complete. */
  isMastered: boolean;
  /** Sequential gating (K.5): true while the previous topic in the level is not yet mastered. */
  isLocked: boolean;
  /** Trial paywall (H.1): true when this topic is past the free allowance and needs a Premium plan. */
  requiresPro: boolean;
  /** Per-skill checklist (K.5) in learning order, so the roadmap can show each skill's done state. */
  modules: TopicModuleScoreDto[];
}

// --- Level Map (PROJECT-SPEC M.3) - the level-centric outer navigation layer. ---

export interface LevelCanDoDto {
  skill: SkillType;
  statementEn: string;
  /** Content key for the vetted Uzbek statement, e.g. "level.b1.speaking" (rule 11). */
  statementCode: string;
}

export interface LevelSkillScoreDto {
  skill: SkillType;
  score: number;
}

export interface LevelReadinessDto {
  exitTestRecommended: boolean;
  masteredSkillCount: number;
  requiredMasteredSkills: number;
  masteryThreshold: number;
  productiveSkillsMeetFloor: boolean;
  productiveSkillFloor: number;
  hasSufficientSamples: boolean;
  minimumSamplesPerSkill: number;
  minimumOverallScore: number;
  minimumStageScore: number;
  productiveStageFloor: number;
  atMaxLevel: boolean;
}

export interface LevelMapDto {
  level: CefrLevel;
  currentLevel: CefrLevel;
  isCurrentLevel: boolean;
  isLevelUnlocked: boolean;
  hasFullAccess: boolean;
  canDo: LevelCanDoDto[];
  topics: VocabularyTopicSummaryDto[];
  topicsTotal: number;
  topicsLearned: number;
  /** How many topics in this level have all six lesson modules passed. */
  topicsMastered: number;
  /** Authoritative roadmap frontier: the first open topic that is not yet mastered. */
  activeTopicId: string | null;
  /** Canonical SkillType name of the next unlocked, unfinished module for the active topic. */
  recommendedNextModule: string | null;
  skillScores: LevelSkillScoreDto[];
  /** Present only for the learner's current level. */
  readiness: LevelReadinessDto | null;
  /** State of this level's Exit Test node (finish line) relative to the learner. */
  exitState: LevelExitState;
}

/** Exit Test node state per level. Values must match the backend LevelExitState enum
 * (0-based declaration order; serialized as numbers by the API). */
export enum LevelExitState {
  Completed = 0,
  Current = 1,
  Locked = 2,
  MaxLevel = 3,
}

// --- Level Exit Test (PROJECT-SPEC M.5) - reuses the adaptive placement engine. ---

export interface StartLevelExitTestResult {
  sessionId: string;
  /** The level being left (the test is pinned to start here). */
  level: CefrLevel;
  currentStage: TestStage;
  requirements: LevelExitRequirementsDto;
  firstItem: PlacementItemDto | null;
}

export interface LevelExitRequirementsDto {
  testedLevel: CefrLevel;
  targetLevel: CefrLevel;
  minimumOverallScore: number;
  minimumStageScore: number;
  productiveStageFloor: number;
  requiredMasteredSkills: number;
  masteryThreshold: number;
  productiveSkillFloor: number;
  minimumSamplesPerSkill: number;
}

export interface FinalizeLevelExitTestResult {
  /** Whether the mini-test itself was passed (the G.4 confirmation gate). */
  passed: boolean;
  /** Whether passing, combined with the skill-mastery bar, moved the learner up a level. */
  advanced: boolean;
  testedLevel: CefrLevel;
  newLevel: CefrLevel;
  masteredSkillCount: number;
  requiredMasteredSkills: number;
  minimumOverallScore: number;
  minimumStageScore: number;
  productiveStageFloor: number;
  failedStages: TestStage[];
  result: PlacementResultDto;
}

export interface TopicWordDto {
  word: string;
  translation: string;
  ipa: string | null;
  exampleSentence: string | null;
  // "noun" | "verb" | "adjective" | "adverb"; null when the word is untagged.
  partOfSpeech: string | null;
  lexicalCategory?: string | null;
  register?: string | null;
  usageNote?: string | null;
  // Licensed illustration URL for the word itself, resolved by the backend from the English concept.
  imageUrl?: string | null;
  imageAttribution?: string | null;
}

// A topic's target word to highlight in the OTHER skills' text - the same canonical set the
// Vocabulary reader teaches (one PostgreSQL source: VocabularyTopic.Words), reused everywhere so a
// learner meets each word, highlighted identically, across Reading, Listening, Grammar and Writing.
export interface TargetWordDto {
  word: string;
  translation: string;
  exampleSentence: string | null;
}

export interface TopicQuizQuestionDto {
  index: number;
  prompt: string;
  options: string[];
}

/** One passage sentence paired with its Uzbek translation (sentence-by-sentence "MATN" card). */
export interface PassageSentenceDto {
  english: string;
  uzbek: string | null;
}

/** The passage split into sentences, each with its Uzbek translation. */
export interface VocabularyTopicPassageTranslationDto {
  sentences: PassageSentenceDto[];
}

export interface VocabularyTopicDetailDto {
  id: string;
  title: string;
  titleUz: string;
  category: string;
  level: CefrLevel;
  // False while the passage is still being generated (honest pending state).
  isReady: boolean;
  passage: string;
  words: TopicWordDto[];
  quiz: TopicQuizQuestionDto[];
}

// One image in a topic's gallery (no bytes - just the slot to request and its attribution).
export interface TopicImageManifestEntry {
  slot: number;
  source: string;
  attribution: string | null;
  sourceUrl: string | null;
}

// A topic's gallery manifest: every slot that has a downloaded image, in slot order.
export interface TopicImageManifestDto {
  topicId: string;
  images: TopicImageManifestEntry[];
}

export interface TopicQuizOutcomeDto {
  questionIndex: number;
  word: string;
  selectedOptionIndex: number;
  correctOptionIndex: number;
  isCorrect: boolean;
}

export interface TopicQuizResultDto {
  topicId: string;
  totalQuestions: number;
  correctCount: number;
  scorePercent: number;
  outcomes: TopicQuizOutcomeDto[];
  /** Present when the quiz was submitted with a learner id (PROJECT-SPEC K.5). */
  completion: TopicCompletionDto | null;
}

/** One module's best score toward mastering a topic (PROJECT-SPEC K.5). */
export interface TopicModuleScoreDto {
  /** Canonical SkillType name: Vocabulary | Grammar | Reading | Listening | Speaking | Writing. */
  module: string;
  score: number;
  passed: boolean;
  /** Sequential gating: false while an earlier skill has not been attempted yet. */
  unlocked: boolean;
  achievedAt: string | null;
}

/** A learner's module-by-module mastery progress for a topic (PROJECT-SPEC K.5). */
export interface TopicCompletionDto {
  learnerId: string;
  topicId: string;
  level: string;
  isMastered: boolean;
  masteredAt: string | null;
  masteryThreshold: number;
  passedModuleCount: number;
  requiredModuleCount: number;
  modules: TopicModuleScoreDto[];
}

/** Authoritative durable reward produced by one qualifying skill completion. */
export interface SkillRewardDto {
  awardedXp: number;
  awardedCoins: number;
  modulePoints: number;
  dailyActivityBonus: number;
  allModulesBonus: number;
  streakMilestoneBonus: number;
  premiumMultiplierApplied: boolean;
  currentStreak: number;
  alreadyCreditedToday: boolean;
}

/** Result of recording a single module's score (PROJECT-SPEC K.5). */
export interface RecordTopicModuleScoreResult {
  completion: TopicCompletionDto;
  justMastered: boolean;
  reward: SkillRewardDto;
}

export interface WordPronunciationCheckDto {
  word: string;
  recognized: boolean;
  correct: boolean;
  isAuthentic: boolean;
  overallScore: number;
  accuracyScore: number;
  errorType: PronunciationErrorType;
  phonemes: PhonemePronunciationDto[];
  feedbackUz: string | null;
}

/**
 * Plans and prices as the server knows them. `paymentsEnabled` is the single source for whether
 * checkout can be completed - the UI must never hardcode a "coming soon" state, or flipping the
 * server flag would require a matching code change in several places.
 */
export interface PlanCatalogDto {
  plans: SubscriptionPlanDto[];
  paymentsEnabled: boolean;
  currency: string;
}

export interface SubscriptionPlanDto {
  plan: SubscriptionPlan;
  code: string;
  priceUzs: number;
  durationDays: number;
  pricePerMonthUzs: number;
  savePercent: number;
  isBestValue: boolean;
}

export interface JoinPremiumWaitlistRequest {
  contact: string;
  interestedPlan?: SubscriptionPlan;
  source?: string;
}

/**
 * The learner's daily speaking budget. Speech-to-text is billed by the second, so speaking is the
 * one feature metered in minutes rather than in uses.
 */
export interface SpeakingQuotaStatusDto {
  limitMinutes: number;
  usedMinutes: number;
  remainingMinutes: number;
  isAllowed: boolean;
  isPremium: boolean;
  /** The learner's next local midnight, when the budget renews. */
  resetsAt: string;
}

export interface SpeakingPracticeWordDto {
  id: string;
  word: string;
  lastAccuracyScore: number;
  lastErrorType: PronunciationErrorType;
  errorCount: number;
  bestPracticeScore: number | null;
  firstFailedAt: string;
  lastFailedAt: string;
}

export interface SpeakingPracticeAttemptResult {
  assessment: WordPronunciationCheckDto;
  mastered: boolean;
  masteryScore: number;
  /** How many times the learner has cleared the mastery score for this word so far. */
  successfulAttempts: number;
  /** Successful attempts required before the word leaves the practice queue. */
  requiredSuccesses: number;
}

export interface NotificationDto {
  id: string;
  code: string;
  message: string;
  createdAt: string;
  isRead: boolean;
  /** In-app destination to open when the notification is tapped (e.g. "/app/vocabulary/review"),
   * or null for informational notifications. */
  linkUrl: string | null;
  /** Bold headline shown above the message; set only for super-admin broadcasts. */
  title: string | null;
}

/** A sent super-admin broadcast (Application.Notifications.Dtos.AdminBroadcastDto). */
export interface AdminBroadcastDto {
  id: string;
  title: string;
  body: string;
  linkUrl: string | null;
  createdAt: string;
}

/** Result of the manual daily notification dispatch
 * (Application.Vocabulary.Dtos.DispatchResultDto). */
export interface DispatchResultDto {
  notifiedLearners: number;
  totalDueItems: number;
}

// ── Video ──────────────────────────────────────────────────────────────────
export interface VideoSummaryDto {
  id: string;
  youTubeVideoId: string;
  title: string;
  channel: string;
  durationSeconds: number;
  topic: string;
  level: CefrLevel;
}

// One card in the infinite video feed (B.3 Bosqich 2). lessonId is set when the video is
// already a stored lesson; otherwise the client opens it on demand by youTubeVideoId.
export interface VideoFeedItemDto {
  lessonId: string | null;
  youTubeVideoId: string;
  title: string;
  channel: string;
  channelAvatarUrl?: string | null;
  durationSeconds: number;
  topic: string;
  level: CefrLevel;
  hasClosedCaptions: boolean;
}

// A page of the feed plus the opaque cursor for the next page (null when no more).
export interface VideoFeedDto {
  items: VideoFeedItemDto[];
  nextCursor: string | null;
}

export interface VideoPlaylistItemDto {
  youTubeVideoId: string;
  title: string;
  channel: string;
  durationSeconds: number;
  thumbnailUrl: string | null;
}

export interface VideoPlaylistDto {
  id: string;
  title: string;
  channel: string;
  thumbnailUrl: string | null;
  items: VideoPlaylistItemDto[];
  totalDurationSeconds: number;
  isSingleVideoCollection: boolean;
}

export interface VideoPlaylistSearchDto {
  items: VideoPlaylistDto[];
}

// One word of a transcript line with its own timing, so the player can highlight the exact
// spoken word (karaoke). Real timing from YouTube's per-word caption offsets, not estimated.
export interface TranscriptWordDto {
  text: string;
  startSeconds: number;
  endSeconds: number;
}

export interface TranscriptSegmentDto {
  startSeconds: number;
  endSeconds: number;
  englishText: string;
  uzbekTranslation: string | null;
  // Per-word timing; empty when the source gave only line-level timing (client then estimates).
  words: TranscriptWordDto[];
}

export interface QuizQuestionDto {
  id: string;
  prompt: string;
  options: string[];
  hintCode: string | null;
}

// One transcript word paired with a short vetted Uzbek meaning (rule 11 dynamic-text
// exception: a real English source word, translated and validated server-side, never
// free-invented UI copy). Shown when a learner hovers/taps the word in the player.
export interface VideoGlossaryEntryDto {
  word: string;
  uzbekMeaning: string;
}

export interface VideoLessonDto {
  id: string;
  youTubeVideoId: string;
  title: string;
  channel: string;
  durationSeconds: number;
  topic: string;
  level: CefrLevel;
  status: IngestionStatus;
  transcriptStatus: TranscriptStatus;
  transcript: TranscriptSegmentDto[];
  glossary: VideoGlossaryEntryDto[];
  questions: QuizQuestionDto[];
}

// One turn of the video player's explain-chat panel - kept client-side only (no server session)
// and sent back as trailing history for follow-up questions.
export interface ChatTurn {
  role: "user" | "assistant";
  text: string;
}

// replyUz is null when the explainer is unavailable/failed - the panel shows an honest
// "javob olinmadi" state rather than fabricated text.
export interface ChatReplyDto {
  replyUz: string | null;
}

// Learner's "was it easy/hard?" rating - recommendation training signal (B.3 Bosqich 2).
// Numeric values mirror Domain.Video.VideoDifficultyRating.
export enum VideoDifficultyRating {
  TooEasy = 0,
  JustRight = 1,
  TooHard = 2,
}

export interface QuestionOutcomeDto {
  questionId: string;
  selectedOptionIndex: number;
  correctOptionIndex: number;
  isCorrect: boolean;
  hint: string | null;
}

export interface VideoQuizResultDto {
  awardedXp?: number;
  lessonId: string;
  totalQuestions: number;
  correctCount: number;
  passed: boolean;
  outcomes: QuestionOutcomeDto[];
}

export interface GeneratedVideoQuestionDto {
  id: string;
  prompt: string;
  promptUz: string;
  options: string[];
  sourceStartSeconds: number;
  sourceEndSeconds: number;
  sourceText: string;
}

export interface GeneratedVideoQuizDto {
  quizId: string;
  videoLessonId: string;
  title: string;
  youTubeVideoId: string;
  expiresAt: string;
  questions: GeneratedVideoQuestionDto[];
  result: VideoQuizResultDto | null;
}

// ── Reading ────────────────────────────────────────────────────────────────
// Every skill teaches the same learning-spine topics, so a reading catalog row IS a topic and
// its lesson (body + glossary + quiz) is generated lazily when the topic is opened.
export interface ReadingSummaryDto {
  topicId: string;
  title: string;
  titleUz: string;
  category: string;
  level: CefrLevel;
}

export interface GlossaryEntryDto {
  word: string;
  translation: string;
  exampleSentence: string | null;
}

export interface ReadingQuestionDto {
  id: string;
  prompt: string;
  options: string[];
}

export interface ReadingPassageDto {
  topicId: string;
  passageId: string;
  title: string;
  body: string;
  topic: string;
  level: CefrLevel;
  wordCount: number;
  // False while the lesson is still being generated (pending) - show an honest "preparing" state.
  isReady: boolean;
  glossary: GlossaryEntryDto[];
  questions: ReadingQuestionDto[];
  // The topic's vocabulary words to highlight in the body (same canonical set as /vocabulary).
  targetWords: TargetWordDto[];
}

export interface ReadingQuizAnswer {
  questionId: string;
  selectedOptionIndex: number;
}

export interface ReadingQuestionOutcomeDto {
  questionId: string;
  selectedOptionIndex: number;
  correctOptionIndex: number;
  isCorrect: boolean;
  // English explanation of the correct answer (immersion teaching note), only for a wrong answer.
  explanation: string | null;
}

export type ReadingAnswerCheckDto = ReadingQuestionOutcomeDto;

export interface ReadingQuizResultDto {
  topicId: string;
  totalQuestions: number;
  correctCount: number;
  scorePercent: number;
  passed: boolean;
  outcomes: ReadingQuestionOutcomeDto[];
  // The topic's six-module mastery checklist after crediting this Reading score (K.5).
  completion: TopicCompletionDto | null;
}

// ── Listening ────────────────────────────────────────────────────────────────
// Each catalog row is one learning-spine topic (the 50 shared topics per level); the listening
// exercise (audio + comprehension quiz) is generated and cached when the topic is opened.
export interface ListeningSummaryDto {
  topicId: string;
  title: string;
  titleUz: string;
  category: string;
  level: CefrLevel;
}

export interface ListeningQuestionDto {
  id: string;
  prompt: string;
  options: string[];
}

export interface ListeningExerciseDto {
  topicId: string;
  title: string;
  topic: string;
  level: CefrLevel;
  wordCount: number;
  // False while the exercise is still being generated (the player shows an honest "preparing" state).
  isReady: boolean;
  transcript: string;
  questions: ListeningQuestionDto[];
  // The topic's vocabulary words to highlight in the transcript (same canonical set as /vocabulary).
  targetWords: TargetWordDto[];
}

export interface ListeningQuizAnswer {
  questionId: string;
  selectedOptionIndex: number;
}

export interface ListeningQuestionOutcomeDto {
  questionId: string;
  selectedOptionIndex: number;
  correctOptionIndex: number;
  isCorrect: boolean;
  // The "why" note shown only for a wrong answer (vetted Uzbek hint or English explanation).
  hint: string | null;
}

export type ListeningAnswerCheckDto = ListeningQuestionOutcomeDto;

export interface ListeningQuizResultDto {
  topicId: string;
  totalQuestions: number;
  correctCount: number;
  scorePercent: number;
  passed: boolean;
  outcomes: ListeningQuestionOutcomeDto[];
  // The topic's six-module mastery checklist after crediting this Listening score (K.5).
  completion: TopicCompletionDto | null;
}

// ── Grammar ────────────────────────────────────────────────────────────────
// Each catalog row is one learning-spine topic; the grammar lesson (the topic's grammar focus
// taught in its own context) is generated and cached when the topic is opened.
// On-demand sentence translation: the Uzbek meaning of one English sentence/text. `translation`
// is null when translation is unavailable (the UI shows an honest "unavailable" state).
export interface TranslationDto {
  text: string;
  translation: string | null;
}

export interface AssistantTurnDto {
  role: "user" | "assistant";
  text: string;
}

export interface AssistantReplyDto {
  reply: string | null;
}

export type AssistantSkill = "general" | "vocabulary" | "grammar" | "reading" | "writing" | "speaking" | "listening" | "video" | "books" | "progress";
export type AssistantResourceType = "page" | "topic" | "lesson" | "video" | "book" | "section";

export interface AssistantSessionSummaryDto {
  id: string;
  skill: AssistantSkill;
  resourceType: AssistantResourceType;
  resourceId: string | null;
  title: string;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
}

export interface AssistantSessionPageDto {
  items: AssistantSessionSummaryDto[];
  nextCursor: string | null;
}

export interface CursorPageDto<T> {
  items: T[];
  nextCursor: string | null;
}

export interface AssistantMessageDto {
  id: string;
  role: "user" | "assistant";
  text: string;
  status: "completed" | "failed" | "fallback";
  source: string;
  clientRequestId: string | null;
  latencyMs: number | null;
  createdAt: string;
  sources: readonly AssistantMessageSourceDto[];
}

export interface AssistantMessageSourceDto {
  area: AssistantSkill;
  resourceType: AssistantResourceType;
  resourceId: string;
  title: string;
  route: string;
}

export interface AssistantSessionDto extends AssistantSessionSummaryDto {
  messages: AssistantMessageDto[];
}

export interface GrammarTopicSummaryDto {
  topicId: string;
  title: string;
  titleUz: string;
  category: string;
  level: CefrLevel;
  grammarFocusCode: string;
}

export interface GrammarExerciseDto {
  id: string;
  type: GrammarExerciseType;
  prompt: string;
  options: string[];
}

export interface GrammarApplicationTaskDto {
  id: string;
  targetSkill: SkillType;
  prompt: string;
}

// One example sentence in a curated lesson: the exact English and its vetted Uzbek meaning.
export interface CuratedGrammarExampleDto {
  english: string;
  uzbek: string;
}

// One explanation block of a curated lesson: a short Uzbek heading and its Uzbek body.
export interface CuratedGrammarRuleDto {
  headingUz: string;
  bodyUz: string;
}

// The vetted, hand-authored Uzbek explanation of a topic's grammar focus (rule 11). When present,
// the page shows this as the primary Step-2 content instead of translating the generated English
// rule, so the Uzbek is always accurate and English is kept only in the formulas and examples.
export interface CuratedGrammarLessonDto {
  titleUz: string;
  summaryUz: string;
  formulas: string[];
  rules: CuratedGrammarRuleDto[];
  examples: CuratedGrammarExampleDto[];
  commonMistakesUz: string[];
}

export interface GrammarLessonDto {
  topicId: string;
  topic: string;
  grammarFocusCode?: string;
  category: ErrorCategory;
  level: CefrLevel;
  // False while the lesson is still being generated (the page shows an honest "preparing" state).
  isReady: boolean;
  contextIntro: string;
  explanation: string;
  exercises: GrammarExerciseDto[];
  applicationTasks: GrammarApplicationTaskDto[];
  // Vetted Uzbek explanation of the topic's grammar focus, when one is authored; otherwise null
  // and the page falls back to the on-demand translation of the English rule.
  curated: CuratedGrammarLessonDto | null;
  // The topic's vocabulary words to highlight in the lesson text (same canonical set as /vocabulary).
  targetWords: TargetWordDto[];
}

export interface GrammarExerciseAnswer {
  exerciseId: string;
  selectedOptionIndex: number;
  textAnswer?: string;
}

export interface GrammarExerciseOutcomeDto {
  exerciseId: string;
  selectedOptionIndex: number;
  correctOptionIndex: number;
  isCorrect: boolean;
  // English "why this is correct" note (immersion teaching content); set only for a wrong answer.
  explanation: string | null;
}

export type GrammarExerciseCheckDto = GrammarExerciseOutcomeDto;

export interface GrammarExerciseResultDto {
  topicId: string;
  totalExercises: number;
  correctCount: number;
  scorePercent: number;
  passed: boolean;
  outcomes: GrammarExerciseOutcomeDto[];
  // The topic's six-module mastery checklist after crediting this Grammar score (K.5).
  completion: TopicCompletionDto | null;
}

// ── Writing ────────────────────────────────────────────────────────────────
// Every skill teaches the same learning-spine topics, so a writing catalog row IS a topic and its
// task (prompt + guidance) is generated lazily when the topic is opened.
export interface WritingTaskSummaryDto {
  topicId: string;
  title: string;
  titleUz: string;
  category: string;
  level: CefrLevel;
}

export interface WritingTaskDto {
  topicId: string;
  taskId: string;
  title: string;
  prompt: string;
  level: CefrLevel;
  minWords: number;
  maxWords: number;
  // False while the prompt is still being generated (pending) - show an honest "preparing" state.
  isReady: boolean;
  guidance: string[];
  // The topic's vocabulary words to highlight in the prompt/guidance (same canonical set as /vocabulary).
  targetWords: TargetWordDto[];
}

export interface DimensionScoreDto {
  dimension: WritingDimension;
  score: number;
}

export interface WritingIssueDto {
  dimension: WritingDimension;
  startOffset: number;
  endOffset: number;
  category: ErrorCategory | null;
  explanation: string;
}

export interface WritingAssessmentDto {
  topicId: string;
  taskId: string;
  dimensionScores: DimensionScoreDto[];
  issues: WritingIssueDto[];
  overallBand: number;
  overallPercent: number;
  estimatedLevel: CefrLevel;
  assessmentSource: WritingAssessmentSource;
  /** The topic's six-module mastery checklist after crediting this Writing score (K.5). */
  completion: TopicCompletionDto | null;
}

// ── Subscription ───────────────────────────────────────────────────────────
export interface SubscriptionDto {
  learnerId: string;
  status: SubscriptionStatus;
  plan: SubscriptionPlan | null;
  expiresAt: string | null;
  daysUntilExpiry: number | null;
  isPremiumActive: boolean;
  isTrialActive: boolean;
  trialExpiresAt: string | null;
  trialDaysUntilExpiry: number | null;
}

export interface StartSubscriptionResultDto {
  transactionId: string;
  checkoutUrl: string;
  amountUzs: number;
  provider: number;
  plan: SubscriptionPlan;
}

export interface PaymentProviderDto {
  provider: PaymentProvider;
  name: string;
  isMock: boolean;
}

// ── Retention ──────────────────────────────────────────────────────────────
export interface ChurnSignalDto {
  type: number;
  risk: ChurnRiskLevel;
}

export interface ChurnAssessmentDto {
  learnerId: string;
  overallRisk: ChurnRiskLevel;
  isAtRisk: boolean;
  signals: ChurnSignalDto[];
}

// ── Books ────────────────────────────────────────────────────────────────────
// The Books library (Home → Books): multi-section graded readers per CEFR level. Each section is
// read once at least 70% of its comprehension quiz is correct; the whole book is confirmed read
// once every section is passed. Section text + questions are generated lazily on first open.
export interface BookSummaryDto {
  id: string;
  title: string;
  titleUz: string;
  author: string;
  level: CefrLevel;
  topic: string;
  coverImageUrl: string | null;
  coverAttribution: string | null;
  sectionCount: number;
  sectionsRead: number;
  isCompleted: boolean;
}

export interface BookSectionSummaryDto {
  id: string;
  order: number;
  title: string;
  isRead: boolean;
  isLocked: boolean;
}

export interface BookDetailDto {
  id: string;
  title: string;
  titleUz: string;
  author: string;
  synopsis: string;
  level: CefrLevel;
  topic: string;
  coverImageUrl: string | null;
  coverAttribution: string | null;
  sectionsRead: number;
  isCompleted: boolean;
  sections: BookSectionSummaryDto[];
}

export interface BookQuestionDto {
  id: string;
  prompt: string;
  options: string[];
}

export interface BookSectionDto {
  bookId: string;
  sectionId: string;
  order: number;
  bookTitle: string;
  title: string;
  body: string;
  level: CefrLevel;
  wordCount: number;
  // False while the section is still being generated (pending) - show an honest "preparing" state.
  isReady: boolean;
  isRead: boolean;
  questions: BookQuestionDto[];
}

export interface BookQuizAnswer {
  questionId: string;
  selectedOptionIndex: number;
}

export interface BookQuestionOutcomeDto {
  questionId: string;
  selectedOptionIndex: number;
  correctOptionIndex: number;
  isCorrect: boolean;
  // English explanation of the correct answer, only for a wrong answer.
  explanation: string | null;
}

export interface BookQuizResultDto {
  bookId: string;
  sectionId: string;
  totalQuestions: number;
  correctCount: number;
  requiredCorrect: number;
  scorePercent: number;
  passed: boolean;
  bookCompleted: boolean;
  sectionsRead: number;
  sectionCount: number;
  outcomes: BookQuestionOutcomeDto[];
}

// ── Competition ("Musobaqa") module - mirrors Application.Competition.Dtos ──
// Enums serialized as their numeric C# values.

export enum CompetitionStatus {
  Draft = 0,
  Lobby = 1,
  Active = 2,
  Finished = 3,
}

export enum SlideSourceType {
  Vocabulary = 0,
  Grammar = 1,
}

export enum ParticipantStatus {
  Joined = 0,
  Playing = 1,
  Finished = 2,
}

export interface CompetitionSettingsDto {
  slideDurationSeconds: number;
  autoAdvance: boolean;
  shuffleSlides: boolean;
  allowLateJoin: boolean;
  questionsPerTopic: number;
  showLiveLeaderboard: boolean;
  basePointsPerCorrect: number;
}

export interface SlideDto {
  id: string;
  order: number;
  sourceType: SlideSourceType;
  sourceTopicId: string;
  questionText: string;
  options: string[];
  points: number;
}

export interface ParticipantDto {
  id: string;
  learnerId: string;
  displayName: string;
  isHost: boolean;
  status: ParticipantStatus;
  score: number;
}

export interface SelectableTopicDto {
  id: string;
  title: string;
  titleUz: string;
  source: "vocabulary" | "grammar";
  category: string;
}

export interface CompetitionDto {
  id: string;
  title: string;
  status: CompetitionStatus;
  settings: CompetitionSettingsDto;
  currentSlideIndex: number;
  totalSlides: number;
  participants: ParticipantDto[];
  accessCode?: string | null;
}

export interface RankingRowDto {
  participantId: string;
  displayName: string;
  isHost: boolean;
  score: number;
  answeredCount: number;
  rank: number;
}

export interface CompetitionResultDto {
  competitionId: string;
  title: string;
  ranking: RankingRowDto[];
  winnerParticipantId: string;
  winnerDisplayName: string;
  winnerScore: number;
  rewardPoints: number;
}
