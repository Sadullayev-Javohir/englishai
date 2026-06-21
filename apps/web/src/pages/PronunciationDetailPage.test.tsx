import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { uz } from "@/content/uz";
import { PronunciationDetailPage } from "./PronunciationDetailPage";

const { playRecordedBlob, pronounce, wordDetail, MockApiError } = vi.hoisted(() => ({
  playRecordedBlob: vi.fn((...args: [Blob, HTMLAudioElement, string?]) => {
    void args;
    return "blob:attempt";
  }),
  pronounce: vi.fn(),
  wordDetail: vi.fn(),
  MockApiError: class MockApiError extends Error {
    constructor(public status: number, message: string) {
      super(message);
    }
  },
}));

vi.mock("@/lib/audio", () => ({
  blobToWav16kMono: vi.fn(async () => new Uint8Array([1, 2, 3])),
  bytesToBase64: vi.fn(() => "AQID"),
  playRecordedBlob,
  playWordAudioBase64: vi.fn(() => true),
  speakEnglishWord: vi.fn(),
}));

vi.mock("@/api/client", () => ({
  api: {
    speaking: { wordDetail, practiceAttempt: vi.fn() },
    vocabulary: { pronounce },
  },
  ApiError: MockApiError,
}));

vi.mock("@/components/app/speaking/MicFrequencyBars", () => ({
  MicFrequencyBars: () => null,
}));

class MediaRecorderStub {
  static isTypeSupported() { return true; }
  mimeType = "audio/webm";
  state = "inactive";
  stream: MediaStream;
  ondataavailable: ((event: { data: Blob }) => void) | null = null;
  onstop: (() => void) | null = null;

  constructor(stream: MediaStream) {
    this.stream = stream;
  }

  start() {
    this.state = "recording";
  }

  stop() {
    this.state = "inactive";
    this.ondataavailable?.({ data: new Blob(["voice"], { type: this.mimeType }) });
    this.onstop?.();
  }
}

function renderPage(initialEntry = "/app/speaking/pronunciation/family") {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route path="/app/speaking/pronunciation/:word" element={<PronunciationDetailPage />} />
        <Route path="/home" element={<div>Home page</div>} />
        <Route path="/app/speaking" element={<div>Speaking page</div>} />
        <Route path="/app/speaking/practice-words" element={<div>Practice queue</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  vi.clearAllMocks();
});

describe("PronunciationDetailPage recording playback", () => {
  it("keeps the latest attempt available for user-triggered playback", async () => {
    wordDetail.mockResolvedValue({
      word: "family",
      spokenForm: "family",
      ipa: "/ˈfæməli/",
      audioBase64: null,
      phonemes: [],
      visemes: [],
      mouthPositionUz: "",
      tipUz: "",
    });
    pronounce.mockResolvedValue({
      word: "family",
      recognized: false,
      correct: false,
      isAuthentic: true,
      overallScore: 0,
      accuracyScore: 0,
      errorType: 0,
      phonemes: [],
      feedbackUz: null,
    });

    const stopTrack = vi.fn();
    const stream = { getTracks: () => [{ stop: stopTrack }] } as unknown as MediaStream;
    Object.defineProperty(navigator, "mediaDevices", {
      configurable: true,
      value: { getUserMedia: vi.fn().mockResolvedValue(stream) },
    });
    vi.stubGlobal("MediaRecorder", MediaRecorderStub);
    const audio = {
      pause: vi.fn(),
      play: vi.fn().mockResolvedValue(undefined),
      load: vi.fn(),
      removeAttribute: vi.fn(),
      onplay: null as (() => void) | null,
      onpause: null as (() => void) | null,
      onended: null as (() => void) | null,
      onerror: null as (() => void) | null,
      src: "",
      currentTime: 0,
    };
    vi.stubGlobal("Audio", vi.fn(() => audio));
    vi.stubGlobal("URL", {
      ...URL,
      createObjectURL: vi.fn(() => "blob:attempt"),
      revokeObjectURL: vi.fn(),
    });

    renderPage();

    const recordButton = await screen.findByRole("button", { name: uz.pronunciation.recordStart });
    fireEvent.click(recordButton);
    const stopButton = await screen.findByRole("button", { name: uz.pronunciation.recordStart });
    fireEvent.click(stopButton);

    const playbackButton = await screen.findByRole("button", { name: uz.pronunciation.listenMine });
    fireEvent.click(playbackButton);

    await waitFor(() => expect(playRecordedBlob).toHaveBeenCalledTimes(1));
    act(() => audio.onplay?.());
    expect(playbackButton.querySelector(".lucide-audio-lines")).toBeTruthy();
    act(() => audio.onended?.());
    expect(playbackButton.querySelector(".lucide-play")).toBeTruthy();
    expect(playRecordedBlob.mock.calls[0][0]).toBeInstanceOf(Blob);
    expect(playRecordedBlob.mock.calls[0][1]).toBe(audio);
    expect(pronounce).toHaveBeenCalledWith("family", "AQID");
  });
});

describe("PronunciationDetailPage return navigation", () => {
  it("uses the Pen heading without a duplicate inner-page back control", async () => {
    wordDetail.mockResolvedValue({
      word: "family",
      spokenForm: "family",
      ipa: "ˈfæməli",
      audioBase64: null,
      phonemes: [],
      visemes: [],
    });

    renderPage("/app/speaking/pronunciation/family?practiceWordId=practice-1");

    expect(await screen.findByText("Tinglang. Takrorlang. Ishonch hosil qiling.")).toBeTruthy();
    expect(screen.queryByRole("button", { name: uz.pronunciation.backHome })).toBeNull();
  });

  it("offers a queue return when a practice word has no detail", async () => {
    wordDetail.mockRejectedValue(new MockApiError(404, "Not found"));

    renderPage("/app/speaking/pronunciation/unsupported?practiceWordId=practice-1");

    expect(await screen.findByText(uz.pronunciation.notFoundBody)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: uz.pronunciation.backHome }));
    expect(await screen.findByText("Practice queue")).toBeTruthy();
  });

  it("does not render a duplicate return control outside the queue", async () => {
    wordDetail.mockResolvedValue({
      word: "family",
      spokenForm: "family",
      ipa: "ˈfæməli",
      audioBase64: null,
      phonemes: [],
      visemes: [],
    });

    renderPage();

    await screen.findByText("Tinglang. Takrorlang. Ishonch hosil qiling.");
    expect(screen.queryByRole("button", { name: uz.pronunciation.back })).toBeNull();
  });
});
