import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ReadingWordPanel } from "./ReadingWordPanel";

const playWordVoice = vi.hoisted(() => vi.fn());
vi.mock("@/lib/useAsync", () => ({
  useAsync: () => ({ data:{ ipa:"/ˈpeɪʃənt/", audioBase64:null } }),
}));
vi.mock("@/lib/audio", () => ({ playWordVoice }));
beforeEach(() => { vi.spyOn(HTMLMediaElement.prototype, "pause").mockImplementation(() => undefined); });
afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.clearAllMocks(); });

const words = [
  { word:"patient", translation:"sabrli", exampleSentence:"She is kind and patient." },
  { word:"mother", translation:"ona", exampleSentence:null },
];

describe("ReadingWordPanel without saving", () => {
  it("retains the word, pronunciation and example without a save button", () => {
    render(<ReadingWordPanel words={words} index={0} onSelect={vi.fn()} />);
    expect(screen.getByRole("heading", { name:"patient" })).toBeTruthy();
    expect(screen.getByText("sabrli")).toBeTruthy();
    expect(screen.getByText("/ˈpeɪʃənt/")).toBeTruthy();
    expect(screen.getByText("She is kind and patient.")).toBeTruthy();
    expect(screen.queryByRole("button", { name:/saqla/i })).toBeNull();
    expect(screen.getAllByRole("button")).toHaveLength(3);
  });

  it("keeps pronunciation playback and word navigation working", () => {
    const onSelect = vi.fn();
    render(<ReadingWordPanel words={words} index={0} onSelect={onSelect} />);
    fireEvent.click(screen.getByRole("button", { name:"patient talaffuzini tinglash" }));
    expect(playWordVoice).toHaveBeenCalledWith(null, "patient", expect.any(HTMLAudioElement));
    fireEvent.click(screen.getByRole("button", { name:"Keyingi so‘z" }));
    expect(onSelect).toHaveBeenCalledWith(1);
    expect(screen.getByRole("button", { name:"Oldingi so‘z" }).hasAttribute("disabled")).toBe(true);
  });
});
