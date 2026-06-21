import { useEffect, useState } from "react";

/**
 * The audience is in Uzbekistan (UTC+5, no daylight saving), and the backend rolls every daily
 * boundary - the Home "kunlik reja", the streak, and the Free-tier daily Speaking session - over at
 * local midnight (00:00 Tashkent). This returns the current Uzbekistan calendar day as a stable
 * `YYYY-MM-DD` string and re-renders the moment it flips, so a page left open past midnight
 * (e.g. at 23:58) refreshes its daily state instead of showing yesterday's plan until reload.
 */
const UZ_OFFSET_MS = 5 * 60 * 60 * 1000;

function uzbekistanDayKey(): string {
  // Shift "now" into UTC+5, then read the date parts off the shifted UTC clock.
  return new Date(Date.now() + UZ_OFFSET_MS).toISOString().slice(0, 10);
}

export function useLocalDay(): string {
  const [day, setDay] = useState(uzbekistanDayKey);

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout>;

    const schedule = () => {
      // Milliseconds until the next local midnight: how far the UTC+5 clock is into its current day.
      const shifted = Date.now() + UZ_OFFSET_MS;
      const sinceMidnight = shifted % 86_400_000;
      // +1s guard so the timer fires just after the boundary, never a hair before it.
      const untilMidnight = 86_400_000 - sinceMidnight + 1000;
      timer = setTimeout(() => {
        setDay(uzbekistanDayKey());
        schedule(); // arm the next day's rollover
      }, untilMidnight);
    };

    schedule();
    return () => clearTimeout(timer);
  }, []);

  return day;
}
