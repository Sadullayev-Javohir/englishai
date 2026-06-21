import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CefrLevel } from "@/api/types";
import { ReadingCatalogPage } from "./ReadingCatalogPage";

const passages = [
  { topicId:"travel", title:"Travel plans", titleUz:"Sayohat rejalari", level:CefrLevel.A1, category:"Travel" },
  { topicId:"family", title:"My Mother", titleUz:"Onam", level:CefrLevel.A1, category:"Family" },
];
const state = vi.hoisted(() => ({ locked:false }));
vi.mock("@/lib/useAsync", () => ({ useAsync:() => ({ data:passages, loading:false, error:null, reload:vi.fn() }) }));
vi.mock("@/api/client", () => ({ api:{ reading:{catalog:vi.fn()}, images:{topicUrl:(id:string)=>`/images/${id}`} } }));
vi.mock("@/app/session", () => ({ getLearnerId:()=>"learner-1" }));
vi.mock("@/lib/skillTopicGates", () => ({ useSkillTopicGates:()=>({ ready:true, gateOf:()=>({ isLocked:state.locked, requiresPro:false, passed:false, passedModuleCount:2, requiredModuleCount:6 }) }) }));
vi.mock("@/components/assistantContext", () => ({ publishAssistantContext:()=>undefined }));
afterEach(()=>{cleanup(); localStorage.clear(); state.locked=false;});
function renderPage() { return render(<MemoryRouter initialEntries={["/reading"]}><Routes><Route path="/reading" element={<ReadingCatalogPage/>}/><Route path="/reading/topic/:topicId" element={<div data-testid="topic-route">opened</div>}/></Routes></MemoryRouter>); }
describe("Reading catalog Pen 31",()=>{
  it("renders the reference heading, search, filters and cards without old progress widgets",()=>{
    renderPage(); expect(screen.getByRole('heading',{name:'Hikoya ichiga kiring.'})).toBeTruthy();
    expect(screen.getByRole('searchbox')).toBeTruthy(); expect(screen.getByRole('combobox').getAttribute('aria-label')).toBe('Reading darajasi');
    expect(screen.getByText('Travel plans')).toBeTruthy(); expect(document.querySelector('.reading-catalog__progress')).toBeNull();
  });
  it("opens the actual reading route",()=>{renderPage(); fireEvent.click(screen.getByRole('button',{name:/Travel plans.*Sayohat/}));expect(screen.getByTestId('topic-route')).toBeTruthy();});
  it("searches title and translation",()=>{renderPage();fireEvent.change(screen.getByRole('searchbox'),{target:{value:'Onam'}});expect(screen.getByText('My Mother')).toBeTruthy();expect(screen.queryByText('Travel plans')).toBeNull();});
  it("bookmarks a topic and filters saved topics",()=>{renderPage();fireEvent.click(screen.getByRole('button',{name:'Travel plans — saqlash'}));fireEvent.click(screen.getByRole('button',{name:'Saqlangan'}));expect(screen.getByText('Travel plans')).toBeTruthy();expect(screen.queryByText('My Mother')).toBeNull();});
  it("does not bypass topic locks",()=>{state.locked=true;renderPage();expect(screen.getByRole('button',{name:/Travel plans.*Sayohat/}).hasAttribute('disabled')).toBe(true);});
});
