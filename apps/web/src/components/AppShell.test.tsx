import { cleanup, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppShell } from "./AppShell";

vi.mock("@/lib/useStudyTimer", () => ({ useStudyTimer: vi.fn() }));
vi.mock("@/lib/usePageAnimations", () => ({ usePageAnimations: vi.fn() }));
vi.mock("@/lib/push", () => ({ initPushNotifications: vi.fn() }));
vi.mock("@/api/notificationsHub", () => ({
  NotificationsHubClient: class {
    start() { return Promise.resolve(); }
    stop() { return Promise.resolve(); }
  },
}));
vi.mock("./PaywallProvider", () => ({ PaywallProvider: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("@/components/game/HeartsProvider", () => ({ HeartsProvider: ({ children }: { children: React.ReactNode }) => children }));
vi.mock("@/components/game/EnergyProvider", () => ({
  EnergyProvider: ({ children }: { children: React.ReactNode }) => children,
  useEnergy: () => ({ energy: { current: 5, maximum: 5 } }),
}));
vi.mock("@/pages/home-concepts/HomeConceptLab", () => ({
  SportSideNav: ({ homeDesign }: { homeDesign?: boolean }) => <div data-testid="home-side-nav" data-home-design={String(Boolean(homeDesign))} />,
  SportTopBar: ({ homeDesign }: { homeDesign?: boolean }) => <div data-testid="home-top-bar" data-home-design={String(Boolean(homeDesign))} />,
  CatalogTopBar: () => <div data-testid="catalog-top-bar" />,
  MobileNav: ({ homeDesign }: { homeDesign?: boolean }) => <div data-testid="home-mobile-nav" data-home-design={String(Boolean(homeDesign))} />,
}));
vi.mock("./LearningAssistant", () => ({ LearningAssistant: () => <div data-testid="learning-assistant" /> }));
vi.mock("./NotificationModal", () => ({ NotificationModal: () => null }));
vi.mock("@/components/grammar/GrammarChrome", () => ({
  GrammarChrome: ({ children, catalog }: { children: React.ReactNode; catalog: boolean }) => (
    <div data-testid="grammar-chrome" data-catalog={catalog}>{children}</div>
  ),
}));
vi.mock("@/components/listening/ListeningChrome", () => ({
  ListeningChrome: ({ children, catalog }: { children: React.ReactNode; catalog: boolean }) => (
    <div data-testid="listening-chrome" data-catalog={catalog}>{children}</div>
  ),
}));

afterEach(cleanup);

vi.mock("@/components/reading/ReadingChrome", () => ({
  ReadingChrome: ({ children, catalog }: { children: React.ReactNode; catalog: boolean }) => (
    <div data-testid="reading-chrome" data-catalog={catalog}>{children}</div>
  ),
}));
vi.mock("@/components/speaking/SpeakingChrome", () => ({
  SpeakingChrome: ({ children, catalog }: { children: React.ReactNode; catalog: boolean }) => (
    <div data-testid="speaking-chrome" data-catalog={catalog}>{children}</div>
  ),
}));

describe("AppShell responsive chrome", () => {
  it.each(["/reading", "/reading/topic/topic-1"])("gives %s the Pen Reading chrome without a duplicate header", (path) => {
    render(<MemoryRouter initialEntries={[path]}><AppShell><div>Reading page</div></AppShell></MemoryRouter>);
    expect(screen.getByTestId("reading-chrome").getAttribute("data-catalog")).toBe(String(path === "/reading"));
    expect(screen.queryByTestId("catalog-top-bar")).toBeNull();
    expect(document.querySelector(".play-lesson-brand")).toBeNull();
    expect(screen.getByRole("main").className).not.toContain("play-lesson-page");
  });
  it.each(["/app/grammar", "/app/grammar/topic/topic-1"])("gives %s exactly one Pen header", (path) => {
    render(<MemoryRouter initialEntries={[path]}><AppShell><div>Grammar page</div></AppShell></MemoryRouter>);
    expect(screen.getByTestId("grammar-chrome").getAttribute("data-catalog")).toBe(String(path === "/app/grammar"));
    expect(screen.queryByTestId("catalog-top-bar")).toBeNull();
    expect(document.querySelector(".play-lesson-brand")).toBeNull();
    expect(screen.getByRole("main").className).not.toContain("play-lesson-page");
    expect(screen.queryByTestId("home-mobile-nav") !== null).toBe(path === "/app/grammar");
  });
  it.each(["/listening", "/listening/topic/topic-1"])("gives %s a single listening-owned viewport and header", (path) => {
    render(<MemoryRouter initialEntries={[path]}><AppShell><div>Listening page</div></AppShell></MemoryRouter>);
    expect(screen.getByTestId("listening-chrome").getAttribute("data-catalog")).toBe(String(path === "/listening"));
    expect(screen.queryByTestId("catalog-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(document.querySelector(".play-lesson-brand")).toBeNull();
    expect(screen.getByRole("main").className).not.toContain("play-lesson-page");
    expect(screen.queryByTestId("home-mobile-nav") !== null).toBe(path === "/listening");
  });
  it.each(["/app/speaking", "/app/speaking/topics"])("gives %s the Pen Speaking chrome without dashboard navigation", (path) => {
    render(<MemoryRouter initialEntries={[path]}><AppShell><div>Speaking page</div></AppShell></MemoryRouter>);
    expect(screen.getByTestId("speaking-chrome").getAttribute("data-catalog")).toBe(String(path === "/app/speaking/topics"));
    expect(screen.queryByTestId("catalog-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
  });
  it.each(["/home", "/levels", "/progress", "/leaderboard", "/profile", "/app/vocabulary/saved", "/support", "/reading", "/listening", "/writing", "/video"])("mounts exactly one global assistant on %s", (path) => {
    render(<MemoryRouter initialEntries={[path]}><AppShell><div>page</div></AppShell></MemoryRouter>);
    expect(screen.getAllByTestId("learning-assistant")).toHaveLength(1);
  });

  it("does not mount the learner assistant on admin routes", () => {
    render(<MemoryRouter initialEntries={["/admin"]}><AppShell><div>admin</div></AppShell></MemoryRouter>);
    expect(screen.queryByTestId("learning-assistant")).toBeNull();
  });

  it.each(["/video", "/video/search", "/video/playlist/PL-puss", "/video/playlists/PL-puss"])("lets %s own its full-width Video header and mobile navigation", (path) => {
    render(
      <MemoryRouter initialEntries={[path]}>
        <AppShell homeChromeModel={{} as never}><div>Video catalog</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.queryByTestId("catalog-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
    expect(screen.getByRole("main").className).toContain("play-video-catalog-workspace");
    expect(screen.getByTestId("app-shell-page").className).toContain("max-w-none");
    expect(screen.getAllByTestId("learning-assistant")).toHaveLength(1);
  });

  it("retains the existing catalog chrome outside the video route", () => {
    render(
      <MemoryRouter initialEntries={["/books"]}>
        <AppShell homeChromeModel={{} as never}><div>Books catalog</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("catalog-top-bar")).toBeTruthy();
    expect(screen.getByTestId("home-mobile-nav")).toBeTruthy();
    expect(screen.getByTestId("app-shell-page").className).toContain("max-w-[1180px]");
    expect(screen.getAllByTestId("learning-assistant")).toHaveLength(1);
  });
  it.each(["/levels", "/progress", "/leaderboard", "/profile", "/app/vocabulary/saved"])("reuses the home navigation contract on %s", (path) => {
    render(
      <MemoryRouter initialEntries={[path]}>
        <AppShell homeChromeModel={{} as never}><div>page</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("home-side-nav")).toBeTruthy();
    expect(screen.getByTestId("home-top-bar")).toBeTruthy();
    expect(screen.getByTestId("home-mobile-nav")).toBeTruthy();
    expect(screen.getByTestId("home-side-nav").getAttribute("data-home-design")).toBe("true");
    expect(screen.getByTestId("home-top-bar").getAttribute("data-home-design")).toBe("true");
    expect(screen.getByTestId("home-mobile-nav").getAttribute("data-home-design")).toBe("true");
    expect(screen.getByTestId("app-shell-layout").className).not.toContain("nh-06");
    expect(screen.getByTestId("app-shell-layout").className).toContain("play-home-shared-shell");
    expect(screen.getByRole("main").className).toContain("play-workspace");
    expect(screen.getByRole("main").className).toContain("play-home-shared-workspace");
    expect(screen.getByTestId("app-shell-page").className).toContain("w-full");
    // Profile screen 81 uses the entire workspace after its 224px sidebar;
    // the other shared learner routes retain the standard 1180px reading cap.
    expect(screen.getByTestId("app-shell-page").className).toContain(path === "/profile" ? "max-w-none" : "max-w-[1180px]");
    // The shell owns the gutter via `.nh-sport`; a second one here pushed page
    // content 32px inside the top bar's left edge.
    expect(screen.getByTestId("app-shell-page").className).not.toContain("px-page-gutter");
  });

  it("lets a home design concept own the full viewport chrome", () => {
    render(
      <MemoryRouter initialEntries={["/home"]}>
        <AppShell conceptFrame><div>CONCEPT</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
    expect(screen.getByText("CONCEPT")).toBeTruthy();
  });

  it("hides the global top bar on speaking topic sessions", () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/topic/my-family"]}>
        <AppShell homeChromeModel={{} as never}><div>speaking topic</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("home-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
    expect(screen.getByText("speaking topic")).toBeTruthy();
  });

  it("hides the top and bottom bars on free-talk sessions", () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/free-talk/my_family"]}>
        <AppShell homeChromeModel={{} as never}><div>free talk</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("home-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
    expect(screen.getByText("free talk")).toBeTruthy();
  });

  it("hides the top and bottom bars on a free speaking session", () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/free"]}>
        <AppShell homeChromeModel={{} as never}><div>free speaking</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("home-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
    expect(screen.getByText("free speaking")).toBeTruthy();
  });

  it("keeps the bottom bar on speaking catalog pages", () => {
    render(
      <MemoryRouter initialEntries={["/app/speaking/free-talk"]}>
        <AppShell homeChromeModel={{} as never}><div>free talk catalog</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("home-mobile-nav")).toBeTruthy();
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
    expect(screen.getByText("free talk catalog")).toBeTruthy();
  });

  it("keeps the sidebar and assistant but hides the top bar on support", () => {
    render(
      <MemoryRouter initialEntries={["/support"]}>
        <AppShell homeChromeModel={{} as never}><div>support chat</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("home-side-nav")).toBeTruthy();
    expect(screen.queryByTestId("home-top-bar")).toBeNull();
    expect(screen.getAllByTestId("learning-assistant")).toHaveLength(1);
    expect(screen.getByText("support chat")).toBeTruthy();
  });

  it("removes every global chrome surface in immersive mode", () => {
    render(
      <MemoryRouter initialEntries={["/writing/task/travel"]}>
        <AppShell lessonFrame><div>lesson content</div></AppShell>
      </MemoryRouter>,
    );

    expect(screen.queryByTestId("home-side-nav")).toBeNull();
    expect(screen.queryByTestId("home-top-bar")).toBeNull();
    expect(screen.queryByTestId("home-mobile-nav")).toBeNull();
    expect(screen.getByText("lesson content")).toBeTruthy();
    expect(screen.getByRole("main").className).toContain("min-h-dvh");
  });

  it("keeps the assistant on immersive writing routes", () => {
    render(
      <MemoryRouter initialEntries={["/writing/topic/travel"]}>
        <AppShell lessonFrame><div>writing content</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
  });

  it("keeps the assistant on other immersive learner routes", () => {
    render(
      <MemoryRouter initialEntries={["/app/grammar/topic/present-simple"]}>
        <AppShell lessonFrame><div>grammar content</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("learning-assistant")).toBeTruthy();
  });

  it("uses the video player contextual AI without a second floating assistant", () => {
    render(
      <MemoryRouter initialEntries={["/video/lesson-id/play"]}>
        <AppShell lessonFrame><div>video content</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.queryByTestId("learning-assistant")).toBeNull();
  });

  it("uses the playlist episode's contextual AI instead of a second global floating trigger", () => {
    render(
      <MemoryRouter initialEntries={["/video/playlists/PL-puss/01"]}>
        <AppShell lessonFrame><div>playlist episode</div></AppShell>
      </MemoryRouter>,
    );
    expect(screen.queryByTestId("learning-assistant")).toBeNull();
  });
});
