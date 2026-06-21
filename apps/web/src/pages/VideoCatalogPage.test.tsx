import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  extractYouTubePlaylistId,
  extractYouTubeVideoId,
  createRefreshPlaylistQueryQueue,
  mergeFreshPlaylists,
  SearchResultRow,
  VideoCatalogPage,
  YouTubeUrlPanel,
} from "./VideoCatalogPage";
import type { VideoFeedItemDto } from "@/api/types";
import { VIDEO_CATEGORIES } from "./videoCategories";
import { VideoCatalogHeader } from "@/components/video/VideoCatalogChrome";

const videoApiMocks = vi.hoisted(() => ({
  feed: vi.fn(),
  search: vi.fn(),
  open: vi.fn(),
  openUrl: vi.fn(),
  playlistSearch: vi.fn(),
}));

vi.mock("@/api/client", () => ({
  api: {
    video: videoApiMocks,
    gamification: {
      points: vi.fn().mockResolvedValue({ lifetimeXp: 360 }),
      energy: vi.fn().mockResolvedValue({ current: 5, maximum: 5, nextRefillAt: "2030-01-01T00:00:00Z" }),
    },
    vocabulary: { notifications: vi.fn().mockResolvedValue([]) },
  },
}));
vi.mock("@/components/game/EnergyProvider", () => ({
  useEnergy: () => ({
    energy: { current: 5, maximum: 5, nextRefillAt: null, fullRefillAt: null, outcome: 0 },
    consume: vi.fn().mockResolvedValue({ current: 4, maximum: 5, nextRefillAt: "2030-01-01T00:00:00Z", fullRefillAt: "2030-01-01T02:24:00Z", outcome: 1 }),
    openEnergyModal: vi.fn(),
  }),
}));

afterEach(() => {
  cleanup();
  localStorage.clear();
  sessionStorage.clear();
  vi.unstubAllGlobals();
  vi.clearAllMocks();
});

const item: VideoFeedItemDto = {
  lessonId: null,
  youTubeVideoId: "abcdefghijk",
  title: "Captioned English lesson",
  channel: "Learning Channel",
  channelAvatarUrl: "https://yt3.ggpht.com/learning-channel-avatar",
  durationSeconds: 240,
  topic: "search",
  level: 2,
  hasClosedCaptions: true,
};

beforeEach(() => {
  class IntersectionObserverStub {
    observe() {}
    disconnect() {}
    unobserve() {}
  }
  vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
  videoApiMocks.feed.mockReset().mockResolvedValue({ items: [item], nextCursor: null });
  videoApiMocks.search.mockReset().mockResolvedValue({ items: [item], nextCursor: null });
  videoApiMocks.playlistSearch.mockReset().mockResolvedValue({ items: [] });
  videoApiMocks.openUrl.mockReset().mockResolvedValue({ id: "url-ready" });
  vi.spyOn(Math, "random").mockReturnValue(0);
});

function renderCatalog() {
  return render(
    <MemoryRouter initialEntries={["/video"]}>
      <Routes>
        <Route path="/video" element={<VideoCatalogPage />} />
        <Route path="/video/playlist/:playlistId" element={<div>PLAYLIST_DESTINATION</div>} />
        <Route path="/video/playlists/:playlistId" element={<div>PLAYLIST_DESTINATION</div>} />
        <Route path="/video/playlists/:playlistId/:episodeNumber" element={<div>PLAYLIST_EPISODE_DESTINATION</div>} />
        <Route path="/video/:id/play" element={<div>PLAYER_DESTINATION</div>} />
        <Route path="/video/search" element={<FullSearchDestination />} />
      </Routes>
    </MemoryRouter>,
  );
}

function FullSearchDestination() {
  const location = useLocation();
  return <div>FULL_SEARCH_DESTINATION{location.search}</div>;
}

describe("SearchResultRow", () => {
  it("shows CC and marks the previously opened video as selected", () => {
    render(<SearchResultRow item={item} index={0} selected onPick={() => undefined} />);

    expect(screen.getByLabelText("Subtitr mavjud").textContent).toContain("CC");
    const card = screen.getByRole("button", { name: /Oldin ochilgan video/ });
    expect(card.getAttribute("aria-pressed")).toBe("true");
  });

  it("does not claim CC when caption availability is unknown", () => {
    render(
      <SearchResultRow
        item={{ ...item, hasClosedCaptions: false }}
        index={0}
        selected={false}
        onPick={() => undefined}
      />,
    );

    expect(screen.queryByLabelText("Subtitr mavjud")).toBeNull();
    expect(screen.getByRole("button").getAttribute("aria-pressed")).toBe("false");
  });
});

describe("VideoCatalogPage", () => {
  it("renders the Pen heading, compact search, filters and five mobile destinations", async () => {
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Tomosha qiling. O‘rganing.");
    expect(screen.getByText("Sevimli video, film va multfilmlaringiz bilan ingliz tilini mashq qiling.")).toBeTruthy();
    expect(screen.getByRole("searchbox", { name: "Video qidirish" }).getAttribute("placeholder")).toBe("Film, multfilm yoki mavzu nomi...");
    expect(screen.getByRole("button", { name: "YouTube link qo‘shish" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Orqaga" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "Bildirishnomalar" })).toBeTruthy();
    expect(document.querySelector(".video-catalog-header__module")?.textContent).toBe("Video");
    expect(document.querySelector(".video-catalog__meta")).toBeNull();
    expect(screen.getByRole("article").textContent).not.toContain("A2 ·");
    expect(screen.getByRole("navigation", { name: "Mobil navigatsiya" }).querySelectorAll("button")).toHaveLength(5);
  });

  it.each([
    [undefined, "HOME_DESTINATION"],
    [{ returnTo: "/progress" }, "PROGRESS_DESTINATION"],
  ])("returns from the catalog using the route's return target %j", (state, destination) => {
    render(
      <MemoryRouter initialEntries={[{ pathname: "/video", state }]}>
        <Routes>
          <Route path="/video" element={<VideoCatalogHeader />} />
          <Route path="/home" element={<div>HOME_DESTINATION</div>} />
          <Route path="/progress" element={<div>PROGRESS_DESTINATION</div>} />
        </Routes>
      </MemoryRouter>,
    );
    fireEvent.click(screen.getByRole("button", { name: "Orqaga" }));
    expect(screen.getByText(destination)).toBeTruthy();
  });

  it("shows the live first-page videos without unrelated placeholders", async () => {
    videoApiMocks.search.mockResolvedValueOnce({
      items: [{ ...item, youTubeVideoId: "I_tRSrPru94", title: "Live captioned introduction" }],
      nextCursor: null,
    });
    renderCatalog();
    await screen.findByRole("heading", { name: "Live captioned introduction" });
    expect(screen.getAllByRole("article")).toHaveLength(1);
  });

  it("keeps only Barchasi, Multfilm and Kino; Barchasi still loads English-learning videos", async () => {
    videoApiMocks.search.mockImplementation((query: string) => Promise.resolve({
      items: [{ ...item, title: query === VIDEO_CATEGORIES[0].query ? item.title : query }], nextCursor: null,
    }));
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    expect(screen.getByRole("group", { name: "Video kataloglari" }).querySelectorAll("button")).toHaveLength(3);
    expect(screen.getByRole("button", { name: "Playlistni ochish" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "To‘liq film va multfilm playlistlari" })).toBeTruthy();
    expect(videoApiMocks.search).toHaveBeenLastCalledWith(VIDEO_CATEGORIES[0].query, null, 12);
    for (const removedCategory of ["Tinglash", "Gapirish", "Lug‘at", "Grammatika", "Talaffuz"]) {
      expect(screen.queryByRole("button", { name: removedCategory })).toBeNull();
    }
  });

  it("opens a full-episode shelf for older-learner multfilms instead of nursery content", async () => {
    const playlist = {
      id: "PL-puss", title: "Puss in Boots full episodes", channel: "DreamWorks English",
      thumbnailUrl: "https://img.example/puss.jpg", totalDurationSeconds: 3600, isSingleVideoCollection: false,
      items: [{ youTubeVideoId: "abcdefghijk", title: "Puss in Boots — Part 1", channel: "DreamWorks English", durationSeconds: 1800, thumbnailUrl: null }],
    };
    videoApiMocks.playlistSearch.mockResolvedValue({ items: [playlist] });
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    fireEvent.click(screen.getByRole("button", { name: "Multfilm" }));
    await screen.findByRole("heading", { name: "10+ yosh uchun multfilm playlistlari" });
    expect(screen.getByText("Puss in Boots full episodes")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: /Puss in Boots full episodes/ }));
    expect(await screen.findByText("PLAYLIST_DESTINATION")).toBeTruthy();
  });

  it("opens the Kino category as a duration-checked full-playlist shelf", async () => {
    const playlist = {
      id: "PL-interstellar", title: "Interstellar — full movie collection", channel: "Cinema English",
      thumbnailUrl: "https://img.example/interstellar.jpg", totalDurationSeconds: 7200, isSingleVideoCollection: false,
      items: [{ youTubeVideoId: "lmnopqrstuv", title: "Interstellar — Part 1", channel: "Cinema English", durationSeconds: 3600, thumbnailUrl: null }],
    };
    videoApiMocks.playlistSearch.mockResolvedValue({ items: [playlist] });
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    fireEvent.click(screen.getByRole("button", { name: "Kino" }));
    await screen.findByRole("heading", { name: "Kino playlistlari" });
    expect(videoApiMocks.playlistSearch).toHaveBeenCalledWith("Interstellar full movie English playlist");
    expect(screen.getByText("Interstellar — full movie collection")).toBeTruthy();
  });

  it.each([
    ["Multfilm", "Inside Out", "?q=Inside+Out&kind=cartoon"],
    ["Kino", "Interstellar", "?q=Interstellar&kind=movie"],
  ])("sends a title typed inside %s to its matching full-playlist search", async (category, title, expectedSearch) => {
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    fireEvent.click(screen.getByRole("button", { name: category }));
    await screen.findByRole("heading", {
      name: category === "Multfilm" ? "10+ yosh uchun multfilm playlistlari" : "Kino playlistlari",
    });
    const input = screen.getByRole("searchbox", { name: "Video qidirish" });
    fireEvent.change(input, { target: { value: title } });
    fireEvent.submit(input.closest("form")!);
    expect(await screen.findByText(`FULL_SEARCH_DESTINATION${expectedSearch}`)).toBeTruthy();
  });

  it.each([
    ["Multfilm", "Inside Out"],
    ["Kino", "Interstellar"],
  ])("shows full playlist suggestions automatically, never individual videos, while typing in %s", async (category, title) => {
    const playlist = {
      id: `PL-${title.replace(/\s+/g, "-").toLowerCase()}`,
      title: `${title} full playlist`,
      channel: "English cinema",
      thumbnailUrl: "https://img.example/playlist.jpg",
      totalDurationSeconds: 5400,
      isSingleVideoCollection: false,
      items: [{ ...item, title: `${title} — Part 1` }],
    };
    videoApiMocks.playlistSearch.mockResolvedValue({ items: [playlist] });
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    const individualSearchCount = videoApiMocks.search.mock.calls.length;
    fireEvent.click(screen.getByRole("button", { name: category }));
    await screen.findByRole("heading", {
      name: category === "Multfilm" ? "10+ yosh uchun multfilm playlistlari" : "Kino playlistlari",
    });

    expect(videoApiMocks.search).toHaveBeenCalledTimes(individualSearchCount);
    fireEvent.change(screen.getByRole("searchbox", { name: "Video qidirish" }), { target: { value: title } });
    const expectedQuery = category === "Multfilm"
      ? `${title} full episodes English`
      : `${title} full movie English playlist`;
    await waitFor(() => expect(videoApiMocks.playlistSearch).toHaveBeenLastCalledWith(expectedQuery));

    expect(videoApiMocks.search).not.toHaveBeenCalledWith(title, null, 12);
    expect(document.querySelectorAll("[data-video-search-id]")).toHaveLength(0);
    const playlistSuggestion = await screen.findByRole("button", { name: `${playlist.title} — playlistni ochish` });
    fireEvent.click(playlistSuggestion);
    expect(await screen.findByText("PLAYLIST_DESTINATION")).toBeTruthy();
  });

  it("does not show the removed more-video or catalog-end controls", async () => {
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    expect(screen.queryByRole("button", { name: "Ko‘proq video" })).toBeNull();
    expect(screen.queryByText("Katalog yakuni")).toBeNull();
  });

  it("starts every refreshed collection shelf from a different discovery query", () => {
    const first = createRefreshPlaylistQueryQueue("cartoon");
    const second = createRefreshPlaylistQueryQueue("cartoon");
    expect(first[0]).not.toBe(second[0]);
  });

  it("prefers unseen playlists on a refreshed full-episode shelf", () => {
    const avengers = {
      id: "PL-avengers", title: "Avengers full movie", channel: "Marvel", thumbnailUrl: null,
      totalDurationSeconds: 7200, isSingleVideoCollection: false, items: [],
    };
    const puss = {
      id: "PL-puss", title: "Puss in Boots full episodes", channel: "DreamWorks", thumbnailUrl: null,
      totalDurationSeconds: 5400, isSingleVideoCollection: false, items: [],
    };

    const merged = mergeFreshPlaylists([], [avengers, puss], new Set(["PL-avengers"]));

    expect(merged.map(playlist => playlist.id)).toEqual(["PL-puss"]);
  });

  it("renders actual channel artwork rather than a channel initial", async () => {
    renderCatalog();
    const avatar = await screen.findByRole("img", { name: "Learning Channel kanal logosi" });
    expect(avatar.getAttribute("src")).toBe(item.channelAvatarUrl);
    expect(avatar.getAttribute("referrerpolicy")).toBe("no-referrer");
  });

  it("opens a YouTube URL from the single search field without sending it to search", async () => {
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "https://youtu.be/dQw4w9WgXcQ" } });
    fireEvent.submit(screen.getByRole("search"));
    await waitFor(() => expect(videoApiMocks.openUrl).toHaveBeenCalledWith("dQw4w9WgXcQ"));
    expect(await screen.findByText("PLAYER_DESTINATION")).toBeTruthy();
    expect(videoApiMocks.search).toHaveBeenCalledTimes(1);
    expect(videoApiMocks.search).toHaveBeenCalledWith(VIDEO_CATEGORIES[0].query, null, 12);
  });

  it("opens a pasted YouTube playlist directly so an official full-content link is not treated as a video search", async () => {
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    fireEvent.change(screen.getByRole("searchbox"), {
      target: { value: "https://www.youtube.com/playlist?list=PLb9HBx1ySEwTUAwQCM_7GbBebp9nV0cmU" },
    });
    fireEvent.submit(screen.getByRole("search"));

    expect(await screen.findByText("PLAYLIST_DESTINATION")).toBeTruthy();
    expect(videoApiMocks.openUrl).not.toHaveBeenCalled();
  });

  it("searches, reports an error and lets the learner close results", async () => {
    videoApiMocks.search.mockResolvedValueOnce({ items: [item], nextCursor: null }).mockRejectedValue(new Error("Unavailable"));
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    fireEvent.change(screen.getByRole("searchbox"), { target: { value: "travel" } });
    fireEvent.submit(screen.getByRole("search"));
    await waitFor(() => expect(videoApiMocks.search).toHaveBeenCalledWith("travel", null, 12));
    expect(await screen.findByRole("alert")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Qidiruv natijalarini yopish" }));
    expect(screen.queryByRole("button", { name: "Qidiruv natijalarini yopish" })).toBeNull();
  });

  it("opens and dismisses card actions with Escape without starting playback", async () => {
    renderCatalog();
    await screen.findByRole("heading", { name: item.title });
    const more = screen.getByRole("button", { name: `${item.title} — amallar` });
    fireEvent.click(more);
    expect(screen.getByRole("link", { name: "YouTube’da ochish" }).getAttribute("href")).toContain(item.youTubeVideoId);
    fireEvent.keyDown(more, { key: "Escape" });
    expect(screen.queryByRole("link", { name: "YouTube’da ochish" })).toBeNull();
    expect(videoApiMocks.open).not.toHaveBeenCalled();
  });

  it("shows the centered preparation state before navigating to the player", async () => {
    const readyItem: VideoFeedItemDto = {
      ...item,
      lessonId: "lesson-ready",
      title: "Ready video lesson",
    };
    videoApiMocks.search.mockResolvedValueOnce({
      items: [readyItem],
      nextCursor: null,
    });

    class IntersectionObserverStub {
      observe() {}
      disconnect() {}
      unobserve() {}
    }

    const animationFrames: FrameRequestCallback[] = [];
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    vi.stubGlobal("requestAnimationFrame", (callback: FrameRequestCallback) => {
      animationFrames.push(callback);
      return animationFrames.length;
    });
    vi.stubGlobal("cancelAnimationFrame", vi.fn());

    render(
      <MemoryRouter initialEntries={["/video"]}>
        <Routes>
          <Route path="/video" element={<VideoCatalogPage />} />
          <Route path="/video/:id/play" element={<div>PLAYER_DESTINATION</div>} />
        </Routes>
      </MemoryRouter>,
    );

    fireEvent.click(await screen.findByRole("button", { name: "Ready video lesson" }));

    const preparationState = screen.getByRole("status", { name: /Tayyorlanmoqda/ });
    expect(preparationState.textContent).toContain("Tayyorlanmoqda...");
    expect(screen.queryByText("PLAYER_DESTINATION")).toBeNull();

    await act(async () => {
      const firstFrame = animationFrames.splice(0);
      firstFrame.forEach((callback) => callback(0));
    });
    await act(async () => {
      const secondFrame = animationFrames.splice(0);
      secondFrame.forEach((callback) => callback(16));
    });

    expect(await screen.findByText("PLAYER_DESTINATION")).toBeTruthy();
  });
});

describe("YouTubeUrlPanel", () => {
  it.each([
    ["https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ"],
    ["https://youtu.be/dQw4w9WgXcQ?t=10", "dQw4w9WgXcQ"],
    ["https://youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ"],
    ["dQw4w9WgXcQ", "dQw4w9WgXcQ"],
  ])("extracts the video id from %s", (value, expected) => {
    expect(extractYouTubeVideoId(value)).toBe(expected);
  });

  it("rejects non-YouTube URLs", () => {
    expect(extractYouTubeVideoId("https://example.com/watch?v=dQw4w9WgXcQ")).toBeNull();
  });

  it.each([
    ["https://www.youtube.com/playlist?list=PLb9HBx1ySEwTUAwQCM_7GbBebp9nV0cmU", "PLb9HBx1ySEwTUAwQCM_7GbBebp9nV0cmU"],
    ["https://www.youtube.com/watch?v=dQw4w9WgXcQ&list=PLb9HBx1ySEwTUAwQCM_7GbBebp9nV0cmU", "PLb9HBx1ySEwTUAwQCM_7GbBebp9nV0cmU"],
  ])("extracts the playlist id from %s", (value, expected) => {
    expect(extractYouTubePlaylistId(value)).toBe(expected);
  });

  it("opens a valid pasted YouTube video", async () => {
    const onOpen = vi.fn().mockResolvedValue(undefined);
    render(<YouTubeUrlPanel onOpen={onOpen} />);

    fireEvent.click(screen.getByRole("button", { name: /YouTube URL orqali ochish/ }));

    fireEvent.change(screen.getByLabelText("YouTube video URL"), {
      target: { value: "https://youtu.be/dQw4w9WgXcQ" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Videoni ochish" }));

    await waitFor(() => expect(onOpen).toHaveBeenCalledWith("dQw4w9WgXcQ"));
  });

  it("shows an error for an invalid URL", () => {
    render(<YouTubeUrlPanel onOpen={vi.fn()} />);

    fireEvent.click(screen.getByRole("button", { name: /YouTube URL orqali ochish/ }));

    fireEvent.change(screen.getByLabelText("YouTube video URL"), {
      target: { value: "not-a-youtube-url" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Videoni ochish" }));

    expect(screen.getByRole("alert").textContent).toContain("To‘g‘ri YouTube video havolasini kiriting");
  });

  it("starts closed and restores the latest toggle choice", () => {
    const { unmount } = render(<YouTubeUrlPanel onOpen={vi.fn()} />);
    const toggle = screen.getByRole("button", { name: /YouTube URL orqali ochish/ });

    expect(toggle.getAttribute("aria-expanded")).toBe("false");
    expect(screen.queryByLabelText("YouTube video URL")).toBeNull();

    fireEvent.click(toggle);
    expect(screen.getByLabelText("YouTube video URL")).not.toBeNull();

    unmount();
    render(<YouTubeUrlPanel onOpen={vi.fn()} />);
    expect(screen.getByRole("button", { name: /YouTube URL orqali ochish/ }).getAttribute("aria-expanded")).toBe("true");
  });
});
