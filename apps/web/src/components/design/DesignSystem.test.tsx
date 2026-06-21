import React from "react";
import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  AppButton,
  AppIconButton,
  ChartCard,
  DesignCard,
  DesignImage,
  DesignText,
  DesignInput,
  DesignModal,
  DesignSheet,
  DesignToast,
  EmptyState,
  ErrorState,
  FormField,
  LoadingState,
  PageHeader,
  ResponsiveGrid,
  SectionHeader,
  StatCard,
} from "./index";

afterEach(() => {
  cleanup();
  document.body.style.overflow = "";
});

describe("shared design foundation", () => {
  it("renders button variants with accessible loading and icon actions", () => {
    render(
      <div>
        <AppButton tone="primary" loading>Saqlash</AppButton>
        <AppIconButton icon="close" label="Yopish" />
      </div>,
    );

    expect(screen.getByRole("button", { name: "Saqlash" }).getAttribute("aria-busy")).toBe("true");
    expect(screen.getByRole("button", { name: "Yopish" }).getAttribute("title")).toBe("Yopish");
  });

  it("renders card, header, stat, chart, grid and state contracts", () => {
    const view = render(
      <main>
        <PageHeader eyebrow="Bugun" title="Bosh sahifa" description="Rejangiz" />
        <SectionHeader title="Natijalar" />
        <ResponsiveGrid minItemWidth={280} data-testid="grid">
          <DesignCard tone="primary">
            <DesignImage src="/assets/app-icon.png" alt="EnglishAI" />
            <DesignText as="h3" tone="heading">Missiya</DesignText>
          </DesignCard>
          <StatCard label="Ball" value="120" icon="star" />
        </ResponsiveGrid>
        <ChartCard title="Hafta" chartLabel="Haftalik ball chizmasi"><canvas /></ChartCard>
        <LoadingState />
        <EmptyState title="Ma’lumot yo‘q" />
        <ErrorState />
      </main>,
    );

    expect(screen.getByRole("heading", { level: 1, name: "Bosh sahifa" })).toBeTruthy();
    expect(screen.getByRole("img", { name: "Haftalik ball chizmasi" })).toBeTruthy();
    expect(screen.getByRole("status")).toBeTruthy();
    expect(screen.getByRole("alert")).toBeTruthy();
    expect(screen.getByTestId("grid").getAttribute("style")).toContain("280px");
    expect(view.container.querySelector(".ea-card--primary")).toBeTruthy();
    expect(screen.getByRole("img", { name: "EnglishAI" }).classList.contains("responsive-media")).toBe(true);
    expect(screen.getByRole("heading", { level: 3, name: "Missiya" }).classList.contains("text-responsive-heading")).toBe(true);
  });

  it("connects form field messaging and opens a portal modal", () => {
    const onClose = vi.fn();
    render(
      <div>
        <FormField label="Ism" htmlFor="name" error="Ism majburiy">
          <DesignInput id="name" aria-describedby="name-message" />
        </FormField>
        <DesignModal open onClose={onClose} title="Tasdiqlash">Mazmun</DesignModal>
      </div>,
    );

    expect(screen.getByLabelText("Ism")).toBeTruthy();
    expect(screen.getByRole("dialog", { name: "Tasdiqlash" })).toBeTruthy();
    expect(document.body.style.overflow).toBe("hidden");
    fireEvent.click(within(screen.getByRole("dialog", { name: "Tasdiqlash" })).getByRole("button", { name: "Yopish" }));
    expect(onClose).toHaveBeenCalledOnce();
  });

  it("traps focus, closes with Escape and returns focus", async () => {
    const onClose = vi.fn();
    function Harness() {
      const [open, setOpen] = React.useState(false);
      return <div><button onClick={() => setOpen(true)}>Ochuvchi</button><DesignModal open={open} onClose={() => { onClose(); setOpen(false); }} title="Dialog"><button>Birinchi</button><button>Oxirgi</button></DesignModal></div>;
    }
    render(<Harness />);
    const opener = screen.getByRole("button", { name: "Ochuvchi" });
    opener.focus();
    fireEvent.click(opener);
    const dialog = screen.getByRole("dialog", { name: "Dialog" });
    const buttons = within(dialog).getAllByRole("button");
    buttons.at(-1)?.focus();
    fireEvent.keyDown(document, { key: "Tab" });
    expect(document.activeElement).toBe(buttons[0]);
    fireEvent.keyDown(document, { key: "Escape" });
    expect(onClose).toHaveBeenCalledOnce();
    await vi.waitFor(() => expect(document.activeElement).toBe(opener));
  });

  it("renders shared sheet and live toast feedback", () => {
    render(
      <>
        <DesignSheet open onClose={() => undefined} title="Darslar">Mazmun</DesignSheet>
        <DesignToast open title="Saqlandi" message="O‘zgarishlar tayyor" tone="success" />
      </>,
    );
    expect(screen.getByRole("dialog", { name: "Darslar" })).toBeTruthy();
    expect(screen.getByRole("status").textContent).toContain("Saqlandi");
    expect(screen.getByRole("dialog", { name: "Darslar" }).parentElement?.classList.contains("ea-responsive-overlay")).toBe(true);
    expect(screen.getByRole("status").parentElement?.classList.contains("ea-responsive-toast-region")).toBe(true);
    expect(screen.getByRole("status").parentElement?.parentElement).toBe(document.body);
  });

  it("keeps body scroll locked until the last nested overlay closes", () => {
    const view = render(
      <>
        <DesignModal open onClose={() => undefined} title="Birinchi">Mazmun</DesignModal>
        <DesignSheet open onClose={() => undefined} title="Ikkinchi">Mazmun</DesignSheet>
      </>,
    );

    expect(document.body.style.overflow).toBe("hidden");
    view.rerender(<DesignModal open onClose={() => undefined} title="Birinchi">Mazmun</DesignModal>);
    expect(document.body.style.overflow).toBe("hidden");
    view.unmount();
    expect(document.body.style.overflow).toBe("");
  });
});
