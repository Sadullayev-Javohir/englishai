import { useLessonProgress } from "@/lib/lessonProgress";

/** Device-local reading bookmarks, scoped by the shared learner storage helper. */
export function useReadingBookmarks() {
  const [ids, setIds] = useLessonProgress<string[]>("reading.bookmarks", []);
  return {
    ids,
    has: (id: string) => ids.includes(id),
    toggle: (id: string) => setIds(current => current.includes(id) ? current.filter(item => item !== id) : [...current, id]),
  };
}
