import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EnglishAiLogo } from "./EnglishAiLogo";

describe("EnglishAiLogo", () => {
  it("renders the approved Dialog mark without a background tile", () => {
    render(<EnglishAiLogo />);

    const logo = screen.getByRole("img", { name: "EnglishAI" });
    expect(logo.getAttribute("src")).toBe("/assets/brand/dialog.svg");

    const tile = logo.parentElement;
    expect(tile?.getAttribute("data-englishai-logo-tile")).toBe("true");
    expect(tile?.style.background).toBe("transparent");
    expect(logo.getAttribute("width")).toBe("48");
    expect(document.querySelector('[data-englishai-wordmark]')).toBeNull();
  });

  it("provides the EnglishAI desktop name and retains an accessible mark", () => {
    const { container } = render(<EnglishAiLogo size={36} withWordmark />);
    const wordmark = container.querySelector('[data-englishai-wordmark]');
    expect(wordmark?.textContent).toBe("EnglishAI");
    expect(wordmark?.getAttribute("aria-hidden")).toBe("true");
    expect(wordmark?.classList.contains("ea-brand-name")).toBe(true);
    expect(screen.getByRole("img", { name: "EnglishAI" }).getAttribute("width")).toBe("36");
    expect(container.textContent).not.toContain(".uz");
  });
});
