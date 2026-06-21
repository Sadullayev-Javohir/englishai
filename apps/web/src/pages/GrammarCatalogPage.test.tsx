import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { GrammarCatalogPage } from "./GrammarCatalogPage";
const catalog=vi.fn();
const data=[{topicId:"topic-1",title:"My Mother",titleUz:"Oila",category:"People",level:3,grammarFocusCode:"present-perfect"}];
vi.mock("@/api/client",()=>({api:{grammar:{catalog:(...args:unknown[])=>catalog(...args)}}}));
vi.mock("@/app/session",()=>({getLearnerId:()=>"learner-1"}));
vi.mock("@/components/assistantContext",()=>({publishAssistantContext:vi.fn()}));
vi.mock("@/lib/skillTopicGates",()=>({useSkillTopicGates:()=>({ready:true,gateOf:()=>({isLocked:false,requiresPro:false,passed:false,isMastered:false,passedModuleCount:2,requiredModuleCount:6})})}));
function setup(){render(<MemoryRouter initialEntries={["/app/grammar"]}><Routes><Route path="/app/grammar" element={<GrammarCatalogPage/>}/><Route path="/app/grammar/topic/:topicId" element={<div>Lesson route</div>}/></Routes></MemoryRouter>);}
beforeEach(()=>{localStorage.clear();catalog.mockReset().mockResolvedValue(data);});afterEach(cleanup);
describe("Grammar Pen catalog",()=>{
 it("uses the Pen heading, search, compact filters and grammar focus title",async()=>{setup();await screen.findByText("Present Perfect");expect(screen.getByRole("heading",{name:"Qoida emas, gap tuzing."})).toBeTruthy();expect(screen.getByRole("textbox",{name:"Mavzuni qidiring"})).toBeTruthy();expect((screen.getByRole("combobox") as HTMLSelectElement).value).toBe("3");expect(screen.getByRole("img",{name:"Present Perfect"}).getAttribute("src")).toBe("/assets/grammar/my-mother.jpg");});
 it("opens the actual topic, not a focus-code placeholder",async()=>{setup();fireEvent.click(await screen.findByRole("button",{name:"Present Perfect — Boshlash"}));expect(screen.getByText("Lesson route")).toBeTruthy();});
 it("searches and saves per learner, with an honest empty state",async()=>{setup();await screen.findByText("Present Perfect");fireEvent.change(screen.getByRole("textbox"),{target:{value:"absent"}});expect(screen.getByText("Mavzu topilmadi.")).toBeTruthy();fireEvent.change(screen.getByRole("textbox"),{target:{value:""}});fireEvent.click(screen.getByRole("button",{name:"Present Perfect: saqlash"}));expect(JSON.parse(localStorage.getItem("englishai.grammar.saved.learner-1")!)).toEqual(["topic-1"]);fireEvent.click(screen.getByRole("button",{name:"Saqlangan"}));await screen.findByText("Present Perfect");fireEvent.click(screen.getByRole("button",{name:"Present Perfect: saqlangandan olib tashlash"}));expect(screen.getByText("Hali saqlangan mavzu yo‘q.")).toBeTruthy();});
 it("requests actual levels and all topics",async()=>{setup();await screen.findByText("Present Perfect");fireEvent.change(screen.getByRole("combobox"),{target:{value:"4"}});await waitFor(()=>expect(catalog).toHaveBeenLastCalledWith("learner-1",4,false));fireEvent.click(screen.getByRole("button",{name:"Barchasi"}));await waitFor(()=>expect(catalog).toHaveBeenLastCalledWith("learner-1",undefined,true));});
});
