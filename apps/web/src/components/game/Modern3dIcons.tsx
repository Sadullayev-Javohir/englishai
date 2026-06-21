import { cn } from "@/lib/cn";

/**
 * Modern 3D-style stat icons for the global Nav HUD (replaces the flat emoji glyphs).
 * Each icon is a self-contained SVG with a radial/linear gradient body, a top highlight
 * rim for a glossy "3D" sheen, and a soft drop shadow - the same visual language as the
 * Duolingo-style raised pills. Render at 22–26px inside the stat pills.
 */

interface Icon3dProps {
  /** Tailwind size + colour-inherit classes (e.g. "text-[22px]"). */
  className?: string;
  title?: string;
}

const base = "inline-block shrink-0 align-[-0.125em] w-[1em] h-[1em]";
const dropShadow = "drop-shadow-[0_2px_2px_rgba(0,0,0,0.25)]";

/** 🔥 Streak flame - warm orange/red. */
export function Flame3d({ className, title }: Icon3dProps) {
  const id = "flame-grad";
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn(base, dropShadow, className)}
      role="img"
      aria-label={title ?? "Streak"}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#FFD24A" />
          <stop offset="45%" stopColor="#FF9A2E" />
          <stop offset="100%" stopColor="#FF4D2E" />
        </linearGradient>
      </defs>
      <path
        d="M12 2c2.4 3.1 4.7 5.2 4.7 9.2A4.7 4.7 0 0 1 12 16a3 3 0 0 1-2.9-3.7c.3-1.1.9-1.9 1.4-2.9.4 1 .5 1.9 1.5 2.4-.9-1.8-.6-4.6-.8-6.8Z"
        fill={`url(#${id})`}
        stroke="#D83A1E"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <path
        d="M12 9c1 .9 1.6 1.8 1.6 3.1A1.6 1.6 0 0 1 12 14a1.3 1.3 0 0 1-1.1-2c.3-.5.7-.8 1.1-1.4Z"
        fill="#FFE7A8"
        opacity="0.85"
      />
    </svg>
  );
}

/** ⚡ XP bolt - electric yellow. */
export function Bolt3d({ className, title }: Icon3dProps) {
  const id = "bolt-grad";
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn(base, dropShadow, className)}
      role="img"
      aria-label={title ?? "XP"}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#FFE96B" />
          <stop offset="100%" stopColor="#FFB300" />
        </linearGradient>
      </defs>
      <path
        d="M13 2 5 13h5l-1 9 9-12h-5l1-8Z"
        fill={`url(#${id})`}
        stroke="#E09600"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <path d="M12 5.5 8.4 12h3.6l-.8 4 4.6-6.2h-3.6L13 5.5Z" fill="#FFF4C2" opacity="0.8" />
    </svg>
  );
}

/** 💎 Gems - blue crystal. */
export function Gem3d({ className, title }: Icon3dProps) {
  const id = "gem-grad";
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn(base, dropShadow, className)}
      role="img"
      aria-label={title ?? "Gems"}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#8AD6FF" />
          <stop offset="100%" stopColor="#2E9BFF" />
        </linearGradient>
      </defs>
      <path
        d="M6 4h12l3 5-9 11L3 9l3-5Z"
        fill={`url(#${id})`}
        stroke="#1577D6"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <path d="M3 9h18l-3 5H6L3 9Z" fill="#BFE9FF" opacity="0.7" />
      <path d="M12 4 9 9l3 11 3-11-3-5Z" fill="#E6F6FF" opacity="0.5" />
    </svg>
  );
}

/** 🏆 League trophy - purple/gold. */
export function Trophy3d({ className, title }: Icon3dProps) {
  const id = "trophy-grad";
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn(base, dropShadow, className)}
      role="img"
      aria-label={title ?? "League"}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#C9A6FF" />
          <stop offset="100%" stopColor="#7C3AED" />
        </linearGradient>
      </defs>
      <path
        d="M6 4h12v3a6 6 0 0 1-12 0V4Z"
        fill={`url(#${id})`}
        stroke="#5B21B6"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <path d="M4.5 5H6v1.5A4.5 4.5 0 0 1 3 8.5 3 3 0 0 0 4.5 5Z" fill={`url(#${id})`} stroke="#5B21B6" strokeWidth="0.5" />
      <path d="M19.5 5H18v1.5A4.5 4.5 0 0 0 21 8.5 3 3 0 0 1 19.5 5Z" fill={`url(#${id})`} stroke="#5B21B6" strokeWidth="0.5" />
      <path d="M11 12h2v3h-2z" fill="#7C3AED" stroke="#5B21B6" strokeWidth="0.5" />
      <path d="M8.5 15h7l1 3H7.5l1-3Z" fill={`url(#${id})`} stroke="#5B21B6" strokeWidth="0.6" strokeLinejoin="round" />
      <ellipse cx="12" cy="6.5" rx="4.5" ry="2" fill="#EDE3FF" opacity="0.55" />
    </svg>
  );
}

/** ❤️ Hearts - red. */
export function Heart3d({ className, title }: Icon3dProps) {
  const id = "heart-grad";
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn(base, dropShadow, className)}
      role="img"
      aria-label={title ?? "Hearts"}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor="#FF8A8A" />
          <stop offset="100%" stopColor="#FF3B5C" />
        </linearGradient>
      </defs>
      <path
        d="M12 21s-7-4.6-9.2-9C1.3 8.8 2.8 5.5 6 5.5c2 0 3.2 1.2 4 2.5.8-1.3 2-2.5 4-2.5 3.2 0 4.7 3.3 3.2 6.5C19 16.4 12 21 12 21Z"
        fill={`url(#${id})`}
        stroke="#D6203E"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <path d="M8 8.5c.6-1 1.7-1.6 2.7-1.4-.6.7-1 1.6-1.4 2.4-.9-.2-1.6-.4-2.1-1Z" fill="#FFD2DC" opacity="0.8" />
    </svg>
  );
}

/** 🛡️ Shield - for premium/subscription. */
export function Shield3d({ className, title }: Icon3dProps) {
  const id = "shield-grad";
  return (
    <svg
      viewBox="0 0 24 24"
      className={cn(base, dropShadow, className)}
      role="img"
      aria-label={title ?? "Shield"}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#E0B3FF" />
          <stop offset="100%" stopColor="#9B59E6" />
        </linearGradient>
      </defs>
      <path
        d="M12 2L4 6.5V18a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V6.5L12 2Z"
        fill={`url(#${id})`}
        stroke="#7C3AED"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <path d="M12 5.5v5l4.5 3.5" fill="none" stroke="#D8B4FE" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M8 14h8" stroke="#D8B4FE" strokeWidth="1.5" strokeLinecap="round" />
      <ellipse cx="12" cy="7" rx="5" ry="2" fill="#F3E8FF" opacity="0.5" />
    </svg>
  );
}

/** ⭐ XP star - gold. */
export function Star3d({ className, title }: Icon3dProps) {
  const id = "star3d-grad";
  return (
    <svg viewBox="0 0 24 24" className={cn(base, dropShadow, className)} role="img" aria-label={title ?? "XP"}>
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#FFE96B" />
          <stop offset="100%" stopColor="#FFB300" />
        </linearGradient>
      </defs>
      <path
        d="M12 2.6 14.7 8.5 21 9.2 16.3 13.6 17.6 20 12 16.8 6.4 20 7.7 13.6 3 9.2 9.3 8.5Z"
        fill={`url(#${id})`}
        stroke="#E09600"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      <ellipse cx="10.4" cy="8.9" rx="2" ry="1.1" fill="#FFF7CC" opacity="0.75" />
    </svg>
  );
}

/** ☀️ Sun - gold (light-mode toggle). */
export function Sun3d({ className, title }: Icon3dProps) {
  const id = "sun3d-grad";
  return (
    <svg viewBox="0 0 24 24" className={cn(base, dropShadow, className)} role="img" aria-label={title ?? "Sun"}>
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#FFE96B" />
          <stop offset="100%" stopColor="#FFB300" />
        </linearGradient>
      </defs>
      <path d="M12 2.2V4.4 M12 19.6V21.8 M2.2 12H4.4 M19.6 12H21.8 M5 5l1.6 1.6 M17.4 17.4l1.6 1.6 M19 5l-1.6 1.6 M6.6 17.4L5 19" stroke="#F5A300" strokeWidth="2" strokeLinecap="round" />
      <circle cx="12" cy="12" r="5.4" fill={`url(#${id})`} stroke="#E09600" strokeWidth="0.6" />
      <ellipse cx="10.2" cy="10.2" rx="2" ry="1.3" fill="#FFF7CC" opacity="0.7" />
    </svg>
  );
}

/** 🌙 Moon - crescent with craters + sparkles (dark-mode toggle). */
export function Moon3d({ className, title }: Icon3dProps) {
  const id = "moon3d-grad";
  return (
    <svg viewBox="0 0 24 24" className={cn(base, dropShadow, className)} role="img" aria-label={title ?? "Moon"}>
      <defs>
        <linearGradient id={id} x1="0.15" y1="0" x2="0.7" y2="1">
          <stop offset="0%" stopColor="#C7B8FF" />
          <stop offset="55%" stopColor="#8B5CF6" />
          <stop offset="100%" stopColor="#6D28D9" />
        </linearGradient>
      </defs>
      {/* bold single crescent (one closed shape: outer arc + terminator) */}
      <path
        d="M12 2.8A9.2 9.2 0 1 0 12 21.2 5 9.2 0 1 1 12 2.8Z"
        fill={`url(#${id})`}
        stroke="#5B21B6"
        strokeWidth="0.6"
        strokeLinejoin="round"
      />
      {/* craters on the thick face */}
      <circle cx="7" cy="10" r="1.5" fill="#4C1D95" opacity="0.28" />
      <circle cx="5.7" cy="13.4" r="1.05" fill="#4C1D95" opacity="0.24" />
      <circle cx="9.2" cy="14.8" r="0.85" fill="#4C1D95" opacity="0.22" />
      {/* gloss on the outer rim */}
      <path d="M5 5.4A9 9 0 0 0 2.9 11.5" stroke="#F1ECFF" strokeWidth="1.6" strokeLinecap="round" fill="none" opacity="0.6" />
      {/* sparkles in the sky */}
      <path d="M19.6 3.6Q20.1 5.9 22.2 6.4 20.1 6.9 19.6 9.2 19.1 6.9 17 6.4 19.1 5.9 19.6 3.6Z" fill="#FFE96B" stroke="#F5C400" strokeWidth="0.3" strokeLinejoin="round" />
      <circle cx="21.4" cy="11" r="0.95" fill="#FDE68A" />
    </svg>
  );
}
