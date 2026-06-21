import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import { NotFoundPage } from "./NotFoundPage";

afterEach(cleanup);

describe("NotFoundPage", () => {
  it("offers a focusable Home recovery action", () => {
    render(
      <MemoryRouter initialEntries={["/missing"]}>
        <Routes>
          <Route path="*" element={<NotFoundPage />} />
          <Route path="/home" element={<div>Home destination</div>} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByRole("heading", { name: "Sahifa topilmadi" })).toBeTruthy();
    expect(screen.getByLabelText("EnglishAI.uz")).toBeTruthy();
    expect(screen.getByRole("button", { name: "Oldingi sahifaga qaytish" })).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Asosiy sahifaga o'tish" }));
    expect(screen.getByText("Home destination")).toBeTruthy();
  });
});
