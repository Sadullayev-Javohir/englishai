import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useNavigate } from "react-router-dom";
import { AnimatePresence, motion } from "framer-motion";
import { NavHud } from "./NavHud";
import { Icon } from "@/components/ui/Icon";
import { ProgressRing } from "@/components/ui/ProgressRing";
import { cn } from "@/lib/cn";

/**
 * Shared 3D jungle frame for every lesson type (video / books / grammar / writing
 * / listening). Wraps the lesson content in the same Duolingo-style shell:
 *   - JUNGLE.jpg background + scrim, no app sidebar.
 *   - NavHud (global game HUD) pinned at the very top.
 *   - A lesson sub-bar: exit, module chip + title, progress ring, hearts, lesson XP.
 *   - The lesson body in a frosted white "stage" card.
 *
 * Inline exercises reach the HUD through useLessonHud(): loseHearts() drops a heart
 * on a wrong answer, addXp() floats a "+XP" reward on a correct one / on completion.
 * Canonical game-layer copy (migrated from duo/*).
 */

export type LessonModule =
  | "video"
  | "books"
  | "grammar"
  | "writing"
  | "listening";

interface ModuleMeta {
  label: string;
  accent: string;
  accentText: string;
  ring: string;
}

const MODULE_META: Record<LessonModule, ModuleMeta> = {
  video:     { label: "Video",     accent: "bg-ea-primary",   accentText: "text-ea-on-primary", ring: "text-ea-primary" },
  books:     { label: "Books",     accent: "bg-ea-orange-500",  accentText: "text-ea-on-primary", ring: "text-ea-orange-600" },
  grammar:   { label: "Grammar",   accent: "bg-ea-primary", accentText: "text-ea-on-primary", ring: "text-ea-primary" },
  writing:   { label: "Writing",   accent: "bg-ea-orange-500", accentText: "text-ea-on-primary", ring: "text-ea-orange-600" },
  listening: { label: "Listening", accent: "bg-ea-primary",   accentText: "text-ea-on-primary", ring: "text-ea-primary" },
};

const MAX_HEARTS = 5;

interface LessonHudState {
  hearts: number;
  xp: number;
  loseHearts: (n?: number) => void;
  addXp: (amount: number) => void;
}

const LessonHudContext = createContext<LessonHudState | null>(null);

export function useLessonHud(): LessonHudState {
  const ctx = useContext(LessonHudContext);
  if (!ctx) throw new Error("useLessonHud must be used inside <LessonFrame>");
  return ctx;
}

interface LessonFrameProps {
  module: LessonModule;
  title?: string;
  /** 0..1 lesson progress; drives the ring. */
  progress?: number;
  /** Optional explicit exit handler; defaults to history back. */
  onExit?: () => void;
  /** Stage card max width (video is wider). */
  maxWidth?: number;
  children: ReactNode;
}

export function LessonFrame({
  module,
  title,
  progress = 0,
  onExit,
  maxWidth = 760,
  children,
}: LessonFrameProps) {
  const navigate = useNavigate();
  const meta = MODULE_META[module];

  const [hearts, setHearts] = useState(MAX_HEARTS);
  const [xp, setXp] = useState(0);
  const [xpFlash, setXpFlash] = useState<number | null>(null);

  const loseHearts = useCallback((n = 1) => {
    setHearts((h) => Math.max(0, h - n));
  }, []);

  const addXp = useCallback((amount: number) => {
    setXp((x) => x + amount);
    setXpFlash(amount);
    window.setTimeout(() => setXpFlash(null), 1100);
  }, []);

  const hud = useMemo(
    () => ({ hearts, xp, loseHearts, addXp }),
    [hearts, xp, loseHearts, addXp],
  );

  const exit = onExit ?? (() => navigate(-1));
  const pct = Math.round(Math.max(0, Math.min(1, progress)) * 100);

  return (
    <LessonHudContext.Provider value={hud}>
      {/* Jungle background + scrim */}
      <div
        className="fixed inset-0 z-0 bg-cover bg-center"
        style={{ backgroundImage: "url(/assets/jungle.jpg)" }}
        aria-hidden
      />
      <div
        className="fixed inset-0 z-0 bg-gradient-to-b from-black/35 via-black/25 to-black/45"
        aria-hidden="true"
      />

      <div className="relative z-10 min-h-screen flex flex-col">
        {/* Pinned top cluster: global HUD + lesson sub-bar */}
        <header className="sticky top-0 z-30">
          <NavHud />
          <div className="flex items-center gap-2 px-3 py-2 md:px-4 bg-ea-surface/85  border-b border-white/40 shadow-sm">
            <button
              onClick={exit}
              aria-label="Exit lesson"
              className="shrink-0 flex items-center justify-center w-9 h-9 rounded-full bg-ea-surface/90 shadow-sm transition-transform"
            >
              <Icon name="close" className="text-[20px] text-text-primary" />
            </button>

            <div className="flex items-center gap-2 min-w-0">
              <span
                className={cn(
                  "shrink-0 px-2.5 py-0.5 rounded-full font-duo font-extrabold text-[12px] uppercase tracking-wide",
                  meta.accent,
                  meta.accentText,
                )}
              >
                {meta.label}
              </span>
              {title && (
                <span className="font-duo font-extrabold text-[15px] text-text-primary truncate">
                  {title}
                </span>
              )}
            </div>

            <div className="ml-auto flex items-center gap-3 shrink-0">
              <ProgressRing
                value={Math.max(0, Math.min(1, progress))}
                size={38}
                strokeWidth={5}
                trackClassName="text-white/30"
                arcClassName={meta.ring}
              >
                <span className="font-duo font-extrabold text-[10px] text-text-primary">
                  {pct}%
                </span>
              </ProgressRing>

              <Hearts hearts={hearts} />

              <div className="relative">
                <div className="flex items-center gap-1 rounded-full bg-ea-orange-500/15 px-2.5 py-1.5">
                  <Icon
                    name="diamond"
                    filled
                    className="text-ea-orange-600 text-[16px]"
                  />
                  <span className="font-duo font-extrabold text-[14px] text-ea-orange-600 tabular-nums">
                    {xp}
                  </span>
                </div>
                <AnimatePresence>
                  {xpFlash !== null && (
                    <motion.span
                      key={xpFlash}
                      initial={{ opacity: 0, y: 6, scale: 0.6 }}
                      animate={{ opacity: 1, y: -18, scale: 1 }}
                      exit={{ opacity: 0, y: -28, scale: 0.8 }}
                      transition={{
                        type: "spring",
                        stiffness: 420,
                        damping: 18,
                      }}
                      className="absolute left-1/2 -translate-x-1/2 -top-1 whitespace-nowrap font-duo font-extrabold text-[13px] text-ea-orange-600"
                    >
                      +{xpFlash} XP
                    </motion.span>
                  )}
                </AnimatePresence>
              </div>
            </div>
          </div>
        </header>

        {/* Lesson body in a frosted stage card */}
        <main className="flex-1 px-3 pt-4 pb-10 md:px-6 md:pt-6">
          <div
            className="mx-auto rounded-card border border-white/60 bg-ea-surface/95 p-4 shadow-none md:p-8"
            style={{ maxWidth }}
          >
            {children}
          </div>
        </main>
      </div>
    </LessonHudContext.Provider>
  );
}

/** Row of hearts; lost hearts animate out, the rest stay filled. */
function Hearts({ hearts }: { hearts: number }) {
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: MAX_HEARTS }, (_, i) => {
        const filled = i < hearts;
        return (
          <motion.span
            key={i}
            animate={
              filled
                ? { scale: [1, 1.18, 1], opacity: 1 }
                : { scale: 0.7, opacity: 0.35 }
            }
            transition={{ duration: 0.35 }}
            className="text-[18px] leading-none"
          >
            {filled ? "❤️" : "🤍"}
          </motion.span>
        );
      })}
    </div>
  );
}
