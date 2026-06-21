import { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useNavigate, useSearchParams } from "react-router-dom";
import { motion } from "framer-motion";
import {
  EllipsisVertical,
  ExternalLink,
  Film,
  Grid2X2,
  Link as LinkIcon,
  ListVideo,
  Popcorn,
  Search,
} from "lucide-react";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import type { VideoFeedItemDto, VideoPlaylistDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { Spinner } from "@/components/ui/Spinner";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { VideoCatalogHeader, VideoCatalogMobileNav } from "@/components/video/VideoCatalogChrome";
import { useEnergy } from "@/components/game/EnergyProvider";
import { EnergyOutcome } from "@/api/types";
import { YouTubeChannelAvatar } from "@/components/video/YouTubeChannelAvatar";
import { formatDuration } from "@/lib/labels";
import { tapLight } from "@/lib/haptics";
import {
  buildFullPlaylistSearchQuery,
  FULL_PLAYLIST_DISCOVERY_QUERIES,
  getVideoCategory,
  type VideoPlaylistCollectionKind,
  VIDEO_CATEGORIES,
} from "./videoCategories";
import { useVideoCatalog } from "./useVideoCatalog";
import {
  loadVideoSearchState,
  saveVideoSearchState,
  type VideoSearchState,
} from "./videoSearchState";
import "./VideoCatalogPage.css";

function looksLikeFilmOrCartoonSearch(query: string): boolean {
  const normalized = query.trim().toLowerCase();
  if (normalized.length < 3) return false;
  if (/\b(movie|film|cartoon|animation|multfilm|full movie|full film|full episode|playlist)\b/.test(normalized)) return true;
  return /^(puss in boots|inside out|interstellar|avengers|shrek|kung fu panda|how to train your dragon)\b/.test(normalized);
}

const FILTER_ICONS = {
  all: Grid2X2,
  cartoon: Popcorn,
  movie: Film,
} as const;

const LOCAL_POSTERS: Record<string, string> = {
  I_tRSrPru94: "/assets/play/home-videos/I_tRSrPru94.jpg",
  bq6GBbh3uhU: "/assets/play/home-videos/bq6GBbh3uhU.jpg",
};

const VIDEO_METADATA: Record<string, string> = {
  I_tRSrPru94: "1,09 mln ko‘rish · 1 yil oldin",
  bq6GBbh3uhU: "2,48 mln ko‘rish · 1 yil oldin",
};

export function createRefreshPlaylistQueryQueue(collection: VideoPlaylistCollectionKind): string[] {
  const queries = [...FULL_PLAYLIST_DISCOVERY_QUERIES[collection]];
  if (queries.length < 2 || typeof window === "undefined") return queries;

  const storageKey = `englishai.video.playlist-query-start:${collection}`;
  let start = Math.floor(Math.random() * queries.length);
  try {
    const previous = Number.parseInt(window.sessionStorage.getItem(storageKey) ?? "", 10);
    if (Number.isInteger(previous) && previous >= 0 && previous < queries.length && previous === start) {
      start = (start + 1) % queries.length;
    }
    window.sessionStorage.setItem(storageKey, String(start));
  } catch {
    // Storage is optional; random rotation still makes a refresh fresh in normal browsers.
  }
  return [...queries.slice(start), ...queries.slice(0, start)];
}

const INITIAL_PLAYLIST_QUERY_COUNT = 4;
const RECENT_PLAYLIST_LIMIT = 24;

function playlistHistoryKey(collection: VideoPlaylistCollectionKind): string {
  return `englishai.video.recent-playlists:${collection}`;
}

function loadRecentPlaylistIds(collection: VideoPlaylistCollectionKind): Set<string> {
  if (typeof window === "undefined") return new Set();
  try {
    const stored = JSON.parse(window.sessionStorage.getItem(playlistHistoryKey(collection)) ?? "[]");
    return new Set(Array.isArray(stored) ? stored.filter((id): id is string => typeof id === "string") : []);
  } catch {
    return new Set();
  }
}

function rememberPlaylistIds(collection: VideoPlaylistCollectionKind, ids: Iterable<string>) {
  if (typeof window === "undefined") return;
  try {
    const current = Array.from(loadRecentPlaylistIds(collection));
    const next = [...new Set([...ids, ...current])].slice(0, RECENT_PLAYLIST_LIMIT);
    window.sessionStorage.setItem(playlistHistoryKey(collection), JSON.stringify(next));
  } catch {
    // Session storage is a preference only. Discovery itself must stay available without it.
  }
}

export function mergeFreshPlaylists(
  existing: readonly VideoPlaylistDto[],
  incoming: readonly VideoPlaylistDto[],
  recentIds: ReadonlySet<string>,
): VideoPlaylistDto[] {
  const next = [...existing];
  const knownIds = new Set(next.map((playlist) => playlist.id));
  const fresh = incoming.filter((playlist) => !knownIds.has(playlist.id) && !recentIds.has(playlist.id));
  // A sparse YouTube result may only contain a playlist the learner has seen before. Keep it as a
  // graceful fallback instead of rendering an empty shelf, but prefer genuinely fresh playlists.
  const candidates = fresh.length > 0 ? fresh : incoming;
  for (const playlist of candidates) {
    if (!knownIds.has(playlist.id)) {
      knownIds.add(playlist.id);
      next.push(playlist);
    }
  }
  return next;
}


export function extractYouTubeVideoId(value: string): string | null {
  const trimmed = value.trim();
  if (/^[A-Za-z0-9_-]{11}$/.test(trimmed)) return trimmed;

  let url: URL;
  try {
    url = new URL(trimmed);
  } catch {
    return null;
  }

  const hostname = url.hostname.toLowerCase().replace(/^www\./, "").replace(/^m\./, "");
  let candidate = "";

  if (hostname === "youtu.be") {
    candidate = url.pathname.split("/").filter(Boolean)[0] ?? "";
  } else if (hostname === "youtube.com" || hostname === "music.youtube.com") {
    if (url.pathname === "/watch") candidate = url.searchParams.get("v") ?? "";
    else {
      const [kind, id] = url.pathname.split("/").filter(Boolean);
      if (["embed", "shorts", "live"].includes(kind)) candidate = id ?? "";
    }
  }

  return /^[A-Za-z0-9_-]{11}$/.test(candidate) ? candidate : null;
}

export function extractYouTubePlaylistId(value: string): string | null {
  let url: URL;
  try {
    url = new URL(value.trim());
  } catch {
    return null;
  }

  const hostname = url.hostname.toLowerCase().replace(/^www\./, "").replace(/^m\./, "");
  if (!["youtube.com", "music.youtube.com"].includes(hostname)) return null;

  const candidate = url.searchParams.get("list") ?? "";
  // YouTube uses several playlist prefixes (PL, UU, OLAK, RD, etc.). Keep the validation
  // permissive enough for official URLs while rejecting arbitrary query-string input.
  return /^[A-Za-z0-9_-]{10,200}$/.test(candidate) ? candidate : null;
}

function waitForOpeningFrame(): Promise<void> {
  return new Promise((resolve) => {
    window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => resolve());
    });
  });
}

/** Pen screen 56: catalog chrome, discovery controls, featured playlist, and video choices. */
export function VideoCatalogPage() {
  const navigate = useNavigate();
  const learnerId = getLearnerId();
  const { consume, openEnergyModal } = useEnergy();
  const [opening, setOpening] = useState(false);
  const [featured, setFeatured] = useState<VideoPlaylistDto | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();
  const category = getVideoCategory(searchParams.get("category"));
  const { items, loading, loadingMore, error: feedError, hasMore, loadMore, retry } = useVideoCatalog(category);
  const loadMoreSentinelRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (typeof api.video.featuredPlaylist !== "function") return;
    let cancelled = false;
    void api.video.featuredPlaylist().then((playlist) => {
      if (!cancelled) setFeatured(playlist);
    }).catch(() => undefined);
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    const sentinel = loadMoreSentinelRef.current;
    if (!sentinel || !hasMore || loading || feedError) return;
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) loadMore();
    }, { rootMargin: "600px 0px" });
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [feedError, hasMore, loadMore, loading]);

  async function openItem(item: VideoFeedItemDto) {
    if (opening) return;
    setOpening(true);
    await waitForOpeningFrame();

    let energy;
    try {
      energy = await consume("video", item.youTubeVideoId);
    } catch {
      setOpening(false);
      return;
    }
    if (energy.outcome === EnergyOutcome.Insufficient) {
      setOpening(false);
      openEnergyModal({
        action: "video",
        resume: () => openItem(item),
      });
      return;
    }

    try {
      const lesson = item.lessonId ? { id: item.lessonId } : await api.video.open(item);
      navigate(`/video/${lesson.id}/play`);
    } catch {
      setOpening(false);
    }
  }

  async function openByUrl(videoId: string) {
    if (opening) return;
    setOpening(true);
    await waitForOpeningFrame();

    let energy;
    try {
      energy = await consume("video", videoId);
    } catch {
      setOpening(false);
      return;
    }
    if (energy.outcome === EnergyOutcome.Insufficient) {
      setOpening(false);
      openEnergyModal({
        action: "video",
        resume: () => openByUrl(videoId),
      });
      return;
    }
    try {
      const lesson = await api.video.openUrl(videoId);
      navigate(`/video/${lesson.id}/play`);
    } catch {
      setOpening(false);
    }
  }

  return (
    <div className="video-catalog" data-testid="video-catalog" data-category={category.id}>
      <VideoCatalogHeader />
      <main className="video-catalog__content">
        <section className="video-catalog__hero" aria-labelledby="video-library-title">
          <h1 id="video-library-title">Tomosha qiling. O‘rganing.</h1>
          <p>Sevimli video, film va multfilmlaringiz bilan ingliz tilini mashq qiling.</p>
        </section>

        <div className="video-catalog__search-row">
          <VideoSearchPanel
            categoryId={category.id}
            learnerId={learnerId}
            onOpen={openByUrl}
            onOpenPlaylist={(playlistId) => navigate(`/video/playlists/${encodeURIComponent(playlistId)}`)}
            onFindFull={(query, collection) => {
              const search = new URLSearchParams({ q: query });
              const targetCollection = collection ?? category.collection;
              if (targetCollection) search.set("kind", targetCollection);
              navigate(`/video/search?${search.toString()}`);
            }}
            disabled={opening}
            className="video-catalog__search"
          />
          <button
            type="button"
            className="video-catalog__import"
            onClick={() => document.getElementById("video-search")?.focus()}
          >
            <LinkIcon size={17} aria-hidden />
            <span>YouTube link qo‘shish</span>
          </button>
        </div>

        <div className="video-catalog__filters" role="group" aria-label="Video kataloglari">
          {VIDEO_CATEGORIES.map((item) => {
            const FilterIcon = FILTER_ICONS[item.id];
            const selected = category.id === item.id;
            return <button
              key={item.id}
              type="button"
              className="video-catalog__filter"
              aria-pressed={selected}
              aria-controls="video-catalog-grid"
              disabled={opening}
              onClick={() => setSearchParams((previous) => {
                const next = new URLSearchParams(previous);
                if (item.id === "all") next.delete("category");
                else next.set("category", item.id);
                return next;
              }, { replace: true })}
            >
              <FilterIcon size={17} aria-hidden />
              <span>{item.label}</span>
            </button>;
          })}
        </div>

        <section className="video-catalog__featured" aria-labelledby="featured-playlist-title">
          <div className={`video-catalog__featured-art${featured?.thumbnailUrl ? "" : " is-fallback"}`} role="img" aria-label={`${featured?.title ?? "Puss in Boots"} playlisti`}>
            {featured?.thumbnailUrl ? <img src={featured.thumbnailUrl} alt="" /> : <div className="video-catalog__featured-fallback"><Film size={52} aria-hidden /><strong>FULL<br />EPISODES</strong><small>10+</small></div>}
            <span>{featured ? `${featured.items.length} ta video` : "Playlist yangilanmoqda"}</span>
          </div>
          <div className="video-catalog__featured-copy">
            <span className="video-catalog__featured-tag">10+ · FULL EPISODES / FILMS</span>
            <h2 id="featured-playlist-title">{featured?.title ?? "To‘liq film va multfilm playlistlari"}</h2>
            <p>{featured
              ? `${featured.channel} · ${featured.items.length} ta video. Playlist ichidagi har bir qismni EnglishAI’da ochib ko‘rishingiz mumkin.`
              : "YouTube’dagi to‘liq film va multfilm playlistlari muntazam yangilanadi."}</p>
            <button type="button" onClick={() => navigate(featured ? `/video/playlists/${encodeURIComponent(featured.id)}` : "/video/search?q=Puss%20in%20Boots")}>
              <ListVideo size={17} aria-hidden />
              Playlistni ochish
            </button>
          </div>
        </section>

        {category.collection ? (
          <FullEpisodePlaylistShelf key={category.collection} collection={category.collection} />
        ) : (
        <section id="video-catalog-grid" className="video-catalog__library" aria-label={`${category.label} videolari`} aria-busy={loading}>
          <div className="video-catalog__library-heading">
            <h2>Siz uchun video darslar</h2>
            <span>A2–B1</span>
          </div>

          {feedError && <div className="video-catalog__notice" role="status"><Icon name="cloud_off" /><span><strong>{category.label} katalogini yuklab bo‘lmadi.</strong> Ulanishni tekshirib, qayta urinib ko‘ring.</span><button type="button" onClick={retry}>Qayta urinish</button></div>}

          {loading && items.length === 0 ? (
            <ModulePageLoader icon="smart_display" accent="purple" embedded />
          ) : (
            <>
              <div className="video-catalog__grid">
                {items.map((video, index) => (
                  <VideoCard key={`${video.youTubeVideoId}-${index}`} video={video} disabled={opening} onOpen={() => void openItem(video)} />
                ))}
              </div>

              {items.length === 0 && !feedError && (
                <div className="video-catalog__empty" role="status">
                  <p>{category.label} katalogida hozircha video topilmadi.</p>
                  <button type="button" onClick={retry}>Qayta urinish</button>
                </div>
              )}
              <div ref={loadMoreSentinelRef} className="video-catalog__pagination" aria-live="polite">
                {loadingMore ? <Spinner /> : null}
              </div>
            </>
          )}
        </section>
        )}
      </main>
      <VideoCatalogMobileNav />

      {opening && createPortal(
        <div className="video-catalog__opening" role="status" aria-live="polite" aria-label={uz.videoCatalog.opening}>
          <Spinner />
          <p className="font-body-md text-white">{uz.videoCatalog.opening}</p>
        </div>,
        document.body,
      )}
    </div>
  );
}

function FullEpisodePlaylistShelf({ collection }: {
  collection: VideoPlaylistCollectionKind;
}) {
  const navigate = useNavigate();
  const [queries] = useState(() => createRefreshPlaylistQueryQueue(collection));
  const [playlists, setPlaylists] = useState<VideoPlaylistDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const queryIndexRef = useRef(0);
  const requestRef = useRef(0);
  const inFlightRef = useRef(false);
  const loadMoreSentinelRef = useRef<HTMLDivElement | null>(null);

  const loadNextPlaylistPage = useCallback((reset = false) => {
    if (inFlightRef.current || queries.length === 0) return;
    if (reset) queryIndexRef.current = 0;

    inFlightRef.current = true;
    const request = ++requestRef.current;
    // A single YouTube query can return only one repetitive result. The first shelf load therefore
    // combines four different full-film/full-episode searches, so the desktop's four-card row is
    // populated with genuinely different playlist candidates immediately.
    const queryCount = reset ? Math.min(INITIAL_PLAYLIST_QUERY_COUNT, queries.length) : 1;
    const pageQueries = Array.from({ length: queryCount }, () => {
      const query = queries[queryIndexRef.current % queries.length];
      queryIndexRef.current += 1;
      return query;
    });
    setLoading(true);
    setError(false);
    if (reset) setPlaylists([]);

    void Promise.all(pageQueries.map(query => api.video.playlistSearch(query)))
      .then((pages) => {
        if (request !== requestRef.current) return;
        setPlaylists((previous) => {
          const base = reset ? [] : previous;
          const incoming = pages.flatMap(page => page.items);
          const next = mergeFreshPlaylists(base, incoming, loadRecentPlaylistIds(collection));
          rememberPlaylistIds(collection, next.map(playlist => playlist.id));
          return next;
        });
      })
      .catch(() => {
        if (request === requestRef.current) setError(true);
      })
      .finally(() => {
        if (request === requestRef.current) {
          inFlightRef.current = false;
          setLoading(false);
        }
      });
  }, [collection, queries]);

  useEffect(() => {
    loadNextPlaylistPage(true);
    return () => {
      requestRef.current += 1;
      inFlightRef.current = false;
    };
  }, [loadNextPlaylistPage]);

  useEffect(() => {
    const sentinel = loadMoreSentinelRef.current;
    if (!sentinel || loading || error || playlists.length === 0) return;
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting) loadNextPlaylistPage();
    }, { rootMargin: "500px 0px" });
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [error, loadNextPlaylistPage, loading, playlists.length]);

  const retry = () => loadNextPlaylistPage(true);
  const title = collection === "cartoon" ? "10+ yosh uchun multfilm playlistlari" : "Kino playlistlari";
  const description = collection === "cartoon"
    ? "Puss in Boots singari, juda kichik bolalar uchun bo‘lmagan to‘liq inglizcha qismlar."
    : "Interstellar va Avengers kabi mavzularga yaqin uzun formatdagi inglizcha YouTube playlistlari.";

  return <section id="video-catalog-grid" className="video-catalog__playlist-shelf" aria-label={title} aria-busy={loading}>
    <div className="video-catalog__library-heading">
      <div>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      <span>FULL EPISODES</span>
    </div>
    {loading && playlists.length === 0 ? <ModulePageLoader icon="playlist_play" accent="purple" embedded /> : error ? (
      <div className="video-catalog__notice" role="status"><Icon name="cloud_off" /><span><strong>Playlistlarni yuklab bo‘lmadi.</strong> Ulanishni tekshirib, qayta urinib ko‘ring.</span><button type="button" onClick={retry}>Qayta urinish</button></div>
    ) : playlists.length ? (
      <div className="video-catalog__playlist-grid">
        {playlists.map((playlist) => <button
          key={playlist.id}
          type="button"
          className="video-catalog__playlist-card"
          onClick={() => navigate(`/video/playlists/${encodeURIComponent(playlist.id)}`)}
        >
          {playlist.thumbnailUrl ? <img src={playlist.thumbnailUrl} alt="" /> : <span className="video-catalog__playlist-card-placeholder"><Film size={28} aria-hidden /></span>}
          <span>{playlist.items.length} qism · {formatPlaylistDuration(playlist.totalDurationSeconds)}</span>
          <strong>{playlist.title}</strong>
          <em>{playlist.channel}</em>
          <small>Playlistni ochish <ListVideo size={15} aria-hidden /></small>
        </button>)}
        <div ref={loadMoreSentinelRef} className="video-catalog__playlist-pagination" aria-live="polite">
          {loading ? <Spinner /> : null}
        </div>
      </div>
    ) : (
      <div className="video-catalog__empty" role="status">
        <p>Tekshirilgan to‘liq playlist hozircha topilmadi. Qisqa parchalarni full episode deb ko‘rsatmaymiz.</p>
        <button type="button" onClick={retry}>Qayta urinish</button>
      </div>
    )}
  </section>;
}

function formatPlaylistDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return hours ? `${hours} soat${minutes ? ` ${minutes} daq` : ""}` : `${minutes} daq`;
}

export function YouTubeUrlPanel({
  onOpen,
  disabled = false,
}: {
  onOpen: (videoId: string) => Promise<void>;
  disabled?: boolean;
}) {
  const [isOpen, setIsOpen] = useState(() => {
    try { return window.sessionStorage.getItem("englishai.video.url-panel-open") === "true"; } catch { return false; }
  });
  const [url, setUrl] = useState("");
  const [error, setError] = useState<string | null>(null);

  function toggle() {
    const next = !isOpen;
    setIsOpen(next);
    try { window.sessionStorage.setItem("englishai.video.url-panel-open", String(next)); } catch { /* storage is optional */ }
  }

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const videoId = extractYouTubeVideoId(url);
    if (!videoId) {
      setError("To‘g‘ri YouTube video havolasini kiriting.");
      return;
    }
    setError(null);
    await onOpen(videoId);
  }

  return (
    <section className="video-url-panel" aria-labelledby="video-url-title">
      <button type="button" aria-expanded={isOpen} aria-controls="video-url-panel-content" onClick={toggle}>
        <span id="video-url-title">YouTube URL orqali ochish</span>
      </button>
      {isOpen && <div id="video-url-panel-content">
        <form onSubmit={(event) => void submit(event)} noValidate>
          <label htmlFor="youtube-video-url">YouTube video URL</label>
          <input id="youtube-video-url" type="url" value={url} onChange={(event) => { setUrl(event.target.value); setError(null); }} disabled={disabled} />
          <button type="submit" disabled={disabled || !url.trim()}>Videoni ochish</button>
        </form>
        {error && <p role="alert">{error}</p>}
      </div>}
    </section>
  );
}

/** Screen 56 card: Pen's quiet border, local reference poster where available, and real channel identity. */
function VideoCard({ video, disabled, onOpen }: { video: VideoFeedItemDto; disabled: boolean; onOpen: () => void }) {
  const [menuOpen, setMenuOpen] = useState(false);
  const [copied, setCopied] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const moreRef = useRef<HTMLButtonElement>(null);
  const poster = LOCAL_POSTERS[video.youTubeVideoId] ?? `https://i.ytimg.com/vi/${video.youTubeVideoId}/hqdefault.jpg`;

  useEffect(() => {
    if (!menuOpen) return;
    const close = (event: PointerEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) setMenuOpen(false);
    };
    document.addEventListener("pointerdown", close);
    return () => document.removeEventListener("pointerdown", close);
  }, [menuOpen]);

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(`https://www.youtube.com/watch?v=${video.youTubeVideoId}`);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  return (
    <article className="video-catalog__card">
      <button type="button" aria-label={`${video.title} — ijro`} disabled={disabled} onClick={() => { tapLight(); onOpen(); }} className="video-catalog__video">
        <div className="video-catalog__thumbnail">
          <img
            src={poster}
            alt=""
            loading="lazy"
            onError={(event) => {
              const image = event.currentTarget;
              if (!image.dataset.fallback) {
                image.dataset.fallback = "1";
                image.src = `https://i.ytimg.com/vi/${video.youTubeVideoId}/mqdefault.jpg`;
              }
            }}
          />
          <span className="video-catalog__duration">{formatDuration(video.durationSeconds)}</span>
        </div>
      </button>
      <div className="video-catalog__card-body">
        <YouTubeChannelAvatar channel={video.channel} url={video.channelAvatarUrl} />
        <div className="video-catalog__details">
          <h3><button type="button" className="video-catalog__title" disabled={disabled} onClick={() => { tapLight(); onOpen(); }}>{video.title}</button></h3>
          <span className="video-catalog__channel">{video.channel}</span>
          <span className="video-catalog__views">{VIDEO_METADATA[video.youTubeVideoId] ?? "Ingliz tili darsi"}</span>
        </div>
        <div ref={menuRef} className="video-catalog__more" onKeyDown={(event) => {
          if (event.key === "Escape") { setMenuOpen(false); moreRef.current?.focus(); }
        }}>
          <button ref={moreRef} type="button" aria-label={`${video.title} — amallar`} aria-expanded={menuOpen} onClick={() => { setMenuOpen(!menuOpen); setCopied(false); }}><EllipsisVertical size={20} aria-hidden /></button>
          {menuOpen && <div className="video-catalog__menu">
            <button type="button" onClick={() => void copyLink()}><LinkIcon size={16} aria-hidden />{copied ? "Nusxalandi" : "Havolani nusxalash"}</button>
            <a href={`https://www.youtube.com/watch?v=${video.youTubeVideoId}`} target="_blank" rel="noopener noreferrer"><ExternalLink size={16} aria-hidden />YouTube’da ochish</a>
          </div>}
        </div>
      </div>
    </article>
  );
}

/**
 * Search box (replaces the old paste-URL panel). General learning categories show live
 * English-video recommendations. Multfilm and Kino intentionally never show individual
 * video suggestions: a submitted title always opens duration-checked full playlists.
 */
function VideoSearchPanel({
  categoryId,
  learnerId,
  onOpen,
  onOpenPlaylist,
  onFindFull,
  className,
  disabled = false,
}: {
  categoryId: string;
  learnerId: string;
  onOpen: (videoId: string) => Promise<void>;
  onOpenPlaylist: (playlistId: string) => void;
  onFindFull: (query: string, collection?: VideoPlaylistCollectionKind) => void;
  className?: string;
  disabled?: boolean;
}) {
  const restoredRef = useRef<VideoSearchState | null>(null);
  if (restoredRef.current === null) restoredRef.current = loadVideoSearchState(learnerId);
  const restored = restoredRef.current;
  const previousCategoryRef = useRef(categoryId);
  const fullPlaylistCollection: VideoPlaylistCollectionKind | null =
    categoryId === "cartoon" || categoryId === "movie" ? categoryId : null;

  const [query, setQuery] = useState(restored.query);
  const [results, setResults] = useState<VideoFeedItemDto[]>(restored.results);
  const [playlistResults, setPlaylistResults] = useState<VideoPlaylistDto[]>([]);
  const [open, setOpen] = useState(
    !fullPlaylistCollection && restored.isOpen && restored.query.trim().length >= 2,
  );
  const [selectedVideoId, setSelectedVideoId] = useState<string | null>(restored.selectedVideoId);
  const [loading, setLoading] = useState(false);
  const [playlistLoading, setPlaylistLoading] = useState(false);
  const [loadingMoreResults, setLoadingMoreResults] = useState(false);
  const [searchCursor, setSearchCursor] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const debounceRef = useRef<number | null>(null);
  const reqIdRef = useRef(0);
  const resultsScrollRef = useRef<HTMLDivElement | null>(null);
  const resultsSentinelRef = useRef<HTMLDivElement | null>(null);
  const loadingMoreResultsRef = useRef(false);
  const resultsScrollTopRef = useRef(restored.resultsScrollTop);

  useEffect(() => () => {
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    reqIdRef.current += 1;
  }, []);

  useEffect(() => {
    if (previousCategoryRef.current === categoryId) return;
    previousCategoryRef.current = categoryId;
    reqIdRef.current += 1;
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    setQuery("");
    setOpen(false);
    setResults([]);
    setPlaylistResults([]);
    setSearchCursor(null);
    setSelectedVideoId(null);
    setError(null);
    setLoading(false);
    setPlaylistLoading(false);
  }, [categoryId]);

  const currentState = useCallback((): VideoSearchState => ({
    query,
    results,
    isOpen: open,
    selectedVideoId,
    resultsScrollTop: resultsScrollTopRef.current,
    pageScrollY: window.scrollY,
  }), [open, query, results, selectedVideoId]);

  useEffect(() => {
    saveVideoSearchState(learnerId, currentState());
  }, [currentState, learnerId]);

  useEffect(() => {
    if (!open) return;
    const frame = window.requestAnimationFrame(() => {
      if (resultsScrollRef.current) {
        resultsScrollRef.current.scrollTop = resultsScrollTopRef.current;
      }
      if (restored.pageScrollY > 0) window.scrollTo({ top: restored.pageScrollY });
    });
    return () => window.cancelAnimationFrame(frame);
  }, [open, restored.pageScrollY, restored.selectedVideoId]);

  function runSearch(term: string) {
    const q = term.trim();
    const reqId = ++reqIdRef.current;
    if (fullPlaylistCollection || q.length < 2 || extractYouTubeVideoId(q) || extractYouTubePlaylistId(q)) {
      setOpen(false);
      setResults([]);
      setSelectedVideoId(null);
      setLoading(false);
      setSearchCursor(null);
      return;
    }
    setOpen(true);
    setLoading(true);
    setError(null);
    void api.video
      .search(q, null, 12)
      .then((page) => {
        // Ignore stale responses for an older query.
        if (reqIdRef.current !== reqId) return;
        setResults(page.items);
        setSearchCursor(page.nextCursor);
        setLoading(false);
      })
      .catch(() => {
        if (reqIdRef.current !== reqId) return;
        setLoading(false);
        setError(uz.videoCatalog.searchError);
      });
  }

  function runPlaylistSearch(term: string) {
    const title = term.trim();
    const request = ++reqIdRef.current;
    if (!fullPlaylistCollection || title.length < 2) {
      setOpen(false);
      setPlaylistResults([]);
      setPlaylistLoading(false);
      return;
    }

    setOpen(true);
    setPlaylistLoading(true);
    setError(null);
    void api.video.playlistSearch(buildFullPlaylistSearchQuery(title, fullPlaylistCollection))
      .then((result) => {
        if (reqIdRef.current !== request) return;
        setPlaylistResults(result.items);
        setPlaylistLoading(false);
      })
      .catch(() => {
        if (reqIdRef.current !== request) return;
        setPlaylistResults([]);
        setPlaylistLoading(false);
        setError("Playlist qidiruviga ulanib bo‘lmadi.");
      });
  }

  const loadMoreResults = useCallback(() => {
    const q = query.trim();
    if (fullPlaylistCollection || !searchCursor || loadingMoreResultsRef.current || q.length < 2) return;

    loadingMoreResultsRef.current = true;
    setLoadingMoreResults(true);
    setError(null);
    const reqId = reqIdRef.current;

    void api.video.search(q, searchCursor, 12)
      .then((page) => {
        if (reqIdRef.current !== reqId) return;
        setResults((current) => {
          const seen = new Set(current.map((item) => item.youTubeVideoId));
          return [...current, ...page.items.filter((item) => !seen.has(item.youTubeVideoId))];
        });
        setSearchCursor(page.nextCursor);
      })
      .catch(() => {
        if (reqIdRef.current === reqId) setError(uz.videoCatalog.searchError);
      })
      .finally(() => {
        loadingMoreResultsRef.current = false;
        setLoadingMoreResults(false);
      });
  }, [fullPlaylistCollection, query, searchCursor]);

  useEffect(() => {
    const sentinel = resultsSentinelRef.current;
    const scrollRoot = resultsScrollRef.current;
    if (fullPlaylistCollection || !open || !sentinel || !scrollRoot || !searchCursor) return;

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) loadMoreResults();
      },
      { root: scrollRoot, rootMargin: "300px 0px" },
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [fullPlaylistCollection, loadMoreResults, open, searchCursor]);

  function onType(value: string) {
    setQuery(value);
    setSelectedVideoId(null);
    reqIdRef.current += 1;
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    if (fullPlaylistCollection) {
      setResults([]);
      setSearchCursor(null);
      setLoading(false);
      setError(null);
      debounceRef.current = window.setTimeout(() => runPlaylistSearch(value), 300);
      return;
    }
    debounceRef.current = window.setTimeout(() => runSearch(value), 250);
  }

  function submitSearch() {
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    const videoId = extractYouTubeVideoId(query);
    if (videoId) {
      void onOpen(videoId);
      return;
    }
    const playlistId = extractYouTubePlaylistId(query);
    if (playlistId) {
      onOpenPlaylist(playlistId);
      return;
    }
    if (!query.trim()) return;
    // Multfilm/Kino search has a specific contract: title -> full collection.
    // Do this before the generic clip-search fallback.
    if (fullPlaylistCollection) {
      onFindFull(query.trim(), fullPlaylistCollection);
      return;
    }
    if (looksLikeFilmOrCartoonSearch(query)) {
      onFindFull(query.trim());
      return;
    }
    if (/^https?:\/\//i.test(query.trim())) {
      setError("To‘g‘ri YouTube video havolasini kiriting.");
      setOpen(false);
      return;
    }
    runSearch(query);
  }

  async function pick(item: VideoFeedItemDto) {
    const state: VideoSearchState = {
      query,
      results,
      isOpen: true,
      selectedVideoId: item.youTubeVideoId,
      resultsScrollTop: resultsScrollRef.current?.scrollTop ?? resultsScrollTopRef.current,
      pageScrollY: window.scrollY,
    };
    resultsScrollTopRef.current = state.resultsScrollTop;
    setSelectedVideoId(item.youTubeVideoId);
    saveVideoSearchState(learnerId, state);
    await onOpen(item.youTubeVideoId);
  }

  return (
    <div className={["video-search-panel relative", className ?? ""].join(" ")}>
      <div className="relative">
        <form className="video-search-panel__form" role="search" onSubmit={(event) => { event.preventDefault(); submitSearch(); }}>
          <label htmlFor="video-search" className="sr-only">Video qidirish</label>
          <input
            id="video-search"
            type="search"
            value={query}
            onChange={(e) => onType(e.target.value)}
            onFocus={() => {
              if (fullPlaylistCollection && query.trim().length >= 2) {
                runPlaylistSearch(query);
              } else if (!fullPlaylistCollection && query.trim().length >= 2 && !extractYouTubeVideoId(query) && !extractYouTubePlaylistId(query)) {
                setOpen(true);
              }
            }}
            onKeyDown={(e) => {
              if (e.key === "Escape") setOpen(false);
            }}
            placeholder={fullPlaylistCollection === "cartoon"
              ? "Multfilm nomini yozing..."
              : fullPlaylistCollection === "movie"
                ? "Kino nomini yozing..."
                : "Film, multfilm yoki mavzu nomi..."}
            disabled={disabled}
            aria-describedby="video-search-hint"
            className="video-search-panel__input"
          />
          <button
            type="submit"
            aria-label={extractYouTubeVideoId(query)
              ? "Videoni ochish"
              : extractYouTubePlaylistId(query)
                ? "Playlistni ochish"
                : fullPlaylistCollection
                  ? "To‘liq playlist qidirish"
                  : "Qidirish"}
            disabled={disabled}
            className="video-search-panel__button"
          >
            <Search size={22} aria-hidden />
          </button>
        </form>

      {/* Search results stay in normal flow. Collection categories render only full-playlist cards. */}
      {open && (
        <motion.div
          initial={{ opacity: 0, y: -8, scale: 0.98 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          transition={{ type: "spring", stiffness: 280, damping: 26 }}
          className="video-search-modal"
        >
          <div className="video-search-modal__header">
            <div className="video-search-modal__title">
              <Icon name="auto_awesome" className="text-[20px]" />
              <span className="line-clamp-2 min-w-0 break-words font-duo text-[15px] font-extrabold">
                {query ? `“${query}”` : ""} {fullPlaylistCollection ? "to‘liq playlistlar" : uz.videoCatalog.searchTitle}
              </span>
            </div>
            <button
              type="button"
              onClick={() => setOpen(false)}
              className="video-search-modal__close"
              aria-label="Qidiruv natijalarini yopish"
            >
              <Icon name="close" className="text-[18px]" />
            </button>
          </div>

          <div
            ref={resultsScrollRef}
            onScroll={(event) => {
              resultsScrollTopRef.current = event.currentTarget.scrollTop;
            }}
            className="video-search-modal__body"
          >
            {fullPlaylistCollection ? (
              playlistLoading ? (
                <div className="flex flex-col items-center gap-3 py-xl text-ea-text" role="status">
                  <Spinner />
                  <span className="font-caption text-caption">To‘liq playlistlar qidirilmoqda...</span>
                </div>
              ) : playlistResults.length === 0 ? (
                <p className="py-xl text-center font-body-md text-ea-muted">
                  {error ?? "Tekshirilgan to‘liq playlist topilmadi."}
                </p>
              ) : (
                <div className="video-search-modal__grid">
                  {playlistResults.map((playlist, index) => (
                    <PlaylistSearchResultRow
                      key={playlist.id}
                      playlist={playlist}
                      index={index}
                      onPick={() => onOpenPlaylist(playlist.id)}
                    />
                  ))}
                </div>
              )
            ) : loading && results.length === 0 ? (
              <div className="flex flex-col items-center gap-3 py-xl text-ea-text" role="status">
                <Spinner />
                <span className="font-caption text-caption">{uz.videoCatalog.searchLoading}</span>
              </div>
            ) : results.length === 0 ? (
              <p className="py-xl text-center font-body-md text-ea-muted">
                {error ?? uz.videoCatalog.searchEmpty}
              </p>
            ) : (
              <div className="video-search-modal__grid">
                {results.map((item, i) => (
                  <SearchResultRow
                    key={`${item.youTubeVideoId}-${i}`}
                    item={item}
                    index={i}
                    selected={item.youTubeVideoId === selectedVideoId}
                    onPick={() => void pick(item)}
                  />
                ))}
              </div>
            )}
            {!fullPlaylistCollection && results.length > 0 && (
              <div ref={resultsSentinelRef} className="video-search-modal__pagination" aria-live="polite">
                {loadingMoreResults ? <Spinner /> : searchCursor ? null : <span><Icon name="check_circle" filled /> Natijalar tugadi</span>}
              </div>
            )}
          </div>
        </motion.div>
      )}
      </div>

      <p id="video-search-hint" className="sr-only">{fullPlaylistCollection
        ? "Nomni yozib qidiring. Faqat to‘liq playlistlar ko‘rsatiladi, alohida video ko‘rsatilmaydi."
        : "Video mavzusini yoki YouTube havolasini kiriting."}</p>
      {error && <p className="video-search-panel__error" role="alert">{error}</p>}
    </div>
  );
}

/** One result inside the recommendation modal. */
export function SearchResultRow({
  item,
  index,
  selected,
  onPick,
}: {
  item: VideoFeedItemDto;
  index: number;
  selected: boolean;
  onPick: () => void;
}) {
  return (
    <motion.button
      data-video-search-id={item.youTubeVideoId}
      aria-label={selected ? `${item.title}. ${uz.videoCatalog.selectedVideo}` : item.title}
      aria-pressed={selected}
      type="button"
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.03, type: "spring", stiffness: 260, damping: 22 }}
      onClick={() => {
        tapLight();
        onPick();
      }}
      className={`video-search-result${selected ? " is-selected" : ""}`}
    >
      <div className="video-search-result__thumbnail">
        <img
          src={`https://i.ytimg.com/vi/${item.youTubeVideoId}/mqdefault.jpg`}
          alt={item.title}
          className="video-search-result__image"
          loading="lazy"
          onError={(e) => {
            const img = e.currentTarget;
            if (!img.dataset.fallback) {
              img.dataset.fallback = "1";
              img.src = `https://i.ytimg.com/vi/${item.youTubeVideoId}/hqdefault.jpg`;
            }
          }}
        />
        <span className="video-search-result__play-layer">
          <span className="video-search-result__play">
            <Icon name="play_arrow" filled />
          </span>
        </span>
        {item.hasClosedCaptions && (
          <span
            aria-label={uz.videoCatalog.closedCaptionsAvailable}
            className="video-search-result__cc"
          >
            CC
          </span>
        )}
        {selected && (
          <span className="video-search-result__selected">
            <Icon name="check" />
          </span>
        )}
      </div>
      <div className="video-search-result__body">
        <h3>
          {item.title}
        </h3>
        <div className="video-search-result__channel">
          <YouTubeChannelAvatar channel={item.channel} url={item.channelAvatarUrl} />
          {item.channel}
        </div>
      </div>
    </motion.button>
  );
}

function PlaylistSearchResultRow({
  playlist,
  index,
  onPick,
}: {
  playlist: VideoPlaylistDto;
  index: number;
  onPick: () => void;
}) {
  return (
    <motion.button
      data-playlist-search-id={playlist.id}
      aria-label={`${playlist.title} — playlistni ochish`}
      type="button"
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay: index * 0.03, type: "spring", stiffness: 260, damping: 22 }}
      onClick={() => {
        tapLight();
        onPick();
      }}
      className="video-search-result"
    >
      <div className="video-search-result__thumbnail">
        <img
          src={playlist.thumbnailUrl ?? "/assets/play/youtube-playlist/eCgNH.png"}
          alt=""
          className="video-search-result__image"
          loading="lazy"
        />
      </div>
      <div className="video-search-result__body">
        <h3>{playlist.title}</h3>
        <div className="video-search-result__channel">
          <ListVideo size={16} aria-hidden />
          {playlist.items.length} qism · {formatPlaylistDuration(playlist.totalDurationSeconds)}
        </div>
        <div className="video-search-result__channel">{playlist.channel}</div>
      </div>
    </motion.button>
  );
}
