import http from "node:http";

let skillModulesFor = (passed) =>
  ["Vocabulary", "Grammar", "Reading", "Listening", "Speaking", "Writing"].map((m, i) => ({
    module: m,
    score: passed[i] ? 100 : 0,
    passed: passed[i] === 1,
    unlocked: i === 0 || passed[i - 1] === 1,
    achievedAt: passed[i] ? "2026-07-10T10:00:00Z" : null,
  }));

const MOCK_TOPICS = [
  { id: "11111111-1111-1111-1111-111111111111", title: "Salomlashuv va tanishuv", titleUz: "Greeting & introductions", category: "Daily_Life", level: 1, isFilled: true, learned: true, passedModuleCount: 6, requiredModuleCount: 6, isMastered: true, isLocked: false, requiresPro: false, modules: skillModulesFor([1, 1, 1, 1, 1, 1]) },
  { id: "22222222-2222-2222-2222-222222222222", title: "Oila a'zolari", titleUz: "Family members", category: "People", level: 1, isFilled: true, learned: true, passedModuleCount: 3, requiredModuleCount: 6, isMastered: false, isLocked: false, requiresPro: false, modules: skillModulesFor([1, 1, 1, 0, 0, 0]) },
  { id: "33333333-3333-3333-3333-333333333333", title: "Ranglar va sonlar", titleUz: "Colors & numbers", category: "Basics", level: 1, isFilled: true, learned: false, passedModuleCount: 0, requiredModuleCount: 6, isMastered: false, isLocked: true, requiresPro: false, modules: skillModulesFor([0, 0, 0, 0, 0, 0]) },
  { id: "44444444-4444-4444-4444-444444444444", title: "Ovqatlanish", titleUz: "Food & drink", category: "Daily_Life", level: 1, isFilled: true, learned: false, passedModuleCount: 0, requiredModuleCount: 6, isMastered: false, isLocked: true, requiresPro: false, modules: skillModulesFor([0, 0, 0, 0, 0, 0]) },
];

const MOCK_TOPIC_ID = MOCK_TOPICS[0].id;
const MOCK_BOOK_ID = "b1111111-1111-4111-8111-111111111111";
const MOCK_BOOK_SECTION_ID = "s1111111-1111-4111-8111-111111111111";
const MOCK_VIDEO_ID = "v1111111-1111-4111-8111-111111111111";
const MOCK_ADMIN_ID = "u1111111-1111-4111-8111-111111111111";
const MOCK_ADMIN_VOCAB_ID = "a1111111-1111-1111-1111-111111111111";
const MOCK_ADMIN_GRAMMAR_ID = "g1111111-1111-4111-8111-111111111111";
const MOCK_ADMIN_LISTENING_ID = "l1111111-1111-4111-8111-111111111111";
const MOCK_ADMIN_READING_ID = "r1111111-1111-4111-8111-111111111111";

const MOCK_STUDY_STATS = {
  todaySeconds: 1800,
  weekSeconds: 7200,
  monthSeconds: 21600,
  yearSeconds: 86400,
  totalSeconds: 108000,
  activeDays: 24,
  currentDayStreak: 12,
  averageSecondsPerActiveDay: 4500,
  longestDaySeconds: 7200,
  last7Days: Array.from({ length: 7 }, (_, index) => ({ day: `2026-08-0${index + 1}`, seconds: 900 + index * 120 })),
  last12Months: Array.from({ length: 12 }, (_, index) => ({ year: 2026, month: index + 1, seconds: 3600 + index * 600 })),
  bySkill: [1, 2, 3, 4, 5, 6].map((skill, index) => ({ skill, seconds: 3600 + index * 300 })),
  yearHeatmap: Array.from({ length: 14 }, (_, index) => ({ day: `2026-07-${String(index + 15).padStart(2, "0")}`, seconds: 600 + index * 60 })),
};

function mockOverview() {
  return {
    learnerId: "mock-learner-0001",
    overallLevel: 1,
    skillScores: [1, 2, 3, 4, 5, 6].map((skill, index) => ({ skill, score: 62 + index * 5, sampleCount: 8 + index })),
    errorHeatmap: [{ category: 2, count: 3 }, { category: 3, count: 2 }],
    levelStatus: { isEligible: false, masteredSkillCount: 3, requiredMasteredSkills: 5, confirmationTestPassed: false, atMaxLevel: false },
  };
}

function mockProgressDashboard() {
  const overview = mockOverview();
  const gamification = {
    todayCompletedTasks: 3,
    dailyGoalTarget: 4,
    isGoalMet: false,
    currentStreak: 12,
    longestStreak: 27,
    isStreakAtRisk: false,
    skillsCompletedToday: ["Vocabulary", "Grammar", "Reading"],
  };
  return {
    studyStats: MOCK_STUDY_STATS,
    progressInsight: {
      snapshot: {
        learnerId: overview.learnerId,
        asOfDate: "2026-08-05",
        overallLevel: overview.overallLevel,
        levelStatus: overview.levelStatus,
        skills: overview.skillScores.map((item) => ({ ...item, confidence: 2, isPlacementBaseline: false, eightWeekDelta: 4, last8Weeks: [] })),
        errorsLast30Days: [],
        topicProgress: { started: 3, mastered: 1, modulesPassed: 9 },
        studyTime: MOCK_STUDY_STATS,
        vocabulary: { total: 24, learning: 18, mastered: 6, due: 3, addedLast7Days: 5, addedLast30Days: 14, totalFailCount: 2, stageBreakdown: [] },
        gamification,
        hasLearningProfile: true,
      },
      insight: {
        overallCode: "steady_progress",
        achievementCodes: ["streak_7"],
        skillsToStrengthen: [],
        recurringErrors: [],
        habitCode: "consistent",
        nextActionCode: "continue_topic",
        targetRoute: "/levels",
        source: 1,
        isFallback: false,
      },
      generatedAt: "2026-08-05T08:00:00Z",
    },
    overview,
    growth: [1, 2, 3, 4, 5, 6].map((skill) => ({ weekEnding: "2026-08-03", skill, score: 70 + skill })),
    recommendations: [{ code: "continue", text: "Bugungi mashqni davom ettiring.", skill: 6, category: null }],
    gamification,
    levelMap: levelMapMock(),
  };
}

function mockVocabularyTopic() {
  return {
    id: MOCK_TOPIC_ID,
    title: "Greetings and introductions",
    titleUz: "Salomlashuv va tanishuv",
    category: "Daily_Life",
    level: 1,
    isReady: true,
    passage: "Hello, my name is Ali. I am happy to meet you.",
    words: [
      { word: "hello", translation: "salom", ipa: "/həˈləʊ/", exampleSentence: "Hello, my name is Ali.", partOfSpeech: "noun", lexicalCategory: "Noun", register: null, usageNote: null, imageUrl: null, imageAttribution: null },
      { word: "name", translation: "ism", ipa: "/neɪm/", exampleSentence: "My name is Ali.", partOfSpeech: "noun", lexicalCategory: "Noun", register: null, usageNote: null, imageUrl: null, imageAttribution: null },
    ],
    quiz: [],
  };
}

function mockGrammarLesson() {
  return {
    topicId: MOCK_TOPIC_ID,
    topic: "Present Simple",
    category: 2,
    level: 1,
    isReady: true,
    contextIntro: "I introduce myself every day.",
    explanation: "Use the present simple for routines and facts.",
    exercises: [{ id: "grammar-q1", type: 1, prompt: "Choose the correct form", options: ["am", "is", "are"] }],
    applicationTasks: [],
    curated: {
      titleUz: "Hozirgi oddiy zamon",
      summaryUz: "Odat va faktlarni ifodalash uchun ishlatiladi.",
      formulas: ["Subject + verb"],
      rules: [{ headingUz: "Qoida", bodyUz: "I bilan am ishlatiladi." }],
      examples: [{ english: "I am Ali.", uzbek: "Men Aliman." }],
      commonMistakesUz: [],
    },
    targetWords: [],
  };
}

function mockReadingPassage() {
  return {
    topicId: MOCK_TOPIC_ID,
    passageId: "passage-1",
    title: "Meeting a new friend",
    body: "Ali meets Sara at school. They introduce themselves and talk about their families.",
    topic: "Greetings",
    level: 1,
    wordCount: 14,
    isReady: true,
    glossary: [{ word: "meet", translation: "uchrashmoq", exampleSentence: "Nice to meet you." }],
    questions: [{ id: "reading-q1", prompt: "Where do Ali and Sara meet?", options: ["At school", "At the airport"] }],
    targetWords: [],
  };
}

function mockListeningExercise() {
  return {
    topicId: MOCK_TOPIC_ID,
    title: "A short introduction",
    topic: "Greetings",
    level: 1,
    wordCount: 9,
    isReady: true,
    transcript: "Hello, my name is Ali. Nice to meet you.",
    questions: [{ id: "listening-q1", prompt: "What is the speaker's name?", options: ["Ali", "Tom"] }],
    targetWords: [],
  };
}

function mockWritingTask() {
  return {
    topicId: MOCK_TOPIC_ID,
    taskId: "writing-task-1",
    title: "Introduce yourself",
    prompt: "Write a short introduction about yourself.",
    level: 1,
    minWords: 5,
    maxWords: 80,
    isReady: true,
    guidance: ["Say your name", "Mention where you live"],
    targetWords: [],
  };
}

function mockBookDetail() {
  return {
    id: MOCK_BOOK_ID,
    title: "A New Friend",
    titleUz: "Yangi do‘st",
    author: "EnglishAI",
    synopsis: "A beginner story about meeting a new classmate.",
    level: 1,
    topic: "Friendship",
    coverImageUrl: null,
    coverAttribution: null,
    sectionsRead: 0,
    isCompleted: false,
    sections: [{ id: MOCK_BOOK_SECTION_ID, order: 1, title: "First day", isRead: false, isLocked: false }],
  };
}

function levelMapMock() {
  return {
    level: 1,
    currentLevel: 1,
    isCurrentLevel: true,
    isLevelUnlocked: true,
    hasFullAccess: false,
    canDo: [
      { skill: 6, statementEn: "I can use simple phrases to introduce myself.", statementCode: "a1_vocab_intro" },
      { skill: 5, statementEn: "I can form basic present-tense sentences.", statementCode: "a1_grammar_present" },
      { skill: 3, statementEn: "I can understand very short, simple texts.", statementCode: "a1_read_basic" },
      { skill: 2, statementEn: "I can recognise familiar words in speech.", statementCode: "a1_listen_words" },
      { skill: 1, statementEn: "I can answer simple personal questions aloud.", statementCode: "a1_speak_personal" },
      { skill: 4, statementEn: "I can write a short postcard.", statementCode: "a1_write_postcard" },
    ],
    topics: MOCK_TOPICS,
    topicsTotal: 4,
    topicsLearned: 2,
    topicsMastered: 1,
    skillScores: [
      { skill: 6, score: 92 },
      { skill: 5, score: 78 },
      { skill: 3, score: 71 },
      { skill: 2, score: 65 },
      { skill: 1, score: 60 },
      { skill: 4, score: 55 },
    ],
    readiness: { exitTestRecommended: false, masteredSkillCount: 4, requiredMasteredSkills: 5, atMaxLevel: false },
    exitState: 2,
  };
}

// ── Placement test mock ─────────────────────────────────────────────────────
// The web build points at this mock server because the .NET backend is not runnable.
// Without real placement endpoints the test hangs on "loading" forever - so we serve a
// full, deterministic 10-item session (MCQ / writing / speaking / listening) and advance
// through it on each answer, finalizing with a result. Mirrors the DTO shapes in
// src/api/types.ts so the frontend renders exactly as against the real backend.

const PLACEMENT_ITEMS = [
  { kind: 0, prompt: "Choose the correct option: 'She ___ to school every day.'", options: ["go", "goes", "going", "gone"], hasAudio: false, passageText: null },
  { kind: 0, prompt: "'Kitob' so'zi qaysi tilda?", options: ["Inglizcha", "O'zbekcha", "Fransuzcha", "Nemischa"], hasAudio: false, passageText: null },
  { kind: 0, prompt: "Read the text. What is Tom doing? 'Tom is reading a book in the garden.'", options: ["He is sleeping", "He is reading", "He is running", "He is eating"], hasAudio: false, passageText: "Tom is a student. He likes books. Every morning he reads in the garden." },
  { kind: 0, prompt: "Listen and choose the word you hear.", options: ["Cat", "Cap", "Cup", "Car"], hasAudio: true, passageText: null },
  { kind: 0, prompt: "Choose the correct translation: 'Mening ismim Ali.'", options: ["My name is Ali", "I am Ali", "This is Ali", "He is Ali"], hasAudio: false, passageText: null },
  { kind: 1, prompt: "Write a short sentence about your family (3–6 words).", options: null, hasAudio: false, passageText: null, minWords: 3, maxWords: 6 },
  { kind: 0, prompt: "Which is the past tense of 'eat'?", options: ["eat", "ate", "eaten", "eating"], hasAudio: false, passageText: null },
  { kind: 2, prompt: "Read this aloud: 'The quick brown fox jumps over the lazy dog.'", options: null, hasAudio: false, passageText: null },
  { kind: 0, prompt: "Fill in: 'They ___ playing football.'", options: ["am", "is", "are", "be"], hasAudio: false, passageText: null },
  { kind: 0, prompt: "Choose the synonym of 'happy':", options: ["sad", "glad", "angry", "tired"], hasAudio: false, passageText: null },
];

const sessions = new Map();
let sessionSeq = 1;

function buildItem(i) {
  const raw = PLACEMENT_ITEMS[i];
  return {
    id: `q-${i + 1}`,
    kind: raw.kind,
    stage: 1,
    difficulty: 1,
    prompt: raw.prompt,
    options: raw.options ?? null,
    hasAudio: !!raw.hasAudio,
    passageText: raw.passageText ?? null,
    minWords: raw.minWords ?? null,
    maxWords: raw.maxWords ?? null,
  };
}

function advance(sessionId) {
  const s = sessions.get(sessionId);
  if (!s) return { isCompleted: true, nextItem: null };
  const current = s.index;
  const isCompleted = current + 1 >= PLACEMENT_ITEMS.length;
  const nextItem = isCompleted ? null : buildItem(current + 1);
  s.index = current + 1;
  return { isCompleted, nextItem };
}

function readJson(req) {
  return new Promise((resolve) => {
    let data = "";
    req.on("data", (c) => (data += c));
    req.on("end", () => {
      try {
        resolve(data ? JSON.parse(data) : {});
      } catch {
        resolve({});
      }
    });
  });
}

const server = http.createServer(async (req, res) => {
  const origin = req.headers.origin || "*";
  res.setHeader("Access-Control-Allow-Origin", origin);
  res.setHeader("Access-Control-Allow-Credentials", "true");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
  res.setHeader("Content-Type", "application/json");

  const url = req.url || "";
  const requestUrl = new URL(url, "http://localhost");
  const path = requestUrl.pathname;

  if (req.method === "OPTIONS") {
    res.statusCode = 204;
    res.end();
    return;
  }

  if (req.method === "GET" && /^\/api\/images\/topics\/[^/]+(?:\/slots\/\d+)?$/.test(path)) {
    res.setHeader("Content-Type", "image/svg+xml");
    res.end('<svg xmlns="http://www.w3.org/2000/svg" width="16" height="9" viewBox="0 0 16 9"><rect width="16" height="9" fill="#315c55"/><circle cx="12" cy="2" r="1.5" fill="#dfff35"/><path d="M0 9 5 4l3 3 2-2 6 4Z" fill="#eef7e8"/></svg>');
    return;
  }

  if (req.method === "GET" && path === "/api/public/metrics") {
    res.end(JSON.stringify({
      asOf: "2026-08-05",
      registeredUsers: 12500,
      activePremiumUsers: 1840,
      activeLearners30d: 7200,
      totalStudyMinutes: 980000,
      speakingSessions: 86000,
      speakingMinutes: 420000,
    }));
    return;
  }

  if (req.method === "POST" && /^\/api\/learning\/[^/]+\/events$/.test(path)) {
    res.end(JSON.stringify({ ok: true }));
    return;
  }

  if (req.method === "GET" && /^\/api\/gamification\/[^/]+\/energy$/.test(path)) {
    res.end(JSON.stringify({
      current: 5,
      maximum: 5,
      nextRefillAt: "2026-08-06T00:00:00Z",
      consumed: false,
    }));
    return;
  }

  if (req.method === "GET" && path === "/api/speaking/live/capabilities") {
    res.end(JSON.stringify({ enabled: false }));
    return;
  }

  if (req.method === "POST" && path === "/api/speaking/start") {
    res.end(JSON.stringify({
      sessionId: "11111111-2222-4333-8444-555555555555",
      tutorText: "Hello! What would you like to talk about today?",
      tutorAudioBase64: "",
      visemes: [],
      visemeAnimation: null,
      topicProgress: null,
      isNaturalVoice: false,
    }));
    return;
  }

  if (req.method === "POST" && path === "/api/speaking/idea-cards") {
    res.end(JSON.stringify({
      cards: [
        { prompt: "Tell me about your day.", starter: "Today I...", emoji: "💬" },
        { prompt: "Describe something you enjoy.", starter: "I enjoy...", emoji: "✨" },
        { prompt: "Share a simple plan.", starter: "Tomorrow I will...", emoji: "🗓️" },
      ],
    }));
    return;
  }

  if (url.startsWith("/api/auth/config")) {
    res.end(JSON.stringify({ googleClientId: "mock-google-client.apps.googleusercontent.com" }));
    return;
  }

  // ── Placement endpoints ──
  if (req.method === "POST" && url.startsWith("/api/placement/start")) {
    const id = `mock-session-${sessionSeq++}`;
    sessions.set(id, { index: 0 });
    res.end(JSON.stringify({ sessionId: id, currentStage: 1, firstItem: buildItem(0) }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/placement/answer/writing")) {
    const body = await readJson(req);
    const { isCompleted, nextItem } = advance(body.sessionId);
    res.end(JSON.stringify({
      score: 80, level: 1, isTestCompleted: isCompleted, currentStage: 1, currentDifficulty: 1, nextItem,
    }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/placement/answer/speaking")) {
    const body = await readJson(req);
    const { isCompleted, nextItem } = advance(body.sessionId);
    res.end(JSON.stringify({
      score: 75, level: 1, isEstimate: false, isTestCompleted: isCompleted, currentStage: 1, currentDifficulty: 1, nextItem,
    }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/placement/answer")) {
    const body = await readJson(req);
    const { isCompleted, nextItem } = advance(body.sessionId);
    res.end(JSON.stringify({
      wasCorrect: true, isTestCompleted: isCompleted, currentStage: 1, currentDifficulty: 1, nextItem,
    }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/placement/finalize")) {
    res.end(JSON.stringify({
      overallLevel: 1,
      overallScore: 82,
      stageResults: [{ stage: 1, level: 1, score: 82 }],
    }));
    return;
  }

  if (url.startsWith("/api/levels/map/")) {
    res.end(JSON.stringify(levelMapMock()));
    return;
  }
  if (req.method === "POST" && /^\/api\/speaking\/accent-tutors\/[^/]+\/start$/.test(url)) {
    res.end(JSON.stringify({
      text: "Hey there! What have you been up to today?",
      tutorAudioBase64: "",
      isNaturalVoice: false,
      wordTimings: [],
    }));
    return;
  }
  if (req.method === "GET" && /^\/api\/speaking\/accent-tutors\/[^/]+\/voice-live\/token$/.test(url)) {
    res.end(JSON.stringify({
      webSocketUrl: "ws://127.0.0.1:5055/voice-live?",
      authorizationQueryParameter: "authorization",
      authorizationValue: "mock-token",
      expiresAt: "2027-08-15T00:00:00Z",
      session: {
        voiceName: "en-US-AvaMultilingualNeural",
        inputAudioFormat: "pcm16",
        outputAudioFormat: "pcm16",
        inputSamplingRate: 24000,
        silenceDurationMs: 700,
        turnDetectionType: "server_vad",
      },
    }));
    return;
  }
  if (req.method === "POST" && /^\/api\/speaking\/accent-tutors\/[^/]+\/turn$/.test(url)) {
    res.end(JSON.stringify({
      transcript: "I visited the library yesterday.",
      tutorText: "Nice! You could also say, I popped into the library yesterday. What did you read?",
      tutorAudioBase64: "",
      isNaturalVoice: false,
      pronunciation: {
        overallScore: 78,
        accuracyScore: 74,
        fluencyScore: 82,
        completenessScore: 100,
        band: 1,
        isAuthentic: true,
        words: [{ word: "library", accuracyScore: 61, errorType: 1, needsPractice: true, phonemes: [], spokenForm: null }],
      },
      wordTimings: [],
    }));
    return;
  }
  if (url.startsWith("/api/auth/me")) {
    const q = new URL(url, "http://localhost").searchParams;
    const mode = q.get("audit") ?? req.headers["x-englishai-audit-auth"];
    if (mode === "signed-out") { res.statusCode = 401; res.end(JSON.stringify({ message: "Not signed in" })); return; }
    const user = {
      id: "mock-learner-0001",
      email: "demo@englishai.uz",
      displayName: "Demo Learner",
      username: "demo",
      pictureUrl: null,
      hasOnboarded: true,
      preferredName: "Demo",
      learningGoal: 1,
      birthDate: "2000-01-01",
      gender: 1,
      acquisitionSource: 1,
      acquisitionSourceOther: null,
      hasCompletedDemographics: true,
    };
    if (mode === "new-user") {
      user.username = null;
      user.hasOnboarded = false;
      user.preferredName = null;
      user.learningGoal = 0;
      user.birthDate = null;
      user.gender = null;
      user.acquisitionSource = null;
      user.hasCompletedDemographics = false;
    } else if (mode === "onboarding") {
      user.hasOnboarded = false;
      user.learningGoal = 0;
    } else if (mode === "goal") {
      user.learningGoal = 0;
    }
    res.end(JSON.stringify({
      ...user,
    }));
    return;
  }
  if (url.startsWith("/api/auth/username-available")) {
    const q = new URL(url, "http://localhost").searchParams;
    const name = (q.get("username") || "").toLowerCase();
    const valid = /^[a-z0-9_.]+$/.test(name) && name.length >= 3 && name.length <= 30;
    // "demo" is the seeded mock account handle → treat as taken.
    const available = valid && name !== "demo";
    res.end(JSON.stringify({ isValidFormat: valid, isAvailable: available }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/auth/dev-login")) {
    res.end(JSON.stringify({
      token: "mock-token",
      expiresAt: "2027-08-05T00:00:00Z",
      user: {
        id: "mock-learner-0001",
        email: "demo@englishai.uz",
        displayName: "Demo Learner",
        username: "demo",
        pictureUrl: null,
        hasOnboarded: true,
        preferredName: "Demo",
        learningGoal: 1,
      },
    }));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/support/conversation")) {
    res.end(JSON.stringify({
      id: "support-conversation-0001",
      learnerId: "mock-learner-0001",
      learnerName: "Demo Learner",
      learnerEmail: "demo@englishai.uz",
      learnerPictureUrl: null,
      assignedAdminId: null,
      assignedAdminName: null,
      status: 0,
      createdAt: "2026-08-08T10:00:00Z",
      updatedAt: "2026-08-08T10:00:00Z",
      closedAt: null,
      unreadCount: 0,
      messages: [],
    }));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/admin/support/conversations?")) {
    res.end(JSON.stringify([{
      id: "support-conversation-0001",
      learnerId: "mock-learner-0001",
      learnerName: "Demo Learner",
      learnerEmail: "demo@englishai.uz",
      learnerPictureUrl: null,
      assignedAdminId: null,
      assignedAdminName: null,
      status: 0,
      updatedAt: "2026-08-08T10:00:00Z",
      unreadCount: 0,
      lastMessagePreview: "Support test conversation",
    }]));
    return;
  }
  // ── Video lesson + quiz (GET /api/video/{id}, POST /api/video/quiz, POST /api/video/rate) ──
  // Deterministic, DTO-shaped lesson so /video/:id/quiz renders real cards instead of
  // hanging on "loading" when the .NET backend is unauthenticated/unavailable.
  if (req.method === "GET" && /^\/api\/video\/[^/]+$/.test(url) && !url.includes("/catalog")) {
    const id = url.split("/").pop();
    res.end(JSON.stringify({
      id,
      youTubeVideoId: "dQw4w9WgXcQ",
      title: "English Conversation for Beginners",
      channel: "EnglishAI",
      durationSeconds: 312,
      topic: "Daily conversation",
      level: 1,
      status: 2,
      transcriptStatus: 2,
      transcript: [],
      glossary: [
        { word: "hello", translation: "salom", exampleSentence: "Hello, how are you?" },
        { word: "thanks", translation: "rahmat", exampleSentence: "Thanks for your help." },
      ],
      questions: [
        {
          id: "vq-1",
          prompt: "What does the speaker say first when greeting someone?",
          options: ["Goodbye", "Hello, how are you?", "See you later", "I am tired"],
        },
        {
          id: "vq-2",
          prompt: "Which word means 'rahmat' in English?",
          options: ["Sorry", "Please", "Thanks", "Welcome"],
        },
        {
          id: "vq-3",
          prompt: "What is the best reply to 'How are you?'",
          options: ["I am fine, thank you", "Goodbye", "Nice to meet you", "See you"],
        },
        {
          id: "vq-4",
          prompt: "What does 'see you later' mean?",
          options: ["Salom", "Xayr", "Rahmat", "Kechirasiz"],
        },
        {
          id: "vq-5",
          prompt: "Which phrase is used to ask for help?",
          options: ["Can you help me?", "I am sorry", "Good night", "Well done"],
        },
      ],
    }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/video/quiz")) {
    const body = await readJson(req);
    const answers = body.answers || {};
    // The mock doesn't know the correct answers, so grade optimistically: treat the
    // first option as correct for a believable ≥70% pass on the demo lesson.
    const correctOptionIndex = 0;
    const ids = Object.keys(answers);
    const outcomes = ids.map((qid) => ({
      questionId: qid,
      selectedOptionIndex: answers[qid],
      correctOptionIndex,
      isCorrect: answers[qid] === correctOptionIndex,
      hint: answers[qid] === correctOptionIndex ? null : "Diqqat bilan video qismini qayta tinglang.",
    }));
    const correctCount = outcomes.filter((o) => o.isCorrect).length;
    res.end(JSON.stringify({
      lessonId: body.videoLessonId || "mock-lesson",
      totalQuestions: ids.length,
      correctCount,
      passed: correctCount / Math.max(1, ids.length) >= 0.7,
      outcomes,
    }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/video/rate")) {
    res.end(JSON.stringify({ ok: true }));
    return;
  }

  // ── Home dashboard data ──────────────────────────────────────────────
  // Goal-tailored topic suggestions (GET /api/learning/{id}/recommended-topics).
  const recMatch = url.match(/^\/api\/learning\/[^/]+\/recommended-topics/);
  if (recMatch) {
    res.end(JSON.stringify({
      goal: 0,
      level: 1,
      topics: [
        { id: "t-1", title: "Salomlashuv va tanishuv", titleUz: "Greeting & introductions", category: "Daily_Life", level: 1, isGoalRelevant: true },
        { id: "t-2", title: "Oila a'zolari", titleUz: "Family members", category: "People", level: 1, isGoalRelevant: false },
        { id: "t-3", title: "Ranglar va sonlar", titleUz: "Colors & numbers", category: "Basics", level: 1, isGoalRelevant: false },
        { id: "t-4", title: "Ovqatlanish", titleUz: "Food & drink", category: "Daily_Life", level: 1, isGoalRelevant: false },
        { id: "t-5", title: "Vaqt va kun tartibi", titleUz: "Time & daily routine", category: "Daily_Life", level: 1, isGoalRelevant: false },
        { id: "t-6", title: "Yo'nalish va joylar", titleUz: "Directions & places", category: "Travel", level: 1, isGoalRelevant: false },
      ],
    }));
    return;
  }
  // Gamification status (GET /api/gamification/{id}/status) - DTO-compliant shape
  // (see src/api/types.ts GamificationStatusDto). The frontend reads currentStreak,
  // longestStreak, etc. so these field names must match.
  const statusMatch = url.match(/^\/api\/gamification\/[^/]+\/status/);
  if (statusMatch) {
    res.end(JSON.stringify({
      todayCompletedTasks: 3,
      dailyGoalTarget: 4,
      isGoalMet: false,
      currentStreak: 12,
      longestStreak: 27,
      isStreakAtRisk: false,
      skillsCompletedToday: ["Vocabulary", "Grammar", "Reading"],
    }));
    return;
  }
  // Vocabulary due for SRS review (GET /api/vocabulary/{id}/due).
  const dueMatch = url.match(/^\/api\/vocabulary\/[^/]+\/due/);
  if (dueMatch) {
    res.end(JSON.stringify([
      { id: "w-1", word: "apple", translation: "olma", exampleSentence: "I eat an apple.", stage: 0, miniTestType: 0, sourceTopicId: MOCK_TOPIC_ID, partOfSpeech: "noun", options: null },
      { id: "w-2", word: "book", translation: "kitob", exampleSentence: "This is my book.", stage: 1, miniTestType: 0, sourceTopicId: MOCK_TOPIC_ID, partOfSpeech: "noun", options: null },
    ]));
    return;
  }
  if (url.match(/^\/api\/vocabulary\/[^/]+\/review-status/)) {
    res.end(JSON.stringify({ isRequired: false, dueItemCount: 2, dueTopicCount: 1 }));
    return;
  }
  // Saved vocabulary list (GET /api/vocabulary/{id}).
  const vocabMatch = url.match(/^\/api\/vocabulary\/[^/]+$/);
  if (vocabMatch && req.method === "GET") {
    res.end(JSON.stringify([
      { id: "w-1", word: "hello", translation: "salom", exampleSentence: "Hello, my name is Ali.", stage: 0, failCount: 0, nextReviewAt: "2026-08-01T10:00:00Z", sourceTopicId: MOCK_TOPIC_ID, partOfSpeech: "noun" },
      { id: "w-2", word: "name", translation: "ism", exampleSentence: "My name is Ali.", stage: 3, failCount: 0, nextReviewAt: null, sourceTopicId: MOCK_TOPIC_ID, partOfSpeech: "noun" },
    ]));
    return;
  }

  if (url.startsWith("/api/speaking/practice-words/")) {
    res.end(JSON.stringify([
      { id: "practice-1", word: "world", lastAccuracyScore: 61, lastErrorType: 2, errorCount: 2, bestPracticeScore: 74, firstFailedAt: "2026-08-01T10:00:00Z", lastFailedAt: "2026-08-05T10:00:00Z" },
    ]));
    return;
  }

  // ── Gamification endpoints (deterministic demo data for design preview) ──
  // Without these the page rendered zeros behind the spinner. Mirror the DTO
  // shapes in src/api/types.ts so the frontend paints a real, populated arena.
  // NOTE: api.gamification.status(id) hits the BARE path /api/gamification/{id}
  // (no /status suffix), so we match that explicitly.
  if (url.match(/^\/api\/gamification\/[^/]+$/)) {
    res.end(JSON.stringify({
      todayCompletedTasks: 3,
      dailyGoalTarget: 4,
      isGoalMet: false,
      currentStreak: 12,
      longestStreak: 27,
      isStreakAtRisk: false,
      skillsCompletedToday: ["Vocabulary", "Grammar", "Reading"],
    }));
    return;
  }
  if (url.startsWith("/api/gamification/") && url.includes("/leaderboard")) {
    res.end(JSON.stringify({
      level: 1,
      top: [
        { rank: 1, learnerId: "l-1", displayName: "Alijon", pictureUrl: null, score: 9820, isPremium: true, isCurrentUser: false },
        { rank: 2, learnerId: "l-2", displayName: "Madina", pictureUrl: null, score: 8740, isPremium: false, isCurrentUser: false },
        { rank: 3, learnerId: "l-3", displayName: "Shahzod", pictureUrl: null, score: 7610, isPremium: false, isCurrentUser: false },
        { rank: 4, learnerId: "l-4", displayName: "Dilorom", pictureUrl: null, score: 6540, isPremium: true, isCurrentUser: false },
        { rank: 5, learnerId: "l-5", displayName: "Jasur", pictureUrl: null, score: 5120, isPremium: false, isCurrentUser: false },
      ],
      currentUserEntry: { rank: 23, learnerId: "mock-learner-0001", displayName: "Demo Learner", pictureUrl: null, score: 3480, isPremium: false, isCurrentUser: true },
    }));
    return;
  }
  if (url.startsWith("/api/gamification/") && url.endsWith("/points")) {
    res.end(JSON.stringify({
      lifetimeXp: 3480,
      spendableCoins: 320,
      tiers: [
        { coinsCost: 200, discountPercent: 10, canAfford: true },
        { coinsCost: 400, discountPercent: 20, canAfford: false },
      ],
      activeRedemptions: [],
    }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/gamification/") && url.endsWith("/redeem-discount")) {
    res.end(JSON.stringify({
      code: "PRO-DEMO5",
      discountPercent: 5,
      expiresAt: new Date(Date.now() + 30 * 86400000).toISOString(),
    }));
    return;
  }
  if (url.startsWith("/api/gamification/") && url.endsWith("/status")) {
    res.end(JSON.stringify({
      todayCompletedTasks: 3,
      dailyGoalTarget: 4,
      isGoalMet: false,
      currentStreak: 12,
      longestStreak: 27,
      isStreakAtRisk: false,
      skillsCompletedToday: ["Vocabulary", "Grammar", "Reading"],
    }));
    return;
  }

  // ── Learning overview (skill scores feed the progress ring + explorer achievement) ──
  if (url.startsWith("/api/learning/") && url.endsWith("/overview")) {
    res.end(JSON.stringify(mockOverview()));
    return;
  }

  if (url.match(/^\/api\/learning\/[^/]+\/public-progress(?:\?|$)/)) {
    const last7Days = Array.from({ length: 7 }, (_, index) => ({
      day: `2026-08-${String(9 + index).padStart(2, "0")}`,
      seconds: 1200 + index * 180,
    }));
    res.end(JSON.stringify({
      learnerId: "11111111-1111-4111-8111-111111111112",
      displayName: "Demo Learner",
      pictureUrl: null,
      isPremium: true,
      lifetimeXp: 3480,
      rank: 23,
      overallLevel: 3,
      skillScores: [1, 2, 3, 4, 5, 6].map((skill) => ({
        skill,
        score: 68 + skill * 3,
        sampleCount: 12 + skill,
      })),
      topicsTotal: 24,
      topicsLearned: 16,
      topicsMastered: 11,
      studyStats: {
        ...MOCK_STUDY_STATS,
        last7Days,
      },
      gamification: {
        todayCompletedTasks: 3,
        dailyGoalTarget: 4,
        isGoalMet: false,
        currentStreak: 12,
        longestStreak: 27,
        isStreakAtRisk: false,
        skillsCompletedToday: ["Vocabulary", "Grammar", "Reading"],
      },
      growth: [1, 2, 3, 4, 5, 6].flatMap((skill) =>
        [0, 1, 2, 3].map((week) => ({
          weekEnding: `2026-07-${String(20 + week * 7).padStart(2, "0")}`,
          skill,
          score: 62 + skill * 3 + week,
        }))),
    }));
    return;
  }

  if (url.includes("/progress-dashboard")) {
    res.end(JSON.stringify(mockProgressDashboard()));
    return;
  }

  // ── Study stats (GET /api/learning/{id}/study-stats?today=...) ──
  const studyStatsMatch = url.match(/^\/api\/learning\/[^/]+\/study-stats/);
  if (studyStatsMatch) {
    res.end(JSON.stringify(MOCK_STUDY_STATS));
    return;
  }

  // ── Growth points (GET /api/learning/{id}/growth?weeks=8) ──
  const growthMatch = url.match(/^\/api\/learning\/[^/]+\/growth/);
  if (growthMatch) {
    const points = [1, 2, 3, 4, 5, 6].map((skill) => ({ weekEnding: "2026-08-03", skill, score: 68 + skill * 3 }));
    res.end(JSON.stringify(points));
    return;
  }

  // ── Recommendations (GET /api/learning/{id}/recommendations) ──
  const recommendationsMatch = url.match(/^\/api\/learning\/[^/]+\/recommendations/);
  if (recommendationsMatch) {
    res.end(JSON.stringify([
      { code: "streak", text: "Kunningizni 10 daqiqalik mashq bilan boshlang!", skill: null, category: null },
      { code: "vocab", text: "Yangi so'zlar o'rganish davom eting.", skill: 6, category: null },
    ]));
    return;
  }

  // Unread notifications (GET /api/vocabulary/{id}/notifications).
  // Mirrors the real NotificationDto shape consumed by the frontend
  // (see src/api/types.ts): fields are message/code/linkUrl/title/isRead,
  // not the legacy titleUz/bodyUz/kind. The demo feed follows the production
  // reminder codes so the per-code icons in NotificationsPage.iconFor() resolve.
  const notifMatch = path.match(/^\/api\/vocabulary\/[^/]+\/notifications$/);
  if (notifMatch && req.method === "GET") {
    res.end(JSON.stringify([
      {
        id: "n-1",
        code: "notify.daily_learn",
        message: "Ingliz tilini o'rganing! Bugungi mashg'ulotni boshlab, seriyangizni davom ettiring.",
        createdAt: "2026-07-18T08:00:00Z",
        isRead: false,
        linkUrl: "/home",
        title: null,
      },
      {
        id: "n-2",
        code: "notify.daily_plan",
        message: "Bugungi mashg'ulotni boshlang! Kunlik rejangizni bajarib, seriyangizni davom ettiring.",
        createdAt: "2026-07-18T08:05:00Z",
        isRead: false,
        linkUrl: "/home",
        title: null,
      },
      {
        id: "n-3",
        code: "notify.review_due_topic",
        message: "«Salomlashuv va tanishuv» mavzusidagi 3 ta so'zni takrorlash vaqti keldi. Keling, mustahkamlaymiz!",
        createdAt: "2026-07-18T09:15:00Z",
        isRead: false,
        linkUrl: "/vocabulary/review",
        title: null,
      },
      {
        id: "n-4",
        code: "notify.practice_speaking",
        message: "Bugun AI bilan gaplashing - talaffuzingizni charxlang.",
        createdAt: "2026-07-18T10:30:00Z",
        isRead: false,
        linkUrl: "/app/speaking",
        title: null,
      },
      {
        id: "n-5",
        code: "notify.practice_listening",
        message: "Bugun tinglash mashqini bajaring - quloqni ingliz tiliga o'rgating.",
        createdAt: "2026-07-18T11:45:00Z",
        isRead: false,
        linkUrl: "/listening",
        title: null,
      },
      {
        id: "n-6",
        code: "notify.winback_day3",
        message: "Sizni sog'indik! Bugun atigi 5 daqiqa vaqt ajrating va o'rganishni davom ettiring.",
        createdAt: "2026-07-17T14:00:00Z",
        isRead: false,
        linkUrl: "/home",
        title: null,
      },
      {
        id: "n-7",
        code: "notify.admin_broadcast",
        message: "Yangi 'O‘qish' bo‘limi qo‘shildi! Endi matnlarni tushunib o‘qiysiz va savollarga javob berasiz.",
        createdAt: "2026-07-16T12:00:00Z",
        isRead: false,
        linkUrl: "/reading",
        title: "Yangi bo‘lim: O‘qish",
      },
    ]));
    return;
  }

  // Mark all read / dismiss one tapped notification (POST). The mock just acknowledges so
  // the client's best-effort calls never error during the design preview.
  if (notifMatch && req.method === "POST") {
    res.statusCode = 204;
    res.end();
    return;
  }

  // ── Subscription (GET /api/subscription/{id}) ──
  // DTO-compliant active Pro trial for the profile expiry card in the local design preview.
  if (url.match(/^\/api\/subscription\/[^/]+$/)) {
    res.end(JSON.stringify({
      learnerId: "mock-learner-0001",
      status: 0,
      plan: null,
      expiresAt: null,
      daysUntilExpiry: null,
      isPremiumActive: false,
      isTrialActive: true,
      trialExpiresAt: "2026-08-24T23:59:59Z",
      trialDaysUntilExpiry: 32,
    }));
    return;
  }
  if (url.startsWith("/api/subscription/payments/providers")) {
    res.end(JSON.stringify([{ provider: 0, name: "Mock Pay", isMock: true }]));
    return;
  }
  if (url === "/api/developer/keys" && req.method === "GET") {
    res.end(JSON.stringify([]));
    return;
  }
  // ── User preferences (GET /api/auth/preferences) ──
  // DTO-compliant shape (see UserPreferencesDto): DailyGoal.Normal=2, LanguageBalance.Bilingual=2.
  if (url.startsWith("/api/auth/preferences")) {
    res.end(JSON.stringify({
      dailyGoal: 2,
      languageBalance: 2,
      emailNotifications: true,
      pushNotifications: true,
    }));
    return;
  }

  // ── Vocabulary topics for a level (GET /api/vocabulary/topics/{id}?level=...) ──
  // Mirrors VocabularyTopicSummaryDto so the progress "Mavzular progressi" grid paints.
  const vocabTopicsMatch = url.match(/^\/api\/vocabulary\/topics\//);
  if (vocabTopicsMatch) {
    res.end(JSON.stringify(MOCK_TOPICS));
    return;
  }

  if (req.method === "GET" && url.startsWith("/api/vocabulary/topic/")) {
    res.end(JSON.stringify(mockVocabularyTopic()));
    return;
  }
  if (req.method === "POST" && url.includes("/passage-translation")) {
    res.end(JSON.stringify({ topicId: MOCK_TOPIC_ID, sentences: [{ english: "Hello, my name is Ali.", uzbek: "Salom, mening ismim Ali." }] }));
    return;
  }

  if (url.startsWith("/api/grammar/catalog/")) {
    res.end(JSON.stringify(MOCK_TOPICS.map((topic) => ({ topicId: topic.id, title: topic.title, titleUz: topic.titleUz, category: topic.category, level: topic.level, grammarFocusCode: "present-simple" }))));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/grammar/topic/")) {
    res.end(JSON.stringify(mockGrammarLesson()));
    return;
  }

  if (url.startsWith("/api/reading/catalog/")) {
    res.end(JSON.stringify(MOCK_TOPICS.map((topic) => ({ topicId: topic.id, title: topic.title, titleUz: topic.titleUz, category: topic.category, level: topic.level }))));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/reading/topic/")) {
    res.end(JSON.stringify(mockReadingPassage()));
    return;
  }

  if (url.startsWith("/api/listening/catalog/")) {
    res.end(JSON.stringify(MOCK_TOPICS.map((topic) => ({ topicId: topic.id, title: topic.title, titleUz: topic.titleUz, category: topic.category, level: topic.level }))));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/listening/topic/") && !url.endsWith("/audio")) {
    res.end(JSON.stringify(mockListeningExercise()));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/listening/topic/") && url.endsWith("/audio")) {
    res.setHeader("Content-Type", "audio/mpeg");
    res.end(Buffer.alloc(0));
    return;
  }

  if (url.startsWith("/api/writing/catalog/")) {
    res.end(JSON.stringify(MOCK_TOPICS.map((topic) => ({ topicId: topic.id, title: topic.title, titleUz: topic.titleUz, category: topic.category, level: topic.level }))));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/writing/topic/")) {
    res.end(JSON.stringify(mockWritingTask()));
    return;
  }

  if (url.startsWith("/api/books/catalog/")) {
    const detail = mockBookDetail();
    res.end(JSON.stringify([{ ...detail, sectionCount: detail.sections.length }]));
    return;
  }
  if (req.method === "GET" && /^\/api\/books\/[^/]+\/learner\//.test(url)) {
    res.end(JSON.stringify(mockBookDetail()));
    return;
  }
  if (req.method === "GET" && /^\/api\/books\/[^/]+\/sections\/[^/]+\/learner\//.test(url)) {
    res.end(JSON.stringify({
      bookId: MOCK_BOOK_ID,
      sectionId: MOCK_BOOK_SECTION_ID,
      order: 1,
      bookTitle: "A New Friend",
      title: "First day",
      body: "Ali starts a new school and meets Sara in class.",
      level: 1,
      wordCount: 10,
      isReady: true,
      isRead: false,
      questions: [{ id: "book-q1", prompt: "Who does Ali meet?", options: ["Sara", "Tom"] }],
    }));
    return;
  }

  if (url.startsWith("/api/video/feed")) {
    res.end(JSON.stringify({
      items: [{ lessonId: MOCK_VIDEO_ID, youTubeVideoId: "dQw4w9WgXcQ", title: "English Conversation for Beginners", channel: "EnglishAI", durationSeconds: 312, topic: "Daily conversation", level: 1, hasClosedCaptions: true }],
      nextCursor: null,
    }));
    return;
  }

  // ── Free-talk topics (GET /api/speaking/free-talk-topics?level=&all=) ──
  // Mirrors FreeTalkTopicDto (code/englishTitle/level/imageId) so the
  // /speaking/free-talk catalog paints real cards instead of hanging on "loading".
  // The frontend sends only the snake_case `code`; Uzbek labels resolve from the
  // content store by code. 20 topics per CEFR level (A1→C2).
  if (url.startsWith("/api/speaking/free-talk-topics")) {
    const q = new URL(url, "http://localhost").searchParams;
    const levelParam = q.get("level");
    const allLevels = q.get("all") === "true";

    const FREE_TALK_TOPICS = [
      // A1
      ["my_family", "My family", 1], ["my_daily_routine", "My daily routine", 1],
      ["favorite_food", "Favorite food", 1], ["my_house", "My house", 1],
      ["colors_and_numbers", "Colors and numbers", 1], ["my_pet", "My pet", 1],
      ["days_of_the_week", "Days of the week", 1], ["the_weather_today", "The weather today", 1],
      ["my_friends", "My friends", 1], ["clothes_i_wear", "Clothes I wear", 1],
      ["my_school", "My school", 1], ["fruits_and_vegetables", "Fruits and vegetables", 1],
      ["my_hobbies", "My hobbies", 1], ["body_parts", "Body parts", 1],
      ["my_hometown", "My hometown", 1], ["shopping_for_food", "Shopping for food", 1],
      ["my_morning", "My morning", 1], ["telling_the_time", "Telling the time", 1],
      ["my_favorite_animal", "My favorite animal", 1], ["greetings_and_names", "Greetings and names", 1],
      // A2
      ["weekend_plans", "Weekend plans", 2], ["my_last_holiday", "My last holiday", 2],
      ["going_to_the_doctor", "Going to the doctor", 2], ["my_favorite_movie", "My favorite movie", 2],
      ["describing_a_friend", "Describing a friend", 2], ["shopping_for_clothes", "Shopping for clothes", 2],
      ["my_neighborhood", "My neighborhood", 2], ["getting_around_town", "Getting around town", 2],
      ["my_favorite_season", "My favorite season", 2], ["cooking_a_meal", "Cooking a meal", 2],
      ["birthday_celebrations", "Birthday celebrations", 2], ["my_free_time", "My free time", 2],
      ["making_plans_with_friends", "Making plans with friends", 2], ["describing_your_town", "Describing your town", 2],
      ["healthy_habits", "Healthy habits", 2], ["my_favorite_sport", "My favorite sport", 2],
      ["a_typical_workday", "A typical workday", 2], ["using_the_phone", "Using the phone", 2],
      ["asking_for_directions", "Asking for directions", 2], ["my_dream_vacation", "My dream vacation", 2],
      // B1
      ["social_media_habits", "Social media habits", 3], ["learning_a_language", "Learning a language", 3],
      ["my_future_goals", "My future goals", 3], ["travel_experiences", "Travel experiences", 3],
      ["environmental_issues", "Environmental issues", 3], ["technology_in_life", "Technology in life", 3],
      ["cultural_differences", "Cultural differences", 3], ["work_life_balance", "Work-life balance", 3],
      ["music_and_mood", "Music and mood", 3], ["books_vs_movies", "Books vs movies", 3],
      ["healthy_eating", "Healthy eating", 3], ["online_shopping", "Online shopping", 3],
      ["volunteering", "Volunteering", 3], ["city_vs_countryside", "City vs countryside", 3],
      ["dealing_with_stress", "Dealing with stress", 3], ["family_traditions", "Family traditions", 3],
      ["learning_from_mistakes", "Learning from mistakes", 3], ["my_role_model", "My role model", 3],
      ["the_importance_of_sleep", "The importance of sleep", 3], ["social_skills", "Social skills", 3],
      // B2
      ["climate_change", "Climate change", 4], ["artificial_intelligence", "Artificial intelligence", 4],
      ["remote_work", "Remote work", 4], ["mental_health", "Mental health", 4],
      ["globalization", "Globalization", 4], ["entrepreneurship", "Entrepreneurship", 4],
      ["digital_privacy", "Digital privacy", 4], ["higher_education", "Higher education", 4],
      ["sustainable_living", "Sustainable living", 4], ["the_gig_economy", "The gig economy", 4],
      ["space_exploration", "Space exploration", 4], ["gender_equality", "Gender equality", 4],
      ["the_news_media", "The news media", 4], ["urbanization", "Urbanization", 4],
      ["cultural_identity", "Cultural identity", 4], ["scientific_ethics", "Scientific ethics", 4],
      ["the_future_of_work", "The future of work", 4], ["minimalism", "Minimalism", 4],
      ["genetic_engineering", "Genetic engineering", 4], ["tourism_impact", "Tourism impact", 4],
      // C1
      ["economic_inequality", "Economic inequality", 5], ["political_polarization", "Political polarization", 5],
      ["bioethics", "Bioethics", 5], ["the_attention_economy", "The attention economy", 5],
      ["post_truth_society", "Post-truth society", 5], ["automation_and_jobs", "Automation and jobs", 5],
      ["cultural_appropriation", "Cultural appropriation", 5], ["the_ethics_of_ai", "The ethics of AI", 5],
      ["climate_adaptation", "Climate adaptation", 5], ["renewable_energy", "Renewable energy", 5],
      ["social_mobility", "Social mobility", 5], ["media_literacy", "Media literacy", 5],
      ["the_philosophy_of_happiness", "The philosophy of happiness", 5], ["urban_sustainability", "Urban sustainability", 5],
      ["data_and_privacy", "Data and privacy", 5], ["the_future_of_education", "The future of education", 5],
      ["geopolitical_shifts", "Geopolitical shifts", 5], ["the_creator_economy", "The creator economy", 5],
      ["ethical_consumption", "Ethical consumption", 5], ["loneliness_in_society", "Loneliness in society", 5],
      // C2
      ["the_nature_of_consciousness", "The nature of consciousness", 6], ["existential_risk", "Existential risk", 6],
      ["the_limits_of_knowledge", "The limits of knowledge", 6], ["democratic_resilience", "Democratic resilience", 6],
      ["the_philosophy_of_language", "The philosophy of language", 6], ["technological_singularity", "Technological singularity", 6],
      ["moral_luck", "Moral luck", 6], ["collective_action", "Collective action", 6],
      ["the_anthropocene", "The anthropocene", 6], ["narrative_and_identity", "Narrative and identity", 6],
      ["the_economics_of_attention", "The economics of attention", 6], ["wisdom_and_aging", "Wisdom and aging", 6],
      ["the_ethics_of_memory", "The ethics of memory", 6], ["civilizational_risk", "Civilizational risk", 6],
      ["the_ontology_of_time", "The ontology of time", 6], ["meaning_in_modernity", "Meaning in modernity", 6],
      ["the_politics_of_truth", "The politics of truth", 6], ["collective_memory", "Collective memory", 6],
      ["the_aesthetics_of_silence", "The aesthetics of silence", 6], ["trust_in_institutions", "Trust in institutions", 6],
    ];

    let topics = FREE_TALK_TOPICS.map(([code, englishTitle, lvl]) => ({
      code,
      englishTitle,
      level: lvl,
      imageId: MOCK_TOPICS[(lvl - 1) % MOCK_TOPICS.length].id,
    }));

    // `all` returns the whole catalog; `level` narrows to that band.
    if (levelParam) {
      const lvl = Number(levelParam);
      if (!Number.isNaN(lvl)) topics = topics.filter((t) => t.level === lvl);
    }

    res.end(JSON.stringify(topics));
    return;
  }

  // ── Referral status (GET /api/referral) ──
  // DTO-compliant shape (see ReferralStatusDto).
  if (url.startsWith("/api/referral")) {
    res.end(JSON.stringify({
      code: "DEMOLEARN",
      invitedCount: 0,
      qualifiedCount: 0,
      rewardCap: 10,
      bonusTopicsUnlocked: 0,
      remainingSpeakingCredits: 0,
      remainingWritingCredits: 0,
      topicsPerReferral: 2,
      speakingCreditsPerReferral: 1,
      writingCreditsPerReferral: 1,
    }));
    return;
  }

  // ── Word pronunciation detail (GET /api/speaking/word/:word) ──
  // Mirrors WordPronunciationDetailDto so the /speaking/pronunciation/:word screen
  // paints real cards instead of hanging on "loading". Without this the mock returns
  // {ok:true} which fails shape validation and the screen never leaves the spinner.
  const wordMatch = url.match(/^\/api\/speaking\/word\/([^/]+)/);
  if (wordMatch) {
    const raw = decodeURIComponent(wordMatch[1]);
    const word = raw.charAt(0).toUpperCase() + raw.slice(1);
    const ipa = `/${word.toLowerCase()}/`;
    // Split the word into a small IPA-ish phoneme list for the 3D chips.
    const letters = word.replace(/[^a-zA-Z]/g, "").split("");
    const phonemes = letters.map((l, i) => ({
      phoneme: l.toLowerCase(),
      svgId: `ph-${i}`,
      isHardForUzbek: ["r", "w", "th", "v"].includes(l.toLowerCase()),
    }));
    res.end(JSON.stringify({
      word,
      ipa,
      phonemes,
      tipUz:
        "So'zni sekin va aniq ayting. Og'iz holatini yuqoridagi animatsiyadan kuzating.",
      visemes: phonemes.map((_, i) => ({ id: `v-${i}`, start: i * 0.2, end: i * 0.2 + 0.2 })),
      visemeAnimation: null,
      audioBase64: null,
      keyWord: word,
      exampleSentenceUz: `"${word}" so'zi ingliz tilida ko'p ishlatiladi.`,
    }));
    return;
  }

  // ── Roleplay scenarios (GET /api/speaking/roleplay/scenarios) ──
  // Returns the full curated catalog (120 scenarios, 20 per CEFR level A1→C2) so the
  // /speaking/roleplay picker paints real dark 3D cards instead of hanging on the {ok:true}
  // fallback (which is an object, not an array, and crashes the page).
  if (url.startsWith("/api/speaking/roleplay/scenarios")) {
    const q = new URL(req.url, "http://localhost").searchParams;
    const levelParam = q.get("level");
    const all = q.get("all") === "true";
    const catalog = [
      {"code":"airport","englishTitle":"airport","level":0,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9"},
{"code":"restaurant","englishTitle":"restaurant","level":0,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a0"},
{"code":"shop","englishTitle":"shop","level":0,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b1"},
{"code":"cafe","englishTitle":"cafe","level":0,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c2"},
{"code":"taxi","englishTitle":"taxi","level":0,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d3"},
{"code":"hotel_check_in","englishTitle":"hotel check in","level":0,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3e4"},
{"code":"bus_ticket","englishTitle":"bus ticket","level":0,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4f5"},
{"code":"bakery","englishTitle":"bakery","level":0,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f506"},
{"code":"fruit_market","englishTitle":"fruit market","level":0,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f50617"},
{"code":"asking_directions","englishTitle":"asking directions","level":0,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f5061728"},
{"code":"new_neighbor","englishTitle":"new neighbor","level":0,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f506172839"},
{"code":"pharmacy","englishTitle":"pharmacy","level":0,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f5061728394a"},
{"code":"post_office","englishTitle":"post office","level":0,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a5b"},
{"code":"supermarket","englishTitle":"supermarket","level":0,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b6c"},
{"code":"pizza_order","englishTitle":"pizza order","level":0,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c7d"},
{"code":"lost_bag","englishTitle":"lost bag","level":0,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d8e"},
{"code":"zoo_tickets","englishTitle":"zoo tickets","level":0,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9"},
{"code":"library_card","englishTitle":"library card","level":0,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a1"},
{"code":"ice_cream_stand","englishTitle":"ice cream stand","level":0,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b2"},
{"code":"new_classmate","englishTitle":"new classmate","level":0,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c3"},
{"code":"job_interview","englishTitle":"job interview","level":1,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d4"},
{"code":"doctor","englishTitle":"doctor","level":1,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3e5"},
{"code":"hairdresser","englishTitle":"hairdresser","level":1,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4f6"},
{"code":"bank_account","englishTitle":"bank account","level":1,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f507"},
{"code":"lost_luggage","englishTitle":"lost luggage","level":1,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f50618"},
{"code":"train_station","englishTitle":"train station","level":1,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f5061729"},
{"code":"bike_rental","englishTitle":"bike rental","level":1,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f50617283a"},
{"code":"hotel_problem","englishTitle":"hotel problem","level":1,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f5061728394b"},
{"code":"return_item","englishTitle":"return item","level":1,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a5c"},
{"code":"clinic_call","englishTitle":"clinic call","level":1,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b6d"},
{"code":"gym_signup","englishTitle":"gym signup","level":1,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c7e"},
{"code":"food_delivery","englishTitle":"food delivery","level":1,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d8f"},
{"code":"phone_shop","englishTitle":"phone shop","level":1,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f0"},
{"code":"dentist","englishTitle":"dentist","level":1,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a2"},
{"code":"tourist_info","englishTitle":"tourist info","level":1,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b3"},
{"code":"birthday_invitation","englishTitle":"birthday invitation","level":1,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c4"},
{"code":"cinema_tickets","englishTitle":"cinema tickets","level":1,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d5"},
{"code":"flower_shop","englishTitle":"flower shop","level":1,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3e6"},
{"code":"talking_to_teacher","englishTitle":"talking to teacher","level":1,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4f7"},
{"code":"lost_wallet","englishTitle":"lost wallet","level":1,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f508"},
{"code":"apartment_viewing","englishTitle":"apartment viewing","level":2,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f50619"},
{"code":"internship_interview","englishTitle":"internship interview","level":2,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f506172a"},
{"code":"bank_card_issue","englishTitle":"bank card issue","level":2,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f50617283b"},
{"code":"missed_flight","englishTitle":"missed flight","level":2,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f5061728394c"},
{"code":"restaurant_complaint","englishTitle":"restaurant complaint","level":2,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a5d"},
{"code":"travel_agency","englishTitle":"travel agency","level":2,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b6e"},
{"code":"new_coworker","englishTitle":"new coworker","level":2,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c7f"},
{"code":"market_bargaining","englishTitle":"market bargaining","level":2,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d80"},
{"code":"car_rental","englishTitle":"car rental","level":2,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f1"},
{"code":"tech_support","englishTitle":"tech support","level":2,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a3"},
{"code":"hotel_booking_change","englishTitle":"hotel booking change","level":2,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b4"},
{"code":"gym_cancellation","englishTitle":"gym cancellation","level":2,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c5"},
{"code":"university_admissions","englishTitle":"university admissions","level":2,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d6"},
{"code":"insurance_claim","englishTitle":"insurance claim","level":2,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3e7"},
{"code":"cake_order","englishTitle":"cake order","level":2,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4f8"},
{"code":"car_mechanic","englishTitle":"car mechanic","level":2,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f509"},
{"code":"parent_teacher_meeting","englishTitle":"parent teacher meeting","level":2,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f5061a"},
{"code":"career_advisor","englishTitle":"career advisor","level":2,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f506172b"},
{"code":"trip_with_friend","englishTitle":"trip with friend","level":2,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f50617283c"},
{"code":"mobile_plan","englishTitle":"mobile plan","level":2,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f5061728394d"},
{"code":"salary_negotiation","englishTitle":"salary negotiation","level":3,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a5e"},
{"code":"competency_interview","englishTitle":"competency interview","level":3,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b6f"},
{"code":"performance_review","englishTitle":"performance review","level":3,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c70"},
{"code":"lease_negotiation","englishTitle":"lease negotiation","level":3,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d81"},
{"code":"bank_loan","englishTitle":"bank loan","level":3,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f2"},
{"code":"visa_interview","englishTitle":"visa interview","level":3,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a4"},
{"code":"pitching_an_idea","englishTitle":"pitching an idea","level":3,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b5"},
{"code":"complaint_escalation","englishTitle":"complaint escalation","level":3,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c6"},
{"code":"networking_event","englishTitle":"networking event","level":3,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d7"},
{"code":"medical_results","englishTitle":"medical results","level":3,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3e8"},
{"code":"cancelled_flight","englishTitle":"cancelled flight","level":3,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4f9"},
{"code":"seminar_debate","englishTitle":"seminar debate","level":3,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f50a"},
{"code":"buying_a_car","englishTitle":"buying a car","level":3,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f5061b"},
{"code":"client_meeting","englishTitle":"client meeting","level":3,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f506172c"},
{"code":"roommate_conflict","englishTitle":"roommate conflict","level":3,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f50617283d"},
{"code":"deadline_pushback","englishTitle":"deadline pushback","level":3,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f5061728394e"},
{"code":"house_purchase","englishTitle":"house purchase","level":3,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a5f"},
{"code":"promotion_case","englishTitle":"promotion case","level":3,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b60"},
{"code":"friendly_debate","englishTitle":"friendly debate","level":3,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c71"},
{"code":"claim_dispute","englishTitle":"claim dispute","level":4,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d82"},
{"code":"panel_interview","englishTitle":"panel interview","level":4,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f3"},
{"code":"board_presentation","englishTitle":"board presentation","level":4,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a5"},
{"code":"investor_pitch","englishTitle":"investor pitch","level":4,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b6"},
{"code":"contract_negotiation","englishTitle":"contract negotiation","level":4,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c7"},
{"code":"media_crisis_interview","englishTitle":"media crisis interview","level":4,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d8"},
{"code":"conference_qna","englishTitle":"conference qna","level":4,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3e9"},
{"code":"lawyer_consultation","englishTitle":"lawyer consultation","level":4,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4fa"},
{"code":"policy_debate","englishTitle":"policy debate","level":4,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f50b"},
{"code":"partnership_negotiation","englishTitle":"partnership negotiation","level":4,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f5061c"},
{"code":"giving_feedback","englishTitle":"giving feedback","level":4,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f506172d"},
{"code":"diplomatic_reception","englishTitle":"diplomatic reception","level":4,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f50617283e"},
{"code":"medical_second_opinion","englishTitle":"medical second opinion","level":4,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f5061728394f"},
{"code":"mediation_session","englishTitle":"mediation session","level":4,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a50"},
{"code":"thesis_defense","englishTitle":"thesis defense","level":4,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b61"},
{"code":"press_briefing","englishTitle":"press briefing","level":4,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c72"},
{"code":"supplier_negotiation","englishTitle":"supplier negotiation","level":4,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d83"},
{"code":"expert_panel","englishTitle":"expert panel","level":4,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f4"},
{"code":"key_client_rescue","englishTitle":"key client rescue","level":4,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a6"},
{"code":"town_hall","englishTitle":"town hall","level":4,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b7"},
{"code":"radio_interview","englishTitle":"radio interview","level":4,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c8"},
{"code":"keynote_qna","englishTitle":"keynote qna","level":4,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2d9"},
{"code":"live_tv_debate","englishTitle":"live tv debate","level":5,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3ea"},
{"code":"parliamentary_hearing","englishTitle":"parliamentary hearing","level":5,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4fb"},
{"code":"summit_negotiation","englishTitle":"summit negotiation","level":5,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f50c"},
{"code":"crisis_press_conference","englishTitle":"crisis press conference","level":5,"imageId":"8293a4b5-c6d7-e8f9-a0b1-c2d3e4f5061d"},
{"code":"philosophy_seminar","englishTitle":"philosophy seminar","level":5,"imageId":"93a4b5c6-d7e8-f9a0-b1c2-d3e4f506172e"},
{"code":"expert_witness","englishTitle":"expert witness","level":5,"imageId":"a4b5c6d7-e8f9-a0b1-c2d3-e4f50617283f"},
{"code":"emergency_board_meeting","englishTitle":"emergency board meeting","level":5,"imageId":"b5c6d7e8-f9a0-b1c2-d3e4-f50617283940"},
{"code":"diplomatic_negotiation","englishTitle":"diplomatic negotiation","level":5,"imageId":"c6d7e8f9-a0b1-c2d3-e4f5-061728394a51"},
{"code":"adversarial_interview","englishTitle":"adversarial interview","level":5,"imageId":"d7e8f9a0-b1c2-d3e4-f506-1728394a5b62"},
{"code":"peer_review_defense","englishTitle":"peer review defense","level":5,"imageId":"e8f9a0b1-c2d3-e4f5-0617-28394a5b6c73"},
{"code":"merger_negotiation","englishTitle":"merger negotiation","level":5,"imageId":"f9a0b1c2-d3e4-f506-1728-394a5b6c7d84"},
{"code":"policy_advisory","englishTitle":"policy advisory","level":5,"imageId":"0a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f5"},
{"code":"literary_panel","englishTitle":"literary panel","level":5,"imageId":"1b2c3d4e-5f60-7182-9304-b5c6d7e8f9a7"},
{"code":"ethics_committee","englishTitle":"ethics committee","level":5,"imageId":"2c3d4e5f-6071-8293-a4b5-c6d7e8f9a0b8"},
{"code":"final_investment_round","englishTitle":"final investment round","level":5,"imageId":"3d4e5f60-7182-9304-b5c6-d7e8f9a0b1c9"},
{"code":"moderating_a_panel","englishTitle":"moderating a panel","level":5,"imageId":"4e5f6071-8293-a4b5-c6d7-e8f9a0b1c2da"},
{"code":"ambassador_briefing","englishTitle":"ambassador briefing","level":5,"imageId":"5f607182-9304-b5c6-d7e8-f9a0b1c2d3eb"},
{"code":"labor_negotiation","englishTitle":"labor negotiation","level":5,"imageId":"60718293-a4b5-c6d7-e8f9-a0b1c2d3e4fc"},
{"code":"legacy_interview","englishTitle":"legacy interview","level":5,"imageId":"718293a4-b5c6-d7e8-f9a0-b1c2d3e4f50d"}
    ];
    res.setHeader("Content-Type", "application/json");
    res.end(JSON.stringify(
      levelParam != null && !all
        ? catalog.filter((s) => String(s.level) === String(levelParam))
        : catalog,
    ));
    return;
  }

  // Default 200 for any other /api call so the app shell doesn't error out during preview.
  // ── Admin access (GET /api/admin/access) ──
  if (url.startsWith("/api/admin/access")) {
    res.end(JSON.stringify({ role: 2, isAdmin: true, canManageAdmins: true }));
    return;
  }

  if (req.method === "GET" && url.startsWith("/api/admin/users/")) {
    res.end(JSON.stringify({
      id: MOCK_ADMIN_ID,
      email: "admin@englishai.uz",
      displayName: "Demo Admin",
      preferredName: "Admin",
      username: "demo-admin",
      pictureUrl: null,
      role: 2,
      registeredAt: "2026-01-01T10:00:00Z",
      lastLoginAt: "2026-08-05T10:00:00Z",
      proTrialExpiresAt: "2026-09-01T00:00:00Z",
      isProTrialActive: true,
      hasOnboarded: true,
      level: "B1",
      learningGoal: "Career",
      profileCreatedAt: "2026-01-01T10:00:00Z",
      profileUpdatedAt: "2026-08-05T10:00:00Z",
      lastActivityAt: "2026-08-05T10:00:00Z",
      confirmationTestPassedAt: null,
      lastWinBackStage: "Active",
      skillSeedCount: 6,
      activityCount: 18,
      errorObservationCount: 4,
      subscriptionStatus: "Premium",
      subscriptionPlan: "Monthly",
      subscriptionExpiresAt: "2026-09-01T00:00:00Z",
      subscriptionCreatedAt: "2026-01-03T00:00:00Z",
      subscriptionUpdatedAt: "2026-08-01T00:00:00Z",
      learning: {
        study: MOCK_STUDY_STATS,
        gamification: mockProgressDashboard().gamification,
        skills: [], errors: [], topics: { startedTopics: 3, masteredTopics: 1, passedModules: 9 },
        vocabulary: { total: 24, learning: 18, mastered: 6, due: 3, addedLast7Days: 5, addedLast30Days: 14, totalFailCount: 2, stageBreakdown: [] },
        churn: { overallRisk: "Low", isAtRisk: false, signals: [] },
        registeredDeviceCount: 1,
        devicePlatforms: ["Web"],
      },
    }));
    return;
  }
  if (req.method === "GET" && url.startsWith("/api/admin/users")) {
    res.end(JSON.stringify({
      viewerRole: 2,
      totalUsers: 1,
      adminCount: 1,
      onboardedCount: 1,
      premiumCount: 1,
      nextCursor: null,
      users: [{ id: MOCK_ADMIN_ID, email: "admin@englishai.uz", displayName: "Demo Admin", username: "demo-admin", pictureUrl: null, role: 2, registeredAt: "2026-01-01T10:00:00Z", lastLoginAt: "2026-08-05T10:00:00Z", hasOnboarded: true, level: "B1", lastActivityAt: "2026-08-05T10:00:00Z", subscriptionStatus: "Premium", subscriptionPlan: "Monthly", subscriptionExpiresAt: "2026-09-01T00:00:00Z" }],
    }));
    return;
  }

  if (req.method === "GET" && url === "/api/admin/curriculum/runs") {
    res.end(JSON.stringify([{ id: "run-1", version: "v4", provider: "Mock", model: "fixture", status: "Completed", createdAt: "2026-08-01T00:00:00Z", updatedAt: "2026-08-02T00:00:00Z", completedAt: "2026-08-02T00:00:00Z", publishedAt: "2026-08-03T00:00:00Z", totalItems: 1, approvedItems: 1, publishedItems: 1, problemItems: 0 }]));
    return;
  }
  if (req.method === "GET" && /\/api\/admin\/curriculum\/runs\/[^/]+\/items/.test(url)) {
    res.end(JSON.stringify([{ id: "item-1", module: "Vocabulary", subjectKey: "greetings", level: "A1", status: "Published", attemptCount: 1, errorCode: null, errorMessage: null, updatedAt: "2026-08-02T00:00:00Z" }]));
    return;
  }

  if (req.method === "GET" && url.startsWith("/api/admin/broadcasts")) {
    res.end(JSON.stringify({ items: [{ id: "broadcast-1", title: "Yangi dars", body: "Bugungi dars tayyor.", linkUrl: "/home", createdAt: "2026-08-05T08:00:00Z" }], nextCursor: null }));
    return;
  }
  if (req.method === "POST" && url.startsWith("/api/admin/force-due-reviews")) {
    res.end(JSON.stringify({ totalWords: 2, dueNow: 2, seededWords: 0 }));
    return;
  }

  // ── Founder growth dashboard metrics (GET /api/admin/metrics) ──
  // Serves a deterministic, realistic payload so the /admin/metrics screen paints
  // real cards instead of hanging on "Yuklanmoqda" when no .NET backend is running.
  if (req.method === "GET" && url.startsWith("/api/admin/metrics")) {
    const dailyTrend = (base) =>
      Array.from({ length: 30 }, (_, i) => {
        const d = new Date();
        d.setDate(d.getDate() - (29 - i));
        const wobble = Math.round(Math.sin(i / 3) * base * 0.08);
        const growth = Math.round((i / 29) * base * 0.15);
        return {
          day: d.toISOString().slice(0, 10),
          count: base + wobble + growth,
        };
      });

    const asOf = new Date().toISOString();
    const activationFunnel = [
      { code: "registered", count: 24860, rateFromRegisteredPct: 100, rateFromPreviousPct: 100 },
      { code: "usernameSetupCompleted", count: 22110, rateFromRegisteredPct: 89, rateFromPreviousPct: 89 },
      { code: "placementStarted", count: 19040, rateFromRegisteredPct: 77, rateFromPreviousPct: 86 },
      { code: "placementCompleted", count: 16880, rateFromRegisteredPct: 68, rateFromPreviousPct: 89 },
      { code: "firstTopicOpened", count: 14920, rateFromRegisteredPct: 60, rateFromPreviousPct: 88 },
      { code: "firstSpeakingSessionCompleted", count: 11870, rateFromRegisteredPct: 48, rateFromPreviousPct: 80 },
      { code: "firstPronunciationFeedbackViewed", count: 10430, rateFromRegisteredPct: 42, rateFromPreviousPct: 88 },
      { code: "day1Returned", count: 9120, rateFromRegisteredPct: 37, rateFromPreviousPct: 87 },
      { code: "day7Active", count: 6210, rateFromRegisteredPct: 25, rateFromPreviousPct: 68 },
      { code: "monetizationIntent", count: 4180, rateFromRegisteredPct: 17, rateFromPreviousPct: 67 },
    ];

    const payload = {
      asOf,
      overview: {
        totalUsers: 24860,
        onboardedUsers: 16880,
        premiumUsers: 3120,
        freeUsers: 21740,
        conversionRatePct: 12.6,
        dau: 7840,
        wau: 18920,
        mau: 24610,
        stickinessPct: 41.3,
        newUsersToday: 412,
        newUsers7d: 2640,
        mrrUzs: 47280000,
        arppuUzs: 15154,
      },
      activeUsersTrend: dailyTrend(7200),
      signupsTrend: dailyTrend(320),
      retention: {
        d1: { cohortSize: 9120, retained: 7120, ratePct: 78.1 },
        d7: { cohortSize: 6210, retained: 3310, ratePct: 53.3 },
        d30: { cohortSize: 4180, retained: 1490, ratePct: 35.6 },
      },
      funnel: {
        registered: 24860,
        onboarded: 16880,
        active7d: 7840,
        premium: 3120,
      },
      activationFunnel,
      goalSegments: [
        { goal: "IeltsCefr", users: 6120, premiumUsers: 1180, conversionRatePct: 19.3, d7: { cohortSize: 6120, retained: 3680, ratePct: 60.1 }, mrrUzs: 21240000 },
        { goal: "Work", users: 5340, premiumUsers: 940, conversionRatePct: 17.6, d7: { cohortSize: 5340, retained: 3010, ratePct: 56.4 }, mrrUzs: 13920000 },
        { goal: "Migration", users: 4210, premiumUsers: 610, conversionRatePct: 14.5, d7: { cohortSize: 4210, retained: 2210, ratePct: 52.5 }, mrrUzs: 9180000 },
        { goal: "Travel", users: 3980, premiumUsers: 280, conversionRatePct: 7.0, d7: { cohortSize: 3980, retained: 1910, ratePct: 48.0 }, mrrUzs: 3360000 },
        { goal: "GeneralSpeaking", users: 3120, premiumUsers: 70, conversionRatePct: 2.2, d7: { cohortSize: 3120, retained: 1380, ratePct: 44.2 }, mrrUzs: 840000 },
        { goal: "School", users: 2090, premiumUsers: 40, conversionRatePct: 1.9, d7: { cohortSize: 2090, retained: 900, ratePct: 43.1 }, mrrUzs: 480000 },
      ],
      planBreakdown: [
        { plan: "Monthly", count: 1480, mrrUzs: 13320000 },
        { plan: "Quarterly", count: 940, mrrUzs: 16920000 },
        { plan: "SemiAnnual", count: 420, mrrUzs: 11340000 },
        { plan: "Yearly", count: 280, mrrUzs: 5700000 },
      ],
      demographics: {
        completed: 21840,
        missing: 3020,
        completionRatePct: 87.9,
        gender: [
          { label: "Male", count: 11320, ratePct: 51.8 },
          { label: "Female", count: 10520, ratePct: 48.2 },
        ],
        acquisitionSources: [
          { label: "Telegram", count: 7640, ratePct: 35.0 },
          { label: "Instagram", count: 5680, ratePct: 26.0 },
          { label: "Google", count: 4580, ratePct: 21.0 },
          { label: "Other", count: 3940, ratePct: 18.0 },
        ],
        ageGroups: [
          { label: "13-17", count: 3280, ratePct: 15.0 },
          { label: "18-24", count: 9828, ratePct: 45.0 },
          { label: "25-34", count: 6552, ratePct: 30.0 },
          { label: "35+", count: 2180, ratePct: 10.0 },
        ],
      },
    };
    res.end(JSON.stringify(payload));
    return;
  }

  // ── Super-admin server diagnostics (GET /api/admin/server) ──
  if (req.method === "GET" && url.startsWith("/api/admin/server") && !url.includes("/logs")) {
    const now = new Date().toISOString();
    const started = new Date(Date.now() - 1000 * 60 * 60 * 37).toISOString();
    res.end(JSON.stringify({
      generatedAtUtc: now,
      overallStatus: "Healthy",
      runtime: {
        environment: "Production",
        version: "2.4.1",
        framework: ".NET 9.0",
        operatingSystem: "Linux 6.8 (Ubuntu 24.04)",
        machineName: "ea-prod-web-01",
        startedAtUtc: started,
        uptimeSeconds: 133200,
        processorCount: 4,
        managedMemoryMb: 312,
        workingSetMb: 748,
        threadCount: 28,
        gen0Collections: 1842,
        gen1Collections: 213,
        gen2Collections: 41,
      },
      dependencies: [
        { name: "PostgreSQL", status: "Healthy", detail: "Asosiy ma'lumotlar bazasi ulanishi faol.", latencyMs: 12 },
        { name: "Redis", status: "Healthy", detail: "Kesh va sessiya do'koni mavjud.", latencyMs: 3 },
      ],
      services: [
        { name: "Azure Speech", configured: true, detail: "Talaffuz baholash uchun ulangan." },
        { name: "OpenAI", configured: true, detail: "AI tutor va tarjima uchun ulangan." },
        { name: "SendGrid", configured: false, detail: "Email xabarnomalar hali sozlanmagan." },
      ],
      logs: {
        warningCount: 1,
        errorCount: 0,
        capacity: 200,
        recent: [
          { id: "log-1", timestampUtc: now, level: "Warning", message: "Redis keshida yuqori kechikish (38 ms) qayd etildi - kuzatuvda.", exception: null },
        ],
      },
    }));
    return;
  }
  if (req.method === "DELETE" && /\/api\/admin\/server\/logs\//.test(url)) {
    res.statusCode = 204;
    res.end();
    return;
  }
  if (req.method === "DELETE" && url.startsWith("/api/admin/server/logs")) {
    res.statusCode = 204;
    res.end();
    return;
  }

  const adminGrammarMatch = url.match(/^\/api\/admin\/grammar(?:\/([^/]+))?(?:\/full)?$/);
  if (adminGrammarMatch && req.method === "GET") {
    const detail = {
      id: MOCK_ADMIN_GRAMMAR_ID, title: "Present Simple", category: "VerbTense", level: "A1", status: "Filled", exerciseCount: 1,
      vocabularyTopicId: MOCK_TOPIC_ID, createdAt: "2026-08-01T00:00:00Z", grammarFocusCode: "present-simple", contextIntro: "I am Ali.", explanation: "Use present simple.",
      curatedTitleUz: "Hozirgi oddiy zamon", curatedSummaryUz: "Odatlar uchun.", curatedFormulas: ["Subject + verb"], curatedRules: [{ headingUz: "Qoida", bodyUz: "I bilan am." }],
      examples: [{ english: "I am Ali.", uzbek: "Men Aliman." }], commonMistakes: [], exercises: [{ type: "Recognition", prompt: "Choose", options: ["am", "is"], correctOptionIndex: 0, hintCode: null, explanation: "I uses am." }], applicationTasks: [],
    };
    res.end(JSON.stringify(adminGrammarMatch[1] ? detail : [detail]));
    return;
  }

  const adminListeningMatch = url.match(/^\/api\/admin\/listening(?:\/([^/]+))?(?:\/full)?$/);
  if (adminListeningMatch && req.method === "GET") {
    const detail = {
      id: MOCK_ADMIN_LISTENING_ID, title: "Airport", topic: "travel", level: "A2", status: "Filled", questionCount: 1, wordCount: 4,
      vocabularyTopicId: MOCK_TOPIC_ID, createdAt: "2026-08-01T00:00:00Z", transcript: "Welcome to the airport.",
      audio: { streamUrl: "/audio", hasCachedAudio: false, sizeBytes: null, generatedAt: null, contentType: "audio/mpeg" }, segments: [],
      questions: [{ id: "listening-admin-q1", prompt: "Where?", options: ["Airport", "School"], correctOptionIndex: 0, hintCode: null, explanation: "The speaker says airport." }], vocabularyContext: [],
    };
    res.end(JSON.stringify(adminListeningMatch[1] ? detail : [detail]));
    return;
  }

  const adminReadingMatch = url.match(/^\/api\/admin\/reading(?:\/([^/]+))?(?:\/full)?$/);
  if (adminReadingMatch && req.method === "GET") {
    const detail = {
      id: MOCK_ADMIN_READING_ID, title: "City gardens", topic: "Nature", category: "environment", level: "B1", status: "Filled", questionCount: 1, wordCount: 20,
      vocabularyTopicId: MOCK_TOPIC_ID, createdAt: "2026-08-01T00:00:00Z", body: "First paragraph.\n\nSecond paragraph.", sections: ["First paragraph.", "Second paragraph."], contextGaps: null,
      vocabulary: [{ id: "reading-word-1", word: "garden", translation: "bog‘", exampleSentence: "A city garden." }],
      questions: [{ id: "reading-admin-q1", prompt: "Where is the garden?", options: ["City", "Village"], correctOptionIndex: 0, hintCode: "place", explanation: "It says city." }],
    };
    res.end(JSON.stringify(adminReadingMatch[1] ? detail : [detail]));
    return;
  }

  if (url === "/api/admin/vocabulary-images" && req.method === "GET") {
    res.end(JSON.stringify([
      {
        topicId: MOCK_TOPIC_ID,
        imageId: "91111111-1111-4111-8111-111111111111",
        topicTitle: "Salomlashuv va tanishuv",
        level: "A1",
        word: "hello",
        translation: "salom",
        imageUrl: `/api/images/topics/${MOCK_TOPIC_ID}`,
        imageSource: "mock",
        imageAttribution: null,
        version: 1,
      },
    ]));
    return;
  }

  // ── Admin vocabulary topics (GET /api/admin/vocabulary) ──
  // Mirrors AdminVocabularyTopicDto so the /admin/vocabulary admin screen paints real
  // 3D colored topic cards instead of hanging on the {ok:true} fallback (an object, not
  // an array, which crashes the page before it ever leaves the spinner).
  const ADMIN_VOCAB_MATCH = url.match(/^\/api\/admin\/vocabulary(\/([0-9a-fA-F-]{36}))?$/);
  if (ADMIN_VOCAB_MATCH) {
    const id = ADMIN_VOCAB_MATCH[2];
    if (req.method === "GET" && !id) {
      const list = adminVocabStore();
      res.end(JSON.stringify(list));
      return;
    }
    if (req.method === "GET" && id) {
      const found = adminVocabStore().find((t) => t.id === id);
      res.end(JSON.stringify(found ?? {}));
      return;
    }
    if (req.method === "POST" && !id) {
      const body = await readJson(req);
      const created = {
        id: (globalThis.crypto?.randomUUID?.() ?? `mock-${Date.now()}-${Math.floor(Math.random() * 1e6)}`),
        slug: body.slug || "new-topic",
        title: body.title || "Yangi mavzu",
        titleUz: body.titleUz || "New topic",
        category: body.category || "General",
        grammarFocusCode: body.grammarFocusCode || "",
        sequence: Number(body.sequence ?? 0),
        level: body.level || "A1",
        status: "Draft",
        wordCount: 0,
        createdAt: new Date().toISOString(),
      };
      ADMIN_VOCAB.push(created);
      res.statusCode = 201;
      res.end(JSON.stringify(created));
      return;
    }
    if (req.method === "PUT" && id) {
      const body = await readJson(req);
      const idx = ADMIN_VOCAB.findIndex((t) => t.id === id);
      if (idx >= 0) {
        ADMIN_VOCAB[idx] = {
          ...ADMIN_VOCAB[idx],
          title: body.title ?? ADMIN_VOCAB[idx].title,
          titleUz: body.titleUz ?? ADMIN_VOCAB[idx].titleUz,
          category: body.category ?? ADMIN_VOCAB[idx].category,
          grammarFocusCode: body.grammarFocusCode ?? ADMIN_VOCAB[idx].grammarFocusCode,
          sequence: body.sequence != null ? Number(body.sequence) : ADMIN_VOCAB[idx].sequence,
          level: body.level ?? ADMIN_VOCAB[idx].level,
        };
        res.end(JSON.stringify(ADMIN_VOCAB[idx]));
        return;
      }
    }
    if (req.method === "DELETE" && id) {
      const idx = ADMIN_VOCAB.findIndex((t) => t.id === id);
      if (idx >= 0) ADMIN_VOCAB.splice(idx, 1);
      res.statusCode = 204;
      res.end();
      return;
    }
  }

  res.statusCode = 404;
  res.end(JSON.stringify({
    code: "mock_endpoint_not_implemented",
    method: req.method,
    path: url,
  }));
});

// Seed catalog for the admin vocabulary mock. Mirrors AdminVocabularyTopicDto
// (id/slug/title/titleUz/category/grammarFocusCode/sequence/level/status/wordCount/createdAt).
const ADMIN_VOCAB = [
  { id: MOCK_ADMIN_VOCAB_ID, slug: "greetings-introductions", title: "Greetings & Introductions", titleUz: "Salomlashuv va tanishuv", category: "Daily_Life", grammarFocusCode: "be-verb-present", sequence: 1, level: "A1", status: "Published", wordCount: 2, passage: "Hello, my name is Ali.", words: [{ word: "hello", translation: "salom", exampleSentence: "Hello, my name is Ali.", partOfSpeech: "Noun", lexicalCategory: "Noun", register: null, usageNote: null, imageUrl: null, imageSource: null, imageAttribution: null }], createdAt: "2026-06-01T10:00:00Z" },
  { id: "a2222222-2222-2222-2222-222222222222", slug: "family-members", title: "Family Members", titleUz: "Oila a'zolari", category: "People", grammarFocusCode: "possessive-adjectives", sequence: 2, level: "A1", status: "Published", wordCount: 31, createdAt: "2026-06-02T10:00:00Z" },
  { id: "a3333333-3333-3333-3333-333333333333", slug: "colors-numbers", title: "Colors & Numbers", titleUz: "Ranglar va sonlar", category: "Basics", grammarFocusCode: "plural-nouns", sequence: 3, level: "A1", status: "Ready", wordCount: 28, createdAt: "2026-06-03T10:00:00Z" },
  { id: "a4444444-4444-4444-4444-444444444444", slug: "food-drink", title: "Food & Drink", titleUz: "Ovqatlanish", category: "Daily_Life", grammarFocusCode: "count-uncount-nouns", sequence: 4, level: "A2", status: "Published", wordCount: 42, createdAt: "2026-06-04T10:00:00Z" },
  { id: "a5555555-5555-5555-5555-555555555555", slug: "travel-directions", title: "Travel & Directions", titleUz: "Sayohat va yo'nalish", category: "Travel", grammarFocusCode: "prepositions-place", sequence: 5, level: "A2", status: "Generating", wordCount: 0, createdAt: "2026-06-05T10:00:00Z" },
  { id: "a6666666-6666-6666-6666-666666666666", slug: "work-business", title: "Work & Business", titleUz: "Ish va biznes", category: "Professional", grammarFocusCode: "present-perfect", sequence: 6, level: "B1", status: "Published", wordCount: 53, createdAt: "2026-06-06T10:00:00Z" },
  { id: "a7777777-7777-7777-7777-777777777777", slug: "health-medicine", title: "Health & Medicine", titleUz: "Sog'liq va tibbiyot", category: "Professional", grammarFocusCode: "modal-verbs", sequence: 7, level: "B2", status: "Ready", wordCount: 47, createdAt: "2026-06-07T10:00:00Z" },
  { id: "a8888888-8888-8888-8888-888888888888", slug: "environment", title: "Environment & Nature", titleUz: "Atrof-muhit va tabiat", category: "Academic", grammarFocusCode: "passive-voice", sequence: 8, level: "C1", status: "Draft", wordCount: 0, createdAt: "2026-06-08T10:00:00Z" },
];
function adminVocabStore() {
  return ADMIN_VOCAB;
}

const PORT = process.env.PORT ? Number(process.env.PORT) : 5055;
server.listen(PORT, () => {
  console.log(`Mock API listening on http://localhost:${PORT}`);
});
