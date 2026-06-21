import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type { AdminReadingPassageDto, AdminReadingPassageUpsertDto } from "@/api/types";
import {
  AppButton,
  AppIconButton,
  DesignCard,
  DesignConfirm,
  DesignInput,
  DesignModal,
  DesignSelect,
  DesignToast,
  EmptyState,
  ErrorState,
  FormField,
  PageHeader,
  ResponsiveGrid,
  StatCard,
} from "@/components/design";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { uz } from "@/content/uz";
import { cn } from "@/lib/cn";
import { useAsync } from "@/lib/useAsync";

const LEVELS = ["A1", "A2", "B1", "B2", "C1", "C2"] as const;
type Level = (typeof LEVELS)[number];

interface FormState {
  title: string;
  topic: string;
  level: Level;
}

type ToastState = { tone: "success" | "danger"; message: string } | null;

function emptyForm(): FormState {
  return { title: "", topic: "", level: "A1" };
}

export function AdminReadingPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.reading.list(), []);
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<{ id: string | null; form: FormState } | null>(null);
  const [pendingDelete, setPendingDelete] = useState<AdminReadingPassageDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [toast, setToast] = useState<ToastState>(null);

  const showToast = (nextToast: Exclude<ToastState, null>) => {
    setToast(nextToast);
    window.setTimeout(() => setToast(null), 3200);
  };

  const removePassage = async () => {
    if (!pendingDelete) return;
    setDeleting(true);
    try {
      await api.admin.reading.remove(pendingDelete.id);
      setPendingDelete(null);
      showToast({ tone: "success", message: "Reading matni o‘chirildi." });
      reload();
    } catch {
      showToast({ tone: "danger", message: uz.admin.reading.deleteError });
    } finally {
      setDeleting(false);
    }
  };

  if (loading) return <LoadingSkeleton variant="table" rows={7} />;
  if (error || !data) {
    return (
      <ErrorState
        title={uz.admin.reading.updateError}
        action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton>}
      />
    );
  }

  const normalizedQuery = query.trim().toLowerCase();
  const filtered = data.filter((passage) => (
    !normalizedQuery
    || passage.title.toLowerCase().includes(normalizedQuery)
    || passage.topic.toLowerCase().includes(normalizedQuery)
    || passage.level.toLowerCase().includes(normalizedQuery)
  ));
  const publishedCount = data.filter((passage) => ["Filled", "Ready", "Published"].includes(passage.status)).length;
  const questionCount = data.reduce((total, passage) => total + passage.questionCount, 0);
  const levelCount = new Set(data.map((passage) => passage.level)).size;

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Admin workspace"
        title={uz.admin.reading.title}
        description={uz.admin.reading.subtitle}
        actions={(
          <>
            <AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>{uz.admin.reading.back}</AppButton>
            <AppButton leadingIcon="add" onClick={() => setEditing({ id: null, form: emptyForm() })}>{uz.admin.reading.createNew}</AppButton>
          </>
        )}
      />

      <ResponsiveGrid minItemWidth={210} data-accent="reading" aria-label="Reading overview">
        <StatCard icon="auto_stories" label="Jami matnlar" value={data.length} />
        <StatCard icon="check_circle" label="Tayyor matnlar" value={publishedCount} />
        <StatCard icon="quiz" label={uz.admin.reading.colQuestions} value={questionCount} />
        <StatCard icon="bar_chart" label={uz.admin.reading.colLevel} value={levelCount} />
      </ResponsiveGrid>

      <DesignCard as="section" className="space-y-4" aria-labelledby="reading-directory-title">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <span className="ea-eyebrow">Matnlar katalogi</span>
            <h2 id="reading-directory-title" className="text-responsive-heading mt-2">Reading kontenti holati</h2>
          </div>
          <div className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-center">
            <FormField label={uz.admin.reading.search} htmlFor="reading-search" className="min-w-0 sm:w-80">
              <DesignInput
                id="reading-search"
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={uz.admin.reading.search}
              />
            </FormField>
            <span className="shrink-0 rounded-lg bg-ea-surface-soft px-3 py-2 text-sm font-bold text-ea-muted">{filtered.length} / {data.length}</span>
          </div>
        </div>

        <ReadingDirectory
          passages={filtered}
          onEdit={(passage) => window.open(`/admin/reading/${passage.id}/edit`, "_blank")}
          onDelete={setPendingDelete}
        />
      </DesignCard>

      <ReadingForm
        key={`${editing?.id ?? "closed"}:${editing?.form.title ?? ""}`}
        editing={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          showToast({ tone: "success", message: editing?.id === null ? "Reading matni yaratildi." : "Reading matni yangilandi." });
          setEditing(null);
          reload();
        }}
      />

      <DesignConfirm
        open={pendingDelete !== null}
        onClose={() => { if (!deleting) setPendingDelete(null); }}
        title={uz.admin.reading.delete}
        description={pendingDelete?.title}
        message={uz.admin.reading.confirmDelete}
        confirmLabel={uz.admin.reading.delete}
        destructive
        loading={deleting}
        closeOnBackdrop={!deleting}
        closeOnEscape={!deleting}
        onConfirm={() => void removePassage()}
      />

      <DesignToast open={toast !== null} title={toast?.message ?? ""} tone={toast?.tone ?? "success"} onClose={() => setToast(null)} />
    </main>
  );
}

function ReadingForm({
  editing,
  onClose,
  onSaved,
}: {
  editing: { id: string | null; form: FormState } | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<FormState>(editing?.form ?? emptyForm());
  const [saving, setSaving] = useState(false);
  const [failed, setFailed] = useState(false);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editing) return;
    setSaving(true);
    setFailed(false);
    const dto: AdminReadingPassageUpsertDto = {
      title: form.title.trim(),
      topic: form.topic.trim(),
      level: form.level,
    };
    try {
      if (editing.id === null) await api.admin.reading.create(dto);
      else await api.admin.reading.update(editing.id, dto);
      onSaved();
    } catch {
      setFailed(true);
    } finally {
      setSaving(false);
    }
  };

  return (
    <DesignModal
      open={editing !== null}
      onClose={onClose}
      title={editing?.id === null ? uz.admin.reading.createTitle : uz.admin.reading.editTitle}
      description="Reading matnining sarlavha, mavzu va CEFR darajasini boshqaring."
      closeOnBackdrop={!saving}
      closeOnEscape={!saving}
      footer={(
        <>
          <AppButton tone="standard" size="lg" onClick={onClose} disabled={saving}>{uz.admin.reading.cancel}</AppButton>
          <AppButton type="submit" form="reading-editor-form" size="lg" loading={saving} disabled={!form.title.trim() || !form.topic.trim()}>
            {saving ? (editing?.id === null ? uz.admin.reading.creating : uz.admin.reading.updating) : uz.admin.reading.save}
          </AppButton>
        </>
      )}
    >
      <form id="reading-editor-form" onSubmit={submit} className="space-y-5">
        <FormField
          label={uz.admin.reading.colTitle}
          htmlFor="reading-title"
          required
          error={failed ? (editing?.id === null ? uz.admin.reading.createError : uz.admin.reading.updateError) : undefined}
        >
          <DesignInput id="reading-title" value={form.title} onChange={(event) => set("title", event.target.value)} required disabled={saving} />
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={uz.admin.reading.colTopic} htmlFor="reading-topic" required>
            <DesignInput id="reading-topic" value={form.topic} onChange={(event) => set("topic", event.target.value)} required disabled={saving} />
          </FormField>
          <FormField label={uz.admin.reading.level} htmlFor="reading-level">
            <DesignSelect id="reading-level" value={form.level} onChange={(event) => set("level", event.target.value as Level)} disabled={saving}>
              {LEVELS.map((level) => <option key={level} value={level}>{level}</option>)}
            </DesignSelect>
          </FormField>
        </div>
      </form>
    </DesignModal>
  );
}

function ReadingDirectory({ passages, onEdit, onDelete }: {
  passages: AdminReadingPassageDto[];
  onEdit: (passage: AdminReadingPassageDto) => void;
  onDelete: (passage: AdminReadingPassageDto) => void;
}) {
  if (passages.length === 0) return <EmptyState title={uz.admin.reading.empty} icon="search_off" />;

  return (
    <>
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full min-w-[900px] border-collapse text-left">
          <thead>
            <tr className="border-b border-ea-border text-xs uppercase tracking-wide text-ea-muted">
              <th className="px-3 py-3">{uz.admin.reading.colTitle}</th>
              <th className="px-3 py-3">{uz.admin.reading.colTopic}</th>
              <th className="px-3 py-3">{uz.admin.reading.colLevel}</th>
              <th className="px-3 py-3">{uz.admin.reading.colStatus}</th>
              <th className="px-3 py-3 text-center">{uz.admin.reading.colQuestions}</th>
              <th className="px-3 py-3 text-right">Amallar</th>
            </tr>
          </thead>
          <tbody>
            {passages.map((passage) => (
              <tr key={passage.id} className="border-b border-ea-border last:border-b-0">
                <td className="px-3 py-4 font-bold text-ea-text">{passage.title}</td>
                <td className="px-3 py-4 text-ea-muted">{passage.topic}</td>
                <td className="px-3 py-4"><LevelBadge level={passage.level} /></td>
                <td className="px-3 py-4"><StatusBadge status={passage.status} /></td>
                <td className="px-3 py-4 text-center font-bold text-ea-text">{passage.questionCount}</td>
                <td className="px-3 py-4">
                  <div className="flex justify-end gap-2">
                    <AppIconButton icon="edit" label={`${passage.title}: ${uz.admin.reading.edit}`} onClick={() => onEdit(passage)} />
                    <AppIconButton icon="delete" label={`${passage.title}: ${uz.admin.reading.delete}`} tone="danger" onClick={() => onDelete(passage)} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ResponsiveGrid minItemWidth={280} className="xl:hidden">
        {passages.map((passage) => (
          <DesignCard as="article" key={passage.id} padding="sm" className="space-y-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="break-words font-bold text-ea-text">{passage.title}</p>
                <p className="mt-1 break-words text-sm text-ea-muted">{passage.topic}</p>
              </div>
              <LevelBadge level={passage.level} />
            </div>
            <div className="flex items-center justify-between gap-3 border-t border-ea-border pt-3">
              <StatusBadge status={passage.status} />
              <span className="text-xs font-bold text-ea-muted">{passage.questionCount} {uz.admin.reading.colQuestions.toLowerCase()}</span>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <AppButton tone="standard" leadingIcon="edit" onClick={() => onEdit(passage)}>{uz.admin.reading.edit}</AppButton>
              <AppButton tone="danger" leadingIcon="delete" onClick={() => onDelete(passage)}>{uz.admin.reading.delete}</AppButton>
            </div>
          </DesignCard>
        ))}
      </ResponsiveGrid>
    </>
  );
}

function LevelBadge({ level }: { level: string }) {
  return <span className="inline-flex rounded-full bg-ea-primary-soft px-3 py-1 text-xs font-extrabold text-ea-primary-deep">{level}</span>;
}

function StatusBadge({ status }: { status: string }) {
  const ready = ["Filled", "Ready", "Published"].includes(status);
  const generating = status === "Generating";
  return (
    <span className={cn(
      "inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold",
      ready && "bg-ea-green-50 text-ea-green-600",
      generating && "bg-ea-orange-50 text-ea-orange-600",
      !ready && !generating && "bg-ea-surface-soft text-ea-muted",
    )}>
      <span className={cn(
        "h-2 w-2 rounded-full",
        ready && "bg-ea-green-600",
        generating && "bg-ea-orange-500",
        !ready && !generating && "bg-ea-muted",
      )} />
      {status}
    </span>
  );
}
