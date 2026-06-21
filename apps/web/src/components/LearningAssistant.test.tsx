import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LearningAssistant } from "./LearningAssistant";
import { api } from "@/api/client";

class VisualViewportMock extends EventTarget {
  width = 390;
  height = 800;
  offsetLeft = 0;
  offsetTop = 0;
}

const session = {
  id: "session-1",
  skill: "general",
  resourceType: "page",
  resourceId: null,
  title: "EnglishAI",
  createdAt: new Date().toISOString(),
  sources: [],
  updatedAt: new Date().toISOString(),
  expiresAt: new Date(Date.now() + 86400000).toISOString(),
  messages: [],
} as const;

vi.mock("@/api/client", () => ({
  api: {
    assistant: {
      sessions: {
        list: vi.fn(),
        create: vi.fn(),
        get: vi.fn(),
        delete: vi.fn(),
        send: vi.fn(),
        rename: vi.fn(),
      },
    },
  },
  rateLimitDetails: () => null,
}));
vi.mock("@/components/ParrotLogo", () => ({
  ParrotLogo: () => (
    <img data-testid="parrot-logo" draggable={false} alt="EnglishAI" />
  ),
}));

const sessions = api.assistant.sessions;
const visualViewport = new VisualViewportMock();

function pointerEvent(
  type: string,
  init: { pointerId: number; clientX: number; clientY: number }
) {
  const event = new Event(type, { bubbles: true, cancelable: true });
  Object.assign(event, init);
  return event;
}

beforeEach(() => {
  localStorage.clear();
  visualViewport.width = 390;
  visualViewport.height = 800;
  visualViewport.offsetLeft = 0;
  visualViewport.offsetTop = 0;
  Object.defineProperty(window, "innerHeight", {
    configurable: true,
    value: 800,
  });
  Object.defineProperty(window, "innerWidth", {
    configurable: true,
    value: 390,
  });
  Object.defineProperty(window, "visualViewport", {
    configurable: true,
    value: visualViewport,
  });
  vi.mocked(sessions.list).mockResolvedValue({ items: [], nextCursor: null });
  vi.mocked(sessions.create).mockResolvedValue({ ...session, messages: [] });
  vi.mocked(sessions.get).mockResolvedValue({ ...session, messages: [] });
  vi.mocked(sessions.delete).mockResolvedValue(undefined);
  vi.mocked(sessions.send).mockResolvedValue({
    id: "answer-1",
    role: "assistant",
    text: "have/has + V3",
    status: "completed",
    source: "cache",
    clientRequestId: "request-1",
    latencyMs: 10,
    createdAt: new Date().toISOString(),
    sources: [],
  });
});

describe("LearningAssistant context payload", () => {
  it("sends full page context and focus with the question", async () => {
    vi.mocked(sessions.list).mockResolvedValue({
      items: [session],
      nextCursor: null,
    });
    vi.mocked(sessions.get).mockResolvedValue({ ...session, messages: [] });
    vi.mocked(sessions.send).mockResolvedValue({
      id: "answer",
      role: "assistant",
      text: "A financial institution.",
      status: "completed",
      source: "ai",
      clientRequestId: "request",
      latencyMs: 10,
      createdAt: new Date().toISOString(),
      sources: [],
    });
    (
      window as typeof window & { __englishAiAssistantContext?: unknown }
    ).__englishAiAssistantContext = {
      area: "vocabulary",
      resourceId: "banking",
      title: "Banking",
      context: "Passage about opening a bank account.",
      focusText: "bank",
    };

    render(<LearningAssistant />);
    fireEvent.click(
      await screen.findByRole("button", { name: "AI Yordamchi" })
    );
    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "What does bank mean here?" } });
    fireEvent.click(screen.getByRole("button", { name: "Yuborish" }));

    await waitFor(() =>
      expect(sessions.send).toHaveBeenCalledWith(
        session.id,
        "What does bank mean here?",
        "Passage about opening a bank account.",
        "bank",
        expect.any(String),
        expect.any(String),
        undefined,
        expect.any(Function)
      )
    );
  });

  it("renders a clickable lesson source under the answer", async () => {
    vi.mocked(sessions.list).mockResolvedValue({
      items: [session],
      nextCursor: null,
    });
    vi.mocked(sessions.get).mockResolvedValue({ ...session, messages: [] });
    vi.mocked(sessions.send).mockResolvedValue({
      id: "answer-source",
      role: "assistant",
      text: "Mother is a noun.",
      status: "completed",
      source: "ai",
      clientRequestId: "request",
      latencyMs: 10,
      createdAt: new Date().toISOString(),
      sources: [
        {
          area: "vocabulary",
          resourceType: "topic",
          resourceId: "family",
          title: "Family",
          route: "/app/vocabulary/topic/family",
        },
      ],
    });

    render(<LearningAssistant />);
    fireEvent.click(
      await screen.findByRole("button", { name: "AI Yordamchi" })
    );
    const input = await screen.findByRole("textbox");
    fireEvent.change(input, { target: { value: "mother" } });
    fireEvent.click(screen.getByRole("button", { name: "Yuborish" }));

    const source = await screen.findByRole("link", { name: "Manba: Family" });
    expect(source.getAttribute("href")).toBe("/app/vocabulary/topic/family");
  });
});

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("LearningAssistant sessions", () => {
  function prepareDraggableTrigger() {
    render(<LearningAssistant />);
    return screen.getByRole("button", {
      name: "AI Yordamchi",
    }) as HTMLButtonElement;
  }

  function addMobileNav(top: number) {
    const mobileNav = document.createElement("nav");
    mobileNav.className = "play-mobile-nav";
    mobileNav.setAttribute("aria-label", "Mobil navigatsiya");
    document.body.appendChild(mobileNav);
    vi.spyOn(mobileNav, "getBoundingClientRect").mockReturnValue({
      x: 0,
      y: top,
      left: 0,
      top,
      right: 390,
      bottom: 800,
      width: 390,
      height: 800 - top,
      toJSON: () => ({}),
    });
    return mobileNav;
  }

  function addSidebar(right: number) {
    const sidebar = document.createElement("aside");
    sidebar.className = "play-sidebar";
    document.body.appendChild(sidebar);
    vi.spyOn(sidebar, "getBoundingClientRect").mockReturnValue({
      x: 0,
      y: 0,
      left: 0,
      top: 0,
      right,
      bottom: 800,
      width: right,
      height: 800,
      toJSON: () => ({}),
    });
    return sidebar;
  }

  it("renders one icon-only launcher outside the page layout", () => {
    const trigger = prepareDraggableTrigger();
    expect(
      screen.getAllByRole("button", { name: "AI Yordamchi" })
    ).toHaveLength(1);
    expect(trigger.textContent).toBe("");
    expect(trigger.getAttribute("aria-haspopup")).toBe("dialog");
    expect(trigger.parentElement).toBe(document.body);
  });

  it("does not reposition the fixed launcher when dragged over a sidebar", () => {
    visualViewport.width = 1440;
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: 1440,
    });
    const sidebar = addSidebar(248);
    const trigger = prepareDraggableTrigger();
    fireEvent(
      trigger,
      pointerEvent("pointerdown", { pointerId: 4, clientX: 348, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointermove", { pointerId: 4, clientX: 0, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointerup", { pointerId: 4, clientX: 0, clientY: 678 })
    );
    expect(trigger.style.left).toBe("");
    expect(trigger.style.top).toBe("");
    sidebar.remove();
  });

  it("does not offset the launcher for a collapsed sidebar", () => {
    visualViewport.width = 1440;
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: 1440,
    });
    const sidebar = addSidebar(88);
    const trigger = prepareDraggableTrigger();
    fireEvent(
      trigger,
      pointerEvent("pointerdown", { pointerId: 5, clientX: 348, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointermove", { pointerId: 5, clientX: 0, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointerup", { pointerId: 5, clientX: 0, clientY: 678 })
    );
    expect(trigger.style.left).toBe("");
    sidebar.remove();
  });

  it("reserves space above the visible mobile navigation", () => {
    const mobileNav = addMobileNav(720);
    const trigger = prepareDraggableTrigger();
    fireEvent(
      trigger,
      pointerEvent("pointerdown", { pointerId: 1, clientX: 348, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointermove", { pointerId: 1, clientX: 348, clientY: 900 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointerup", { pointerId: 1, clientX: 348, clientY: 900 })
    );
    expect(trigger.style.getPropertyValue("--ea-assistant-bottom-inset")).toBe(
      "80px"
    );
    mobileNav.remove();
  });

  it("uses the visible viewport bottom when mobile navigation is hidden", () => {
    const mobileNav = addMobileNav(720);
    mobileNav.style.display = "none";
    const trigger = prepareDraggableTrigger();
    fireEvent(
      trigger,
      pointerEvent("pointerdown", { pointerId: 2, clientX: 348, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointermove", { pointerId: 2, clientX: 348, clientY: 900 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointerup", { pointerId: 2, clientX: 348, clientY: 900 })
    );
    expect(trigger.style.getPropertyValue("--ea-assistant-bottom-inset")).toBe(
      "0px"
    );
    mobileNav.remove();
  });

  it("keeps the fixed launcher above the keyboard when the visible viewport shrinks", async () => {
    const trigger = prepareDraggableTrigger();
    fireEvent(
      trigger,
      pointerEvent("pointerdown", { pointerId: 3, clientX: 348, clientY: 678 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointermove", { pointerId: 3, clientX: 348, clientY: 760 })
    );
    fireEvent(
      trigger,
      pointerEvent("pointerup", { pointerId: 3, clientX: 348, clientY: 760 })
    );
    expect(trigger.style.getPropertyValue("--ea-assistant-bottom-inset")).toBe(
      "0px"
    );
    visualViewport.height = 620;
    visualViewport.dispatchEvent(new Event("resize"));
    await waitFor(() =>
      expect(
        trigger.style.getPropertyValue("--ea-assistant-bottom-inset")
      ).toBe("180px")
    );
  });

  it("restores or creates an active session", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    await waitFor(() => expect(sessions.create).toHaveBeenCalled());
    expect(screen.getByText("Salom, Javohir!")).toBeTruthy();
    expect(screen.getByText("My mother is very kind.")).toBeTruthy();
  });

  it("opens the compact Pen modal without unreferenced header controls", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const dialog = await screen.findByRole("dialog", { name: "AI Yordamchi" });

    await waitFor(() =>
      expect(
        dialog.style.getPropertyValue("--ea-assistant-mobile-height")
      ).toBe("728px")
    );
    expect(dialog.classList.contains("is-responsive-fullscreen")).toBe(false);
    expect(
      screen.queryByRole("separator", { name: /balandligini/i })
    ).toBeNull();
    expect(screen.queryByRole("button", { name: "Full size" })).toBeNull();
    expect(screen.queryByTitle("Yangi chat")).toBeNull();
    expect(screen.queryByTitle("Chatlar tarixi")).toBeNull();
  });

  it("keeps close available without the mobile drag handle", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    await screen.findByRole("dialog", { name: "AI Yordamchi" });
    expect(
      screen.queryByRole("separator", { name: /balandligini/i })
    ).toBeNull();
    expect(screen.getByRole("button", { name: "Yopish" })).toBeTruthy();
  });

  it("shows thinking before the reply and no empty bubble", async () => {
    let resolve!: (value: Awaited<ReturnType<typeof sessions.send>>) => void;
    vi.mocked(sessions.send).mockImplementation(
      () =>
        new Promise((done) => {
          resolve = done;
        })
    );
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    );
    fireEvent.change(input, { target: { value: "Present Perfect" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(await screen.findByText("Present Perfect")).toBeTruthy();
    expect(document.querySelector(".ea-assistant-thinking")).toBeTruthy();
    expect(
      document.querySelector(".ea-assistant-message__bubble:empty")
    ).toBeNull();
    resolve({
      id: "answer",
      role: "assistant",
      text: "have/has + V3",
      status: "completed",
      source: "local",
      clientRequestId: "request",
      latencyMs: 20,
      createdAt: new Date().toISOString(),
      sources: [],
    });
    expect(await screen.findByText("have/has + V3")).toBeTruthy();
  });

  it("restores the persisted answer when the stream returns an empty message", async () => {
    vi.spyOn(crypto, "randomUUID").mockReturnValue(
      "00000000-0000-4000-8000-000000000001"
    );
    const persistedReply = {
      id: "answer",
      role: "assistant",
      text: "Persisted answer",
      status: "completed",
      source: "local",
      clientRequestId: "00000000-0000-4000-8000-000000000001",
      latencyMs: 20,
      createdAt: new Date().toISOString(),
      sources: [],
    } as const;
    vi.mocked(sessions.send).mockResolvedValue({ ...persistedReply, text: "" });
    vi.mocked(sessions.get).mockResolvedValue({
      ...session,
      messages: [persistedReply],
    });
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    );
    fireEvent.change(input, { target: { value: "Explain" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(await screen.findByText("Persisted answer")).toBeTruthy();
  });

  it("sends a suggested Pen prompt through the active session", async () => {
    vi.mocked(sessions.send).mockResolvedValue({
      id: "answer",
      role: "assistant",
      text: "Yana bir misol.",
      status: "completed",
      source: "local",
      clientRequestId: "request",
      latencyMs: 20,
      createdAt: new Date().toISOString(),
      sources: [],
    });
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    fireEvent.click(
      await screen.findByRole("button", { name: "Misollar keltir" })
    );
    await waitFor(() => expect(sessions.send).toHaveBeenCalled());
    expect(await screen.findByText("Yana bir misol.")).toBeTruthy();
  });

  it("keeps the dialog as a centered compact modal on desktop", async () => {
    Object.defineProperty(window, "innerWidth", {
      configurable: true,
      value: 1280,
    });
    visualViewport.width = 1280;
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const panel = document.querySelector<HTMLElement>(".ea-assistant-panel");
    expect(panel).toBeTruthy();
    const overlay = document.querySelector<HTMLElement>(
      ".ea-assistant-overlay"
    );
    expect(overlay).toBeTruthy();
    expect(overlay?.parentElement).toBe(document.body);
    expect(panel?.classList.contains("is-full-width")).toBe(false);
    expect(screen.queryByRole("button", { name: "Full size" })).toBeNull();
    expect(
      overlay?.style.getPropertyValue("--ea-assistant-viewport-height")
    ).toBe("800px");
  });

  it("fits the panel to the visible viewport when the mobile keyboard opens", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const panel = document.querySelector<HTMLElement>(".ea-assistant-panel");
    expect(panel).toBeTruthy();
    visualViewport.height = 460;
    visualViewport.dispatchEvent(new Event("resize"));
    await waitFor(() =>
      expect(panel?.classList.contains("is-keyboard-open")).toBe(true)
    );
    expect(
      panel?.style.getPropertyValue("--ea-assistant-viewport-height")
    ).toBe("460px");
    expect(panel?.style.getPropertyValue("--ea-assistant-mobile-height")).toBe(
      "460px"
    );
    expect(panel?.classList.contains("is-compact-viewport")).toBe(true);
  });

  it("keeps only the Pen close action in the dialog header", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const panel = document.querySelector<HTMLElement>(".ea-assistant-panel");
    expect(panel?.classList.contains("is-responsive-fullscreen")).toBe(false);
    expect(
      panel?.style.getPropertyValue("--ea-assistant-viewport-height")
    ).toBe("800px");
    expect(
      screen.queryByRole("separator", {
        name: "Yordamchi oynasi balandligini o'zgartirish",
      })
    ).toBeNull();
    expect(screen.getByRole("button", { name: "Yopish" })).toBeTruthy();
    expect(screen.queryByTitle("Yangi chat")).toBeNull();
    expect(screen.queryByTitle("Chatlar tarixi")).toBeNull();
  });

  it("marks the document while the assistant is open so mobile navigation can hide", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    expect(document.documentElement.dataset.assistantOpen).toBe("true");
    fireEvent.click(await screen.findByRole("button", { name: "Yopish" }));
    await waitFor(() =>
      expect(document.documentElement.dataset.assistantOpen).toBeUndefined()
    );
  });

  it("keeps pointer interaction on the modal overlay while open", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));

    const overlay = document.querySelector<HTMLElement>(
      ".ea-assistant-overlay"
    );
    const backdrop = overlay?.querySelector<HTMLElement>(
      ".ea-overlay__backdrop"
    );

    expect(document.documentElement.dataset.assistantOpen).toBe("true");
    expect(overlay).toBeTruthy();
    expect(backdrop).toBeTruthy();
  });

  it("limits textarea growth while the mobile keyboard is open", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = (await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    )) as HTMLTextAreaElement;
    Object.defineProperty(input, "scrollHeight", {
      configurable: true,
      value: 150,
    });
    visualViewport.height = 460;
    visualViewport.dispatchEvent(new Event("resize"));
    await waitFor(() =>
      expect(
        document.querySelector(".ea-assistant-panel.is-keyboard-open")
      ).toBeTruthy()
    );
    fireEvent.change(input, {
      target: {
        value:
          "A long mobile message\nwith several lines\nwhile the keyboard is open",
      },
    });
    expect(input.style.height).toBe("96px");
    expect(input.style.overflowY).toBe("auto");
  });

  it("restores the regular panel state when the keyboard closes", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const panel = document.querySelector<HTMLElement>(".ea-assistant-panel");
    visualViewport.height = 460;
    visualViewport.dispatchEvent(new Event("resize"));
    await waitFor(() =>
      expect(panel?.classList.contains("is-keyboard-open")).toBe(true)
    );
    visualViewport.height = 800;
    visualViewport.dispatchEvent(new Event("resize"));
    await waitFor(() =>
      expect(panel?.classList.contains("is-keyboard-open")).toBe(false)
    );
    expect(panel?.classList.contains("is-responsive-fullscreen")).toBe(false);
    expect(
      panel?.style.getPropertyValue("--ea-assistant-viewport-height")
    ).toBe("800px");
    await waitFor(() =>
      expect(
        panel?.style.getPropertyValue("--ea-assistant-mobile-height")
      ).toBe("728px")
    );
  });

  it("keeps the sent question out of the input when network and recovery fail", async () => {
    vi.mocked(sessions.send).mockRejectedValue(new Error("offline"));
    vi.mocked(sessions.get).mockRejectedValue(new Error("offline"));
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    );
    fireEvent.change(input, { target: { value: "Explain this" } });
    fireEvent.keyDown(input, { key: "Enter" });
    await waitFor(() =>
      expect(screen.getByRole("alert").textContent).toContain(
        "Savolingiz chatda saqlandi"
      )
    );
    expect((input as HTMLTextAreaElement).value).toBe("");
    expect(screen.getByText("Explain this")).toBeTruthy();
  });

  it("shows a persisted answer immediately when the stream fails after saving it", async () => {
    vi.spyOn(crypto, "randomUUID").mockReturnValue(
      "00000000-0000-4000-8000-000000000002"
    );
    const persistedReply = {
      id: "answer",
      role: "assistant",
      text: "Immediate persisted answer",
      status: "completed",
      source: "cache",
      clientRequestId: "00000000-0000-4000-8000-000000000002",
      latencyMs: 20,
      createdAt: new Date().toISOString(),
      sources: [],
    } as const;
    vi.mocked(sessions.send).mockRejectedValue(new Error("stream closed late"));
    vi.mocked(sessions.get).mockResolvedValue({
      ...session,
      messages: [persistedReply],
    });
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    );
    fireEvent.change(input, { target: { value: "Present Perfect" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(await screen.findByText("Immediate persisted answer")).toBeTruthy();
    expect(screen.queryByRole("alert")).toBeNull();
  });

  it("grows the composer textarea with its content", async () => {
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = (await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    )) as HTMLTextAreaElement;
    Object.defineProperty(input, "scrollHeight", {
      configurable: true,
      value: 116,
    });
    fireEvent.change(input, {
      target: { value: "First line\nSecond line\nThird line" },
    });
    expect(input.style.height).toBe("116px");
    expect(input.style.overflowY).toBe("hidden");
  });

  it("does not render an empty assistant bubble when recovery has no reply", async () => {
    vi.mocked(sessions.send).mockResolvedValue({
      id: "answer",
      role: "assistant",
      text: "",
      status: "completed",
      source: "local",
      clientRequestId: "request",
      latencyMs: 20,
      createdAt: new Date().toISOString(),
      sources: [],
    });
    vi.mocked(sessions.get).mockResolvedValue({ ...session, messages: [] });
    render(<LearningAssistant />);
    fireEvent.click(screen.getByRole("button", { name: "AI Yordamchi" }));
    const input = await screen.findByPlaceholderText(
      "Ingliz tili bo'yicha savolingiz..."
    );
    fireEvent.change(input, { target: { value: "Present Perfect" } });
    fireEvent.keyDown(input, { key: "Enter" });
    await waitFor(() => expect(screen.getByRole("alert")).toBeTruthy());
    expect(
      document.querySelector(".ea-assistant-message__bubble:empty")
    ).toBeNull();
    expect((input as HTMLTextAreaElement).value).toBe("");
  });
});
