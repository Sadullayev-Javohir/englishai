import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId } from "@/app/session";
import { useEffect } from "react";
import { useAuth } from "@/app/auth";
import { useAsync } from "@/lib/useAsync";
import { REVIEW_MILESTONE_DAYS } from "@/lib/srsStage";
import { useLocalDay } from "@/lib/useLocalDay";
import { HomeConceptLab } from "./home-concepts/HomeConceptLab";
import { buildHomeViewModel } from "./home-concepts/homeViewModel";
import { ensureCompletedHomeTopicPending, pendingHomeFallback } from "./home-concepts/homeRecommendation";
import { HOME_WORD } from "./home-concepts/homeDashboardContent";
import { buildHomeEntryPrompts } from "./home-concepts/HomeEntryModal";
import { getNativeCapabilities } from "@/api/nativeCapabilities";

export function HomePage() {
  const learnerId = getLearnerId();
  const today = useLocalDay();
  const { user } = useAuth();
  const statusState = useAsync(() => api.gamification.status(learnerId), [learnerId, today]);
  const levelState = useAsync(() => api.levels.map(learnerId), [learnerId]);
  const practiceWordsState = useAsync(() => api.speaking.practiceWords(learnerId), [learnerId]);
  const savedWordsState = useAsync(() => api.vocabulary.list(learnerId), [learnerId]);
  const dueWordsState = useAsync(() => api.vocabulary.due(learnerId), [learnerId, today]);
  const studyState = useAsync(() => api.learning.studyStats(learnerId, today), [learnerId, today]);
  const leaderboardState = useAsync(() => api.gamification.leaderboard(learnerId), [learnerId]);
  const videoState = useAsync(
    () => api.video.catalog(learnerId),
    [learnerId],
    getNativeCapabilities().video,
  );

  const activeTopic = levelState.data?.topics.find((topic) => topic.id === levelState.data?.activeTopicId) ?? null;
  const dueWords = dueWordsState.data ?? [];
  const dueTopicIds = new Set(dueWords.map(word => word.sourceTopicId));
  const dueTopic = dueTopicIds.size === 1
    ? levelState.data?.topics.find(topic => topic.id === dueWords[0]?.sourceTopicId)
    : null;
  const dueStage = dueWords.length ? Math.min(...dueWords.map(word => word.stage)) : -1;
  useEffect(() => {
    ensureCompletedHomeTopicPending(
      learnerId,
      levelState.data?.activeTopicId ?? null,
      (levelState.data?.topicsMastered ?? 0) > 0,
    );
  }, [learnerId, levelState.data?.activeTopicId, levelState.data?.topicsMastered]);
  const model = buildHomeViewModel({
    name: user?.displayName?.trim() || uz.home.defaultName,
    pictureUrl: user?.pictureUrl,
    activeTopic,
    activeTopicLevel: levelState.data?.level,
    fallbackKind: pendingHomeFallback(learnerId),
    streakAtRisk: statusState.data?.isStreakAtRisk,
    goalMet: statusState.data?.isGoalMet,
    currentStreak: statusState.data?.currentStreak,
    dailyGoalTarget: statusState.data?.dailyGoalTarget,
    todayCompletedTasks: statusState.data?.todayCompletedTasks,
    skillsCompletedToday: statusState.data?.skillsCompletedToday,
    practiceWordCount: practiceWordsState.data?.length ?? 0,
    savedCount: savedWordsState.data?.length ?? 0,
    dueSavedCount: dueWords.length,
    dueTopicTitle: dueTopic?.title,
    dueReviewDay: REVIEW_MILESTONE_DAYS[dueStage],
    leaderboardRank: leaderboardState.data?.currentUserEntry?.rank
      ?? leaderboardState.data?.top.find(entry => entry.isCurrentUser)?.rank,
    weekSeconds: studyState.data?.weekSeconds,
    last7Days: studyState.data?.last7Days,
  });
  const loading = [statusState, levelState, practiceWordsState, savedWordsState, studyState].some((state) => state.loading && !state.data);
  const entryPromptsReady = !loading && !dueWordsState.loading && !videoState.loading;
  const entryPrompts = entryPromptsReady
    ? buildHomeEntryPrompts({
        savedWords: savedWordsState.data ?? [],
        dueWords,
        currentStreak: statusState.data?.currentStreak ?? 0,
        activeTopic: activeTopic
          ? { id: activeTopic.id, title: activeTopic.title, level: levelState.data?.level ?? 1 }
          : null,
        videos: videoState.data ?? [],
      })
    : undefined;
  const error = statusState.error ?? levelState.error ?? practiceWordsState.error ?? savedWordsState.error ?? studyState.error;
  const retry = () => {
    statusState.reload();
    levelState.reload();
    practiceWordsState.reload();
    savedWordsState.reload();
    dueWordsState.reload();
    studyState.reload();
    leaderboardState.reload();
    videoState.reload();
  };

  return <HomeConceptLab model={model} loading={loading} error={error} onRetry={retry} wordSaved={savedWordsState.data?.some(word => word.word.toLowerCase() === HOME_WORD.word)} onWordSaved={savedWordsState.refresh} entryPrompts={entryPrompts} entryDay={today} />;
}
