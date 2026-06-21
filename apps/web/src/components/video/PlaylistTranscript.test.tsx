import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { PlaylistTranscript } from "./PlaylistTranscript";
import type { TranscriptSegmentDto } from "@/api/types";

const transcript: TranscriptSegmentDto[] = Array.from({ length: 9 }, (_, index) => ({
  startSeconds: index * 5, endSeconds: index * 5 + 5,
  englishText: `Sentence number ${index + 1}.`, uzbekTranslation: `Tarjima ${index + 1}.`, words: [],
}));
const defaults = {
  transcript, selectedIndex: 2, currentTime: 12, showTranslation: true, unavailable: false, aiSending: false,
  onSelect: vi.fn(), onAskAi: vi.fn(), onToggleTranslation: vi.fn(),
};

describe("Pen 72 playlist transcript", () => {
  it("renders a bounded timed window and the selected sentence translation", () => {
    render(<PlaylistTranscript {...defaults} />);
    expect(screen.getAllByRole("listitem")).toHaveLength(3);
    expect(screen.getByText("Tarjima 3.")).toBeTruthy();
    expect(screen.queryByText("Tarjima 2.")).toBeNull();
    expect(screen.getAllByRole("listitem")[2].getAttribute("aria-current")).toBe("true");
    expect(screen.getAllByRole("listitem")[2].getAttribute("aria-setsize")).toBe("9");
  });

  it("allows paging independently and returning to the playback window", () => {
    render(<PlaylistTranscript {...defaults} />);
    fireEvent.click(screen.getByRole("button", { name: "Keyingi jumlalar" }));
    expect(screen.getByRole("button", { name: "00:15 — Sentence number 4." })).toBeTruthy();
    expect(screen.queryByRole("button", { name: "00:00 — Sentence number 1." })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Videoga mos kuzatish" }));
    expect(screen.getByRole("button", { name: "00:00 — Sentence number 1." })).toBeTruthy();
  });

  it("selects the correct sentence and sends only its own context to AI", () => {
    const onSelect = vi.fn();
    const onAskAi = vi.fn();
    render(<PlaylistTranscript {...defaults} onSelect={onSelect} onAskAi={onAskAi} />);
    fireEvent.click(screen.getByRole("button", { name: "00:05 — Sentence number 2." }));
    expect(onSelect).toHaveBeenCalledWith(1);
    fireEvent.click(screen.getByRole("button", { name: '"Sentence number 2." ni AI bilan tushunish' }));
    expect(onAskAi).toHaveBeenCalledWith(1);
  });

  it("keeps hidden translations out of the accessible tree", () => {
    const onToggleTranslation = vi.fn();
    render(<PlaylistTranscript {...defaults} showTranslation={false} onToggleTranslation={onToggleTranslation} />);
    expect(screen.getByText("Tarjima 3.").hidden).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: "Tarjimani ko‘rsatish" }));
    expect(onToggleTranslation).toHaveBeenCalledOnce();
  });

  it("follows a new active sentence and bounds rendering for very long videos", () => {
    const long = Array.from({ length: 2300 }, (_, index) => ({ ...transcript[0], startSeconds: index * 5, englishText: `Line ${index}` }));
    const { rerender } = render(<PlaylistTranscript {...defaults} transcript={long} selectedIndex={0} />);
    rerender(<PlaylistTranscript {...defaults} transcript={long} selectedIndex={1800} />);
    expect(screen.getAllByRole("listitem")).toHaveLength(3);
    expect(screen.getAllByRole("listitem")[2].getAttribute("aria-posinset")).toBe("1801");
    expect(screen.getAllByRole("listitem")[2].getAttribute("aria-setsize")).toBe("2300");
  });

  it("distinguishes unavailable subtitles from pending subtitles without blocking video", () => {
    const { rerender } = render(<PlaylistTranscript {...defaults} transcript={[]} />);
    expect(screen.getByRole("status").getAttribute("aria-label")).toBe("Subtitrlar yuklanmoqda…");
    expect(screen.getByRole("status").getAttribute("aria-busy")).toBe("true");
    expect(document.querySelector(".video-playlist-player__spinner")).not.toBeNull();
    rerender(<PlaylistTranscript {...defaults} transcript={[]} unavailable />);
    expect(screen.getByRole("status").getAttribute("aria-label")).toBe("Bu video uchun EnglishAI subtitrlari mavjud emas");
    expect(document.querySelector(".video-playlist-player__spinner")).toBeNull();
  });

  it("offers retry instead of an endless loading spinner after an error", () => {
    const onRetry = vi.fn();
    render(<PlaylistTranscript {...defaults} transcript={[]} loadState="error" onRetry={onRetry} />);
    expect(screen.getByRole("status").getAttribute("aria-busy")).toBe("false");
    expect(document.querySelector(".video-playlist-player__spinner")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
    expect(onRetry).toHaveBeenCalledOnce();
  });

  it("disables navigation at both ends of a one-sentence transcript", () => {
    render(<PlaylistTranscript {...defaults} transcript={[transcript[0]]} selectedIndex={0} />);
    expect(screen.getByRole<HTMLButtonElement>("button", { name: "Oldingi jumlalar" }).disabled).toBe(true);
    expect(screen.getByRole<HTMLButtonElement>("button", { name: "Keyingi jumlalar" }).disabled).toBe(true);
  });
});
