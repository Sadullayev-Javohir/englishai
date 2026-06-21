import { useState } from "react";
import { UserRound } from "lucide-react";

export function YouTubeChannelAvatar({ channel, url }: { channel: string; url?: string | null }) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null);
  const source = url?.startsWith("https://") && failedUrl !== url ? url : null;

  return (
    <span className="video-catalog__avatar">
      {source
        ? <img src={source} alt={`${channel} kanal logosi`} loading="lazy" decoding="async" referrerPolicy="no-referrer" onError={() => setFailedUrl(source)} />
        : <UserRound size={20} aria-hidden />}
    </span>
  );
}
