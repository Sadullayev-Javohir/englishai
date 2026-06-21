import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";

// Vitest runs without `globals: true`, so React Testing Library's automatic afterEach
// cleanup never registers. Any test that renders without its own cleanup then leaks its
// mounted tree into later tests. Combined with the localStorage-backed lesson-guidance
// preference (a module-level store shared across a file), that produces order-dependent
// failures under the parallel suite — e.g. a leftover "Bu sahifa nima uchun kerak?"
// heading surviving a "close the guidance" assertion, which is flaky by test scheduling
// rather than by any product bug. Guarantee a clean DOM and clean UI-preference storage
// after every test so each one starts from a known state.
afterEach(() => {
  cleanup();
  // Reset BOTH web-storage areas: tests persist UI preferences in localStorage and per-session
  // state (e.g. "englishai.speaking.session") in sessionStorage. Leaving either populated lets one
  // test's storage bleed into the next and fail only under the parallel suite ordering.
  try {
    localStorage.clear();
    sessionStorage.clear();
  } catch {
    // web storage is unavailable in some environments; the reset is best-effort.
  }
});
