import { useState } from "react";
import { DesignConfirm } from "@/components/design";
import { DuoButton } from "@/components/game";
import { Icon } from "@/components/ui/Icon";

export function LessonStopControl({ onConfirm, loading = false, resetScore = true }: {
  onConfirm: () => void | Promise<void>;
  loading?: boolean;
  resetScore?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState(false);

  async function confirmStop() {
    if (confirming) return;
    setConfirming(true);
    setError(false);
    try {
      await onConfirm();
    } catch {
      setError(true);
      setConfirming(false);
    }
  }

  return (
    <>
      <DuoButton color="red" size="sm" onClick={() => setOpen(true)}>
        <span className="inline-flex items-center gap-2"><Icon name="stop_circle" filled />To‘xtatish</span>
      </DuoButton>
      <DesignConfirm
        open={open}
        onClose={() => setOpen(false)}
        title="Testni to‘xtatish"
        message={<div className="space-y-3">
          <p>{resetScore === false
            ? "Joriy tugallanmagan urinish o‘chiriladi va bosh sahifaga qaytiladi. Davom etasizmi?"
            : "Joriy urinish va ushbu skill bo‘yicha saqlangan natija o‘chiriladi. Davom etasizmi?"}</p>
          {error && <p role="alert" className="font-semibold text-[var(--ea-danger)]">To‘xtatib bo‘lmadi. Internetni tekshirib, qayta urinib ko‘ring.</p>}
        </div>}
        confirmLabel="Ha, to‘xtatish"
        destructive
        loading={loading || confirming}
        onConfirm={() => void confirmStop()}
      />
    </>
  );
}
