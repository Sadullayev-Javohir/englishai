import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LandingPage } from "./LandingPage";

const { publicMetrics, askProject, plans } = vi.hoisted(() => ({
  publicMetrics: vi.fn().mockResolvedValue({
    asOf: "2026-07-28",
    registeredUsers: 0,
    activePremiumUsers: 0,
    activeLearners30d: 0,
    totalStudyMinutes: 0,
    speakingSessions: 0,
    speakingMinutes: 0,
  }),
  askProject: vi.fn().mockResolvedValue({ reply: "EnglishAI o'zbek tilida ingliz tilini o'rganishga yordam beradi." }),
  // Checkout closed, matching production until a payment provider is connected.
  plans: vi.fn().mockResolvedValue({ plans: [], paymentsEnabled: false, currency: "UZS" }),
}));

beforeEach(() => {
  vi.spyOn(window, "scrollTo").mockImplementation(() => {});
});

vi.mock("@/api/client", () => ({
  api: {
    publicMetrics,
    assistant: { askProject },
    subscription: { plans },
  },
}));

let authStatus: "authenticated" | "unauthenticated" = "unauthenticated";

vi.mock("@/app/auth", () => ({
  useAuth: () => ({ status: authStatus }),
}));

vi.mock("framer-motion", async () => {
  const React = await import("react");
  const motionProps = new Set(["animate", "initial", "layout", "transition", "variants", "viewport", "whileHover", "whileInView", "whileTap"]);
  const motion = new Proxy({}, {
    get: (_target, tag: string) => React.forwardRef(
      ({ children, ...props }: Record<string, unknown>, ref) => {
        const elementProps = Object.fromEntries(Object.entries(props).filter(([key]) => !motionProps.has(key)));
        return React.createElement(tag, { ...elementProps, ref } as React.Attributes, children as React.ReactNode);
      },
    ),
  });
  return {
    motion,
    AnimatePresence: ({ children }: { children: React.ReactNode }) => children,
    useReducedMotion: () => true,
    useScroll: () => ({ scrollYProgress: 0 }),
    useTransform: () => 0,
    useInView: () => true,
  };
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  authStatus = "unauthenticated";
  publicMetrics.mockResolvedValue({
    asOf: "2026-07-28",
    registeredUsers: 0,
    activePremiumUsers: 0,
    activeLearners30d: 0,
    totalStudyMinutes: 0,
    speakingSessions: 0,
    speakingMinutes: 0,
  });
  askProject.mockReset();
  askProject.mockResolvedValue({ reply: "EnglishAI o'zbek tilida ingliz tilini o'rganishga yordam beradi." });
});

function renderLanding() {
  render(
    <MemoryRouter initialEntries={["/"]}>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<div>LOGIN_DESTINATION</div>} />
        <Route path="/home" element={<div>HOME_DESTINATION</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("Play landing conversion flow", () => {
 it("opens login for guests", () => { renderLanding(); fireEvent.click(screen.getAllByRole("button",{name:"Bepul boshlash"})[0]); expect(screen.getByText("LOGIN_DESTINATION")).toBeTruthy(); });
 it("opens home for authenticated learners", () => { authStatus="authenticated";renderLanding();fireEvent.click(screen.getByRole("link",{name:"Davom etish"}));expect(screen.getByText("HOME_DESTINATION")).toBeTruthy(); });
 it("uses the actual Pen headline and original artwork", () => { renderLanding();expect(screen.getByRole("heading",{name:/Bilish yaxshi.*Gapirish zo‘r/})).toBeTruthy();expect(screen.getByRole("img",{name:"EnglishAI binafsha to‘tiqushi"}).getAttribute("src")).toBe("/assets/play/parrot.svg");expect(screen.queryByRole("button",{name:/dark|qorong/i})).toBeNull(); });
 it("explores all six skills without fake runtime statistics", () => { renderLanding();for(const label of ["Speaking","Vocabulary","Listening","Grammar","Reading","Writing"])expect(screen.getByRole("button",{name:new RegExp(label)})).toBeTruthy();expect(publicMetrics).not.toHaveBeenCalled(); });
 it("provides feedback for both answers in the real interactive demo", () => { renderLanding();fireEvent.click(screen.getByRole("button",{name:"Rahmat"}));expect(screen.getByRole("status").textContent).toContain("Yana bir bor");fireEvent.click(screen.getByRole("button",{name:"Salom"}));expect(screen.getByRole("status").textContent).toContain("To‘ppa-to‘g‘ri"); });
 it("offers APK download but does not pretend the Play listing exists", () => { renderLanding();expect(screen.getByRole("link",{name:/APK yuklab olish/}).getAttribute("download")).toBe("englishai.apk");expect((screen.getByRole("button",{name:/Play Market/}) as HTMLButtonElement).disabled).toBe(true); });
 it("keeps pricing, learning guides, methodology and support discoverable", () => { renderLanding();for(const path of ["/pricing","/learn","/methodology","/contact"])expect(document.querySelector(`a[href="${path}"]`)).toBeTruthy(); });
 it("opens the working public assistant without a static contact block", async () => {
   renderLanding();
   fireEvent.click(screen.getByRole("button", { name: "EnglishAI loyiha yordamchisini ochish" }));
   const panel = screen.getByRole("dialog", { name: "AI yordamchi" });
   const dialog = within(panel);
   expect(panel.querySelector(".ea-overlay__header")).toBeTruthy();
   expect(panel.querySelector(".ea-overlay__footer .pl-assistant-composer")).toBeTruthy();
   expect(panel.querySelector(".ea-overlay__body .pl-assistant-messages")).toBeTruthy();
   expect(panel.querySelector(".ea-overlay__body textarea")).toBeNull();
   expect(dialog.queryByText("Rasmiy aloqa kanallari")).toBeNull();
   expect(dialog.queryByText("Telegram support")).toBeNull();
   expect(dialog.queryByText("EnglishAI.uz LinkedIn")).toBeNull();
   expect(dialog.queryByText("Javohir Sadullayev")).toBeNull();
   expect(dialog.queryAllByRole("link")).toHaveLength(0);
   fireEvent.change(screen.getByLabelText("EnglishAI haqida savol"), { target: { value: "EnglishAI nima?" } });
   fireEvent.keyDown(screen.getByLabelText("EnglishAI haqida savol"), { key: "Enter" });
   await waitFor(() => expect(askProject).toHaveBeenCalled());
   await screen.findByText("EnglishAI o'zbek tilida ingliz tilini o'rganishga yordam beradi.");
   expect(dialog.queryAllByRole("link")).toHaveLength(0);
 });

 it("still renders official contact information when the assistant answers a support question", async () => {
   askProject.mockResolvedValueOnce({
     reply: "**Telegram support:** https://t.me/englishaiuz\n\n**EnglishAI.uz LinkedIn:** https://www.linkedin.com/company/englishai-uz/\n\n**Javohir Sadullayev:** https://www.linkedin.com/in/javohir-sadullayev-b8737725a/",
   });
   renderLanding();
   fireEvent.click(screen.getByRole("button", { name: "EnglishAI loyiha yordamchisini ochish" }));
   const dialog = within(screen.getByRole("dialog", { name: "AI yordamchi" }));
   expect(dialog.queryAllByRole("link")).toHaveLength(0);
   fireEvent.click(screen.getByRole("button", { name: "Support uchun qanday bog'lanaman?" }));
   await waitFor(() => expect(askProject).toHaveBeenCalledWith("Support uchun qanday bog'lanaman?", expect.any(Array), "uz"));
   await dialog.findByText("Telegram support:");
   for (const url of ["https://t.me/englishaiuz", "https://www.linkedin.com/company/englishai-uz/", "https://www.linkedin.com/in/javohir-sadullayev-b8737725a/"]) {
     expect(dialog.getByText(url)).toBeTruthy();
   }
   expect(dialog.queryByText("Rasmiy aloqa kanallari")).toBeNull();
 });
});
