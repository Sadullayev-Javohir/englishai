import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import { usePaywall } from "@/components/PaywallProvider";
import type { TopicGate } from "@/lib/skillTopicGates";

/**
 * Shared card chrome for a topic in any skill catalog (grammar, reading, listening, writing). It
 * applies the sequential-unlock rule consistently: a locked topic is dimmed, non-interactive and
 * shows a lock hint; a passed topic gets a green "tugatildi" footer; an open topic behaves as a
 * normal clickable card. The skill-specific body (icon, title, focus label, …) is passed as
 * children so each catalog keeps its own look.
 */
export function SkillTopicCard({
  gate,
  onOpen,
  image,
  children,
}: {
  gate: TopicGate;
  onOpen: () => void;
  /** Optional topic thumbnail rendered full-bleed at the top of the card. */
  image?: React.ReactNode;
  children: React.ReactNode;
}) {
  const { open: openPaywall } = usePaywall();

  // The K.5 sequential lock is the first blocker (the previous topic must be mastered). Once that
  // clears, the trial paywall (H.1) takes over for topics past the free allowance: the card stays
  // clickable but opens the upgrade paywall instead of the lesson.
  const locked = gate.isLocked;
  const proLocked = !locked && gate.requiresPro;

  const body = (
    <>
      {children}
      {locked ? (
        <span className="inline-flex items-center gap-xs mt-sm text-text-secondary font-caption text-caption">
          <Icon name="lock" filled className="text-[14px]" />
          {uz.skillTopic.lockedHint}
        </span>
      ) : proLocked ? (
        <span className="inline-flex items-center gap-xs mt-sm text-accent font-caption text-caption">
          <Icon name="workspace_premium" filled className="text-[14px]" />
          {uz.skillTopic.proHint}
        </span>
      ) : gate.passed ? (
        <span className="inline-flex items-center gap-xs mt-sm text-success font-caption text-caption">
          <Icon name="check_circle" filled className="text-[14px]" />
          {uz.skillTopic.done}
        </span>
      ) : null}
    </>
  );

  return (
    <button
      type="button"
      disabled={locked}
      onClick={() => {
        if (locked) return;
        if (proLocked) openPaywall();
        else onOpen();
      }}
      className={cn(
        "group relative min-h-11 w-full overflow-hidden rounded-xl border bg-surface text-left transition-all",
        // Without a thumbnail the body keeps its original padding; with one the image is
        // full-bleed at the top and the padding moves to the body wrapper below it. Padding is
        // tighter on the phone shell (compact native cards) and full from md.
        image ? "" : "p-sm min-[390px]:p-md md:p-lg",
        locked
          ? "opacity-60 cursor-default border-border"
          : "hover:shadow-lg  cursor-pointer " +
              (proLocked ? "border-accent/40" : gate.passed ? "border-success/40" : "border-border"),
      )}
    >
      {proLocked && (
        <span className="absolute top-2 right-2 z-10 inline-flex items-center gap-xs bg-accent text-white font-caption text-caption px-2 py-[2px] rounded-full shadow-sm">
          <Icon name="workspace_premium" filled className="text-[12px]" />
          {uz.skillTopic.proBadge}
        </span>
      )}
      {image ? (
        <>
          {image}
          <div className="p-sm min-[390px]:p-md md:p-lg">{body}</div>
        </>
      ) : (
        body
      )}
    </button>
  );
}
