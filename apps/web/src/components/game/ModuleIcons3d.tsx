import { useId } from "react";
import { cn } from "@/lib/cn";

/**
 * 3D-style module glyphs for the Home learning cards - richer volume than the
 * flat {@link ./Modern3dIcons} HUD set: a 3-stop directional gradient body, a
 * bright specular highlight, and a soft cast + drop shadow for real "clay 3D"
 * depth. One per learning module, tinted with that module's accent hue. Render
 * at 28-36px inside the `.ph-action__icon` tile (sized via font-size, `w-[1em]`).
 */

interface ModuleIcon3dProps {
  /** Normalized module key (lowercase): vocabulary/grammar/reading/... */
  module: string;
  /** Tailwind size classes (e.g. "text-[30px]"). */
  className?: string;
  title?: string;
}

const base = "inline-block shrink-0 align-[-0.125em] w-[1em] h-[1em]";

/** Per-module palette: [top, light, dark, stroke, gloss]. Matches --ea-mod-* hues. */
const HUES: Record<string, [string, string, string, string, string]> = {
  vocabulary: ["#7bf0da", "#25cbb0", "#0e8577", "#0b6b60", "#e5fff9"],
  grammar: ["#c9b8ff", "#9a7cf7", "#6d28d9", "#5b21b6", "#efeaff"],
  reading: ["#a9ccfd", "#5c9bf9", "#2563eb", "#1d4ed8", "#e3efff"],
  writing: ["#ffb3a8", "#ff8271", "#e4402f", "#c22f20", "#ffe1db"],
  speaking: ["#8ee0b6", "#43c489", "#16875d", "#0f6b49", "#dcf9ec"],
  listening: ["#ffdd8f", "#ffbb3f", "#d98a08", "#b06d05", "#fff0cf"],
  roleplay: ["#fbafd7", "#f473b9", "#db2777", "#b31e63", "#ffe2f1"],
  books: ["#ffc088", "#fb943f", "#ea580c", "#c2470a", "#ffe6cf"],
  video: ["#ffb1bd", "#fb7f92", "#e11d48", "#be1739", "#ffdde2"],
};

/** Specular highlight blob position per glyph [cx,cy,rx,ry], inside the shape. */
const SPEC: Record<string, [number, number, number, number]> = {
  vocabulary: [9, 7, 3.1, 1.5],
  grammar: [9, 8.4, 3, 1.5],
  reading: [7, 8.4, 1.9, 1],
  writing: [16.6, 6.6, 1.3, 0.9],
  speaking: [11.3, 5.2, 1.3, 2],
  listening: [5.4, 14.2, 1.3, 1.7],
  roleplay: [9.2, 8, 2.5, 1.3],
  books: [10, 5.6, 3, 0.9],
  video: [7, 8, 3, 1.1],
};

export function ModuleIcon3d({ module, className, title }: ModuleIcon3dProps) {
  const uid = useId().replace(/:/g, "");
  const key = HUES[module] ? module : "grammar";
  const [top, light, dark, stroke, gloss] = HUES[key];
  const gid = `mig-${uid}`;
  const sid = `mis-${uid}`;
  const fid = `mif-${uid}`;
  const grad = `url(#${gid})`;
  const [scx, scy, srx, sry] = SPEC[key] ?? [9, 8, 3, 1.4];

  return (
    <svg viewBox="0 0 24 24" className={cn(base, className)} role="img" aria-label={title ?? module}>
      <defs>
        <linearGradient id={gid} x1="0.2" y1="0" x2="0.5" y2="1">
          <stop offset="0%" stopColor={top} />
          <stop offset="45%" stopColor={light} />
          <stop offset="100%" stopColor={dark} />
        </linearGradient>
        <radialGradient id={sid} cx="0.5" cy="0.5" r="0.5">
          <stop offset="0%" stopColor="#ffffff" stopOpacity="0.9" />
          <stop offset="100%" stopColor="#ffffff" stopOpacity="0" />
        </radialGradient>
        <filter id={fid} x="-25%" y="-25%" width="150%" height="155%">
          <feDropShadow dx="0" dy="0.7" stdDeviation="0.75" floodColor={dark} floodOpacity="0.45" />
        </filter>
      </defs>
      {/* soft contact shadow on the tile */}
      <ellipse cx="12" cy="21.4" rx="7" ry="1.35" fill={dark} opacity="0.2" />
      <g filter={`url(#${fid})`}>
        {renderGlyph(key, { grad, stroke, gloss, dark })}
        {/* specular sheen */}
        <ellipse cx={scx} cy={scy} rx={srx} ry={sry} fill={`url(#${sid})`} />
      </g>
    </svg>
  );
}

function renderGlyph(key: string, c: { grad: string; stroke: string; gloss: string; dark: string }) {
  const { grad, stroke, gloss } = c;
  const sw = 0.7;
  switch (key) {
    case "vocabulary": // closed book with bookmark
      return (
        <>
          <rect x="4.5" y="3.5" width="15" height="17" rx="3.2" fill={grad} stroke={stroke} strokeWidth={sw} />
          <rect x="6.2" y="17.4" width="11.6" height="2.4" rx="1.1" fill="#fffdf6" opacity="0.92" />
          <path d="M14.6 3.5v6l-2-1.6-2 1.6v-6z" fill="#ffd24a" stroke="#e0a400" strokeWidth="0.5" strokeLinejoin="round" />
          <path d="M7 6.6c1.7-1 4-1.2 6-.7" stroke={gloss} strokeWidth="1.4" strokeLinecap="round" opacity="0.7" fill="none" />
        </>
      );
    case "grammar": // clipboard checklist
      return (
        <>
          <rect x="4.5" y="4" width="15" height="16.2" rx="3" fill={grad} stroke={stroke} strokeWidth={sw} />
          <rect x="8.4" y="2.4" width="7.2" height="3.6" rx="1.5" fill={gloss} stroke={stroke} strokeWidth="0.5" />
          <path d="M7.6 11l1.7 1.7 3-3.1" stroke="#fff" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M13.6 11.2h3M7.8 15.3h8.4" stroke={gloss} strokeWidth="1.4" strokeLinecap="round" opacity="0.85" />
        </>
      );
    case "reading": // open book
      return (
        <>
          <path d="M12 6.4C9.4 4.8 6 4.8 3.4 5.9v11.7C6 16.5 9.4 16.5 12 18z" fill={grad} stroke={stroke} strokeWidth={sw} strokeLinejoin="round" />
          <path d="M12 6.4c2.6-1.6 6-1.6 8.6-.5v11.7C18 16.5 14.6 16.5 12 18z" fill={grad} stroke={stroke} strokeWidth={sw} strokeLinejoin="round" opacity="0.88" />
          <path d="M5.5 8.6c1.6-.5 3.4-.5 4.8.1M13.7 8.7c1.4-.6 3.2-.6 4.8-.1" stroke={gloss} strokeWidth="1.1" strokeLinecap="round" opacity="0.8" fill="none" />
        </>
      );
    case "writing": // pencil
      return (
        <>
          <path d="M5.6 18.4l1.1-3.9 9-9 2.8 2.8-9 9z" fill={grad} stroke={stroke} strokeWidth={sw} strokeLinejoin="round" />
          <path d="M15.6 5.5l2-2 2.8 2.8-2 2z" fill={gloss} stroke={stroke} strokeWidth="0.5" strokeLinejoin="round" />
          <path d="M5.6 18.4l1.1-3.9 2.8 2.8z" fill="#ffe0b0" />
          <path d="M5.6 18.4l.5-1.9 1.4 1.4z" fill="#2b2233" />
          <path d="M9 12.4l2.8 2.8" stroke={gloss} strokeWidth="1" strokeLinecap="round" opacity="0.7" />
        </>
      );
    case "speaking": // microphone
      return (
        <>
          <rect x="8.7" y="2.6" width="6.6" height="11" rx="3.3" fill={grad} stroke={stroke} strokeWidth={sw} />
          <path d="M6.2 11a5.8 5.8 0 0 0 11.6 0" stroke={c.dark} strokeWidth="1.6" fill="none" strokeLinecap="round" />
          <path d="M12 16.8v3.2M9.4 20h5.2" stroke={c.dark} strokeWidth="1.6" strokeLinecap="round" />
          <rect x="10.4" y="4.2" width="1.7" height="6.4" rx="0.85" fill={gloss} opacity="0.65" />
        </>
      );
    case "listening": // headphones
      return (
        <>
          <path d="M4.6 13.4v-1.2a7.4 7.4 0 0 1 14.8 0v1.2" stroke={grad} strokeWidth="2.6" fill="none" strokeLinecap="round" />
          <rect x="3.3" y="12.4" width="4.4" height="7" rx="2.1" fill={grad} stroke={stroke} strokeWidth={sw} />
          <rect x="16.3" y="12.4" width="4.4" height="7" rx="2.1" fill={grad} stroke={stroke} strokeWidth={sw} />
          <path d="M5 8.8a7.4 7.4 0 0 1 5-2.6" stroke={gloss} strokeWidth="1.2" fill="none" strokeLinecap="round" opacity="0.75" />
        </>
      );
    case "roleplay": // theatre mask
      return (
        <>
          <path d="M5.5 5h13v6.2A6.5 6.5 0 0 1 12 17.7 6.5 6.5 0 0 1 5.5 11.2z" fill={grad} stroke={stroke} strokeWidth={sw} strokeLinejoin="round" />
          <circle cx="9.4" cy="9.2" r="1.1" fill="#fff" />
          <circle cx="14.6" cy="9.2" r="1.1" fill="#fff" />
          <path d="M9 12.6c1.6 1.6 4.4 1.6 6 0" stroke="#fff" strokeWidth="1.4" fill="none" strokeLinecap="round" />
          <path d="M7 6.6c1.4-.7 3-.9 4.6-.6" stroke={gloss} strokeWidth="1.2" fill="none" strokeLinecap="round" opacity="0.7" />
        </>
      );
    case "books": // stacked books
      return (
        <>
          <rect x="4" y="13.6" width="16" height="5" rx="1.8" fill={grad} stroke={stroke} strokeWidth={sw} />
          <rect x="5.6" y="8.9" width="12.8" height="5" rx="1.8" fill={grad} stroke={stroke} strokeWidth={sw} opacity="0.92" />
          <rect x="7.2" y="4.2" width="9.6" height="5" rx="1.8" fill={grad} stroke={stroke} strokeWidth={sw} />
          <path d="M9 6.7h6M7.4 11.4h6.4M6 16.1h7" stroke={gloss} strokeWidth="1.1" strokeLinecap="round" opacity="0.7" />
        </>
      );
    case "video": // play tile
      return (
        <>
          <rect x="3.4" y="5" width="17.2" height="14" rx="4.4" fill={grad} stroke={stroke} strokeWidth={sw} />
          <path d="M10.2 9.3l5 2.7-5 2.7z" fill="#fff" stroke={stroke} strokeWidth="0.4" strokeLinejoin="round" />
          <rect x="6.2" y="7" width="8.6" height="2" rx="1" fill={gloss} opacity="0.5" />
        </>
      );
    default:
      return <circle cx="12" cy="12" r="8" fill={grad} stroke={stroke} strokeWidth={sw} />;
  }
}
