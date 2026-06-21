import { describe, expect, it } from "vitest";
import { ICON_MAP } from "./iconMap";

const APP_ICON_NAMES = [
  "analytics",
  "admin_panel_settings",
  "arrow_downward",
  "auto_awesome",
  "auto_edit",
  "book",
  "bookmark_add",
  "calendar_view_month",
  "category",
  "chevron_left",
  "dashboard",
  "devices",
  "draw",
  "event_busy",
  "favorite",
  "flip",
  "folder",
  "format_quote",
  "key",
  "leaderboard",
  "library_books",
  "library_music",
  "login",
  "mail",
  "monitoring",
  "near_me",
  "notes",
  "paid",
  "pets",
  "photo_camera",
  "route",
  "routine",
  "save",
  "signal_cellular_alt",
  "sparkles",
  "speed",
  "spellcheck",
  "stars",
  "storage",
  "swipe",
  "timer_off",
  "tips_and_updates",
  "title",
  "tune",
  "upload_file",
  "verified_user",
  "view_module",
  "wifi_off",
] as const;

describe("ICON_MAP", () => {
  it.each(APP_ICON_NAMES)("maps %s to a real icon", (name) => {
    expect(ICON_MAP[name]).toBeTypeOf("object");
  });
});
