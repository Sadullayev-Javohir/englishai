import { AnimatePresence, motion } from "framer-motion";
import { NavLink } from "react-router-dom";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";

/**
 * Slide-down nav menu for the global Nav HUD (Jungle Academy spec §2.2).
 * Icon pills in the HUD open this frosted popover below the bar.
 * Canonical game-layer copy (migrated from duo/*).
 */

interface NavMenuProps {
  open: boolean;
  items: { to: string; icon: string; label: string }[];
  current: string;
  onClose: () => void;
}

export function NavMenu({ open, items, current, onClose }: NavMenuProps) {
  return (
    <AnimatePresence>
      {open && (
        <>
          <div className="fixed inset-0 z-30" onClick={onClose} aria-hidden />
          <motion.div
            initial={{ opacity: 0, y: -8, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -8, scale: 0.96 }}
            transition={{ type: "spring", stiffness: 400, damping: 30 }}
            className="absolute right-0 top-12 z-40 w-52 rounded-2xl bg-ea-surface/95 p-2  "
          >
            {items.map((item) => {
              const active =
                current === item.to || current.startsWith(item.to + "/");
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  onClick={onClose}
                  className={cn(
                    "flex items-center gap-3 rounded-xl px-3 py-2 font-duo font-bold transition-colors",
                    active
                      ? "bg-ea-green-600/15 text-ea-green-600"
                      : "text-ea-green-900 hover:bg-black/5",
                  )}
                >
                  <Icon name={item.icon} filled={active} className="text-[20px]" />
                  <span>{item.label}</span>
                </NavLink>
              );
            })}
          </motion.div>
        </>
      )}
    </AnimatePresence>
  );
}
