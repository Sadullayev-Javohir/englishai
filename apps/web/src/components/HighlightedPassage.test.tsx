import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { HighlightedPassage } from "./HighlightedPassage";
const openDefault = vi.hoisted(() => vi.fn());
vi.mock("@/components/word/useWordPopover", () => ({ useWordPopover:()=>({ bind:()=>({ onClick:openDefault }), overlay:<div data-testid="default-overlay"/> }) }));
afterEach(()=>{cleanup();vi.clearAllMocks();});
const words=[{word:"teach",translation:"o‘rgatmoq",exampleSentence:null}];
describe('HighlightedPassage dictionary integration',()=>{
  it('preserves the existing default popover for other skills',()=>{render(<HighlightedPassage passage="She taught me." words={words}/>);fireEvent.click(screen.getByText('taught'));expect(openDefault).toHaveBeenCalledOnce();expect(screen.getByTestId('default-overlay')).toBeTruthy();});
  it('uses an accessible inline selection button when Reading provides a dictionary',()=>{const select=vi.fn();render(<HighlightedPassage passage="She taught me." words={words} selectedWord="teach" onWordSelect={select}/>);const word=screen.getByRole('button',{name:'taught'});expect(word.getAttribute('aria-pressed')).toBe('true');fireEvent.click(word);expect(select).toHaveBeenCalledWith('teach');expect(openDefault).not.toHaveBeenCalled();expect(screen.queryByTestId('default-overlay')).toBeNull();});
  it('keeps noninteractive passages read-only',()=>{render(<HighlightedPassage passage="She taught me." words={words} interactive={false}/>);expect(screen.queryByRole('button')).toBeNull();expect(screen.getByText('taught')).toBeTruthy();expect(screen.queryByTestId('default-overlay')).toBeNull();});
});
