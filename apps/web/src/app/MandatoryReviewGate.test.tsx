import { render, screen } from "@testing-library/react";
import {
  MemoryRouter,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { RequireMandatoryVocabularyReview } from "./MandatoryReviewGate";

function LocationProbe() {
  const location = useLocation();
  return <div>{`${location.pathname}${location.search}${location.hash}`}</div>;
}

describe("RequireMandatoryVocabularyReview", () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it("does not block a learner route when vocabulary reviews are due", () => {
    render(
      <MemoryRouter
        initialEntries={["/video/lesson-1/quiz?mode=review#question-2"]}
      >
        <Routes>
          <Route element={<RequireMandatoryVocabularyReview />}>
            <Route path="/video/:id/quiz" element={<LocationProbe />} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText("/video/lesson-1/quiz?mode=review#question-2")).toBeTruthy();
  });

  it("renders learner content without waiting for review status", () => {
    render(
      <MemoryRouter initialEntries={["/home"]}>
        <Routes>
          <Route element={<RequireMandatoryVocabularyReview />}>
            <Route path="/home" element={<LocationProbe />} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByText("/home")).toBeTruthy();
  });
});
