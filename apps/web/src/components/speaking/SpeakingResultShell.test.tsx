import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { SpeakingResultShell } from "./SpeakingResultShell";

vi.mock("@/components/game", () => ({ Confetti: () => null }));

describe("SpeakingResultShell", () => {
  it("renders shared score, details, progression, and actions", () => {
    const { container } = render(
      <SpeakingResultShell
        status="passed"
        kicker="Speaking natijasi"
        title="Ajoyib ish"
        subtitle="Mashq yakunlandi"
        score={84}
        scoreLabel="Talaffuz"
        scoreContent={<p>4 ta javob</p>}
        details={<section>Yaxshilash kerak bo‘lgan so‘zlar</section>}
        progression={<section>Keyingi ko‘nikma</section>}
        actions={<button type="button">Yana mashq qilish</button>}
        celebrate
      />,
    );

    expect(screen.getByRole("heading", { name: "Ajoyib ish" })).toBeTruthy();
    expect(screen.getByLabelText("Talaffuz: 84")).toBeTruthy();
    expect(screen.getByText("Yaxshilash kerak bo‘lgan so‘zlar")).toBeTruthy();
    expect(screen.getByText("Keyingi ko‘nikma")).toBeTruthy();
    expect(container.querySelector(".speaking-result--passed")).toBeTruthy();
  });

  it("supports non-evaluable results without an empty score card", () => {
    const { container } = render(
      <SpeakingResultShell
        status="neutral"
        kicker="Suhbat yakunlandi"
        title="Sahna yakunlandi"
        subtitle="Baholash uchun yetarli nutq yo‘q"
        actions={<button type="button">Bosh sahifa</button>}
      />,
    );

    expect(screen.getByText("Baholash uchun yetarli nutq yo‘q")).toBeTruthy();
    expect(container.querySelector(".speaking-result__score")).toBeNull();
    expect(container.querySelector(".speaking-result--neutral")).toBeTruthy();
  });
});
