import { motion } from "framer-motion";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

export interface MicButtonProps {
  recording: boolean;
  busy: boolean;
  disabled: boolean;
  onToggle: () => void;
  label?: string;
  recordingLabel?: string;
  labelColor?: string;
  className?: string;
}

/**
 * Big circular 3D microphone button. Blue + blue lip when idle, red + red lip + a
 * pulsing ping ring while recording. Sinks on press via a bouncy spring.
 */
export function MicButton({
  recording,
  busy,
  disabled,
  onToggle,
  label,
  recordingLabel,
  labelColor = "text-white",
  className,
}: MicButtonProps) {
  const active = recording || busy;
  const shownLabel = recording ? recordingLabel ?? label : label;

  return (
    <div className={cn("flex flex-col items-center gap-3", className)}>
      <motion.button
        type="button"
        onClick={onToggle}
        disabled={disabled}
        aria-pressed={recording}
        aria-busy={busy || undefined}
        aria-label={recording ? "Stop recording" : "Start recording"}
        transition={{ type: "spring", stiffness: 600, damping: 30 }}
        className={cn(
          "relative flex h-16 w-16 items-center justify-center rounded-full sm:h-20 sm:w-20 [@media(max-height:560px)]:h-14 [@media(max-height:560px)]:w-14",
          "outline-none transition-colors duration-100",
          "disabled:opacity-50 disabled:cursor-not-allowed",
          recording
            ? "bg-ea-danger"
            : "bg-ea-primary",
        )}
      >
        {/* Pulsing ring while recording */}
        {recording && (
          <span
            aria-hidden
            className="absolute inset-0 rounded-full bg-ea-danger/60 animate-ping"
          />
        )}
        <Icon
          name={active ? "graphic_eq" : "mic"}
          filled
          className="relative text-white text-3xl"
        />
      </motion.button>

      {shownLabel && (
        <span className={cn("font-duo font-extrabold text-sm drop-shadow", labelColor)}>
          {shownLabel}
        </span>
      )}
    </div>
  );
}
