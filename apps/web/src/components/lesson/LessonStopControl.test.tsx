import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LessonStopControl } from "./LessonStopControl";
import { LessonStageFrame } from "./LessonStageFrame";

describe("LessonStopControl", () => {
  afterEach(cleanup);
  it("awaits the confirmed stop action", async () => {
    const onConfirm = vi.fn().mockResolvedValue(undefined);
    render(<LessonStopControl onConfirm={onConfirm} />);
    fireEvent.click(screen.getByRole("button", { name: "To‘xtatish" }));
    fireEvent.click(screen.getByRole("button", { name: "Ha, to‘xtatish" }));
    await waitFor(() => expect(onConfirm).toHaveBeenCalledTimes(1));
  });

  it("shows an error and allows retry when stopping fails", async () => {
    const onConfirm = vi.fn().mockRejectedValueOnce(new Error("network")).mockResolvedValue(undefined);
    render(<LessonStopControl onConfirm={onConfirm} />);
    fireEvent.click(screen.getByRole("button", { name: "To‘xtatish" }));
    fireEvent.click(screen.getByRole("button", { name: "Ha, to‘xtatish" }));
    expect(await screen.findByRole("alert")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Ha, to‘xtatish" }));
    await waitFor(() => expect(onConfirm).toHaveBeenCalledTimes(2));
  });

  it("renders inside the lesson card top-right action slot", () => {
    render(
      <LessonStageFrame topRightAction={<LessonStopControl onConfirm={() => undefined} />}>
        Test body
      </LessonStageFrame>,
    );

    const slot = screen.getByRole("button", { name: "To‘xtatish" }).closest("[data-lesson-stage-top-right-action]");
    expect(slot).toBeTruthy();
  });
});
