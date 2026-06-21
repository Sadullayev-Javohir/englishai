import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ErrorCategory, ProgressDataConfidence, ProgressInsightSource, SkillType } from "@/api/types";
import type { ProgressInsightDto } from "@/api/types";
import { AiInsightPanel } from "./AiInsightPanel";

const insight = {
  generatedAt: "2026-08-04T08:00:00Z",
  snapshot: {
    errorsLast30Days: [{
      category: ErrorCategory.WordOrder,
      countLast30Days: 4,
      recentExamples: [{
        id: "error-1",
        category: ErrorCategory.WordOrder,
        skill: SkillType.Grammar,
        occurredAt: "2026-08-03T08:00:00Z",
        source: null,
        sourceId: null,
        prompt: "Example",
        learnerAnswer: null,
        expectedAnswer: null,
        explanation: null,
      }],
    }],
  },
  insight: {
    source: ProgressInsightSource.Hermes,
    isFallback: false,
    overallCode: "steady",
    habitCode: "consistent",
    targetRoute: "/app/grammar",
    skillsToStrengthen: [{ skill: SkillType.Grammar, score: 62, confidence: ProgressDataConfidence.High, eightWeekDelta: 3 }],
    recurringErrors: [{ category: ErrorCategory.WordOrder, countLast30Days: 4 }],
  },
} as unknown as ProgressInsightDto;

afterEach(cleanup);

describe("AiInsightPanel", () => {
  it("keeps details collapsed until the learner requests them", () => {
    render(<AiInsightPanel insight={insight} onNavigate={vi.fn()} />);

    const toggle = screen.getByRole("button", { expanded: false });
    expect(toggle).toBeTruthy();
    expect(screen.queryByText("Example")).toBeNull();

    fireEvent.click(toggle);
    expect(screen.getByRole("button", { expanded: true })).toBeTruthy();
    expect(screen.getByText("Example")).toBeTruthy();
  });

  it("uses the insight target route for the primary action", () => {
    const onNavigate = vi.fn();
    render(<AiInsightPanel insight={insight} onNavigate={onNavigate} />);

    fireEvent.click(screen.getByRole("button", { name: /boshl/i }));
    expect(onNavigate).toHaveBeenCalledWith("/app/grammar");
  });
});
