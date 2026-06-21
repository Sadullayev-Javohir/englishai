import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { CefrLevel, type TopicWordDto } from "@/api/types";
import { accentFor } from "@/lib/cardPalette";
import { TopicImage } from "./TopicImage";
import { WordImage } from "./WordImage";
import { RoleCard3D } from "./arena/RoleCard3D";

const word: TopicWordDto = {
  word: "family",
  translation: "oila",
  imageUrl: "https://example.com/unsafe-photo.jpg",
  imageAttribution: "ma'lumot",
} as TopicWordDto;

afterEach(cleanup);

describe("safe learning images", () => {
  it("renders the stored topic cover through the local image endpoint", () => {
    render(
      <TopicImage
        topicId="11111111-1111-1111-1111-111111111111"
        title="My family"
        level={CefrLevel.A1}
        category="family"
      />,
    );

    const image = screen.getByRole("img", { name: "My family" });
    expect(image.getAttribute("src")).toContain(
      "/api/images/topics/11111111-1111-1111-1111-111111111111",
    );
  });

  it("uses an icon-free local illustration when the stored cover fails", () => {
    const { container } = render(
      <TopicImage
        topicId="22222222-2222-2222-2222-222222222222"
        title="Airport"
        level={CefrLevel.A2}
      />,
    );

    fireEvent.error(screen.getByRole("img", { name: "Airport" }));

    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("[data-safe-art^='topic-']")).not.toBeNull();
    expect(container.querySelector(".material-symbols-rounded")).toBeNull();
    expect(screen.getByText("Airport")).toBeTruthy();
  });

  it("rejects an external word image and renders safe local art", () => {
    const { container } = render(<WordImage word={word} />);

    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("[src]")).toBeNull();
    expect(container.querySelector("[data-safe-art^='word-']")).not.toBeNull();
    expect(screen.queryByText("ma'lumot")).toBeNull();
  });

  it("renders a stored local word illustration without burning labels into the artwork", () => {
    const { container } = render(
      <WordImage
        word={{
          ...word,
          imageUrl:
            "/api/images/vocabulary-topics/11111111-1111-1111-1111-111111111111/words/22222222-2222-2222-2222-222222222222",
        }}
      />,
    );

    expect(screen.queryByText("oila")).toBeNull();
    expect(screen.queryByText("family")).toBeNull();
    expect(screen.getByRole("img", { name: "family — oila" })).toBeTruthy();
    expect(container.querySelector("img")?.getAttribute("src")).toContain(
      "/api/images/vocabulary-topics/11111111-1111-1111-1111-111111111111/words/",
    );
  });

  it("falls back to bundled semantic art when a stored word image fails", () => {
    const { container } = render(
      <WordImage
        word={{
          ...word,
          imageUrl:
            "/api/images/vocabulary-topics/11111111-1111-1111-1111-111111111111/words/22222222-2222-2222-2222-222222222222",
        }}
      />,
    );

    fireEvent.error(screen.getByRole("img", { name: "family — oila" }));
    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("[data-safe-art^='word-']")).not.toBeNull();
  });

  it("uses a deterministic fallback icon for words outside semantic groups", () => {
    const first = render(
      <WordImage
        word={{
          ...word,
          word: "unmappedword",
          translation: "noma'lum",
        }}
      />,
    );
    const firstIcon = first.container.querySelector("svg")?.getAttribute("class");
    first.unmount();
    const second = render(<WordImage word={{ ...word, word: "unmappedword", translation: "noma'lum" }} />);
    expect(second.container.querySelector("svg")?.getAttribute("class")).toBe(firstIcon);
  });

  it("renders role-talk covers from the scenario image id", () => {
    render(
      <RoleCard3D
        title="Job interview"
        level={CefrLevel.B1}
        icon="theater_comedy"
        onClick={() => undefined}
        cover={
          <TopicImage
            topicId="33333333-3333-3333-3333-333333333333"
            title="Job interview"
            level={CefrLevel.B1}
            category="roleplay"
          />
        }
      />,
    );

    const image = screen.getByRole("img", { name: "Job interview" });
    expect(image.getAttribute("src")).toContain(
      "/api/images/topics/33333333-3333-3333-3333-333333333333",
    );
    expect(screen.queryByText("theater_comedy")).toBeNull();
  });

  it("uses distinct front and back accents for all 15 word cards", () => {
    for (let index = 0; index < 15; index += 1) {
      expect(accentFor(index + 5)).not.toBe(accentFor(index));
    }
  });
});
