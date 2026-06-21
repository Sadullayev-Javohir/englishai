import { StrictMode } from "react";
import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, PronunciationBand, PronunciationErrorType } from "@/api/types";
import { SpeakingPage } from "./SpeakingPage";

const topicsMock = vi.fn().mockResolvedValue([
  {
    id: "family",
    title: "My Family",
    titleUz: "Mening oilam",
    category: "People",
    level: CefrLevel.A1,
    isFilled: true,
    learned: false,
    passedModuleCount: 2,
    requiredModuleCount: 6,
    isMastered: false,
    isLocked: false,
    requiresPro: false,
    modules: [{ module: "Speaking", passed: false, unlocked: true }],
  },
]);
const vocabularyTopicMock = vi.fn().mockResolvedValue({
  id: "family",
  title: "My Family",
  titleUz: "Mening oilam",
});
const startMock = vi.fn().mockResolvedValue({
  sessionId: "session-1",
  tutorText: "Tell me about your family.",
  tutorAudioBase64: "",
  visemes: [],
  visemeAnimation: null,
  topicProgress: null,
  isNaturalVoice: false,
});
const ideaCardsMock = vi.fn().mockResolvedValue({ cards: [] });
const roleplayStartMock = vi.fn().mockResolvedValue({
  sessionId: "roleplay-session-1",
  scenarioCode: "airport_check_in",
  tutorText: "May I see your passport?",
  tutorAudioBase64: "",
  visemes: [],
  visemeAnimation: null,
  isNaturalVoice: false,
});
const roleplayEvaluateMock = vi.fn();
const utteranceStreamMock = vi.fn();
const translateMock = vi.fn().mockResolvedValue({ text: "Tell me about your family.", translation: "Oilangiz haqida gapirib bering." });
const analyticsTrackMock = vi.fn().mockResolvedValue(undefined);
const setPreferredNameMock = vi.fn();
const applyUserMock = vi.fn();
const liveCapabilitiesMock = vi.fn().mockResolvedValue({ enabled: false });
const liveHubJoinMock = vi.fn().mockResolvedValue(undefined);
const liveHubStopMock = vi.fn().mockResolvedValue(undefined);
const liveRecorderStartMock = vi.fn().mockResolvedValue(undefined);
const liveRecorderStopMock = vi.fn().mockResolvedValue(undefined);

vi.mock("@/api/speakingLiveHub", () => ({
  SpeakingLiveHubClient: class {
    joinSession = (...args: unknown[]) => liveHubJoinMock(...args);
    stop = (...args: unknown[]) => liveHubStopMock(...args);
    submitTurn = vi.fn().mockResolvedValue(undefined);
    interruptTutor = vi.fn().mockResolvedValue(undefined);
  },
}));

vi.mock("@/lib/liveVoiceRecorder", () => ({
  LiveVoiceRecorder: class {
    start = (...args: unknown[]) => liveRecorderStartMock(...args);
    stop = (...args: unknown[]) => liveRecorderStopMock(...args);
    pause = vi.fn();
    resume = vi.fn();
  },
}));

vi.mock("@/api/client", () => ({
  ApiError: class ApiError extends Error {
    constructor(public status: number, message: string, public body?: unknown) { super(message); }
  },
  rateLimitDetails: (error: { status?: number; body?: unknown }) => error?.status === 429 ? error.body : null,
  apiErrorDetails: (error: { body?: unknown }) =>
    typeof error?.body === "object" && error.body !== null ? error.body : null,
  api: {
    vocabulary: {
      topics: (...args: unknown[]) => topicsMock(...args),
      topic: (...args: unknown[]) => vocabularyTopicMock(...args),
    },
    speaking: {
      start: (...args: unknown[]) => startMock(...args),
      liveCapabilities: (...args: unknown[]) => liveCapabilitiesMock(...args),
      ideaCards: (...args: unknown[]) => ideaCardsMock(...args),
      freeTalkTopics: vi.fn().mockResolvedValue([]),
      roleplayScenarios: vi.fn().mockResolvedValue([{
        code: "airport_check_in",
        englishTitle: "Airport check-in",
        level: CefrLevel.A2,
        imageId: "00000000-0000-0000-0000-000000000001",
      }]),
      roleplayStart: (...args: unknown[]) => roleplayStartMock(...args),
      roleplayEvaluate: (...args: unknown[]) => roleplayEvaluateMock(...args),
      utteranceStream: (...args: unknown[]) => utteranceStreamMock(...args),
    },
    translate: (...args: unknown[]) => translateMock(...args),
    auth: { setPreferredName: (...args: unknown[]) => setPreferredNameMock(...args) },
    analytics: { track: (...args: unknown[]) => analyticsTrackMock(...args) },
  },
}));

vi.mock("@/app/auth", () => ({
  useAuth: () => ({
    user: { preferredName: "Javohir" },
    applyUser: applyUserMock,
  }),
}));

vi.mock("@/app/session", () => ({
  getLearnerId: () => "learner-1",
  getStoredLevel: () => CefrLevel.A2,
}));
vi.mock("@/components/TopicImage", () => ({ TopicImage: ({ title }: { title: string }) => <div>{title} rasmi</div> }));
vi.mock("@/components/ui/ModulePageLoader", () => ({ ModulePageLoader: () => <div>Yuklanmoqda</div> }));
vi.mock("@/components/PaywallProvider", () => ({ usePaywall: () => ({ open: vi.fn() }) }));
vi.mock("@/components/game/EnergyProvider", () => ({
  useEnergy: () => ({
    energy: { current: 5, maximum: 5, nextRefillAt: null, fullRefillAt: null, outcome: 0 },
    openEnergyModal: vi.fn(),
  }),
}));
vi.mock("@/components/TutorNameModal", () => ({ TutorNameModal: () => null }));
vi.mock("@/lib/audio", () => ({
  blobToWav16kMono: vi.fn().mockResolvedValue(new Uint8Array([1, 2, 3])),
  bytesToBase64: vi.fn().mockReturnValue("recorded-audio"),
}));
Object.defineProperty(window, "matchMedia", {
  writable: true,
  value: vi.fn().mockImplementation(() => ({
    matches: false,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
  })),
});

afterEach(() => {
  cleanup();
  topicsMock.mockClear();
  vocabularyTopicMock.mockClear();
  startMock.mockReset();
  ideaCardsMock.mockClear();
  roleplayStartMock.mockClear();
  roleplayEvaluateMock.mockReset();
  utteranceStreamMock.mockReset();
  translateMock.mockReset();
  analyticsTrackMock.mockClear();
  setPreferredNameMock.mockClear();
  applyUserMock.mockClear();
  liveCapabilitiesMock.mockReset();
  liveCapabilitiesMock.mockResolvedValue({ enabled: false });
  liveHubJoinMock.mockReset();
  liveHubJoinMock.mockResolvedValue(undefined);
  liveHubStopMock.mockReset();
  liveHubStopMock.mockResolvedValue(undefined);
  liveRecorderStartMock.mockReset();
  liveRecorderStartMock.mockResolvedValue(undefined);
  liveRecorderStopMock.mockReset();
  liveRecorderStopMock.mockResolvedValue(undefined);
  sessionStorage.clear();
  vi.useRealTimers();
  vi.unstubAllGlobals();
  startMock.mockResolvedValue({
    sessionId: "session-1",
    tutorText: "Tell me about your family.",
    tutorAudioBase64: "",
    visemes: [],
    visemeAnimation: null,
    topicProgress: null,
    isNaturalVoice: false,
    wordTimings: [],
  });
  translateMock.mockResolvedValue({
    text: "Tell me about your family.",
    translation: "Oilangiz haqida gapirib bering.",
  });
  setPreferredNameMock.mockResolvedValue({ preferredName: "Javohir" });
  roleplayStartMock.mockResolvedValue({
    sessionId: "roleplay-session-1",
    scenarioCode: "airport_check_in",
    tutorText: "May I see your passport?",
    tutorAudioBase64: "",
    visemes: [],
    visemeAnimation: null,
    isNaturalVoice: false,
  });
});

beforeEach(() => {
  vi.unstubAllGlobals();
  const microphoneTrack = {
    stop: vi.fn(),
    readyState: "live",
    addEventListener: vi.fn(),
  };
  Object.defineProperty(navigator, "mediaDevices", {
    configurable: true,
    value: {
      getUserMedia: vi.fn().mockResolvedValue({
        getTracks: () => [microphoneTrack],
        getAudioTracks: () => [microphoneTrack],
      } as unknown as MediaStream),
    },
  });
  startMock.mockResolvedValue({
    sessionId: "session-1",
    tutorText: "Tell me about your family.",
    tutorAudioBase64: "",
    visemes: [],
    visemeAnimation: null,
    topicProgress: null,
    isNaturalVoice: false,
    wordTimings: [],
  });
});

class DefaultRecorderStub {
  state = "inactive";
  mimeType = "audio/webm";
  ondataavailable: ((event: BlobEvent) => void) | null = null;
  onstop: (() => void) | null = null;
  start() { this.state = "recording"; }
  stop() { this.state = "inactive"; this.onstop?.(); }
}

describe("SpeakingPage", () => {
  async function waitForSessionReady() {
    await waitFor(() => {
      expect(
        screen.queryByTestId("speaking-preparation") ??
        screen.queryByTestId("speaking-lesson-frame"),
      ).toBeTruthy();
    });
    const grantButton = screen.queryByRole("button", { name: /mikrofonga ruxsat berish/i });
    if (grantButton) {
      fireEvent.click(grantButton);
    }
    return screen.findByRole("button", { name: /tugmani bosing va gapiring/i });
  }

  async function startTopicFromPreparation() {
    expect(await screen.findByTestId("speaking-preparation")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /mikrofonga ruxsat berish/i }));
  }

  function renderWithSpeakingCatalog(initialEntry: string) {
    return render(
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/home" element={<div data-testid="home-page">Home</div>} />
          <Route path="/app/speaking" element={<div data-testid="speaking-catalog">Speaking catalog</div>} />
          <Route path="/app/speaking/free" element={<SpeakingPage />} />
          <Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} />
          <Route path="/app/speaking/role-talk" element={<div data-testid="roleplay-catalog">Roleplay catalog</div>} />
          <Route path="/app/speaking/role-talk/:scenarioCode" element={<SpeakingPage />} />
        </Routes>
      </MemoryRouter>,
    );
  }

  it("shows the Pen preparation screen before it starts a topic conversation", async () => {
    renderWithSpeakingCatalog("/app/speaking/topic/family");

    expect(await screen.findByTestId("speaking-preparation")).toBeTruthy();
    expect(screen.getByText("My Mother")).toBeTruthy();
    expect(screen.getByText("She has always…")).toBeTruthy();
    expect(screen.getByRole("button", { name: /mikrofonga ruxsat berish/i })).toBeTruthy();
    expect(startMock).not.toHaveBeenCalled();
  });

  it("renders Pen 48 section progress, topic status, and recording controls", async () => {
    startMock.mockResolvedValueOnce({
      sessionId: "session-1",
      tutorText: "Tell me about your family.",
      tutorAudioBase64: "",
      visemes: [],
      visemeAnimation: null,
      topicProgress: { spokenSeconds: 75, goalSeconds: 300, learned: false, justLearned: false },
      isNaturalVoice: false,
    });

    render(
      <MemoryRouter initialEntries={[{
        pathname: "/app/speaking/topic/family",
        state: { topicTitle: "My Family" },
      }]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    expect(screen.getByTestId("speaking-lesson-frame").classList.contains("sp17--lesson-frame")).toBe(true);
    await waitFor(() => expect(screen.getByTitle("My Family")).toBeTruthy());
    expect(screen.getByRole("progressbar", { name: /dars bosqichlari/i }).getAttribute("aria-valuetext")).toBe("2 / 3 · Suhbat");
    expect(screen.getByText("Tayyorgarlik")).toBeTruthy();
    expect(screen.getByText("Natija")).toBeTruthy();
    expect(screen.getByRole("button", { name: /mavzularga qaytish/i })).toBeTruthy();
    expect(screen.getByRole("button", { name: /^yopish$/i })).toBeTruthy();
    expect(screen.getByRole("button", { name: /tugmani bosing va gapiring/i })).toBeTruthy();
  });

  it("starts a vocabulary-topic conversation without loading the full vocabulary lesson", async () => {
    vocabularyTopicMock.mockImplementationOnce(() => new Promise(() => undefined));

    renderWithSpeakingCatalog("/app/speaking/topic/family");

    expect(await waitForSessionReady()).toBeTruthy();
    expect(startMock).toHaveBeenCalledWith("learner-1", CefrLevel.A2, undefined, "family");
    expect(vocabularyTopicMock).not.toHaveBeenCalled();
  });

  it("uses the Pen 48 transcript, replay, translation, and recording dock", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    const transcript = screen.getByLabelText("Jonli suhbat matni");
    expect(transcript.classList.contains("sp17__live-transcript-panel")).toBe(true);
    expect(screen.getByText(/AI matni yonidagi ovoz tugmasi/i)).toBeTruthy();
    expect(screen.getByRole("button", { name: /tutor xabarini qayta tinglash/i })).toBeTruthy();
    expect(document.querySelector(".sp17__recording-dock")).toBeTruthy();
    expect(await screen.findByRole("button", { name: /tarjimani ko‘rish/i })).toBeTruthy();
    expect(document.querySelector(".sp17__voice-object")).toBeNull();
    expect(document.querySelector(".sp17__bubble-reaction")).toBeNull();
  });

  it("keeps the same live template for roleplay while retaining roleplay mode", async () => {
    renderWithSpeakingCatalog("/app/speaking/role-talk/airport_check_in");

    await waitForSessionReady();
    const frame = screen.getByTestId("speaking-lesson-frame");
    expect(frame.getAttribute("data-speaking-mode")).toBe("roleplay");
    expect(screen.getByLabelText("Jonli suhbat matni")).toBeTruthy();
    expect(screen.getByRole("progressbar", { name: /dars bosqichlari/i })).toBeTruthy();
    expect(document.querySelector(".sp17__recording-dock")).toBeTruthy();
  });

  it.each([
    ["/app/speaking/free", "conversation"],
    ["/app/speaking/topic/family", "conversation"],
    ["/app/speaking/role-talk/airport_check_in", "roleplay"],
  ])("keeps the Pen 48 layout across %s", async (route, mode) => {
    renderWithSpeakingCatalog(route);

    await waitForSessionReady();
    const frame = screen.getByTestId("speaking-lesson-frame");
    expect(frame.getAttribute("data-speaking-mode")).toBe(mode);
    expect(frame.querySelector(".sp17__pen-live-shell")).toBeTruthy();
    expect(frame.querySelector(".speaking-progress")).toBeTruthy();
    expect(frame.querySelector(".sp17__live-transcript-panel")).toBeTruthy();
    expect(frame.querySelector(".sp17__recording-dock")).toBeTruthy();
  });

  it("uses the same three-stage Pen progress for an open conversation", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/free"]}>
        <Routes><Route path="/app/speaking/free" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByTestId("speaking-lesson-frame")).toBeTruthy();
    expect(screen.getByRole("progressbar", { name: /dars bosqichlari/i }).getAttribute("aria-valuetext")).toBe("2 / 3 · Suhbat");
    expect(screen.queryByLabelText(/0 ta gapirish navbati/i)).toBeNull();
    expect(screen.getByText("2 / 3")).toBeTruthy();
  });

  it("finishes a slow conversation start when React StrictMode replays effects", async () => {
    let resolveStart!: (value: Awaited<ReturnType<typeof startMock>>) => void;
    startMock.mockReturnValueOnce(new Promise((resolve) => { resolveStart = resolve; }));
    render(
      <StrictMode>
        <MemoryRouter initialEntries={["/app/speaking/free"]}>
          <Routes><Route path="/app/speaking/free" element={<SpeakingPage />} /></Routes>
        </MemoryRouter>
      </StrictMode>,
    );

    expect(await screen.findByRole("status", { name: "Tayyorlanmoqda..." })).toBeTruthy();
    resolveStart({
      sessionId: "slow-session",
      tutorText: "Hello! What would you like to talk about today?",
      tutorAudioBase64: "",
      visemes: [],
      visemeAnimation: null,
      topicProgress: null,
      isNaturalVoice: false,
      wordTimings: [],
    });

    expect(await waitForSessionReady()).toBeTruthy();
    expect(screen.queryByRole("status", { name: "Tayyorlanmoqda..." })).toBeNull();
  });

  it("closes a speaking session immediately and returns to the speaking catalog", async () => {
    renderWithSpeakingCatalog("/app/speaking/free");

    await waitForSessionReady();
    expect(sessionStorage.getItem("englishai.speaking.session")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /^yopish$/i }));

    expect(await screen.findByTestId("speaking-catalog")).toBeTruthy();
    expect(sessionStorage.getItem("englishai.speaking.session")).toBeNull();
  });

  it("closes roleplay by evaluating it and shows the result", async () => {
    roleplayEvaluateMock.mockResolvedValueOnce({
      evaluable: true,
      scenarioCode: "airport_check_in",
      overallScore: 82,
      taskCompletion: 84,
      fluency: 80,
      grammar: 81,
      appropriateness: 83,
      band: PronunciationBand.Good,
      strongestDimension: "task_completion",
      weakestDimension: "fluency",
      summaryUz: "Vazifani yaxshi bajardingiz.",
      strengthUz: "Maqsadga erishdingiz.",
      tipUz: "Ravonlikni mashq qiling.",
    });
    renderWithSpeakingCatalog("/app/speaking/role-talk/airport_check_in");

    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /^yopish$/i }));

    expect(await screen.findByText("82")).toBeTruthy();
    expect(screen.getByText("Vazifani yaxshi bajardingiz.")).toBeTruthy();
    expect(roleplayEvaluateMock).toHaveBeenCalledWith("roleplay-session-1");
    expect(sessionStorage.getItem("englishai.speaking.session")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: "Bosh sahifaga qaytish" }));
    expect(await screen.findByTestId("home-page")).toBeTruthy();
  });

  it("shows an AI availability message for a server-side start failure", async () => {
    const { ApiError } = await import("@/api/client");
    startMock.mockRejectedValueOnce(new ApiError(503, "temporarily unavailable", {
      code: "timeout",
      retryable: true,
    }));

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await startTopicFromPreparation();
    expect(await screen.findByText(/AI tutor vaqtincha band/i)).toBeTruthy();
    expect(screen.queryByText(/Internet aloqangiz/i)).toBeNull();
  });

  it("shows the retry delay for a rate-limited start", async () => {
    const { ApiError } = await import("@/api/client");
    startMock.mockRejectedValueOnce(new ApiError(429, "slow down", {
      code: "rate_limited",
      message: "Slow down",
      retryAfterSeconds: 30,
      correlationId: "corr-1",
    }));

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await startTopicFromPreparation();
    expect(await screen.findByText(/Juda ko'p urinish bo'ldi.*30 soniyadan keyin/i)).toBeTruthy();
    expect(startMock).toHaveBeenCalledTimes(1);
  });

  it("prefetches and reveals tutor translations", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    expect(await screen.findByRole("button", { name: /tarjimani ko‘rish/i })).toBeTruthy();
    expect(translateMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: /tarjimani ko‘rish/i }));
    const translation = await screen.findByText("Oilangiz haqida gapirib bering.");
    expect(translation.classList.contains("sp17__translation-text--ready")).toBe(true);
    expect(translateMock).toHaveBeenCalledWith(
      "Tell me about your family.",
      CefrLevel.A2,
      expect.objectContaining({ speaker: "tutor" }),
    );
  });

  it("silently retries when AI translation temporarily fails", async () => {
    translateMock
      .mockRejectedValueOnce(new Error("offline"))
      .mockResolvedValueOnce({ text: "Tell me about your family.", translation: "Oilangiz haqida gapirib bering." });
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    fireEvent.click(await screen.findByRole("button", { name: /tarjimani ko‘rish/i }));

    expect(await screen.findByText("Oilangiz haqida gapirib bering.", {}, { timeout: 2_000 })).toBeTruthy();
    expect(translateMock).toHaveBeenCalledTimes(2);
    expect(screen.queryByText(/xizmati javob bermadi|limit/i)).toBeNull();
  });

  it("silently retries empty AI translations", async () => {
    translateMock
      .mockResolvedValueOnce({ text: "Tell me about your family.", translation: null })
      .mockResolvedValueOnce({ text: "Tell me about your family.", translation: "Oilangiz haqida gapirib bering." });
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    fireEvent.click(await screen.findByRole("button", { name: /tarjimani ko‘rish/i }));

    expect(await screen.findByText("Oilangiz haqida gapirib bering.", {}, { timeout: 2_000 })).toBeTruthy();
    expect(translateMock).toHaveBeenCalledTimes(2);
    expect(screen.queryByText(/Tarjima olinmadi/i)).toBeNull();
  });

  it("prefills the saved preferred name when the name input is reopened", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /AI sizni qanday chaqirsin/i }));

    expect((screen.getByRole("textbox") as HTMLInputElement).value).toBe("Javohir");
  });

  it("never auto-opens the name prompt from a tutor event — only the learner's button does", async () => {
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock.mockImplementationOnce(async () => undefined);

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    const recordingButton = screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    );
    fireEvent.click(recordingButton as HTMLButtonElement);
    await waitFor(() => expect(utteranceStreamMock).toHaveBeenCalledTimes(1));
    await act(async () => {
      await utteranceStreamMock.mock.calls[0][2].onRecognized({ text: "Hello there." });
      // The tutor asks for a name (namePrompt: true) — the UI must ignore it and NOT surface the input.
      await utteranceStreamMock.mock.calls[0][2].onTutor({
        text: "What should I call you?",
        sessionLimitReached: false,
        topicProgress: null,
        completion: null,
        namePrompt: true,
      });
    });

    expect(await screen.findByText(/What should I call you\?/i)).toBeTruthy();
    // No name input appeared on its own; the learner's explicit button is still the only way in.
    expect(screen.queryByRole("textbox")).toBeNull();
    expect(screen.getByRole("button", { name: /AI sizni qanday chaqirsin/i })).toBeTruthy();
  });

  it("requests microphone permission only after the learner explicitly starts the preparation step", async () => {
    const getUserMedia = vi.fn().mockRejectedValue(new DOMException("blocked", "NotAllowedError"));
    Object.defineProperty(navigator, "mediaDevices", { configurable: true, value: { getUserMedia } });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    expect(getUserMedia).not.toHaveBeenCalled();
    expect(screen.queryByText(/Mikrofonga ruxsat berilmadi/i)).toBeNull();
    await startTopicFromPreparation();

    await waitFor(() => expect(getUserMedia).toHaveBeenCalled());
    expect(await screen.findByText(/Mikrofonga ruxsat berilmadi/i)).toBeTruthy();
    expect(screen.queryByRole("dialog", { name: /mikrofon/i })).toBeNull();
    expect(screen.queryByText(/Mikrofonni ishga tushirib bo‘lmadi/i)).toBeNull();
  });

  it("keeps the preparation screen visible when the microphone device is unavailable", async () => {
    const getUserMedia = vi.fn().mockRejectedValue(new DOMException("device busy", "NotReadableError"));
    Object.defineProperty(navigator, "mediaDevices", { configurable: true, value: { getUserMedia } });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await startTopicFromPreparation();
    await waitFor(() => expect(getUserMedia).toHaveBeenCalled());

    expect(screen.queryByText(/Mikrofonga ruxsat berilmadi/i)).toBeNull();
    expect(await screen.findByText(/Mikrofonni ishga tushirib bo‘lmadi/i)).toBeTruthy();
    expect(screen.getByTestId("speaking-preparation")).toBeTruthy();
  });

  it("falls through to manual recording on the same press when live conversation fails to start", async () => {
    // Live capabilities are on, so the mic button's first press tries the live path; the hub fails.
    liveCapabilitiesMock.mockResolvedValue({ enabled: true });
    liveHubJoinMock.mockRejectedValue(new Error("hub unavailable"));
    vi.stubGlobal("MediaRecorder", DefaultRecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    const getUserMedia = vi.fn().mockResolvedValue({
      getTracks: () => [audioTrack],
      getAudioTracks: () => [audioTrack],
    } as unknown as MediaStream);
    Object.defineProperty(navigator, "mediaDevices", { configurable: true, value: { getUserMedia } });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    // Let the capabilities response flip the button into live mode before the single press.
    await waitFor(() => expect(liveCapabilitiesMock).toHaveBeenCalled());
    await act(async () => { await Promise.resolve(); await Promise.resolve(); });

    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));

    // The press attempted live (hub.joinSession) AND, when that failed, fell straight through to the
    // manual microphone — no second tap required.
    await waitFor(() => expect(liveHubJoinMock).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(getUserMedia).toHaveBeenCalledTimes(2));
    expect(await screen.findByRole("button", { name: /to'xtatish/i })).toBeTruthy();
  });

  it("attaches pronunciation after the tutor event and shows practice words", async () => {
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock.mockImplementationOnce(async () => undefined);

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    const transcript = screen.getByTestId("speaking-transcript");
    let transcriptScrollTop = 0;
    Object.defineProperties(transcript, {
      scrollHeight: { configurable: true, get: () => 1_200 },
      clientHeight: { configurable: true, get: () => 300 },
      scrollTop: {
        configurable: true,
        get: () => transcriptScrollTop,
        set: (value: number) => { transcriptScrollTop = value; },
      },
    });
    vi.stubGlobal("requestAnimationFrame", (callback: FrameRequestCallback) => {
      callback(performance.now());
      return 1;
    });
    vi.stubGlobal("cancelAnimationFrame", vi.fn());
    // The learner is at the bottom of the transcript (default follow state), so learner feedback and
    // the following AI message expanding the card must keep the newest content pinned into view.
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    const recordingButton = screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    );
    expect(recordingButton).toBeTruthy();
    fireEvent.click(recordingButton as HTMLButtonElement);
    await waitFor(() => expect(utteranceStreamMock).toHaveBeenCalledTimes(1));
    await act(async () => {
      await utteranceStreamMock.mock.calls[0][2].onRecognized({ text: "She always wakes up at five." });
      await utteranceStreamMock.mock.calls[0][2].onTutor({
        text: "Five is quite early.",
        sessionLimitReached: false,
        topicProgress: null,
        completion: null,
        namePrompt: false,
      });
      await utteranceStreamMock.mock.calls[0][2].onPronunciation({
        pronunciation: {
          overallScore: 67,
          accuracyScore: 65,
          fluencyScore: 70,
          completenessScore: 68,
          band: PronunciationBand.NeedsImprovement,
          isAuthentic: true,
          words: [{
            word: "always",
            spokenForm: "olways",
            accuracyScore: 42,
            errorType: PronunciationErrorType.Mispronunciation,
            needsPractice: true,
            phonemes: [],
          }],
        },
        feedbackUz: "always so‘zini yana mashq qiling.",
        focusWord: "always",
      });
    });

    expect(await screen.findByLabelText(/talaffuz/i)).toBeTruthy();
    expect(screen.getByText("67/100")).toBeTruthy();
    expect(screen.getByRole("button", { name: /always.*42/i })).toBeTruthy();
    expect(screen.getByText(/always so‘zini yana mashq qiling/i)).toBeTruthy();
    expect(transcriptScrollTop).toBe(1_200);
  });

  it("keeps the learner's scroll position when they have scrolled up to reread earlier turns", async () => {
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock.mockImplementationOnce(async () => undefined);
    // Run the auto-follow rAF pins synchronously from the first render so no stale mount pin lands
    // after the learner has scrolled away below.
    vi.stubGlobal("requestAnimationFrame", (callback: FrameRequestCallback) => {
      callback(performance.now());
      return 1;
    });
    vi.stubGlobal("cancelAnimationFrame", vi.fn());

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    const transcript = screen.getByTestId("speaking-transcript");
    let transcriptScrollTop = 300;
    Object.defineProperties(transcript, {
      scrollHeight: { configurable: true, get: () => 1_200 },
      clientHeight: { configurable: true, get: () => 300 },
      scrollTop: {
        configurable: true,
        get: () => transcriptScrollTop,
        set: (value: number) => { transcriptScrollTop = value; },
      },
    });
    // The learner scrolls far above the bottom (distance 1200 - 200 - 300 = 700 > follow threshold),
    // so new turns must not yank the view back down while they are rereading an earlier message.
    transcriptScrollTop = 200;
    fireEvent.scroll(transcript);
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    const recordingButton = screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    );
    fireEvent.click(recordingButton as HTMLButtonElement);
    await waitFor(() => expect(utteranceStreamMock).toHaveBeenCalledTimes(1));
    await act(async () => {
      await utteranceStreamMock.mock.calls[0][2].onRecognized({ text: "She always wakes up at five." });
      await utteranceStreamMock.mock.calls[0][2].onTutor({
        text: "Five is quite early.",
        sessionLimitReached: false,
        topicProgress: null,
        completion: null,
        namePrompt: false,
      });
    });

    expect(await screen.findByText(/Five is quite early\./i)).toBeTruthy();
    // Still where the learner left it — no forced jump to the bottom.
    expect(transcriptScrollTop).toBe(200);
  });

  it("does not re-pin the transcript when an existing turn mutates (tutor word highlight)", async () => {
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock.mockImplementationOnce(async () => undefined);
    vi.stubGlobal("requestAnimationFrame", (callback: FrameRequestCallback) => {
      callback(performance.now());
      return 1;
    });
    vi.stubGlobal("cancelAnimationFrame", vi.fn());

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    const transcript = screen.getByTestId("speaking-transcript");
    let transcriptScrollTop = 0;
    Object.defineProperties(transcript, {
      scrollHeight: { configurable: true, get: () => 1_200 },
      clientHeight: { configurable: true, get: () => 300 },
      scrollTop: {
        configurable: true,
        get: () => transcriptScrollTop,
        set: (value: number) => { transcriptScrollTop = value; },
      },
    });

    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    const recordingButton = screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    );
    fireEvent.click(recordingButton as HTMLButtonElement);
    await waitFor(() => expect(utteranceStreamMock).toHaveBeenCalledTimes(1));
    await act(async () => {
      await utteranceStreamMock.mock.calls[0][2].onRecognized({ text: "She always wakes up at five." });
      await utteranceStreamMock.mock.calls[0][2].onTutor({
        text: "Five is quite early.",
        sessionLimitReached: false,
        topicProgress: null,
        completion: null,
        namePrompt: false,
      });
    });

    // The learner nudges up a little — still within the follow threshold (distance 50 < 96), so the
    // transcript would legitimately follow a *new* message, but a same-length turn mutation such as a
    // karaoke word highlight must not fire the auto-scroll and drag them back down.
    transcriptScrollTop = 850;
    fireEvent.scroll(transcript);
    await act(async () => {
      await utteranceStreamMock.mock.calls[0][2].onPronunciation({
        pronunciation: {
          overallScore: 80,
          accuracyScore: 82,
          fluencyScore: 78,
          completenessScore: 80,
          band: PronunciationBand.Good,
          isAuthentic: true,
          words: [{
            word: "always",
            spokenForm: "always",
            accuracyScore: 88,
            errorType: PronunciationErrorType.None,
            needsPractice: false,
            phonemes: [],
          }],
        },
        feedbackUz: "Yaxshi.",
        focusWord: "always",
      });
    });

    // A mutation that does not add a message leaves the scroll exactly where the learner put it.
    expect(transcriptScrollTop).toBe(850);
  });

  it("does not show pronunciation errors when a perfect result has no practice words", async () => {
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock.mockImplementationOnce(async (_sessionId, _audio, handlers) => {
      handlers.onRecognized({ text: "Hello." });
      handlers.onTutor({
        text: "Hello!",
        sessionLimitReached: false,
        topicProgress: null,
        completion: null,
        namePrompt: false,
      });
      handlers.onPronunciation({
        pronunciation: {
          overallScore: 100,
          accuracyScore: 100,
          fluencyScore: 100,
          completenessScore: 100,
          band: PronunciationBand.Good,
          isAuthentic: true,
          words: [{
            word: "hello",
            spokenForm: null,
            accuracyScore: 100,
            errorType: PronunciationErrorType.None,
            needsPractice: false,
            phonemes: [],
          }],
        },
        feedbackUz: "Talaffuz yaxshi.",
        focusWord: null,
      });
      handlers.onAudio({
        audioBase64: "",
        visemes: [],
        visemeAnimation: null,
        isNaturalVoice: false,
        wordTimings: [],
      });
    });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    fireEvent.click(screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    ) as HTMLButtonElement);

    expect(await screen.findByText("100/100")).toBeTruthy();
    expect(screen.queryByText(/xato qilingan so'zlar/i)).toBeNull();
    expect(screen.queryByRole("button", { name: /hello.*100/i })).toBeNull();
  });

  it("highlights the tutor word that matches the current audio time", async () => {
    const audio = {
      currentTime: 0,
      onended: null as (() => void) | null,
      onerror: null as (() => void) | null,
      pause: vi.fn(),
      play: vi.fn().mockResolvedValue(undefined),
    };
    vi.stubGlobal("Audio", vi.fn(() => audio));
    startMock.mockResolvedValueOnce({
      sessionId: "session-1",
      tutorText: "Hello there",
      tutorAudioBase64: "UklGRg==",
      visemes: [],
      visemeAnimation: null,
      topicProgress: null,
      isNaturalVoice: true,
      wordTimings: [
        { text: "Hello", textOffset: 0, wordLength: 5, audioOffsetMs: 0, durationMs: 300 },
        { text: "there", textOffset: 6, wordLength: 5, audioOffsetMs: 300, durationMs: 300 },
      ],
    });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await startTopicFromPreparation();
    expect(await screen.findByText("Hello", { selector: "mark" })).toBeTruthy();
    act(() => { audio.currentTime = 0.35; });
    expect(await screen.findByText("there", { selector: "mark" })).toBeTruthy();
  });

  it("keeps vocabulary topic cards off the hub and opens the topic catalog", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking"]}>
        <Routes>
          <Route path="/app/speaking" element={<SpeakingPage />} />
          <Route path="/app/speaking/topics" element={<div data-testid="topics-catalog">Topics catalog</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(await screen.findByRole("button", { name: /mavzuli suhbat/i })).toBeTruthy();
    expect(screen.queryByLabelText("Mavzu progressi 33%")).toBeNull();

    fireEvent.click(screen.getByRole("button", { name: /mavzuli suhbat/i }));

    expect(await screen.findByTestId("topics-catalog")).toBeTruthy();
  });

  it("keeps live session actions and controls outside the transcript scroller", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes>
          <Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    const header = screen.getByTestId("speaking-fixed-header");
    const transcript = screen.getByTestId("speaking-transcript");
    const controls = document.querySelector(".sp17__recording-dock") as HTMLElement;

    expect(transcript.contains(header)).toBe(false);
    expect(transcript.contains(controls)).toBe(false);
    expect(header.closest(".sp17__pen-live-shell")?.contains(controls)).toBe(true);
    expect(transcript.closest(".sp17__pen-live-shell")).toBeTruthy();
    expect(controls.classList.contains("sp17__recording-dock")).toBe(true);
    expect(screen.queryByLabelText(/0 ta gapirish navbati/i)).toBeNull();
    expect(screen.getByRole("button", { name: /^yopish$/i })).toBeTruthy();
    const recordButton = screen.getByRole("button", { name: /tugmani bosing va gapiring/i });
    expect(recordButton.textContent).toContain("Tugmani bosing va gapiring");
    expect(recordButton.querySelector("svg")).toBeTruthy();
    expect(document.documentElement.classList.contains("speaking-session-page-active")).toBe(true);
  });

  it("shows AI speaking state inside the microphone control", async () => {
    class SpeechSynthesisUtteranceStub {
      lang = "";
      rate = 1;
      voice: SpeechSynthesisVoice | null = null;
      onend: (() => void) | null = null;
      onerror: (() => void) | null = null;
      constructor(public text: string) {}
    }
    const originalSpeechSynthesis = window.speechSynthesis;
    vi.stubGlobal("SpeechSynthesisUtterance", SpeechSynthesisUtteranceStub);
    Object.defineProperty(window, "speechSynthesis", {
      configurable: true,
      value: {
        cancel: vi.fn(),
        getVoices: vi.fn().mockReturnValue([]),
        speak: vi.fn(),
        resume: vi.fn(),
      },
    });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await startTopicFromPreparation();
    const speakingControl = await screen.findByRole("button", { name: /AI gapirmoqda/i });
    expect(speakingControl.textContent).not.toContain("AI gapirmoqda");
    expect(speakingControl.classList.contains("is-tutor-speaking")).toBe(true);
    expect(screen.queryByText(/AI gapirmoqda/i, { selector: ".sp17__activity" })).toBeNull();
    expect(screen.queryByText("AI bilan jonli suhbat")).toBeNull();
    Object.defineProperty(window, "speechSynthesis", {
      configurable: true,
      value: originalSpeechSynthesis,
    });
    vi.unstubAllGlobals();
  });

  it("keeps the response loader inside the microphone control", async () => {
    let resolveUtterance: (() => void) | undefined;
    utteranceStreamMock.mockReturnValueOnce(new Promise<void>((resolve) => { resolveUtterance = resolve; }));
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    const recordingControl = await screen.findByRole("button", { name: /to'xtatish/i });
    fireEvent.click(recordingControl);

    const loadingControl = await screen.findByRole("button", { name: /AI tutor javobi kutilmoqda/i });
    expect(loadingControl.textContent).toContain("AI tutor javobi kutilmoqda");
    expect(loadingControl.classList.contains("is-loading")).toBe(true);
    expect(loadingControl.querySelector(".sp17__record-loader")).toBeTruthy();
    expect(loadingControl.querySelectorAll("svg")).toHaveLength(1);
    expect(document.querySelector(".sp17__activity--thinking")).toBeNull();
    resolveUtterance?.();
  });

  it("automatically continues with the best uncertain transcript without showing correction inputs", async () => {
    class RecorderStub extends DefaultRecorderStub {
      override stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock
      .mockImplementationOnce(async (_sessionId, _audio, handlers) => {
        handlers.onTranscriptConfirmationRequired({
          suggestedText: "I prefer tea in the morning.",
          alternatives: [
            { text: "I prefer tea in the morning.", confidence: 0.71 },
            { text: "I prefer tea in Jammu.", confidence: 0.70 },
          ],
        });
      })
      .mockImplementationOnce(async (_sessionId, _audio, handlers) => {
        handlers.onRecognized({ text: "I prefer tea in the morning." });
        handlers.onTutor({
          text: "Tea is a nice morning drink.",
          sessionLimitReached: false,
          topicProgress: null,
          completion: null,
          namePrompt: false,
        });
      });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    fireEvent.click(await screen.findByRole("button", { name: /to'xtatish/i }));

    expect(await screen.findByText("Tea is a nice morning drink.")).toBeTruthy();
    expect(screen.queryByRole("region", { name: /gapingizni tekshiring/i })).toBeNull();
    expect(screen.queryByRole("textbox", { name: /men aytgan gap/i })).toBeNull();
    expect(utteranceStreamMock).toHaveBeenNthCalledWith(
      2,
      "session-1",
      "recorded-audio",
      expect.any(Object),
      {
        transcript: "I prefer tea in the morning.",
        outcome: "candidate_selected",
      },
    );
  });

  it("shows the learned celebration in a fixed modal and keeps the conversation active", async () => {
    class RecorderStub extends DefaultRecorderStub {
      stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["audio"], { type: "audio/webm" }) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: {
        getUserMedia: vi.fn().mockResolvedValue({
          getTracks: () => [audioTrack],
          getAudioTracks: () => [audioTrack],
        } as unknown as MediaStream),
      },
    });
    utteranceStreamMock.mockImplementationOnce(async (_sessionId, _audio, handlers) => {
      handlers.onProgress({
        text: "",
        sessionLimitReached: false,
        namePrompt: false,
        topicProgress: {
          spokenSeconds: 300,
          goalSeconds: 300,
          learned: true,
          justLearned: true,
        },
        completion: null,
      });
    });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    const recordingButton = screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    );
    expect(recordingButton).toBeTruthy();
    expect(recordingButton?.textContent).toContain("Mikrofonni o‘chirish");
    expect(recordingButton?.querySelector("svg")).toBeTruthy();
    fireEvent.click(recordingButton as HTMLButtonElement);

    const dialog = await screen.findByRole("dialog", { name: /bu mavzu o'rganildi/i });
    const transcript = screen.getByTestId("speaking-transcript");

    expect(document.body.contains(dialog)).toBe(true);
    expect(transcript.contains(dialog)).toBe(false);
    expect(screen.getByText(/5 daqiqa gaplashing/i)).toBeTruthy();

    const continueButton = Array.from(dialog.querySelectorAll("button")).find(
      (button) => button.textContent?.trim() === "Davom etish",
    );
    expect(continueButton).toBeTruthy();
    fireEvent.click(continueButton as HTMLButtonElement);

    expect(screen.queryByRole("dialog", { name: /bu mavzu o'rganildi/i })).toBeNull();
    expect(screen.getByTestId("speaking-transcript")).toBeTruthy();
  });

  it("loads idea cards only after the learner opens the idea helper", async () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    const transcript = screen.getByTestId("speaking-transcript");
    expect(transcript.querySelector(".sp17__conversation-reserve")).toBeTruthy();
    expect(screen.queryByRole("dialog", { name: /gapirish uchun g'oyalar/i })).toBeNull();
    expect(ideaCardsMock).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole("button", { name: /gapirish g‘oyalarini ko‘rsatish/i }));
    const dialog = await screen.findByRole("dialog", { name: /gapirish uchun g'oyalar/i });
    expect(document.body.contains(dialog)).toBe(true);
    expect(transcript.contains(dialog)).toBe(false);
    expect(ideaCardsMock).toHaveBeenCalledTimes(1);
  });

  it("keeps the chat height reserved while the speaking session connects", async () => {
    let resolveStart: ((value: Awaited<ReturnType<typeof startMock>>) => void) | undefined;
    startMock.mockReturnValueOnce(new Promise((resolve) => { resolveStart = resolve; }));

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );

    await startTopicFromPreparation();
    expect(await screen.findByRole("status", { name: /tayyorlanmoqda/i })).toBeTruthy();
    expect(screen.queryByTestId("speaking-transcript")).toBeNull();
    expect(screen.queryByRole("button", { name: /gapirish g‘oyalarini ko‘rsatish/i })).toBeNull();
    expect(ideaCardsMock).not.toHaveBeenCalled();

    resolveStart?.({
      sessionId: "session-delayed",
      tutorText: "Tell me about your family.",
      tutorAudioBase64: "",
      visemes: [],
      visemeAnimation: null,
      topicProgress: null,
      isNaturalVoice: false,
    });

    expect(await screen.findByRole("button", { name: /tugmani bosing va gapiring/i })).toBeTruthy();
    expect(screen.getByTestId("speaking-transcript")).toBeTruthy();
    expect(screen.queryByRole("dialog", { name: /bir oz tayyorlaning/i })).toBeNull();
    expect(ideaCardsMock).not.toHaveBeenCalled();
  });

  it("blocks recording controls while the learner-triggered microphone request is pending", async () => {
    vi.stubGlobal("MediaRecorder", DefaultRecorderStub);
    let resolveStream: ((stream: MediaStream) => void) | undefined;
    const preparationTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    const preparationStream = {
      getTracks: () => [preparationTrack],
      getAudioTracks: () => [preparationTrack],
    } as unknown as MediaStream;
    const getUserMedia = vi.fn()
      .mockResolvedValueOnce(preparationStream)
      .mockReturnValueOnce(new Promise<MediaStream>((resolve) => { resolveStream = resolve; }));
    Object.defineProperty(navigator, "mediaDevices", { configurable: true, value: { getUserMedia } });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    const record = screen.getByRole("button", { name: /mikrofon tayyorlanmoqda/i });

    expect(getUserMedia).toHaveBeenCalledTimes(2);
    expect(record).toHaveProperty("disabled", true);
    resolveStream?.({ getTracks: () => [] } as unknown as MediaStream);
  });

  it("cleans up an active recorder on unmount without submitting it", async () => {
    const stopTrack = vi.fn();
    const stopRecorder = vi.fn();
    const preparationTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    const preparationStream = {
      getTracks: () => [preparationTrack],
      getAudioTracks: () => [preparationTrack],
    } as unknown as MediaStream;
    const audioTrack = { stop: stopTrack, readyState: "live", addEventListener: vi.fn() };
    const stream = { getTracks: () => [audioTrack], getAudioTracks: () => [audioTrack] } as unknown as MediaStream;
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: { getUserMedia: vi.fn().mockResolvedValueOnce(preparationStream).mockResolvedValueOnce(stream) },
    });
    class RecorderStub {
      state = "inactive";
      stream = stream;
      mimeType = "audio/webm";
      ondataavailable: ((event: BlobEvent) => void) | null = null;
      onstop: (() => void) | null = null;
      start() { this.state = "recording"; }
      stop() { stopRecorder(); this.state = "inactive"; }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);

    const view = render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });

    view.unmount();

    expect(stopRecorder).toHaveBeenCalledTimes(1);
    expect(stopTrack).toHaveBeenCalledTimes(1);
    expect(utteranceStreamMock).not.toHaveBeenCalled();
    vi.unstubAllGlobals();
  });

  it("retries the same recognized turn without duplicating the learner message", async () => {
    sessionStorage.clear();
    const audioTrack = { stop: vi.fn(), readyState: "live", addEventListener: vi.fn() };
    const stream = { getTracks: () => [audioTrack], getAudioTracks: () => [audioTrack] } as unknown as MediaStream;
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: { getUserMedia: vi.fn().mockResolvedValue(stream) },
    });
    class RecorderStub {
      state = "inactive";
      stream = stream;
      mimeType = "audio/webm";
      ondataavailable: ((event: BlobEvent) => void) | null = null;
      onstop: (() => void) | null = null;
      start() { this.state = "recording"; }
      stop() {
        this.state = "inactive";
        this.ondataavailable?.({ data: new Blob(["voice"]) } as BlobEvent);
        this.onstop?.();
      }
    }
    vi.stubGlobal("MediaRecorder", RecorderStub);
    utteranceStreamMock
      .mockImplementationOnce(async (_sessionId, _audio, handlers) => {
        handlers.onRecognized({ text: "She always wakes up at five." });
        handlers.onTutorUnavailable();
      })
      .mockImplementationOnce(async (_sessionId, _audio, handlers) => {
        handlers.onRecognized({ text: "She always wakes up at five." });
        handlers.onTutor({
          text: "Five is quite early. What does she do first?",
          sessionLimitReached: false,
          topicProgress: null,
          completion: null,
          namePrompt: false,
        });
      });

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes><Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} /></Routes>
      </MemoryRouter>,
    );
    await waitForSessionReady();
    fireEvent.click(screen.getByRole("button", { name: /tugmani bosing va gapiring/i }));
    await screen.findByRole("button", { name: /to'xtatish/i });
    const stop = screen.getAllByRole("button", { name: /to'xtatish/i }).find(
      (button) => button.classList.contains("sp17__record-action"),
    );
    expect(stop).toBeTruthy();
    fireEvent.click(stop as HTMLButtonElement);
    await waitFor(() => expect(utteranceStreamMock).toHaveBeenCalledTimes(1));

    expect(await screen.findByText(/AI tutor hozir javob bera olmadi/i)).toBeTruthy();
    expect(screen.getAllByText("She always wakes up at five.")).toHaveLength(1);
    fireEvent.click(screen.getByRole("button", { name: /qayta yuborish/i }));

    expect(await screen.findAllByText("Five is quite early. What does she do first?")).not.toHaveLength(0);
    expect(screen.getAllByText("She always wakes up at five.")).toHaveLength(1);
    expect(utteranceStreamMock).toHaveBeenNthCalledWith(2, "session-1", "recorded-audio", expect.any(Object), undefined);
  });

  it("shows skeleton cards after the idea helper is opened", async () => {
    let resolveIdeas: ((value: { cards: never[] }) => void) | undefined;
    ideaCardsMock.mockReturnValueOnce(new Promise((resolve) => { resolveIdeas = resolve; }));

    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/family"]}>
        <Routes>
          <Route path="/app/speaking/topic/:topicId" element={<SpeakingPage />} />
        </Routes>
      </MemoryRouter>,
    );

    await waitForSessionReady();
    expect(ideaCardsMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: /gapirish g‘oyalarini ko‘rsatish/i }));
    expect(await screen.findByRole("status", { name: /g'oyalar tayyorlanmoqda/i })).toBeTruthy();
    expect(screen.getAllByTestId("speaking-idea-skeleton")).toHaveLength(4);
    expect(screen.queryByText("What do you like most about this topic?")).toBeNull();
    expect(ideaCardsMock).toHaveBeenCalledTimes(1);
    resolveIdeas?.({ cards: [] });
  });
});
