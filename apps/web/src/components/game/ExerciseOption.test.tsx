import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { ExerciseOption, type ExerciseAccent } from "./ExerciseOption";

const ACCENTS: ExerciseAccent[] = ["blue", "purple", "orange", "teal"];

describe("ExerciseOption", () => {
  afterEach(cleanup);

  it("keeps every idle option on the same neutral palette regardless of accent", () => {
    const { rerender } = render(<ExerciseOption label="Option" />);
    const neutralClassName = screen.getByRole("button").className;

    for (const accent of ACCENTS) {
      rerender(<ExerciseOption label="Option" accent={accent} />);
      expect(screen.getByRole("button").className).toBe(neutralClassName);
    }
  });

  it("preserves selected, feedback, dim, disabled and keyboard-focus states", () => {
    const { rerender } = render(<ExerciseOption label="Option" selected />);
    expect(screen.getByRole("button").className).toContain("ring-2");
    expect(screen.getByRole("button").className).toContain("focus-visible:");

    rerender(<ExerciseOption label="Option" state="correct" />);
    expect(screen.getByRole("button").className).toContain("ea-card--success");

    rerender(<ExerciseOption label="Option" state="wrong" />);
    expect(screen.getByRole("button").className).toContain("ea-card--danger");

    rerender(<ExerciseOption label="Option" state="dim" />);
    expect(screen.getByRole("button").className).toContain("opacity-");

    rerender(<ExerciseOption label="Option" disabled />);
    expect(screen.getByRole("button").hasAttribute("disabled")).toBe(true);
  });
});
