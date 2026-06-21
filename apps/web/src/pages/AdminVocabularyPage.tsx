import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "@/api/client";
import type {
  AdminVocabularyTopicDto,
  AdminVocabularyTopicUpsertDto,
  AdminVocabularyTopicWordDto,
} from "@/api/types";
import {
  AppButton,
  AppIconButton,
  DesignCard,
  DesignConfirm,
  DesignInput,
  DesignModal,
  DesignSelect,
  DesignTextarea,
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
  slug: string;
  title: string;
  titleUz: string;
  category: string;
  grammarFocusCode: string;
  level: Level;
  sequence: string;
  passage: string;
  words: AdminVocabularyTopicWordDto[];
}

type ToastState = { tone: "success" | "danger"; message: string } | null;

function emptyForm(): FormState {
  return {
    slug: "",
    title: "",
    titleUz: "",
    category: "",
    grammarFocusCode: "",
    level: "A1",
    sequence: "",
    passage: "",
    words: [],
  };
}

export function rowToForm(row: AdminVocabularyTopicDto): FormState {
  const isKnownLevel = (LEVELS as readonly string[]).includes(row.level);
  return {
    slug: row.slug,
    title: row.title,
    titleUz: row.titleUz,
    category: row.category,
    grammarFocusCode: row.grammarFocusCode,
    level: isKnownLevel ? (row.level as Level) : "A1",
    sequence: String(row.sequence),
    passage: row.passage,
    words: row.words.map((word) => ({ ...word })),
  };
}

export function AdminVocabularyPage() {
  const navigate = useNavigate();
  const { data, loading, error, reload } = useAsync(() => api.admin.vocabulary.list(), []);
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<{ id: string | null; form: FormState } | null>(null);
  const [pendingDelete, setPendingDelete] = useState<AdminVocabularyTopicDto | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [toast, setToast] = useState<ToastState>(null);

  const showToast = (nextToast: Exclude<ToastState, null>) => {
    setToast(nextToast);
    window.setTimeout(() => setToast(null), 3200);
  };

  const removeTopic = async () => {
    if (!pendingDelete) return;
    setDeleting(true);
    try {
      await api.admin.vocabulary.remove(pendingDelete.id);
      setPendingDelete(null);
      showToast({ tone: "success", message: "Vocabulary mavzusi o‘chirildi." });
      reload();
    } catch {
      showToast({ tone: "danger", message: uz.admin.vocabulary.deleteError });
    } finally {
      setDeleting(false);
    }
  };

  if (loading) return <LoadingSkeleton variant="table" rows={7} />;
  if (error || !data) {
    return (
      <ErrorState
        title={uz.admin.vocabulary.loadError}
        description={uz.admin.loadError}
        action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>{uz.admin.vocabulary.retry}</AppButton>}
      />
    );
  }

  const normalizedQuery = query.trim().toLowerCase();
  const filtered = data.filter((topic) => (
    !normalizedQuery
    || topic.title.toLowerCase().includes(normalizedQuery)
    || topic.titleUz.toLowerCase().includes(normalizedQuery)
    || topic.slug.toLowerCase().includes(normalizedQuery)
    || topic.category.toLowerCase().includes(normalizedQuery)
  ));
  const readyCount = data.filter((topic) => ["Ready", "Published", "Filled"].includes(topic.status)).length;
  const wordCount = data.reduce((total, topic) => total + topic.wordCount, 0);
  const categoryCount = new Set(data.map((topic) => topic.category).filter(Boolean)).size;

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Admin workspace"
        title={uz.admin.vocabulary.title}
        description={uz.admin.vocabulary.subtitle}
        actions={(
          <>
            <AppButton tone="standard" leadingIcon="arrow_back" onClick={() => navigate("/admin")}>{uz.admin.vocabulary.back}</AppButton>
            <AppButton leadingIcon="add" onClick={() => setEditing({ id: null, form: emptyForm() })}>{uz.admin.vocabulary.createNew}</AppButton>
          </>
        )}
      />

      <ResponsiveGrid minItemWidth={210} data-accent="vocabulary" aria-label="Vocabulary statistikasi">
        <StatCard icon="menu_book" label="Jami mavzular" value={data.length} />
        <StatCard icon="verified" label="Tayyor mavzular" value={readyCount} />
        <StatCard icon="category" label="Kategoriyalar" value={categoryCount} />
        <StatCard icon="format_list_numbered" label="Jami so‘zlar" value={wordCount} />
      </ResponsiveGrid>

      <DesignCard as="section" className="space-y-4" aria-labelledby="vocabulary-directory-title">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <span className="ea-eyebrow">Kontent katalogi</span>
            <h2 id="vocabulary-directory-title" className="text-responsive-heading mt-2">{uz.admin.vocabulary.title}</h2>
          </div>
          <div className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-center">
            <FormField label={uz.admin.vocabulary.search} htmlFor="vocabulary-search" className="min-w-0 sm:w-80">
              <DesignInput
                id="vocabulary-search"
                type="search"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={uz.admin.vocabulary.search}
              />
            </FormField>
            <span className="shrink-0 rounded-lg bg-ea-surface-soft px-3 py-2 text-sm font-bold text-ea-muted">
              {filtered.length} / {data.length}
            </span>
          </div>
        </div>

        <VocabularyDirectory
          topics={filtered}
          onEdit={(topic) => window.open(`/admin/vocabulary/${topic.id}/edit`, "_blank", "noopener,noreferrer")}
          onDelete={setPendingDelete}
        />
      </DesignCard>

      <VocabularyForm
        key={`${editing?.id ?? "closed"}:${editing?.form.slug ?? ""}`}
        editing={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          showToast({ tone: "success", message: editing?.id === null ? "Vocabulary mavzusi yaratildi." : "Vocabulary mavzusi yangilandi." });
          setEditing(null);
          reload();
        }}
      />

      <DesignConfirm
        open={pendingDelete !== null}
        onClose={() => { if (!deleting) setPendingDelete(null); }}
        title={uz.admin.vocabulary.delete}
        description={pendingDelete?.title}
        message={uz.admin.vocabulary.confirmDelete}
        confirmLabel={uz.admin.vocabulary.delete}
        destructive
        loading={deleting}
        closeOnBackdrop={!deleting}
        closeOnEscape={!deleting}
        onConfirm={() => void removeTopic()}
      />

      <DesignToast
        open={toast !== null}
        title={toast?.message ?? ""}
        tone={toast?.tone ?? "success"}
        onClose={() => setToast(null)}
      />
    </main>
  );
}

export function VocabularyForm({
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

    const dto: AdminVocabularyTopicUpsertDto = {
      title: form.title.trim(),
      titleUz: form.titleUz.trim(),
      category: form.category.trim(),
      grammarFocusCode: form.grammarFocusCode.trim(),
      level: form.level,
      sequence: form.sequence.trim() === "" ? undefined : Number(form.sequence),
      ...(editing.id === null ? { slug: form.slug.trim() } : { passage: form.passage, words: form.words }),
    };

    try {
      if (editing.id === null) await api.admin.vocabulary.create(dto);
      else await api.admin.vocabulary.update(editing.id, dto);
      onSaved();
    } catch {
      setFailed(true);
    } finally {
      setSaving(false);
    }
  };

  const errorText = editing?.id === null ? uz.admin.vocabulary.createError : uz.admin.vocabulary.updateError;

  return (
    <DesignModal
      open={editing !== null}
      onClose={onClose}
      title={editing?.id === null ? uz.admin.vocabulary.createTitle : uz.admin.vocabulary.editTitle}
      description="Vocabulary mavzusi va PostgreSQL contextlarini canonical formatda boshqaring."
      closeOnBackdrop={!saving}
      closeOnEscape={!saving}
      footer={(
        <>
          <AppButton tone="standard" size="lg" onClick={onClose} disabled={saving}>{uz.admin.vocabulary.cancel}</AppButton>
          <AppButton
            type="submit"
            form="vocabulary-editor-form"
            size="lg"
            loading={saving}
            disabled={!form.title.trim() || !form.titleUz.trim() || !form.category.trim() || (editing?.id === null && !form.slug.trim())}
          >
            {saving
              ? (editing?.id === null ? uz.admin.vocabulary.creating : uz.admin.vocabulary.updating)
              : uz.admin.vocabulary.save}
          </AppButton>
        </>
      )}
    >
      <form id="vocabulary-editor-form" onSubmit={submit} className="space-y-5">
        {editing?.id === null ? (
          <FormField label={uz.admin.vocabulary.slug} htmlFor="vocabulary-slug" required>
            <DesignInput
              id="vocabulary-slug"
              value={form.slug}
              onChange={(event) => set("slug", event.target.value)}
              placeholder={uz.admin.vocabulary.slug}
              required
              disabled={saving}
            />
          </FormField>
        ) : (
          <DesignCard padding="sm">
            <span className="ea-eyebrow">{uz.admin.vocabulary.slug}</span>
            <p className="mt-2 break-all font-bold text-ea-text">/{form.slug}</p>
          </DesignCard>
        )}

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label={uz.admin.vocabulary.colTitle} htmlFor="vocabulary-title" required error={failed ? errorText : undefined}>
            <DesignInput
              id="vocabulary-title"
              value={form.title}
              onChange={(event) => set("title", event.target.value)}
              placeholder={uz.admin.vocabulary.titlePlaceholder}
              required
              disabled={saving}
            />
          </FormField>
          <FormField label={uz.admin.vocabulary.colTitleUz} htmlFor="vocabulary-title-uz" required>
            <DesignInput
              id="vocabulary-title-uz"
              value={form.titleUz}
              onChange={(event) => set("titleUz", event.target.value)}
              placeholder={uz.admin.vocabulary.titleUzPlaceholder}
              required
              disabled={saving}
            />
          </FormField>
          <FormField label={uz.admin.vocabulary.colCategory} htmlFor="vocabulary-category" required>
            <DesignInput
              id="vocabulary-category"
              value={form.category}
              onChange={(event) => set("category", event.target.value)}
              placeholder={uz.admin.vocabulary.categoryPlaceholder}
              required
              disabled={saving}
            />
          </FormField>
          <FormField label={uz.admin.vocabulary.grammarFocus} htmlFor="vocabulary-grammar-focus">
            <DesignInput
              id="vocabulary-grammar-focus"
              value={form.grammarFocusCode}
              onChange={(event) => set("grammarFocusCode", event.target.value)}
              placeholder={uz.admin.vocabulary.grammarFocusPlaceholder}
              disabled={saving}
            />
          </FormField>
          <FormField label={uz.admin.vocabulary.level} htmlFor="vocabulary-level">
            <DesignSelect id="vocabulary-level" value={form.level} onChange={(event) => set("level", event.target.value as Level)} disabled={saving}>
              {LEVELS.map((level) => <option key={level} value={level}>{level}</option>)}
            </DesignSelect>
          </FormField>
          <FormField label={uz.admin.vocabulary.sequence} htmlFor="vocabulary-sequence">
            <DesignInput
              id="vocabulary-sequence"
              type="number"
              min={0}
              value={form.sequence}
              onChange={(event) => set("sequence", event.target.value)}
              placeholder="0"
              disabled={saving}
            />
          </FormField>
        </div>

        {editing?.id !== null && editing !== null && (
          <DesignCard as="section" padding="sm" className="space-y-4" aria-labelledby="vocabulary-context-title">
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <span className="ea-eyebrow">PostgreSQL content</span>
                <h3 id="vocabulary-context-title" className="text-responsive-heading mt-2">Mavzu contextlari</h3>
              </div>
              <span className="rounded-full bg-ea-primary-soft px-3 py-1 text-xs font-extrabold text-ea-primary-deep">{form.words.length} ta so‘z</span>
            </div>
            <FormField label="Asosiy passage" htmlFor="vocabulary-passage" required={form.words.length > 0}>
              <DesignTextarea
                id="vocabulary-passage"
                value={form.passage}
                onChange={(event) => set("passage", event.target.value)}
                rows={8}
                required={form.words.length > 0}
                disabled={saving}
              />
            </FormField>
            <div className="space-y-4">
              {form.words.map((word, index) => (
                <WordContextEditor
                  key={`${index}-${word.word}`}
                  index={index}
                  word={word}
                  disabled={saving}
                  onChange={(nextWord) => set("words", form.words.map((item, itemIndex) => itemIndex === index ? nextWord : item))}
                  onRemove={() => set("words", form.words.filter((_, itemIndex) => itemIndex !== index))}
                />
              ))}
            </div>
            <AppButton tone="standard" leadingIcon="add" onClick={() => set("words", [...form.words, emptyWord()])} disabled={saving}>
              So‘z contexti qo‘shish
            </AppButton>
          </DesignCard>
        )}
      </form>
    </DesignModal>
  );
}

function emptyWord(): AdminVocabularyTopicWordDto {
  return {
    word: "",
    translation: "",
    exampleSentence: "",
    partOfSpeech: "Other",
    lexicalCategory: "PartsOfSpeech",
    register: null,
    usageNote: null,
    imageUrl: null,
    imageSource: null,
    imageAttribution: null,
  };
}

function WordContextEditor({ index, word, disabled, onChange, onRemove }: {
  index: number;
  word: AdminVocabularyTopicWordDto;
  disabled: boolean;
  onChange: (word: AdminVocabularyTopicWordDto) => void;
  onRemove: () => void;
}) {
  const update = <K extends keyof AdminVocabularyTopicWordDto>(key: K, value: AdminVocabularyTopicWordDto[K]) => {
    onChange({ ...word, [key]: value });
  };
  const fieldId = (field: string) => `vocabulary-word-${index}-${field}`;

  return (
    <DesignCard as="article" padding="sm" className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <strong className="text-ea-text">Context {index + 1}</strong>
        <AppIconButton icon="delete" label={`${index + 1}-contextni o‘chirish`} tone="danger" onClick={onRemove} disabled={disabled} />
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label="So‘z yoki ibora" htmlFor={fieldId("word")} required>
          <DesignInput id={fieldId("word")} value={word.word} onChange={(event) => update("word", event.target.value)} required disabled={disabled} />
        </FormField>
        <FormField label="O‘zbekcha tarjima" htmlFor={fieldId("translation")} required>
          <DesignInput id={fieldId("translation")} value={word.translation} onChange={(event) => update("translation", event.target.value)} required disabled={disabled} />
        </FormField>
        <FormField label="So‘z turkumi" htmlFor={fieldId("part-of-speech")}>
          <DesignInput id={fieldId("part-of-speech")} value={word.partOfSpeech} onChange={(event) => update("partOfSpeech", event.target.value)} disabled={disabled} />
        </FormField>
        <FormField label="Lexical kategoriya" htmlFor={fieldId("lexical-category")}>
          <DesignInput id={fieldId("lexical-category")} value={word.lexicalCategory} onChange={(event) => update("lexicalCategory", event.target.value)} disabled={disabled} />
        </FormField>
        <FormField label="Register" htmlFor={fieldId("register")}>
          <DesignInput id={fieldId("register")} value={word.register ?? ""} onChange={(event) => update("register", event.target.value || null)} disabled={disabled} />
        </FormField>
        <FormField label="Rasm URL" htmlFor={fieldId("image-url")}>
          <DesignInput id={fieldId("image-url")} value={word.imageUrl ?? ""} onChange={(event) => update("imageUrl", event.target.value || null)} disabled={disabled} />
        </FormField>
      </div>
      <FormField label="Context gap" htmlFor={fieldId("example")}>
        <DesignTextarea id={fieldId("example")} value={word.exampleSentence ?? ""} onChange={(event) => update("exampleSentence", event.target.value || null)} rows={3} disabled={disabled} />
      </FormField>
      <FormField label="Qo‘llash izohi" htmlFor={fieldId("usage-note")}>
        <DesignTextarea id={fieldId("usage-note")} value={word.usageNote ?? ""} onChange={(event) => update("usageNote", event.target.value || null)} rows={2} disabled={disabled} />
      </FormField>
    </DesignCard>
  );
}

function VocabularyDirectory({
  topics,
  onEdit,
  onDelete,
}: {
  topics: AdminVocabularyTopicDto[];
  onEdit: (topic: AdminVocabularyTopicDto) => void;
  onDelete: (topic: AdminVocabularyTopicDto) => void;
}) {
  if (topics.length === 0) return <EmptyState title={uz.admin.vocabulary.empty} icon="search_off" />;

  return (
    <>
      <div className="hidden overflow-x-auto xl:block">
        <table className="w-full min-w-[980px] border-collapse text-left">
          <thead>
            <tr className="border-b border-ea-border text-xs uppercase tracking-wide text-ea-muted">
              <th className="px-3 py-3">{uz.admin.vocabulary.colTitle}</th>
              <th className="px-3 py-3">{uz.admin.vocabulary.colLevel}</th>
              <th className="px-3 py-3">{uz.admin.vocabulary.colCategory}</th>
              <th className="px-3 py-3">{uz.admin.vocabulary.colStatus}</th>
              <th className="px-3 py-3 text-center">So‘zlar</th>
              <th className="px-3 py-3 text-right">Amallar</th>
            </tr>
          </thead>
          <tbody>
            {topics.map((topic) => (
              <tr key={topic.id} className="border-b border-ea-border last:border-b-0">
                <td className="px-3 py-4">
                  <p className="font-bold text-ea-text">{topic.title}</p>
                  <p className="mt-1 text-sm text-ea-muted">{topic.titleUz} · /{topic.slug}</p>
                </td>
                <td className="px-3 py-4"><LevelBadge level={topic.level} /></td>
                <td className="px-3 py-4 text-ea-muted">{topic.category || "-"}</td>
                <td className="px-3 py-4"><StatusBadge status={topic.status} /></td>
                <td className="px-3 py-4 text-center font-bold text-ea-text">{topic.wordCount}</td>
                <td className="px-3 py-4">
                  <div className="flex justify-end gap-2">
                    <AppIconButton icon="edit" label={`${topic.title}: ${uz.admin.vocabulary.edit}`} onClick={() => onEdit(topic)} />
                    <AppIconButton icon="delete" label={`${topic.title}: ${uz.admin.vocabulary.delete}`} tone="danger" onClick={() => onDelete(topic)} />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ResponsiveGrid minItemWidth={280} className="xl:hidden">
        {topics.map((topic) => (
          <DesignCard as="article" key={topic.id} padding="sm" className="space-y-4">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="break-words font-bold text-ea-text">{topic.title}</p>
                <p className="mt-1 break-words text-sm text-ea-muted">{topic.titleUz}</p>
                <p className="mt-1 break-all text-xs text-ea-muted">/{topic.slug}</p>
              </div>
              <LevelBadge level={topic.level} />
            </div>
            <div className="flex flex-wrap items-center gap-2 border-t border-ea-border pt-3">
              <StatusBadge status={topic.status} />
              <span className="rounded-full bg-ea-surface-soft px-3 py-1 text-xs font-bold text-ea-muted">{topic.category || "-"}</span>
              <span className="rounded-full bg-ea-surface-soft px-3 py-1 text-xs font-bold text-ea-muted">{topic.wordCount} so‘z</span>
              {topic.grammarFocusCode && <span className="rounded-full bg-ea-surface-soft px-3 py-1 text-xs font-bold text-ea-muted">{topic.grammarFocusCode}</span>}
            </div>
            <div className="grid grid-cols-2 gap-2">
              <AppButton tone="standard" leadingIcon="edit" onClick={() => onEdit(topic)}>{uz.admin.vocabulary.edit}</AppButton>
              <AppButton tone="danger" leadingIcon="delete" onClick={() => onDelete(topic)}>{uz.admin.vocabulary.delete}</AppButton>
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
