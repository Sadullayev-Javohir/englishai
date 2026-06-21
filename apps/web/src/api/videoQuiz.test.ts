import { afterEach, describe, expect, it, vi } from "vitest";
import { api } from "./client";

afterEach(() => vi.unstubAllGlobals());
describe("video quiz HTTP contract", () => {
  it("sends the server's answer array and opaque quiz id, never a client answer key", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response("{}", { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    await api.video.quiz("video-1", "learner-1", { "q-1": 2, "q-2": 0 }, "quiz-1");
    const [url, request] = fetchMock.mock.calls[0];
    expect(String(url)).toContain("/api/video/quiz");
    expect(JSON.parse(request.body)).toEqual({
      videoLessonId: "video-1", learnerId: "learner-1", quizId: "quiz-1",
      answers: [{ questionId: "q-1", selectedOptionIndex: 2 }, { questionId: "q-2", selectedOptionIndex: 0 }],
    });
  });
  it("generates from a video id and learner id, not browser-supplied transcript content", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response("{}", { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    await api.video.generateQuiz("video-1", "learner-1");
    expect(String(fetchMock.mock.calls[0][0])).toContain("/api/video/video-1/quiz");
    expect(JSON.parse(fetchMock.mock.calls[0][1].body)).toEqual({ learnerId: "learner-1" });
  });
});
