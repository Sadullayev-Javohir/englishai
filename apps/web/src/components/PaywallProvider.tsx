import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from "react";
import { api } from "@/api/client";
import { ProductEventType } from "@/api/types";
import { getLearnerId } from "@/app/session";
import { AppButton, DesignModal } from "@/components/design";
import { Icon } from "@/components/ui/Icon";
import { getNativeCapabilities } from "@/api/nativeCapabilities";
import { uz } from "@/content/uz";

interface PaywallContextValue {
  /** Opens the upgrade paywall (e.g. when the learner taps a Pro-locked topic). */
  open: () => void;
}

const PaywallContext = createContext<PaywallContextValue>({ open: () => {} });

/** Lets any screen open the trial paywall: `const { open } = usePaywall();`. */
export function usePaywall(): PaywallContextValue {
  return useContext(PaywallContext);
}

/**
 * Hosts the trial paywall (PROJECT-SPEC H.1). It opens both on demand (a Pro-locked topic card) and
 * automatically when any API call returns 402 `subscription_required` (the global `paywall:required`
 * event from the API client) - so a learner who reaches a paywalled topic through any path is asked
 * to upgrade, and other lessons stay closed until they do.
 *
 * Online payments (Click/Payme) are not wired up yet, so the modal explains that Premium is coming
 * soon rather than starting a checkout - the server also refuses the upgrade (503 payments_unavailable)
 * so Premium is never granted without a real payment. Only the free plan (first 2 topics) is active.
 */
export function PaywallProvider({ children }: { children: React.ReactNode }) {
  const capabilities = getNativeCapabilities();
  const [isOpen, setOpen] = useState(false);

  const trackPaywallHit = useCallback(() => {
    void api.analytics
      .track(getLearnerId(), ProductEventType.PaywallHit, "paywall-modal")
      .catch(() => undefined);
  }, []);

  const open = useCallback(() => {
    if (capabilities.premiumAccess) return;
    trackPaywallHit();
    setOpen(true);
  }, [capabilities.premiumAccess, trackPaywallHit]);
  const close = useCallback(() => setOpen(false), []);

  // Auto-open when a content request is blocked by the server-side paywall.
  useEffect(() => {
    const handler = () => {
      if (capabilities.premiumAccess) return;
      trackPaywallHit();
      setOpen(true);
    };
    window.addEventListener("paywall:required", handler);
    return () => window.removeEventListener("paywall:required", handler);
  }, [capabilities.premiumAccess, trackPaywallHit]);

  return (
    <PaywallContext.Provider value={{ open }}>
      {children}
      {!capabilities.premiumAccess && <DesignModal
        open={isOpen}
        onClose={close}
        title={uz.paywall.title}
        description={uz.paywall.body}
        closeLabel={uz.common.close}
        footer={<AppButton fullWidth onClick={close}>{uz.paywall.gotIt}</AppButton>}
      >
          <div>
            <div className="flex items-center justify-center w-14 h-14 rounded-full bg-accent/15 mx-auto mb-md">
              <Icon
                name="workspace_premium"
                filled
                className="text-accent text-[32px]"
              />
            </div>

            <ul className="space-y-sm mb-lg">
              {uz.paywall.benefits.map((b) => (
                <li key={b} className="flex items-start gap-sm">
                  <Icon
                    name="check_circle"
                    filled
                    className="text-success text-[18px] mt-[2px]"
                  />
                  <span className="font-body-md text-body-md text-text-primary">
                    {b}
                  </span>
                </li>
              ))}
            </ul>

            {/* Payments not wired up yet: explain Premium is coming soon instead of a checkout. */}
            <div className="flex items-start gap-sm rounded-2xl border-2 border-outline-variant bg-accent/5 px-md py-md mb-xl">
              <Icon
                name="schedule"
                className="text-accent text-[22px] mt-[2px]"
              />
              <div>
                <p className="font-body-md text-body-md font-semibold text-text-primary mb-xs">
                  {uz.paywall.comingSoonTitle}
                </p>
                <p className="font-label-md text-label-md text-text-secondary">
                  {uz.paywall.comingSoonBody}
                </p>
              </div>
            </div>

          </div>
      </DesignModal>}
    </PaywallContext.Provider>
  );
}
