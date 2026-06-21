import { useLocation, useNavigate } from "react-router-dom";
import { AppButton } from "@/components/design";
import { uz } from "@/content/uz";
import { routeReturnTarget } from "@/lib/routeReturn";

export function HomeBackButton({ className }: { className?: string }) {
  const navigate = useNavigate();
  const location = useLocation();

  return (
    <AppButton
      tone="standard"
      leadingIcon="arrow_back"
      onClick={() => navigate(routeReturnTarget(location))}
      className={className}
    >
      {uz.common.back}
    </AppButton>
  );
}
