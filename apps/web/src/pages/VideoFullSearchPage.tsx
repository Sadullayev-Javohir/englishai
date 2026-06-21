import { useEffect, useRef, useState } from "react";
import { Film, Link as LinkIcon, Search } from "lucide-react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { api } from "@/api/client";
import type { VideoPlaylistDto } from "@/api/types";
import { VideoCatalogHeader, VideoCatalogMobileNav } from "@/components/video/VideoCatalogChrome";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import {
  buildFullPlaylistSearchQuery,
  parseVideoPlaylistCollectionKind,
} from "./videoCategories";
// Search / playlist screens share the catalog-owned header and mobile dock from Pen screen 56.
import "./VideoCatalogPage.css";
import "./VideoFullSearchPage.css";

export function VideoFullSearchPage() {
  const navigate = useNavigate();
  const [params, setParams] = useSearchParams();
  const requested = params.get("q")?.trim() ?? "";
  const collection = parseVideoPlaylistCollectionKind(params.get("kind"));
  const playlistQuery = buildFullPlaylistSearchQuery(requested, collection);
  const [query, setQuery] = useState(requested);
  const [items, setItems] = useState<VideoPlaylistDto[] | null>(null);
  const [failed, setFailed] = useState(false);
  const requestRef = useRef(0);

  useEffect(() => {
    setQuery(requested);
    if (!requested) { setItems(null); setFailed(false); return; }
    const request = ++requestRef.current;
    setItems(null);
    setFailed(false);
    void api.video.playlistSearch(playlistQuery)
      .then((result) => { if (request === requestRef.current) setItems(result.items); })
      .catch(() => { if (request === requestRef.current) { setItems([]); setFailed(true); } });
  }, [playlistQuery, requested]);

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const next = query.trim();
    if (!next) return;
    setParams(collection ? { q: next, kind: collection } : { q: next });
  }

  return (
    <div className="video-full-search" data-testid="video-full-search">
      <VideoCatalogHeader />
      <main className="video-full-search__content">
        {items === null && requested ? <ModulePageLoader icon="movie" accent="purple" embedded /> : items?.length ? (
          <>
            <div className="video-full-search__intro">
              <span>TO‘LIQ KONTENT</span>
              <h1>Topilgan playlistlar</h1>
              <p>Qisqa parcha emas, davomiyligi tekshirilgan film yoki epizodlar to‘plamini tanlang.</p>
            </div>
            <form className="video-full-search__form" role="search" onSubmit={submit}>
              <Search size={20} aria-hidden />
              <label className="sr-only" htmlFor="full-video-search">Film yoki multfilm nomi</label>
              <input id="full-video-search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Film yoki multfilm nomi..." />
              <button type="submit">Qidirish</button>
            </form>
            <section className="video-full-search__results" aria-label="To‘liq video playlistlari">
              {items.map((playlist) => <button key={playlist.id} type="button" className="video-full-search__playlist" onClick={() => navigate(`/video/playlists/${encodeURIComponent(playlist.id)}`)}>
                <img src={playlist.thumbnailUrl ?? "/assets/play/youtube-playlist/eCgNH.png"} alt="" />
                <span className="video-full-search__playlist-copy">
                  <small>{playlist.items.length} ta video · {formatLongDuration(playlist.totalDurationSeconds)}</small>
                  <strong>{playlist.title}</strong>
                  <em>{playlist.channel}</em>
                </span>
              </button>)}
            </section>
          </>
        ) : (
          <FullVideoNotFound query={requested} failed={failed} value={query} onChange={setQuery} onSubmit={submit} onCatalog={() => navigate("/video")} />
        )}
      </main>
      <VideoCatalogMobileNav />
    </div>
  );
}

export function FullVideoNotFound({
  query,
  failed,
  value,
  onChange,
  onSubmit,
  onCatalog,
}: {
  query: string;
  failed: boolean;
  value: string;
  onChange: (value: string) => void;
  onSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  onCatalog: () => void;
}) {
  return <>
    <div className="video-full-search__empty-intro">
      <h1>To‘liq video topilmadi</h1>
      <p>{failed
        ? "YouTube qidiruviga hozir ulanib bo‘lmadi. Qayta urinib ko‘ring."
        : `“${query || "Bu nom"}” bo‘yicha tekshirilgan to‘liq film yoki epizod playlisti topilmadi. Qisqa parchalarni “full” deb ko‘rsatmaymiz.`}</p>
    </div>
    <section className="video-full-search__empty" aria-labelledby="full-video-empty-title">
      <Film size={48} aria-hidden />
      <h2 id="full-video-empty-title">Boshqa nom bilan sinab ko‘ring</h2>
      <p>Asl inglizcha nomni yozing yoki rasmiy YouTube playlistining havolasini qo‘shing.</p>
      <form className="video-full-search__form video-full-search__form--empty" role="search" onSubmit={onSubmit}>
        <Search size={18} aria-hidden />
        <label className="sr-only" htmlFor="empty-full-video-search">Qidiruvni o‘zgartirish</label>
        <input id="empty-full-video-search" value={value} onChange={(event) => onChange(event.target.value)} placeholder="Masalan: Puss in Boots" />
        <button type="submit">Qidiruvni o‘zgartirish</button>
      </form>
      <button type="button" className="video-full-search__link" onClick={onCatalog}><LinkIcon size={17} aria-hidden /> YouTube link qo‘shish</button>
    </section>
  </>;
}

export function formatLongDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return hours ? `${hours} soat ${minutes ? `${minutes} daq` : ""}`.trim() : `${minutes} daq`;
}
