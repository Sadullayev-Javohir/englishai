import { afterEach, describe, expect, it, vi } from "vitest";
import { isPlacementResult, placementStorage } from "./placementStorage";

const result = { overallLevel: 3, overallScore: 70, stageResults: [1, 2, 3, 4, 5, 6].map(stage => ({ stage, level: 3, score: 70 })) };
afterEach(() => vi.restoreAllMocks());
describe("placement persistence", () => {
  it("isolates sessions, results and drafts by learner", () => {
    const a = placementStorage("a"), b = placementStorage("b");
    a.saveSession("test-a"); a.saveResult(result); a.saveDraft("test-a", "writing", "My answer");
    expect(b.readSession()).toBeNull();
    expect(b.readResult()).toBeNull();
    expect(b.readDraft("test-a", "writing")).toBe("");
    expect(a.readDraft("test-a", "writing")).toBe("My answer");
  });
  it("survives blocked storage without breaking a successful finalization", () => {
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new Error("blocked"); });
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => { throw new Error("blocked"); });
    const store = placementStorage("blocked");
    expect(() => { store.saveSession("test"); store.saveResult(result); }).not.toThrow();
    expect(store.readSession()).toBe("test");
    expect(store.readResult()).toEqual(result);
  });
  it("rejects malformed and duplicate skill results", () => {
    for (const invalid of [null, {}, { ...result, overallLevel: 99 }, { ...result, overallScore: NaN },
      { ...result, stageResults: [result.stageResults[0], result.stageResults[0]] }]) {
      expect(isPlacementResult(invalid)).toBe(false);
    }
    expect(isPlacementResult(result)).toBe(true);
  });
  it("migrates a legacy session only after its server-verified adoption", () => {
    const legacy = "9e9bd422-9c6b-4cdd-8b74-7c8809f6f3ea";
    localStorage.setItem("englishai.placement.session", legacy);
    const store = placementStorage("legacy-owner");
    expect(store.readSession()).toBe(legacy);
    store.saveSession(legacy);
    expect(localStorage.getItem("englishai.placement.session")).toBeNull();
    expect(store.readSession()).toBe(legacy);
  });
});
