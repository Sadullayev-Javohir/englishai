import { cleanup, render } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { AppPending } from "./AppPending";

afterEach(() => {
  cleanup();
  window.history.replaceState({}, "", "/");
  delete document.documentElement.dataset.appBooting;
});

describe("AppPending", () => {
  it("does not render the generic first skeleton on /home", () => {
    window.history.replaceState({}, "", "/home");
    document.documentElement.dataset.appBooting = "true";

    const view = render(<AppPending />);

    expect(view.container.firstChild).toBeNull();
    expect(document.querySelector(".ea-app-pending")).toBeNull();
    expect(document.documentElement.dataset.appBooting).toBeUndefined();
  });

  it("keeps the generic pending skeleton on other routes", () => {
    window.history.replaceState({}, "", "/levels");

    const view = render(<AppPending />);

    expect(view.container.querySelector(".ea-app-pending")).toBeTruthy();
  });
});
