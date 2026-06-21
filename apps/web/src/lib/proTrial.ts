/** Formats an API expiry timestamp as the learner-facing `dd.mm.yyyy` form. */
export function formatProTrialDate(value: string): string {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  // The API expiry is an absolute UTC boundary. Preserve its calendar date instead of
  // shifting it to the browser timezone (23:59Z becomes the next day in Uzbekistan).
  const day = date.getUTCDate().toString().padStart(2, "0");
  const month = (date.getUTCMonth() + 1).toString().padStart(2, "0");
  return `${day}.${month}.${date.getUTCFullYear()}`;
}
