import { act, render, screen } from "@testing-library/react";
import { Suspense } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { lazyPage, setChunkReloadForTests } from "./lazyPage";

describe("lazyPage", () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    setChunkReloadForTests(null);
  });

  it("renders the requested named export", async () => {
    const Page = lazyPage(
      async () => ({ ExamplePage: () => <h1>Loaded</h1> }),
      "ExamplePage"
    );

    await act(async () => {
      render(
        <Suspense fallback={<p>Loading</p>}>
          <Page />
        </Suspense>
      );
    });

    expect(await screen.findByRole("heading", { name: "Loaded" })).toBeTruthy();
  });

  it("reloads once when a deployed chunk no longer exists", async () => {
    const reload = vi.fn();
    setChunkReloadForTests(reload);
    const Page = lazyPage(
      async () => {
        throw new TypeError(
          "Failed to fetch dynamically imported module: /assets/OldPage.js"
        );
      },
      "ExamplePage"
    );

    render(
      <Suspense fallback={<p>Loading</p>}>
        <Page />
      </Suspense>
    );

    await vi.waitFor(() => expect(reload).toHaveBeenCalledOnce());
    expect(sessionStorage.getItem("englishai:chunk-reload")).toBe(
      window.location.href
    );
  });
});
