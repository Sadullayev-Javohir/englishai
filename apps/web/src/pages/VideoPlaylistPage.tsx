import { ArrowRight, ListVideo, Play } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { formatDuration } from "@/lib/labels";
import { VideoCatalogHeader, VideoCatalogMobileNav } from "@/components/video/VideoCatalogChrome";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { formatLongDuration } from "./VideoFullSearchPage";
import "./VideoCatalogPage.css";
import "./VideoFullSearchPage.css";

export function VideoPlaylistPage() {
  const { playlistId = "" } = useParams();
  const navigate = useNavigate();
  const { data: playlist, loading, error } = useAsync(() => api.video.playlist(playlistId), [playlistId], Boolean(playlistId));
  const episodePath = (episodeIndex: number) => `/video/playlists/${encodeURIComponent(playlistId)}/${String(episodeIndex + 1).padStart(2, "0")}`;

  return <div className="video-playlist-page" data-testid="video-playlist-page">
    <VideoCatalogHeader />
    <main className="video-playlist-page__content">
      {loading ? <ModulePageLoader icon="playlist_play" accent="purple" embedded /> : playlist ? <>
        <section className="video-playlist-page__hero">
          <img src={playlist.thumbnailUrl ?? "/assets/play/youtube-playlist/eCgNH.png"} alt="" />
          <div>
            <span><ListVideo size={17} aria-hidden /> PLAYLIST</span>
            <h1>{playlist.title}</h1>
            <p>{playlist.channel} · {playlist.items.length} ta video · {formatLongDuration(playlist.totalDurationSeconds)}</p>
            <p className="video-playlist-page__progress">Playlist ichidagi har bir qism alohida EnglishAI mashq sahifasida ochiladi.</p>
            <a href={`https://www.youtube.com/playlist?list=${encodeURIComponent(playlist.id)}`} target="_blank" rel="noopener noreferrer">YouTube’da ochish <ArrowRight size={16} aria-hidden /></a>
          </div>
        </section>
        <section className="video-playlist-page__items" aria-label={`${playlist.title} videolari`}>
          {playlist.items.map((item, index) => <article key={item.youTubeVideoId} className="video-playlist-page__item">
            <button type="button" onClick={() => navigate(episodePath(index))} aria-label={`${item.title} — ijro`} className="video-playlist-page__thumb">
              <img src={item.thumbnailUrl ?? `https://i.ytimg.com/vi/${item.youTubeVideoId}/hqdefault.jpg`} alt="" />
              <span>{formatDuration(item.durationSeconds)}</span><i><Play size={20} fill="currentColor" aria-hidden /></i>
            </button>
            <div><small>{index + 1}-qism</small><h2>{item.title}</h2><p>{item.channel}</p></div>
            <button type="button" className="video-playlist-page__watch" onClick={() => navigate(episodePath(index))}>Ko‘rish <ArrowRight size={17} aria-hidden /></button>
          </article>)}
        </section>
      </> : <section className="video-playlist-page__missing"><h1>Playlist topilmadi</h1><p>{error ? "Playlistni yuklab bo‘lmadi." : "Bu YouTube playlist endi mavjud emas."}</p><button type="button" onClick={() => navigate("/video")}>Video katalogiga qaytish</button></section>}
    </main>
    <VideoCatalogMobileNav />
  </div>;
}
