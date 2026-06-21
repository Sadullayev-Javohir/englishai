import { motion } from "framer-motion";

type FriendMood = "idle" | "happy" | "cheer";

interface FriendProps {
  mood?: FriendMood;
  size?: number;
  className?: string;
}

/**
 * Friend - the app's parrot mascot (brand to'tiqush), drawn as a clean on-brand SVG (no copy
 * of the deprecated duo/Parrot). A gentle idle bob, plus a happy bounce / cheer wing-raise for
 * milestone moments. `key` changes drive the mood animation.
 */
export function Friend({ mood = "idle", size = 96, className }: FriendProps) {
  const idle = {
    y: [0, -4, 0],
    rotate: mood === "cheer" ? [0, -8, 0] : [0, 0, 0],
  };
  const happy = { scale: [1, 1.12, 1], y: [0, -6, 0] };
  const cheer = { y: [0, -10, 0], rotate: [0, -10, 0, -6, 0] };

  const animate = mood === "cheer" ? cheer : mood === "happy" ? happy : idle;

  return (
    <motion.svg
      width={size}
      height={size}
      viewBox="0 0 120 120"
      className={className}
      animate={animate}
      transition={
        mood === "idle"
          ? { duration: 2.4, repeat: Infinity, ease: "easeInOut" }
          : { type: "spring", stiffness: 300, damping: 16 }
      }
    >
      {/* tail */}
      <path d="M44 86 C20 96 18 116 30 118 C40 116 50 104 54 92 Z" fill="#B8D7E9" />
      <path d="M50 88 C34 102 34 118 44 120 C52 116 58 104 60 94 Z" fill="#28719B" />
      {/* body */}
      <ellipse cx="64" cy="76" rx="30" ry="34" fill="#B9D7AA" />
      <ellipse cx="64" cy="82" rx="20" ry="24" fill="#FFD700" />
      {/* wing */}
      <path
        d="M84 64 C104 70 106 96 92 102 C86 92 84 80 82 70 Z"
        fill="#568744"
      />
      {/* head */}
      <circle cx="62" cy="44" r="26" fill="#B9D7AA" />
      {/* cheek */}
      <circle cx="54" cy="50" r="9" fill="#FFD700" opacity="0.9" />
      {/* eye */}
      <circle cx="60" cy="40" r="7" fill="#fff" />
      <circle cx="61" cy="41" r="3.6" fill="#172017" />
      <circle cx="62.2" cy="39.6" r="1.1" fill="#fff" />
      {/* beak */}
      <path d="M84 42 C98 44 98 56 84 54 C80 50 80 46 84 42 Z" fill="#D97745" />
      <path d="M84 53 C92 54 92 60 84 60 C82 57 82 55 84 53 Z" fill="#D97745" />
    </motion.svg>
  );
}
