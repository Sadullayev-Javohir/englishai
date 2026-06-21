import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { uz } from "@/content/uz";
import { api } from "@/api/client";
import { Icon } from "@/components/ui/Icon";
import {
  AppButton,
  DesignCard,
  PageHeader,
  SectionHeader,
  StatCard,
} from "@/components/design";
import { tapLight } from "@/lib/haptics";
import "./AdminSectionsPage.css";

const SECTION_ITEMS = [
  { to: "/app/vocabulary/review", icon: "history", title: uz.admin.previewReview, hint: uz.admin.previewReviewHint, status: "SRS faol", meta: "4 bosqichli mashq" },
  { to: "/app/vocabulary/saved", icon: "bookmarks", title: uz.admin.previewSaved, hint: uz.admin.previewSavedHint, status: "Tayyor", meta: "Saqlangan lug‘at" },
] as const;

export function AdminSectionsPage() {
  const navigate = useNavigate();

  return (
    <main className="admin-sections-page">
      <PageHeader
        eyebrow="Super admin katalogi"
        title={uz.admin.previewTitle}
        description={uz.admin.previewHint}
        actions={<AppButton type="button" tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>Orqaga</AppButton>}
      />

      <section className="admin-sections-page__stats" aria-label="Bo‘limlar holati">
        <StatCard icon="grid_view" label="Mavjud bo‘limlar" value="2" />
        <StatCard icon="check_circle" label="Ishlayotgan havolalar" value="2 / 2" />
        <StatCard icon="admin_panel_settings" label="Kirish darajasi" value="Super admin" />
      </section>

      <section className="admin-sections-page__catalog" aria-labelledby="admin-sections-catalog-title">
        <SectionHeader
          eyebrow="Navigation catalog"
          title="Learner bo‘limlarini sinash"
          description="Har bir karta learner oqimini yangi oynasiz, shu sessiyada ochadi."
        />
        <div className="admin-sections-page__grid">
          {SECTION_ITEMS.map((item) => (
            <DesignCard
              as="button"
              type="button"
              interactive
              className="admin-sections-card"
              key={item.to}
              onClick={() => navigate(item.to)}
            >
              <div className="admin-sections-card__topline">
                <span className="admin-sections-card__status"><i />{item.status}</span>
                <span className="admin-sections-card__meta">{item.meta}</span>
              </div>
              <span className="admin-sections-card__icon"><Icon name={item.icon} /></span>
              <div className="admin-sections-card__copy"><h3>{item.title}</h3><p>{item.hint}</p></div>
              <span className="admin-sections-card__link">Bo‘limni ochish<Icon name="arrow_forward" /></span>
            </DesignCard>
          ))}
        </div>
      </section>

      <DesignCard as="section" className="admin-sections-page__tool" aria-labelledby="admin-sections-tool-title">
        <span className="admin-sections-page__tool-icon"><Icon name="schedule_send" /></span>
        <div className="admin-sections-page__tool-copy">
          <span className="ea-eyebrow">Test yordamchisi</span>
          <h2 id="admin-sections-tool-title">{uz.admin.forceDue}</h2>
          <p>{uz.admin.forceDueHint}</p>
        </div>
        <ForceDueReviews />
      </DesignCard>
    </main>
  );
}

function ForceDueReviews() {
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<{ totalWords: number; dueNow: number; seededWords: number } | null>(null);
  const [failed, setFailed] = useState(false);

  const run = async () => {
    setRunning(true);
    setFailed(false);
    setResult(null);
    try {
      setResult(await api.admin.forceDueReviews());
    } catch {
      setFailed(true);
    } finally {
      setRunning(false);
    }
  };

  return (
    <div className="admin-sections-page__tool-action">
      <AppButton
        type="button"
        tone="danger"
        leadingIcon="bolt"
        loading={running}
        fullWidth
        onClick={() => {
          tapLight();
          void run();
        }}
      >
        {running ? uz.admin.forceDueRunning : "Navbatni tayyorlash"}
      </AppButton>
      <div className="admin-sections-page__feedback" aria-live="polite">
        {running && <p className="is-loading"><Icon name="progress_activity" /> {uz.admin.forceDueRunning}</p>}
        {result?.totalWords === 0 && <p className="is-empty"><Icon name="info" /> A1 “My Family” test lug‘atini tayyorlab bo‘lmadi.</p>}
        {result && result.totalWords > 0 && <p className="is-success"><Icon name="check_circle" filled /><span>{result.seededWords > 0 ? `A1 “My Family” mavzusidan ${result.seededWords} ta test so‘zi qo‘shildi. ` : ""}{uz.admin.forceDueDone(result.dueNow)} <Link to="/app/vocabulary/review">Takrorlashni ochish</Link></span></p>}
        {failed && <p className="is-error"><Icon name="error" filled /> {uz.admin.forceDueError}</p>}
      </div>
    </div>
  );
}
