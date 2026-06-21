import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { expect, it, vi } from "vitest";
import { AuthContext, type AuthContextValue } from "@/app/auth";
import { AcquisitionSource, Gender, LearningGoal, type AuthenticatedUserDto } from "@/api/types";
import { DemographicsSetupPage } from "./DemographicsSetupPage";

const { setDemographics } = vi.hoisted(() => ({ setDemographics: vi.fn() }));
vi.mock("@/api/client", () => ({ api: { auth: { setDemographics } } }));

const user: AuthenticatedUserDto = { id:"1", email:"a@b.com", displayName:"A", username:"a-user", pictureUrl:null, hasOnboarded:false, preferredName:null, learningGoal:LearningGoal.Unspecified, birthDate:null, gender:null, acquisitionSource:null, acquisitionSourceOther:null, hasCompletedDemographics:false };

it("collects and saves the required demographics", async () => {
  const updated = { ...user, birthDate:"2000-01-02", gender:Gender.Male, acquisitionSource:AcquisitionSource.Telegram, hasCompletedDemographics:true };
  setDemographics.mockResolvedValue(updated);
  const auth: AuthContextValue = { status:"authenticated", user, signInWithGoogle:vi.fn(), signOut:vi.fn(), applyUser:vi.fn() };
  render(<AuthContext.Provider value={auth}><MemoryRouter><DemographicsSetupPage /></MemoryRouter></AuthContext.Provider>);
  fireEvent.click(screen.getByRole("button", { name: "Kun" }));
  fireEvent.click(screen.getByRole("button", { name: "2" }));
  fireEvent.click(screen.getByRole("button", { name: "Oy" }));
  fireEvent.click(screen.getByRole("button", { name: "Yanvar" }));
  fireEvent.click(screen.getByRole("button", { name: "Yil" }));
  fireEvent.click(screen.getByRole("button", { name: "2000" }));
  fireEvent.click(screen.getByRole("button", { name:/Erkak/ }));
  fireEvent.click(screen.getByRole("button", { name:/Telegram/ }));
  fireEvent.click(screen.getByRole("button", { name:/Saqlash va davom etish/ }));
  await waitFor(() => expect(setDemographics).toHaveBeenCalledWith("2000-01-02", Gender.Male, AcquisitionSource.Telegram, null));
  expect(auth.applyUser).toHaveBeenCalledWith(updated);
});
