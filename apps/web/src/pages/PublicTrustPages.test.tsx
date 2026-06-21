import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getPublicPage } from "@/content/publicContent";
import { officialContacts } from "@/content/officialContacts";
import { PublicContentPage } from "./PublicContentPage";

const { send, copyText } = vi.hoisted(() => ({ send: vi.fn(), copyText: vi.fn() }));
let authStatus = "unauthenticated";
vi.mock("@/app/auth", () => ({ useAuth: () => ({ status: authStatus }) }));
vi.mock("@/api/client", () => ({ api: { support: { send } } }));

beforeEach(() => {
  authStatus = "unauthenticated";
  send.mockReset().mockResolvedValue({ id: "test-message" });
  copyText.mockReset().mockResolvedValue(undefined);
  Object.defineProperty(navigator, "clipboard", { configurable: true, value: { writeText: copyText } });
  vi.spyOn(window, "scrollTo").mockImplementation(() => {});
});

afterEach(() => vi.restoreAllMocks());

function renderPage(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <Routes>
        <Route path="/support" element={<div>SUPPORT_DESTINATION</div>} />
        <Route path="*" element={<PublicContentPage />} />
      </Routes>
    </MemoryRouter>,
  );
}

function fillMessage() {
  fireEvent.change(screen.getByLabelText(/^Mavzu/), { target: { value: "Mashq natijasi" } });
  fireEvent.change(screen.getByLabelText(/^Xabaringiz/), { target: { value: "Natija saqlanmadi. Grammar mashqini tugatgandan keyin xato paydo bo‘ldi." } });
}

describe("EnglishAI public page redesign", () => {
  it.each([
    ["/editorial-policy", "Ishonchli bilim.Ochiq tamoyillar."],
    ["/methodology", "Yodlashdanqo‘llashgacha."],
    ["/contact", "Savolingiz bormi?Biz shu yerdamiz."],
  ])("renders %s with the shared brand chrome and one main heading", (path, title) => {
    const { container } = renderPage(path);
    expect(screen.getAllByRole("heading", { level: 1 })).toHaveLength(1);
    expect(screen.getByRole("heading", { level: 1 }).textContent).toBe(title);
    expect(container.querySelector(".pt-page")).toBeTruthy();
    expect(screen.getByRole("link", { name: "EnglishAI bosh sahifasi" }).querySelector("img")?.getAttribute("src")).toBe("/assets/brand/dialog.svg");
    expect(screen.getByRole("link", { name: "Switch to English" }).getAttribute("href")).toBe("/en/");
    expect(screen.getByRole("main")).toBeTruthy();
    expect(screen.getByRole("contentinfo")).toBeTruthy();
    expect(document.documentElement.lang).toBe("uz");
  });

  it("preserves every editorial paragraph, source, FAQ and the actual review date", () => {
    const page = getPublicPage("/editorial-policy")!;
    const { container } = renderPage(page.path);
    for (const section of page.sections) {
      expect(screen.getByRole("heading", { name: section.heading })).toBeTruthy();
      for (const paragraph of section.paragraphs) expect(screen.getByText(paragraph)).toBeTruthy();
    }
    for (const faq of page.faq) expect(screen.getByText(faq.question)).toBeTruthy();
    for (const source of page.sources) expect(screen.getByRole("link", { name: new RegExp(source.label.split(" — ")[0]) }).getAttribute("href")).toBe(source.url);
    expect(container.querySelector("time")?.getAttribute("dateTime")).toBe(page.reviewedAt);
    expect(container.querySelector("time")?.textContent).toContain("6-avgust, 2026");
    expect(screen.getByRole("link", { name: "Tuzatish yuborish" }).getAttribute("href")).toBe("/contact?topic=content#form");
  });

  it("gives every editorial chapter link a real anchor target", () => {
    const { container } = renderPage("/editorial-policy");
    const navigation = screen.getByRole("navigation", { name: "Sahifa bo‘limlari" });
    for (const link of within(navigation).getAllByRole("link")) {
      const target = link.getAttribute("href")!;
      expect(container.querySelector(target)).toBeTruthy();
    }
  });

  it("keeps methodology content and makes all six skill examples selectable", () => {
    const page = getPublicPage("/methodology")!;
    renderPage(page.path);
    for (const section of page.sections) {
      for (const paragraph of section.paragraphs) expect(screen.getByText(paragraph)).toBeTruthy();
    }
    for (const label of ["Vocabulary", "Grammar", "Reading", "Writing", "Speaking", "Listening"]) {
      const button = screen.getByRole("button", { name: label });
      fireEvent.click(button);
      expect(button.getAttribute("aria-pressed")).toBe("true");
      expect(screen.getAllByRole("button", { pressed: true })).toHaveLength(1);
    }
    expect(screen.getByText("The next train to London leaves at nine.")).toBeTruthy();
    expect(screen.getAllByRole("link", { name: "O‘quv yo‘lini boshlash" }).every((link) => link.getAttribute("href") === page.cta.href)).toBe(true);
    expect(screen.getByText("21")).toBeTruthy();
  });

  it("leaves unrelated public guides on their existing layout", () => {
    const { container } = renderPage("/learn/soz-yodlash");
    expect(container.querySelector(".pc-page")).toBeTruthy();
    expect(container.querySelector(".pt-page")).toBeNull();
  });
});

describe("Contact page delivery states", () => {
  it("links the actual official channels and the existing support route", () => {
    renderPage("/contact");
    expect(screen.getByRole("link", { name: /Rasmiy Telegram/ }).getAttribute("href")).toBe(officialContacts.telegram);
    expect(screen.getByRole("link", { name: /Hamkorlik uchun/ }).getAttribute("href")).toBe(officialContacts.companyLinkedIn);
    fireEvent.click(screen.getByRole("link", { name: /Ilova ichidagi yordam/ }));
    expect(screen.getByText("SUPPORT_DESTINATION")).toBeTruthy();
  });

  it("prefills content corrections without accepting arbitrary topic values", () => {
    const first = renderPage("/contact?topic=content#form");
    expect((screen.getByLabelText("Murojaat turi") as HTMLSelectElement).value).toBe("content");
    first.unmount();
    renderPage("/contact?topic=unknown");
    expect((screen.getByLabelText("Murojaat turi") as HTMLSelectElement).value).toBe("product");
  });

  it("copies a guest draft without pretending to send it", async () => {
    renderPage("/contact");
    fillMessage();
    fireEvent.click(screen.getByRole("button", { name: "Xabar matnini nusxalash" }));
    await waitFor(() => expect(copyText).toHaveBeenCalledWith(expect.stringContaining("[Texnik yordam] Mashq natijasi")));
    expect(send).not.toHaveBeenCalled();
    expect(screen.getByRole("status").textContent).toContain("Hali yuborilmadi");
    expect(screen.queryByText("Xabaringiz yuborildi.")).toBeNull();
  });

  it("provides a manual fallback when clipboard access fails", async () => {
    copyText.mockRejectedValueOnce(new Error("Clipboard denied"));
    renderPage("/contact");
    fillMessage();
    fireEvent.click(screen.getByRole("button", { name: "Xabar matnini nusxalash" }));
    expect(await screen.findByRole("alert")).toHaveProperty("textContent", expect.stringContaining("Matnni qo‘lda nusxalang"));
    expect(send).not.toHaveBeenCalled();
  });

  it("uses the real support API for signed-in users and only confirms after success", async () => {
    authStatus = "authenticated";
    let resolveSend!: (value: unknown) => void;
    send.mockReturnValue(new Promise((resolve) => { resolveSend = resolve; }));
    renderPage("/contact?topic=content");
    fillMessage();
    fireEvent.click(screen.getByRole("button", { name: "Murojaatni yuborish" }));
    expect(send).toHaveBeenCalledWith(expect.stringContaining("[Kontentdagi xato] Mashq natijasi"), []);
    expect(screen.queryByText("Xabaringiz yuborildi.")).toBeNull();
    expect((screen.getByRole("button", { name: /Yuborilmoqda/ }) as HTMLButtonElement).disabled).toBe(true);
    resolveSend({ id: "test-message" });
    expect(await screen.findByRole("heading", { name: "Xabaringiz yuborildi." })).toBeTruthy();
    expect(screen.getByRole("link", { name: "Yordam chatini ochish" }).getAttribute("href")).toBe("/support");
    fireEvent.click(screen.getByRole("button", { name: "Yangi murojaat" }));
    expect((screen.getByLabelText(/^Mavzu/) as HTMLInputElement).value).toBe("");
  });

  it("keeps a failed message available for retry", async () => {
    authStatus = "authenticated";
    send.mockRejectedValueOnce(new Error("Unavailable"));
    renderPage("/contact");
    fillMessage();
    fireEvent.click(screen.getByRole("button", { name: "Murojaatni yuborish" }));
    expect(await screen.findByRole("alert")).toHaveProperty("textContent", expect.stringContaining("Xabar yuborilmadi"));
    expect((screen.getByLabelText(/^Mavzu/) as HTMLInputElement).value).toBe("Mashq natijasi");
    expect(screen.queryByText("Xabaringiz yuborildi.")).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Murojaatni yuborish" }));
    expect(await screen.findByRole("heading", { name: "Xabaringiz yuborildi." })).toBeTruthy();
  });

  it("rejects whitespace-only messages and keeps field limits", () => {
    authStatus = "authenticated";
    const { container } = renderPage("/contact");
    fireEvent.change(screen.getByLabelText(/^Mavzu/), { target: { value: " " } });
    fireEvent.change(screen.getByLabelText(/^Xabaringiz/), { target: { value: " " } });
    fireEvent.submit(container.querySelector("form")!);
    expect(screen.getByRole("alert").textContent).toContain("Mavzu va xabar matnini kiriting");
    expect(send).not.toHaveBeenCalled();
    expect(screen.getByLabelText(/^Xabaringiz/).getAttribute("maxlength")).toBe("2000");
  });
});
