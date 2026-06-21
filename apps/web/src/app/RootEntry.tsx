import { Navigate } from "react-router-dom";
import { isNativePlatform } from "@/api/nativeAuth";
import { LandingPage } from "@/pages/LandingPage";
import { useAuth } from "./auth";

export function RootEntry() {
  const { status } = useAuth();

  if (isNativePlatform() || status === "authenticated") {
    return <Navigate to="/home" replace />;
  }

  return <LandingPage />;
}
