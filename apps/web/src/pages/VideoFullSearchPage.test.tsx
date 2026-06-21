import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { VideoFullSearchPage } from "./VideoFullSearchPage";
import { VideoPlaylistPage } from "./VideoPlaylistPage";

const mocks = vi.hoisted(() => ({
  playlistSearch: vi.fn(), playlist: vi.fn(), open: vi.fn(), featuredPlaylist: vi.fn(),
}));

vi.mock("@/api/client", () => ({
  api: {
    video: mocks,
    gamification: { points: vi.fn().mockResolvedValue({ lifetimeXp: 360 }), energy: vi.fn().mockResolvedValue({ current: 5, maximum: 5 }) },
    vocabulary: { notifications: vi.fn().mockResolvedValue([]) },
  },
}));
vi.mock("@/components/game/EnergyProvider", () => ({
  useEnergy: () => ({
    energy: { current: 5, maximum: 5, nextRefillAt: null, fullRefillAt: null, outcome: 0 },
    consume: vi.fn().mockResolvedValue({ current: 4, maximum: 5, nextRefillAt: null, fullRefillAt: null, outcome: 1 }),
    openEnergyModal: vi.fn(),
  }),
}));

const playlist = {
  id: "PL-puss", title: "Puss in Boots full movie collection", channel: "Official English channel",
  thumbnailUrl: "https://img.example/puss.jpg", totalDurationSeconds: 5400, isSingleVideoCollection: false,
  items: [
    { youTubeVideoId: "abcdefghijk", title: "Puss in Boots — Part 1", channel: "Official English channel", durationSeconds: 2700, thumbnailUrl: "https://img.example/one.jpg" },
    { youTubeVideoId: "lmnopqrstuv", title: "Puss in Boots — Part 2", channel: "Official English channel", durationSeconds: 2700, thumbnailUrl: "https://img.example/two.jpg" },
  ],
};

function route(initial: string) {
  return render(<MemoryRouter initialEntries={[initial]}><Routes>
    <Route path="/video/search" element={<VideoFullSearchPage />} />
    <Route path="/video/playlist/:playlistId" element={<VideoPlaylistPage />} />
    <Route path="/video/playlists/:playlistId" element={<VideoPlaylistPage />} />
    <Route path="/video/playlists/:playlistId/:episodeNumber" element={<p>PLAYLIST_PLAYER</p>} />
    <Route path="/video/:id/play" element={<p>PLAYER</p>} />
    <Route path="/video" element={<p>CATALOG</p>} />
  </Routes></MemoryRouter>);
}

afterEach(() => vi.clearAllMocks());
beforeEach(() => {
  mocks.playlistSearch.mockResolvedValue({ items: [] });
  mocks.playlist.mockResolvedValue(playlist);
  mocks.open.mockResolvedValue({ id: "lesson-1" });
  mocks.featuredPlaylist.mockResolvedValue(null);
});

describe("full film and playlist discovery", () => {
  it("renders Pen 56D when a full collection is not found and lets the learner change the search", async () => {
    route("/video/search?q=unknown%20title");
    expect(await screen.findByRole("heading", { name: "To‘liq video topilmadi" })).toBeTruthy();
    expect(screen.getByRole("heading", { name: "Boshqa nom bilan sinab ko‘ring" })).toBeTruthy();
    const input = screen.getByLabelText("Qidiruvni o‘zgartirish");
    fireEvent.change(input, { target: { value: "Puss in Boots" } });
    fireEvent.submit(input.closest("form")!);
    await waitFor(() => expect(mocks.playlistSearch).toHaveBeenLastCalledWith("Puss in Boots"));
  });

  it("adds a full-episode or full-movie intent for category-specific title searches", async () => {
    route("/video/search?q=Inside%20Out&kind=cartoon");
    await waitFor(() => expect(mocks.playlistSearch).toHaveBeenLastCalledWith("Inside Out full episodes English"));

    route("/video/search?q=Interstellar&kind=movie");
    await waitFor(() => expect(mocks.playlistSearch).toHaveBeenLastCalledWith("Interstellar full movie English playlist"));
  });

  it("opens a discovered playlist and routes every episode to the dedicated playlist player", async () => {
    mocks.playlistSearch.mockResolvedValue({ items: [playlist] });
    route("/video/search?q=Puss%20in%20Boots");
    await screen.findByRole("heading", { name: "Topilgan playlistlar" });
    fireEvent.click(screen.getByRole("button", { name: /Puss in Boots full movie collection/ }));
    await screen.findByRole("heading", { name: playlist.title });
    fireEvent.click(screen.getByRole("button", { name: "Puss in Boots — Part 1 — ijro" }));
    expect(await screen.findByText("PLAYLIST_PLAYER")).toBeTruthy();
  });
});
