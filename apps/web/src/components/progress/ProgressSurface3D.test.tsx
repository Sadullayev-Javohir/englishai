import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { ProgressIcon3D, ProgressSurface3D, type ProgressAccent } from "./ProgressSurface3D";

const ACCENTS: ProgressAccent[] = ["green", "blue", "purple", "red", "yellow", "teal", "orange", "amber", "pink", "brown"];

afterEach(cleanup);

describe("ProgressSurface3D", () => {
  it("keeps every card on the same neutral surface regardless of accent", () => {
    const { rerender } = render(<ProgressSurface3D accent="green">Content</ProgressSurface3D>);
    const baseClassName = screen.getByText("Content").parentElement?.parentElement?.className;

    for (const accent of ACCENTS) {
      rerender(<ProgressSurface3D accent={accent}>Content</ProgressSurface3D>);
      expect(screen.getByText("Content").parentElement?.parentElement?.className).toBe(baseClassName);
    }
  });

  it("renders interactive surfaces as accessible buttons", () => {
    render(<ProgressSurface3D accent="blue" as="button" interactive>Open details</ProgressSurface3D>);

    const button = screen.getByRole("button", { name: "Open details" });
    expect(button.getAttribute("type")).toBe("button");
    expect(button.className).toContain("focus-visible:");
  });

  it("uses accent color only inside the icon treatment", () => {
    const { rerender } = render(<ProgressIcon3D accent="blue" icon={<span>Icon</span>} />);
    const blueClassName = screen.getByText("Icon").parentElement?.className;

    rerender(<ProgressIcon3D accent="purple" icon={<span>Icon</span>} />);
    expect(screen.getByText("Icon").parentElement?.className).not.toBe(blueClassName);
  });
});
