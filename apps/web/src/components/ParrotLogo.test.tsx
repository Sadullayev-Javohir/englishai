import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ParrotLogo } from "./ParrotLogo";

describe("ParrotLogo", () => {
  it("keeps legacy callers on the canonical Dialog logo", () => {
    render(<ParrotLogo />);

    const logo = screen.getByRole("img", { name: "EnglishAI" });
    expect(logo.getAttribute("src")).toBe("/assets/brand/dialog.svg");

    const tile = logo.parentElement;
    expect(tile?.getAttribute("data-englishai-logo-tile")).toBe("true");
    expect(tile?.style.background).toBe("transparent");
  });
});
