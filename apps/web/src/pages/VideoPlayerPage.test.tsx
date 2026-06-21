import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CefrLevel, IngestionStatus, TranscriptStatus, type VideoLessonDto } from "@/api/types";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { VideoPlayerPage } from "./VideoPlayerPage";

const player = vi.hoisted(() => ({
  currentTime: 0, duration: 157, isPlaying: false, hasStarted: false, ended: false,
  seekTo: vi.fn(), togglePlay: vi.fn(), play: vi.fn(), pause: vi.fn(), getCurrentTime: () => 0,
  volume: 100, muted: false, setVolume: vi.fn(), toggleMute: vi.fn(),
  playbackRate: 1, setPlaybackRate: vi.fn(),
}));
const energyState = vi.hoisted(() => ({
  energy: { current: 5, maximum: 5, nextRefillAt: null, fullRefillAt: null, outcome: 0 },
  consume: vi.fn().mockResolvedValue({ current: 4, maximum: 5, nextRefillAt: "2030-01-01T00:00:00Z", fullRefillAt: "2030-01-01T02:24:00Z", outcome: 1 }),
  openEnergyModal: vi.fn(),
}));
const fixture: VideoLessonDto = {
  id: "lesson-1", title: "How to introduce yourself", channel: "BBC Learning English",
  youTubeVideoId: "I_tRSrPru94", durationSeconds: 157, topic: "Introductions",
  level: CefrLevel.A2, status: IngestionStatus.Leveled, transcriptStatus: TranscriptStatus.Available,
  transcript: [
    { startSeconds: 6, endSeconds: 9, englishText: "Hello, what's your name?", uzbekTranslation: "Salom, ismingiz nima?", words: [] },
    { startSeconds: 9, endSeconds: 14, englishText: "Hi, I'm Tim.", uzbekTranslation: "Salom, men Timman.", words: [] },
    { startSeconds: 14, endSeconds: 18, englishText: "Hello, I'm Sian.", uzbekTranslation: null, words: [] },
    { startSeconds: 18, endSeconds: 22, englishText: "It's nice to meet you.", uzbekTranslation: "Tanishganimdan xursandman.", words: [] },
  ],
  glossary: [{ word: "hello", uzbekMeaning: "salom" }],
  questions: [{ id: "q1", prompt: "What is his name?", options: ["Tim", "Tom"], hintCode: null }],
};

vi.mock("@/api/client", () => ({
  api: {
    video: { lesson: vi.fn(), explainStream: vi.fn().mockResolvedValue({ replyUz: "Izoh" }) },
    gamification: {
      points: vi.fn().mockResolvedValue({ lifetimeXp: 410 }),
      energy: vi.fn().mockResolvedValue({ current: 5, maximum: 5, consumed: false }),
    },
  }, apiErrorDetails: () => null,
}));
vi.mock("@/lib/useYouTubePlayer", async (original) => ({
  ...(await original<typeof import("@/lib/useYouTubePlayer")>()), useYouTubePlayer: () => player,
}));
vi.mock("@/components/video/WordDetailSheet", () => ({
  WordDetailSheet: ({ word, exampleSentence, translation, onAskAi }: { word: string; exampleSentence: string; translation: string; onAskAi: (question: string) => void }) => (
    <div data-testid="word-detail">{word} · {exampleSentence} · {translation}<button onClick={() => onAskAi(exampleSentence)}>AI izoh</button></div>
  ),
}));
vi.mock("@/components/video/ExplainChatPanel", () => ({ ExplainChatPanel: () => <div data-testid="video-chat" /> }));

vi.mock("@/lib/useUnreadNotifications", () => ({ useUnreadNotifications: () => 3 }));
vi.mock("@/components/game/EnergyProvider", () => ({
  useEnergy: () => energyState,
}));

function page() {
  return (
    <MemoryRouter initialEntries={["/video/lesson-1/play"]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <Routes>
        <Route path="/video/:id/play" element={<VideoPlayerPage />} />
        <Route path="/video/:id/quiz" element={<p>Quiz destination</p>} />
        <Route path="/video" element={<p>Video catalog destination</p>} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.mocked(api.video.lesson).mockResolvedValue(structuredClone(fixture));
  localStorage.clear();
  player.currentTime = 0;
});
afterEach(cleanup);

describe("screen 72 native YouTube layout", () => {
  it("removes the energy message while preserving the shared header balance", async () => {
    const current = 5;
    render(page());
    await screen.findByRole("heading", { name: fixture.title });
    expect(await screen.findByRole("button", { name: `Energiya: ${current} / 5` })).toBeTruthy();
    expect(document.querySelector(".video-player-energy")).toBeNull();
    expect(screen.queryByText("Video ochildi. Yaxshi tomosha!")).toBeNull();
    expect(screen.queryByText(/^Energiya ·/)).toBeNull();
  });

  it("renders the Pen content around an uncovered native player, not a custom player skin", async () => {
    render(page());
    await waitFor(() => expect(document.querySelector(".video-player-frame")).not.toBeNull());
    const frame = document.querySelector(".video-player-frame")!;
    expect(frame.querySelector("#video-lesson-player")).not.toBeNull();
    expect(frame.querySelector("button, img, [data-video-caption], [data-video-controls]")).toBeNull();
    expect(document.querySelector(".video-player-stage__bar, .video-player-guidance, .video-player-assistant")).toBeNull();
    expect(screen.getByRole("heading", { name: "Transcript" })).toBeTruthy();
    expect(frame.contains(screen.getByRole("button", { name: "Video testiga o‘tish" }))).toBe(false);
    expect(screen.getAllByRole("listitem")).toHaveLength(4);
  });

  it("highlights the spoken word at playback time and removes the right-hand close control", async () => {
    player.currentTime = 7;
    render(page());
    await screen.findByRole("heading", { name: fixture.title });
    expect(document.querySelectorAll('[data-speaking="true"]')).toHaveLength(1);
    expect(screen.getByRole("link", { name: uz.common.back })).toBeTruthy();
    expect(screen.queryByRole("link", { name: uz.videoPlayer.close })).toBeNull();
    expect(document.querySelector(".video-player-watch")).not.toBeNull();
  });

  it("keeps the real lesson and YouTube source instead of copying the sample's video or view count", async () => {
    render(page());
    await waitFor(() => expect(document.querySelector(".video-player-frame")).not.toBeNull());
    expect(screen.getByText("BBC Learning English · A2 · 2:37")).toBeTruthy();
    expect(await screen.findByText("410 XP")).toBeTruthy();
    expect(screen.queryByText(/−1 energiya/)).toBeNull();
    expect(screen.getByRole("link", { name: /YouTube’da ochish/ }).getAttribute("href")).toBe("https://www.youtube.com/watch?v=I_tRSrPru94");
    expect(screen.queryByText(/mln ko‘rish/)).toBeNull();
  });

  it("selects a caption and seeks using the timestamp without hijacking page keyboard shortcuts", async () => {
    render(page());
    const timestamp = await screen.findByRole("button", { name: "00:14 — Hello, I'm Sian." });
    fireEvent.click(timestamp);
    expect(player.seekTo).toHaveBeenCalledWith(14);
    expect(timestamp.closest("li")?.getAttribute("aria-current")).toBe("true");
    fireEvent.keyDown(document.body, { code: "Space", key: " " });
    expect(player.togglePlay).not.toHaveBeenCalled();
  });

  it("follows playback from YouTube's own controls", async () => {
    const view = render(page());
    await screen.findByRole("heading", { name: fixture.title });
    player.currentTime = 18;
    view.rerender(page());
    await waitFor(() => expect(document.querySelector('[data-caption-index="3"]')?.getAttribute("aria-current")).toBe("true"));
  });

  it("toggles translation inline for only the selected sentence", async () => {
    render(page());
    await screen.findByRole("heading", { name: fixture.title });
    expect(screen.getByText("Salom, ismingiz nima?")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "00:14 — Hello, I'm Sian." }));
    expect(screen.queryByText("Salom, ismingiz nima?")).toBeNull();
    expect(screen.getByText(uz.videoPlayer.translationPending)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "00:14 — Hello, I'm Sian." }));
    expect(screen.getByText(uz.videoPlayer.translationPending).closest("[hidden]")).not.toBeNull();
    fireEvent.click(screen.getByRole("button", { name: uz.videoPlayer.showTranslation }));
    expect(screen.getByText(uz.videoPlayer.translationPending).closest("[hidden]")).toBeNull();
  });

  it("a single click on the sentence text toggles inline translation, not a word dialog", async () => {
    render(page());
    await screen.findByRole("heading", { name: fixture.title });
    const firstRow = document.querySelector('[data-caption-index="0"]') as HTMLElement;
    fireEvent.click(within(firstRow).getByRole("button", { name: "Hello" }));
    expect(firstRow.querySelector(".video-practice-translation")?.hasAttribute("hidden")).toBe(true);
    expect(screen.queryByTestId("word-detail")).toBeNull();
    fireEvent.click(within(firstRow).getByRole("button", { name: "Hello" }));
    expect(firstRow.querySelector(".video-practice-translation")?.hasAttribute("hidden")).toBe(false);
  });

  it("asking about the selected sentence does not pause native playback", async () => {
    render(page());
    await screen.findByRole("heading", { name: fixture.title });
    player.pause.mockClear();
    fireEvent.click(document.querySelector(".video-practice-ask")!);
    expect(player.pause).not.toHaveBeenCalled();
    await waitFor(() => expect(api.video.explainStream).toHaveBeenCalled());
    expect(document.querySelector(".video-player-main")?.classList.contains("is-ai-open")).toBe(true);
  });

  it("preserves word details, glossary meanings and the AI explanation flow", async () => {
    render(page());
    await screen.findByRole("heading", { name: fixture.title });
    const firstRow = document.querySelector('[data-caption-index="0"]') as HTMLElement;
    fireEvent.doubleClick(within(firstRow).getByRole("button", { name: "Hello" }));
    expect(player.pause).toHaveBeenCalled();
    expect(screen.getByTestId("word-detail").textContent).toContain("hello · Hello, what's your name? · salom");
    expect(player.seekTo).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button", { name: "AI izoh" }));
    expect(screen.queryByTestId("word-detail")).toBeNull();
    expect(screen.getByTestId("video-chat")).toBeTruthy();
    await waitFor(() => expect(api.video.explainStream).toHaveBeenCalledWith("lesson-1", "[00:06] Hello, what's your name?", "Hello, what's your name?", expect.any(Array), expect.any(Function)));
  });

  it("keeps caption selection in the player without a repeat button or shadowing page", async () => {
    const view = render(page());
    fireEvent.click(await screen.findByRole("button", { name: "00:14 — Hello, I'm Sian." }));
    expect(player.seekTo).toHaveBeenCalledWith(14);
    expect(screen.queryByRole("button", { name: "Tanlangan gapni takrorlash", hidden: true })).toBeNull();
    expect(screen.queryByRole("region", { name: "Shadowing mashqi", hidden: true })).toBeNull();
    expect(document.querySelector(".video-player-main")?.hasAttribute("hidden")).toBe(false);
    expect(within(document.querySelector(".video-player-actions") as HTMLElement).getAllByRole("button")).toEqual([
      screen.getByRole("button", { name: "Video testiga o‘tish" }),
    ]);
    player.currentTime = 18;
    view.rerender(page());
    await waitFor(() => expect(document.querySelector('[data-caption-index="3"]')?.getAttribute("aria-current")).toBe("true"));
    expect(document.querySelector(".shadowing-practice-page, .shadowing-panel")).toBeNull();
    expect(screen.getByRole("heading", { name: fixture.title })).toBeTruthy();
  });

  it("keeps quiz and close navigation working", async () => {
    const view = render(page());
    fireEvent.click(await screen.findByRole("button", { name: "Video testiga o‘tish" }));
    expect(screen.getByText("Quiz destination")).toBeTruthy();
    view.unmount();
    render(page());
    fireEvent.click(screen.getByRole("link", { name: uz.common.back }));
    expect(screen.getByText("Video catalog destination")).toBeTruthy();
    expect(document.body.classList.contains("video-player-route-active")).toBe(false);
  });

  it("keeps the player usable when interactive subtitles are unavailable", async () => {
    vi.mocked(api.video.lesson).mockResolvedValue({ ...fixture, transcript: [], questions: [], transcriptStatus: TranscriptStatus.Unavailable });
    render(page());
    await screen.findByText(uz.videoPlayer.transcriptUnavailable);
    expect(document.getElementById("video-lesson-player")).not.toBeNull();
    expect(screen.queryByRole("button", { name: "Tanlangan gapni takrorlash", hidden: true })).toBeNull();
    expect(screen.getByRole("button", { name: "Video testiga o‘tish" }).hasAttribute("disabled")).toBe(true);
  });

  it("does not embed an invalid lesson identifier when the API fails", async () => {
    vi.mocked(api.video.lesson).mockRejectedValue(new Error("Offline"));
    render(page());
    await screen.findByText(uz.videoPlayer.videoUnavailable);
    expect(document.getElementById("video-lesson-player")).toBeNull();
    expect(screen.getByRole("link", { name: uz.common.back }).getAttribute("href")).toBe("/video");
  });
});
