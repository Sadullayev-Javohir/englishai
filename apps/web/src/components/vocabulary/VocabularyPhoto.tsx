import { TopicImage } from "@/components/TopicImage";
import { WordImage } from "@/components/WordImage";
import type { TopicWordDto, VocabularyTopicDetailDto, VocabularyTopicSummaryDto } from "@/api/types";

const TOPIC_PHOTOS: Record<string, string> = {
  "my mother": "my-mother",
  "daily routines": "daily-routines",
  "at the market": "at-the-market",
  "travel plans": "travel-plans",
};

/** Exact photos from the approved Pen; unknown topics retain their API-backed imagery. */
export function VocabularyPhoto({ topic, className = "" }: { topic: Pick<VocabularyTopicSummaryDto, "id" | "title" | "level" | "category">; className?: string }) {
  const photo = TOPIC_PHOTOS[topic.title.toLowerCase()];
  return photo
    ? <img className={`vocabulary-photo ${className}`} src={`/assets/vocabulary/${photo}.jpg`} alt={topic.title} />
    : <TopicImage topicId={topic.id} title={topic.title} level={topic.level} category={topic.category} hideLevelBadge hideTitle className={`vocabulary-photo ${className}`} />;
}

export function VocabularyWordPhoto({ word, topic, loading = "lazy" }: { word: TopicWordDto; topic: VocabularyTopicDetailDto; loading?: "eager" | "lazy" }) {
  if (word.word.toLowerCase() === "mother" && topic.title.toLowerCase() === "my mother") {
    return <img src="/assets/vocabulary/my-mother.jpg" alt="mother — ona" loading={loading} />;
  }
  return <WordImage key={word.word} word={word} className="vocabulary-word-photo" loading={loading} />;
}
