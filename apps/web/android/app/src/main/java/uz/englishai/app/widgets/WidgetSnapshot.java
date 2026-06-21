package uz.englishai.app.widgets;

import java.util.Arrays;

public record WidgetSnapshot(
    int[] skillScores,
    boolean[] completedSkills,
    int completedCount,
    int leaderboardRank,
    String leagueName
) {
    public static final int SKILL_COUNT = 6;

    public static WidgetSnapshot create(
        int[] rawScores,
        boolean[] rawCompleted,
        int completedCount,
        int leaderboardRank,
        String leagueName
    ) {
        int[] scores = new int[SKILL_COUNT];
        boolean[] completed = new boolean[SKILL_COUNT];
        if (rawScores != null) {
            for (int index = 0; index < Math.min(rawScores.length, SKILL_COUNT); index++) {
                scores[index] = Math.max(0, Math.min(100, rawScores[index]));
            }
        }
        if (rawCompleted != null) {
            System.arraycopy(rawCompleted, 0, completed, 0, Math.min(rawCompleted.length, SKILL_COUNT));
        }
        String normalizedLeague = leagueName == null || leagueName.isBlank()
            ? "Liga aniqlanmagan"
            : leagueName.trim();
        return new WidgetSnapshot(
            scores,
            completed,
            Math.max(0, Math.min(SKILL_COUNT, completedCount)),
            Math.max(0, leaderboardRank),
            normalizedLeague);
    }

    public WidgetSnapshot {
        skillScores = Arrays.copyOf(skillScores, SKILL_COUNT);
        completedSkills = Arrays.copyOf(completedSkills, SKILL_COUNT);
    }

    @Override
    public int[] skillScores() {
        return Arrays.copyOf(skillScores, SKILL_COUNT);
    }

    @Override
    public boolean[] completedSkills() {
        return Arrays.copyOf(completedSkills, SKILL_COUNT);
    }
}
