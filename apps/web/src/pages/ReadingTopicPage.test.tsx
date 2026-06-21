import { act, cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { ReadingTopicPage } from "./ReadingTopicPage";

const mocks=vi.hoisted(()=>({check:vi.fn(),submit:vi.fn(),track:vi.fn().mockResolvedValue(undefined),passage:vi.fn()}));
vi.mock("@/lib/useAsync",()=>({useAsync:()=>({data:mocks.passage(),loading:false,error:null,reload:vi.fn()})}));
vi.mock("@/api/client",()=>({api:{reading:{passage:mocks.passage,checkAnswer:mocks.check,submit:mocks.submit},analytics:{track:mocks.track}}}));
vi.mock("@/app/session",()=>({getLearnerId:()=>"learner-1"}));
vi.mock("@/components/assistantContext",()=>({publishAssistantContext:()=>undefined}));
vi.mock("@/components/lesson/useLessonSounds",()=>({useLessonSounds:()=>({playAnswer:vi.fn(),playComplete:vi.fn()})}));
vi.mock("@/components/reading/ReadingWordPanel",()=>({ReadingWordPanel:({words,index}:{words:{word:string;translation:string}[];index:number})=><aside data-testid="word-panel">{words[index]?.word} — {words[index]?.translation}</aside>}));
vi.mock("@/components/reading/ReadingResult",()=>({ReadingResult:({onRetry}:{onRetry:()=>void})=><div data-testid="reading-result"><button onClick={onRetry}>Qayta mashq qilish</button></div>}));
const topic={topicId:"topic-1",passageId:"passage-1",title:"Small steps, big support",body:"Her patience has taught me to stay calm. We make a journey together.",topic:"My Mother",level:CefrLevel.B1,wordCount:17,isReady:true,glossary:[],targetWords:[{word:"journey",translation:"sayohat",exampleSentence:null}],questions:[{id:"question-1",prompt:"Question one",options:["To stay calm.","To hurry."]},{id:"question-2",prompt:"Question two",options:["To stay calm.","To hurry."]}]};
function renderPage(){return render(<MemoryRouter initialEntries={["/reading/topic/topic-1"]}><Routes><Route path="/reading/topic/:topicId" element={<ReadingTopicPage/>}/></Routes></MemoryRouter>);}
function practice(){renderPage();fireEvent.click(screen.getByRole('button',{name:'Savollarga o‘tamiz'}));}
async function choose(correct=true){fireEvent.click(screen.getByRole('button',{name:correct ? /To stay calm/ : /To hurry/}));fireEvent.click(screen.getByRole('button',{name:'Tekshirish'}));await screen.findByRole('heading',{name:'Javob matnning o‘zida.'});}
beforeEach(()=>{localStorage.clear();mocks.passage.mockReturnValue(topic);mocks.check.mockImplementation((_t:string,q:string,index:number)=>Promise.resolve({questionId:q,selectedOptionIndex:index,correctOptionIndex:0,isCorrect:index===0,explanation:null}));mocks.submit.mockResolvedValue({topicId:'topic-1',totalQuestions:2,correctCount:2,passed:true,scorePercent:100,outcomes:[],completion:null});});
afterEach(()=>{cleanup();localStorage.clear();vi.clearAllMocks();vi.useRealTimers();});
describe('Reading Pen 32–35 flow',()=>{
  it('omits the passage bookmark control while keeping its metadata',()=>{
    renderPage();
    expect(screen.queryByRole('button',{name:'Matnni saqlash'})).toBeNull();
    expect(document.querySelector('.reading-passage__controls .lucide-bookmark')).toBeNull();
    expect(document.querySelector('.reading-passage__controls')?.textContent).toContain('MY MOTHER');
    expect(document.querySelector('.reading-passage__controls')?.textContent).toContain('1 ta so‘z');
  });
  it('opens directly on the passage and inline dictionary, without retired hub or glossary slides',()=>{renderPage();expect(screen.getByRole('heading',{name:topic.title})).toBeTruthy();expect(screen.getByTestId('word-panel').textContent).toContain('sayohat');expect(document.querySelector('.reading-topic-hero-card')).toBeNull();expect(screen.queryByRole('button',{name:'Darsni boshlash'})).toBeNull();});
  it('uses the shared labelled progress bar for catalog, passage, test, and result sections', async()=>{
    renderPage();
    const progress = document.querySelector('.reading-progress');
    expect(progress?.getAttribute('data-lesson-step')).toBe('text');
    expect(progress?.querySelectorAll('.reading-progress__segment')).toHaveLength(4);
    expect(progress?.querySelector('[role="progressbar"]')?.getAttribute('aria-valuetext')).toBe('2 / 4 · Matn');

    fireEvent.click(screen.getByRole('button',{name:'Savollarga o‘tamiz'}));
    expect(progress?.getAttribute('data-lesson-step')).toBe('practice');
    expect(progress?.querySelector('[role="progressbar"]')?.getAttribute('aria-valuetext')).toBe('3 / 4 · Test');

    await choose();
    fireEvent.click(screen.getByRole('button',{name:'Keyingi savol'}));
    await choose();
    fireEvent.click(screen.getByRole('button',{name:'Natijani ko‘rish'}));
    await screen.findByTestId('reading-result');
    expect(progress?.getAttribute('data-lesson-step')).toBe('result');
    expect(progress?.getAttribute('data-complete')).toBe('true');
    expect(progress?.querySelector('[role="progressbar"]')?.getAttribute('aria-valuetext')).toBe('4 / 4 · Yakun');
  });
  it('requires selection and an explicit Tekshirish click before calling the API',async()=>{practice();expect(screen.getByRole('button',{name:'Tekshirish'}).hasAttribute('disabled')).toBe(true);fireEvent.click(screen.getByRole('button',{name:/To stay calm/}));expect(mocks.check).not.toHaveBeenCalled();fireEvent.click(screen.getByRole('button',{name:'Tekshirish'}));await screen.findByText('Aniq topdingiz!');expect(mocks.check).toHaveBeenCalledWith('topic-1','question-1',0);});
  it('shows a separate feedback screen and quotes an actual sentence from the passage',async()=>{practice();await choose();expect(document.querySelector('.reading-options')).toBeNull();expect(screen.getByText('MATNDAN DALIL')).toBeTruthy();expect(screen.getByText('“Her patience has taught me to stay calm.”')).toBeTruthy();});
  it('keeps the selection when returning to the passage',()=>{practice();fireEvent.click(screen.getByRole('button',{name:/To hurry/}));fireEvent.click(screen.getByRole('button',{name:'Matnga qaytish ↗'}));expect(screen.getByRole('heading',{name:topic.title})).toBeTruthy();fireEvent.click(screen.getByRole('button',{name:'Savolga qaytish'}));expect(screen.getByRole('button',{name:/To hurry/}).getAttribute('aria-pressed')).toBe('true');});
  it('does not auto-advance away from the evidence while the learner reads it',async()=>{practice();await choose();vi.useFakeTimers();act(()=>vi.advanceTimersByTime(10_000));expect(screen.getByText('Aniq topdingiz!')).toBeTruthy();expect(mocks.submit).not.toHaveBeenCalled();});
  it('clears selection when advancing and submits the complete answer batch once',async()=>{practice();await choose();fireEvent.click(screen.getByRole('button',{name:'Keyingi savol'}));expect(screen.getByText('Question two')).toBeTruthy();expect(screen.getByRole('button',{name:'Tekshirish'}).hasAttribute('disabled')).toBe(true);await choose();const next=screen.getByRole('button',{name:'Natijani ko‘rish'});fireEvent.click(next);fireEvent.click(next);await screen.findByTestId('reading-result');expect(mocks.submit).toHaveBeenCalledTimes(1);expect(mocks.submit).toHaveBeenCalledWith('topic-1','learner-1',[{questionId:'question-1',selectedOptionIndex:0},{questionId:'question-2',selectedOptionIndex:0}]);});
  it('retains feedback and answers when submit fails, and permits retry',async()=>{mocks.submit.mockRejectedValueOnce(new Error('offline'));practice();await choose();fireEvent.click(screen.getByRole('button',{name:'Keyingi savol'}));await choose();fireEvent.click(screen.getByRole('button',{name:'Natijani ko‘rish'}));await screen.findByRole('alert');fireEvent.click(screen.getByRole('button',{name:'Natijani ko‘rish'}));await screen.findByTestId('reading-result');expect(mocks.submit).toHaveBeenCalledTimes(2);});
  it('retains check-error selection and permits retry',async()=>{mocks.check.mockRejectedValueOnce(new Error('offline'));practice();fireEvent.click(screen.getByRole('button',{name:/To stay calm/}));fireEvent.click(screen.getByRole('button',{name:'Tekshirish'}));await screen.findByRole('alert');expect(screen.getByRole('button',{name:/To stay calm/}).getAttribute('aria-pressed')).toBe('true');fireEvent.click(screen.getByRole('button',{name:'Tekshirish'}));await screen.findByText('Aniq topdingiz!');});
  it('exhausted hearts restart from the new passage screen',async()=>{practice();await choose(false);fireEvent.click(screen.getByRole('button',{name:'Keyingi savol'}));await choose(false);fireEvent.click(screen.getByRole('button',{name:'Natijani ko‘rish'}));await screen.findByText('Yuraklar tugadi');fireEvent.click(screen.getByRole('button',{name:'Qayta boshlash'}));expect(screen.getByRole('heading',{name:topic.title})).toBeTruthy();expect(mocks.submit).not.toHaveBeenCalled();});
  it('restarts a completed attempt without retaining old answers',async()=>{practice();await choose();fireEvent.click(screen.getByRole('button',{name:'Keyingi savol'}));await choose();fireEvent.click(screen.getByRole('button',{name:'Natijani ko‘rish'}));fireEvent.click(await screen.findByRole('button',{name:'Qayta mashq qilish'}));fireEvent.click(screen.getByRole('button',{name:'Savollarga o‘tamiz'}));await waitFor(()=>expect(screen.getByText('Question one')).toBeTruthy());expect(screen.getByRole('button',{name:'Tekshirish'}).hasAttribute('disabled')).toBe(true);});
});
