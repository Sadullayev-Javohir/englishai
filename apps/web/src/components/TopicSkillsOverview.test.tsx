import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import type { TopicCompletionDto } from "@/api/types";
import { TopicSkillsOverview } from "./TopicSkillsOverview";

function CurrentPath() {
  const location = useLocation();
  return <span data-testid="current-path">{location.pathname}|{String(location.state?.lessonOrigin)}</span>;
}

function completion(speakingPassed: boolean): TopicCompletionDto {
  return {
    learnerId: "learner",
    topicId: "topic-1",
    level: "B1",
    isMastered: false,
    masteredAt: null,
    masteryThreshold: 75,
    passedModuleCount: speakingPassed ? 5 : 4,
    requiredModuleCount: 6,
    modules: [
      { module: "Vocabulary", score: 90, passed: true, unlocked: true, achievedAt: null },
      { module: "Grammar", score: 90, passed: true, unlocked: true, achievedAt: null },
      { module: "Reading", score: 90, passed: true, unlocked: true, achievedAt: null },
      { module: "Writing", score: 90, passed: true, unlocked: true, achievedAt: null },
      { module: "Speaking", score: speakingPassed ? 94 : 74, passed: speakingPassed, unlocked: true, achievedAt: null },
      { module: "Listening", score: 0, passed: false, unlocked: speakingPassed, achievedAt: null },
    ],
  };
}

function renderOverview(value: TopicCompletionDto, lessonOrigin?: "levels") {
  render(
    <MemoryRouter initialEntries={[{ pathname: "/app/speaking/topic/topic-1", state: lessonOrigin ? { lessonOrigin } : null }]}>
      <TopicSkillsOverview completion={value} topicId="topic-1" topicTitle="Travel" />
      <Routes>
        <Route path="*" element={<CurrentPath />} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("TopicSkillsOverview", () => {
  afterEach(cleanup);

  it("recommends and opens Listening after Speaking passes", () => {
    renderOverview(completion(true));

    const continueButton = screen.getByRole("button", { name: /Keyingi ko'nikmaga o'tish Listening/i });
    fireEvent.click(continueButton);

    expect(screen.getByTestId("current-path").textContent).toBe("/listening/topic/topic-1|catalog");
  });

  it("keeps the levels origin when opening the next skill", () => {
    renderOverview(completion(true), "levels");

    fireEvent.click(screen.getByRole("button", { name: /Keyingi ko'nikmaga o'tish Listening/i }));

    expect(screen.getByTestId("current-path").textContent).toBe("/listening/topic/topic-1|levels");
  });

  it("keeps Speaking as the next skill when its score is below the threshold", () => {
    renderOverview(completion(false));

    expect(screen.getByRole("button", { name: /Keyingi ko'nikmaga o'tish Speaking/i })).toBeTruthy();
  });
});
