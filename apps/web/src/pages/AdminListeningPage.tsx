import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type { AdminListeningExerciseDto, AdminListeningExerciseUpsertDto } from "@/api/types";
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

export function AdminListeningPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.listening.list(), []);
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<{ id: string | null; form: FormState } | null>(null);
  const [pendingDelete, setPendingDelete] = useState<AdminListeningExerciseDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [toast, setToast] = useState<ToastState>(null);

  const showToast = (nextToast: Exclude<ToastState, null>) => {
    setToast(nextToast);
    window.setTimeout(() => setToast(null), 3200);
  };

  const removeExercise = async () => {
    if (!pendingDelete) return;
    setDeleting(true);
    try {
      await api.admin.listening.remove(pendingDelete.id);
      setPendingDelete(null);
      showToast({ tone: "success", message: "Listening mashqi o‘chirildi." });
      reload();
    } catch {
      showToast({ tone: "danger", message: uz.admin.listening.deleteError });
    } finally {
      setDeleting(false);
    }
  };

  if (loading) return <LoadingSkeleton variant="table" rows={7} />;
  if (error || !data) {
    return (
      <ErrorState
        title={uz.admin.listening.loadError}
        description="Server bilan bog‘lanishni tekshirib, qayta urinib ko‘ring."
        action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton>}
      />
    );
  }

  const normalizedQuery = query.trim().toLowerCase();
  const filtered = data.filter((exercise) => (
    !normalizedQuery
    || exercise.title.toLowerCase().includes(normalizedQuery)
    || exercise.topic.toLowerCase().includes(normalizedQuery)
    || exercise.level.toLowerCase().includes(normalizedQuery)
  ));
  const readyCount = data.filter((exercise) => isReady(exercise.status)).length;
  const questionCount = data.reduce((total, exercise) => total + exercise.questionCount, 0);
  const wordCount = data.reduce((total, exercise) => total + exercise.wordCount, 0);

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Admin workspace"
        title={uz.admin.listening.title}
        description={`${uz.admin.listening.subtitle}. Mashq metadatasi, tayyorlik holati va savollar hajmini boshqaring.`}
        actions={(
          <>
            <AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>{uz.admin.listening.back}</AppButton>
            <AppButton leadingIcon="add" onClick={() => setEditing({ id: null, form: emptyForm() })}>{uz.admin.listening.createNew}</AppButton>
          </>
        )}
      />

      <ResponsiveGrid minItemWidth={210} data-accent="listening" aria-label="Listening statistikasi">
        <StatCard icon="library_music" label="Jami mashqlar" value={data.length} />
        <StatCard icon="task_alt" label="Tayyor audio" value={readyCount} />
        <StatCard icon="quiz" label="Savollar" value={questionCount} />
        <StatCard icon="notes" label="Transcript so‘zlari" value={wordCount} />
      </ResponsiveGrid>

      <DesignCard as="section" className="space-y-4" aria-labelledby="listening-directory-title">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <span className="ea-eyebrow">Mashqlar katalogi</span>
            <h2 id="listening-directory-title" className="text-responsive-heading mt-2">Audio va transcript holati</h2>
          </div>
          <div className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-center">
            <FormField label={uz.admin.listening.search} htmlFor="listening-search" className="min-w-0 sm:w-80">
              <DesignInput
                id="listening-search"
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={`${uz.admin.listening.search}: sarlavha, mavzu yoki daraja`}
              />
            </FormField>
            <span className="shrink-0 rounded-lg bg-ea-surface-soft px-3 py-2 text-sm font-bold text-ea-muted">{filtered.length} / {data.length}</span>
          </div>
        </div>

        <ListeningDirectory
          exercises={filtered}
          onEdit={(exercise) => window.open(`/admin/listening/${exercise.id}/edit`, "_blank", "noopener,noreferrer")}
          onDelete={setPendingDelete}
        />
      </DesignCard>

      <ListeningForm
        key={`${editing?.id ?? "closed"}:${editing?.form.title ?? ""}`}
        editing={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          showToast({ tone: "success", message: editing?.id === null ? "Listening mashqi yaratildi." : "Listening mashqi yangilandi." });
          setEditing(null);
          reload();
        }}
      />

      <DesignConfirm
        open={pendingDelete !== null}
        onClose={() => { if (!deleting) setPendingDelete(null); }}
        title={uz.admin.listening.delete}
        description={pendingDelete?.title}
        message={uz.admin.listening.confirmDelete}
        confirmLabel={uz.admin.listening.delete}
        destructive
        loading={deleting}
        closeOnBackdrop={!deleting}
        closeOnEscape={!deleting}
        onConfirm={() => void removeExercise()}
      />

      <DesignToast open={toast !== null} title={toast?.message ?? ""} tone={toast?.tone ?? "success"} onClose={() => setToast(null)} />
    </main>
  );
}

function ListeningForm({
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

  const set = <Key extends keyof FormState>(key: Key, value: FormState[Key]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editing) return;
    setSaving(true);
    setFailed(false);
    const dto: AdminListeningExerciseUpsertDto = {
      title: form.title.trim(),
      topic: form.topic.trim(),
      level: form.level,
    };
    try {
      if (editing.id === null) await api.admin.listening.create(dto);
      else await api.admin.listening.update(editing.id, dto);
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
      title={editing?.id === null ? uz.admin.listening.createTitle : uz.admin.listening.editTitle}
      description="Listening mashqining sarlavha, mavzu va CEFR darajasini boshqaring."
      closeOnBackdrop={!saving}
      closeOnEscape={!saving}
      footer={(
        <>
          <AppButton tone="standard" size="lg" onClick={onClose} disabled={saving}>{uz.admin.listening.cancel}</AppButton>
          <AppButton type="submit" form="listening-editor-form" size="lg" loading={saving} disabled={!form.title.trim() || !form.topic.trim()}>
            {saving ? (editing?.id === null ? uz.admin.listening.creating : uz.admin.listening.updating) : uz.admin.listening.save}
          </AppButton>
        </>
      )}
    >
      <form id="listening-editor-form" onSubmit={submit} className="space-y-5">
        <FormField
          label={uz.admin.listening.colTitle}
          htmlFor="listening-title"
          required
          error={failed ? (editing?.id === null ? uz.admin.listening.createError : uz.admin.listening.updateError) : undefined}
        >
          <DesignInput id="listening-title" value={form.title} onChange={(event) => set("title", event.target.value)} placeholder={uz.admin.listening.titlePlaceholder} required disabled={saving} />
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={uz.admin.listening.colTopic} htmlFor="listening-topic" required>
            <DesignInput id="listening-topic" value={form.topic} onChange={(event) => set("topic", event.target.value)} placeholder={uz.admin.listening.topicPlaceholder} required disabled={saving} />
          </FormField>
          <FormField label={uz.admin.listening.level} htmlFor="listening-level">
            <DesignSelect id="listening-level" value={form.level} onChange={(event) => set("level", event.target.value as Level)} disabled={saving}>
              {LEVELS.map((level) => <option key={level} value={level}>{level}</option>)}
            </DesignSelect>
          </FormField>
        </div>
        <DesignCard padding="sm" className="grid gap-3 sm:grid-cols-3">
          <Capability icon="upload_file" title="Audio upload" description="Kontent generatori orqali boshqariladi" />
          <Capability icon="subtitles" title="Transcript" description="So‘zlar soni katalogda kuzatiladi" />
          <Capability icon="quiz" title="Exercise form" description="Savollar holati jadvalda ko‘rsatiladi" />
        </DesignCard>
      </form>
    </DesignModal>
  );
}

function Capability({ icon, title, description }: { icon: string; title: string; description: string }) {
  return (
    <div className="rounded-xl bg-ea-surface-soft p-3">
      <span className="ea-eyebrow">{icon.replace(/_/g, " ")}</span>
      <strong className="mt-2 block text-sm text-ea-text">{title}</strong>
      <p className="mt-1 text-xs text-ea-muted">{description}</p>
    </div>
  );
}

function ListeningDirectory({ exercises, onEdit, onDelete }: {
  exercises: AdminListeningExerciseDto[];
  onEdit: (exercise: AdminListeningExerciseDto) => void;
  onDelete: (exercise: AdminListeningExerciseDto) => void;
}) {
  if (exercises.length === 0) return <EmptyState title={uz.admin.listening.empty} icon="search_off" />;

  return (
    <>
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full min-w-[980px] border-collapse text-left">
          <thead>
            <tr className="border-b border-ea-border text-xs uppercase tracking-wide text-ea-muted">
              <th className="px-3 py-3">{uz.admin.listening.colTitle}</th>
              <th className="px-3 py-3">{uz.admin.listening.colTopic}</th>
              <th className="px-3 py-3">{uz.admin.listening.colLevel}</th>
              <th className="px-3 py-3">{uz.admin.listening.colStatus}</th>
              <th className="px-3 py-3 text-center">Kontent</th>
              <th className="px-3 py-3 text-right">{uz.admin.listening.colActions}</th>
            </tr>
          </thead>
          <tbody>
            {exercises.map((exercise) => (
              <tr key={exercise.id} className="border-b border-ea-border last:border-b-0">
                <td className="px-3 py-4 font-bold text-ea-text">{exercise.title}</td>
                <td className="px-3 py-4 text-ea-muted">{exercise.topic}</td>
                <td className="px-3 py-4"><LevelBadge level={exercise.level} /></td>
                <td className="px-3 py-4"><StatusBadge status={exercise.status} /></td>
                <td className="px-3 py-4 text-center text-sm font-bold text-ea-muted">{exercise.questionCount} savol · {exercise.wordCount} so‘z</td>
                <td className="px-3 py-4">
                  <div className="flex justify-end gap-2">
                    <AppIconButton icon="edit" label={`${exercise.title}: ${uz.admin.listening.edit}`} onClick={() => onEdit(exercise)} />
                    <AppIconButton icon="delete" label={`${exercise.title}: ${uz.admin.listening.delete}`} tone="danger" onClick={() => onDelete(exercise)} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ResponsiveGrid minItemWidth={280} className="xl:hidden">
        {exercises.map((exercise) => (
          <DesignCard as="article" key={exercise.id} padding="sm" className="space-y-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="break-words font-bold text-ea-text">{exercise.title}</p>
                <p className="mt-1 break-words text-sm text-ea-muted">{exercise.topic}</p>
              </div>
              <LevelBadge level={exercise.level} />
            </div>
            <div className="flex items-center justify-between gap-3 border-t border-ea-border pt-3">
              <StatusBadge status={exercise.status} />
              <span className="text-xs font-bold text-ea-muted">{exercise.questionCount} savol · {exercise.wordCount} so‘z</span>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <AppButton tone="standard" leadingIcon="edit" onClick={() => onEdit(exercise)}>{uz.admin.listening.edit}</AppButton>
              <AppButton tone="danger" leadingIcon="delete" onClick={() => onDelete(exercise)}>{uz.admin.listening.delete}</AppButton>
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
  const ready = isReady(status);
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

function isReady(status: string) {
  return ["Ready", "Published", "Filled"].includes(status);
}
