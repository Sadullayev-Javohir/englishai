/**
 * Weekday index for an ISO date (`2026-07-28`), Monday-first (0 = Du … 6 = Ya).
 * `uz.progress.stats.weekdays` is stored in the same order, so the two line up.
 */
export function mondayIndex(day: string): number {
  const [year, month, date] = day.split("-").map(Number);
  return (new Date(year, month - 1, date).getDay() + 6) % 7;
}
