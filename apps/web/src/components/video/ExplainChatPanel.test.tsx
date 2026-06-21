import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ExplainChatPanel } from "./ExplainChatPanel";

Object.defineProperty(HTMLElement.prototype, "scrollTo", { configurable: true, value: vi.fn() });
afterEach(cleanup);

describe("ExplainChatPanel", () => {
  it("renders selected sentence context and submits inline without a dialog", () => {
    const send = vi.fn();
    render(<ExplainChatPanel variant="contextual" selectedSentence="It's nice to meet you." timestamp="00:18" conversation={{
      messages: [{ role: "assistant", text: "Bu ibora tanishuvda ishlatiladi." }],
      input: "Ma’nosi nima?", setInput: vi.fn(), sending: false, focusToken: 0,
      focusInput: vi.fn(), prefill: vi.fn(), send,
    }} />);
    expect(screen.queryByRole("dialog")).toBeNull();
    expect(screen.getByRole("region", { name: "AI’dan so‘rash" })).toBeTruthy();
    expect(screen.getByText("“It's nice to meet you.”")).toBeTruthy();
    expect(screen.getByRole("log").textContent).toContain("Bu ibora tanishuvda");
    fireEvent.click(screen.getByRole("button", { name: "Yuborish" }));
    expect(send).toHaveBeenCalledOnce();
  });

  it("uses the shared assistant response formatting", () => {
    render(<ExplainChatPanel variant="panel" conversation={{
      messages: [{ role: "assistant", text: "## Tuzilishi\n**Present Perfect**: have/has + V3." }],
      input: "",
      setInput: vi.fn(),
      sending: false,
      focusToken: 0,
      focusInput: vi.fn(),
      prefill: vi.fn(),
      send: vi.fn(),
    }} />);

    expect(screen.getByRole("heading", { name: "Tuzilishi" })).toBeTruthy();
    expect(screen.getByText("Present Perfect").tagName).toBe("STRONG");
  });

  it("exposes sheet dialog semantics and closes with Escape", () => {
    const onClose = vi.fn();
    render(<ExplainChatPanel variant="sheet" onClose={onClose} conversation={{
      messages: [],
      input: "",
      setInput: vi.fn(),
      sending: false,
      focusToken: 0,
      focusInput: vi.fn(),
      prefill: vi.fn(),
      send: vi.fn(),
    }} />);

    const dialog = screen.getByRole("dialog", { name: "AI'dan so'rash" });
    expect(within(dialog).getByRole("log").getAttribute("aria-live")).toBe("polite");
    expect(within(dialog).getByRole("textbox", { name: "Savolingizni yozing..." })).toBeTruthy();
    fireEvent.keyDown(dialog, { key: "Escape" });
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("keeps textarea focus when the sheet rerenders with a new close callback", () => {
    const conversation = {
      messages: [],
      input: "",
      setInput: vi.fn(),
      sending: false,
      focusToken: 0,
      focusInput: vi.fn(),
      prefill: vi.fn(),
      send: vi.fn(),
    };
    const { rerender } = render(
      <ExplainChatPanel variant="sheet" onClose={() => undefined} conversation={conversation} focusInputOnOpen={false} />,
    );
    const textarea = screen.getByRole("textbox", { name: "Savolingizni yozing..." });
    textarea.focus();

    rerender(
      <ExplainChatPanel
        variant="sheet"
        onClose={() => undefined}
        conversation={{ ...conversation, input: "a" }}
        focusInputOnOpen={false}
      />,
    );

    expect(document.activeElement).toBe(textarea);
  });

  it("grows the textarea until its maximum height and then scrolls", () => {
    const setInput = vi.fn();
    const { rerender } = render(<ExplainChatPanel variant="panel" conversation={{
      messages: [],
      input: "Short",
      setInput,
      sending: false,
      focusToken: 0,
      focusInput: vi.fn(),
      prefill: vi.fn(),
      send: vi.fn(),
    }} />);
    const textarea = screen.getByRole("textbox", { name: "Savolingizni yozing..." });
    Object.defineProperty(textarea, "scrollHeight", { configurable: true, value: 180 });
    vi.spyOn(window, "getComputedStyle").mockReturnValue({ maxHeight: "116px" } as CSSStyleDeclaration);

    rerender(<ExplainChatPanel variant="panel" conversation={{
      messages: [],
      input: "This is a long message that wraps onto multiple lines",
      setInput,
      sending: false,
      focusToken: 0,
      focusInput: vi.fn(),
      prefill: vi.fn(),
      send: vi.fn(),
    }} />);

    expect(textarea.style.height).toBe("116px");
    expect(textarea.style.overflowY).toBe("auto");
    vi.restoreAllMocks();
  });
});
