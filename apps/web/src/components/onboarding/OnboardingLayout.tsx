import { motion, type Variants } from "framer-motion";

/**
 * Immersive onboarding shell (Jungle Academy spec §2 + §3).
 *
 * Full-bleed JUNGLE.jpg + scrim, centred frosted card, no left sidebar,
 * no GlobalHud - the learner sees only the jungle, the parrot, and one
 * decision per screen.
 *
 * `staggerIndex` lets each page control staggered entrance per child.
 */

const containerVariants: Variants = {
  hidden: {},
  visible: {
    transition: { staggerChildren: 0.12 },
  },
};

const childVariants: Variants = {
  hidden: { opacity: 0, y: 24 },
  visible: {
    opacity: 1,
    y: 0,
    transition: { type: "spring", stiffness: 300, damping: 28 },
  },
};

interface OnboardingLayoutProps {
  children: React.ReactNode;
  /** When true, disables stagger animation (for reduced-motion). */
  noAnimate?: boolean;
}

export function OnboardingLayout({ children, noAnimate }: OnboardingLayoutProps) {
  return (
    <div className="relative flex min-h-[100dvh] flex-col items-center justify-start overflow-x-hidden pb-[max(1.5rem,env(safe-area-inset-bottom))] pt-[max(1.25rem,env(safe-area-inset-top))] md:justify-center md:py-8">
      <motion.div
        className="relative z-10 w-full max-w-[440px] px-4 sm:px-5 md:max-w-[720px] md:px-8 min-[1200px]:max-w-[880px]"
        variants={noAnimate ? undefined : containerVariants}
        initial={noAnimate ? undefined : "hidden"}
        animate={noAnimate ? undefined : "visible"}
      >
        {noAnimate
          ? children
          : (
            <div className="flex flex-col items-center gap-6">
              {children}
            </div>
          )}
      </motion.div>
    </div>
  );
}

/**
 * Wraps a single child with the stagger animation entry.
 * Use inside OnboardingLayout when you want per-element staggered entrance.
 */
export function StaggerChild({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <motion.div variants={childVariants} className={className}>
      {children}
    </motion.div>
  );
}
