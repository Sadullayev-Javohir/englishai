import { describe, expect, it } from "vitest";
import { getRouteMetadata, isRouteSection } from "./routeMetadata";

describe("route metadata", () => {
  it("classifies titles and active navigation consistently", () => {
    expect(getRouteMetadata("/writing/topic/travel")).toMatchObject({
      title: "Writing",
      documentTitle: "Writing darsi",
      section: "lessons",
      mobileBack: true,
    });
    expect(isRouteSection("/app/speaking/free-talk/travel", "speaking")).toBe(true);
    expect(getRouteMetadata("/app/speaking/topics").documentTitle).toBe("Mavzuli suhbatlar");
    expect(isRouteSection("/leaderboard/weekly", "leaderboard")).toBe(true);
  });

  it("maps public, onboarding, admin and unknown routes to browser titles", () => {
    expect(getRouteMetadata("/").documentTitle).toBe("AI bilan ingliz tilini o‘rganing");
    expect(getRouteMetadata("/login").documentTitle).toBe("Kirish");
    expect(getRouteMetadata("/placement/result").documentTitle).toBe("Daraja natijasi");
    expect(getRouteMetadata("/admin/server").documentTitle).toBe("Server holati");
    expect(getRouteMetadata("/admin/vocabulary-images")).toMatchObject({
      documentTitle: "So‘z rasmlarini boshqarish",
      section: "admin",
      homeBack: true,
      mobileBack: true,
    });
    expect(getRouteMetadata("/missing-page").documentTitle).toBe("Sahifa topilmadi");
  });

  it("classifies immersive lesson routes before their catalog prefixes", () => {
    expect(getRouteMetadata("/app/speaking/free").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/app/speaking/topic/family").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/app/speaking/free-talk/travel").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/app/speaking/role-talk/restaurant").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/app/speaking/pronunciation/hello").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/app/vocabulary/saved/family/practice").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/writing/task/travel").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/writing/topic/travel").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/video/lesson-1/play").shellMode).toBe("lessonFrame");
    expect(getRouteMetadata("/video").shellMode).toBe("standard");
    expect(getRouteMetadata("/app/speaking").shellMode).toBe("standard");
    expect(getRouteMetadata("/app/speaking/practice-words").shellMode).toBe("standard");
  });
});
