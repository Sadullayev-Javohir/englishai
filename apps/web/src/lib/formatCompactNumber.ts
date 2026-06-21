export function formatCompactNumber(value: number): string {
  if (value < 1000) return String(value);

  const thousands = value / 1000;
  return `${thousands.toFixed(value % 1000 === 0 ? 0 : 1)}k`;
}
