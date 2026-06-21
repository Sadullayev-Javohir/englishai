import { act, fireEvent, render } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useNavigate } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { DocumentTitleProvider, formatDocumentTitle, useDocumentTitle } from "./documentTitle";

function DynamicPage({ title }: { title?: string }) {
  useDocumentTitle(title, "Vocabulary");
  return null;
}

function NavigationFixture() {
  const navigate = useNavigate();
  return <button type="button" onClick={() => navigate("/profile")}>Profil</button>;
}

describe("document title", () => {
  it("formats page and brand names without empty fragments", () => {
    expect(formatDocumentTitle("Speaking")).toBe("Speaking | EnglishAI.uz");
    expect(formatDocumentTitle("Family", "Vocabulary")).toBe("Family — Vocabulary | EnglishAI.uz");
    expect(formatDocumentTitle("EnglishAI.uz")).toBe("EnglishAI.uz");
    expect(formatDocumentTitle()).toBe("EnglishAI.uz");
  });

  it("uses the route fallback then applies a dynamic title", () => {
    const view = render(
      <MemoryRouter initialEntries={["/app/vocabulary/topic/family"]}>
        <DocumentTitleProvider><DynamicPage /></DocumentTitleProvider>
      </MemoryRouter>,
    );
    expect(document.title).toBe("Vocabulary darsi | EnglishAI.uz");

    view.rerender(
      <MemoryRouter initialEntries={["/app/vocabulary/topic/family"]}>
        <DocumentTitleProvider><DynamicPage title="Family" /></DocumentTitleProvider>
      </MemoryRouter>,
    );
    expect(document.title).toBe("Family — Vocabulary | EnglishAI.uz");
  });

  it("clears a dynamic title when the route changes", () => {
    const view = render(
      <MemoryRouter initialEntries={["/app/vocabulary/topic/family"]}>
        <DocumentTitleProvider>
          <Routes>
            <Route path="/app/vocabulary/topic/:topicId" element={<><DynamicPage title="Family" /><NavigationFixture /></>} />
            <Route path="/profile" element={null} />
          </Routes>
        </DocumentTitleProvider>
      </MemoryRouter>,
    );
    expect(document.title).toBe("Family — Vocabulary | EnglishAI.uz");

    act(() => fireEvent.click(view.getByRole("button", { name: "Profil" })));
    expect(document.title).toBe("Profil | EnglishAI.uz");
  });
});
