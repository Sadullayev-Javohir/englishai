import { motion } from "framer-motion";

/**
 * Wraps its child and plays a horizontal "shake" when `shake` flips to true -
 * the Duolingo wrong-answer tell. Used for both MC tiles and the type input.
 */
export function WrongShake({
  shake,
  className,
  children,
}: {
  shake: boolean;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <motion.div
      animate={shake ? { x: [0, -10, 10, -8, 8, -4, 4, 0] } : { x: 0 }}
      transition={{ duration: 0.5 }}
      className={className}
    >
      {children}
    </motion.div>
  );
}
