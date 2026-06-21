import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AdminGrammarPage } from "./AdminGrammarPage";

const { grammarList } = vi.hoisted(() => ({
  grammarList: vi.fn(),
}));

vi.mock("@/api/client", () => ({
  api: {
    admin: {
      grammar: { list: grammarList },
    },
  },
}));

afterEach(() => {
  cleanup();
  grammarList.mockReset();
});

async function assertRecoverableError(Page: () => React.ReactElement, loader: ReturnType<typeof vi.fn>, expectedCopy: string) {
  loader.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce([]);
  render(<MemoryRouter><Page /></MemoryRouter>);

  const alert = await screen.findByRole("alert");
  expect(alert.textContent).toContain(expectedCopy);
  expect(screen.queryByText("ERROR CARD DEBUG")).toBeNull();

  fireEvent.click(screen.getByRole("button", { name: "Qayta urinish" }));
  await waitFor(() => expect(loader).toHaveBeenCalledTimes(2));
  await waitFor(() => expect(screen.queryByRole("alert")).toBeNull());
}

describe("admin CRUD load error states", () => {
  it("renders and retries the grammar load error", async () => {
    await assertRecoverableError(AdminGrammarPage, grammarList, "Darslarni yuklashda xatolik");
  });
});
