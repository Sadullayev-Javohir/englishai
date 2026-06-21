import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { LiveVoiceBackdrop } from "./LiveVoiceBackdrop";

describe("LiveVoiceBackdrop", () => {
  it("switches to an audio-reactive tutor scene while the AI speaks", () => {
    const { container } = render(
      <LiveVoiceBackdrop state="speaking" intensity={0.72} />,
    );

    const scene = container.querySelector<HTMLElement>("[data-live-voice-scene]");
    expect(scene?.dataset.voiceState).toBe("speaking");
    expect(scene?.style.getPropertyValue("--voice-intensity")).toBe("0.72");
    expect(container.querySelectorAll("[data-voice-orb]")).toHaveLength(3);
  });

  it("renders distinct listening and thinking scenes", () => {
    const { container, rerender } = render(
      <LiveVoiceBackdrop state="listening" intensity={0.25} />,
    );

    const scene = container.querySelector<HTMLElement>("[data-live-voice-scene]");
    expect(scene?.dataset.voiceState).toBe("listening");

    rerender(<LiveVoiceBackdrop state="thinking" intensity={0.1} />);
    expect(scene?.dataset.voiceState).toBe("thinking");
  });

  it("stays visually quiet while the conversation is idle", () => {
    const { container } = render(
      <LiveVoiceBackdrop state="idle" intensity={0} />,
    );

    expect(
      container.querySelector<HTMLElement>("[data-live-voice-scene]")?.dataset.voiceState,
    ).toBe("idle");
  });
});
