import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { api, ApiError } from "@/api/client";
import { PlacementTestPage } from "./PlacementTestPage";
import { PLACEMENT_PREVIEWS } from "./onboarding/placementPreviewItems";
import { placementStorage } from "./onboarding/placementStorage";

const secure = vi.hoisted(() => ({
  warningOpen: false, invalidated: false, reporting: false,
  leaveSecureAssessment: vi.fn(async () => {}),
  returnToTest: vi.fn(async () => {}),
  markInvalidated: vi.fn(),
  withoutFullscreenWatch: <T,>(action: () => Promise<T>) => action(),
}));
vi.mock("@/lib/secureAssessment", () => ({
  useSecureAssessment: () => secure,
  supportsFullscreenAssessment: () => true,
  enterSecureAssessment: async () => "fullscreen",
  isIntegrityApiError: () => false,
}));
vi.mock("@/api/client", async importOriginal => {
  const original = await importOriginal<typeof import("@/api/client")>();
  return { ...original, api: { placement: {
    start: vi.fn(), resume: vi.fn(), answer: vi.fn(), answerWriting: vi.fn(),
    answerSpeaking: vi.fn(), finalize: vi.fn(),
  }, learning: { start: vi.fn() } } };
});

const learnerId = "placement-learner";
const storage = placementStorage(learnerId);
const question = PLACEMENT_PREVIEWS.grammar;
const result = { overallLevel: 3, overallScore: 70, stageResults: [1, 2, 3, 4, 5, 6].map(stage => ({ stage, level: 3, score: 70 })) };
const failed = () => new ApiError(503, "unavailable");
function mount() {
  return render(<MemoryRouter initialEntries={["/placement"]}><Routes>
    <Route path="/placement" element={<PlacementTestPage />} />
    <Route path="/placement/result" element={<h1>RESULT DESTINATION</h1>} />
  </Routes></MemoryRouter>);
}
async function begin() {
  fireEvent.click(screen.getByRole("button", { name: /Testni boshlash|Testni davom ettirish/ }));
  await screen.findAllByText(/Grammar ·|Writing ·|RESULT DESTINATION|Testga ulanib|Natija saqlanmoqda/);
}
beforeEach(() => {
  vi.resetAllMocks();
  localStorage.setItem("englishai.learnerId", learnerId);
  vi.mocked(api.placement.start).mockResolvedValue({ sessionId: "session", currentStage: 2, firstItem: question });
  vi.mocked(api.placement.resume).mockResolvedValue({ sessionId: "session", isCompleted: false, currentItem: question });
  vi.mocked(api.placement.finalize).mockResolvedValue(result);
});

describe("real placement session lifecycle", () => {
  it("never replaces a stored session after a temporary resume failure", async () => {
    storage.saveSession("session");
    vi.mocked(api.placement.resume).mockRejectedValueOnce(failed());
    mount(); await begin();
    expect(api.placement.start).not.toHaveBeenCalled();
    expect(storage.readSession()).toBe("session");
    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
    await screen.findByText("Grammar · 2 / 6");
    expect(api.placement.resume).toHaveBeenCalledTimes(2);
    expect(api.placement.start).not.toHaveBeenCalled();
  });
  it("finalizes a completed restored session instead of starting over", async () => {
    storage.saveSession("session");
    vi.mocked(api.placement.resume).mockResolvedValue({ sessionId: "session", isCompleted: true, currentItem: null });
    mount(); await begin();
    await screen.findByText("RESULT DESTINATION");
    expect(api.placement.finalize).toHaveBeenCalledWith("session");
    expect(api.placement.start).not.toHaveBeenCalled();
    expect(storage.readResult()).toEqual(result);
    expect(storage.readSession()).toBeNull();
  });
  it("retries a failed finalize without repeating the last answer or all six stages", async () => {
    storage.saveSession("session");
    vi.mocked(api.placement.resume).mockResolvedValue({ sessionId: "session", isCompleted: true, currentItem: null });
    vi.mocked(api.placement.finalize).mockRejectedValueOnce(failed()).mockResolvedValue(result);
    mount(); await begin();
    expect(storage.readSession()).toBe("session");
    fireEvent.click(screen.getByRole("button", { name: "Natijani qayta olish" }));
    await screen.findByText("RESULT DESTINATION");
    expect(api.placement.finalize).toHaveBeenCalledTimes(2);
    expect(api.placement.start).not.toHaveBeenCalled();
  });
  it("recovers the next question when an answer was saved but its response was lost", async () => {
    vi.mocked(api.placement.answer).mockRejectedValueOnce(failed());
    vi.mocked(api.placement.resume).mockResolvedValue({ sessionId: "session", isCompleted: false, currentItem: { ...question, id: "next", prompt: "The next question." } });
    mount(); await begin();
    fireEvent.click(screen.getByRole("button", { name: "A She don’t like coffee." }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
    await screen.findByRole("heading", { name: "The next question." });
    expect(api.placement.answer).toHaveBeenCalledTimes(1);
    expect(api.placement.start).toHaveBeenCalledTimes(1);
    expect((screen.getByRole("button", { name: "Javobni yuborish" }) as HTMLButtonElement).disabled).toBe(true);
  });
  it("retains a writing draft across a failed submit, recovery and a reload", async () => {
    const writing = { ...PLACEMENT_PREVIEWS.writing, minWords: 3, maxWords: 50 };
    vi.mocked(api.placement.start).mockResolvedValue({ sessionId: "session", currentStage: 5, firstItem: writing });
    vi.mocked(api.placement.resume).mockResolvedValue({ sessionId: "session", isCompleted: false, currentItem: writing });
    vi.mocked(api.placement.answerWriting).mockRejectedValue(failed());
    const view = mount(); await begin();
    fireEvent.change(screen.getByRole("textbox"), { target: { value: "This is my saved answer." } });
    fireEvent.click(screen.getByRole("button", { name: "Yuborish va davom etish" }));
    await screen.findByRole("alert");
    expect((screen.getByRole("textbox") as HTMLTextAreaElement).value).toBe("This is my saved answer.");
    view.unmount();
    mount(); await begin();
    expect((screen.getByRole("textbox") as HTMLTextAreaElement).value).toBe("This is my saved answer.");
    expect(api.placement.start).toHaveBeenCalledTimes(1);
  });
  it("does not interpret a missing next item as a completed test", async () => {
    vi.mocked(api.placement.answer).mockResolvedValue({ wasCorrect: true, isTestCompleted: false, nextItem: null, currentStage: 2, currentDifficulty: 3 });
    mount(); await begin();
    fireEvent.click(screen.getByRole("button", { name: "A She don’t like coffee." }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
    await screen.findByRole("alert");
    expect(api.placement.finalize).not.toHaveBeenCalled();
    expect(api.placement.resume).toHaveBeenCalledWith("session");
  });
  it("ignores rapid double clicks on start", async () => {
    mount();
    const button = screen.getByRole("button", { name: "Testni boshlash" });
    await act(async () => { fireEvent.click(button); fireEvent.click(button); });
    await waitFor(() => expect(api.placement.start).toHaveBeenCalledOnce());
  });
});
