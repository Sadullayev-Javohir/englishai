import { beforeEach, describe, expect, it } from "vitest";
import {
  consumeHomeFallback,
  ensureCompletedHomeTopicPending,
  markHomeTopicCompleted,
  pendingHomeFallback,
  rememberActiveHomeTopic,
} from "./homeRecommendation";

const learnerId = "learner-a";

describe("homeRecommendation", () => {
  beforeEach(() => localStorage.clear());

  it("keeps a completed topic pending until the fallback card is clicked", () => {
    markHomeTopicCompleted(learnerId, "travel");

    expect(pendingHomeFallback(learnerId)).toBe("books");
    expect(pendingHomeFallback(learnerId)).toBe("books");
  });

  it("alternates library and video only after consumption", () => {
    markHomeTopicCompleted(learnerId, "travel");
    consumeHomeFallback(learnerId);
    markHomeTopicCompleted(learnerId, "work");

    expect(pendingHomeFallback(learnerId)).toBe("video");
    consumeHomeFallback(learnerId);
    markHomeTopicCompleted(learnerId, "family");
    expect(pendingHomeFallback(learnerId)).toBe("books");
  });

  it("detects an externally completed topic when the roadmap frontier advances", () => {
    rememberActiveHomeTopic(learnerId, "travel");
    ensureCompletedHomeTopicPending(learnerId, "work", true);

    expect(pendingHomeFallback(learnerId)).toBe("books");
  });
});
