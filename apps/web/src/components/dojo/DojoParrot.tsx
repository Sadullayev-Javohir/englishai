import { Parrot } from "@/components/gamified/Parrot";

interface DojoParrotProps {
  size?: number;
  speaking?: boolean;
  className?: string;
}

/** Pronunciation-dojo parrot: floats, and pulses while the reference clip plays. */
export function DojoParrot({ size = 132, speaking = false, className }: DojoParrotProps) {
  return <Parrot size={size} speaking={speaking} float className={className} />;
}
