import { useNavigate, useParams } from "react-router-dom";
import { api } from "@/api/client";
import { AdminRole } from "@/api/types";
import type { AdminUserDetailDto, AdminUserDto } from "@/api/types";
import { useAsync } from "@/lib/useAsync";
import { useDocumentTitle } from "@/app/documentTitle";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import {
  AppButton,
  DesignCard,
  DesignState,
  PageHeader,
  SectionHeader,
  StatCard,
} from "@/components/design";
import { formatDateTime } from "@/lib/labels";
import "./AdminUserDetailPage.css";

export function AdminUserDetailPage() {
  const { userId = "" } = useParams();
  const { data, loading, error, reload } = useAsync(async () => {
    try {
      return await api.admin.user(userId);
    } catch (detailError) {
      const users = await api.admin.users(undefined, 100);
      const fallback = users.users.find((user) => user.id === userId);
      if (!fallback) throw detailError;
      return toFallbackDetail(fallback);
    }
  }, [userId], Boolean(userId));

  useDocumentTitle(data?.displayName || data?.email, "Foydalanuvchi");

  if (loading) return <LoadingSkeleton variant="dashboard" />;
  if (error || !data) {
    return <DetailState title="Foydalanuvchi ma'lumotlarini yuklab bo'lmadi" action={reload} />;
  }

  return (
    <main className="admin-user-detail">
      <Hero user={data} />

      <section className="admin-user-detail__summary" aria-label="Foydalanuvchi holati">
        <StatCard icon="badge" label="Rol" value={roleLabel(data.role)} />
        <StatCard icon="school" label="Daraja" value={data.level ?? "Boshlamagan"} />
        <StatCard icon="workspace_premium" label="Obuna" value={data.subscriptionStatus} />
        <StatCard icon="history" label="Faolliklar" value={String(data.activityCount)} />
      </section>

      <LearningOverview user={data} />

      <div className="admin-user-detail__grid">
        <InfoSection
          title="Hisob ma'lumotlari"
          icon="person"
          items={[
            ["ID", data.id],
            ["Email", data.email],
            ["Google nomi", data.displayName],
            ["Tanlangan ism", data.preferredName],
            ["Username", data.username ? `@${data.username}` : null],
            ["Ro'yxatdan o'tgan", dateTime(data.registeredAt)],
            ["Oxirgi kirish", dateTime(data.lastLoginAt)],
            ["Tug'ilgan sana", data.birthDate],
            ["Jins", data.gender === "Male" ? "Erkak" : data.gender === "Female" ? "Ayol" : null],
            ["Eshitish manbasi", acquisitionSourceLabel(data.acquisitionSource, data.acquisitionSourceOther)],
          ]}
        />
        <InfoSection
          title="O'quv profili"
          icon="school"
          items={[
            ["Onboarding", data.hasOnboarded ? "Yakunlangan" : "Boshlanmagan"],
            ["CEFR daraja", data.level],
            ["O'rganish maqsadi", data.learningGoal],
            ["Profil yaratilgan", dateTime(data.profileCreatedAt)],
            ["Profil yangilangan", dateTime(data.profileUpdatedAt)],
            ["Oxirgi faollik", dateTime(data.lastActivityAt)],
            ["Tasdiqlash testi", dateTime(data.confirmationTestPassedAt)],
            ["Win-back holati", data.lastWinBackStage],
          ]}
        />
        <InfoSection
          title="Progress statistikasi"
          icon="analytics"
          items={[
            ["Skill baseline soni", String(data.skillSeedCount)],
            ["Faollik yozuvlari", String(data.activityCount)],
            ["Xato kuzatuvlari", String(data.errorObservationCount)],
          ]}
        />
        <InfoSection
          title="Obuna va trial"
          icon="payments"
          items={[
            ["Obuna holati", data.subscriptionStatus],
            ["Tarif", data.subscriptionPlan],
            ["Obuna tugashi", dateTime(data.subscriptionExpiresAt)],
            ["Obuna yaratilgan", dateTime(data.subscriptionCreatedAt)],
            ["Obuna yangilangan", dateTime(data.subscriptionUpdatedAt)],
            ["Pro trial", data.isProTrialActive ? "Faol" : "Tugagan"],
            ["Trial tugashi", dateTime(data.proTrialExpiresAt)],
          ]}
        />
      </div>
    </main>
  );
}

function LearningOverview({ user }: { user: AdminUserDetailDto }) {
  const learning = user.learning;
  if (!learning) return null;

  return (
    <DesignCard as="section" className="admin-user-detail__learning">
      <SectionHeader title="To'liq o'quv analitikasi" />
      <div className="admin-user-detail__metrics">
        <StatCard label="Umumiy vaqt" value={duration(learning.study.totalSeconds)} padding="sm" />
        <StatCard label="Haftalik vaqt" value={duration(learning.study.weekSeconds)} padding="sm" />
        <StatCard label="Joriy streak" value={`${learning.gamification.currentStreak} kun`} padding="sm" />
        <StatCard label="Eng uzun streak" value={`${learning.gamification.longestStreak} kun`} padding="sm" />
        <StatCard label="Saqlangan so'zlar" value={String(learning.vocabulary.total)} padding="sm" />
        <StatCard label="Takrorlashga tayyor" value={String(learning.vocabulary.due)} padding="sm" />
        <StatCard label="Mastered mavzular" value={String(learning.topics.masteredTopics)} padding="sm" />
        <StatCard label="Churn risk" value={learning.churn.overallRisk} padding="sm" />
      </div>

      <div className="admin-user-detail__skills">
        {learning.skills.map((skill) => (
          <DesignCard as="article" padding="sm" key={skill.skill}>
            <header><strong>{skill.skill}</strong><span>{Math.round(skill.score)}%</span></header>
            <div><i style={{ width: `${Math.max(0, Math.min(100, skill.score))}%` }} /></div>
            <small>{skill.sampleCount} ta namuna · {skill.eightWeekDelta >= 0 ? "+" : ""}{skill.eightWeekDelta.toFixed(1)} / 8 hafta</small>
          </DesignCard>
        ))}
      </div>

      <div className="admin-user-detail__learning-grid">
        <InfoSection
          title="Vocabulary va SRS"
          icon="translate"
          items={[
            ["O'rganilmoqda", String(learning.vocabulary.learning)],
            ["O'zlashtirilgan", String(learning.vocabulary.mastered)],
            ["7 kunda qo'shilgan", String(learning.vocabulary.addedLast7Days)],
            ["30 kunda qo'shilgan", String(learning.vocabulary.addedLast30Days)],
            ["Jami xato review", String(learning.vocabulary.totalFailCount)],
          ]}
        />
        <InfoSection
          title="Qurilmalar va retention"
          icon="devices"
          items={[
            ["Ro'yxatdan o'tgan qurilmalar", String(learning.registeredDeviceCount)],
            ["Platformalar", learning.devicePlatforms.join(", ")],
            ["Risk holati", learning.churn.isAtRisk ? "Xavf mavjud" : "Barqaror"],
            ["Risk signallari", learning.churn.signals.map((signal) => signal.type).join(", ")],
          ]}
        />
      </div>
    </DesignCard>
  );
}

function Hero({ user }: { user: AdminUserDetailDto }) {
  const navigate = useNavigate();
  return (
    <DesignCard className="admin-user-detail__hero">
      <div className="admin-user-detail__avatar">
        {user.pictureUrl ? <img src={user.pictureUrl} alt="" /> : (user.displayName || user.email).charAt(0).toUpperCase()}
      </div>
      <PageHeader
        eyebrow="Foydalanuvchi profili"
        title={user.displayName || user.email}
        description={user.email}
        actions={<AppButton type="button" tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin/users")}>Orqaga</AppButton>}
      />
    </DesignCard>
  );
}

function InfoSection({ title, icon, items }: { title: string; icon: string; items: Array<[string, string | null]> }) {
  return (
    <DesignCard as="section" className="admin-user-detail__panel">
      <header><Icon name={icon} filled /><h2>{title}</h2></header>
      <dl>
        {items.map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{value || "-"}</dd></div>)}
      </dl>
    </DesignCard>
  );
}

function DetailState({ title, action }: { title: string; action?: () => void }) {
  const navigate = useNavigate();
  return (
    <main className="admin-user-detail">
      <DesignState
        role="alert"
        className="ea-state--error"
        icon="cloud_off"
        title={title}
        action={(
          <div className="admin-user-detail__state-actions">
            <AppButton type="button" tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin/users")}>Foydalanuvchilarga qaytish</AppButton>
            {action && <AppButton type="button" tone="primary" leadingIcon="refresh" onClick={action}>Qayta urinish</AppButton>}
          </div>
        )}
      />
    </main>
  );
}

function dateTime(value: string | null) {
  return value ? formatDateTime(value) : null;
}

function acquisitionSourceLabel(source: string | null, other: string | null) {
  switch (source) {
    case "Telegram": return "Telegram";
    case "Instagram": return "Instagram";
    case "Google": return "Google";
    case "AiAssistants": return "AI yordamchilari";
    case "FriendReferral": return "Do'st-tanish";
    case "Other": return other || "Boshqa";
    default: return null;
  }
}

function roleLabel(role: AdminRole) {
  return role === AdminRole.SuperAdmin ? "Super admin" : role === AdminRole.Admin ? "Admin" : "O'quvchi";
}

function duration(seconds: number) {
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);
  return hours ? `${hours} soat ${minutes} daqiqa` : `${minutes} daqiqa`;
}

function toFallbackDetail(user: AdminUserDto): AdminUserDetailDto {
  return {
    ...user,
    preferredName: null,
    proTrialExpiresAt: user.registeredAt,
    isProTrialActive: false,
    learningGoal: null,
    birthDate: null,
    gender: null,
    acquisitionSource: null,
    acquisitionSourceOther: null,
    profileCreatedAt: null,
    profileUpdatedAt: null,
    confirmationTestPassedAt: null,
    lastWinBackStage: null,
    skillSeedCount: 0,
    activityCount: 0,
    errorObservationCount: 0,
    subscriptionCreatedAt: null,
    subscriptionUpdatedAt: null,
    learning: null as unknown as AdminUserDetailDto["learning"],
  };
}
