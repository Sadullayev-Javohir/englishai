import { useCallback, useEffect, useState } from "react";
import { api } from "@/api/client";
import type { AdminCurriculumItemDto, AdminCurriculumRunDto } from "@/api/types";
import {
  AppButton,
  DesignCard,
  DesignState,
  PageHeader,
} from "@/components/design";
import "./AdminCurriculumPage.css";

export function AdminCurriculumPage() {
  const [runs, setRuns] = useState<AdminCurriculumRunDto[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [items, setItems] = useState<AdminCurriculumItemDto[]>([]);
  const [error, setError] = useState("");
  const [loadingRuns, setLoadingRuns] = useState(true);
  const [loadingItems, setLoadingItems] = useState(false);

  const loadRuns = useCallback(async () => {
    setLoadingRuns(true);
    setError("");
    try {
      const rows = await api.admin.curriculum.runs();
      setRuns(rows);
      setSelected((value) => value ?? rows[0]?.id ?? null);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Runlarni yuklab bo'lmadi.");
    } finally {
      setLoadingRuns(false);
    }
  }, []);

  useEffect(() => {
    void loadRuns();
  }, [loadRuns]);

  useEffect(() => {
    if (!selected) {
      setItems([]);
      return;
    }

    let active = true;
    setLoadingItems(true);
    setError("");
    api.admin.curriculum.items(selected)
      .then((rows) => {
        if (active) setItems(rows);
      })
      .catch((reason) => {
        if (active) setError(reason instanceof Error ? reason.message : "Elementlarni yuklab bo'lmadi.");
      })
      .finally(() => {
        if (active) setLoadingItems(false);
      });

    return () => {
      active = false;
    };
  }, [selected]);

  return (
    <main className="admin-curriculum">
      <PageHeader
        eyebrow="Curriculum V4"
        title="Backfill nazorati"
        description="PostgreSQL checkpoint, retry va publish holatlari."
        actions={(
          <AppButton
            type="button"
            tone="standard"
            leadingIcon="refresh"
            loading={loadingRuns}
            onClick={() => void loadRuns()}
          >
            Yangilash
          </AppButton>
        )}
      />

      {error && (
        <DesignState
          role="alert"
          className="ea-state--error"
          icon="error"
          title="Curriculum ma'lumotlari yuklanmadi"
          description={error}
          action={<AppButton type="button" tone="standard" onClick={() => void loadRuns()}>Qayta urinish</AppButton>}
        />
      )}

      <section className="admin-curriculum__runs" aria-label="Curriculum runlari">
        {runs.map((run) => (
          <DesignCard
            as="button"
            type="button"
            interactive
            className="admin-curriculum__run"
            aria-pressed={selected === run.id}
            key={run.id}
            onClick={() => setSelected(run.id)}
          >
            <strong>{run.version} · {run.status}</strong>
            <span>{run.model}</span>
            <progress max={run.totalItems || 1} value={run.publishedItems || run.approvedItems} />
            <small>{run.approvedItems} approved · {run.publishedItems} published · {run.problemItems} muammo</small>
          </DesignCard>
        ))}
      </section>

      {loadingItems ? (
        <DesignState icon="progress_activity" title="Curriculum elementlari yuklanmoqda" />
      ) : items.length === 0 ? (
        <DesignState icon="database" title="Elementlar topilmadi" description="Tanlangan run uchun hali elementlar mavjud emas." />
      ) : (
        <DesignCard padding="none" className="admin-curriculum__table" tabIndex={0} aria-label="Curriculum ishlar jadvali">
          <table>
            <thead><tr><th>Modul</th><th>Mavzu</th><th>Daraja</th><th>Status</th><th>Urinish</th><th>Xato</th></tr></thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.id}>
                  <td>{item.module}</td>
                  <td>{item.subjectKey}</td>
                  <td>{item.level ?? "-"}</td>
                  <td><span data-status={item.status}>{item.status}</span></td>
                  <td>{item.attemptCount}</td>
                  <td title={item.errorMessage ?? ""}>{item.errorCode ?? "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </DesignCard>
      )}
    </main>
  );
}
