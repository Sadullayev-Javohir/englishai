import { AnimatePresence } from "framer-motion";
import { RewardPop as BaseRewardPop } from "@/components/game/RewardPop";

interface DojoRewardPopProps {
  show: boolean;
  label: string;
  className?: string;
}

/** Mount/unmount wrapper around the game layer's RewardPop, driven by a `show` flag. */
export function RewardPop({ show, label, className }: DojoRewardPopProps) {
  return (
    <AnimatePresence>
      {show && <BaseRewardPop label={label} className={className} />}
    </AnimatePresence>
  );
}
