import type { LeaderboardEntryDto } from "@/api/types";

const PREVIEW_USERS = [
  { learnerId: "preview-dilnoza", displayName: "Dilnoza Abdurahmonova", score: 2480, isPremium: true },
  { learnerId: "preview-muhammadali", displayName: "Muhammadali Karimov", score: 2215, isPremium: false },
  { learnerId: "preview-zuhra", displayName: "Zuhra Rasulova", score: 2070, isPremium: true },
  { learnerId: "preview-azizbek", displayName: "Azizbek Toshpo‘latov", score: 1840, isPremium: false },
  { learnerId: "preview-madina", displayName: "Madina Yoqubova", score: 1675, isPremium: false },
  { learnerId: "preview-shahzod", displayName: "Shahzod Qodirov", score: 1490, isPremium: true },
  { learnerId: "preview-mohira", displayName: "Mohira Abdullayeva", score: 1325, isPremium: false },
  { learnerId: "preview-sardor", displayName: "Sardor Ismoilov", score: 1160, isPremium: false },
  { learnerId: "preview-nilufar", displayName: "Nilufar Ergasheva", score: 980, isPremium: true },
  { learnerId: "preview-umar", displayName: "Umar Xolmatov", score: 845, isPremium: false },
] as const;

export function withLeaderboardPreview(entries: LeaderboardEntryDto[]) {
  if (!import.meta.env.DEV || entries.length >= 10) return entries;

  const existingIds = new Set(entries.map((entry) => entry.learnerId));
  const previewEntries: LeaderboardEntryDto[] = PREVIEW_USERS
    .filter((entry) => !existingIds.has(entry.learnerId))
    .map((entry) => ({
      ...entry,
      rank: 0,
      pictureUrl: null,
      isCurrentUser: false,
    }));

  const currentUser = entries.find((entry) => entry.isCurrentUser);
  const ranked = [...entries, ...previewEntries]
    .sort((left, right) => right.score - left.score || left.displayName.localeCompare(right.displayName, "uz"));
  const preview = ranked.slice(0, 10);

  if (currentUser && !preview.some((entry) => entry.learnerId === currentUser.learnerId)) {
    preview[preview.length - 1] = currentUser;
    preview.sort((left, right) => right.score - left.score || left.displayName.localeCompare(right.displayName, "uz"));
  }

  return preview
    .map((entry, index) => ({ ...entry, rank: index + 1 }));
}
