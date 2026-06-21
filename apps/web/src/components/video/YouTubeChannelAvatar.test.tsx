import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { YouTubeChannelAvatar } from "./YouTubeChannelAvatar";

afterEach(cleanup);

describe("YouTubeChannelAvatar", () => {
  it("uses the actual channel image, not the video thumbnail", () => {
    render(<YouTubeChannelAvatar channel="BBC Learning English" url="https://yt3.ggpht.com/bbc-avatar" />);
    expect(screen.getByRole("img").getAttribute("src")).toBe("https://yt3.ggpht.com/bbc-avatar");
    expect(screen.getByRole("img").getAttribute("alt")).toBe("BBC Learning English kanal logosi");
  });
  it("handles missing or failed artwork without a broken image, and retries a new URL", () => {
    const { rerender } = render(<YouTubeChannelAvatar channel="Channel" url="https://yt3.ggpht.com/expired" />);
    fireEvent.error(screen.getByRole("img"));
    expect(screen.queryByRole("img")).toBeNull();
    rerender(<YouTubeChannelAvatar channel="Channel" url="https://yt3.ggpht.com/fresh" />);
    expect(screen.getByRole("img").getAttribute("src")).toBe("https://yt3.ggpht.com/fresh");
    rerender(<YouTubeChannelAvatar channel="Channel" />);
    expect(screen.queryByRole("img")).toBeNull();
  });
  it("rejects non-HTTPS image sources", () => {
    render(<YouTubeChannelAvatar channel="Channel" url="http://insecure.example/avatar" />);
    expect(screen.queryByRole("img")).toBeNull();
  });
});
