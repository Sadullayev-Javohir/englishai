import { expect, type Page } from "@playwright/test";

const mockApi = process.env.VOCABULARY_MOCK_API ?? "http://127.0.0.1:5055";
export const topicId = "11111111-1111-1111-1111-111111111111";
export const pairs = [
  ["mother", "ona"], ["father", "ota"], ["family", "oila"], ["support", "yordam"], ["care", "g‘amxo‘rlik"],
  ["patient", "sabrli"], ["proud", "faxr"], ["kind", "mehribon"], ["advice", "maslahat"], ["trust", "ishonch"],
  ["smile", "tabassum"], ["hug", "quchoq"], ["teach", "o‘rgatish"], ["listen", "tinglash"], ["encourage", "ruhlantirish"],
  ["calm", "xotirjam"], ["brave", "jasur"], ["warm", "iliq"], ["together", "birga"], ["grateful", "minnatdor"],
];
export const topic = {
  id: topicId, title: "My Mother", titleUz: "Oila", category: "People", level: 3, isReady: true,
  passage: "Whenever I face a challenge, my mother gives me support. She is kind and patient. My father and our family care about my dreams.\n\nTheir advice helps me feel brave. I trust them because they listen and encourage me to keep trying. They teach me to stay calm.\n\nA warm smile and a hug make every day better. We learn together. My parents are proud of me, and I am grateful.",
  words: pairs.map(([word, translation]) => ({ word, translation,
    ipa: word === "mother" ? "/ˈmʌðə/" : word === "patient" ? "/ˈpeɪʃənt/" : word === "support" ? "/səˈpɔːt/" : null,
    exampleSentence: word === "mother" ? "My mother always supports me." : word === "patient" ? "My mother is very patient." : `We learn the word ${word} together.`,
    partOfSpeech: "noun", lexicalCategory: "Noun", register: null, usageNote: null, imageUrl: null, imageAttribution: null,
  })), quiz: [],
};
export const catalog = [
  { id: topicId, title: "My Mother", titleUz: "Oila", wordCount: 20, isStarted: true },
  { id: "daily", title: "Daily Routines", titleUz: "Kundalik hayot", wordCount: 14 },
  { id: "market", title: "At the Market", titleUz: "Xaridlar", wordCount: 10 },
  { id: "travel", title: "Travel Plans", titleUz: "Sayohat", wordCount: 15 },
].map(item => ({ ...item, category: "People", level: 3, isFilled: true, learned: false, passedModuleCount: 0, requiredModuleCount: 6, isMastered: false, isLocked: false, requiresPro: false, modules: [{ module: "Vocabulary", passed: false, unlocked: true, score: null }] }));
export function completionResponse(score = 100) {
  return { completion: { topicId, passedModuleCount: score >= 75 ? 1 : 0, requiredModuleCount: 6, isMastered: false,
    modules: ["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"].map((module, index) => ({ module, passed: index === 0 && score >= 75, unlocked: index === 0 || index === 1 && score >= 75, score: index === 0 ? score : null })) },
    justMastered: false, reward: { awardedXp: score >= 75 ? 100 : 0, awardedCoins: 0, dailyActivityBonus: 0, allModulesBonus: 0, streakMilestoneBonus: 0, premiumMultiplierApplied: false, alreadyCreditedToday: false } };
}
export async function vocabularyFixture(page: Page) {
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "3");
    localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
    sessionStorage.setItem("englishai.due-review-modal.mock-learner-0001", "1");
  });
  // Never send fixture traffic, scoring or microphone recordings to a real account/backend.
  await page.route(url => url.pathname.startsWith("/api/"), async route => {
    const url = new URL(route.request().url());
    const response = await route.fetch({ url: `${mockApi}${url.pathname}${url.search}` });
    if (route.request().failure()) return;
    try { await route.fulfill({ response }); } catch (error) { if (!route.request().failure()) throw error; }
  });
  await page.route("**/api/auth/me", route => route.fulfill({ json: {
    id: "mock-learner-0001", email: "demo@englishai.uz", displayName: "Demo", username: "demo", pictureUrl: null,
    hasOnboarded: true, preferredName: "Demo", learningGoal: 1, birthDate: "2000-01-01", gender: 1,
    acquisitionSource: 1, acquisitionSourceOther: null, hasCompletedDemographics: true,
  } }));
  await page.route("**/api/subscription/mock-learner-0001", async route => {
    const response = await route.fetch({ url: `${mockApi}/api/subscription/mock-learner-0001` });
    await route.fulfill({ json: { ...await response.json(), isTrialActive: false } });
  });
  await page.route("**/api/gamification/*/points", route => route.fulfill({ json: { lifetimeXp: 360, spendableCoins: 0, tiers: [], activeRedemptions: [] } }));
  await page.route("**/api/gamification/*/energy", route => route.fulfill({ json: { current: 5, maximum: 5, nextRefillAt: new Date(Date.now() + 3600000).toISOString(), consumed: false } }));
  await page.route("**/api/vocabulary/topics/**", route => route.fulfill({ json: catalog }));
  await page.route(`**/api/vocabulary/topic/${topicId}`, route => route.fulfill({ json: topic }));
  await page.route(`**/api/vocabulary/topic/${topicId}/passage-translation`, route => route.fulfill({ json: { sentences: [{ english: topic.passage, uzbek: "Qiyinchilikka duch kelganimda, onam menga yordam beradi." }] } }));
  await page.route("**/api/vocabulary/topic/completion", route => route.fulfill({ json: completionResponse(route.request().postDataJSON().score) }));
  await page.route("**/api/speaking/word/*", route => route.fulfill({ json: { word: "support", spokenForm: null, ipa: "səˈpɔːt", phonemes: [], visemes: [], audioBase64: null, tipUz: "Urg‘u ikkinchi bo‘g‘inda: sup-PORT." } }));
  await page.route("**/api/vocabulary/pronounce", route => route.fulfill({ json: { word: "support", recognized: true, isAuthentic: true, correct: false, overallScore: 82, accuracyScore: 82, errorType: 0,
    phonemes: [{ phoneme: "sə", accuracyScore: 96 }, { phoneme: "pɔːt", accuracyScore: 70 }], feedbackUz: "Yaxshi! Urg‘uni biroz o‘zgartiring." } }));
}
export async function openCards(page: Page) {
  await page.goto(`/app/vocabulary/topic/${topicId}`);
  await page.getByRole("button", { name: "Darsni boshlash", exact: true }).click();
  await page.getByRole("button", { name: "So‘zlarga o‘tamiz", exact: true }).click();
  await expect(page.locator('[data-pen-screen="16"]')).toBeVisible();
}
export async function learnAllCards(page: Page) {
  for (let index = 0; index < pairs.length; index++) {
    await page.getByRole("button", { name: "Aylantirish", exact: true }).click();
    await page.getByRole("button", { name: "O‘rgandim →", exact: true }).click();
  }
  await expect(page.getByRole("heading", { name: "Testga tayyorsiz!" })).toBeVisible();
}
export async function answerExercise(page: Page, index: number) {
  const [word, translation] = pairs[index];
  if (index % 2 === 0) await page.locator(".vocabulary-topic__test-options").getByRole("button", { name: new RegExp(translation) }).click();
  else { await page.getByRole("textbox", { name: "Javobingiz" }).fill(word); await page.getByRole("button", { name: "Tekshirish", exact: true }).click(); }
  await page.getByRole("button", { name: "Keyingi", exact: true }).click();
}
