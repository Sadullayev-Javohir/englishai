import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type { EnergyAction, EnergyDto } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { EnergyModal, type PendingEnergyAction } from "./EnergyModal";

interface EnergyContextValue {
  energy: EnergyDto | null;
  loading: boolean;
  consume: (action: EnergyAction, referenceId: string) => Promise<EnergyDto>;
  refresh: () => Promise<void>;
  openEnergyModal: (pending?: PendingEnergyAction) => void;
  closeEnergyModal: () => void;
}

const EnergyContext = createContext<EnergyContextValue | null>(null);

export function EnergyProvider({ children }: { children: React.ReactNode }) {
  const learnerId = getLearnerId();
  const navigate = useNavigate();
  const [energy, setEnergy] = useState<EnergyDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState<PendingEnergyAction | undefined>();
  const [isModalOpen, setModalOpen] = useState(false);

  const refresh = useCallback(async () => {
    try {
      setEnergy(await api.gamification.energy(learnerId));
    } catch {
      return;
    } finally {
      setLoading(false);
    }
  }, [learnerId]);

  useEffect(() => {
    void refresh();
    const onVisible = () => {
      if (document.visibilityState === "visible") void refresh();
    };
    window.addEventListener("focus", onVisible);
    document.addEventListener("visibilitychange", onVisible);
    return () => {
      window.removeEventListener("focus", onVisible);
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, [refresh]);

  useEffect(() => {
    if (!energy) return;
    if (!energy.nextRefillAt) return;
    const delay = Math.max(1000, new Date(energy.nextRefillAt).getTime() - Date.now() + 250);
    const timeout = window.setTimeout(() => void refresh(), delay);
    return () => window.clearTimeout(timeout);
  }, [energy, refresh]);

  const consume = useCallback(async (action: EnergyAction, referenceId: string) => {
    const result = await api.gamification.consumeEnergy(learnerId, action, referenceId);
    setEnergy(result);
    return result;
  }, [learnerId]);

  const openEnergyModal = useCallback((nextPending?: PendingEnergyAction) => {
    setPending(nextPending);
    setModalOpen(true);
  }, []);
  const closeEnergyModal = useCallback(() => {
    setModalOpen(false);
    setPending(undefined);
  }, []);

  useEffect(() => {
    const handleExhausted = () => {
      void refresh();
      openEnergyModal();
    };
    window.addEventListener("energy:exhausted", handleExhausted);
    return () => window.removeEventListener("energy:exhausted", handleExhausted);
  }, [openEnergyModal, refresh]);

  const handlePrimary = useCallback(() => {
    const resume = pending?.resume;
    closeEnergyModal();
    if (resume) {
      void Promise.resolve(resume());
      return;
    }
    navigate("/video");
  }, [closeEnergyModal, navigate, pending]);

  const value = useMemo(
    () => ({ energy, loading, consume, refresh, openEnergyModal, closeEnergyModal }),
    [closeEnergyModal, consume, energy, loading, openEnergyModal, refresh],
  );

  return (
    <EnergyContext.Provider value={value}>
      {children}
      <EnergyModal
        open={isModalOpen}
        energy={energy}
        pending={pending}
        onClose={closeEnergyModal}
        onPrimary={handlePrimary}
      />
    </EnergyContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useEnergy(): EnergyContextValue {
  const context = useContext(EnergyContext);
  if (!context) throw new Error("useEnergy must be used within EnergyProvider");
  return context;
}
