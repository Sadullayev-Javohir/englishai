import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { EnergyOutcome, type EnergyDto } from "@/api/types";
import { EnergyModal } from "./EnergyModal";

afterEach(cleanup);

function balance(current: number): EnergyDto {
  const full = current === 5;
  return {
    current,
    maximum: 5,
    nextRefillAt: full ? null : "2030-01-01T00:36:00.000Z",
    fullRefillAt: full ? null : "2030-01-01T01:48:00.000Z",
    outcome: EnergyOutcome.None,
  };
}

describe("EnergyModal", () => {
  it("renders the empty state with no filled cells", () => {
    render(<EnergyModal open energy={balance(0)} onClose={vi.fn()} onPrimary={vi.fn()} />);
    expect(screen.getByRole("heading", { name: "ENERGIYA" })).toBeTruthy();
    expect(screen.getByText("Energiya tugadi")).toBeTruthy();
    expect(document.querySelectorAll(".energy-modal__cell.is-full")).toHaveLength(0);
    expect(screen.getByRole("button", { name: "Boshqa mashqlarni bajarish" })).toBeTruthy();
  });

  it("renders the two-unit restored state", () => {
    render(<EnergyModal open energy={balance(2)} onClose={vi.fn()} onPrimary={vi.fn()} />);
    expect(screen.getByText("2 ta energiya tiklandi")).toBeTruthy();
    expect(document.querySelectorAll(".energy-modal__cell.is-full")).toHaveLength(2);
    expect(screen.getByText("Keyingi +1 energiya")).toBeTruthy();
  });

  it("renders the full ready panel without countdowns", () => {
    render(<EnergyModal open energy={balance(5)} onClose={vi.fn()} onPrimary={vi.fn()} />);
    expect(screen.getByText("Energiya to'liq tiklandi!")).toBeTruthy();
    expect(document.querySelectorAll(".energy-modal__cell.is-full")).toHaveLength(5);
    expect(screen.getByText("Tiklanish yakunlandi")).toBeTruthy();
    expect(screen.getByText("Tayyor")).toBeTruthy();
    expect(screen.queryByText("Keyingi +1 energiya")).toBeNull();
  });
});
