import { readFileSync } from "node:fs";
import type { Page } from "@playwright/test";

export const listeningTopicId = "11111111-1111-1111-1111-111111111111";
const sentence = "My mother has worked at the local library for ten years. She helps children choose books. Every morning she walks to work.";
export const listeningTopic = {
  topicId: listeningTopicId, title: "A morning at the library", topic: "My Mother", level: 3,
  isReady: true, wordCount: 30, transcript: sentence,
  targetWords: [{ word: "the local library", translation: "mahalliy kutubxona" }],
  questions: [
    { id: "q1", prompt: "Where does her mother work?", options: ["At a school.", "At the local library.", "At a hospital.", "At a shop."] },
    { id: "q2", prompt: "How does she get to work?", options: ["She walks.", "By train.", "By bus.", "By car."] },
  ],
};

function audioFixture() {
  const bytes = Buffer.alloc(44 + 16000 * 2 * 4);
  bytes.write("RIFF"); bytes.writeUInt32LE(bytes.length - 8, 4); bytes.write("WAVEfmt ", 8);
  bytes.writeUInt32LE(16, 16); bytes.writeUInt16LE(1, 20); bytes.writeUInt16LE(1, 22);
  bytes.writeUInt32LE(16000, 24); bytes.writeUInt32LE(32000, 28); bytes.writeUInt16LE(2, 32); bytes.writeUInt16LE(16, 34);
  bytes.write("data", 36); bytes.writeUInt32LE(bytes.length - 44, 40);
  for (let i = 0; i < (bytes.length - 44) / 2; i++) bytes.writeInt16LE(Math.round(Math.sin(i * 2 * Math.PI * 220 / 16000) * 500), 44 + i * 2);
  return bytes;
}

/** All API calls stay on a dedicated fixture service, never the operator's account. */
export async function listeningFixture(page: Page, options: { longText?: boolean; longQuestion?: boolean; unavailableAudio?: boolean } = {}) {
  const mockApi = process.env.LISTENING_MOCK_API ?? "http://127.0.0.1:5055";
  const topic = {
    ...listeningTopic,
    transcript: options.longText ? Array.from({ length: 80 }, () => sentence).join("\n\n") : sentence,
    questions: options.longQuestion ? listeningTopic.questions.map(question => ({ ...question, prompt: question.prompt.repeat(12), options: question.options.map(option => option.repeat(30)) })) : listeningTopic.questions,
  };
  const catalog = ["A Morning at the Library", "Weekend Plans", "At the Station", "A Voice Message"].map((title, index) => ({ topicId: index ? `topic-${index}` : listeningTopicId, title, titleUz: "Tinglash mashqi", category: "Daily life", level: 3 }));
  const answers: Array<{ questionId: string; selectedOptionIndex: number; correctOptionIndex: number; isCorrect: boolean; hint: string | null }> = [];
  const submissions: unknown[] = [];
  await page.route("**/api/**", async route => {
    const url = new URL(route.request().url());
    const response = await route.fetch({ url: `${mockApi}${url.pathname}${url.search}` });
    if (route.request().failure()) return;
    try { await route.fulfill({ response }); } catch (error) { if (!route.request().failure()) throw error; }
  });
  await page.route("**/api/auth/me", route => route.fulfill({ json: { id: "mock-learner-0001", email: "demo@englishai.uz", displayName: "Demo", username: "demo", pictureUrl: null, hasOnboarded: true, learningGoal: 1, hasCompletedDemographics: true } }));
  await page.route("**/api/listening/catalog/**", route => route.fulfill({ json: catalog }));
  await page.route("**/api/levels/map/**", route => route.fulfill({ json: { level: "B1", topics: catalog.map(item => ({ id: item.topicId, title: item.title, level: 3, isLocked: false, requiresPro: false, passedModuleCount: 2, requiredModuleCount: 6, modules: [{ module: "Listening", passed: false, unlocked: true }] })) } }));
  await page.route(`**/api/listening/topic/${listeningTopicId}`, route => route.fulfill({ json: topic }));
  await page.route("**/api/listening/topic/*/audio", route => route.fulfill(options.unavailableAudio ? { status: 503, body: "unavailable" } : { contentType: "audio/wav", body: audioFixture() }));
  await page.route("**/api/images/topics/**", route => route.fulfill({ contentType: "image/jpeg", body: readFileSync("public/assets/vocabulary/my-mother.jpg") }));
  await page.route("**/api/listening/answers/check", route => {
    const answer = route.request().postDataJSON();
    const correctOptionIndex = answer.questionId === "q1" ? 1 : 0;
    const checked = { ...answer, correctOptionIndex, isCorrect: answer.selectedOptionIndex === correctOptionIndex, hint: answer.selectedOptionIndex === correctOptionIndex ? null : "She works at the local library." };
    answers.push(checked);
    return route.fulfill({ json: checked });
  });
  await page.route("**/api/listening/quiz", route => {
    submissions.push(route.request().postDataJSON());
    const correctCount = answers.filter(answer => answer.isCorrect).length;
    const passed = correctCount === 2;
    return route.fulfill({ json: { topicId: listeningTopicId, totalQuestions: 2, correctCount, scorePercent: correctCount * 50, passed, outcomes: answers,
      completion: passed ? { topicId: listeningTopicId, level: "B1", isMastered: false, passedModuleCount: 4, requiredModuleCount: 6, modules: [
        { module: "Listening", passed: true, unlocked: true, score: 100, achievedAt: "2026-09-12T00:00:00Z" },
        { module: "Writing", passed: false, unlocked: true, score: null, achievedAt: null },
      ] } : null } });
  });
  return { submissions };
}
