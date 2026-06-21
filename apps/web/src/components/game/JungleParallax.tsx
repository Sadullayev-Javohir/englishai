import { useEffect, useRef } from "react";
import {
  motion,
  useScroll,
  useTransform,
  useSpring,
  useMotionValue,
} from "framer-motion";
import { cn } from "@/lib/cn";

/**
 * JungleParallax - a fresh, decorative depth overlay for the Level Map.
 *
 * It sits ABOVE the fixed JUNGLE.jpg photo (mounted by AppShell/JungleBackground)
 * and gives the winding path a sense of living depth:
 *
 *   depth 0  deep gradient wash + god-rays (sunlight through the canopy)
 *   depth 1  far canopy silhouette (blurred, dark) - moves slowest on scroll
 *   depth 2  drifting mist band
 *   depth 3  mid-ground monstera / palm / fern leaves - gentle wind sway
 *   depth 4  floating pollen motes
 *   depth 5  foreground fronds framing the corners - move fastest, biggest sway
 *
 * Scroll parallax (framer-motion) + pointer parallax (CSS custom props). Purely
 * decorative (pointer-events-none, aria-hidden). Masked toward the bottom so the
 * roadmap cards + text stay readable over the photo. Respects prefers-reduced-motion.
 *
 * Rebuilt fresh - NOT copied from the old "dabdala" layer (src/components/duo/*).
 */

export function JungleParallax({
  className,
  intensity = 18,
}: {
  className?: string;
  intensity?: number;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const reduce =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  const { scrollYProgress } = useScroll({
    target: ref,
    offset: ["start start", "end start"],
  });
  const yFar = useTransform(scrollYProgress, [0, 1], [0, 60]);
  const yMid = useTransform(scrollYProgress, [0, 1], [0, 140]);
  const yNear = useTransform(scrollYProgress, [0, 1], [0, 260]);

  const mx = useMotionValue(0);
  const my = useMotionValue(0);
  const pxFar = useSpring(useTransform(mx, [-1, 1], [-intensity / 2.2, intensity / 2.2]), {
    stiffness: 60,
    damping: 20,
  });
  const pyFar = useSpring(useTransform(my, [-1, 1], [-intensity / 3, intensity / 3]), {
    stiffness: 60,
    damping: 20,
  });
  const pxNear = useSpring(useTransform(mx, [-1, 1], [-intensity, intensity]), {
    stiffness: 80,
    damping: 18,
  });
  const pyNear = useSpring(useTransform(my, [-1, 1], [-intensity * 0.7, intensity * 0.7]), {
    stiffness: 80,
    damping: 18,
  });

  useEffect(() => {
    if (reduce) return;
    const el = ref.current;
    if (!el) return;
    const onMove = (e: PointerEvent) => {
      mx.set((e.clientX / window.innerWidth) * 2 - 1);
      my.set((e.clientY / window.innerHeight) * 2 - 1);
    };
    window.addEventListener("pointermove", onMove, { passive: true });
    return () => window.removeEventListener("pointermove", onMove);
  }, [mx, my, reduce]);

  const sway = (dur: number, deg: number) =>
    reduce
      ? {}
      : {
          animate: { rotate: [-deg, deg, -deg] },
          transition: { duration: dur, repeat: Infinity, ease: "easeInOut" },
        };

  return (
    <div
      ref={ref}
      aria-hidden
      className={cn(
        "pointer-events-none absolute inset-0 overflow-hidden",
        className,
      )}
      style={{
        maskImage:
          "linear-gradient(to bottom, rgba(0,0,0,1) 0%, rgba(0,0,0,0.85) 55%, rgba(0,0,0,0.35) 100%)",
        WebkitMaskImage:
          "linear-gradient(to bottom, rgba(0,0,0,1) 0%, rgba(0,0,0,0.85) 55%, rgba(0,0,0,0.35) 100%)",
      }}
    >
      {/* depth 0: god-rays only (no darkening gradient wash - the background
          video/photo stays fully clear) */}
      {!reduce && (
        <motion.div
          className="absolute -top-1/4 right-0 h-[150%] w-[70%] opacity-30 mix-blend-screen"
          style={{
            background:
              "repeating-linear-gradient(105deg, rgba(255,246,200,0.25) 0px, rgba(255,246,200,0.25) 8px, transparent 8px, transparent 60px)",
          }}
          animate={{ opacity: [0.18, 0.4, 0.18] }}
          transition={{ duration: 8, repeat: Infinity, ease: "easeInOut" }}
        />
      )}

      {/* depth 1: far canopy silhouette */}
      <motion.svg
        className="absolute -top-8 left-0 w-full blur-[3px] opacity-60"
        viewBox="0 0 1440 400"
        preserveAspectRatio="xMidYMin slice"
        style={{ y: yFar, x: pxFar, translateY: pyFar }}
      >
        <path
          d="M0 0h1440v150c-60 40-120-10-190 20s-120 60-200 30-140-50-230-20-160 70-260 40S170 190 90 210 0 180 0 180Z"
          fill="#06331f"
        />
        {canopyBlobs("#0a4a2c", 0)}
      </motion.svg>

      {/* depth 3: mid-ground leaves (sway) */}
      <motion.div
        className="absolute inset-0"
        style={{ y: yMid, x: pxFar, translateY: pyFar }}
      >
        <motion.div
          className="absolute -left-10 -top-6 origin-top-left"
          style={{ width: 220 }}
          {...sway(7, 2.5)}
        >
          <Monstera color="#0f7a45" />
        </motion.div>
        <motion.div
          className="absolute right-2 top-24 origin-top-right"
          style={{ width: 200 }}
          {...sway(9, 2)}
        >
          <Palm color="#12894f" flip />
        </motion.div>
        <motion.div
          className="absolute left-6 top-1/2 origin-bottom-left"
          style={{ width: 160 }}
          {...sway(8, 3)}
        >
          <Fern color="#0d6b3c" />
        </motion.div>
      </motion.div>

      {/* depth 4: pollen motes */}
      {!reduce && <Pollen />}

      {/* depth 5: foreground fronds framing the corners */}
      <motion.div
        className="absolute inset-0"
        style={{ y: yNear, x: pxNear, translateY: pyNear }}
      >
        <motion.div
          className="absolute -left-16 -top-10 origin-top-left"
          style={{ width: 340 }}
          {...sway(6, 3.5)}
        >
          <Monstera color="#0a5a33" dark />
        </motion.div>
        <motion.div
          className="absolute -right-20 -top-6 origin-top-right"
          style={{ width: 360 }}
          {...sway(6.5, 3)}
        >
          <Palm color="#0a5a33" flip dark />
        </motion.div>
        <motion.div
          className="absolute -left-10 -bottom-16 origin-bottom-left"
          style={{ width: 300 }}
          {...sway(7.5, 3)}
        >
          <Fern color="#083f24" dark />
        </motion.div>
        <motion.div
          className="absolute -right-12 -bottom-20 origin-bottom-right"
          style={{ width: 320 }}
          {...sway(7, 3.5)}
        >
          <Monstera color="#083f24" flip dark />
        </motion.div>
      </motion.div>
    </div>
  );
}

function canopyBlobs(color: string, seed: number) {
  const blobs = [];
  for (let i = 0; i < 10; i++) {
    const cx = (i * 151 + seed * 37) % 1440;
    const cy = 40 + ((i * 53) % 90);
    const r = 40 + ((i * 29) % 50);
    blobs.push(
      <circle key={i} cx={cx} cy={cy} r={r} fill={color} opacity={0.6} />,
    );
  }
  return <g>{blobs}</g>;
}

function Monstera({
  color,
  flip,
  dark,
}: {
  color: string;
  flip?: boolean;
  dark?: boolean;
}) {
  return (
    <svg
      viewBox="0 0 200 200"
      className="w-full h-auto"
      style={{ transform: flip ? "scaleX(-1)" : undefined }}
    >
      <defs>
        <linearGradient
          id={`mj-${color}-${dark ? "d" : "l"}`}
          x1="0"
          y1="0"
          x2="1"
          y2="1"
        >
          <stop offset="0%" stopColor={color} />
          <stop offset="100%" stopColor={dark ? "#052a18" : "#0a5a33"} />
        </linearGradient>
      </defs>
      <g fill={`url(#mj-${color}-${dark ? "d" : "l"})`}>
        <path d="M100 12c46 6 78 44 82 96 3 44-22 82-64 82-10 0-18-6-18-16 0-30 6-40 6-70 0-40-14-70-14-92 0 0 4 0 8 0Z" />
        <path d="M96 20C58 30 26 66 24 116c-2 42 24 76 62 74 10 0 16-8 14-18-6-28-14-36-20-64-8-38-2-72 4-92 0 0-4-2-8 0Z" />
      </g>
      <g
        stroke={dark ? "#04160f" : "#063d24"}
        strokeWidth="5"
        opacity="0.55"
        fill="none"
        strokeLinecap="round"
      >
        <path d="M100 60c14 4 22 16 24 32M100 96c16 4 24 16 26 34M100 132c14 2 22 12 22 26" />
        <path d="M92 60c-16 6-24 18-26 36M92 98c-18 6-26 18-26 38M92 134c-14 4-22 12-22 26" />
      </g>
      <path
        d="M96 20c2 60 4 110 6 168"
        stroke={dark ? "#03110a" : "#053a22"}
        strokeWidth="4"
        fill="none"
      />
    </svg>
  );
}

function Palm({
  color,
  flip,
  dark,
}: {
  color: string;
  flip?: boolean;
  dark?: boolean;
}) {
  const leaflets = [];
  for (let i = 0; i < 12; i++) {
    const y = 20 + i * 14;
    const len = 70 - i * 4;
    leaflets.push(
      <path
        key={`l${i}`}
        d={`M100 ${y} C ${100 - len} ${y - 6}, ${100 - len} ${y + 4}, ${100 - len + 8} ${y + 12}`}
      />,
    );
    leaflets.push(
      <path
        key={`r${i}`}
        d={`M100 ${y} C ${100 + len} ${y - 6}, ${100 + len} ${y + 4}, ${100 + len - 8} ${y + 12}`}
      />,
    );
  }
  return (
    <svg
      viewBox="0 0 200 220"
      className="w-full h-auto"
      style={{ transform: flip ? "scaleX(-1)" : undefined }}
    >
      <g
        stroke={color}
        strokeWidth="9"
        strokeLinecap="round"
        fill="none"
        opacity={dark ? 0.95 : 0.9}
      >
        {leaflets}
      </g>
      <path
        d="M100 8 L100 210"
        stroke={dark ? "#052a18" : "#0a5a33"}
        strokeWidth="6"
        strokeLinecap="round"
      />
    </svg>
  );
}

function Fern({
  color,
  flip,
  dark,
}: {
  color: string;
  flip?: boolean;
  dark?: boolean;
}) {
  const parts = [];
  for (let i = 0; i < 16; i++) {
    const y = 14 + i * 11;
    const len = 46 - i * 2.4;
    parts.push(<path key={`l${i}`} d={`M100 ${y} q ${-len} -4 ${-len - 6} 10`} />);
    parts.push(<path key={`r${i}`} d={`M100 ${y} q ${len} -4 ${len + 6} 10`} />);
  }
  return (
    <svg
      viewBox="0 0 200 200"
      className="w-full h-auto"
      style={{ transform: flip ? "scaleX(-1)" : undefined }}
    >
      <g
        stroke={color}
        strokeWidth="4"
        strokeLinecap="round"
        fill="none"
        opacity={dark ? 0.95 : 0.85}
      >
        {parts}
      </g>
      <path
        d="M100 6 L100 196"
        stroke={dark ? "#052a18" : "#0a5a33"}
        strokeWidth="5"
        strokeLinecap="round"
      />
    </svg>
  );
}

function Pollen() {
  const motes = Array.from({ length: 26 }, (_, i) => {
    const left = (i * 37) % 100;
    const size = 2 + ((i * 7) % 4);
    const dur = 10 + ((i * 13) % 12);
    const delay = (i * 0.7) % 8;
    return (
      <motion.span
        key={i}
        className="absolute rounded-full bg-ea-yellow-50"
        style={{
          left: `${left}%`,
          bottom: -10,
          width: size,
          height: size,
          filter: "blur(0.3px)",
        }}
        animate={{
          y: [0, -600],
          opacity: [0, 0.8, 0],
          x: [0, 20, -10, 15],
        }}
        transition={{
          duration: dur,
          repeat: Infinity,
          delay,
          ease: "easeInOut",
        }}
      />
    );
  });
  return <div className="absolute inset-0 overflow-hidden">{motes}</div>;
}
