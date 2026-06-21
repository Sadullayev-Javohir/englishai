import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { TopicCompletionDto } from "@/api/types";
import { LessonFlowAction } from "./LessonFlowAction";

vi.mock("@/api/client", () => ({
  api: { levels: { map: vi.fn() } },
}));

function CurrentPath() {
  const location = useLocation();
  return <span data-testid="path">{location.pathname}</span>;
}

function completion(grammarPassed: boolean): TopicCompletionDto {
  return {
    learnerId: "learner",
    topicId: "topic-1",
    level: "B1",
    isMastered: false,
    masteredAt: null,
    masteryThreshold: 75,
    passedModuleCount: grammarPassed ? 2 : 1,
    requiredModuleCount: 6,
    modules: [
      { module: "Vocabulary", score: 90, passed: true, unlocked: true, achievedAt: "2026-08-04T00:00:00Z" },
      { module: "Grammar", score: grammarPassed ? 90 : 74, passed: grammarPassed, unlocked: true, achievedAt: "2026-08-04T00:00:00Z" },
      { module: "Reading", score: 0, passed: false, unlocked: true, achievedAt: null },
      { module: "Writing", score: 0, passed: false, unlocked: false, achievedAt: null },
      { module: "Speaking", score: 0, passed: false, unlocked: false, achievedAt: null },
      { module: "Listening", score: 0, passed: false, unlocked: false, achievedAt: null },
    ],
  };
}

describe("LessonFlowAction", () => {
  afterEach(cleanup);

  it("opens the next module from authoritative completion state", () => {
    render(
      <MemoryRouter initialEntries={["/app/grammar/topic/topic-1"]}>
        <LessonFlowAction current="grammar" topicId="topic-1" topicTitle="Travel" completion={completion(true)} passed />
        <Routes><Route path="*" element={<CurrentPath />} /></Routes>
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByRole("button", { name: /Reading/i }));
    expect(screen.getByTestId("path").textContent).toBe("/reading");
  });

  it("allows continuing after a completed attempt below the mastery threshold", () => {
    render(
      <MemoryRouter>
        <LessonFlowAction current="grammar" topicId="topic-1" topicTitle="Travel" completion={completion(false)} passed={false} />
      </MemoryRouter>,
    );

    expect(screen.getByRole("button").hasAttribute("disabled")).toBe(false);
  });
});
