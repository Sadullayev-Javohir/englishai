import { render, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SupportNotificationPermission } from "./SupportNotificationPermission";

const originalNotification = window.Notification;

afterEach(() => {
  Object.defineProperty(window, "Notification", { configurable: true, value: originalNotification });
});

function mockNotification(permission: NotificationPermission, next: NotificationPermission = permission) {
  Object.defineProperty(window, "Notification", {
    configurable: true,
    value: { permission, requestPermission: vi.fn().mockResolvedValue(next) },
  });
}

describe("SupportNotificationPermission", () => {
  it("asks for browser notification permission", async () => {
    mockNotification("default", "granted");
    render(<SupportNotificationPermission />);

    await waitFor(() => expect(window.Notification.requestPermission).toHaveBeenCalledOnce());
  });

  it("does not ask again after permission is granted", () => {
    mockNotification("granted");
    render(<SupportNotificationPermission />);
    expect(window.Notification.requestPermission).not.toHaveBeenCalled();
  });

  it("does not ask again after permission was denied", () => {
    mockNotification("denied");
    render(<SupportNotificationPermission />);
    expect(window.Notification.requestPermission).not.toHaveBeenCalled();
  });
});
