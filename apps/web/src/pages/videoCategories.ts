export type VideoPlaylistCollectionKind = "cartoon" | "movie";

export interface VideoCategory {
  id: "all" | "cartoon" | "movie";
  label: string;
  query: string;
  collection: VideoPlaylistCollectionKind | null;
}

export const VIDEO_CATEGORIES: readonly VideoCategory[] = [
  { id: "all", label: "Barchasi", query: "english learning videos", collection: null },
  // Full collections are intentionally selected for older learners, rather than nursery
  // rhymes or pre-school playlists. The provider still checks every returned duration.
  { id: "cartoon", label: "Multfilm", query: "Puss in Boots full episodes English", collection: "cartoon" },
  { id: "movie", label: "Kino", query: "Interstellar Avengers full movie English playlist", collection: "movie" },
] as const;

/**
 * Query rotations are deliberately broad and age-appropriate. A new first query is chosen for
 * every refresh and the catalog keeps cycling them as the learner scrolls for more playlists.
 */
export const FULL_PLAYLIST_DISCOVERY_QUERIES: Record<VideoPlaylistCollectionKind, readonly string[]> = {
  cartoon: [
    "Puss in Boots full episodes English",
    "Inside Out full episodes English",
    "Shrek full episodes English",
    "Kung Fu Panda full episodes English",
    "How to Train Your Dragon full episodes English",
    "The Incredibles full episodes English",
    "Spider-Man Into the Spider-Verse full episodes English",
    "Big Hero 6 full episodes English",
  ],
  movie: [
    "Interstellar full movie English playlist",
    "Captain America full movie English playlist",
    "Avengers full movie English playlist",
    "The Martian full movie English playlist",
    "Harry Potter full movie English playlist",
    "Pirates of the Caribbean full movie English playlist",
    "The Hunger Games full movie English playlist",
    "Jurassic Park full movie English playlist",
  ],
};

export function getVideoCategory(id: string | null): VideoCategory {
  return VIDEO_CATEGORIES.find(category => category.id === id) ?? VIDEO_CATEGORIES[0];
}

export function parseVideoPlaylistCollectionKind(value: string | null): VideoPlaylistCollectionKind | null {
  return value === "cartoon" || value === "movie" ? value : null;
}

/**
 * Preserve the learner's title while making the full-content intent explicit.
 * This keeps trailers and isolated clips out of Multfilm/Kino title searches.
 */
export function buildFullPlaylistSearchQuery(
  title: string,
  collection: VideoPlaylistCollectionKind | null,
): string {
  const normalized = title.trim();
  if (!normalized || collection === null) return normalized;
  return collection === "cartoon"
    ? `${normalized} full episodes English`
    : `${normalized} full movie English playlist`;
}
