import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import { CefrLevel, ErrorCategory, GrammarExerciseType } from "@/api/types";
import type { GrammarExerciseCheckDto, GrammarExerciseResultDto, GrammarLessonDto } from "@/api/types";
import { GrammarLessonPage } from "./GrammarLessonPage";

vi.mock("@/api/client", () => ({ api: { grammar: { lesson:vi.fn(), checkExercise:vi.fn(), submitExercises:vi.fn() }, vocabulary:{resetTopicModuleScore:vi.fn()} } }));
vi.mock("@/components/TopicImage", () => ({ TopicImage: () => <div>topic image</div> }));
vi.mock("@/components/assistantContext", () => ({ publishAssistantContext:vi.fn() }));
vi.mock("@/components/lesson/useLessonSounds", () => ({ useLessonSounds:()=>({playAnswer:vi.fn(),playComplete:vi.fn()}) }));
const lesson:GrammarLessonDto = {
  topicId:"topic-1",topic:"My Mother",grammarFocusCode:"present-perfect",category:ErrorCategory.VerbTense,level:CefrLevel.B1,isReady:true,
  contextIntro:"My mother has supported me for years.",explanation:"Use have / has + V3.",targetWords:[],applicationTasks:[],
  curated:{titleUz:"O‘tgan ish. Hozirgi natija.",summaryUz:"Ta’siri hozir ham bor.",formulas:["have / has + V3"],rules:[{headingUz:"I / you / we / they",bodyUz:"have"}],examples:[{english:"I have finished my work.",uzbek:"Men ishimni tugatdim."}],commonMistakesUz:[]},
  exercises:[
    {id:"q1",type:GrammarExerciseType.Recognition,prompt:"She ___ never been to Paris.",options:["has","have","is","was"]},
    {id:"q2",type:GrammarExerciseType.FillInBlank,prompt:"I have ___ my work.",options:["finished","finish","finishes"]},
    {id:"q3",type:GrammarExerciseType.Rephrase,prompt:"I finished my work. I am free now.",options:["I have finished my work.","I finish my work."]},
  ],
};
const checked = (id:string,correct=true):GrammarExerciseCheckDto => ({exerciseId:id,selectedOptionIndex:correct?0:-1,correctOptionIndex:0,isCorrect:correct,explanation:correct?null:"Have / has dan keyin V3."});
const result:GrammarExerciseResultDto = {topicId:"topic-1",totalExercises:3,correctCount:2,scorePercent:67,passed:false,outcomes:[checked("q1"),checked("q2",false),checked("q3")],completion:null};
function renderPage() { return render(<MemoryRouter initialEntries={["/app/grammar/topic/topic-1"]}><Routes><Route path="/app/grammar/topic/:topicId" element={<GrammarLessonPage />} /></Routes></MemoryRouter>); }
async function openQuiz() {
  renderPage(); await screen.findByRole("heading",{name:"Present Perfect"});
  fireEvent.click(screen.getByRole("button",{name:"Qoidani ko‘rish"}));
  fireEvent.click(screen.getByRole("button",{name:"Misollarda ko‘rish"}));
  fireEvent.click(screen.getByRole("button",{name:"Endi o‘zim sinayman"}));
}
async function checkChoice() {
  fireEvent.click(screen.getByRole("button",{name:/^A\. has$/}));
  fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
  await screen.findByRole("heading",{name:"Ajoyib!"});
  fireEvent.click(screen.getByRole("button",{name:"Keyingi mashq"}));
}
beforeEach(()=>{localStorage.clear();vi.mocked(api.grammar.lesson).mockResolvedValue(lesson);vi.mocked(api.grammar.checkExercise).mockReset();vi.mocked(api.grammar.submitExercises).mockReset().mockResolvedValue(result);});
afterEach(()=>{cleanup();vi.restoreAllMocks();});
describe("Grammar Pen 22–30",()=>{
  it("opens directly on context, followed by formula and example designs",async()=>{
    renderPage(); await screen.findByRole("heading",{name:"Present Perfect"});
    expect(document.querySelector('[data-pen-screen="22"]')).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("1 / 7 · Kontekst");
    expect(screen.queryByRole("button",{name:/Darsni ochish/})).toBeNull();
    fireEvent.click(screen.getByRole("button",{name:"Qoidani ko‘rish"}));
    expect(document.querySelector('[data-pen-screen="23"]')).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("2 / 7 · Qoida");
    expect(screen.getByText("have / has + V3")).toBeTruthy();
    fireEvent.click(screen.getByRole("button",{name:"Misollarda ko‘rish"}));
    expect(document.querySelector('[data-pen-screen="24"]')).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("3 / 7 · Misollar");
    expect(screen.getByText("Men ishimni tugatdim.")).toBeTruthy();
  });
  it("selects without grading until Tekshirish and prevents duplicate checks",async()=>{
    let resolve!:(value:GrammarExerciseCheckDto)=>void;
    vi.mocked(api.grammar.checkExercise).mockImplementation(()=>new Promise(r=>{resolve=r;}));
    await openQuiz();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("4 / 7 · Variantlar · 1 / 1 savol · Test 1 / 3");
    expect((screen.getByRole("button",{name:"Tekshirish"}) as HTMLButtonElement).disabled).toBe(true);
    fireEvent.click(screen.getByRole("button",{name:/^A\. has$/}));
    expect(api.grammar.checkExercise).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
    fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
    expect(api.grammar.checkExercise).toHaveBeenCalledTimes(1);
    resolve(checked("q1")); await screen.findByRole("heading",{name:"Ajoyib!"});
    expect(document.querySelector('[data-pen-screen="26"]')).toBeTruthy();
  });
  it("renders a typed blank and a sentence textarea, both graded on the server",async()=>{
    vi.mocked(api.grammar.checkExercise).mockResolvedValueOnce(checked("q1")).mockResolvedValueOnce(checked("q2"));
    await openQuiz(); await checkChoice();
    expect(document.querySelector('[data-pen-screen="27"]')).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("5 / 7 · Yozish · 1 / 1 savol · Test 2 / 3");
    expect(screen.queryByRole("group",{name:"Javob variantlari"})).toBeNull();
    const input=screen.getByRole("textbox",{name:"Fe’lning mos shakli"});
    fireEvent.change(input,{target:{value:"finished"}});fireEvent.submit(input.closest("form")!);
    await screen.findByRole("heading",{name:"Ajoyib!"});
    expect(api.grammar.checkExercise).toHaveBeenLastCalledWith("topic-1","q2",-1,"finished");
    fireEvent.click(screen.getByRole("button",{name:"Keyingi mashq"}));
    expect(document.querySelector('[data-pen-screen="29"]')).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("6 / 7 · Gap tuzish · 1 / 1 savol · Test 3 / 3");
    expect(screen.getByRole("textbox",{name:"SIZNING GAPINGIZ"}).tagName).toBe("TEXTAREA");
  });
  it("keeps the first wrong answer for scoring and charges one heart across correction",async()=>{
    vi.mocked(api.grammar.checkExercise).mockResolvedValueOnce(checked("q1")).mockResolvedValueOnce(checked("q2",false)).mockResolvedValueOnce(checked("q2")).mockResolvedValueOnce(checked("q3"));
    await openQuiz();await checkChoice();
    fireEvent.change(screen.getByRole("textbox"),{target:{value:"finish"}});fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
    await waitFor(()=>expect(document.querySelector('[data-pen-screen="28"]')).toBeTruthy());
    expect(screen.getByLabelText("4 yurak")).toBeTruthy();
    fireEvent.click(screen.getByRole("button",{name:"Tushundim, yana sinayman"}));
    fireEvent.change(screen.getByRole("textbox"),{target:{value:"finished"}});fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
    await screen.findByRole("heading",{name:"Ajoyib!"});expect(screen.getByLabelText("4 yurak")).toBeTruthy();
    fireEvent.click(screen.getByRole("button",{name:"Keyingi mashq"}));
    fireEvent.change(screen.getByRole("textbox"),{target:{value:"I have finished my work."}});fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
    await screen.findByRole("heading",{name:"Ma’no saqlandi."});fireEvent.click(screen.getByRole("button",{name:"Natijamni ko‘rish"}));
    await screen.findByRole("heading",{name:"Birga yana mashq qilamiz."});
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toBe("7 / 7 · Yakun");
    expect(document.querySelectorAll(".lesson-progress__segment.is-done")).toHaveLength(7);
    expect(api.grammar.submitExercises).toHaveBeenCalledTimes(1);
    expect(vi.mocked(api.grammar.submitExercises).mock.calls[0][2][1]).toEqual({exerciseId:"q2",selectedOptionIndex:-1,textAnswer:"finish"});
    fireEvent.click(screen.getByRole("button",{name:"Xatolarimni ko‘rish"}));expect(screen.getByRole("region",{name:"Xatolarim"})).toBeTruthy();
  });
  it("can retry a failed final save without restarting the lesson",async()=>{
    vi.mocked(api.grammar.lesson).mockResolvedValue({...lesson,exercises:[lesson.exercises[0]]});
    vi.mocked(api.grammar.checkExercise).mockResolvedValue(checked("q1"));
    vi.mocked(api.grammar.submitExercises).mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(result);
    await openQuiz();fireEvent.click(screen.getByRole("button",{name:/^A\. has$/}));fireEvent.click(screen.getByRole("button",{name:"Tekshirish"}));
    await screen.findByRole("heading",{name:"Ajoyib!"});fireEvent.click(screen.getByRole("button",{name:"Natijamni ko‘rish"}));
    await screen.findByRole("alert");fireEvent.click(screen.getByRole("button",{name:"Natijamni ko‘rish"}));
    await screen.findByRole("heading",{name:"Birga yana mashq qilamiz."});expect(api.grammar.submitExercises).toHaveBeenCalledTimes(2);
  });
  it("keeps a wrong choice, the correct choice and other options in the shared test layout", async () => {
    vi.mocked(api.grammar.checkExercise).mockResolvedValue(checked("q1", false));
    await openQuiz();
    fireEvent.click(screen.getByRole("button", { name: "B. have" }));
    fireEvent.click(screen.getByRole("button", { name: "Tekshirish" }));
    await waitFor(() => expect(document.querySelector('[data-pen-screen="28"]')).toBeTruthy());
    expect(screen.getByRole("group", { name: "Javob variantlari" })).toBeTruthy();
    expect(screen.getByRole("button", { name: "A. has — to‘g‘ri javob" }).getAttribute("data-answer-state")).toBe("correct");
    expect(screen.getByRole("button", { name: "B. have — xato javob" }).getAttribute("data-answer-state")).toBe("wrong");
    expect(screen.getByRole("progressbar").getAttribute("aria-valuetext")).toContain("Variantlar · 1 / 1 savol");
    expect(screen.getByLabelText("4 yurak")).toBeTruthy();
  });
});
