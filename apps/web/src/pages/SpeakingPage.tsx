import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { uz } from "@/content/uz";
import { api, apiErrorDetails, ApiError, rateLimitDetails } from "@/api/client";
import { useAuth } from "@/app/auth";
import { getLearnerId, getStoredLevel } from "@/app/session";
import { CefrLevel, ProductEventType, PronunciationBand } from "@/api/types";
import type {
  IdeaCardDto,
  PronunciationResultDto,
  RoleplayEvaluationResult,
  SpeakingRejectionCode,
  SpeakingTranscriptConfirmationOutcome,
  SpeechWordTimingDto,
  TopicCompletionDto,
  TopicSpeakingProgressDto,
  VisemeFrameDto,
} from "@/api/types";
import { motion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";
import { Button } from "@/components/ui/Button";
import { Spinner } from "@/components/ui/Spinner";
import { SpeakingResultShell } from "@/components/speaking/SpeakingResultShell";
import { DuoButton } from "@/components/game";
import { AppButton, DesignModal } from "@/components/design";
import { LessonFlowAction } from "@/components/LessonFlowAction";
import { LessonProgress, type LessonStep } from "@/components/lesson/LessonProgress";
import { lessonBackTarget, lessonOriginFrom, lessonState, type LessonNavigationState } from "@/lib/lessonNavigation";
import { useAsync } from "@/lib/useAsync";
import { cn } from "@/lib/cn";
import { blobToWav16kMono, bytesToBase64, encodeWav16 } from "@/lib/audio";
import { cardClass, type CardAccent } from "@/lib/cardPalette";
import { publishAssistantContext } from "@/components/assistantContext";
import {
  SpeakingLiveHubClient,
  type SpeakingLiveAudioEvent,
  type SpeakingLivePronunciationEvent,
  type SpeakingLiveTutorEvent,
  type SpeakingQuotaDto,
  type SpeakingLiveTranscriptEvent,
} from "@/api/speakingLiveHub";
import { LiveVoiceRecorder } from "@/lib/liveVoiceRecorder";
import {
  createAudioRecorder,
  isMicrophonePermissionDenied,
  requestSpeakingMicrophone,
  stopMediaStream,
} from "@/lib/microphoneCapture";
import { SpeakingIndicator } from "@/components/speaking/SpeakingIndicator";
import { useEnergy } from "@/components/game/EnergyProvider";
import "./SpeakingPage.css";
import "./SpeakingLiveTemplate.css";
import "@/components/catalog/CatalogTheme.css";

/**
 * The learner's chosen conversation subject. A vocabulary-linked topic carries the id of
 * the studied topic (the server resolves it to the topic title + words it teaches); a curated
 * free-talk topic carries its snake_case `topicCode` (sent to the tutor as the conversation
 * topic); a fully free conversation carries neither. `label` is only for display/resume.
 */
interface TopicSelection {
  label: string;
  vocabularyTopicId?: string;
  topicCode?: string;
}

/**
 * Navigation state that auto-starts a conversation on arrival at /speaking:
 * - `vocabularyTopicId` - from the vocabulary topic page's "speak about this" CTA (study a
 *   topic, then practice speaking it).
 * - `topicCode` - from the "Erkin suhbat mavzulari" page: a curated free-talk topic's
 *   snake_case code, sent to the tutor so the AI anchors the dialog on exactly that topic.
 * - `roleplayScenario` - from the "Rolli suhbat" page (/speaking/role-talk): a scenario code
 *   that auto-starts a scored roleplay against the matching AI persona instead of an open chat.
 * `topicTitle` is only the display/resume label for a conversation launch.
 */
interface SpeakingLaunchState extends LessonNavigationState {
  vocabularyTopicId?: string;
  topicCode?: string;
  topicTitle?: string;
  roleplayScenario?: string;
}

interface ChatTurn {
  id: string;
  role: "tutor" | "learner";
  text: string;
  audioBase64?: string;
  pronunciation?: PronunciationResultDto;
  feedbackUz?: string | null;
  focusWord?: string | null;
  translation?: string | null;
  translationStatus?: "idle" | "loading" | "ready";
  activeWordOffset?: number | null;
}

function chatTurn(role: ChatTurn["role"], text: string): ChatTurn {
  return { id: crypto.randomUUID(), role, text, translationStatus: "idle" };
}

const TRANSLATION_RETRY_DELAYS_MS = [500, 1_000, 2_000, 4_000, 8_000];

function waitForTranslationRetry(delayMs: number, signal: AbortSignal) {
  return new Promise<void>((resolve, reject) => {
    if (signal.aborted) {
      reject(new DOMException("Translation cancelled", "AbortError"));
      return;
    }
    const timeout = window.setTimeout(resolve, delayMs);
    signal.addEventListener("abort", () => {
      window.clearTimeout(timeout);
      reject(new DOMException("Translation cancelled", "AbortError"));
    }, { once: true });
  });
}

function rejectionMessage(code: SpeakingRejectionCode | null | undefined, fallback?: string | null) {
  if (code === "invalid_audio") return uz.speaking.invalidAudio;
  if (code === "low_confidence") return uz.speaking.lowConfidence;
  if (code === "service_failure") return uz.speaking.speechServiceError;
  return fallback ?? uz.speaking.notRecognized;
}

const QUICK_IDEA_CARDS: IdeaCardDto[] = [
  { prompt: "What do you like most about this topic?", starter: "What I like most is", emoji: "⭐" },
  { prompt: "When or where does it usually happen?", starter: "It usually happens", emoji: "📍" },
  { prompt: "Can you give one simple example?", starter: "For example,", emoji: "✨" },
  { prompt: "Why is it interesting or important to you?", starter: "It is important because", emoji: "💭" },
];

// Persisted so opening a word's pronunciation detail (which unmounts this page) and
// coming back resumes the same conversation in the same topic - instead of dropping
// the learner back at the topic picker. Survives only within the browser session.
const SESSION_KEY = "englishai.speaking.session";

interface PersistedConversation {
  sessionId: string;
  selection: TopicSelection;
  turns: ChatTurn[];
  // Roleplay sittings persist their mode + chosen scenario code so a round-trip to the
  // pronunciation detail screen resumes the same scene rather than dropping back to the picker.
  mode?: SpeakingMode;
  scenario?: string;
}

function loadConversation(): PersistedConversation | null {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as PersistedConversation;
    if (!parsed.sessionId || !Array.isArray(parsed.turns)) return null;
    // A snapshot from before scenarios became code-based stored a numeric enum - drop it so a
    // resume can't send a number where the API now expects a scenario code.
    if (parsed.scenario !== undefined && typeof parsed.scenario !== "string")
      return null;
    return parsed;
  } catch {
    return null;
  }
}

function saveConversation(data: PersistedConversation) {
  try {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(data));
  } catch {
    /* storage unavailable (private mode/quota) - resume is best-effort */
  }
}

function clearConversation() {
  try {
    sessionStorage.removeItem(SESSION_KEY);
  } catch {
    /* ignore */
  }
}

type Phase =
  | "topic"
  | "preparation"
  | "conversation"
  | "summary"
  | "limit"
  | "timeUp"
  | "error"
  | "scoring"
  | "scorecard";
type Status = "connecting" | "ready" | "thinking" | "recording";
type StartErrorKind = "network" | "unavailable" | "rate-limited";
const LIVE_SPEAKING_BUILD_ENABLED = import.meta.env.VITE_SPEAKING_LIVE_ENABLED !== "false";

/** Speaking has two flavours: an open/topic conversation, and a roleplay against a fixed persona
 *  (scored at the end). Both share the same live-chat machinery below; only entry and end differ. */
type SpeakingMode = "conversation" | "roleplay";

const SPEAKING_LIVE_STEPS: readonly LessonStep[] = [
  { id: "preparation", label: "Tayyorgarlik" },
  { id: "conversation", label: "Suhbat" },
  { id: "result", label: "Natija" },
];

function playBase64Audio(base64: string): HTMLAudioElement | null {
  if (!base64) return null;
  try {
    const audio = new Audio(`data:audio/wav;base64,${base64}`);
    void audio.play().catch(() => undefined);
    return audio;
  } catch {
    return null;
  }
}

/** Roughly how long a viseme track keeps the parrot "speaking" before it relaxes. */
function trackDurationMs(frames: VisemeFrameDto[]): number {
  return frames.length ? frames[frames.length - 1].offsetMs + 400 : 0;
}

/**
 * Screen 05 - AI Speaking: selection → live tutor chat → results. Two flavours share this page:
 * an open/topic **conversation** (pronunciation summary at the end) and a **roleplay** against a
 * fixed persona (a 0-100 score card at the end). The page always opens on the conversation picker;
 * a roleplay is entered from the dedicated scenario catalog (/speaking/role-talk), which returns
 * here with a launch state that auto-starts the scored roleplay.
 */
export function SpeakingPage() {
  const navigate = useNavigate();
  const { energy, openEnergyModal } = useEnergy();
  const location = useLocation();
  const origin = lessonOriginFrom(location);
  const topicBackTarget = lessonBackTarget("speaking", origin);
  const {
    topicId: routeTopicId,
    topicCode: routeTopicCode,
    scenarioCode: routeScenarioCode,
  } = useParams();
  const launch = location.state as SpeakingLaunchState | null;
  const isFreeRoute = location.pathname === "/app/speaking/free";
  const hasExplicitLaunch = Boolean(
    isFreeRoute || routeTopicId || routeTopicCode || routeScenarioCode
  );

  const {
    data: routeFreeTalkTopics,
    loading: routeFreeTalkLoading,
    error: routeFreeTalkError,
    reload: reloadRouteFreeTalk,
  } = useAsync(
    () => api.speaking.freeTalkTopics(undefined, true),
    [routeTopicCode],
    Boolean(routeTopicCode),
  );
  const routeFreeTalkTopic = routeTopicCode
    ? routeFreeTalkTopics?.find((topic) => topic.code === routeTopicCode)
    : undefined;
  const {
    data: routeRoleplayScenarios,
    loading: routeRoleplayLoading,
    error: routeRoleplayError,
    reload: reloadRouteRoleplay,
  } = useAsync(
    () => api.speaking.roleplayScenarios(undefined, true),
    [routeScenarioCode],
    Boolean(routeScenarioCode),
  );
  const routeRoleplayScenario = routeScenarioCode
    ? routeRoleplayScenarios?.find((scenario) => scenario.code === routeScenarioCode)
    : undefined;
  // Resume an in-progress conversation when returning from a sub-screen (e.g. the
  // pronunciation detail page), so the learner stays in their chosen topic. Any explicit topic
  // launch overrides a resume - the learner asked for a new conversation. Read once.
  const [resumed] = useState<PersistedConversation | null>(() => {
    if (launch?.vocabularyTopicId || launch?.topicCode || launch?.roleplayScenario) return null;
    const saved = loadConversation();
    if (!hasExplicitLaunch) return saved;
    if (routeTopicId)
      return saved?.selection.vocabularyTopicId === routeTopicId ? saved : null;
    if (routeTopicCode)
      return saved?.selection.topicCode === routeTopicCode ? saved : null;
    if (routeScenarioCode)
      return saved?.mode === "roleplay" && saved.scenario === routeScenarioCode ? saved : null;
    return saved && !saved.selection.vocabularyTopicId && !saved.selection.topicCode && saved.mode !== "roleplay"
      ? saved
      : null;
  });

  const sessionIdRef = useRef<string | null>(resumed?.sessionId ?? null);
  const scenarioRef = useRef<string | null>(resumed?.scenario ?? null);
  const lastSelectionRef = useRef<TopicSelection>(
    resumed?.selection ?? { label: uz.speaking.freeConversation }
  );
  // Ensures a topic launch only auto-starts once (not on every render/state change).
  const launchedRef = useRef(Boolean(resumed));
  const recorderRef = useRef<MediaRecorder | null>(null);
  const liveHubRef = useRef<SpeakingLiveHubClient | null>(null);
  const liveRecorderRef = useRef<LiveVoiceRecorder | null>(null);
  const [liveEnabled, setLiveEnabled] = useState(false);
  const [liveConnecting, setLiveConnecting] = useState(false);
  const [liveAvailable, setLiveAvailable] = useState(false);
  const chunksRef = useRef<Blob[]>([]);
  const scrollRef = useRef<HTMLDivElement>(null);
  const shouldFollowTranscriptRef = useRef(true);
  const speakTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const tutorAudioRef = useRef<HTMLAudioElement | null>(null);
  const tutorUtteranceRef = useRef<SpeechSynthesisUtterance | null>(null);
  const tutorWordTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const tutorSpeakStartTimerRef = useRef<number | null>(null);
  const speakingTurnIdRef = useRef<string | null>(null);
  const mountedRef = useRef(true);

  // A topic launch jumps straight into the conversation (the auto-start effect fills it),
  // so we skip the picker and avoid a flash of it before the effect runs.
  const [phase, setPhase] = useState<Phase>(
    routeTopicId && !resumed
      ? "preparation"
      : resumed || hasExplicitLaunch || launch?.vocabularyTopicId || launch?.topicCode || launch?.roleplayScenario
      ? "conversation"
      : "topic"
  );
  const isLiveSessionPhase =
    phase !== "scorecard" && phase !== "summary" && phase !== "topic" && phase !== "preparation";

  useEffect(() => {
    if (!hasExplicitLaunch || !isLiveSessionPhase) return;
    const pageRoots = [document.documentElement, document.body, document.getElementById("root")].filter(
      (element): element is HTMLElement => Boolean(element),
    );
    pageRoots.forEach((element) => element.classList.add("speaking-session-page-active"));
    return () => pageRoots.forEach((element) => element.classList.remove("speaking-session-page-active"));
  }, [hasExplicitLaunch, isLiveSessionPhase]);
  // Which flavour of speaking is active. A resumed sitting keeps its stored mode; a fresh visit
  // opens as a conversation, and beginRoleplay flips it when a roleplay is launched from the catalog.
  const [mode, setMode] = useState<SpeakingMode>(
    resumed?.mode ?? "conversation"
  );
  // The end-of-scene roleplay score (null until scored, or when scoring failed → shown as an error).
  const [evaluation, setEvaluation] = useState<RoleplayEvaluationResult | null>(
    null
  );
  const [turns, setTurns] = useState<ChatTurn[]>(resumed?.turns ?? []);
  const translationControllersRef = useRef(new Map<string, AbortController>());

  async function prefetchTranslation(turn: ChatTurn) {
    const id = turn.id;
    if (!id || !turn.text.trim()) return;
    translationControllersRef.current.get(id)?.abort();
    const controller = new AbortController();
    translationControllersRef.current.set(id, controller);
    setTurns((previous) => previous.map((item) => item.id === id && item.translationStatus !== "ready"
      ? { ...item, translationStatus: "loading" }
      : item));
    let attempt = 0;
    while (!controller.signal.aborted) {
      try {
        const result = await api.translate(turn.text, (getStoredLevel() as CefrLevel) || CefrLevel.A2, {
          speaker: turn.role,
          topic: lastSelectionRef.current.label,
          previousTurns: turns.slice(-4).map((item) => `${item.role}: ${item.text}`),
        });
        if (result.translation?.trim()) {
          setTurns((previous) => previous.map((item) => item.id === id
            ? { ...item, translation: result.translation?.trim(), translationStatus: "ready" }
            : item));
          break;
        }
      } catch (error) {
        if (controller.signal.aborted || (error instanceof DOMException && error.name === "AbortError")) break;
      }

      const delay = TRANSLATION_RETRY_DELAYS_MS[Math.min(attempt, TRANSLATION_RETRY_DELAYS_MS.length - 1)];
      attempt += 1;
      try {
        await waitForTranslationRetry(delay, controller.signal);
      } catch {
        break;
      }
    }
    if (translationControllersRef.current.get(id) === controller)
      translationControllersRef.current.delete(id);
  }

  useEffect(() => () => {
    translationControllersRef.current.forEach((controller) => controller.abort());
    translationControllersRef.current.clear();
  }, []);

  function appendTurn(role: ChatTurn["role"], text: string, extra?: Partial<ChatTurn>) {
    const turn = { ...chatTurn(role, text), ...extra };
    setTurns((previous) => [...previous, turn]);
    return turn;
  }

  function attachPronunciation(turnId: string, event: {
    pronunciation: PronunciationResultDto;
    feedbackUz: string | null;
    focusWord: string | null;
  }) {
    setTurns((previous) => previous.map((turn) => turn.id === turnId
      ? {
          ...turn,
          pronunciation: event.pronunciation,
          feedbackUz: event.feedbackUz,
          focusWord: event.focusWord,
        }
      : turn));
  }

  function setTutorActiveWord(turnId: string | null, textOffset: number | null) {
    speakingTurnIdRef.current = turnId;
    setTurns((previous) => previous.map((turn) => turn.role === "tutor"
      ? { ...turn, activeWordOffset: turn.id === turnId ? textOffset : null }
      : turn));
  }

  useEffect(() => publishAssistantContext({
    area: "speaking",
    resourceId: lastSelectionRef.current.vocabularyTopicId ?? lastSelectionRef.current.topicCode ?? lastSelectionRef.current.label,
    title: lastSelectionRef.current.label,
    context: [
      `Mode: ${mode}`,
      `Phase: ${phase}`,
      scenarioRef.current ? `Roleplay scenario: ${scenarioRef.current}` : "",
      `Conversation topic: ${lastSelectionRef.current.label}`,
      `Latest tutor prompt: ${turns.filter((turn) => turn.role === "tutor").at(-1)?.text ?? ""}`,
    ].filter(Boolean).join("\n"),
    focusText: turns.slice(-4).map((turn) => `${turn.role}: ${turn.text}`).join("\n"),
    route: window.location.pathname,
    stage: phase,
  }), [mode, phase, turns]);
  const [status, setStatus] = useState<Status>("ready");
  const [startErrorKind, setStartErrorKind] = useState<StartErrorKind>("unavailable");
  const [startRetryAfterSeconds, setStartRetryAfterSeconds] = useState(0);
  const [micPending, setMicPending] = useState(false);
  const micPendingRef = useRef(false);
  const microphoneRequestRef = useRef(0);
  const [micError, setMicError] = useState(false);
  // The learner tried to say/spell their name but STT couldn't capture it (the server flags this):
  // surface a small input so they can type it instead. Also openable manually at any time to set or
  // correct the name.
  const [namePromptOpen, setNamePromptOpen] = useState(false);
  // Transient hint shown when a clip couldn't be recognized (silence/too quiet) so
  // the learner knows to retry - this is not an error, just a missed turn.
  const [notRecognized, setNotRecognized] = useState<string | null>(null);
  const [utteranceError, setUtteranceError] = useState(false);
  const [pendingAudio, setPendingAudio] = useState<string | null>(null);
  const pendingLiveAudioRef = useRef<string | null>(null);
  const pendingLearnerTurnIdRef = useRef<string | null>(null);
  const pendingPronunciationRef = useRef<{
    turnId: string;
    pronunciation: PronunciationResultDto;
    feedbackUz: string | null;
    focusWord: string | null;
  } | null>(null);
  const pendingTutorTextRef = useRef<string | null>(null);
  const pendingTutorTurnIdRef = useRef<string | null>(null);
  const [isTutorSpeaking, setIsTutorSpeaking] = useState(false);
  const statusRef = useRef<Status>("ready");
  const tutorSpeakingRef = useRef(false);

  function showMicrophoneIssue(error?: unknown) {
    // A denied permission still shows its own guidance. Any other microphone failure (device busy,
    // NotReadableError, no device, …) resets silently so the learner can simply press the mic again —
    // no "Mikrofon ishga tushmadi" card, per product request.
    setMicError(isMicrophonePermissionDenied(error));
  }
  // Progress toward learning the linked vocabulary topic by speaking about it (the 5-minute
  // rule). Null for a free conversation. `justLearned` shows a one-off celebration banner.
  const [, setTopicProgress] =
    useState<TopicSpeakingProgressDto | null>(null);
  const [justLearned, setJustLearned] = useState(false);
  // The topic's six-module mastery checklist (K.5), so once Speaking is done the learner sees the
  // full skill overview and which skills are still left. Null for a free conversation.
  const [completion, setCompletion] = useState<TopicCompletionDto | null>(null);
  // How much of today's speaking budget is left. Speech-to-text is billed by the second, so this is
  // the one allowance measured in minutes; showing it lets the learner pace a sitting instead of
  // discovering the limit only when a turn is refused.
  const [speakingQuota, setSpeakingQuota] = useState<SpeakingQuotaDto | null>(null);
  const [quotaExhausted, setQuotaExhausted] = useState(false);

  // "I don't know what to say" helper (the blank-page cure): concrete talking-point cards fetched
  // only when the learner explicitly opens the idea sheet, then cached until a new sitting or an
  // explicit refresh.
  const [ideaCards, setIdeaCards] = useState<IdeaCardDto[] | null>(null);
  const [ideaState, setIdeaState] = useState<"idle" | "loading" | "error">(
    "idle"
  );
  const [ideaSheetOpen, setIdeaSheetOpen] = useState(false);
  // Fetches idea cards for the current session. Cached after the first load; `force` refetches for
  // the "other ideas" button.
  async function loadIdeaCards(force = false) {
    if (!sessionIdRef.current) return;
    if (!force && ideaState === "loading") return;
    setIdeaState("loading");
    try {
      const res = await api.speaking.ideaCards(sessionIdRef.current);
      setIdeaCards(res.cards);
      setIdeaState("idle");
    } catch {
      setIdeaState("error");
    }
  }

  // Drops any idea cards and closes the helper UI when a new sitting starts.
  function resetIdeas() {
    setIdeaCards(null);
    setIdeaState("idle");
    setIdeaSheetOpen(false);
  }

  useEffect(() => {
    const transcript = scrollRef.current;
    if (!transcript) return;

    // Only auto-pin to the newest message when the learner is already reading the bottom of the
    // conversation. `status` flips several times per exchange (recording -> thinking -> ready), so
    // pinning unconditionally would yank the view back to the bottom every time the learner scrolled
    // up to reread an earlier turn, making the transcript feel like it "won't scroll". The onScroll
    // handler clears this flag the moment they scroll away, and re-arms it when they return to the
    // bottom.
    if (!shouldFollowTranscriptRef.current) return;

    // A pronunciation result can increase the learner bubble before the tutor bubble is appended.
    // Smooth scrolling races those consecutive layouts and can leave the new AI text below the
    // viewport, so pin the scroll position after React has committed both the current and next
    // layout frame.
    let secondFrame = 0;
    const firstFrame = window.requestAnimationFrame(() => {
      transcript.scrollTop = transcript.scrollHeight;
      secondFrame = window.requestAnimationFrame(() => {
        transcript.scrollTop = transcript.scrollHeight;
      });
    });
    return () => {
      window.cancelAnimationFrame(firstFrame);
      window.cancelAnimationFrame(secondFrame);
    };
    // Depend on the message count, not the whole `turns` array: the tutor karaoke rewrites the
    // active turn (activeWordOffset) every ~40ms while speaking, and pinning on each of those would
    // re-yank the scroll to the bottom faster than the learner can drag up to reread — the "won't
    // scroll / vibrates" symptom. Only a genuinely new message (or a status change) should follow.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [turns.length, status]);

  useEffect(() => {
    statusRef.current = status;
  }, [status]);

  useEffect(() => {
    tutorSpeakingRef.current = isTutorSpeaking;
  }, [isTutorSpeaking]);

  useEffect(() => {
    if (!LIVE_SPEAKING_BUILD_ENABLED) return;
    let active = true;
    void api.speaking.liveCapabilities()
      .then((capabilities) => {
        if (active) {
          setLiveAvailable(capabilities.enabled);
        }
      })
      .catch(() => {
        if (active) {
          setLiveAvailable(false);
        }
      });
    return () => {
      active = false;
    };
  }, []);

  // Browser media resources are owned for the component lifetime; refs keep this cleanup current.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(
    () => {
      // React StrictMode deliberately runs setup → cleanup → setup in development. Restore the
      // guard on every setup so a slow conversation-start response is not discarded after the
      // simulated cleanup, leaving the learner on the connecting screen forever.
      mountedRef.current = true;
      return () => {
        mountedRef.current = false;
        microphoneRequestRef.current += 1;
        if (speakTimerRef.current) clearTimeout(speakTimerRef.current);
        stopTutorPlayback(false);
        // If the learner navigates away mid-recording, nothing else ever calls
        // recorder.stop() - the mic stays hot and recording in the background for the rest
        // of the session (and, if it later fires, `onstop` would try to transcribe and
        // setState after unmount). Detach the handlers first, then stop the recorder and
        // its mic tracks directly.
        const recorder = recorderRef.current;
        if (recorder && recorder.state !== "inactive") {
          recorder.ondataavailable = null;
          recorder.onstop = null;
          try {
            recorder.stop();
          } catch {
            /* already stopping/stopped */
          }
          recorder.stream?.getTracks().forEach((t) => t.stop());
        }
        void liveRecorderRef.current?.stop();
        liveRecorderRef.current = null;
        void liveHubRef.current?.stop();
        liveHubRef.current = null;
      };
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );


  // Keep the resumable snapshot in sync. While chatting we persist the live turns so a
  // round-trip to a sub-screen restores them; once the session ends or resets we drop it.
  useEffect(() => {
    if (phase === "conversation" && sessionIdRef.current) {
      saveConversation({
        sessionId: sessionIdRef.current,
        selection: lastSelectionRef.current,
        turns: turns.map((turn) => {
          const persistedTurn = { ...turn };
          delete persistedTurn.audioBase64;
          return persistedTurn;
        }),
        mode,
        scenario: scenarioRef.current ?? undefined,
      });
    } else if (
      phase === "topic" ||
      phase === "summary" ||
      phase === "scorecard"
    ) {
      clearConversation();
    }
  }, [phase, turns, mode]);

  useEffect(() => {
    if (launchedRef.current) return;
    if (routeTopicId) {
      launchedRef.current = true;
      return;
    }
    if (routeTopicCode && routeFreeTalkTopic) {
      launchedRef.current = true;
      void beginConversation({
        label: uz.speaking.freeTalkTopics[routeFreeTalkTopic.code] ?? routeFreeTalkTopic.englishTitle,
        topicCode: routeFreeTalkTopic.code,
      });
      return;
    }
    if (routeScenarioCode && routeRoleplayScenario) {
      launchedRef.current = true;
      void beginRoleplay(routeRoleplayScenario.code);
      return;
    }
    if (isFreeRoute) {
      launchedRef.current = true;
      void beginConversation({ label: uz.speaking.freeConversation });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [routeTopicId, routeFreeTalkTopic, routeRoleplayScenario, isFreeRoute, launch?.topicTitle]);

  useEffect(() => {
    if (hasExplicitLaunch || launchedRef.current) return;
    if (launch?.roleplayScenario) {
      navigate(`/app/speaking/role-talk/${encodeURIComponent(launch.roleplayScenario)}`, { replace: true });
    } else if (launch?.topicCode) {
      navigate(`/app/speaking/free-talk/${encodeURIComponent(launch.topicCode)}`, { replace: true });
    } else if (launch?.vocabularyTopicId) {
      navigate(`/app/speaking/topic/${encodeURIComponent(launch.vocabularyTopicId)}`, { replace: true });
    }
  }, [hasExplicitLaunch, launch, navigate]);

  useEffect(() => {
    if (location.pathname !== "/app/speaking" || phase !== "conversation") return;
    const selection = lastSelectionRef.current;
    if (selection.vocabularyTopicId) {
      navigate(`/app/speaking/topic/${encodeURIComponent(selection.vocabularyTopicId)}`, {
        replace: true,
        state: lessonState(origin, { topicTitle: selection.label }),
      });
      return;
    }
    if (selection.topicCode) {
      navigate(`/app/speaking/free-talk/${encodeURIComponent(selection.topicCode)}`, {
        replace: true,
      });
    }
  }, [location.pathname, navigate, origin, phase]);

  function trackPronunciationFeedbackViewed(source?: string) {
    void api.analytics
      .track(
        getLearnerId(),
        ProductEventType.PronunciationFeedbackViewed,
        source
      )
      .catch(() => undefined);
  }

  function playTutor(
    turnId: string,
    text: string,
    audioBase64: string,
    visemes: VisemeFrameDto[],
    isNaturalVoice: boolean,
    wordTimings: SpeechWordTimingDto[] = [],
  ) {
    if (!mountedRef.current) return;
    setTurns((previous) => previous.map((turn) => turn.id === turnId
      ? { ...turn, audioBase64 }
      : turn));
    stopTutorPlayback(false);
    setIsTutorSpeaking(true);
    tutorSpeakingRef.current = true;
    if (speakTimerRef.current) clearTimeout(speakTimerRef.current);

    const finish = () => {
      if (speakTimerRef.current) clearTimeout(speakTimerRef.current);
      speakTimerRef.current = null;
      setIsTutorSpeaking(false);
      tutorSpeakingRef.current = false;
      setTutorActiveWord(null, null);
    };
    const estimatedDuration = Math.max(
      1200,
      trackDurationMs(visemes),
      text.trim().split(/\s+/).length * 430
    );
    speakTimerRef.current = setTimeout(finish, estimatedDuration + 2500);

    if (isNaturalVoice) {
      const audio = playBase64Audio(audioBase64);
      if (audio) {
        tutorAudioRef.current = audio;
        const orderedTimings = [...wordTimings].sort((left, right) => left.audioOffsetMs - right.audioOffsetMs);
        const syncWord = () => {
          if (tutorAudioRef.current !== audio) return;
          const currentMs = audio.currentTime * 1000;
          let current: SpeechWordTimingDto | undefined;
          for (const timing of orderedTimings) {
            if (currentMs < timing.audioOffsetMs) break;
            current = timing;
          }
          setTutorActiveWord(turnId, current?.textOffset ?? null);
          tutorWordTimerRef.current = setTimeout(syncWord, 40);
        };
        setTutorActiveWord(turnId, orderedTimings[0]?.textOffset ?? null);
        syncWord();
        audio.onended = finish;
        audio.onerror = finish;
        return;
      }
    }

    const synth = window.speechSynthesis;
    if (!synth || !text.trim()) {
      finish();
      return;
    }
    synth.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.lang = "en-US";
    utterance.rate = 0.9;
    tutorUtteranceRef.current = utterance;
    utterance.onend = finish;
    utterance.onerror = finish;
    utterance.onboundary = (event) => {
      if (event.name === "word") setTutorActiveWord(turnId, event.charIndex);
    };
    const voice = synth
      .getVoices()
      .find((candidate) => candidate.lang.toLowerCase().startsWith("en"));
    if (voice) utterance.voice = voice;
    tutorSpeakStartTimerRef.current = window.setTimeout(() => {
      tutorSpeakStartTimerRef.current = null;
      if (!mountedRef.current || tutorUtteranceRef.current !== utterance) return;
      synth.speak(utterance);
      synth.resume();
    }, 0);
  }

  function stopTutorPlayback(updateState = true) {
    if (speakTimerRef.current) {
      clearTimeout(speakTimerRef.current);
      speakTimerRef.current = null;
    }
    if (tutorWordTimerRef.current) {
      clearTimeout(tutorWordTimerRef.current);
      tutorWordTimerRef.current = null;
    }
    if (tutorSpeakStartTimerRef.current) {
      clearTimeout(tutorSpeakStartTimerRef.current);
      tutorSpeakStartTimerRef.current = null;
    }
    const audio = tutorAudioRef.current;
    tutorAudioRef.current = null;
    if (audio) {
      audio.onended = null;
      audio.onerror = null;
      audio.pause();
      audio.currentTime = 0;
    }
    if (tutorUtteranceRef.current) {
      tutorUtteranceRef.current.onend = null;
      tutorUtteranceRef.current.onerror = null;
      tutorUtteranceRef.current.onboundary = null;
      tutorUtteranceRef.current = null;
    }
    window.speechSynthesis?.cancel();
    tutorSpeakingRef.current = false;
    setTutorActiveWord(null, null);
    if (updateState) {
      setIsTutorSpeaking(false);
    }
  }

  async function beginConversation(selection: TopicSelection) {
    if (statusRef.current === "connecting") return;
    if (energy?.current === 0) {
      openEnergyModal({
        action: "speaking",
        resume: () => beginConversation(selection),
      });
      return;
    }
    const level = (getStoredLevel() as CefrLevel) || CefrLevel.A2;
    lastSelectionRef.current = selection;
    setPhase("conversation");
    setStatus("connecting");
    statusRef.current = "connecting";
    setTurns([]);
    resetIdeas();
    try {
      // A vocabulary-linked topic is sent as an id (server resolves title + words); a curated
      // free-talk topic is sent as its code; a fully free conversation sends neither. We never
      // send free-text topic labels.
      const res = await api.speaking.start(
        getLearnerId(),
        level,
        selection.topicCode,
        selection.vocabularyTopicId
      );
      if (!mountedRef.current) return;
      sessionIdRef.current = res.sessionId;
      setTopicProgress(res.topicProgress);
      setJustLearned(false);
      const greeting = res.tutorText || uz.speaking.greeting;
      const tutorTurn = appendTurn("tutor", greeting);
      setStatus("ready");
      statusRef.current = "ready";
      playTutor(tutorTurn.id, greeting, res.tutorAudioBase64, res.visemes, res.isNaturalVoice, res.wordTimings ?? []);
    } catch (err) {
      // 402 = bepul tarif kunlik Speaking limiti tugadi (PROJECT-SPEC H.1) - Premium upsell.
      // Boshqa xato (tarmoq/server) - qayta urinish. Soxta salom ko'rsatib o'lik sahifa
      // qoldirmaymiz: sessionId bo'lmasa mikrofon baribir ishlamaydi.
      if (apiErrorDetails(err)?.code === "energy_exhausted") {
        setPhase("topic");
      } else if (err instanceof ApiError && err.status === 402) {
        setPhase("limit");
      } else {
        const limited = rateLimitDetails(err);
        setStartErrorKind(
          limited || (err instanceof ApiError && err.status === 429)
            ? "rate-limited"
            : err instanceof ApiError
              ? "unavailable"
              : "network"
        );
        setStartRetryAfterSeconds(limited?.retryAfterSeconds ?? 0);
        setPhase("error");
      }
      setStatus("ready");
      statusRef.current = "ready";
    }
  }

  // Roleplay entry: the tutor greets the learner in character as the chosen persona. Same
  // freemium gate and failure handling as an open conversation; turns then flow through the
  // shared /utterance pipeline (sendUtterance below), and the sitting is scored on finish.
  async function beginRoleplay(scenarioCode: string) {
    if (statusRef.current === "connecting") return;
    if (energy?.current === 0) {
      openEnergyModal({
        action: "speaking",
        resume: () => beginRoleplay(scenarioCode),
      });
      return;
    }
    const level = (getStoredLevel() as CefrLevel) || CefrLevel.A2;
    scenarioRef.current = scenarioCode;
    setMode("roleplay");
    setEvaluation(null);
    setPhase("conversation");
    setStatus("connecting");
    statusRef.current = "connecting";
    setTurns([]);
    setTopicProgress(null);
    setJustLearned(false);
    setCompletion(null);
    resetIdeas();
    try {
      const res = await api.speaking.roleplayStart(
        getLearnerId(),
        level,
        scenarioCode
      );
      if (!mountedRef.current) return;
      sessionIdRef.current = res.sessionId;
      const greeting = res.tutorText || uz.speaking.greeting;
      const tutorTurn = appendTurn("tutor", greeting);
      setStatus("ready");
      statusRef.current = "ready";
      playTutor(tutorTurn.id, greeting, res.tutorAudioBase64, res.visemes, res.isNaturalVoice, res.wordTimings ?? []);
    } catch (err) {
      if (apiErrorDetails(err)?.code === "energy_exhausted") {
        setPhase("topic");
      } else if (err instanceof ApiError && err.status === 402) {
        setPhase("limit");
      } else {
        const limited = rateLimitDetails(err);
        setStartErrorKind(
          limited || (err instanceof ApiError && err.status === 429)
            ? "rate-limited"
            : err instanceof ApiError
              ? "unavailable"
              : "network"
        );
        setStartRetryAfterSeconds(limited?.retryAfterSeconds ?? 0);
        setPhase("error");
      }
      setStatus("ready");
      statusRef.current = "ready";
    }
  }

  // Ends a roleplay and scores it: send the transcript through the evaluator and show the score
  // card. A missing session (never started) just goes straight to the card; a scoring error keeps
  // evaluation null so the card renders its retry state.
  async function finishRoleplay() {
    if (!sessionIdRef.current) {
      setPhase("scorecard");
      return;
    }
    setPhase("scoring");
    try {
      const result = await api.speaking.roleplayEvaluate(sessionIdRef.current);
      setEvaluation(result);
      // Scored successfully - drop the resumable session so it can't be re-scored or resumed.
      clearConversation();
      sessionIdRef.current = null;
      setPhase("scorecard");
    } catch {
      // Keep the session so the score card's retry can re-evaluate the same transcript.
      setEvaluation(null);
      setPhase("scorecard");
    }
  }

  async function sendUtterance(
    audioBase64: string,
    isRetry = false,
    confirmation?: { transcript: string; outcome: SpeakingTranscriptConfirmationOutcome },
  ) {
    if (!sessionIdRef.current) return;
    setStatus("thinking");
    statusRef.current = "thinking";
    setNotRecognized(null);
    setUtteranceError(false);
    setPendingAudio(audioBase64);
    if (!isRetry) {
      pendingLearnerTurnIdRef.current = null;
      pendingPronunciationRef.current = null;
    }
    pendingTutorTextRef.current = null;
    try {
      await api.speaking.utteranceStream(sessionIdRef.current, audioBase64, {
        onRecognized: ({ text }) => {
          if (isRetry && pendingLearnerTurnIdRef.current) return;
          const learnerTurn = appendTurn("learner", text);
          pendingLearnerTurnIdRef.current = learnerTurn.id;
        },
        onTutor: (event) => {
          if (event.sessionLimitReached) {
            clearConversation();
            sessionIdRef.current = null;
            setPhase("timeUp");
            return;
          }
          pendingTutorTextRef.current = event.text;
          const tutorTurn = appendTurn("tutor", event.text);
          pendingTutorTurnIdRef.current = tutorTurn.id;
          if (event.topicProgress) {
            setTopicProgress(event.topicProgress);
            if (event.topicProgress.justLearned) setJustLearned(true);
          }
          if (event.completion) setCompletion(event.completion);
          // The tutor must never pull up the name prompt on its own — the learner opens it only via
          // the "AI sizni qanday chaqirsin" button. So `event.namePrompt` is intentionally ignored.
          setPendingAudio(null);
          setStatus("ready");
          statusRef.current = "ready";
        },
        onPronunciation: (event) => {
          const learnerTurnId = pendingLearnerTurnIdRef.current;
          if (learnerTurnId) {
            pendingPronunciationRef.current = { turnId: learnerTurnId, ...event };
            attachPronunciation(learnerTurnId, event);
            window.setTimeout(() => {
              const pendingPronunciation = pendingPronunciationRef.current;
              if (pendingPronunciation?.turnId === learnerTurnId) {
                attachPronunciation(learnerTurnId, pendingPronunciation);
                pendingPronunciationRef.current = null;
              }
            }, 0);
          }
          pendingLearnerTurnIdRef.current = null;
          trackPronunciationFeedbackViewed(sessionIdRef.current ?? undefined);
        },
        onAudio: (event) => {
          const text = pendingTutorTextRef.current;
          if (text) {
            playTutor(
              pendingTutorTurnIdRef.current ?? crypto.randomUUID(),
              text,
              event.audioBase64,
              event.visemes,
              event.isNaturalVoice,
              event.wordTimings ?? [],
            );
            pendingTutorTextRef.current = null;
            pendingTutorTurnIdRef.current = null;
          }
        },
        onProgress: (event) => {
          if (event.topicProgress) {
            setTopicProgress(event.topicProgress);
            if (event.topicProgress.justLearned) setJustLearned(true);
          }
          if (event.completion) setCompletion(event.completion);
        },
        onUnrecognized: (event) => {
          pendingLearnerTurnIdRef.current = null;
          setNotRecognized(rejectionMessage(event.rejectionCode, event.feedbackUz));
          setPendingAudio(null);
        },
        onTranscriptConfirmationRequired: (event) => {
          const transcript = event.suggestedText.trim();
          if (transcript) {
            void sendUtterance(audioBase64, true, {
              transcript,
              outcome: "candidate_selected",
            });
          } else {
            setNotRecognized(uz.speaking.notRecognized);
            setPendingAudio(null);
          }
        },
        onTutorUnavailable: () => {
          setUtteranceError(true);
        },
      }, confirmation);
    } catch {
      setUtteranceError(true);
    } finally {
      setStatus("ready");
      statusRef.current = "ready";
    }
  }

  function applyLiveTranscript(event: SpeakingLiveTranscriptEvent) {
    appendTurn("learner", event.text, { id: event.turnId });
    const pendingPronunciation = pendingPronunciationRef.current;
    if (pendingPronunciation?.turnId === event.turnId) {
      attachPronunciation(event.turnId, pendingPronunciation);
      pendingPronunciationRef.current = null;
    }
  }

  function applyLiveTutor(event: SpeakingLiveTutorEvent) {
    if (event.sessionLimitReached) {
      clearConversation();
      sessionIdRef.current = null;
      setPhase("timeUp");
      return;
    }
    if (event.quota) setSpeakingQuota(event.quota);
    pendingTutorTextRef.current = event.text;
    const tutorTurn = appendTurn("tutor", event.text);
    pendingTutorTurnIdRef.current = tutorTurn.id;
    if (event.topicProgress) {
      setTopicProgress(event.topicProgress);
      if (event.topicProgress.justLearned) setJustLearned(true);
    }
    if (event.completion) setCompletion(event.completion);
    // Never auto-open the name prompt from a tutor event — only the learner's explicit button does.
    pendingLiveAudioRef.current = null;
    setPendingAudio(null);
    setStatus("ready");
    statusRef.current = "ready";
  }

  function applyLivePronunciation(event: SpeakingLivePronunciationEvent) {
    pendingPronunciationRef.current = event;
    attachPronunciation(event.turnId, event);
    trackPronunciationFeedbackViewed(sessionIdRef.current ?? undefined);
  }

  function applyLiveAudio(event: SpeakingLiveAudioEvent) {
    const text = pendingTutorTextRef.current;
    if (!text) return;
    playTutor(
      pendingTutorTurnIdRef.current ?? crypto.randomUUID(),
      text,
      event.audioBase64,
      event.visemes,
      event.isNaturalVoice,
      event.wordTimings ?? [],
    );
    pendingTutorTextRef.current = null;
    pendingTutorTurnIdRef.current = null;
  }

  async function enableLiveConversation() {
    const sessionId = sessionIdRef.current;
    if (!liveAvailable || !sessionId || liveEnabled || liveConnecting) return;
    setLiveConnecting(true);
    setMicError(false);
    try {
      const hub = new SpeakingLiveHubClient({
        onFinalTranscript: applyLiveTranscript,
        onTutorText: applyLiveTutor,
        onPronunciation: applyLivePronunciation,
        onTutorAudio: applyLiveAudio,
        onUnrecognized: (event) => {
          setNotRecognized(rejectionMessage(event.rejectionCode, event.feedbackUz));
          setStatus("ready");
          statusRef.current = "ready";
        },
        onTranscriptConfirmationRequired: (event) => {
          const transcript = event.suggestedText.trim();
          const audioBase64 = pendingLiveAudioRef.current ?? "";
          if (transcript && audioBase64) {
            liveRecorderRef.current?.pause();
            void hub.submitTurn(audioBase64, {
                transcript,
                outcome: "candidate_selected",
                turnId: event.turnId,
              })
              .then(() => liveRecorderRef.current?.resume())
              .catch(() => setUtteranceError(true));
          } else {
            setNotRecognized(uz.speaking.notRecognized);
          }
        },
        onRecoverableError: (event) => {
          // The daily speaking budget is not a live-mode failure: turning live off would tell the
          // learner the wrong thing and hide the upgrade path. Stop the microphone, show the quota
          // message and open the paywall instead.
          if (event.code === "speaking_minutes_exhausted") {
            void liveRecorderRef.current?.stop();
            setQuotaExhausted(true);
            setStatus("ready");
            statusRef.current = "ready";
            window.dispatchEvent(new CustomEvent("paywall:required"));
            return;
          }
          setLiveAvailable(false);
          setLiveEnabled(false);
          void liveRecorderRef.current?.stop();
          void liveHubRef.current?.stop();
          liveRecorderRef.current = null;
          liveHubRef.current = null;
          setUtteranceError(true);
          setStatus("ready");
          statusRef.current = "ready";
        },
        onClosed: () => {
          setLiveAvailable(false);
          setLiveEnabled(false);
          setStatus("ready");
          statusRef.current = "ready";
        },
      });
      liveHubRef.current = hub;
      await hub.joinSession(sessionId);
      const recorder = new LiveVoiceRecorder({
        onSpeechStarted: () => {
          stopTutorPlayback();
          void hub.interruptTutor();
          setNotRecognized(null);
          setUtteranceError(false);
          window.dispatchEvent(new Event("assistant:close-for-recording"));
          setStatus("recording");
          statusRef.current = "recording";
        },
        onSpeechEnded: async (audio) => {
          setStatus("thinking");
          statusRef.current = "thinking";
          try {
            const wav = encodeWav16(audio);
            const audioBase64 = bytesToBase64(wav);
            pendingLiveAudioRef.current = audioBase64;
            setPendingAudio(audioBase64);
            await hub.submitTurn(audioBase64);
          } catch {
            setNotRecognized(uz.speaking.notRecognized);
            setStatus("ready");
            statusRef.current = "ready";
          }
        },
        onError: (error) => {
          setLiveAvailable(false);
          setLiveEnabled(false);
          showMicrophoneIssue(error);
        },
      });
      liveRecorderRef.current = recorder;
      await recorder.start();
      setLiveEnabled(true);
      setStatus("ready");
      statusRef.current = "ready";
    } catch (error) {
      setLiveEnabled(false);
      setLiveAvailable(false);
      await liveRecorderRef.current?.stop();
      await liveHubRef.current?.stop();
      liveRecorderRef.current = null;
      liveHubRef.current = null;
      setLiveConnecting(false);
      // Live conversation could not start for this session. Without this the mic button would have
      // just disabled live and done nothing else, forcing the learner to tap a SECOND time for the
      // manual push-to-talk fallback to kick in ("why do I have to press twice?"). So start that
      // manual recording immediately, on the same press. A denied permission is the one case we do
      // not retry — it would only prompt again for the same refusal — so we show its guidance.
      if (isMicrophonePermissionDenied(error)) {
        showMicrophoneIssue(error);
      } else {
        void startRecording();
      }
      return;
    } finally {
      setLiveConnecting(false);
    }
  }

  async function disableLiveConversation() {
    await liveRecorderRef.current?.stop();
    await liveHubRef.current?.stop();
    liveRecorderRef.current = null;
    liveHubRef.current = null;
    pendingLiveAudioRef.current = null;
    setLiveEnabled(false);
    setStatus("ready");
    statusRef.current = "ready";
  }

  async function exitSession() {
    microphoneRequestRef.current += 1;
    stopTutorPlayback();

    const recorder = recorderRef.current;
    recorderRef.current = null;
    if (recorder) {
      recorder.ondataavailable = null;
      recorder.onstop = null;
      if (recorder.state !== "inactive") {
        try {
          recorder.stop();
        } catch {
          /* recorder already stopped */
        }
      }
      recorder.stream?.getTracks().forEach((track) => track.stop());
    }

    const liveRecorder = liveRecorderRef.current;
    const liveHub = liveHubRef.current;
    liveRecorderRef.current = null;
    liveHubRef.current = null;
    await Promise.allSettled([
      liveRecorder?.stop() ?? Promise.resolve(),
      liveHub?.stop() ?? Promise.resolve(),
    ]);

    if (mode === "roleplay" && sessionIdRef.current) {
      await finishRoleplay();
      return;
    }

    if (mode === "conversation" && turns.some((turn) => turn.role === "learner")) {
      clearConversation();
      sessionIdRef.current = null;
      setPhase("summary");
      return;
    }

    clearConversation();
    sessionIdRef.current = null;
    scenarioRef.current = null;
    chunksRef.current = [];
    pendingLearnerTurnIdRef.current = null;
    pendingPronunciationRef.current = null;
    pendingTutorTextRef.current = null;
    micPendingRef.current = false;
    statusRef.current = "ready";

    setTurns([]);
    setStatus("ready");
    setMicPending(false);
    setLiveEnabled(false);
    setLiveConnecting(false);
    setIsTutorSpeaking(false);
    setNotRecognized(null);
    setUtteranceError(false);
    setPendingAudio(null);
    setMicError(false);
    setNamePromptOpen(false);
    setTopicProgress(null);
    setJustLearned(false);
    setCompletion(null);
    setEvaluation(null);
    resetIdeas();
    launchedRef.current = false;
    setPhase("topic");

    navigate(mode === "roleplay" ? "/app/speaking/role-talk" : "/app/speaking", {
      replace: true,
    });
  }

  async function toggleRecording() {
    if (liveEnabled) {
      await disableLiveConversation();
      return;
    }
    if (status === "recording") {
      const recorder = recorderRef.current;
      if (recorder?.state === "recording") {
        // Stop is an explicit submit action: close the microphone immediately, then keep the
        // control disabled while the captured clip is converted and sent through STT → tutor → TTS.
        recorder.stop();
        setStatus("thinking");
        statusRef.current = "thinking";
      }
      return;
    }
    await startRecording();
  }

  async function startRecording() {
    if (micPendingRef.current || statusRef.current === "recording" || statusRef.current === "thinking" || tutorSpeakingRef.current)
      return;
    const microphoneRequest = microphoneRequestRef.current + 1;
    microphoneRequestRef.current = microphoneRequest;
    micPendingRef.current = true;
    setMicPending(true);
    setNotRecognized(null);
    setUtteranceError(false);
    setPendingAudio(null);
    setMicError(false);
    try {
      if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === "undefined") {
        showMicrophoneIssue(new DOMException("Audio recording is not supported", "NotSupportedError"));
        return;
      }
      const stream = await requestSpeakingMicrophone();
      if (microphoneRequest !== microphoneRequestRef.current) {
        stopMediaStream(stream);
        return;
      }
      const audioTrack = stream.getAudioTracks()[0];
      if (!audioTrack || audioTrack.readyState === "ended") {
        stopMediaStream(stream);
        showMicrophoneIssue(new DOMException("Microphone is unavailable", "NotFoundError"));
        return;
      }
      audioTrack.addEventListener("ended", () => {
        if (recorderRef.current?.state === "recording") recorderRef.current.stop();
        showMicrophoneIssue(new DOMException("Microphone track ended", "NotReadableError"));
      }, { once: true });
      const recorder = createAudioRecorder(stream);
      recorderRef.current = recorder;
      chunksRef.current = [];
      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) chunksRef.current.push(e.data);
      };
      recorder.onstop = async () => {
        recorderRef.current = null;
        stopMediaStream(stream);
        const blob = new Blob(chunksRef.current, { type: recorder.mimeType });
        if (blob.size === 0) {
          setNotRecognized(uz.speaking.notRecognized);
          setMicError(false);
          setStatus("ready");
          statusRef.current = "ready";
          return;
        }
        // MediaRecorder gives WebM/Opus; the backend (Azure push-stream) needs
        // 16 kHz mono 16-bit PCM WAV - transcode before upload, else STT returns
        // NoMatch and the turn is reported unrecognized.
        try {
          const wav = await blobToWav16kMono(blob);
          void sendUtterance(bytesToBase64(wav));
        } catch {
          setNotRecognized(uz.speaking.invalidAudio);
          setMicError(false);
          setStatus("ready");
          statusRef.current = "ready";
        }
      };
      recorder.start();
      window.dispatchEvent(new Event("assistant:close-for-recording"));
      setStatus("recording");
      statusRef.current = "recording";
    } catch (error) {
      if (microphoneRequest === microphoneRequestRef.current) {
        showMicrophoneIssue(error);
      }
    } finally {
      if (microphoneRequest === microphoneRequestRef.current) {
        micPendingRef.current = false;
        setMicPending(false);
      }
    }
  }

  function restart() {
    stopTutorPlayback();
    sessionIdRef.current = null;
    setTurns([]);
    setStatus("ready");
    setIsTutorSpeaking(false);
    setTopicProgress(null);
    setJustLearned(false);
    resetIdeas();
    setPhase("topic");
    if (hasExplicitLaunch) {
      navigate(topicBackTarget, { replace: true });
      launchedRef.current = false;
    }
  }

  const routeLoading =
    (Boolean(routeTopicCode) && routeFreeTalkLoading) ||
    (Boolean(routeScenarioCode) && routeRoleplayLoading);
  const routeError = routeFreeTalkError ?? routeRoleplayError;
  const routeMissing =
    (Boolean(routeTopicCode) && !routeFreeTalkLoading && !routeFreeTalkError && !routeFreeTalkTopic) ||
    (Boolean(routeScenarioCode) && !routeRoleplayLoading && !routeRoleplayError && !routeRoleplayScenario);

  // A vocabulary topic deliberately opens on Pen screen 47 first. It must not
  // be swallowed by the generic "route is starting" loading gate, otherwise a
  // direct visit briefly (and in tests permanently) renders the connection
  // surface instead of the learner-controlled microphone-permission step.
  if (hasExplicitLaunch && !launchedRef.current && !routeTopicId) {
    if (routeLoading) {
      return <SpeakingConnectionScreen />;
    }
    if (routeError) {
      const reload = routeTopicCode ? reloadRouteFreeTalk : reloadRouteRoleplay;
      return (
        <NoticeScreen
          icon="cloud_off"
          tone="error"
          title={uz.speaking.error.title}
          text={uz.speaking.error.unavailableText}
          primaryLabel={uz.speaking.error.retry}
          primaryIcon="refresh"
          onPrimary={reload}
          secondaryLabel={uz.common.back}
          secondaryIcon="arrow_back"
          onSecondary={() => navigate(topicBackTarget, { replace: true })}
       />
      );
    }
    if (routeMissing) {
      return (
        <NoticeScreen
          icon="search_off"
          tone="error"
          title={uz.speaking.error.title}
          text={uz.speaking.topicsEmpty}
          primaryLabel={uz.common.back}
          primaryIcon="arrow_back"
          onPrimary={() => navigate(topicBackTarget, { replace: true })}
       />
      );
    }
    return <SpeakingConnectionScreen />;
  }

  if (phase === "topic") {
    // Roleplay now lives on its own catalog page (/speaking/role-talk), mirroring free-talk: the
    // "Rolli suhbat" entry navigates there, and picking a scenario returns here with a launch state
    // that auto-starts the scored roleplay. So the picker itself only handles open conversations.
    // Do NOT ask for the learner's name here - they haven't picked a topic yet.
    return (
      <>
        <TopicPicker onRoleplay={() => navigate("/app/speaking/role-talk")} />
      </>
    );
  }

  if (phase === "preparation" && routeTopicId) {
    return (
      <SpeakingPreparationScreen
        topicTitle={launch?.topicTitle?.trim() || "My Mother"}
        onBack={() => navigate("/app/speaking/topics")}
        onStart={() => {
          void beginConversation({
            label: launch?.topicTitle?.trim() || "My Mother",
            vocabularyTopicId: routeTopicId,
          });
        }}
      />
    );
  }

  if (phase === "scoring") {
    return (
      <div className="max-w-[480px] mx-auto w-full flex flex-col items-center text-center pt-xl gap-md">
        <Spinner />
        <p className="font-body-md text-body-md text-text-secondary">
          {uz.speaking.roleplay.scoring}
        </p>
      </div>
    );
  }

  if (phase === "scorecard") {
    return (
      <main className="speaking-result-page">
        <RoleplayScoreCard
          evaluation={evaluation}
          onPlayAgain={() =>
            scenarioRef.current != null && beginRoleplay(scenarioRef.current)
          }
          onChooseScenario={() => navigate("/app/speaking/role-talk")}
          onRetry={finishRoleplay}
          onSpeaking={() => navigate("/app/speaking")}
          onHome={() => navigate("/home")}
       />
      </main>
    );
  }

  if (phase === "limit") {
    return (
      <NoticeScreen
        icon="lock"
        tone="warning"
        title={uz.speaking.limit.title}
        text={uz.speaking.limit.text}
        primaryLabel={uz.speaking.limit.upgrade}
        primaryIcon="workspace_premium"
        onPrimary={() => navigate("/profile")}
        secondaryLabel={uz.speaking.limit.home}
        secondaryIcon="home"
        onSecondary={() => navigate("/home")}
        cardAccent="board3"
     />
    );
  }

  if (phase === "timeUp") {
    return (
      <NoticeScreen
        icon="timer"
        tone="warning"
        title={uz.speaking.timeUp.title}
        text={uz.speaking.timeUp.text}
        primaryLabel={uz.speaking.timeUp.ok}
        primaryIcon="check"
        onPrimary={() => navigate("/home")}
     />
    );
  }

  if (phase === "error") {
    const errorText = startErrorKind === "network"
      ? uz.speaking.error.networkText
      : startErrorKind === "rate-limited"
        ? startRetryAfterSeconds > 0
          ? `${uz.speaking.error.rateLimitedText} ${startRetryAfterSeconds} soniyadan keyin qayta urinishingiz mumkin.`
          : uz.speaking.error.rateLimitedText
        : uz.speaking.error.unavailableText;
    return (
      <NoticeScreen
        icon="cloud_off"
        tone="error"
        title={uz.speaking.error.title}
        text={errorText}
        primaryLabel={uz.speaking.error.retry}
        primaryIcon="refresh"
        onPrimary={() =>
          mode === "roleplay" && scenarioRef.current != null
            ? beginRoleplay(scenarioRef.current)
            : beginConversation(lastSelectionRef.current)
        }
        secondaryLabel={uz.speaking.limit.home}
        secondaryIcon="home"
        onSecondary={() => navigate("/home")}
     />
    );
  }

  if (phase === "summary") {
    return (
      <main className="speaking-result-page" data-pen-screen="50">
        <SessionSummary
          turns={turns}
          completion={completion}
          topicTitle={lastSelectionRef.current.label}
          onRestart={restart}
          onPronunciationDetails={() =>
            trackPronunciationFeedbackViewed(sessionIdRef.current ?? undefined)
          }
       />
      </main>
    );
  }

  if (status === "connecting") {
    return <SpeakingConnectionScreen />;
  }

  const isBusy = status === "thinking";
  const isRecording = status === "recording";
  const microphoneLoading = isBusy || micPending || liveConnecting;
  const microphoneActive = isRecording || liveEnabled;
  const microphoneLabel = isTutorSpeaking
    ? uz.speaking.speakingNow
    : liveConnecting
      ? uz.speaking.liveConnecting
      : micPending
        ? uz.speaking.micPreparing
        : isBusy
          ? uz.speaking.thinking
          : microphoneActive
            ? liveEnabled ? uz.speaking.liveActive : uz.speaking.recording
            : uz.speaking.pressToSpeak;
  const microphoneIcon = microphoneLoading
    ? undefined
    : isTutorSpeaking
      ? "graphic_eq"
      : microphoneActive
        ? "stop_circle"
        : "mic";
  const sessionPrompt = lastSelectionRef.current.label;
  // The daily speaking budget outranks the transient banners: once it is gone, nothing else the
  // learner does in this sitting will work, so telling them that first is the useful message.
  const quotaFeedback = quotaExhausted
    ? { tone: "warning" as const, text: uz.speaking.quotaExhausted }
    : speakingQuota && speakingQuota.remainingMinutes <= 0
      ? { tone: "warning" as const, text: uz.speaking.quotaExhausted }
      : speakingQuota && speakingQuota.remainingMinutes < 1
        ? { tone: "warning" as const, text: uz.speaking.quotaAlmostGone }
        : null;
  const sessionFeedback = quotaFeedback
    ?? (micError
      ? { tone: "error" as const, text: uz.speaking.micPermission }
      : notRecognized && !isBusy
        ? { tone: "warning" as const, text: notRecognized }
        : utteranceError && pendingAudio && !isBusy
          ? { tone: "error" as const, text: uz.speaking.utteranceError }
          : null);
  const dockStatus = isTutorSpeaking
    ? "EnglishAI javob bermoqda…"
    : isBusy
      ? "Javob tayyorlanmoqda…"
      : microphoneActive
        ? "Gapiryapsiz…"
        : "Tayyor. Gapirishni boshlang.";
  const dockHint = isTutorSpeaking
    ? "Javob tugagach, o‘z fikringizni ayting."
    : isBusy
      ? "Nutqingiz tahlil qilinmoqda."
      : microphoneActive
        ? "Gap tugagach, qisqa pauza qiling."
        : "Mikrofonni bosib, tabiiy gapiring.";
  const recordAction = () => void (
    liveAvailable
      ? liveEnabled
        ? disableLiveConversation()
        : enableLiveConversation()
      : toggleRecording()
  );
  const primaryControlLabel = isTutorSpeaking || microphoneActive
    ? "Mikrofonni o‘chirish"
    : microphoneLabel;

  return (
    <div className="sp17 sp17--live sp17--lesson-frame vocabulary-topic vocabulary-topic--speaking relative overflow-hidden" data-testid="speaking-lesson-frame" data-pen-screen="48" data-speaking-mode={mode}>
      <DesignModal
        open={justLearned}
        onClose={() => setJustLearned(false)}
        title={uz.speaking.topicLearned}
        description={uz.speaking.topicLearnedHint}
        className="sp17__learned-modal"
        closeLabel="Yopish"
        closeOnBackdrop={false}
        showClose={false}
        footer={
          <AppButton leadingIcon="arrow_forward" onClick={() => setJustLearned(false)}>
            Davom etish
          </AppButton>
        }
      >
        <div className="sp17__learned-modal-celebration" aria-hidden="true">
          <Icon name="celebration" filled className="text-[52px]" />
        </div>
      </DesignModal>
      <main className="sp17__pen-live-shell">
        <LessonProgress
          steps={SPEAKING_LIVE_STEPS}
          step="conversation"
          onBack={() => void exitSession()}
          backLabel="Mavzularga qaytish"
          classPrefix="speaking-progress"
        />

        <SpeakingSessionHeader prompt={sessionPrompt} />

        <p className="sp17__replay-hint">
          <Icon name="volume_up" filled />
          AI matni yonidagi ovoz tugmasi — faqat shu xabarni qayta tinglash.
        </p>

        <section className="sp17__live-transcript-panel" aria-label="Jonli suhbat matni">
          <div
            ref={scrollRef}
            className="sp17__transcript"
            data-testid="speaking-transcript"
            onScroll={(event) => {
              const transcript = event.currentTarget;
              const distanceFromBottom = transcript.scrollHeight - transcript.scrollTop - transcript.clientHeight;
              shouldFollowTranscriptRef.current = distanceFromBottom < 96;
            }}
          >
            <div className="sp17__conversation-reserve" aria-hidden="true" />

            {turns.map((turn) =>
              turn.role === "tutor" ? (
                <TutorBubble
                  key={turn.id}
                  turn={turn}
                  onRetry={() => void prefetchTranslation(turn)}
                  onReplay={() => playTutor(turn.id, turn.text, turn.audioBase64 ?? "", [], Boolean(turn.audioBase64))}
                />
              ) : (
                <LearnerBubble
                  key={turn.id}
                  turn={turn}
                  onRetryTranslation={() => void prefetchTranslation(turn)}
                  onDetails={(word) => {
                    trackPronunciationFeedbackViewed(
                      sessionIdRef.current ?? undefined
                    );
                    navigate(`/app/speaking/pronunciation/${encodeURIComponent(word)}`);
                  }}
               />
              )
            )}
          </div>
        </section>

        <section className="sp17__recording-dock" aria-live="polite">
          <div className="sp17__recording-status">
            <span
              className={cn("sp17__recording-orb", microphoneActive && "is-active", isBusy && "is-busy")}
              aria-hidden="true"
            >
              <Icon name={microphoneLoading ? "progress_activity" : "mic"} filled />
            </span>
            <div>
              <strong>{dockStatus}</strong>
              <span>{dockHint}</span>
            </div>
          </div>

          {sessionFeedback && (
            <p className={cn("sp17__dock-feedback", `sp17__dock-feedback--${sessionFeedback.tone}`)} data-testid="speaking-feedback-slot">
              {sessionFeedback.text}
            </p>
          )}

          <div className="sp17__dock-actions">
            {utteranceError && pendingAudio && !isBusy ? (
              <button type="button" className="sp17__dock-action sp17__dock-action--primary" onClick={() => void sendUtterance(pendingAudio, true)}>
                <Icon name="refresh" /> {uz.speaking.retryUtterance}
              </button>
            ) : (
              <button
                type="button"
                className={cn(
                  "sp17__dock-action",
                  "sp17__dock-action--primary",
                  "sp17__record-action",
                  microphoneActive && "is-active is-recording",
                  isTutorSpeaking && "is-tutor-speaking",
                  microphoneLoading && "is-loading",
                )}
                aria-label={microphoneActive ? uz.speaking.stopRecording : microphoneLabel}
                onClick={recordAction}
                disabled={isBusy || micPending || isTutorSpeaking}
                aria-busy={microphoneLoading || undefined}
              >
                <Icon
                  name={microphoneLoading ? "progress_activity" : microphoneActive ? "mic_off" : microphoneIcon ?? "mic"}
                  filled
                  className={microphoneLoading ? "sp17__record-loader" : undefined}
                />
                {primaryControlLabel}
              </button>
            )}
            <button type="button" className="sp17__dock-action sp17__dock-action--end" aria-label={uz.common.close} onClick={() => void exitSession()}>
              <Icon name="logout" /> Tugatish
            </button>
          </div>

          <div className="sp17__session-tools" data-testid="speaking-session-tools">
            {namePromptOpen && (
              <NamePromptBar
                onSaved={() => setNamePromptOpen(false)}
                onDismiss={() => setNamePromptOpen(false)}
             />
            )}
            {!isTutorSpeaking && (
              <>
                {!isRecording && !isBusy && !micPending && !liveConnecting && (
                  <button
                    type="button"
                    onClick={() => {
                      setIdeaSheetOpen(true);
                      void loadIdeaCards();
                    }}
                    aria-label="Gapirish g‘oyalarini ko‘rsatish"
                    className="sp17__idea-button"
                  >
                    <Icon name="lightbulb" filled /> {uz.speaking.ideas.button}
                  </button>
                )}
                {!isRecording && !namePromptOpen && (
                  <button
                    type="button"
                    onClick={() => setNamePromptOpen(true)}
                    className="sp17__name-button"
                  >
                    <Icon name="badge" /> {uz.speaking.namePrompt.open}
                  </button>
                )}
              </>
            )}
          </div>
        </section>

        {ideaSheetOpen && (
          <IdeaCardsSheet
            cards={ideaCards}
            state={ideaState}
            onRefresh={() => loadIdeaCards(true)}
            onClose={() => setIdeaSheetOpen(false)}
         />
        )}
      </main>
    </div>
  );
}

type IdeaState = "idle" | "loading" | "error";

function SpeakingConnectionScreen() {
  return (
    <div className="sp17 sp17--connecting" role="status" aria-live="polite" aria-label={uz.speaking.connecting}>
      <SpeakingIndicator size={72} />
      <span>{uz.speaking.connecting}</span>
    </div>
  );
}

function SpeakingPreparationScreen({
  topicTitle,
  onBack,
  onStart,
}: {
  topicTitle: string;
  onBack: () => void;
  onStart: () => void;
}) {
  const [requesting, setRequesting] = useState(false);
  const [permissionError, setPermissionError] = useState<string | null>(null);

  async function requestPermissionAndStart() {
    if (requesting) return;
    setRequesting(true);
    setPermissionError(null);
    try {
      const stream = await requestSpeakingMicrophone();
      stopMediaStream(stream);
      onStart();
    } catch (error) {
      setPermissionError(
        isMicrophonePermissionDenied(error)
          ? "Mikrofonga ruxsat berilmadi. Brauzer sozlamalaridan ruxsat bering."
          : "Mikrofonni ishga tushirib bo‘lmadi. Qayta urinib ko‘ring.",
      );
    } finally {
      setRequesting(false);
    }
  }

  return (
    <main className="sp17 sp17--preparation" data-testid="speaking-preparation" data-pen-screen="47">
      <section className="sp17__preparation-content">
        <header className="sp17__preparation-heading">
          <span className="speaking-tag">SPEAKING</span>
          <h1>{topicTitle}</h1>
          <p>Tell me about someone who supports you.</p>
        </header>

        <div className="sp17__preparation-ready">
          <img src="/assets/play/parrot.svg" alt="" width={220} height={184} />
          <p>Tayyor bo‘lsangiz, men tinglayman.</p>
        </div>

        <section className="sp17__preparation-phrases" aria-labelledby="speaking-phrase-title">
          <span id="speaking-phrase-title">BOSHLASH UCHUN IBORALAR</span>
          <strong>She has always…</strong>
          <strong>I admire her because…</strong>
          <strong>She has taught me to…</strong>
        </section>

        <p className="sp17__preparation-permission"><Icon name="mic" filled /> Suhbat uchun mikrofon ruxsati kerak.</p>
        {permissionError && <p className="sp17__preparation-error" role="alert">{permissionError}</p>}
        <div className="sp17__preparation-actions">
          <button type="button" disabled={requesting} onClick={() => void requestPermissionAndStart()}>
            {requesting ? "Ruxsat so‘ralmoqda…" : "Mikrofonga ruxsat berish"} <Icon name="arrow_forward" />
          </button>
          <small>Ruxsat berilmasa, brauzer sozlamalarini tekshiring.</small>
        </div>
      </section>
      <button type="button" className="sp17__preparation-back" onClick={onBack} aria-label="Mavzularga qaytish">
        <Icon name="arrow_back" /> Mavzularga qaytish
      </button>
    </main>
  );
}

function SpeakingSessionHeader({ prompt }: { prompt: string }) {
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const level = (getStoredLevel() as string | null) ?? "B1";

  useEffect(() => {
    const startedAt = Date.now();
    const timer = window.setInterval(() => {
      setElapsedSeconds(Math.floor((Date.now() - startedAt) / 1_000));
    }, 1_000);
    return () => window.clearInterval(timer);
  }, []);

  const elapsedLabel = `${Math.floor(elapsedSeconds / 60)}:${String(elapsedSeconds % 60).padStart(2, "0")}`;

  return (
    <div className="sp17__live-session-status" data-testid="speaking-fixed-header">
      <span className="sp17__session-topic" title={prompt}>{prompt} · {level}</span>
      <span className="sp17__session-time" aria-label={`Suhbat vaqti ${elapsedLabel}`}>
        <Icon name="timer" /> {elapsedLabel}
      </span>
    </div>
  );
}

/**
 * The idea-card list with loading / error / empty handling. Each card shows a concrete English
 * talking-point question and sentence starter the learner finishes in their own words.
 */
function IdeaCardsList({
  cards,
  state,
  onRefresh,
}: {
  cards: IdeaCardDto[] | null;
  state: IdeaState;
  onRefresh: () => void;
}) {
  if (state === "loading" && !cards) {
    return (
      <div
        className="sp17__idea-grid grid grid-cols-1 sm:grid-cols-2 gap-sm"
        role="status"
        aria-live="polite"
        aria-busy="true"
        aria-label={uz.speaking.ideas.loading}
      >
        {QUICK_IDEA_CARDS.map((_, index) => <div key={index} data-testid="speaking-idea-skeleton" className="sp17__idea-card sp17__idea-card--skeleton" />)}
      </div>
    );
  }
  if (state === "error" && !cards) {
    return (
      <div className="flex flex-col items-center gap-sm py-lg text-center">
        <p className="font-caption text-caption text-error">
          {uz.speaking.ideas.error}
        </p>
        <Button variant="outline" icon="refresh" onClick={onRefresh}>
          {uz.speaking.ideas.refresh}
        </Button>
      </div>
    );
  }
  if (!cards || cards.length === 0) {
    return (
      <p className="font-caption text-caption text-text-secondary py-md text-center">
        {uz.speaking.ideas.empty}
      </p>
    );
  }
  return (
    <>
      <div className="sp17__idea-grid grid grid-cols-1 sm:grid-cols-2 gap-sm" aria-busy={state === "loading"}>
        {cards.map((card, i) => <IdeaCardItem key={i} card={card} />)}
      </div>
      <AppButton
        tone="standard"
        onClick={onRefresh}
        disabled={state === "loading"}
        leadingIcon="refresh"
        className="mt-sm mx-auto"
      >
        {uz.speaking.ideas.refresh}
      </AppButton>
    </>
  );
}

/** One idea card: a picture, the talking-point question, and a sentence starter to complete. */
function IdeaCardItem({
  card,
}: {
  card: IdeaCardDto;
}) {
  return (
    <div className="sp17__idea-card p-sm">
      <div className="min-w-0">
        <p className="font-label-md text-label-md text-text-primary">
          {card.prompt}
        </p>
        <p className="font-caption text-caption text-text-secondary mt-xs">
          <span className="text-tertiary">
            {uz.speaking.ideas.starterLabel}{" "}
          </span>
          <span className="italic">{card.starter}…</span>
        </p>
      </div>
    </div>
  );
}

/**
 * Mid-conversation "I need an idea" helper, opened from the button by the mic as a centered modal
 * on every viewport so the conversation remains visible behind one consistent overlay.
 */
function IdeaCardsSheet({
  cards,
  state,
  onRefresh,
  onClose,
}: {
  cards: IdeaCardDto[] | null;
  state: IdeaState;
  onRefresh: () => void;
  onClose: () => void;
}) {
  return (
    <DesignModal
      open
      onClose={onClose}
      title={uz.speaking.ideas.sheetTitle}
      description={uz.speaking.ideas.sheetSubtitle}
      closeLabel={uz.speaking.ideas.close}
      className="sp17__ideas-sheet"
      footer={<AppButton leadingIcon="check" onClick={onClose}>{uz.speaking.ideas.close}</AppButton>}
    >
      <IdeaCardsList cards={cards} state={state} onRefresh={onRefresh} />
    </DesignModal>
  );
}

/**
 * Inline "type your name" panel shown inside the live conversation when speech-to-text could not
 * capture the learner's spoken name (the server flags this), or opened manually to set/correct it.
 * Saving stores it on the account (the tutor's only name source - rule: it never invents one), so the
 * tutor addresses the learner by it from the next turn. All wording lives in the content store (rule 11).
 */
function NamePromptBar({
  onSaved,
  onDismiss,
}: {
  onSaved: () => void;
  onDismiss: () => void;
}) {
  const { applyUser, user } = useAuth();
  const [name, setName] = useState(user?.preferredName ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(false);
  const trimmed = name.trim();

  async function save() {
    if (!trimmed || saving) return;
    setSaving(true);
    setError(false);
    try {
      const updated = await api.auth.setPreferredName(trimmed);
      applyUser(updated); // refresh the cached user so the name is remembered everywhere
      onSaved();
    } catch (err) {
      // A 400 (too long/blank) is a real input problem; anything else is transient - let them retry.
      setError(err instanceof ApiError && err.status === 400);
      setSaving(false);
    }
  }

  return (
    <div className="sp17__name-panel w-full max-w-[420px] p-md">
      <div className="flex items-start gap-sm mb-sm">
        <Icon
          name="badge"
          filled
          className="text-primary text-[22px] shrink-0"
       />
        <div>
          <p className="font-label-md text-label-md text-text-primary">
            {uz.speaking.namePrompt.heading}
          </p>
          <p className="font-caption text-caption text-text-secondary">
            {uz.speaking.namePrompt.hint}
          </p>
        </div>
      </div>
      <input
        type="text"
        value={name}
        autoFocus
        maxLength={40}
        onChange={(e) => {
          setName(e.target.value);
          setError(false);
        }}
        onKeyDown={(e) => {
          if (e.key === "Enter") void save();
        }}
        placeholder={uz.speaking.namePrompt.placeholder}
        className="sp17__name-input w-full px-lg py-sm font-body-md text-body-md text-text-primary focus:outline-none mb-sm"
     />
      {error && (
        <p className="font-caption text-caption text-error mb-sm">
          {uz.speaking.namePrompt.error}
        </p>
      )}
      <div className="flex gap-sm">
        <Button
          variant="accent"
          icon="check"
          onClick={save}
          disabled={!trimmed || saving}
        >
          {uz.speaking.namePrompt.save}
        </Button>
        <Button variant="ghost" onClick={onDismiss} disabled={saving}>
          {uz.speaking.namePrompt.dismiss}
        </Button>
      </div>
    </div>
  );
}

function TopicPicker({ onRoleplay }: { onRoleplay: () => void }) {
  const navigate = useNavigate();

  useEffect(() => {
    const pageRoots = [document.documentElement, document.body, document.getElementById("root")].filter(
      (element): element is HTMLElement => Boolean(element),
    );
    pageRoots.forEach((element) => element.classList.add("speaking-hub-page-active"));
    return () => pageRoots.forEach((element) => element.classList.remove("speaking-hub-page-active"));
  }, []);

  return (
    <div className="sp17 sp17--hub sp17--pen-hub" data-testid="speaking-catalog">
      <div className="sp17__content">
        <section className="sp17__pen-heading" aria-labelledby="speaking-hub-title">
          <span className="speaking-tag">SPEAKING</span>
          <h1 id="speaking-hub-title">Ovozingizni<br />eshittiring.</h1>
          <p>Mukammal bo‘lish shart emas. Gapirish kifoya.</p>
        </section>

        <motion.section
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          className="sp17__pen-companion"
          aria-label="Erkin suhbatni boshlash"
        >
          <img src="/assets/play/parrot.svg" alt="" width={190} height={160} />
          <div className="sp17__pen-companion-copy">
            <span>SABRLI AI SUHBATDOSH</span>
            <h2>Bir suhbatdan<br />boshlaymizmi?</h2>
            <button type="button" onClick={() => navigate("/app/speaking/free")}>
              Erkin suhbatni boshlash <Icon name="arrow_forward" />
            </button>
          </div>
        </motion.section>

        <section className="sp17__pen-modes" aria-label="Speaking mashqlari">
          <button type="button" onClick={() => navigate("/app/speaking/topics")}>
            <Icon name="chat_bubble" filled />
            <span>Mavzuli suhbat</span>
          </button>
          <button type="button" onClick={onRoleplay}>
            <Icon name="diversity_3" filled />
            <span>Rolli suhbat</span>
          </button>
          <button type="button" onClick={() => navigate("/app/speaking/practice-words")}>
            <Icon name="graphic_eq" filled />
            <span>Talaffuz mashqi</span>
          </button>
        </section>

        <section className="sp17__pen-today" aria-labelledby="speaking-today-title">
          <h2 id="speaking-today-title">Bugun shu mavzudan</h2>
          <button type="button" aria-label="My Mother mavzusini ochish" onClick={() => navigate("/app/speaking/topics")} className="sp17__pen-topic-card">
            <img src="/assets/play/home-photos/my-mother.jpg" alt="" />
            <span>
              <small><Icon name="chat_bubble" filled /> Mavzuli suhbat</small>
              <strong>My Mother</strong>
              <em>B1 · Oila haqida suhbat</em>
              <b>Boshlash <Icon name="arrow_forward" /></b>
            </span>
          </button>
        </section>
      </div>
    </div>
  );
}

/**
 * End-of-scene roleplay score card. Shows the overall 0-100 score, a bar per dimension, and the
 * Uzbek summary/strength/tip strings the backend resolved from vetted templates (rule 11). When the
 * learner never spoke the result is non-evaluable (only the summary prompt shows); when scoring
 * failed `evaluation` is null and a retry state is rendered instead.
 */
function RoleplayScoreCard({
  evaluation,
  onPlayAgain,
  onChooseScenario,
  onRetry,
  onSpeaking,
  onHome,
}: {
  evaluation: RoleplayEvaluationResult | null;
  onPlayAgain: () => void;
  onChooseScenario: () => void;
  onRetry: () => void;
  onSpeaking: () => void;
  onHome: () => void;
}) {
  const t = uz.speaking.roleplay;

  if (!evaluation) {
    return (
      <NoticeScreen
        icon="cloud_off"
        tone="error"
        title={t.scoreError}
        text={uz.speaking.error.unavailableText}
        primaryLabel={uz.common.retry}
        primaryIcon="refresh"
        onPrimary={onRetry}
        secondaryLabel={t.score.backHome}
        secondaryIcon="home"
        onSecondary={onHome}
     />
    );
  }

  const overall = Math.round(evaluation.overallScore);
  const passed = evaluation.evaluable && overall >= 60;
  const dimensions: { code: string; score: number }[] = [
    { code: "task_completion", score: evaluation.taskCompletion },
    { code: "fluency", score: evaluation.fluency },
    { code: "grammar", score: evaluation.grammar },
    { code: "appropriateness", score: evaluation.appropriateness },
  ];

  return (
    <SpeakingResultShell
      status={!evaluation.evaluable ? "neutral" : passed ? "passed" : "retry"}
      kicker={evaluation.evaluable ? (passed ? "Ajoyib natija" : "Mashqni davom ettiring") : "Suhbat yakunlandi"}
      title={t.score.title}
      subtitle={evaluation.summaryUz}
      score={evaluation.evaluable ? overall : undefined}
      scoreLabel={evaluation.evaluable ? t.score.overall : undefined}
      celebrate={passed}
      scoreContent={evaluation.evaluable ? (
        <div className="sp17__results-dimensions">
          {dimensions.map((dimension) => (
            <DimensionBar
              key={dimension.code}
              label={t.score.dimensions[dimension.code] ?? dimension.code}
              score={Math.round(dimension.score)}
           />
          ))}
        </div>
      ) : undefined}
      details={evaluation.strengthUz || evaluation.tipUz ? (
        <div className="sp17__results-insights">
          {evaluation.strengthUz && (
            <div className="sp17__results-insight is-strength">
              <Icon name="verified" filled className="text-ea-green-600 text-[20px] shrink-0" />
              <p className="font-body-md text-body-md text-text-primary">{evaluation.strengthUz}</p>
            </div>
          )}
          {evaluation.tipUz && (
            <div className="sp17__results-insight is-tip">
              <Icon name="lightbulb" filled className="text-ea-orange-600 text-[20px] shrink-0" />
              <p className="font-body-md text-body-md text-text-primary">{evaluation.tipUz}</p>
            </div>
          )}
        </div>
      ) : undefined}
      actions={(
        <>
        <DuoButton className="sp17__results-button sp17__results-button--primary" color="green" icon="refresh" fullWidth onClick={onPlayAgain}>
          {t.score.playAgain}
        </DuoButton>
        <DuoButton className="sp17__results-button sp17__results-button--secondary" color="purple" icon="view_module" fullWidth onClick={onChooseScenario}>
          {t.score.chooseScenario}
        </DuoButton>
        <Button className="sp17__results-button sp17__results-button--speaking" variant="outline" fullWidth icon="arrow_back" onClick={onSpeaking}>
          {t.score.backSpeaking}
        </Button>
        <Button className="sp17__results-button sp17__results-button--home" variant="outline" fullWidth icon="home" onClick={onHome}>
          {t.score.backHome}
        </Button>
        </>
      )}
   />
  );
}

/** One 0-100 dimension score with a coloured bar. Green at/above the good cutoff, warning below. */
function DimensionBar({ label, score }: { label: string; score: number }) {
  const good = score >= 60;
  const width = Math.min(100, Math.max(0, score));
  return (
    <div className="w-full text-left">
      <div className="flex items-center justify-between mb-xs">
        <span className="font-label-md text-label-md text-text-secondary">
          {label}
        </span>
        <span
          className={cn(
            "font-label-md text-label-md",
            good ? "text-success" : "text-warning"
          )}
        >
          {score}/100
        </span>
      </div>
      <div className="h-1.5 rounded-full bg-surface-container overflow-hidden">
        <div
          className={cn(
            "h-full rounded-full transition-all duration-500",
            good ? "bg-success" : "bg-warning"
          )}
          style={{ width: `${width}%` }}
       />
      </div>
    </div>
  );
}

/** Centered full-screen notice (daily limit reached, or start failed) with up to two CTAs. */
function NoticeScreen({
  icon,
  tone,
  title,
  text,
  primaryLabel,
  primaryIcon,
  onPrimary,
  secondaryLabel,
  secondaryIcon,
  onSecondary,
  cardAccent,
}: {
  icon: string;
  tone: "warning" | "error";
  title: string;
  text: string;
  primaryLabel: string;
  primaryIcon: string;
  onPrimary: () => void;
  secondaryLabel?: string;
  secondaryIcon?: string;
  onSecondary?: () => void;
  cardAccent?: CardAccent;
}) {
  if (cardAccent) {
    return (
      <div className="mx-auto w-full max-w-[520px] px-2 pt-xl">
        <motion.section
          initial={{ opacity: 0, y: 18, scale: 0.97 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          transition={{ type: "spring", stiffness: 240, damping: 22 }}
          className={cn(cardClass(cardAccent), "relative overflow-hidden p-6 text-center md:p-9")}
        >
          <span className="relative mx-auto mb-5 flex h-20 w-20 items-center justify-center rounded-[20px] border border-white/40 bg-white/20 text-white">
            <Icon name={icon} filled className="text-[38px]" />
          </span>
          <h1 className="relative font-duo text-[25px] font-extrabold leading-tight text-white drop-shadow-[0_2px_2px_rgba(0,0,0,0.35)] md:text-[30px]">
            {title}
          </h1>
          <p className="relative mx-auto mt-3 max-w-[430px] font-body-md text-body-md leading-relaxed text-white">
            {text}
          </p>
          <div className="relative mt-7 flex w-full flex-col gap-3">
            <button
              type="button"
              onClick={onPrimary}
              className="flex min-h-12 w-full items-center justify-center gap-2 rounded-2xl border-2 border-white bg-ea-surface px-5 py-3 font-duo font-extrabold text-ea-primary  transition-transform"
            >
              <Icon name={primaryIcon} filled className="text-[20px]" />
              {primaryLabel}
            </button>
            {secondaryLabel && onSecondary && (
              <button
                type="button"
                onClick={onSecondary}
                className="flex min-h-12 w-full items-center justify-center gap-2 rounded-2xl border-2 border-white/60 bg-white/15 px-5 py-3 font-duo font-extrabold text-white   transition-colors hover:bg-white/25"
              >
                <Icon name={secondaryIcon ?? "home"} className="text-[20px]" />
                {secondaryLabel}
              </button>
            )}
          </div>
        </motion.section>
      </div>
    );
  }

  const toneClasses =
    tone === "warning"
      ? "bg-warning-bg text-warning"
      : "bg-error/10 text-error";
  return (
    <div className="max-w-[480px] mx-auto w-full flex flex-col items-center text-center pt-xl">
      <span
        className={cn(
          "w-20 h-20 rounded-full flex items-center justify-center mb-md",
          toneClasses
        )}
      >
        <Icon name={icon} filled className="text-[36px]" />
      </span>
      <h1 className="font-headline-md text-headline-md text-primary">
        {title}
      </h1>
      <p className="font-body-md text-body-md text-text-secondary mt-sm mb-xl">
        {text}
      </p>
      <div className="w-full flex flex-col gap-sm">
        <Button
          variant="accent"
          fullWidth
          icon={primaryIcon}
          onClick={onPrimary}
        >
          {primaryLabel}
        </Button>
        {secondaryLabel && onSecondary && (
          <Button
            variant="outline"
            fullWidth
            icon={secondaryIcon}
            onClick={onSecondary}
          >
            {secondaryLabel}
          </Button>
        )}
      </div>
    </div>
  );
}

/**
 * Slim progress toward learning the linked vocabulary topic by speaking about it (the 5-minute
 * rule). Shows accumulated minutes vs the 5-minute goal, or a learned badge once reached.
 */
function TranslationToggle({ turn, onRetry }: { turn: ChatTurn; onRetry: () => void }) {
  const [open, setOpen] = useState(false);
  const status = turn.translationStatus ?? "idle";
  return (
    <div className="sp17__translation">
      <button type="button" className="sp17__translation-button" onClick={() => {
        if (status === "idle") onRetry();
        setOpen((value) => !value);
      }}>
        <Icon name="translate" className="text-[16px]" />
        {open ? "Tarjimani yashirish" : "Tarjimani ko‘rish"}
      </button>
      {open && status === "loading" && <span className="sp17__translation-text">Tarjima tayyorlanmoqda...</span>}
      {open && status === "ready" && turn.translation && (
        <p className="sp17__translation-text sp17__translation-text--ready">{turn.translation}</p>
      )}
    </div>
  );
}

function TutorBubble({ turn, onRetry, onReplay }: { turn: ChatTurn; onRetry: () => void; onReplay: () => void }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 10, scale: 0.96 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ type: "spring", stiffness: 320, damping: 24 }}
      className="sp17__bubble sp17__bubble--tutor"
    >
      <div className="sp17__bubble-column">
        <div className="sp17__bubble-card">
          <div className="sp17__bubble-meta">
            <span>ENGLISHAI TUTOR</span>
            <button type="button" onClick={onReplay} aria-label="Tutor xabarini qayta tinglash">
              <Icon name="volume_up" filled />
            </button>
          </div>
          <KaraokeText text={turn.text} activeWordOffset={turn.activeWordOffset ?? null} />
        </div>
        <TranslationToggle turn={turn} onRetry={onRetry} />
      </div>
    </motion.div>
  );
}

function KaraokeText({ text, activeWordOffset }: { text: string; activeWordOffset: number | null }) {
  if (activeWordOffset === null) {
    return <p className="sp17__karaoke-text font-body-md text-body-md">{text}</p>;
  }
  const parts = Array.from(text.matchAll(/\S+|\s+/g));
  return (
    <p className="sp17__karaoke-text font-body-md text-body-md">
      {parts.map((part) => {
        const value = part[0];
        const offset = part.index ?? 0;
        const active = activeWordOffset !== null && !/^\s+$/.test(value) && offset === activeWordOffset;
        return active
          ? <mark key={`${offset}-${value}`} className="sp17__karaoke-word">{value}</mark>
          : <span key={`${offset}-${value}`}>{value}</span>;
      })}
    </p>
  );
}

function LearnerBubble({
  turn,
  onDetails,
  onRetryTranslation,
}: {
  turn: ChatTurn;
  onDetails: (word: string) => void;
  onRetryTranslation: () => void;
}) {
  const pron = turn.pronunciation;
  const good = pron && pron.band === PronunciationBand.Good;
  // Every word the learner mispronounced - each is openable for a detailed view, with the
  // word the tutor flagged first so it leads. De-duplicated, original order otherwise.
  const practiceWords =
    pron?.words.filter((w) => w.needsPractice).map((w) => w.word) ?? [];
  const orderedWords = Array.from(
    new Set([...(turn.focusWord ? [turn.focusWord] : []), ...practiceWords])
  );

  return (
    <motion.div
      initial={{ opacity: 0, y: 10, scale: 0.96 }}
      animate={
        pron && !good
          ? { opacity: 1, y: 0, scale: 1, x: [0, -8, 8, -6, 6, -3, 3, 0] }
          : { opacity: 1, y: 0, scale: 1 }
      }
      transition={{ type: "spring", stiffness: 320, damping: 24 }}
      className="sp17__bubble sp17__bubble--learner"
    >
      <div className="sp17__bubble-column items-end">
        <div
          className={cn(
            "sp17__bubble-card",
            pron && !good ? "is-warning" : "is-standard"
          )}
        >
          <div className="sp17__bubble-meta">
            <span>SIZ · OVOZDAN MATNGA</span>
            <small>hozir</small>
          </div>
          <p className="font-body-md text-body-md">{turn.text}</p>
        </div>
      </div>
      <TranslationToggle turn={turn} onRetry={onRetryTranslation} />
      {pron && (
        <div
          className={cn(
            "sp17__feedback-card relative w-full max-w-[420px] overflow-hidden p-sm",
            good
              ? "is-good"
              : "is-practice",
          )}
          aria-label={uz.speaking.pronunciation}
        >
          <div className="mb-sm flex items-center justify-between gap-sm">
            <span className="inline-flex items-center gap-xs font-duo text-label-md font-extrabold">
              <Icon name={good ? "verified" : "record_voice_over"} filled className="text-[20px]" />
              {uz.speaking.pronunciation}
            </span>
            {pron.isAuthentic ? (
              <span className="rounded-full bg-white/20 px-3 py-1 font-duo text-label-md font-extrabold ring-1 ring-white/30">
                {Math.round(pron.overallScore)}/100
              </span>
            ) : (
              <span className="rounded-full bg-white/15 px-3 py-1 font-caption text-caption ring-1 ring-white/25">
                {uz.speaking.realScoreUnavailable}
              </span>
            )}
          </div>
          <div className="mb-sm rounded-xl bg-black/10 p-sm ring-1 ring-white/15">
            <HighlightedText text={turn.text} highlights={practiceWords} dark />
          </div>
          {turn.feedbackUz && pron.isAuthentic && (
            <p className="mb-sm font-caption text-caption text-white/90">{turn.feedbackUz}</p>
          )}
          {orderedWords.length > 0 && pron.isAuthentic && (
            <div>
              <p className="mb-xs font-caption text-caption text-white/80">
                {uz.speaking.practiceWordsLabel}
              </p>
              <div className="flex flex-wrap gap-xs">
                {orderedWords.map((word) => {
                  const result = pron.words.find(
                    (item) => item.word.toLowerCase() === word.toLowerCase(),
                  );
                  return (
                    <button
                      key={word}
                      onClick={() => onDetails(word)}
                      className="flex items-center gap-xs rounded-xl bg-ea-surface px-md py-xs font-duo text-label-md font-extrabold text-ea-orange-600  transition "
                    >
                      {word}
                      {result?.spokenForm && result.spokenForm !== word && (
                        <span className="font-caption text-[11px] opacity-75">({result.spokenForm})</span>
                      )}
                      {result && <span className="text-[12px] opacity-70">{Math.round(result.accuracyScore)}</span>}
                      <Icon name="arrow_forward" className="text-[16px]" />
                    </button>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      )}
    </motion.div>
  );
}

/** Renders text with the mispronounced words underlined in warning colour. */
function HighlightedText({
  text,
  highlights,
  dark = false,
}: {
  text: string;
  highlights: string[];
  dark?: boolean;
}) {
  const set = new Set(highlights.map((w) => normalizeHighlightToken(w)));
  const parts = text.split(/(\s+)/);
  return (
    <p className={cn("font-body-md text-body-md", dark ? "text-white" : "text-text-primary")}>
      {parts.map((part, i) => {
        const bare = normalizeHighlightToken(part);
        const next = normalizeHighlightToken(parts[i + 2] ?? "");
        return set.has(bare) || set.has(`${bare} ${next}`) ? (
          <span
            key={i}
            className={cn(
              "font-bold underline decoration-2 underline-offset-4",
              dark ? "text-ea-orange-600" : "text-warning",
            )}
          >
            {part}
          </span>
        ) : (
          <span key={i}>{part}</span>
        );
      })}
    </p>
  );
}

function normalizeHighlightToken(value: string) {
  return value.toLowerCase().replace(/^[^a-z0-9']+|[^a-z0-9':.]+$/g, "");
}

function SessionSummary({
  turns,
  completion,
  topicTitle,
  onRestart,
  onPronunciationDetails,
}: {
  turns: ChatTurn[];
  completion: TopicCompletionDto | null;
  topicTitle?: string;
  onRestart: () => void;
  onPronunciationDetails: () => void;
}) {
  const learnerTurns = turns.filter((t) => t.role === "learner");
  const scored = learnerTurns.filter((t) => t.pronunciation);
  const avg =
    scored.length > 0
      ? Math.round(
          scored.reduce(
            (sum, t) => sum + (t.pronunciation?.overallScore ?? 0),
            0
          ) / scored.length
        )
      : 0;

  // Unique words that still need practice across the whole session.
  const focusWords = Array.from(
    new Set(
      learnerTurns.flatMap((t) =>
        (t.pronunciation?.words ?? [])
          .filter((w) => w.needsPractice)
          .map((w) => w.word)
      )
    )
  );

  const navigate = useNavigate();

  return (
    <SpeakingResultShell
      status={avg >= 60 ? "passed" : "retry"}
      kicker="BIR QADAM OLDINGA"
      title="Bugun ovozingiz eshitildi!"
      subtitle={uz.speaking.summary.subtitlePraise}
      celebrate={avg >= 80}
      illustration={<img src="/assets/play/parrot.svg" alt="" className="speaking-result__illustration" />}
      details={(
        <>
          <dl className="sp17__pen-result-metrics">
            <div><dt>javob</dt><dd>{learnerTurns.length}</dd></div>
            <div><dt>talaffuz / 100</dt><dd>{avg}</dd></div>
            <div><dt>suhbat</dt><dd>{Math.max(1, Math.round(learnerTurns.length * 0.7))}:20</dd></div>
          </dl>
          <motion.section
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.08 }}
            className="speaking-result__details-card sp17__pen-result-words"
          >
            <span>YANA MASHQ QILAMIZ</span>
            <h2>{uz.speaking.summary.focusWords}</h2>
            {focusWords.length === 0 ? (
              <p className="speaking-result__empty">{uz.speaking.summary.noFocusWords}</p>
            ) : (
              <div className="sp17__results-words">
                {focusWords.map((word) => (
                  <button
                    key={word}
                    onClick={() => {
                      onPronunciationDetails();
                      navigate(`/app/speaking/pronunciation/${encodeURIComponent(word)}`);
                    }}
                    className="sp17__results-word"
                  >
                    {word}<Icon name="arrow_forward" className="text-[16px]" />
                  </button>
                ))}
              </div>
            )}
          </motion.section>
        </>
      )}
      actions={(
        completion && lastSelectionTopicId(completion, topicTitle) ? (
          <LessonFlowAction
            current="speaking"
            topicId={completion.topicId}
            topicTitle={topicTitle ?? ""}
            completion={completion}
            passed={Boolean(completion.modules.find((module) => module.module === "Speaking")?.passed)}
            className="sp17__results-button sp17__results-button--primary"
          />
        ) : (
          <DuoButton className="sp17__results-button sp17__results-button--primary" color="purple" fullWidth onClick={onRestart}>
            {uz.speaking.summary.practiceAgain}
          </DuoButton>
        )
      )}
   />
  );
}

function lastSelectionTopicId(completion: TopicCompletionDto, topicTitle?: string): boolean {
  return Boolean(completion.topicId && topicTitle);
}
