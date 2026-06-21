import { useEffect, useRef, useState } from "react";
import { motion, useReducedMotion } from "framer-motion";
import { Icon } from "@/components/ui/Icon";

export interface LandingDemoMediaProps {
  src: string;
  poster: string;
  label: string;
  accent: string;
}

export function LandingDemoMedia({ src, poster, label, accent }: LandingDemoMediaProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const reduceMotion = useReducedMotion();
  const [available, setAvailable] = useState(true);
  const [inView, setInView] = useState(false);

  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    if (typeof IntersectionObserver === "undefined") {
      return;
    }

    const observer = new IntersectionObserver(([entry]) => setInView(entry.isIntersecting), {
      rootMargin: "120px 0px",
      threshold: 0.35,
    });
    observer.observe(video);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    const video = videoRef.current;
    if (typeof IntersectionObserver === "undefined") return;
    if (!video || reduceMotion || !inView || !available) {
      video?.pause();
      return;
    }
    void video.play().catch(() => undefined);
  }, [available, inView, reduceMotion]);

  return (
    <motion.figure
      className="pl-demo-media"
      style={{ "--demo-accent": accent } as React.CSSProperties}
      initial={reduceMotion ? false : { opacity: 0, y: 30, rotateY: -4 }}
      whileInView={{ opacity: 1, y: 0, rotateY: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.55 }}
    >
      <div className="pl-demo-media__chrome" aria-hidden="true"><span /><span /><span /><strong>EnglishAI</strong></div>
      {available ? (
        <video
          ref={videoRef}
          data-testid={`landing-demo-${label.toLowerCase()}`}
          muted
          loop
          playsInline
          preload="none"
          poster={poster}
          aria-label={`${label} qisqa mahsulot namoyishi`}
          onError={() => setAvailable(false)}
        >
          <source src={src} type="video/mp4" />
        </video>
      ) : (
        <div className="pl-demo-media__fallback" role="img" aria-label={`${label} mahsulot ko'rinishi`}>
          <Icon name="play_circle" />
          <strong>{label}</strong>
          <span>Interaktiv mahsulot namoyishi</span>
        </div>
      )}
      <figcaption><Icon name="play_circle" filled /><span>{label} · real o'rganish oqimi</span></figcaption>
    </motion.figure>
  );
}
