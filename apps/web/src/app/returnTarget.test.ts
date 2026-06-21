import { beforeEach, describe, expect, it } from "vitest";
import {
  clearReturnTarget,
  getReturnTarget,
  normalizeReturnTarget,
  rememberReturnTarget,
  returnTargetOr,
} from "./returnTarget";

describe("returnTarget", () => {
  beforeEach(() => sessionStorage.clear());

  it("preserves an internal route with query and hash", () => {
    rememberReturnTarget("/video/lesson-1/quiz?mode=review#question-2");

    expect(getReturnTarget()).toBe("/video/lesson-1/quiz?mode=review#question-2");
    expect(returnTargetOr("/home")).toBe("/video/lesson-1/quiz?mode=review#question-2");
  });

  it.each([
    "https://attacker.example/path",
    "//attacker.example/path",
    "/login?next=/admin",
    "/username",
    "/welcome",
    "/assessment",
    "/placement",
    "/placement/result",
    "/onboarding/goal",
    "/app/vocabulary/review",
  ])("rejects unsafe or gate target %s", (value) => {
    expect(normalizeReturnTarget(value)).toBeNull();
  });

  it("clears a stored target after the destination renders", () => {
    rememberReturnTarget("/app/speaking/free-talk?topic=travel");
    clearReturnTarget();

    expect(getReturnTarget()).toBeNull();
    expect(returnTargetOr("/home")).toBe("/home");
  });
});
