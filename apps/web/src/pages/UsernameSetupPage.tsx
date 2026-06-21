import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAuth } from "@/app/auth";
import { UsernameField } from "@/components/UsernameField";
import { canSubmitUsername, normalizeUsername, useUsernameCheck } from "@/lib/useUsernameCheck";
import { OnboardingChrome, PlayButton, PlayChip, PlayCompanion } from "./onboarding/OnboardingChrome";

export function UsernameSetupPage() {
  const navigate = useNavigate();
  const { user, applyUser } = useAuth();

  const [value, setValue] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const status = useUsernameCheck(value);
  const canSubmit = canSubmitUsername(status, value) && !saving;

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (!canSubmit || !user) return;

    setSaving(true);
    setError(null);
    try {
      const updated = await api.auth.updateProfile(user.displayName, normalizeUsername(value));
      applyUser(updated);
      navigate("/onboarding/profile-details", { replace: true });
    } catch {
      setError(uz.username.saveError);
      setSaving(false);
    }
  }

  return (
    <OnboardingChrome className="username-setup" step={1} backTo="/login" testId="username-setup-viewport">
      <div className="play-username">
        <PlayCompanion message="Tanishganimdan xursandman!" />
        <form onSubmit={submit} className="play-username__form">
          <h1 className="onboarding-play__title">Tanishib olaylik.</h1>
          <div className="play-username__profile">
            <span className="play-username__initial">{(value.trim() || user?.displayName || "?").slice(0, 1).toUpperCase()}</span>
            <PlayChip tone="green">Yangi o‘rganuvchi</PlayChip>
          </div>
          <UsernameField variant="setup" value={value} onChange={(next) => { setValue(next); setError(null); }} status={status} />
          <p className="play-username__hint">Lotin harflari, raqam va _ ishlating.</p>
          {error && <p role="alert" className="onboarding-play__alert"><Icon name="error" />{error}</p>}
          <PlayButton type="submit" disabled={!canSubmit} arrow={!saving}>{saving ? uz.username.saving : uz.username.continue}</PlayButton>
          <p className="onboarding-play__note">Keyinroq profilingizda o‘zgartira olasiz.</p>
        </form>
      </div>
    </OnboardingChrome>
  );
}
