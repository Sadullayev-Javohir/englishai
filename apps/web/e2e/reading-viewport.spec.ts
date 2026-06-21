import { expect, test, type Page, type TestInfo } from "@playwright/test";
import { readFile } from "node:fs/promises";

const topicId = process.env.READING_TOPIC_ID ?? "11111111-1111-1111-1111-111111111111";
const lessonUrl = `/reading/topic/${topicId}`;
const words = [
  ["mother", "ona"], ["support", "qo‘llab-quvvatlash"], ["kind", "mehribon"], ["patient", "sabrli"],
  ["father", "ota"], ["family", "oila"], ["care", "g‘amxo‘rlik qilmoq"], ["encourage", "ruhlantirmoq"],
  ["give up", "taslim bo‘lmoq"], ["advice", "maslahat"], ["trust", "ishonmoq"], ["listen", "tinglamoq"],
  ["teach", "o‘rgatmoq"], ["smile", "tabassum"], ["help", "yordam bermoq"], ["calm", "xotirjam"],
  ["brave", "jasur"], ["together", "birga"], ["proud", "faxrlanmoq"], ["grateful", "minnatdor"],
].map(([word, translation]) => ({ word, translation, exampleSentence: word === "patient" ? "She is kind and patient." : null }));
const body = "Whenever I face a challenge, my mother gives me support. She is kind and patient. My father and the rest of our family care about my interests. They encourage me to keep trying.\n\nLast month, I wanted to give up learning the guitar. Her advice was simple: practise for just ten minutes a day. I trust her, and I always listen when she offers to teach me something new.\n\nA warm smile and a gentle hug help me stay calm and brave. We practise together and focus on small steps.\n\nNow I can play my favourite song. My parents are proud of me, and I am grateful.";
const prompt = "What has her mother’s patience taught her?";
const options = ["To stay calm.", "To stop practising.", "To hurry every day.", "To avoid new hobbies."];

async function fixture(page: Page, { longText = false, longQuestion = false, passed = true } = {}) {
  // Even when verifying the running localhost frontend, every API request
  // goes to this suite's mock. Never modify a real learner during visual QA.
  const mockUrl = (url: string) => { const parsed = new URL(url); return `http://127.0.0.1:5075${parsed.pathname}${parsed.search}`; };
  await page.route(url => url.pathname.startsWith("/api/"), async route => route.fulfill({ response:await route.fetch({ url:mockUrl(route.request().url()) }) }));
  await page.route("**/api/auth/me", async route => route.fulfill({ response:await route.fetch({ url:mockUrl(route.request().url()), headers:{ ...route.request().headers(), "x-englishai-audit-auth":"learner" } }) }));
  await page.route("**/api/subscription/mock-learner-0001", async route => { const response = await route.fetch({ url:mockUrl(route.request().url()) }); await route.fulfill({ response, json:{ ...await response.json(), isTrialActive:false } }); });
  const topics = ["My Mother", "A Weekend Away", "The Small Garden", "A New Skill"].map((title, index) => ({ topicId: index ? `${index + 1}`.repeat(8) + "-" + `${index + 1}`.repeat(4) + "-" + `${index + 1}`.repeat(4) + "-" + `${index + 1}`.repeat(4) + "-" + `${index + 1}`.repeat(12) : topicId, title, titleUz:["Oila", "Sayohat", "Tabiat", "Hunar"][index], category:["Family", "Travel", "Nature", "Hobbies"][index], level:3 }));
  await page.route("**/api/reading/catalog/**", route => route.fulfill({ json:topics }));
  await page.route("**/api/vocabulary/topics/**", route => route.fulfill({ json:topics.map(topic => ({ id:topic.topicId, ...topic, isFilled:true, isLocked:false, requiresPro:false, passedModuleCount:2, requiredModuleCount:6, isMastered:false, modules:["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"].map((module,i) => ({ module, passed:i < 2, unlocked:i <= 2, score:i < 2 ? 100 : 0 })) })) }));
  await page.route("**/api/levels/map/**", async route => {
    const response = await route.fetch({ url:mockUrl(route.request().url()) }); const map = await response.json();
    await route.fulfill({ response, json:{ ...map, topics:topics.map(topic => ({ id:topic.topicId, ...topic, isFilled:true, isLocked:false, requiresPro:false, passedModuleCount:2, requiredModuleCount:6, isMastered:false, modules:["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"].map((module,i) => ({ module, passed:i < 2, unlocked:i <= 2, score:i < 2 ? 100 : 0 })) })) } });
  });
  await page.route(`**/api/reading/topic/${topicId}`, route => route.fulfill({ json:{ topicId, passageId:"reading-pen", title:"Small steps, big support", topic:"My Mother", level:3, isReady:true, wordCount:120, body:longText ? Array(20).fill(body).join("\n\n") : body, glossary:words, targetWords:words, questions:[1,2].map(id => ({ id:`question-${id}`, prompt:longQuestion ? Array(12).fill(prompt).join(" ") : prompt, options:longQuestion ? options.map(option => Array(12).fill(option).join(" ")) : options })) } }));
  await page.route("**/api/reading/quiz/check", route => { const answer = route.request().postDataJSON(); return route.fulfill({ json:{ ...answer, correctOptionIndex:0, isCorrect:answer.selectedOptionIndex === 0, explanation:"Her patience has taught me to stay calm and focus on small steps." } }); });
  const completion = { topicId, level:"B1", isMastered:false, passedModuleCount:3, requiredModuleCount:6, modules:["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"].map((module,index) => ({ module, passed:index < 3, score:index < 3 ? 100 : 0, unlocked:index <= 3, achievedAt:index < 3 ? "2026-09-12T06:00:00Z" : null })) };
  await page.route("**/api/reading/quiz", route => route.fulfill({ json:{ topicId, totalQuestions:2, correctCount:passed ? 2 : 1, scorePercent:passed ? 100 : 50, passed, outcomes:[], completion } }));
  await page.route(`**/api/vocabulary/topic/${topicId}/completion/*`, route => route.fulfill({ json:completion }));
  await page.route("**/api/speaking/word/**", route => route.fulfill({ json:{ word:decodeURIComponent(route.request().url().split("/").pop()!), ipa:"/ˈpeɪʃənt/", audioBase64:null } }));
  await page.route("**/api/vocabulary/learn", route => route.fulfill({ json:{ id:"saved-reading-word", ...route.request().postDataJSON() } }));
  const photos = ["my-mother", "a-weekend-away", "the-small-garden", "a-new-skill"];
  await page.route("**/api/images/topics/**", async route => {
    const found = topics.findIndex(topic => route.request().url().includes(topic.topicId));
    const path = `../../data/design-assets/topic-photos/${photos[Math.max(0,found)]}.jpg`;
    try { await route.fulfill({ contentType:"image/jpeg", body:await readFile(path) }); }
    catch { await route.continue(); }
  });
}
async function fitsDocument(page: Page) {
  await expect.poll(() => page.evaluate(() => ({ x:document.documentElement.scrollWidth - innerWidth, y:document.documentElement.scrollHeight - innerHeight }))).toEqual({ x:0, y:0 });
}
async function hasNoHorizontalOverflow(page: Page) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBe(0);
}
async function fitsLesson(page: Page) {
  await fitsDocument(page);
  await expect.poll(() => page.locator("[data-lesson-stage-body]").evaluate(el => el.scrollHeight - el.clientHeight)).toBeLessThanOrEqual(1);
}
async function shot(page: Page, info: TestInfo, name: string) {
  if (name === "31-catalog") {
    // Full-page screenshots alone do not load below-the-fold lazy images.
    // Exercise normal scrolling rather than changing production loading behavior.
    for (const image of await page.locator(".reading-library__photo img").all()) {
      await image.scrollIntoViewIfNeeded();
      await expect.poll(() => image.evaluate(el => (el as HTMLImageElement).complete && (el as HTMLImageElement).naturalWidth > 0)).toBe(true);
    }
    await page.evaluate(() => window.scrollTo({ top:0, behavior:"instant" }));
    await expect.poll(() => page.evaluate(() => window.scrollY)).toBe(0);
  }
  await page.screenshot({ path:info.outputPath(`${name}.png`), fullPage:true });
}
async function openPractice(page: Page) {
  await page.goto(lessonUrl);
  await expect(page.locator('.reading-text')).toBeVisible();
  await page.getByRole("button", { name:"Savollarga o‘tamiz" }).click();
  await expect(page.getByRole("heading", { name:"Asosiy fikrni toping." })).toBeVisible();
}
async function answer(page: Page, correct = true) {
  await page.getByRole("button", { name:correct ? /To stay calm/ : /To stop practising/ }).click();
  await page.getByRole("button", { name:"Tekshirish", exact:true }).click();
  await expect(page.locator('.reading-feedback-page')).toBeVisible();
}
for (const [width,height] of [[1440,900],[1366,768],[1280,720],[1180,820],[1024,768],[820,1180],[768,1024],[700,900],[390,844],[360,800],[360,640],[320,568]]) {
  test(`Pen Reading 31–35 ${width}x${height}`, async ({ page },info) => {
    const errors:string[] = []; page.on("pageerror", error => errors.push(error.message));
    await page.setViewportSize({ width,height }); await fixture(page); await page.goto("/reading");
    await expect(page.getByRole("heading", { name:"Hikoya ichiga kiring." })).toBeVisible();
    await page.evaluate(() => document.fonts.ready);
    await expect(page.locator('.reading-library__card')).toHaveCount(4);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth - innerWidth)).toBe(0);
    const columns = await page.locator('.reading-library__grid').evaluate(el => getComputedStyle(el).gridTemplateColumns.split(' ').length);
    expect(columns).toBe(width > 1100 ? 4 : width > 700 ? 2 : 1);
    await shot(page,info,"31-catalog");
    await page.locator('.reading-library__open').first().click();
    await expect(page.locator('.reading-text')).toBeVisible();
    await expect(page.locator('.reading-progress')).toHaveAttribute('data-lesson-step','text');
    await expect(page.locator('.reading-progress [role="progressbar"]')).toHaveAttribute('aria-valuetext','2 / 4 · Matn');
    await expect(page.locator('.reading-progress__segment')).toHaveCount(4);
    await expect(page.getByRole("button", { name:"Matnni saqlash" })).toHaveCount(0);
    await expect(page.getByRole("button", { name:"So‘zni saqlash" })).toHaveCount(0);
    await hasNoHorizontalOverflow(page);
    await expect.poll(() => page.locator('[data-lesson-stage-body]').evaluate(el => ({ overflow:getComputedStyle(el).overflowY, hidden:el.scrollHeight-el.clientHeight }))).toEqual({overflow:"visible",hidden:0});
    await shot(page,info,"32-passage");
    await page.getByRole("button",{ name:/^patient[.]?$/ }).click();
    await expect(page.locator('.reading-word h2')).toHaveText('patient');
    await expect(page.locator(".reading-word h2")).toBeInViewport();
    await shot(page,info,"32-dictionary-selected");
    await page.getByRole("button", { name:"Savollarga o‘tamiz" }).click();
    await expect(page.getByRole("heading", { name:"Asosiy fikrni toping." })).toBeVisible();
    await expect(page.locator('.reading-progress')).toHaveAttribute('data-lesson-step','practice');
    await expect(page.locator('.reading-progress [role="progressbar"]')).toHaveAttribute('aria-valuetext','3 / 4 · Test');
    if (height >= 720) await fitsLesson(page); else await fitsDocument(page);
    await page.getByRole("button", { name:/To stay calm/ }).click();
    await expect(page.getByRole("button",{ name:"Tekshirish",exact:true })).toBeEnabled();
    await shot(page,info,"33-question-selected");
    await page.getByRole("button",{ name:"Tekshirish",exact:true }).click();
    await expect(page.getByRole("heading",{ name:"Javob matnning o‘zida." })).toBeVisible();
    await expect(page.locator('.reading-progress')).toHaveAttribute('data-lesson-step','practice');
    if (height >= 720) await fitsLesson(page); else await fitsDocument(page);
    await shot(page,info,"34-evidence");
    await page.getByRole("button",{ name:"Keyingi savol" }).click(); await answer(page);
    await page.getByRole("button",{ name:"Natijani ko‘rish" }).click();
    await expect(page.getByRole("heading",{ name:"Hikoyani tushundingiz!" })).toBeVisible();
    await expect(page.locator('.reading-progress')).toHaveAttribute('data-lesson-step','result');
    await expect(page.locator('.reading-progress')).toHaveAttribute('data-complete','true');
    await expect(page.locator('.reading-progress [role="progressbar"]')).toHaveAttribute('aria-valuetext','4 / 4 · Yakun');
    if (height >= 720) await fitsLesson(page); else { await fitsDocument(page); await page.getByRole('button',{ name:"Writing’ga o‘tish" }).scrollIntoViewIfNeeded(); }
    await shot(page,info,"35-result"); expect(errors).toEqual([]);
  });
}

test("Search and learner-scoped bookmarks work on the Pen catalog", async ({ page }) => {
  await fixture(page); await page.goto('/reading');
  await page.getByRole('searchbox').fill('garden'); await expect(page.locator('.reading-library__card')).toHaveCount(1);
  await page.getByRole('button',{ name:'The Small Garden — saqlash' }).click();
  await page.getByRole('searchbox').clear(); await page.getByRole('button',{ name:'Saqlangan',exact:true }).click();
  await expect(page.locator('.reading-library__card')).toHaveCount(1); await page.reload();
  await page.getByRole('button',{ name:'Saqlangan',exact:true }).click(); await expect(page.locator('.reading-library__card')).toHaveCount(1);
});
test("Inline dictionary keeps translation and pronunciation without save controls", async ({ page }) => {
  const saves: string[] = [];
  page.on("request", request => {
    if (new URL(request.url()).pathname === "/api/vocabulary/learn") saves.push(request.url());
  });
  await fixture(page); await page.goto(lessonUrl);
  await page.getByRole('button',{ name:/^patient[.]?$/ }).click();
  await expect(page.locator('.reading-word')).toContainText('sabrli');
  await expect(page.getByRole("button",{ name:"patient talaffuzini tinglash" })).toBeVisible();
  await expect(page.getByRole("button",{ name:"Matnni saqlash" })).toHaveCount(0);
  await expect(page.getByRole("button",{ name:"So‘zni saqlash" })).toHaveCount(0);
  await page.getByRole("button",{ name:"Keyingi so‘z", exact:true }).click();
  await expect(page.locator(".reading-word h2")).toHaveText("father");
  expect(saves).toEqual([]);
});
test("Long passage uses the page scrollbar instead of a nested passage scrollbar",async ({page}) => {
  await page.setViewportSize({width:1366,height:768}); await fixture(page,{longText:true}); await page.goto(lessonUrl);
  const main=page.locator('[data-lesson-stage-body]'); await expect(page.locator('.reading-text')).toBeVisible();
  await expect.poll(()=>page.evaluate(()=>document.documentElement.scrollHeight-innerHeight)).toBeGreaterThan(500);
  await expect.poll(()=>main.evaluate(el=>({ overflow:getComputedStyle(el).overflowY, hidden:el.scrollHeight-el.clientHeight }))).toEqual({overflow:"visible",hidden:0});
  await page.evaluate(()=>window.scrollTo({top:document.documentElement.scrollHeight,behavior:"instant"}));
  await expect(page.getByRole('button',{name:'Savollarga o‘tamiz'})).toBeInViewport();
  await page.getByRole('button',{name:'Savollarga o‘tamiz'}).click(); await fitsLesson(page); await expect.poll(()=>main.evaluate(el=>el.scrollTop)).toBe(0);
});
test("Oversized questions and short landscape stay reachable",async ({page})=>{
  await page.setViewportSize({width:844,height:390}); await fixture(page,{longQuestion:true}); await openPractice(page);
  await fitsDocument(page); const last=page.locator('.reading-option').last(); await last.scrollIntoViewIfNeeded(); await expect(last).toBeInViewport();
});
test("Wrong feedback, return to passage, retry and final submit preserve answers",async ({page})=>{
  await fixture(page,{passed:false}); await openPractice(page);
  await page.getByRole('button',{name:/To stop practising/}).click(); await page.getByRole('button',{name:'Matnga qaytish ↗'}).click();
  await page.getByRole('button',{name:'Savolga qaytish'}).click(); await expect(page.getByRole('button',{name:/To stop practising/})).toHaveAttribute('aria-pressed','true');
  await page.getByRole('button',{name:'Tekshirish',exact:true}).click(); await expect(page.locator('.reading-feedback--wrong')).toBeVisible();
  await page.getByRole('button',{name:'Keyingi savol'}).click(); await answer(page);
  const submitted=page.waitForRequest(request=>request.url().endsWith('/api/reading/quiz'));
  await page.getByRole('button',{name:'Natijani ko‘rish'}).click(); expect((await submitted).postDataJSON().answers).toHaveLength(2);
  await page.getByRole('button',{name:'Qayta boshlash'}).click(); await expect(page.locator('.reading-text')).toBeVisible();
});

test("Pen reference geometry: 1440 desktop header, catalog and 960px lesson column", async ({ page }) => {
  await fixture(page); await page.setViewportSize({width:1440,height:900}); await page.goto('/reading');
  await expect(page.locator('.reading-library__card')).toHaveCount(4); await page.evaluate(()=>document.fonts.ready);
  const header=await page.locator('.reading-header').boundingBox(); expect(header?.height).toBe(88);
  const card=await page.locator('.reading-library__card').first().boundingBox(); expect(card?.x).toBe(64); expect(card?.width).toBe(316);
  expect(Math.abs((card?.y ?? 0)-444)).toBeLessThan(2);
  await expect(page.locator('.reading-heading h1')).toHaveCSS('font-size','44px');
  await page.goto(lessonUrl); await expect(page.locator('.reading-text')).toBeVisible();
  const heading=await page.locator('.reading-heading').boundingBox(); expect(heading?.x).toBe(240); expect(heading?.width).toBe(960);
  const word=await page.locator('.reading-word').boundingBox(); expect(word?.width).toBe(250);
  await expect(page.locator('.reading-text__columns')).toHaveCSS('column-gap','24px');
});

test("Reading completion opens the unlocked Writing lesson for the same topic", async ({ page }) => {
  await fixture(page);
  await openPractice(page);
  await answer(page);
  await page.getByRole("button", { name:"Keyingi savol" }).click();
  await answer(page);
  await page.getByRole("button", { name:"Natijani ko‘rish" }).click();
  await expect(page.getByRole("heading", { name:"Hikoyani tushundingiz!" })).toBeVisible();
  await page.getByRole("button", { name:"Writing’ga o‘tish" }).click();
  await expect(page).toHaveURL(new RegExp(`/writing/task/${topicId}$`));
});
