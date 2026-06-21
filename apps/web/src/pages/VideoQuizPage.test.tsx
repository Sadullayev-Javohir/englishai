import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import type { GeneratedVideoQuizDto, VideoQuizResultDto } from "@/api/types";
import { VideoQuizPage } from "./VideoQuizPage";

vi.mock("@/api/client", () => ({ api: { video: { generateQuiz: vi.fn(), quizSession: vi.fn(), quiz: vi.fn(), rate: vi.fn() } } }));
const quiz: GeneratedVideoQuizDto = {
  quizId: "session-1", videoLessonId: "lesson-8", youTubeVideoId: "I_tRSrPru94", title: "Greetings", expiresAt: "2030-01-01", result: null,
  questions: [
    { id: "q1", prompt: "What is his name?", promptUz: "Uning ismi nima?", options: ["Tim", "Tom", "Sam", "Bob"], sourceStartSeconds: 9, sourceEndSeconds: 14, sourceText: "I'm Tim." },
    { id: "q2", prompt: "What does he say next?", promptUz: "U keyin nima deydi?", options: ["Nice to meet you", "Bye", "Thanks", "Good night"], sourceStartSeconds: 18, sourceEndSeconds: 22, sourceText: "It's nice to meet you." },
  ],
};
const result: VideoQuizResultDto = {
  lessonId: "lesson-8", totalQuestions: 2, correctCount: 2, passed: true, awardedXp: 40,
  outcomes: quiz.questions.map(q => ({ questionId: q.id, selectedOptionIndex: 0, correctOptionIndex: 0, isCorrect: true, hint: "Javob videodagi gapga mos." })),
};
function renderPage(path = "/video/lesson-8/quiz") {
  return render(<MemoryRouter initialEntries={[path]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}><Routes>
    <Route path="/video/:id/quiz" element={<VideoQuizPage />} />
    <Route path="/video/:id/play" element={<p>Video player</p>} />
    <Route path="/video" element={<p>Video catalog</p>} />
  </Routes></MemoryRouter>);
}
async function finish() {
  fireEvent.click(await screen.findByRole("radio", { name: "A Tim" }));
  fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
  fireEvent.click(await screen.findByRole("radio", { name: "A Nice to meet you" }));
  fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
  await screen.findByText("+40");
}
beforeEach(() => {
  vi.mocked(api.video.generateQuiz).mockResolvedValue(structuredClone(quiz));
  vi.mocked(api.video.quizSession).mockResolvedValue(structuredClone(quiz));
  vi.mocked(api.video.quiz).mockResolvedValue(structuredClone(result));
  vi.mocked(api.video.rate).mockResolvedValue(undefined);
});
describe("Pen video quiz screens 59 and 60", () => {
  it("creates a transcript quiz and shows exactly four choices with a separate submit button", async () => {
    renderPage();
    await screen.findByRole("heading", { name: "Uning ismi nima?" });
    expect(api.video.generateQuiz).toHaveBeenCalledTimes(1);
    expect(screen.getAllByRole("radio")).toHaveLength(4);
    expect(screen.getByRole("button", { name: "Javobni yuborish" }).hasAttribute("disabled")).toBe(true);
    fireEvent.click(screen.getByRole("radio", { name: "A Tim" }));
    expect(api.video.quiz).not.toHaveBeenCalled();
    expect(screen.getByRole("radio", { name: "A Tim" }).getAttribute("aria-checked")).toBe("true");
  });
  it("does not reveal or grade future answers; submits one complete server-side batch", async () => {
    renderPage();
    await finish();
    expect(api.video.quiz).toHaveBeenCalledTimes(1);
    expect(api.video.quiz).toHaveBeenCalledWith("lesson-8", expect.any(String), { q1: 0, q2: 0 }, "session-1");
    expect(screen.getByText("2 / 2")).toBeTruthy();
    expect(screen.getByText("+40")).toBeTruthy();
  });
  it("restores an existing quiz without generating again", async () => {
    renderPage("/video/lesson-8/quiz?quiz=session-1");
    await screen.findByRole("heading", { name: "Uning ismi nima?" });
    expect(api.video.generateQuiz).not.toHaveBeenCalled();
    expect(api.video.quizSession).toHaveBeenCalledWith("session-1", expect.any(String));
  });
  it("restores the authoritative completed result", async () => {
    vi.mocked(api.video.quizSession).mockResolvedValue({ ...quiz, result });
    renderPage("/video/lesson-8/quiz?quiz=session-1");
    await screen.findByText("+40");
    expect(screen.queryByRole("radiogroup")).toBeNull();
  });
  it("shows honest generation failure and retries", async () => {
    vi.mocked(api.video.generateQuiz).mockRejectedValueOnce(new Error("provider unavailable"));
    renderPage();
    await screen.findByRole("alert");
    expect(screen.queryByRole("radio")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
    await screen.findByRole("heading", { name: "Uning ismi nima?" });
  });
  it("retains the learner's answers after a submission error", async () => {
    vi.mocked(api.video.quiz).mockRejectedValueOnce(new Error("network"));
    renderPage();
    fireEvent.click(await screen.findByRole("radio", { name: "A Tim" }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
    fireEvent.click(await screen.findByRole("radio", { name: "A Nice to meet you" }));
    fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
    await screen.findByRole("alert");
    expect(screen.getByRole("radio", { name: "A Nice to meet you" }).getAttribute("aria-checked")).toBe("true");
    fireEvent.click(screen.getByRole("button", { name: "Javobni yuborish" }));
    await screen.findByText("+40");
  });
  it("replays the genuine excerpt with native YouTube controls", async () => {
    renderPage();
    fireEvent.click(await screen.findByRole("button", { name: "Parchani qayta ko‘rish" }));
    expect(screen.getByTitle("Savolga tegishli video parchasi").getAttribute("src")).toContain("start=9&end=14&autoplay=1&controls=1");
  });
  it("starts a fresh session on retry rather than restoring the previous result", async () => {
    renderPage();
    await finish();
    vi.mocked(api.video.generateQuiz).mockResolvedValue({ ...quiz, quizId: "session-2" });
    vi.mocked(api.video.quizSession).mockResolvedValue({ ...quiz, quizId: "session-2" });
    fireEvent.click(screen.getByRole("button", { name: "Yana bir marta takrorlash" }));
    await screen.findByRole("heading", { name: "Uning ismi nima?" });
    expect(api.video.generateQuiz).toHaveBeenCalledTimes(2);
  });
  it("confirms difficulty only after the rating is saved", async () => {
    renderPage();
    await finish();
    fireEvent.click(screen.getByRole("button", { name: "Mos keldi" }));
    await waitFor(() => expect(screen.getByRole("button", { name: "Mos keldi" }).getAttribute("aria-pressed")).toBe("true"));
    fireEvent.click(screen.getByRole("button", { name: "Video katalogiga qaytish" }));
    expect(screen.getByText("Video catalog")).toBeTruthy();
  });
});
