package uz.englishai.app.widgets;

import static org.junit.Assert.assertArrayEquals;
import static org.junit.Assert.assertEquals;

import org.junit.Test;

public class WidgetSnapshotTest {
    @Test
    public void normalizesSixSkillsAndCompletedFlags() {
        WidgetSnapshot snapshot = WidgetSnapshot.create(
            new int[] { 81, -5, 105 },
            new boolean[] { true, false },
            4,
            27,
            "Oltin liga");

        assertArrayEquals(new int[] { 81, 0, 100, 0, 0, 0 }, snapshot.skillScores());
        assertArrayEquals(new boolean[] { true, false, false, false, false, false }, snapshot.completedSkills());
        assertEquals(4, snapshot.completedCount());
        assertEquals(27, snapshot.leaderboardRank());
        assertEquals("Oltin liga", snapshot.leagueName());
    }

    @Test
    public void protectsWidgetFromNegativeCountsAndBlankLeague() {
        WidgetSnapshot snapshot = WidgetSnapshot.create(null, null, -2, -8, "  ");

        assertEquals(0, snapshot.completedCount());
        assertEquals(0, snapshot.leaderboardRank());
        assertEquals("Liga aniqlanmagan", snapshot.leagueName());
    }
}
