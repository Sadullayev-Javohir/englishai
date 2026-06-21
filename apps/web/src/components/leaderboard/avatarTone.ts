const AVATAR_TONES = [
  { background: "#dbeafe", color: "#1d4ed8" },
  { background: "#ede9fe", color: "#6d28d9" },
  { background: "#fce7f3", color: "#be185d" },
  { background: "#ffedd5", color: "#c2410c" },
  { background: "#fef3c7", color: "#a16207" },
  { background: "#ccfbf1", color: "#0f766e" },
  { background: "#dcfce7", color: "#15803d" },
  { background: "#fee2e2", color: "#b91c1c" },
] as const;

export function avatarTone(seed: string) {
  let hash = 0;
  for (const character of seed) hash = (hash * 31 + character.charCodeAt(0)) >>> 0;
  return AVATAR_TONES[hash % AVATAR_TONES.length];
}
