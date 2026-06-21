import type { ReactNode } from "react";
import { motion } from "framer-motion";
import { Confetti } from "@/components/game";
import { Icon } from "@/components/ui/Icon";
import { ProgressRing } from "@/components/ui/ProgressRing";
import { cn } from "@/lib/cn";
import "./SpeakingResultShell.css";

export function SpeakingResultShell({
  status,
  kicker,
  title,
  subtitle,
  score,
  scoreLabel,
  scoreContent,
  details,
  progression,
  actions,
  celebrate = false,
  illustration,
}: {
  status: "passed" | "retry" | "neutral";
  kicker: string;
  title: string;
  subtitle?: ReactNode;
  score?: number;
  scoreLabel?: string;
  scoreContent?: ReactNode;
  details?: ReactNode;
  progression?: ReactNode;
  actions: ReactNode;
  celebrate?: boolean;
  illustration?: ReactNode;
}) {
  const icon = status === "passed" ? "workspace_premium" : status === "retry" ? "restart_alt" : "record_voice_over";
  const normalizedScore = typeof score === "number" ? Math.min(100, Math.max(0, score)) : undefined;

  return (
    <section className={cn("speaking-result", `speaking-result--${status}`)}>
      <Confetti show={celebrate} />
      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        className="speaking-result__hero"
      >
        {illustration ?? (
          <motion.div
            initial={{ scale: 0.55, rotate: -8 }}
            animate={{ scale: 1, rotate: 0 }}
            transition={{ type: "spring", stiffness: 300, damping: 15 }}
            className="speaking-result__icon"
          >
            <Icon name={icon} filled />
          </motion.div>
        )}

        <div className="speaking-result__copy">
          <span className="speaking-result__kicker">{kicker}</span>
          <h1>{title}</h1>
          {subtitle ? <div className="speaking-result__subtitle">{subtitle}</div> : null}
        </div>

        {typeof normalizedScore === "number" && scoreLabel ? (
          <div className="speaking-result__score" aria-label={`${scoreLabel}: ${Math.round(normalizedScore)}`}>
            <ProgressRing
              value={normalizedScore / 100}
              size={140}
              trackClassName="text-ea-border"
              arcClassName="text-[color:var(--sr-accent)]"
            >
              <strong>{Math.round(normalizedScore)}</strong>
              <span>{scoreLabel}</span>
            </ProgressRing>
            {scoreContent ? <div className="speaking-result__score-content">{scoreContent}</div> : null}
          </div>
        ) : null}
      </motion.div>

      {details ? <div className="speaking-result__details">{details}</div> : null}
      {progression ? <div className="speaking-result__progression">{progression}</div> : null}
      <div className="speaking-result__actions">{actions}</div>
    </section>
  );
}
