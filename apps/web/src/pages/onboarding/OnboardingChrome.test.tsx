import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { OnboardingChrome, PlayButton, PlayOption, StepProgress } from "./OnboardingChrome";

afterEach(cleanup);

describe("Pen onboarding primitives", () => {
  it("keeps navigation, reassurance and progress without a footer help link", () => {
    render(<MemoryRouter><OnboardingChrome step={2}><h1>Profil</h1></OnboardingChrome></MemoryRouter>);
    expect(screen.getByRole("banner")).toBeTruthy();
    expect(screen.getByRole("main")).toBeTruthy();
    expect(screen.getByRole("contentinfo")).toBeTruthy();
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("2");
    expect(screen.getByRole("progressbar").children).toHaveLength(3);
    expect(screen.getByText("EnglishAI · Bir qadam yaqinroq.")).toBeTruthy();
    expect(screen.queryByRole("link", { name: /Yordam/ })).toBeNull();
    expect(screen.getByRole("contentinfo").querySelector("a")).toBeNull();
  });
  it("lets a secure assessment intercept back before navigating", () => {
    const onBack = vi.fn();
    render(<MemoryRouter><OnboardingChrome onBack={onBack} secureAssessment>Test</OnboardingChrome></MemoryRouter>);
    fireEvent.click(screen.getByRole("button", { name: "Ortga qaytish" }));
    expect(onBack).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("link", { name: "EnglishAI.uz bosh sahifasi" }));
    expect(onBack).toHaveBeenCalledTimes(2);
  });
  it("shares the primary button without bypassing disabled state", () => {
    const onClick = vi.fn();
    render(<PlayButton onClick={onClick} disabled>Davom etish</PlayButton>);
    fireEvent.click(screen.getByRole("button"));
    expect(onClick).not.toHaveBeenCalled();
    expect(screen.getByRole("button").classList.contains("onboarding-play__button")).toBe(true);
  });
  it("announces selection for compact and full-size options", () => {
    render(<><PlayOption icon={<span />} selected>Goal</PlayOption><PlayOption icon={<span />} selected={false} compact>Source</PlayOption></>);
    expect(screen.getByRole("button", { name: "Goal" }).getAttribute("aria-pressed")).toBe("true");
    expect(screen.getByRole("button", { name: "Source" }).getAttribute("aria-pressed")).toBe("false");
  });
  it("keeps progress within its accessible range", () => {
    render(<StepProgress value={10} total={6} label="Test" />);
    expect(screen.getByRole("progressbar").getAttribute("aria-valuenow")).toBe("6");
    expect(screen.getByRole("progressbar").children).toHaveLength(6);
  });
});
