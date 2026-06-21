import { act, render } from "@testing-library/react";
import { MemoryRouter, useNavigate } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { ScrollToTop } from "./ScrollToTop";

function Harness() {
  const navigate = useNavigate();
  return <><ScrollToTop /><button onClick={() => navigate("/second")}>next</button></>;
}

describe("ScrollToTop", () => {
  it("disables browser restoration and resets the document after route layout", () => {
    const scrollTo = vi.spyOn(window, "scrollTo").mockImplementation(() => undefined);
    const frame = vi.spyOn(window, "requestAnimationFrame").mockImplementation((callback) => {
      callback(0);
      return 1;
    });
    Object.defineProperty(window.history, "scrollRestoration", { configurable: true, writable: true, value: "auto" });
    document.documentElement.scrollTop = 500;
    document.body.scrollTop = 500;
    const view = render(<MemoryRouter initialEntries={["/first"]}><Harness /></MemoryRouter>);
    expect(window.history.scrollRestoration).toBe("manual");
    expect(document.documentElement.scrollTop).toBe(0);
    expect(document.body.scrollTop).toBe(0);
    expect(scrollTo).toHaveBeenCalledWith({ top: 0, left: 0, behavior: "auto" });
    act(() => view.getByRole("button", { name: "next" }).click());
    expect(frame).toHaveBeenCalled();
    expect(document.documentElement.scrollTop).toBe(0);
  });
});