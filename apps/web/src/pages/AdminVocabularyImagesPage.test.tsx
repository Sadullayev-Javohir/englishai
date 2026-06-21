import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { api } from "@/api/client";
import { AdminVocabularyImagesPage } from "./AdminVocabularyImagesPage";

vi.mock("@/api/client", async () => {
  const actual = await vi.importActual<typeof import("@/api/client")>("@/api/client");
  return {
    ...actual,
    api: {
      admin: {
        vocabularyImages: {
          list: vi.fn(),
          replace: vi.fn(),
        },
      },
    },
  };
});

const image = {
  topicId: "11111111-1111-1111-1111-111111111111",
  imageId: "22222222-2222-2222-2222-222222222222",
  topicTitle: "My Family",
  level: "A1",
  word: "mother",
  translation: "ona",
  imageUrl: "/api/images/vocabulary-topics/topic/words/image",
  imageSource: "Wikimedia",
  imageAttribution: null,
  version: 0,
};

describe("AdminVocabularyImagesPage", () => {
  beforeEach(() => {
    vi.mocked(api.admin.vocabularyImages.list).mockResolvedValue([image]);
    vi.mocked(api.admin.vocabularyImages.replace).mockResolvedValue({
      ...image,
      imageSource: "Unsplash",
      version: 12345,
    });
  });

  it("shows English and Uzbek labels and updates the image immediately after replacement", async () => {
    render(<MemoryRouter><AdminVocabularyImagesPage /></MemoryRouter>);

    expect(await screen.findByText("mother")).toBeTruthy();
    expect(screen.getByText("ona")).toBeTruthy();
    const picture = screen.getByRole("img", { name: "mother — ona" });
    expect(picture.getAttribute("width")).toBe("100");
    expect(picture.getAttribute("height")).toBe("100");

    fireEvent.click(screen.getByRole("button", { name: "mother rasmini almashtirish" }));

    await waitFor(() => expect(api.admin.vocabularyImages.replace).toHaveBeenCalledWith(image.topicId, image.imageId));
    await waitFor(() => expect(picture.getAttribute("src")).toContain("v=12345"));
    expect(await screen.findByText("“mother” rasmi yangilandi.")).toBeTruthy();
  });
});
