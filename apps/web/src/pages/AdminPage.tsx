import { Link, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { useAsync } from "@/lib/useAsync";
import { Icon } from "@/components/ui/Icon";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { AppButton, DesignCard, DesignState } from "@/components/design";
import "./AdminPage.css";

interface AdminItem {
  to: string;
  icon: string;
  title: string;
  hint: string;
  accent: string;
  superOnly?: boolean;
}

interface Kpi {
  label: string;
  value: number;
  icon: string;
  accent: string;
}

export function AdminPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.access(), []);
  // Live headline figures for the hero. `users` requires an admin (which every
  // visitor of /admin is), and one call returns all four aggregate counts.
  const { data: stats } = useAsync(() => api.admin.users(undefined, 1), []);

  if (loading) {
    return <LoadingSkeleton variant="dashboard" label={uz.admin.loading} />;
  }

  if (error || !data) {
    const forbidden = (error as { status?: number } | null)?.status === 403;
    return (
      <main className="ea-admin-hub">
        <DesignState
          role="alert"
          className={forbidden ? undefined : "ea-state--error"}
          icon={forbidden ? "lock" : "cloud_off"}
          title={forbidden ? uz.admin.forbidden : uz.admin.loadError}
          description="Admin workspace"
          action={!forbidden ? <AppButton type="button" tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton> : undefined}
        />
      </main>
    );
  }

  const canManage = data.canManageAdmins;
  const items: AdminItem[] = [
    { to: "/admin/metrics", icon: "trending_up", title: uz.admin.hub.metrics, hint: uz.admin.hub.metricsHint, accent: "metrics" },
    { to: "/admin/users", icon: "group", title: uz.admin.hub.users, hint: uz.admin.hub.usersHint, accent: "users" },
    { to: "/admin/support", icon: "support_agent", title: uz.admin.hub.support, hint: uz.admin.hub.supportHint, accent: "support" },
    { to: "/admin/vocabulary", icon: "menu_book", title: uz.admin.hub.vocabulary, hint: uz.admin.hub.vocabularyHint, accent: "vocabulary" },
    { to: "/admin/vocabulary-images", icon: "add_photo_alternate", title: "Vocabulary rasmlari", hint: "6000 rasmni English va o‘zbekcha so‘z bilan ko‘ring, mos bo‘lmaganini almashtiring", accent: "vocabulary" },
    { to: "/admin/curriculum", icon: "database", title: "Curriculum backfill", hint: "V4 generation, retry va publish holatlari", accent: "curriculum" },
    { to: "/admin/grammar", icon: "rule", title: uz.admin.hub.grammar, hint: uz.admin.hub.grammarHint, accent: "grammar" },
    { to: "/admin/listening", icon: "headphones", title: uz.admin.hub.listening, hint: uz.admin.hub.listeningHint, accent: "listening" },
    { to: "/admin/reading", icon: "auto_stories", title: uz.admin.hub.reading, hint: uz.admin.hub.readingHint, accent: "reading" },
    { to: "/admin/notifications", icon: "campaign", title: uz.admin.hub.notifications, hint: uz.admin.hub.notificationsHint, accent: "notifications", superOnly: true },
    { to: "/admin/sections", icon: "science", title: uz.admin.hub.sections, hint: uz.admin.hub.sectionsHint, accent: "sections", superOnly: true },
    { to: "/admin/server", icon: "monitor_heart", title: uz.admin.hub.server, hint: uz.admin.hub.serverHint, accent: "server", superOnly: true },
  ];
  const visible = items.filter((item) => !item.superOnly || canManage);

  const kpis: Kpi[] = stats
    ? [
        { label: uz.admin.totalUsers, value: stats.totalUsers, icon: "group", accent: "users" },
        { label: uz.admin.onboarded, value: stats.onboardedCount, icon: "rocket_launch", accent: "metrics" },
        { label: uz.admin.premium, value: stats.premiumCount, icon: "workspace_premium", accent: "notifications" },
        { label: uz.admin.admins, value: stats.adminCount, icon: "shield_person", accent: "sections" },
      ]
    : [];

  return (
    <main className="ea-admin-hub">
      <nav className="ea-admin-hub__breadcrumb" aria-label="Breadcrumb">
        <Link to="/home">EnglishAI</Link>
        <Icon name="chevron_right" />
        <span>{uz.admin.title}</span>
      </nav>

      <header className="ea-admin-hero">
        <div className="ea-admin-hero__copy">
          <span className="ea-eyebrow">{canManage ? "ADMINISTRATOR" : "BOSHQARUV"}</span>
          <h1>Boshqaruv markazi.</h1>
          <p>{uz.admin.subtitle}</p>
        </div>
        <div className="ea-admin-hero__kpis" aria-label="Asosiy ko‘rsatkichlar">
          {kpis.length > 0
            ? kpis.map((kpi) => (
                <div className="ea-admin-kpi" data-accent={kpi.accent} key={kpi.label}>
                  <span className="ea-admin-kpi__icon"><Icon name={kpi.icon} filled /></span>
                  <span className="ea-admin-kpi__copy">
                    <strong>{kpi.value.toLocaleString("uz")}</strong>
                    <small>{kpi.label}</small>
                  </span>
                </div>
              ))
            : Array.from({ length: 4 }).map((_, index) => (
                <div className="ea-admin-kpi ea-admin-kpi--loading" key={index} aria-hidden="true">
                  <span className="ea-admin-kpi__icon" />
                  <span className="ea-admin-kpi__copy"><i /><i /></span>
                </div>
              ))}
        </div>
      </header>

      <div className="ea-admin-hub__section-head">
        <div>
          <span className="ea-eyebrow">Boshqaruv markazi</span>
          <h2>Admin vositalari</h2>
        </div>
        <span className="ea-count-pill">{visible.length} modul</span>
      </div>

      <div className="ea-admin-hub__grid">
        {visible.map((item) => (
          <DesignCard
            as="button"
            type="button"
            interactive
            data-accent={item.accent}
            className="ea-admin-hub__card"
            key={item.to}
            onClick={() => navigate(item.to)}
          >
            <span className="ea-admin-hub__card-icon"><Icon name={item.icon} filled /></span>
            <span className="ea-admin-hub__card-copy">
              <span className="ea-admin-hub__card-meta">{item.superOnly ? "Super admin" : "Admin modul"}</span>
              <strong>{item.title}</strong>
              <small>{item.hint}</small>
            </span>
            <Icon name="arrow_forward" className="ea-admin-hub__card-arrow" />
          </DesignCard>
        ))}
      </div>
    </main>
  );
}
