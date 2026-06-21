import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type { AdminGrammarLessonDto, AdminGrammarLessonUpsertDto } from "@/api/types";
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

const CATEGORIES = [
  "Articles",
  "VerbTense",
  "Prepositions",
  "GerundInfinitive",
  "Modals",
  "SubjectVerbAgreement",
  "WordOrder",
  "Pronunciation",
  "Vocabulary",
  "Spelling",
  "Other",
] as const;
type Category = (typeof CATEGORIES)[number];

interface FormState {
  title: string;
  category: Category;
  level: Level;
}

type ToastState = { tone: "success" | "error"; message: string } | null;

function emptyForm(): FormState {
  return { title: "", category: "Articles", level: "A1" };
}

export function AdminGrammarPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.grammar.list(), []);
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<{ id: string | null; form: FormState } | null>(null);
  const [pendingDelete, setPendingDelete] = useState<AdminGrammarLessonDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [toast, setToast] = useState<ToastState>(null);

  const showToast = (nextToast: Exclude<ToastState, null>) => {
    setToast(nextToast);
    window.setTimeout(() => setToast(null), 3200);
  };

  const removeLesson = async () => {
    if (!pendingDelete) return;
    setDeleting(true);
    try {
      await api.admin.grammar.remove(pendingDelete.id);
      setPendingDelete(null);
      showToast({ tone: "success", message: "Dars o‘chirildi" });
      reload();
    } catch {
      showToast({ tone: "error", message: uz.admin.grammar.deleteError });
    } finally {
      setDeleting(false);
    }
  };

  if (loading) return <LoadingSkeleton variant="table" rows={7} />;
  if (error || !data) {
    return (
      <ErrorState
        title={uz.admin.grammar.loadError}
        action={<AppButton tone="standard" onClick={reload}>{uz.admin.grammar.retry}</AppButton>}
      />
    );
  }

  const normalizedQuery = query.trim().toLowerCase();
  const filtered = data.filter((lesson) => (
    !normalizedQuery
    || lesson.title.toLowerCase().includes(normalizedQuery)
    || lesson.category.toLowerCase().includes(normalizedQuery)
    || lesson.level.toLowerCase().includes(normalizedQuery)
  ));
  const readyCount = data.filter((lesson) => ["Ready", "Published", "Filled"].includes(lesson.status)).length;
  const exerciseCount = data.reduce((total, lesson) => total + lesson.exerciseCount, 0);
  const categoryCount = new Set(data.map((lesson) => lesson.category)).size;

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Admin workspace"
        title={uz.admin.grammar.title}
        description={uz.admin.grammar.subtitle}
        actions={(
          <>
            <AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>{uz.admin.grammar.back}</AppButton>
            <AppButton leadingIcon="add" onClick={() => setEditing({ id: null, form: emptyForm() })}>{uz.admin.grammar.createNew}</AppButton>
          </>
        )}
      />

      <ResponsiveGrid minItemWidth={210} data-accent="grammar" aria-label="Grammatika statistikasi">
        <StatCard icon="library_books" label="Jami darslar" value={data.length} />
        <StatCard icon="verified" label="Tayyor darslar" value={readyCount} />
        <StatCard icon="grid_view" label="Kategoriyalar" value={categoryCount} />
        <StatCard icon="quiz" label="Mashqlar" value={exerciseCount} />
      </ResponsiveGrid>

      <DesignCard as="section" className="space-y-4" aria-labelledby="grammar-directory-title">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <span className="ea-eyebrow">Kontent katalogi</span>
            <h2 id="grammar-directory-title" className="text-responsive-heading mt-2">{uz.admin.grammar.title}</h2>
          </div>
          <div className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-center">
            <FormField label={uz.admin.grammar.search} htmlFor="grammar-search" className="min-w-0 sm:w-80">
              <DesignInput
                id="grammar-search"
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={uz.admin.grammar.search}
              />
            </FormField>
            <span className="shrink-0 rounded-lg bg-ea-surface-soft px-3 py-2 text-sm font-bold text-ea-muted">
              {filtered.length} / {data.length}
            </span>
          </div>
        </div>

        <GrammarDirectory
          lessons={filtered}
          onEdit={(lesson) => window.open(`/admin/grammar/${lesson.id}/edit`, "_blank", "noopener,noreferrer")}
          onDelete={setPendingDelete}
        />
      </DesignCard>

      <GrammarForm
        key={`${editing?.id ?? "closed"}:${editing?.form.title ?? ""}`}
        editing={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          showToast({ tone: "success", message: editing?.id === null ? "Dars yaratildi" : "Dars yangilandi" });
          setEditing(null);
          reload();
        }}
      />

      <DesignConfirm
        open={pendingDelete !== null}
        onClose={() => { if (!deleting) setPendingDelete(null); }}
        title={uz.admin.grammar.delete}
        description={pendingDelete?.title}
        message={uz.admin.grammar.confirmDelete}
        confirmLabel={uz.admin.grammar.delete}
        destructive
        loading={deleting}
        closeOnBackdrop={!deleting}
        closeOnEscape={!deleting}
        onConfirm={() => void removeLesson()}
      />

      <DesignToast
        open={toast !== null}
        title={toast?.message ?? ""}
        tone={toast?.tone === "success" ? "success" : "danger"}
        onClose={() => setToast(null)}
      />
    </main>
  );
}

function GrammarForm({
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
  return (
    <GrammarFormContent
      editing={editing}
      form={form}
      setForm={setForm}
      saving={saving}
      setSaving={setSaving}
      failed={failed}
      setFailed={setFailed}
      onClose={onClose}
      onSaved={onSaved}
    />
  );
}

function GrammarFormContent({
  editing,
  form,
  setForm,
  saving,
  setSaving,
  failed,
  setFailed,
  onClose,
  onSaved,
}: {
  editing: { id: string | null; form: FormState } | null;
  form: FormState;
  setForm: React.Dispatch<React.SetStateAction<FormState>>;
  saving: boolean;
  setSaving: React.Dispatch<React.SetStateAction<boolean>>;
  failed: boolean;
  setFailed: React.Dispatch<React.SetStateAction<boolean>>;
  onClose: () => void;
  onSaved: () => void;
}) {
  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!editing) return;
    setSaving(true);
    setFailed(false);
    const dto: AdminGrammarLessonUpsertDto = {
      title: form.title.trim(),
      category: form.category,
      level: form.level,
    };
    try {
      if (editing.id === null) await api.admin.grammar.create(dto);
      else await api.admin.grammar.update(editing.id, dto);
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
      title={editing?.id === null ? uz.admin.grammar.createTitle : uz.admin.grammar.editTitle}
      description="Grammatika darsi metama’lumotlarini canonical formatda boshqaring."
      closeOnBackdrop={!saving}
      closeOnEscape={!saving}
      footer={(
        <>
          <AppButton tone="standard" size="lg" onClick={onClose} disabled={saving}>{uz.admin.grammar.cancel}</AppButton>
          <AppButton
            type="submit"
            form="grammar-editor-form"
            size="lg"
            loading={saving}
            disabled={form.title.trim().length === 0}
          >
            {saving
              ? (editing?.id === null ? uz.admin.grammar.creating : uz.admin.grammar.updating)
              : uz.admin.grammar.save}
          </AppButton>
        </>
      )}
    >
      <form id="grammar-editor-form" onSubmit={submit} className="space-y-5">
        <FormField
          label={uz.admin.grammar.colTitle}
          htmlFor="grammar-title"
          required
          error={failed ? (editing?.id === null ? uz.admin.grammar.createError : uz.admin.grammar.updateError) : undefined}
        >
          <DesignInput
            id="grammar-title"
            value={form.title}
            onChange={(event) => set("title", event.target.value)}
            placeholder={uz.admin.grammar.titlePlaceholder}
            required
            disabled={saving}
            aria-describedby={failed ? "grammar-title-message" : undefined}
          />
        </FormField>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={uz.admin.grammar.colCategory} htmlFor="grammar-category">
            <DesignSelect id="grammar-category" value={form.category} onChange={(event) => set("category", event.target.value as Category)} disabled={saving}>
              {CATEGORIES.map((category) => <option key={category} value={category}>{category}</option>)}
            </DesignSelect>
          </FormField>
          <FormField label={uz.admin.grammar.level} htmlFor="grammar-level">
            <DesignSelect id="grammar-level" value={form.level} onChange={(event) => set("level", event.target.value as Level)} disabled={saving}>
              {LEVELS.map((level) => <option key={level} value={level}>{level}</option>)}
            </DesignSelect>
          </FormField>
        </div>
      </form>
    </DesignModal>
  );
}

function GrammarDirectory({
  lessons,
  onEdit,
  onDelete,
}: {
  lessons: AdminGrammarLessonDto[];
  onEdit: (lesson: AdminGrammarLessonDto) => void;
  onDelete: (lesson: AdminGrammarLessonDto) => void;
}) {
  if (lessons.length === 0) return <EmptyState title={uz.admin.grammar.empty} icon="search_off" />;

  return (
    <>
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full min-w-[900px] border-collapse text-left">
          <thead>
            <tr className="border-b border-ea-border text-xs uppercase tracking-wide text-ea-muted">
              <th className="px-3 py-3">{uz.admin.grammar.colTitle}</th>
              <th className="px-3 py-3">{uz.admin.grammar.colLevel}</th>
              <th className="px-3 py-3">{uz.admin.grammar.colCategory}</th>
              <th className="px-3 py-3">{uz.admin.grammar.colStatus}</th>
              <th className="px-3 py-3 text-center">{uz.admin.grammar.colExercises}</th>
              <th className="px-3 py-3 text-right">{uz.admin.grammar.colActions}</th>
            </tr>
          </thead>
          <tbody>
            {lessons.map((lesson) => (
              <tr key={lesson.id} className="border-b border-ea-border last:border-b-0">
                <td className="px-3 py-4 font-bold text-ea-text">{lesson.title}</td>
                <td className="px-3 py-4"><LevelBadge level={lesson.level} /></td>
                <td className="px-3 py-4 text-ea-muted">{lesson.category}</td>
                <td className="px-3 py-4"><StatusBadge status={lesson.status} /></td>
                <td className="px-3 py-4 text-center font-bold text-ea-text">{lesson.exerciseCount}</td>
                <td className="px-3 py-4">
                  <div className="flex justify-end gap-2">
                    <AppIconButton icon="edit" label={`${lesson.title}: ${uz.admin.grammar.edit}`} onClick={() => onEdit(lesson)} />
                    <AppIconButton icon="delete" label={`${lesson.title}: ${uz.admin.grammar.delete}`} tone="danger" onClick={() => onDelete(lesson)} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ResponsiveGrid minItemWidth={260} className="xl:hidden">
        {lessons.map((lesson) => (
          <DesignCard as="article" key={lesson.id} padding="sm" className="space-y-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="break-words font-bold text-ea-text">{lesson.title}</p>
                <p className="mt-1 text-sm text-ea-muted">{lesson.category}</p>
              </div>
              <LevelBadge level={lesson.level} />
            </div>
            <div className="flex items-center justify-between gap-3 border-t border-ea-border pt-3">
              <StatusBadge status={lesson.status} />
              <span className="text-sm font-bold text-ea-muted">{lesson.exerciseCount} {uz.admin.grammar.colExercises.toLowerCase()}</span>
            </div>
            <div className="grid grid-cols-2 gap-2">
              <AppButton tone="standard" leadingIcon="edit" onClick={() => onEdit(lesson)}>{uz.admin.grammar.edit}</AppButton>
              <AppButton tone="danger" leadingIcon="delete" onClick={() => onDelete(lesson)}>{uz.admin.grammar.delete}</AppButton>
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
  const ready = ["Ready", "Published", "Filled"].includes(status);
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
