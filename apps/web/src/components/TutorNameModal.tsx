import { useState } from "react";
import { api, ApiError } from "@/api/client";
import { useAuth } from "@/app/auth";
import { AppButton, DesignInput, DesignModal, FormField } from "@/components/design";
import { uz } from "@/content/uz";

/**
 * One-time prompt that collects the name the AI speaking tutor should use. The tutor is
 * instructed never to invent a name, so when the account has no `preferredName` we ask for it
 * here (a modal with an input + Save) instead of letting the AI ask in the chat. Once saved it
 * is stored on the account and the prompt never appears again; the learner may skip for now and
 * the tutor then simply uses no name. All wording lives in the content store (rule 11).
 */
export function TutorNameModal() {
  const { user, applyUser } = useAuth();
  const [name, setName] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(false);
  // Lets the learner postpone for this visit without being trapped; it returns next session
  // (the account still has no name) until they save one.
  const [dismissed, setDismissed] = useState(false);

  // Show only once we know the user and they have not provided a tutor name yet.
  if (!user || user.preferredName || dismissed) return null;

  const trimmed = name.trim();

  async function save() {
    if (!trimmed || saving) return;
    setSaving(true);
    setError(false);
    try {
      const updated = await api.auth.setPreferredName(trimmed);
      applyUser(updated); // refresh the cached user so the modal closes and the tutor gets the name
    } catch (err) {
      // A 400 (too long/blank) is a real input problem; anything else is transient - let them retry.
      setError(err instanceof ApiError && err.status === 400);
      setSaving(false);
    }
  }

  return (
    <DesignModal
      open
      onClose={() => setDismissed(true)}
      title={uz.speaking.nameModal.title}
      description={uz.speaking.nameModal.subtitle}
      closeLabel={uz.common.close}
      footer={
        <div className="flex w-full flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <AppButton tone="standard" onClick={() => setDismissed(true)} disabled={saving}>{uz.speaking.nameModal.skip}</AppButton>
          <AppButton leadingIcon="check" onClick={() => void save()} disabled={!trimmed} loading={saving}>{uz.speaking.nameModal.save}</AppButton>
        </div>
      }
    >
      <FormField label={uz.speaking.nameModal.placeholder} htmlFor="tutor-preferred-name" error={error ? uz.speaking.nameModal.error : undefined}>
        <DesignInput
          id="tutor-preferred-name"
          value={name}
          autoFocus
          maxLength={40}
          onChange={(event) => {
            setName(event.target.value);
            setError(false);
          }}
          onKeyDown={(event) => {
            if (event.key === "Enter") void save();
          }}
          placeholder={uz.speaking.nameModal.placeholder}
          aria-describedby={error ? "tutor-preferred-name-message" : undefined}
        />
      </FormField>
    </DesignModal>
  );
}
