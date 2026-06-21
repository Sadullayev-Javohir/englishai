import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { playLanding } from "@/content/playLanding";
import { LandingPage } from "./LandingPage";

let authStatus = "unauthenticated";
const { askProject } = vi.hoisted(() => ({ askProject: vi.fn() }));
vi.mock("@/app/auth", () => ({ useAuth: () => ({ status: authStatus }) }));
vi.mock("@/api/client", () => ({ api: { assistant: { askProject } } }));

beforeEach(() => {
  authStatus = "unauthenticated";
  askProject.mockReset();
  askProject.mockResolvedValue({ reply: "EnglishAI connects your learning skills in one place." });
  vi.spyOn(window, "scrollTo").mockImplementation(() => {});
});
afterEach(() => vi.restoreAllMocks());

function CurrentPath() {
  return <div data-testid="current-path">{useLocation().pathname}</div>;
}

function renderEnglish(path = "/en/") {
  return render(
    <MemoryRouter initialEntries={[path]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <CurrentPath />
      <Routes>
        <Route path="/en/" element={<LandingPage locale="en" />} />
        <Route path="/landing" element={<LandingPage />} />
        <Route path="/landing/eng" element={<LandingPage locale="en" />} />
        <Route path="/" element={<LandingPage locale="uz" />} />
        <Route path="/login" element={<div>LOGIN_DESTINATION</div>} />
        <Route path="/home" element={<div>HOME_DESTINATION</div>} />
      </Routes>
    </MemoryRouter>,
  );
}

describe("English version of the real Play landing", () => {
  it("uses the same landing sections and artwork instead of an SEO article", () => {
    const { container } = renderEnglish();
    expect(container.querySelector(".play-landing[lang=en]")).toBeTruthy();
    expect(container.querySelector(".seo-page")).toBeNull();
    for (const section of [".pl-hero", ".pl-discover", ".pl-demo", ".pl-steps", ".pl-faq", ".pl-final"]) expect(container.querySelector(section)).toBeTruthy();
    expect(screen.getAllByRole("heading", { level: 1 })).toHaveLength(1);
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Knowing is good.Speaking is better.");
    expect(screen.getByRole("img", { name: "The purple EnglishAI parrot" }).getAttribute("src")).toBe("/assets/play/parrot.svg");
    expect(document.documentElement.lang).toBe("en");
    expect(screen.getByRole("link", { name: "Learning guides" }).getAttribute("href")).toBe("/en/learn-english");
    expect(screen.getByRole("link", { name: /Download APK/ }).getAttribute("download")).toBe("englishai.apk");
    expect(screen.queryByText("Bepul boshlash")).toBeNull();
  });

  it("translates both demo outcomes", () => {
    renderEnglish();
    fireEvent.click(screen.getByRole("button", { name: "To thank someone" }));
    expect(screen.getByRole("status").textContent).toContain("Try again.");
    fireEvent.click(screen.getByRole("button", { name: "To greet someone" }));
    expect(screen.getByRole("status").textContent).toContain("Exactly right!");
  });

  it("uses the same English FAQ source as its metadata", () => {
    renderEnglish();
    expect(playLanding.en.faqs).toHaveLength(6);
    for (const faq of playLanding.en.faqs) {
      expect(screen.getByText(faq.q)).toBeTruthy();
      expect(screen.getByText(faq.a)).toBeTruthy();
    }
  });

  it("keeps sign-in and authenticated continuation functional", () => {
    const guest = renderEnglish();
    fireEvent.click(screen.getAllByRole("button", { name: "Start for free" })[0]);
    expect(screen.getByText("LOGIN_DESTINATION")).toBeTruthy();
    guest.unmount();
    authStatus = "authenticated";
    renderEnglish();
    fireEvent.click(screen.getByRole("link", { name: "Continue learning" }));
    expect(screen.getByText("HOME_DESTINATION")).toBeTruthy();
  });

  it("switches locale and resets the demo without leaving English metadata behind", () => {
    renderEnglish();
    fireEvent.click(screen.getByRole("button", { name: "To greet someone" }));
    fireEvent.click(screen.getByRole("link", { name: "Switch to Uzbek" }));
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Bilish yaxshi.Gapirish zo‘r.");
    expect(document.documentElement.lang).toBe("uz");
    expect(screen.getByRole("status").textContent).toContain("Javobni tanlab");
  });

  it.each(["/LANDING", "/LANDING/", "/landing", "/landing/"])("switches %s to its English child route and back", (path) => {
    renderEnglish(path);
    const landingPath = path.replace(/\/$/, "");
    const englishPath = `${landingPath}/eng`;
    expect(screen.getByRole("link", { name: "Switch to English" }).getAttribute("href")).toBe(englishPath);
    expect(screen.getByRole("link", { name: "English" }).getAttribute("href")).toBe(englishPath);
    fireEvent.click(screen.getByRole("link", { name: "Switch to English" }));
    expect(screen.getByTestId("current-path").textContent).toBe(englishPath);
    expect(document.documentElement.lang).toBe("en");
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe("Knowing is good.Speaking is better.");
    fireEvent.click(screen.getByRole("button", { name: "To greet someone" }));
    fireEvent.click(screen.getByRole("link", { name: "Switch to Uzbek" }));
    expect(screen.getByTestId("current-path").textContent).toBe(landingPath);
    expect(document.documentElement.lang).toBe("uz");
    expect(screen.getByRole("status").textContent).toContain("Javobni tanlab");
  });

  it("opens the English landing directly and keeps footer language changes in the same route family", () => {
    renderEnglish("/LANDING/eng");
    expect(document.documentElement.lang).toBe("en");
    fireEvent.click(screen.getByRole("link", { name: "O‘zbekcha" }));
    expect(screen.getByTestId("current-path").textContent).toBe("/LANDING");
    fireEvent.click(screen.getByRole("link", { name: "English" }));
    expect(screen.getByTestId("current-path").textContent).toBe("/LANDING/eng");
  });

  it("keeps authenticated language switching on the explicit landing routes", () => {
    authStatus = "authenticated";
    renderEnglish("/LANDING/eng");
    fireEvent.click(screen.getByRole("link", { name: "Switch to Uzbek" }));
    expect(screen.getByTestId("current-path").textContent).toBe("/LANDING");
    expect(screen.getByRole("link", { name: "Davom etish" }).getAttribute("href")).toBe("/home");
  });

  it("preserves canonical root language navigation", () => {
    renderEnglish("/");
    fireEvent.click(screen.getByRole("link", { name: "Switch to English" }));
    expect(screen.getByTestId("current-path").textContent).toBe("/en/");
  });

  it("localizes the assistant UI without displaying unsolicited contact information", () => {
    renderEnglish();
    fireEvent.click(screen.getByRole("button", { name: "Open the EnglishAI assistant" }));
    expect(screen.getByRole("dialog", { name: "AI assistant" })).toBeTruthy();
    expect(screen.getByLabelText("Your question about EnglishAI")).toBeTruthy();
    expect(screen.getByRole("button", { name: "How does the AI tutor work?" })).toBeTruthy();
    expect(screen.getByText(/Hello! I’m here to help/)).toBeTruthy();
    expect(screen.queryByText("AI yordamchi")).toBeNull();
    const dialog = within(screen.getByRole("dialog", { name: "AI assistant" }));
    expect(dialog.queryByText("Official contact channels")).toBeNull();
    expect(dialog.queryByText("Telegram support")).toBeNull();
    expect(dialog.queryByText("EnglishAI.uz LinkedIn")).toBeNull();
    expect(dialog.queryByText("Javohir Sadullayev")).toBeNull();
    expect(dialog.queryAllByRole("link")).toHaveLength(0);
  });

  it.each([
    "Why is speaking harder than studying English?",
    "How does the AI tutor work?",
    "How are the six skills connected?",
    "How is the learning path organised?",
    "What can I practise with Books and Video?",
    "How do I contact support?",
  ])("requests an English answer when the suggestion '%s' is clicked", async (question) => {
    renderEnglish("/LANDING/eng");
    fireEvent.click(screen.getByRole("button", { name: "Open the EnglishAI assistant" }));
    fireEvent.click(screen.getByRole("button", { name: question }));
    await waitFor(() => expect(askProject).toHaveBeenCalledWith(question, expect.any(Array), "en"));
    await screen.findByText("EnglishAI connects your learning skills in one place.");
  });

  it("preserves English for typed questions and retry requests", async () => {
    askProject.mockRejectedValueOnce(new Error("Network unavailable"));
    renderEnglish("/LANDING/eng");
    fireEvent.click(screen.getByRole("button", { name: "Open the EnglishAI assistant" }));
    const question = "EnglishAI qanday yordam beradi?";
    fireEvent.change(screen.getByLabelText("Your question about EnglishAI"), { target: { value: question } });
    fireEvent.click(screen.getByRole("button", { name: "Send question" }));
    await screen.findByRole("button", { name: "Try again" });
    expect(askProject).toHaveBeenLastCalledWith(question, expect.any(Array), "en");
    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    await screen.findByText("EnglishAI connects your learning skills in one place.");
    expect(askProject).toHaveBeenCalledTimes(2);
    expect(askProject).toHaveBeenLastCalledWith(question, expect.any(Array), "en");
  });

  it("does not retain English requests after switching back to Uzbek", async () => {
    renderEnglish("/LANDING/eng");
    fireEvent.click(screen.getByRole("link", { name: "Switch to Uzbek" }));
    fireEvent.click(screen.getByRole("button", { name: "EnglishAI loyiha yordamchisini ochish" }));
    fireEvent.click(screen.getByRole("button", { name: "24/7 AI tutor qanday ishlaydi?" }));
    await waitFor(() => expect(askProject).toHaveBeenCalledWith("24/7 AI tutor qanday ishlaydi?", expect.any(Array), "uz"));
    await screen.findByText("EnglishAI connects your learning skills in one place.");
  });
});
