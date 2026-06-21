import { DesignSheet } from "@/components/design";
import { uz } from "@/content/uz";

interface BottomSheetProps {
  open: boolean;
  onClose: () => void;
  title?: string;
  /** Hides the default close (X) button when the sheet has its own dismissal affordance. */
  hideClose?: boolean;
  children: React.ReactNode;
  className?: string;
}

/**
 * Native-style bottom sheet: a backdrop + a panel that slides up from the bottom edge, with a
 * drag handle and safe-area padding. This is the mobile replacement for centered modals - it
 * keeps actions within thumb reach and matches the platform feel. Reused by LessonsSheet and the
 * mobile variants of TutorNameModal / Paywall / word-detail.
 *
 * Rendered unconditionally so the exit can animate later if needed; when closed it renders
 * nothing. Locks body scroll while open and closes on Escape / backdrop tap.
 */
export function BottomSheet({ open, onClose, title, hideClose, children, className }: BottomSheetProps) {
  return (
    <DesignSheet
      open={open}
      onClose={onClose}
      title={title ?? uz.lessonsSheet.title}
      closeLabel={uz.common.close}
      showClose={!hideClose}
      className={className}
    >
      {children}
    </DesignSheet>
  );
}
