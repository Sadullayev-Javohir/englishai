import { expect, test, type Page, type TestInfo } from "@playwright/test";

const topicId = "11111111-1111-1111-1111-111111111111";
const lessonUrl = `/app/grammar/topic/${topicId}`;
const mockApi = "http://127.0.0.1:5091";

// Drain fixture requests before Playwright disposes its request context,
// including requests started by the final navigation to the next module.
test.afterEach(async ({ page }) => {
  await page.unrouteAll({ behavior: "wait" });
});

const lesson = {
  topicId, topic: "Present Perfect", grammarFocusCode: "present-perfect", category: 1, level: 3, isReady: true,
  contextIntro: "My mother has supported me for years. She has taught me to keep trying.",
  explanation: "Use have / has + V3 when a past action is connected to the present.",
  targetWords: [],
  applicationTasks: [],
  curated: {
    titleUz: "O‘tgan ish. Hozirgi natija.",
    summaryUz: "Yordam o‘tmishda boshlangan, ta’siri hozir ham bor.",
    formulas: ["have / has + V3"],
    rules: [{ headingUz: "Bitta sodda qolip", bodyUz: "I / you / we / they bilan have; he / she / it bilan has ishlatiladi." }],
    examples: [
      { english: "I have finished my work.", uzbek: "Men ishimni tugatdim." },
      { english: "She has never been to Paris.", uzbek: "U hech qachon Parijda bo‘lmagan." },
      { english: "Have you eaten?", uzbek: "Ovqatlandingizmi?" },
    ],
    commonMistakesUz: [],
  },
  exercises: [
    { id: "q1", type: 1, prompt: "She ___ never been to Paris.", options: ["has", "have", "is", "was"] },
    { id: "q2", type: 2, prompt: "I have ___ my work.", options: ["finished", "finish", "finishing", "finishes"] },
    { id: "q3", type: 3, prompt: "I finished my work. I am free now.", options: ["I have finished my work.", "I finish my work.", "I am finishing my work.", "I will finish my work."] },
    { id: "q4", type: 1, prompt: "___ you eaten?", options: ["Have", "Has", "Are", "Do"] },
  ],
};

lesson.exercises.sort((left, right) => left.type - right.type);

async function fixture(page: Page, options: { longText?: boolean; longQuiz?: boolean; passed?: boolean; unavailable?: boolean } = {}) {
  let submitted = 0;
  await page.addInitScript(() => {
    localStorage.setItem("englishai.learnerId", "mock-learner-0001");
    localStorage.setItem("englishai.level.mock-learner-0001", "3");
    localStorage.setItem("englishai.goalPromptSeen.mock-learner-0001", "1");
    sessionStorage.setItem("englishai.due-review-modal.mock-learner-0001", "1");
  });
  // No request, including scores, can reach a real learner/backend.
  await page.route(url => url.pathname.startsWith("/api/"), async route => {
    const url = new URL(route.request().url());
    const response = await route.fetch({ url: `${mockApi}${url.pathname}${url.search}` });
    if (!route.request().failure()) {
      try { await route.fulfill({ response }); } catch (error) { if (!route.request().failure()) throw error; }
    }
  });
  await page.route("**/api/auth/me", route => route.fulfill({ json: {
    id: "mock-learner-0001", email: "demo@englishai.uz", displayName: "Demo", username: "demo",
    hasOnboarded: true, preferredName: "Demo", learningGoal: 1, hasCompletedDemographics: true,
  } }));
  await page.route("**/api/subscription/mock-learner-0001", async route => {
    const response = await route.fetch({ url: `${mockApi}/api/subscription/mock-learner-0001` });
    await route.fulfill({ json: { ...await response.json(), isTrialActive: false } });
  });
  await page.route("**/api/grammar/catalog/**", route => route.fulfill({ json: [
    [topicId,"Present Perfect","present-perfect"], ["conditional","First Conditional","first-conditional"], ["passive","Passive Voice","passive-voice"], ["reported","Reported Speech","reported-speech"],
  ].map(([id,title,code]) => ({ topicId:id,title,titleUz:code === "first-conditional" ? "If + present, will…" : code === "passive-voice" ? "Harakat va natija" : "Birovning gapini ayting",level:3,category:"Grammar",grammarFocusCode:code })) }));
  await page.route(`**/api/grammar/topic/${topicId}`, route => options.unavailable
    ? route.fulfill({ status: 503, json: { message: "Fixture unavailable" } })
    : route.fulfill({ json: {
      ...lesson,
      contextIntro: options.longText ? `${lesson.contextIntro}\n\n`.repeat(45) : lesson.contextIntro,
      exercises: options.longQuiz ? lesson.exercises.map(exercise => ({
        ...exercise,
        prompt: `${exercise.prompt} ${"Read the complete sentence before choosing your answer. ".repeat(12)}`,
        options: exercise.options.map(option => `${option} ${"A longer answer that must stay reachable. ".repeat(8)}`),
      })) : lesson.exercises,
    } }));
  const completion = {
    learnerId: "mock-learner-0001", topicId, level: "B1", isMastered: false, masteredAt: null,
    masteryThreshold: 75, passedModuleCount: 2, requiredModuleCount: 6,
    modules: ["Vocabulary", "Grammar", "Reading", "Listening", "Speaking", "Writing"].map((module, index) => ({
      module, passed: index < 2, unlocked: index <= 2, score: index < 2 ? 75 : null,
      achievedAt: index < 2 ? "2026-09-12T00:00:00Z" : null,
    })),
  };
  await page.route(`**/api/vocabulary/topic/${topicId}/completion/*`, route => route.fulfill({ json: completion }));
  await page.route("**/api/grammar/exercises/check", route => {
    const { exerciseId, selectedOptionIndex, textAnswer } = route.request().postDataJSON();
    const question = lesson.exercises.find(item => item.id === exerciseId)!;
    const correct = textAnswer !== undefined ? textAnswer.trim().toLowerCase() === question.options[0].toLowerCase() : selectedOptionIndex === 0;
    return route.fulfill({ json: {
      exerciseId, selectedOptionIndex, correctOptionIndex: 0, isCorrect: correct,
      explanation: correct ? "Qolipni to‘g‘ri ishlatdingiz." : "Have / has dan keyin V3: finish → finished.",
    } });
  });
  await page.route("**/api/grammar/exercises", route => {
    submitted++;
    return route.fulfill({ json: {
      topicId, totalExercises: 4, correctCount: options.passed === false ? 1 : 3,
      scorePercent: options.passed === false ? 25 : 75, passed: options.passed !== false, outcomes: [],
      completion: options.passed === false ? null : completion,
    } });
  });
  return { submissions: () => submitted };
}

async function noDocumentOverflow(page: Page) {
  await expect.poll(() => page.evaluate(() => ({ x: document.documentElement.scrollWidth-innerWidth, y: document.documentElement.scrollHeight-innerHeight }))).toEqual({x:0,y:0});
}
async function fits(page: Page, name: string, info?: TestInfo, allowTextScroll = false) {
  await page.evaluate(() => document.fonts.ready);
  await noDocumentOverflow(page);
  await expect(page.locator(".grammar-progress.lesson-progress")).toHaveCount(1);
  await expect(page.getByRole("progressbar", { name: "Dars bosqichlari" })).toBeInViewport({ ratio: 1 });
  await expect(page.locator('.grammar-progress [aria-current="step"]')).toHaveCount(1);
  if (!allowTextScroll) await expect.poll(() => page.locator(".grammar-lesson").evaluate(root => [root,...root.querySelectorAll<HTMLElement>("*")].filter(node => ["auto","scroll"].includes(getComputedStyle(node).overflowY) && node.scrollHeight > node.clientHeight+1).map(node => ({cls:node.className,overflow:node.scrollHeight-node.clientHeight}))), {message:name}).toEqual([]);
  await expect(page.locator(".grammar-lesson__actions>.grammar-primary,.grammar-result>.grammar-primary")).toBeInViewport({ratio:1});
  if(info) await page.screenshot({path:info.outputPath(`${name}.png`),fullPage:true});
}
async function openQuiz(page: Page) {
  await page.goto(lessonUrl);
  await page.getByRole("button",{name:"Qoidani ko‘rish"}).click();
  await page.getByRole("button",{name:"Misollarda ko‘rish"}).click();
  await page.getByRole("button",{name:"Endi o‘zim sinayman"}).click();
}

for (const width of [1440, 390]) {
  for (const type of [2, 3]) {
    test(`single input focus border ${width}px exercise ${type}`, async ({ page }, info) => {
      await fixture(page);
      const question = lesson.exercises.find(exercise => exercise.type === type)!;
      await page.route(`**/api/grammar/topic/${topicId}`, route => route.fulfill({ json: { ...lesson, exercises: [question] } }));
      await page.setViewportSize({ width, height: 900 });
      await openQuiz(page);
      const input = page.getByRole("textbox");
      const field = page.locator(".grammar-answer-field");
      const assertSingleBorder = async () => {
        await expect(input).toBeFocused();
        await expect(input).toHaveCSS("outline-style", "none");
        await expect(input).toHaveCSS("border-top-width", "0px");
        await expect(input).toHaveCSS("box-shadow", "none");
        await expect(field).toHaveCSS("border-top-width", "2px");
        await expect(field).toHaveCSS("border-top-color", "rgb(117, 69, 232)");
      };
      await input.click();
      await assertSingleBorder();
      await input.fill(question.options[0]);
      // Keyboard users keep the same outer focus cue; other controls retain
      // their own focus ring rather than disabling accessibility globally.
      await input.press("Tab");
      const submit = page.getByRole("button", { name: "Tekshirish", exact: true });
      await expect(submit).toBeFocused();
      await expect(submit).toHaveCSS("outline-style", "solid");
      await submit.press("Shift+Tab");
      await assertSingleBorder();
      await page.screenshot({ path: info.outputPath("single-focus-border.png"), fullPage: true });
    });
  }
}

for(const [width,height] of [[1440,900],[1366,768],[1280,720],[1024,768],[820,1180],[768,1024],[701,900],[700,900],[390,844],[375,667],[360,740]]) {
 test(`Pen 21–30 ${width}x${height}`,async({page},info)=>{
  const calls=await fixture(page);
  await page.clock.install();
  await page.setViewportSize({width,height});
  await page.goto("/app/grammar");
  await expect(page.locator(".grammar-catalog__card")).toHaveCount(4);
  await expect(page.getByRole("heading",{name:"Qoida emas, gap tuzing."})).toBeVisible();
  await expect.poll(()=>page.evaluate(()=>document.documentElement.scrollWidth-innerWidth)).toBe(0);
  const columns=await page.locator(".grammar-catalog__grid").evaluate(el=>getComputedStyle(el).gridTemplateColumns.split(" ").length);
  expect(columns).toBe(width>1100?4:width>700?2:1);
  await page.screenshot({path:info.outputPath("21-catalog.png"),fullPage:true});
  await page.getByRole("button",{name:"Present Perfect — Boshlash"}).click();
  for(const [screen,name,next] of [["22","22-context","Qoidani ko‘rish"],["23","23-rule","Misollarda ko‘rish"],["24","24-examples","Endi o‘zim sinayman"]]) {
    await expect(page.locator(`[data-pen-screen="${screen}"]`)).toBeVisible();
    await fits(page,name,info,screen!=="22");
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuenow", String(Number(screen) - 21));
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuemax", "7");
    await page.getByRole("button",{name:next}).click();
  }
  for(let i=0;i<4;i++) {
    const question=lesson.exercises[i];
    await expect(page.getByRole("heading",{name:question.prompt,exact:true})).toBeVisible();
    const section = question.type === 1 ? "Variantlar" : question.type === 2 ? "Yozish" : "Gap tuzish";
    const withinSection = question.type === 1 ? `${i + 1} / 2` : "1 / 1";
    const currentProgress = `${question.type + 3} / 7 · ${section} · ${withinSection} savol · Test ${i + 1} / 4`;
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext", currentProgress);
    if(question.type===1) await page.locator(".grammar-option").first().click();
    else await page.getByRole("textbox").fill(i===2?"finish":question.options[0]);
    await fits(page,i===2?"27-typed":i===3?"29-rephrase":"25-choice",info);
    await page.clock.pauseAt(await page.evaluate(()=>Date.now()+1000));
    await page.getByRole("button",{name:"Tekshirish",exact:true}).click();
    await expect(page.locator(".grammar-feedback")).toBeVisible();
    await fits(page,i===2?"28-correction":i===3?"29-rephrase-correct":"26-correct",info);
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext", currentProgress);
    if(i===2) {
      await page.getByRole("button",{name:"Tushundim, yana sinayman"}).click();
      await page.getByRole("textbox").fill("finished");
      await page.getByRole("button",{name:"Tekshirish",exact:true}).click();
      await expect(page.getByRole("heading",{name:"Ajoyib!"})).toBeVisible();
      await expect(page.getByLabel("4 yurak")).toBeVisible();
    }
    await page.getByRole("button",{name:i===3?"Natijamni ko‘rish":"Keyingi mashq",exact:true}).click();
    await page.clock.resume();
  }
  await expect(page.locator('[data-pen-screen="30"]')).toBeVisible();
  await fits(page,"30-result",info);
  await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext", "7 / 7 · Yakun");
  await expect(page.locator(".grammar-progress__segment.is-done")).toHaveCount(7);
  expect(calls.submissions()).toBe(1);
  await expect(page.locator(".grammar-result__metrics")).toContainText("3/4");
  await page.getByRole("button",{name:"Reading’ga o‘tish"}).click();
  await expect(page).toHaveURL(new RegExp(`/reading/topic/${topicId}$`));
 });
}
test("long text keeps the footer and scrolls only the reading region",async({page})=>{
 await fixture(page,{longText:true});await page.setViewportSize({width:390,height:844});await page.goto(lessonUrl);
 const body=page.locator(".grammar-lesson__body");
 await expect.poll(()=>body.evaluate(node=>node.scrollHeight-node.clientHeight)).toBeGreaterThan(1000);
 await body.hover();await page.mouse.wheel(0,1200);await expect.poll(()=>body.evaluate(node=>node.scrollTop)).toBeGreaterThan(0);
 await noDocumentOverflow(page);await expect(page.getByRole("button",{name:"Qoidani ko‘rish"})).toBeInViewport({ratio:1});
 await page.getByRole("button",{name:"Qoidani ko‘rish"}).click();await expect.poll(()=>body.evaluate(node=>node.scrollTop)).toBe(0);
});
test("catalog search and saved filters work without replacing topic identity",async({page})=>{
 await fixture(page);await page.goto("/app/grammar");await page.getByRole("textbox",{name:"Mavzuni qidiring"}).fill("conditional");
 await expect(page.locator(".grammar-catalog__card")).toHaveCount(1);
 await page.getByRole("button",{name:"First Conditional: saqlash"}).click();
 await page.getByRole("textbox").fill("");await page.getByRole("button",{name:"Saqlangan",exact:true}).click();await expect(page.locator(".grammar-catalog__card")).toHaveCount(1);
 await page.reload();await page.getByRole("button",{name:"Saqlangan",exact:true}).click();await expect(page.locator(".grammar-catalog__card")).toHaveCount(1);
});
test("oversized questions retain scroll access and the fixed action",async({page})=>{
 await fixture(page,{longQuiz:true});await page.setViewportSize({width:667,height:375});await openQuiz(page);
 const body=page.locator(".grammar-lesson__body");await expect.poll(()=>body.evaluate(node=>node.scrollHeight-node.clientHeight)).toBeGreaterThan(500);
 await noDocumentOverflow(page);const last=page.locator(".grammar-option").last();await last.scrollIntoViewIfNeeded();await last.click();
 await expect(page.getByRole("button",{name:"Tekshirish",exact:true})).toBeInViewport({ratio:1});
});

test("wrong choice keeps shared answer states and the same section progress", async ({page},info) => {
  await fixture(page);
  await page.setViewportSize({width:375,height:667});
  await openQuiz(page);
  await page.locator(".grammar-option").nth(1).click();
  await page.getByRole("button",{name:"Tekshirish",exact:true}).click();
  await expect(page.locator(".grammar-option[data-answer-state=wrong]")).toContainText("have");
  await expect(page.locator(".grammar-option[data-answer-state=correct]")).toContainText("has");
  await expect(page.locator(".grammar-option")).toHaveCount(4);
  await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext", "4 / 7 · Variantlar · 1 / 2 savol · Test 1 / 4");
  await fits(page,"wrong-choice",info);
  await page.getByRole("button",{name:"Tushundim, yana sinayman"}).click();
  await expect(page.locator(".grammar-option[data-answer-state=idle]")).toHaveCount(4);
  await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext", "4 / 7 · Variantlar · 1 / 2 savol · Test 1 / 4");
});

async function progressAppearance(page: Page) {
  return page.locator(".lesson-progress").evaluate(root => {
    const track = root.querySelector(".lesson-progress__track")!;
    const segment = root.querySelector(".lesson-progress__segment.is-current")!;
    const style = getComputedStyle(root);
    const bar = getComputedStyle(segment, "::before");
    return { gap:style.gap, minHeight:style.minHeight, font:style.fontFamily, fontSize:style.fontSize,
      trackGap:getComputedStyle(track).gap, height:bar.height, radius:bar.borderRadius, color:bar.backgroundColor };
  });
}

async function optionAppearance(page: Page) {
  return page.locator(".lesson-quiz-option").first().evaluate(root => {
    const style = getComputedStyle(root);
    const key = getComputedStyle(root.querySelector(".lesson-quiz-option__key")!);
    const label = getComputedStyle(root.querySelector(".lesson-quiz-option__label")!);
    return { radius:style.borderRadius, border:style.border, shadow:style.boxShadow, padding:style.padding,
      gap:style.gap, minHeight:style.minHeight, keySize:key.width, keyRadius:key.borderRadius,
      labelFont:label.font, labelColor:label.color };
  });
}

for(const [width,height] of [[1440,900],[820,1180],[390,844],[375,667]]) {
  test(`shared vocabulary and grammar controls ${width}x${height}`, async ({page},info) => {
    await fixture(page);
    await page.setViewportSize({width,height});
    await page.goto(`/app/vocabulary/topic/${topicId}`);
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext","1 / 5 · Kirish");
    const vocabularyProgress = await progressAppearance(page);
    await page.screenshot({path:info.outputPath("vocabulary-progress.png"),fullPage:true});
    await page.getByRole("button",{name:"Darsni boshlash",exact:true}).click();
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext","2 / 5 · Matn");
    await page.getByRole("button",{name:"So‘zlarga o‘tamiz",exact:true}).click();
    for(let i=0;i<2;i++) {
      await page.getByRole("button",{name:"Aylantirish",exact:true}).click();
      await page.getByRole("button",{name:"O‘rgandim →",exact:true}).click();
    }
    await page.getByRole("button",{name:"Testga o‘tish",exact:true}).click();
    await expect(page.locator(".vocabulary-quiz-option").first()).toBeVisible();
    const vocabularyOption = await optionAppearance(page);
    await page.screenshot({path:info.outputPath("vocabulary-test.png"),fullPage:true});
    await page.goto(lessonUrl);
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext","1 / 7 · Kontekst");
    expect(await progressAppearance(page)).toEqual(vocabularyProgress);
    for(const name of ["Qoidani ko‘rish","Misollarda ko‘rish","Endi o‘zim sinayman"]) await page.getByRole("button",{name,exact:true}).click();
    await expect(page.locator(".grammar-option").first()).toBeVisible();
    expect(await optionAppearance(page)).toEqual(vocabularyOption);
    await fits(page,"grammar-test",info);
  });
}

// Read-only live verification: a separate development session, never submit a result.
test("live localhost catalog and requested topic use Pen UI",async({page},info)=>{
 test.skip(process.env.GRAMMAR_LIVE_PREVIEW!=="1","Opt-in local API only");
 const response=await page.request.post("http://localhost:5173/api/auth/dev-login",{data:{}});
 expect(response.ok()).toBeTruthy();
 const {user}=await response.json();
 await page.addInitScript((id)=>{
  localStorage.setItem("englishai.goalPromptSeen."+id,"1");
  sessionStorage.setItem("englishai.due-review-modal."+id,"1");
 },user.id);
 for(const [width,height] of [[1440,900],[390,844]]) {
  await page.setViewportSize({width,height});
  await page.goto("http://localhost:5173/app/vocabulary/topic/1127ae12-0b33-4afd-8746-851d13f27d6f");
  await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuetext","1 / 5 · Kirish");
  const vocabularyProgress = await progressAppearance(page);
  await page.screenshot({path:info.outputPath(`live-vocabulary-progress-${width}.png`),fullPage:true});
  await page.goto("http://localhost:5173/app/grammar");
  await expect(page.getByRole("heading",{name:"Qoida emas, gap tuzing."})).toBeVisible();
  await expect(page.locator(".grammar-catalog__card").first()).toBeVisible();
  await page.screenshot({path:info.outputPath(`live-catalog-${width}.png`),fullPage:true});
  await page.goto("http://localhost:5173/app/grammar/topic/1127ae12-0b33-4afd-8746-851d13f27d6f");
  await expect(page.locator('[data-pen-screen="22"]')).toBeVisible();
  await expect(page.getByRole("heading",{name:"To Be",exact:true})).toBeVisible();
  await fits(page,`live-context-${width}`,info,true);
  expect(await progressAppearance(page)).toEqual(vocabularyProgress);
  await page.getByRole("button",{name:"Qoidani ko‘rish"}).click();
  await fits(page,`live-rule-${width}`,info,true);
  await page.getByRole("button",{name:"Misollarda ko‘rish"}).click();
  await fits(page,`live-examples-${width}`,info,true);
  await page.getByRole("button",{name:"Endi o‘zim sinayman"}).click();
  await fits(page,`live-test-${width}`,info);
 }
});
