import { render, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { uz } from "@/content/uz";
import { ModulePageLoader } from "./ModulePageLoader";

describe("ModulePageLoader", () => {
  it("announces the default loading state", () => {
    const view = render(<ModulePageLoader icon="menu_book" accent="blue" />);

    const status = within(view.container).getByRole("status");
    expect(status.getAttribute("aria-live")).toBe("polite");
    expect(status.getAttribute("data-module-loader")).toBe("page");
    expect(status.className).toContain("ea-loading-skeleton--detail");
    expect(status.getAttribute("aria-label")).toBe(uz.common.loading);
  });

  it("supports an embedded loading state and custom label", () => {
    const view = render(<ModulePageLoader icon="headphones" label="Loading" embedded />);

    expect(within(view.container).getByRole("status").getAttribute("data-module-loader")).toBe("embedded");
    expect(within(view.container).getByRole("status").getAttribute("aria-label")).toBe("Loading");
  });
});
