import { useState } from "react";
import type { TopicWordDto } from "@/api/types";
import { cn } from "@/lib/cn";
import { Icon } from "@/components/ui/Icon";
import { ILLUSTRATION_GRADIENTS, hashString } from "@/lib/illustrationGradients";

const ICON_RULES: ReadonlyArray<{ icon: string; words: readonly string[] }> = [
  { icon: "group", words: ["family", "parent", "mother", "father", "sister", "brother", "friend", "people", "person", "child", "children", "baby"] },
  { icon: "home", words: ["home", "house", "room", "kitchen", "bedroom", "door", "window", "garden"] },
  { icon: "restaurant", words: ["food", "eat", "meal", "breakfast", "lunch", "dinner", "restaurant", "bread", "rice", "water", "drink", "coffee", "tea"] },
  { icon: "apple", words: ["apple", "fruit", "vegetable", "healthy"] },
  { icon: "flight_takeoff", words: ["travel", "trip", "airport", "plane", "flight", "holiday", "journey", "visit"] },
  { icon: "map", words: ["city", "country", "place", "street", "road", "map", "direction", "world"] },
  { icon: "school", words: ["school", "student", "teacher", "study", "learn", "lesson", "class", "education", "exam"] },
  { icon: "menu_book", words: ["book", "read", "story", "text", "library", "word", "write", "language"] },
  { icon: "work", words: ["work", "job", "office", "business", "career", "company", "money"] },
  { icon: "pets", words: ["animal", "pet", "dog", "cat", "bird", "fish", "horse"] },
  { icon: "fitness_center", words: ["sport", "exercise", "run", "walk", "jump", "play", "football", "game", "strong"] },
  { icon: "favorite", words: ["love", "like", "heart", "kind", "care", "happy", "feel", "emotion"] },
  { icon: "schedule", words: ["time", "day", "week", "month", "year", "morning", "evening", "today", "tomorrow"] },
  { icon: "light_mode", words: ["sun", "summer", "hot", "light", "weather", "warm"] },
  { icon: "cloud_off", words: ["rain", "cloud", "cold", "winter", "snow", "weather"] },
  { icon: "smartphone", words: ["phone", "mobile", "computer", "internet", "technology", "online", "message"] },
  { icon: "shopping_bag", words: ["shop", "shopping", "buy", "sell", "market", "clothes", "shirt", "dress"] },
  { icon: "health_and_safety", words: ["health", "doctor", "hospital", "medicine", "sick", "body"] },
  { icon: "nature", words: ["nature", "tree", "flower", "plant", "forest", "environment", "green"] },
  { icon: "music_note", words: ["music", "song", "sing", "dance", "listen"] },
];

const FALLBACK_ICONS = ["lightbulb", "explore", "auto_stories", "translate", "psychology", "public", "rocket_launch", "star"];

interface WordImageProps {
  word: TopicWordDto;
  className?: string;
  loading?: "eager" | "lazy";
}

export function WordImage({ word, className, loading = "lazy" }: WordImageProps) {
  const [failed, setFailed] = useState(false);
  const seed = `${word.word}:${word.translation}`;
  const artIndex = hashString(seed) % ILLUSTRATION_GRADIENTS.length;
  const icon = semanticIconFor(word.word, word.translation);
  const localImageUrl = word.imageUrl?.startsWith("/api/images/vocabulary-topics/")
    ? word.imageUrl
    : null;

  if (localImageUrl && !failed) {
    return (
      <div className={cn("relative isolate overflow-hidden bg-black/15", className)}>
        <img
          src={localImageUrl}
          alt={`${word.word} — ${word.translation}`}
          loading={loading}
          decoding="async"
          onError={() => setFailed(true)}
          className="h-full w-full object-cover transition-transform duration-500 hover:scale-[1.04]"
        />
        <span aria-hidden className="pointer-events-none absolute inset-0 ring-1 ring-inset ring-white/20" />
      </div>
    );
  }

  return (
    <div
      data-safe-art={`word-${artIndex}`}
      className={cn(
        "relative isolate flex flex-col items-center justify-center overflow-hidden bg-ea-primary p-md text-center text-ea-on-primary",
        ILLUSTRATION_GRADIENTS[artIndex],
        className,
      )}
    >
      <span aria-hidden className="absolute -right-10 -top-12 h-32 w-32 rounded-full bg-white/15" />
      <span aria-hidden className="absolute -bottom-12 -left-10 h-32 w-32 rounded-full bg-black/15" />
      <span aria-hidden className="absolute right-5 top-5 h-5 w-5 rotate-12 rounded-md border-2 border-white/25" />
      <span className="relative flex h-20 w-20 items-center justify-center rounded-3xl bg-white/20  ring-2 ring-white/30">
        <Icon name={icon} filled className="text-[42px] text-white" aria-hidden />
      </span>
    </div>
  );
}

function semanticIconFor(word: string, translation: string): string {
  const normalized = `${word} ${translation}`.toLocaleLowerCase("en-US");
  const match = ICON_RULES.find((rule) => rule.words.some((keyword) => containsWord(normalized, keyword)));
  return match?.icon ?? FALLBACK_ICONS[hashString(normalized) % FALLBACK_ICONS.length];
}

function containsWord(value: string, keyword: string): boolean {
  return new RegExp(`(^|[^a-z])${keyword.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}([^a-z]|$)`, "i").test(value);
}
