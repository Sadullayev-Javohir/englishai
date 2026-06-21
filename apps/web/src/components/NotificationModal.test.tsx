import { readFileSync } from "node:fs";
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { NotificationModal } from "./NotificationModal";
import { resetNotificationsFeedForTests } from "@/lib/notificationsFeed";

const notifications = vi.fn();
const markRead = vi.fn();
const dismissNotification = vi.fn();

vi.mock("@/api/client", () => ({
  api: {
    vocabulary: {
      notifications: (...args: unknown[]) => notifications(...args),
      markRead: (...args: unknown[]) => markRead(...args),
      dismissNotification: (...args: unknown[]) => dismissNotification(...args),
    },
  },
}));

vi.mock("@/app/session", () => ({ getLearnerId: () => "learner-1" }));
vi.mock("@/lib/notificationsRefresh", () => ({
  useNotificationsRefreshKey: () => 0,
}));
vi.mock("@/lib/haptics", () => ({ tapLight: vi.fn() }));

function renderModal() {
  return render(
    <MemoryRouter initialEntries={["/home?notifications=open"]}>
      <Routes>
        <Route path="/home" element={<NotificationModal />} />
        <Route path="/reading" element={<div>READING_DESTINATION</div>} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  notifications.mockReset();
  markRead.mockReset();
  dismissNotification.mockReset();
  markRead.mockResolvedValue(undefined);
  dismissNotification.mockResolvedValue(undefined);
  resetNotificationsFeedForTests();
  localStorage.clear();
});

afterEach(() => {
  cleanup();
  document.body.style.overflow = "";
});

describe("NotificationModal", () => {
  it("uses the Pen 68 modal surface and mobile geometry", () => {
    const css = readFileSync("src/index.css", "utf8");
    expect(css).toContain("Pen 68: notification center");
    expect(css).toContain("width:min(600px, calc(100vw - 32px))");
    expect(css).toContain("background:#FFFCF7");
    expect(css).toContain(".notification-modal__item.is-unread");
    expect(css).toContain("animation:notification-modal-center-in");
    expect(css).toContain(".ea-overlay--modal:has(.notification-modal) { align-items:center; justify-content:center; }");
  });

  it("renders the Pen-style header, tabs, unread markers, and read cards", async () => {
    notifications.mockResolvedValue({
      items: [
        {
          id: "notice-1",
          code: "practice_reading",
          title: "Yangi o'qish darsi",
          message: "Bugungi matn siz uchun tayyor.",
          createdAt: new Date().toISOString(),
          isRead: false,
          linkUrl: "/reading",
        },
        {
          id: "notice-2",
          code: "streak",
          title: "7 kunlik seriya",
          message: "Davom eting.",
          createdAt: new Date().toISOString(),
          isRead: true,
          linkUrl: null,
        },
      ],
    });

    renderModal();

    const dialog = await screen.findByRole("dialog", {
      name: "Bildirishnomalar",
    });
    expect(dialog.classList.contains("notification-modal")).toBe(true);
    expect(screen.getByText("1 ta yangi xabaringiz bor")).toBeTruthy();
    expect(
      screen
        .getByRole("button", { name: "Barchasi" })
        .getAttribute("aria-pressed")
    ).toBe("true");
    expect(screen.getByRole("button", { name: "O‘qilmagan · 1" })).toBeTruthy();
    expect(
      dialog.querySelectorAll(".notification-modal__unread-dot")
    ).toHaveLength(1);
    expect(
      dialog.querySelectorAll(".notification-modal__item.is-read")
    ).toHaveLength(1);
  });

  it("filters to unread notifications", async () => {
    notifications.mockResolvedValue({
      items: [
        {
          id: "notice-1",
          code: "practice_reading",
          title: "O'qilmagan",
          message: "Yangi dars.",
          createdAt: new Date().toISOString(),
          isRead: false,
          linkUrl: null,
        },
        {
          id: "notice-2",
          code: "streak",
          title: "O'qilgan",
          message: "Kecha.",
          createdAt: new Date().toISOString(),
          isRead: true,
          linkUrl: null,
        },
      ],
    });

    renderModal();
    fireEvent.click(
      await screen.findByRole("button", { name: "O‘qilmagan · 1" })
    );

    expect(screen.getByText("O'qilmagan")).toBeTruthy();
    expect(screen.queryByText("O'qilgan")).toBeNull();
  });

  it("marks all notifications as read", async () => {
    notifications.mockResolvedValue({
      items: [
        {
          id: "notice-1",
          code: "daily_plan",
          title: null,
          message: "Bugungi reja tayyor.",
          createdAt: new Date().toISOString(),
          isRead: false,
          linkUrl: null,
        },
      ],
    });

    renderModal();
    fireEvent.click(
      await screen.findByRole("button", {
        name: "Barchasini o‘qilgan deb belgilash",
      })
    );

    await waitFor(() => expect(markRead).toHaveBeenCalledWith("learner-1"));
  });

  it("dismisses an internal notification before navigating", async () => {
    notifications.mockResolvedValue({
      items: [
        {
          id: "notice-1",
          code: "practice_reading",
          title: "O'qishni davom ettiring",
          message: "Yangi matn ochildi.",
          createdAt: new Date().toISOString(),
          isRead: false,
          linkUrl: "/reading",
        },
      ],
    });

    renderModal();
    fireEvent.click(
      await screen.findByRole("button", { name: "O'qishni davom ettiring" })
    );

    await waitFor(() =>
      expect(dismissNotification).toHaveBeenCalledWith("learner-1", "notice-1")
    );
    expect(await screen.findByText("READING_DESTINATION")).toBeTruthy();
  });

});
