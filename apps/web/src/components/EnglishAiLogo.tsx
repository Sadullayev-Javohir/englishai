import type { CSSProperties } from "react";
import { cn } from "@/lib/cn";
import "./EnglishAiLogo.css";

export interface EnglishAiLogoProps {
  size?: number;
  /** Optional optical mark size within the square logo slot. */
  imgSize?: number;
  className?: string;
  /** Legacy slot styling, retained for existing callers. */
  rounded?: string;
  /** Desktop lockup; the name is hidden on mobile without hiding the accessible label. */
  withWordmark?: boolean;
  wordmarkClassName?: string;
  tileStyle?: CSSProperties;
}

/** Canonical Dialog identity, approved from englishai.pen / TVpvv. */
export function EnglishAiLogo({
  size = 48,
  imgSize = size,
  className,
  rounded = "0",
  withWordmark = false,
  wordmarkClassName,
  tileStyle,
}: EnglishAiLogoProps) {
  return (
    <span className={cn("ea-brand-logo", className)} data-englishai-brand="dialog">
      <span
        className="ea-brand-logo__mark"
        data-englishai-logo-tile="true"
        style={{ width: size, height: size, borderRadius: rounded, background: "transparent", ...tileStyle }}
      >
        <img
          src="/assets/brand/dialog.svg"
          alt="EnglishAI"
          width={imgSize}
          height={imgSize}
          style={{ width: imgSize, height: imgSize }}
          decoding="async"
          draggable={false}
        />
      </span>
      {withWordmark && (
        <span className={cn("ea-brand-name", wordmarkClassName)} data-englishai-wordmark="true" aria-hidden="true">
          EnglishAI
        </span>
      )}
    </span>
  );
}
