import { useState } from "react";
import { api } from "@/api/client";
import type { SubscriptionPlan } from "@/api/types";
import { uz } from "@/content/uz";

type Status = "idle" | "sending" | "done" | "error";

/**
 * Captures interest while checkout is closed.
 *
 * A visitor who leaves a contact after seeing the price is the only real demand signal available
 * before there is anything to buy - far better than a pageview, and it is the list to email first
 * on the day a payment provider goes live.
 */
export function PremiumWaitlistForm({
  interestedPlan,
  source,
}: {
  interestedPlan?: SubscriptionPlan;
  source?: string;
}) {
  const [contact, setContact] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const copy = uz.waitlist;

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (!contact.trim() || status === "sending") return;

    setStatus("sending");
    try {
      await api.subscription.joinWaitlist({ contact: contact.trim(), interestedPlan, source });
      setStatus("done");
      setContact("");
    } catch {
      setStatus("error");
    }
  }

  if (status === "done") {
    return (
      <p className="pc-waitlist__done" role="status">
        {copy.thanks}
      </p>
    );
  }

  return (
    <form className="pc-waitlist" onSubmit={submit}>
      <label className="pc-waitlist__label" htmlFor="waitlist-contact">
        {copy.label}
      </label>
      <div className="pc-waitlist__row">
        <input
          id="waitlist-contact"
          className="pc-waitlist__input"
          type="text"
          inputMode="email"
          autoComplete="email"
          placeholder={copy.placeholder}
          value={contact}
          maxLength={128}
          onChange={(event) => setContact(event.target.value)}
          aria-describedby="waitlist-hint"
        />
        <button
          className="pc-waitlist__submit"
          type="submit"
          disabled={status === "sending" || contact.trim().length === 0}
        >
          {status === "sending" ? copy.sending : copy.submit}
        </button>
      </div>
      <p className="pc-waitlist__hint" id="waitlist-hint">
        {status === "error" ? copy.error : copy.hint}
      </p>
    </form>
  );
}
