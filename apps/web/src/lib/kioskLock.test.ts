import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { applyKioskLock, isKioskLocked, releaseKioskLock } from "./kioskLock";

const ORIGINAL_VIEWPORT = "width=device-width, initial-scale=1.0, viewport-fit=cover";

beforeEach(() => {
  document.head.querySelector('meta[name="viewport"]')?.remove();
  const meta = document.createElement("meta");
  meta.name = "viewport";
  meta.content = ORIGINAL_VIEWPORT;
  document.head.appendChild(meta);
});

afterEach(() => {
  // The lock is module-level state - leaking it would pin html/body for every later suite.
  releaseKioskLock();
  document.head.querySelector('meta[name="viewport"]')?.remove();
});

function viewportContent() {
  return document.head.querySelector<HTMLMetaElement>('meta[name="viewport"]')?.content;
}

describe("kioskLock", () => {
  it("locks html and body and restores them exactly", () => {
    expect(isKioskLocked()).toBe(false);

    applyKioskLock();

    expect(isKioskLocked()).toBe(true);
    expect(document.documentElement.classList.contains("secure-assessment-kiosk")).toBe(true);
    expect(document.body.classList.contains("secure-assessment-kiosk")).toBe(true);
    expect(viewportContent()).toContain("user-scalable=no");

    releaseKioskLock();

    expect(isKioskLocked()).toBe(false);
    expect(document.documentElement.classList.contains("secure-assessment-kiosk")).toBe(false);
    expect(document.body.classList.contains("secure-assessment-kiosk")).toBe(false);
    expect(viewportContent()).toBe(ORIGINAL_VIEWPORT);
  });

  it("is idempotent - a second apply does not clobber the saved viewport", () => {
    applyKioskLock();
    applyKioskLock();

    releaseKioskLock();

    expect(viewportContent()).toBe(ORIGINAL_VIEWPORT);
  });

  it("releasing an unlocked document is a no-op", () => {
    expect(() => releaseKioskLock()).not.toThrow();
    expect(viewportContent()).toBe(ORIGINAL_VIEWPORT);
  });

  it("blocks the context menu only while locked", () => {
    const fire = () => {
      const event = new Event("contextmenu", { bubbles: true, cancelable: true });
      document.body.dispatchEvent(event);
      return event.defaultPrevented;
    };

    expect(fire()).toBe(false);
    applyKioskLock();
    expect(fire()).toBe(true);
    releaseKioskLock();
    expect(fire()).toBe(false);
  });

  it("blocks pinch gestures but leaves single-finger touches alone", () => {
    const fireTouch = (touches: number) => {
      const event = new Event("touchmove", { bubbles: true, cancelable: true });
      Object.defineProperty(event, "touches", { value: { length: touches } });
      document.body.dispatchEvent(event);
      return event.defaultPrevented;
    };

    applyKioskLock();

    expect(fireTouch(2)).toBe(true);
    expect(fireTouch(1)).toBe(false);
  });
});
