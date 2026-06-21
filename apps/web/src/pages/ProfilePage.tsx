import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { motion } from "framer-motion";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { getLearnerId, getStoredLevel } from "@/app/session";
import { useAuth } from "@/app/auth";
import { useAsync } from "@/lib/useAsync";
import { CefrLevel, PaymentProvider, SubscriptionPlan, type PaymentProviderDto } from "@/api/types";
import { Icon } from "@/components/ui/Icon";
import { AppButton, DesignModal } from "@/components/design";
import { UserAvatar } from "@/components/UserAvatar";
import { ReferralSection } from "@/components/ReferralSection";
import { cn } from "@/lib/cn";
import { cefrLong } from "@/lib/labels";
import { formatProTrialDate } from "@/lib/proTrial";
import "./ProfilePage.css";

interface PremiumPlan { key: string; label: string; price: string; perMonth: string; badge?: string; best?: boolean; }
const KEY_BY_PLAN: Record<SubscriptionPlan, string> = { [SubscriptionPlan.Monthly]: "monthly", [SubscriptionPlan.Quarterly]: "quarterly", [SubscriptionPlan.SemiAnnual]: "semiannual", [SubscriptionPlan.Yearly]: "yearly" };
const PLAN_BY_KEY: Record<string, SubscriptionPlan> = { monthly: SubscriptionPlan.Monthly, quarterly: SubscriptionPlan.Quarterly, semiannual: SubscriptionPlan.SemiAnnual, yearly: SubscriptionPlan.Yearly };

export function ProfilePage() {
  const navigate = useNavigate();
  const { user, signOut, applyUser } = useAuth();
  const learnerId = getLearnerId();
  const { data: status } = useAsync(() => api.gamification.status(learnerId), [learnerId]);
  const { data: subscription } = useAsync(() => api.subscription.get(learnerId), [learnerId]);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const [avatarBusy, setAvatarBusy] = useState(false);
  const [avatarMessage, setAvatarMessage] = useState<string | null>(null);
  const [checkoutPlan, setCheckoutPlan] = useState<SubscriptionPlan | null>(null);
  const [paymentProviders, setPaymentProviders] = useState<PaymentProviderDto[]>([]);
  const [checkoutBusy, setCheckoutBusy] = useState(false);
  const [checkoutError, setCheckoutError] = useState<string | null>(null);
  const [subscriptionOpen, setSubscriptionOpen] = useState(false);
  const [referralOpen, setReferralOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const level = getStoredLevel() || CefrLevel.A2;
  const isPremium = subscription?.isPremiumActive ?? false;
  const isTrialActive = subscription?.isTrialActive ?? false;
  const currentKey = isPremium && subscription?.plan != null ? KEY_BY_PLAN[subscription.plan] : "free";
  const currentPlan = currentKey === "free" ? uz.profile.planFreemiumLabel : (uz.paywall.plans as readonly PremiumPlan[]).find((plan) => plan.key === currentKey)?.label ?? uz.profile.planFreemiumLabel;
  const daysLeft = subscription?.daysUntilExpiry ?? null;
  const trialExpiryLabel = subscription?.trialExpiresAt ? formatProTrialDate(subscription.trialExpiresAt) : null;
  const streak = status?.currentStreak ?? 0;
  const longestStreak = status?.longestStreak ?? 0;
  const goalTotal = status?.dailyGoalTarget ?? 0;
  const goalDone = goalTotal > 0 ? Math.min(status?.todayCompletedTasks ?? 0, goalTotal) : 0;
  const showRemoveAvatar = user?.pictureUrl?.startsWith("/api/auth/avatar/") ?? false;

  async function changeAvatar(file?: File) {
    if (!file || avatarBusy) return;
    if (!file.type.match(/^image\/(jpeg|png|webp)$/) || file.size > 5 * 1024 * 1024) { setAvatarMessage("JPEG, PNG yoki WebP rasm tanlang. Maksimal hajm 5 MB."); return; }
    setAvatarBusy(true); setAvatarMessage(null);
    try { applyUser(await api.auth.updateAvatar(file)); setAvatarMessage("Profil rasmi yangilandi."); }
    catch { setAvatarMessage("Profil rasmini yuklab bo‘lmadi."); }
    finally { setAvatarBusy(false); if (avatarInputRef.current) avatarInputRef.current.value = ""; }
  }
  async function removeAvatar() {
    if (avatarBusy) return;
    setAvatarBusy(true); setAvatarMessage(null);
    try { applyUser(await api.auth.deleteAvatar()); setAvatarMessage("Profil rasmi o‘chirildi."); }
    catch { setAvatarMessage("Profil rasmini o‘chirib bo‘lmadi."); }
    finally { setAvatarBusy(false); }
  }
  async function openCheckout(plan: SubscriptionPlan) {
    setSubscriptionOpen(false); setCheckoutPlan(plan); setCheckoutError(null);
    try { setPaymentProviders(await api.subscription.providers()); }
    catch { setCheckoutError("To‘lov tizimlarini yuklab bo‘lmadi."); }
  }
  async function startCheckout(provider: PaymentProvider) {
    if (checkoutPlan == null || checkoutBusy) return;
    setCheckoutBusy(true); setCheckoutError(null);
    try { const checkout = await api.subscription.start(learnerId, checkoutPlan, provider); window.location.assign(checkout.checkoutUrl); }
    catch { setCheckoutError("To‘lovni boshlashning iloji bo‘lmadi. Sozlamalarni tekshiring."); setCheckoutBusy(false); }
  }
  async function logout() { await signOut(); navigate("/login", { replace: true }); }

  return <div className="profile-page w-full"><div className="profile-page__content">
    <header className="profile-page__heading"><span>SHAXSIY KABINET</span><h1>Profil va sozlamalar</h1><p>Shaxsiy ma’lumotlar, takliflar va yordam — bir joyda.</p></header>
    <motion.section initial={{ opacity: 0, y: 14 }} animate={{ opacity: 1, y: 0 }} transition={{ type: "spring", stiffness: 220, damping: 24 }} className="profile-page__hero" aria-label="Profil umumiy ma’lumotlari">
      <div className="profile-page__hero-identity"><div className="profile-page__identity-line">
        <UserAvatar pictureUrl={user?.pictureUrl} name={user?.displayName || user?.email} alt={user?.displayName || "Profil rasmi"} className="profile-page__avatar" fallbackClassName="profile-page__avatar-fallback" />
        <div className="profile-page__hero-copy"><h2>{user?.displayName || uz.brand}</h2><p>{user?.username ? `@${user.username}` : user?.email}</p><span><Icon name="verified" filled />{cefrLong(level)}</span></div>
      </div><div className="profile-page__hero-actions"><input ref={avatarInputRef} type="file" accept="image/jpeg,image/png,image/webp" aria-label="Profil rasmini tanlash" className="sr-only" onChange={(event) => void changeAvatar(event.target.files?.[0])} /><button type="button" className="profile-page__avatar-button" disabled={avatarBusy} onClick={() => avatarInputRef.current?.click()}><Icon name="photo_camera" />{avatarBusy ? "Yuklanmoqda..." : "Rasm tanlash"}</button>{showRemoveAvatar && <button type="button" className="profile-page__avatar-remove" aria-label="Profil rasmini o‘chirish" title="Profil rasmini o‘chirish" disabled={avatarBusy} onClick={() => void removeAvatar()}><Icon name="delete" /></button>}</div>{avatarMessage && <p role="status" className="profile-page__avatar-message">{avatarMessage}</p>}</div>
      <div className="profile-page__hero-progress"><div className="profile-page__hero-progress-copy"><h3>Har kuni bir qadam oldinga.</h3><p>Kichik odatlar — katta natijalar.</p></div><div className="profile-page__stats"><ProfileStat icon="local_fire_department" label="KUNLIK STREAK" value={uz.profile.days(streak)} /><ProfileStat icon="military_tech" label="ENG UZUN STREAK" value={uz.profile.days(longestStreak)} /><ProfileStat icon="track_changes" label="BUGUNGI MAQSAD" value={`${goalDone} / ${goalTotal || "-"}`} /></div></div>
    </motion.section>
    <div className="profile-page__columns">
      <ProfileCard eyebrow="HISOB" title="Shaxsiy ma’lumotlar" icon="manage_accounts" className="profile-page__account-card"><div className="profile-page__name-grid"><div><small>Ism</small><strong>{user?.displayName ?? "-"}</strong></div><div><small>Foydalanuvchi nomi</small><strong>{user?.username ? `@${user.username}` : "-"}</strong></div></div><ProfileInfoRow icon="mail" label="Google hisobi" value={user?.email ?? "-"} /><button type="button" className="profile-page__subscription-row" onClick={() => setSubscriptionOpen(true)}><span><small>OBUNA</small><strong>{currentPlan}</strong></span><b><Icon name="check_circle" filled />Faol</b><Icon name="chevron_right" /></button><button type="button" className="profile-page__outline-button" onClick={() => navigate("/onboarding/profile-details?edit=1")}><Icon name="edit" />Ma’lumotlarni tahrirlash</button></ProfileCard>
      <ProfileCard eyebrow="KO‘PROQ" title="Taklif va yordam" icon="widgets" className="profile-page__more-card"><button type="button" className="profile-page__more-row" onClick={() => setReferralOpen(true)}><span className="profile-page__row-icon"><Icon name="group_add" /></span><span><small>DO‘STLARNI TAKLIF QILING</small><strong>Referral kodi va bonuslar</strong></span><Icon name="chevron_right" /></button><button type="button" className="profile-page__more-row" onClick={() => navigate("/support")}><span className="profile-page__row-icon"><Icon name="headset_mic" /></span><span><small>YORDAM VA SUPPORT</small><strong>Savolingiz bormi? Bizga yozing.</strong></span><Icon name="chevron_right" /></button><button type="button" className="profile-page__more-row" onClick={() => navigate("/landing")}><span className="profile-page__row-icon"><Icon name="info" /></span><span><small>LOYIHA HAQIDA</small><strong>EnglishAI bilan tanishing</strong></span><Icon name="chevron_right" /></button></ProfileCard>
    </div>
    <footer className="profile-page__actions"><button type="button" className="profile-page__delete-button" onClick={() => setDeleteOpen(true)}><Icon name="delete" />Hisobni o‘chirish</button><button type="button" className="profile-page__signout-button" onClick={() => void logout()}><Icon name="logout" />{uz.profile.logout}</button></footer><p className="profile-page__footer">EnglishAI · Siz bilan har qadamda</p>
  </div>
  <DesignModal open={subscriptionOpen} onClose={() => setSubscriptionOpen(false)} title="Obuna" description={isTrialActive ? (trialExpiryLabel ? uz.profile.trialUntil(trialExpiryLabel) : uz.profile.trialBody) : daysLeft != null ? uz.profile.expiresInDays(daysLeft) : "Tarifingiz va mos rejalaringizni boshqaring."} closeLabel="Obuna oynasini yopish" className="profile-subscription-modal"><div className="profile-page__plans">{(uz.paywall.plans as readonly PremiumPlan[]).map((plan) => <PlanCard key={plan.key} label={plan.label} price={plan.price} perMonth={plan.perMonth} best={plan.best} isCurrent={currentKey === plan.key} daysLeft={currentKey === plan.key ? daysLeft : undefined} onBuy={() => void openCheckout(PLAN_BY_KEY[plan.key])} />)}</div></DesignModal>
  <DesignModal open={referralOpen} onClose={() => setReferralOpen(false)} title="Do‘stlarni taklif qilish" description="Referral kodingizni ulashing va bonuslarni kuzating." closeLabel="Taklif oynasini yopish"><ReferralSection /></DesignModal>
  <DesignModal open={deleteOpen} onClose={() => setDeleteOpen(false)} title={uz.profile.dangerZone} description={uz.profile.deleteAccountWarning} closeLabel="Hisobni o‘chirish oynasini yopish" className="profile-delete-modal"><DeleteAccountSection /></DesignModal>
  <DesignModal open={checkoutPlan != null} onClose={() => setCheckoutPlan(null)} title="To‘lov tizimini tanlang" description="Hozir mock rejimida xavfsiz sinab ko‘rish mumkin." closeLabel="To‘lov oynasini yopish" className="profile-checkout-modal"><div className="grid gap-3">{paymentProviders.map((provider) => <AppButton key={provider.provider} type="button" tone="standard" size="lg" fullWidth disabled={checkoutBusy} onClick={() => void startCheckout(provider.provider)} className="justify-between"><span>{provider.name === "Uzum" ? "Uzum Bank" : provider.name}</span><span className="text-xs text-ea-primary">{provider.isMock ? "TEST" : "DAVOM ETISH"}</span></AppButton>)}</div>{checkoutError && <p className="mt-4 rounded-xl border border-ea-danger-200 bg-ea-danger-50 p-3 text-sm font-bold text-ea-danger-700">{checkoutError}</p>}</DesignModal>
  </div>;
}

function ProfileStat({ icon, label, value }: { icon: string; label: string; value: string }) { return <div className="profile-page__stat"><Icon name={icon} filled /><span><small>{label}</small><strong>{value}</strong></span></div>; }
function ProfileCard({ eyebrow, title, icon, className, children }: { eyebrow: string; title: string; icon: string; className?: string; children: React.ReactNode }) { return <section className={cn("profile-page__card", className)}><header><span><Icon name={icon} /></span><div><small>{eyebrow}</small><h2>{title}</h2></div></header>{children}</section>; }
function ProfileInfoRow({ icon, label, value }: { icon: string; label: string; value: string }) { return <div className="profile-page__info-row"><span className="profile-page__row-icon"><Icon name={icon} /></span><span><small>{label}</small><strong>{value}</strong></span></div>; }
function PlanCard({ label, price, perMonth, best, isCurrent, daysLeft, onBuy }: PremiumPlan & { isCurrent: boolean; daysLeft?: number | null; onBuy?: () => void }) { return <div className={cn("profile-page__plan-card", isCurrent && "is-current")}>{best && !isCurrent && <span className="profile-page__plan-tag">{uz.paywall.bestLabel}</span>}<div className="flex items-center justify-between gap-2"><h4>{label}</h4>{isCurrent && <span className="profile-page__plan-current-tag"><Icon name="check" className="text-[14px]" />{uz.profile.currentPlan}</span>}</div><p className="profile-page__plan-price">{price}</p><p className="profile-page__plan-per">{perMonth}</p><div className="profile-page__plan-cta">{isCurrent ? <p className="text-center text-sm font-bold text-[color:var(--pf-muted)]">{daysLeft != null ? uz.profile.expiresInDays(daysLeft) : "-"}</p> : <button type="button" onClick={onBuy} className="profile-page__primary-button w-full">Sotib olish</button>}</div></div>; }

type DeleteStep = "idle" | "email" | "confirm" | "final";
function DeleteAccountSection() {
  const navigate = useNavigate(); const { user, signOut } = useAuth(); const [step, setStep] = useState<DeleteStep>("idle"); const [emailInput, setEmailInput] = useState(""); const [deleting, setDeleting] = useState(false); const [error, setError] = useState<string | null>(null);
  const accountEmail = (user?.email ?? "").trim().toLowerCase(); const emailMatches = accountEmail.length > 0 && emailInput.trim().toLowerCase() === accountEmail;
  function reset() { setStep("idle"); setEmailInput(""); setError(null); setDeleting(false); }
  async function confirmDelete() { if (deleting) return; setDeleting(true); setError(null); try { await api.auth.deleteAccount(emailInput.trim()); await signOut(); navigate("/login", { replace: true }); } catch { setError(uz.profile.deleteError); setDeleting(false); } }
  return <div className="profile-page__danger">{step === "idle" && <button type="button" onClick={() => setStep("email")} className="profile-page__danger-btn"><Icon name="delete_forever" />{uz.profile.deleteAccountButton}</button>}{step === "email" && <div className="grid gap-4"><div><label htmlFor="deleteEmail" className="profile-page__label">{uz.profile.deleteEmailPrompt}</label><input id="deleteEmail" type="email" autoComplete="off" value={emailInput} onChange={(e) => setEmailInput(e.target.value)} placeholder={uz.profile.deleteEmailPlaceholder} className="profile-page__input" /></div>{emailInput.trim().length > 0 && !emailMatches && <p className="profile-page__error">{uz.profile.deleteEmailMismatch}</p>}<div className="profile-page__actions-row"><button type="button" onClick={() => setStep("confirm")} disabled={!emailMatches} className="profile-page__danger-btn">{uz.profile.deleteContinue}</button><button type="button" onClick={reset} className="profile-page__outline-button">{uz.profile.cancelDelete}</button></div></div>}{step === "confirm" && <div className="grid gap-4"><p>{uz.profile.deleteConfirmQuestion}</p><div className="profile-page__actions-row"><button type="button" onClick={() => setStep("final")} className="profile-page__danger-btn">{uz.profile.deleteConfirmYes}</button><button type="button" onClick={reset} className="profile-page__outline-button">{uz.profile.deleteConfirmNo}</button></div></div>}{step === "final" && <div className="grid gap-4">{error && <p className="profile-page__error">{error}</p>}<div className="profile-page__actions-row"><button type="button" onClick={() => void confirmDelete()} disabled={deleting} className="profile-page__danger-btn profile-page__danger-btn--solid"><Icon name="delete_forever" />{deleting ? uz.profile.deleting : uz.profile.deleteFinal}</button>{!deleting && <button type="button" onClick={reset} className="profile-page__outline-button">{uz.profile.cancelDelete}</button>}</div></div>}</div>;
}
