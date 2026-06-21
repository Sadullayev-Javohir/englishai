import { useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "@/api/client";
import type { AdminGrammarLessonDetailDto, AdminGrammarLessonFullUpdateDto } from "@/api/types";
import { useDocumentTitle } from "@/app/documentTitle";
import {
  AppButton,
  AppIconButton,
  DesignCard,
  DesignConfirm,
  DesignInput,
  DesignRadio,
  DesignSelect,
  DesignTextarea,
  DesignToast,
  EmptyState,
  ErrorState,
  FormField,
  PageHeader,
  SectionHeader,
} from "@/components/design";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { useAsync } from "@/lib/useAsync";

const LEVELS = ["A1", "A2", "B1", "B2", "C1", "C2"];
const CATEGORIES = ["Articles", "VerbTense", "Prepositions", "GerundInfinitive", "Modals", "SubjectVerbAgreement", "WordOrder", "Pronunciation", "Vocabulary", "Spelling", "Other"];
const EXERCISE_TYPES = ["Recognition", "FillInBlank", "Rephrase"];
const SKILLS = ["Speaking", "Writing"];
type FormState = AdminGrammarLessonFullUpdateDto;

function toForm(data: AdminGrammarLessonDetailDto): FormState {
  return {
    title: data.title,
    category: data.category,
    level: data.level,
    status: data.status,
    vocabularyTopicId: data.vocabularyTopicId,
    grammarFocusCode: data.grammarFocusCode,
    contextIntro: data.contextIntro,
    explanation: data.explanation,
    curatedTitleUz: data.curatedTitleUz,
    curatedSummaryUz: data.curatedSummaryUz,
    curatedFormulas: [...data.curatedFormulas],
    curatedRules: data.curatedRules.map((item) => ({ ...item })),
    examples: data.examples.map((item) => ({ ...item })),
    commonMistakes: data.commonMistakes.map((item) => ({ ...item })),
    exercises: data.exercises.map((item) => ({ ...item, options: [...item.options] })),
    applicationTasks: data.applicationTasks.map((item) => ({ ...item })),
  };
}

export function AdminGrammarEditPage() {
  const { id } = useParams<{ id: string }>();
  const { data, loading, error, reload } = useAsync(
    () => id ? api.admin.grammar.get(id) : Promise.reject(new Error("Missing grammar lesson id")),
    [id],
    Boolean(id),
  );
  useDocumentTitle(data?.title, "Grammar tahrirlash");

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="PostgreSQL grammar editor"
        title="Grammar lessonni tahrirlash"
        description="Dars kontenti, savollar va javoblarni atomik saqlash"
        actions={<AppButton tone="standard" leadingIcon="close" onClick={() => window.close()}>Tabni yopish</AppButton>}
      />
      {loading && <LoadingSkeleton variant="form" />}
      {(error || (!loading && !data)) && (
        <ErrorState
          title="Darsni yuklab bo‘lmadi"
          action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton>}
        />
      )}
      {data && id && <GrammarEditor id={id} initial={toForm(data)} />}
    </main>
  );
}

function GrammarEditor({ id, initial }: { id: string; initial: FormState }) {
  const [form, setForm] = useState(initial);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);
  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const requestSave = (event: React.FormEvent) => {
    event.preventDefault();
    const validation = validate(form);
    if (validation) {
      setError(validation);
      return;
    }
    setError(null);
    setConfirmOpen(true);
  };

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      await api.admin.grammar.updateFull(id, form);
      setConfirmOpen(false);
      setToast("Grammar lesson saqlandi");
      window.opener?.location.reload();
      window.setTimeout(() => window.close(), 500);
    } catch {
      setError("Saqlashda xatolik yuz berdi. Ma'lumotlarni tekshirib, qayta urining.");
      setConfirmOpen(false);
    } finally {
      setSaving(false);
    }
  };

  return (
    <form onSubmit={requestSave} className="space-y-6">
      <EditorSection title="Asosiy ma'lumotlar" description="Darsning katalog va progression metama’lumotlari.">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <FormField label="Title" htmlFor="grammar-edit-title"><DesignInput id="grammar-edit-title" value={form.title} onChange={(event) => set("title", event.target.value)} /></FormField>
          <FormField label="Category" htmlFor="grammar-edit-category"><DesignSelect id="grammar-edit-category" value={form.category} onChange={(event) => set("category", event.target.value)}>{CATEGORIES.map((category) => <option key={category}>{category}</option>)}</DesignSelect></FormField>
          <FormField label="CEFR level" htmlFor="grammar-edit-level"><DesignSelect id="grammar-edit-level" value={form.level} onChange={(event) => set("level", event.target.value)}>{LEVELS.map((level) => <option key={level}>{level}</option>)}</DesignSelect></FormField>
          <FormField label="Status" htmlFor="grammar-edit-status"><DesignSelect id="grammar-edit-status" value={form.status} onChange={(event) => set("status", event.target.value)}><option>Pending</option><option>Filled</option></DesignSelect></FormField>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="Vocabulary topic ID" htmlFor="grammar-edit-vocabulary-topic"><DesignInput id="grammar-edit-vocabulary-topic" value={form.vocabularyTopicId ?? ""} onChange={(event) => set("vocabularyTopicId", event.target.value.trim() || null)} placeholder="UUID yoki bo‘sh" /></FormField>
          <FormField label="Grammar focus code" htmlFor="grammar-edit-focus"><DesignInput id="grammar-edit-focus" value={form.grammarFocusCode ?? ""} onChange={(event) => set("grammarFocusCode", event.target.value || null)} /></FormField>
        </div>
      </EditorSection>

      <EditorSection title="Kontekst va qoida" description="Learner darsida ko‘rinadigan asosiy tushuntirishlar.">
        <FormField label="Context introduction" htmlFor="grammar-edit-context"><DesignTextarea id="grammar-edit-context" rows={5} value={form.contextIntro} onChange={(event) => set("contextIntro", event.target.value)} /></FormField>
        <FormField label="Rule explanation" htmlFor="grammar-edit-explanation"><DesignTextarea id="grammar-edit-explanation" rows={7} value={form.explanation ?? ""} onChange={(event) => set("explanation", event.target.value || null)} /></FormField>
      </EditorSection>

      <EditorSection title="2. QOIDA card kontenti" description="Canonical lesson card uchun O‘zbekcha qisqa mazmun.">
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="O‘zbekcha sarlavha" htmlFor="grammar-edit-title-uz"><DesignInput id="grammar-edit-title-uz" value={form.curatedTitleUz ?? ""} onChange={(event) => set("curatedTitleUz", event.target.value || null)} /></FormField>
          <FormField label="O‘zbekcha qisqa izoh" htmlFor="grammar-edit-summary-uz"><DesignTextarea id="grammar-edit-summary-uz" rows={3} value={form.curatedSummaryUz ?? ""} onChange={(event) => set("curatedSummaryUz", event.target.value || null)} /></FormField>
        </div>
      </EditorSection>

      <Collection
        title="Qoidalar formulalari"
        items={form.curatedFormulas}
        onAdd={() => set("curatedFormulas", [...form.curatedFormulas, ""])}
        onChange={(items) => set("curatedFormulas", items)}
        render={(item, index, update) => <FormField label="Formula" htmlFor={`grammar-formula-${index}`}><DesignTextarea id={`grammar-formula-${index}`} rows={2} value={item} onChange={(event) => update(index, event.target.value)} /></FormField>}
      />
      <Collection
        title="Qoidalar bloklari"
        items={form.curatedRules}
        onAdd={() => set("curatedRules", [...form.curatedRules, { headingUz: "", bodyUz: "" }])}
        onChange={(items) => set("curatedRules", items)}
        render={(item, index, update) => (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Sarlavha" htmlFor={`grammar-rule-title-${index}`}><DesignInput id={`grammar-rule-title-${index}`} value={item.headingUz} onChange={(event) => update(index, { ...item, headingUz: event.target.value })} /></FormField>
            <FormField label="Izoh" htmlFor={`grammar-rule-body-${index}`}><DesignTextarea id={`grammar-rule-body-${index}`} rows={4} value={item.bodyUz} onChange={(event) => update(index, { ...item, bodyUz: event.target.value })} /></FormField>
          </div>
        )}
      />
      <Collection
        title="Misollar"
        items={form.examples}
        onAdd={() => set("examples", [...form.examples, { english: "", uzbek: "" }])}
        onChange={(items) => set("examples", items)}
        render={(item, index, update) => (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="English" htmlFor={`grammar-example-en-${index}`}><DesignTextarea id={`grammar-example-en-${index}`} rows={3} value={item.english} onChange={(event) => update(index, { ...item, english: event.target.value })} /></FormField>
            <FormField label="Uzbek" htmlFor={`grammar-example-uz-${index}`}><DesignTextarea id={`grammar-example-uz-${index}`} rows={3} value={item.uzbek} onChange={(event) => update(index, { ...item, uzbek: event.target.value })} /></FormField>
          </div>
        )}
      />
      <Collection
        title="Keng tarqalgan xatolar"
        items={form.commonMistakes}
        onAdd={() => set("commonMistakes", [...form.commonMistakes, { text: "" }])}
        onChange={(items) => set("commonMistakes", items)}
        render={(item, index, update) => <FormField label="Xato tavsifi" htmlFor={`grammar-mistake-${index}`}><DesignTextarea id={`grammar-mistake-${index}`} rows={3} value={item.text} onChange={(event) => update(index, { text: event.target.value })} /></FormField>}
      />
      <ExerciseCollection exercises={form.exercises} onChange={(exercises) => set("exercises", exercises)} />
      <Collection
        title="Amaliy mashqlar"
        items={form.applicationTasks}
        onAdd={() => set("applicationTasks", [...form.applicationTasks, { targetSkill: "Speaking", prompt: "" }])}
        onChange={(items) => set("applicationTasks", items)}
        render={(item, index, update) => (
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Target skill" htmlFor={`grammar-task-skill-${index}`}><DesignSelect id={`grammar-task-skill-${index}`} value={item.targetSkill} onChange={(event) => update(index, { ...item, targetSkill: event.target.value })}>{SKILLS.map((skill) => <option key={skill}>{skill}</option>)}</DesignSelect></FormField>
            <FormField label="Prompt" htmlFor={`grammar-task-prompt-${index}`}><DesignTextarea id={`grammar-task-prompt-${index}`} rows={3} value={item.prompt} onChange={(event) => update(index, { ...item, prompt: event.target.value })} /></FormField>
          </div>
        )}
      />

      {error && <ErrorState title={error} />}
      <DesignCard className="sticky bottom-3 z-10 flex flex-col gap-3 sm:flex-row sm:justify-end" padding="sm">
        <AppButton type="button" tone="standard" leadingIcon="close" onClick={() => window.close()} disabled={saving}>Bekor qilish</AppButton>
        <AppButton type="submit" leadingIcon="check_circle" loading={saving}>PostgreSQL&apos;ga saqlash</AppButton>
      </DesignCard>

      <DesignConfirm
        open={confirmOpen}
        onClose={() => { if (!saving) setConfirmOpen(false); }}
        title="Grammar lessonni saqlash"
        message="Barcha grammar kontentini PostgreSQL'ga saqlaysizmi?"
        confirmLabel="PostgreSQL'ga saqlash"
        loading={saving}
        closeOnBackdrop={!saving}
        closeOnEscape={!saving}
        onConfirm={() => void save()}
      />
      <DesignToast open={Boolean(toast)} title={toast ?? ""} tone="success" onClose={() => setToast(null)} />
    </form>
  );
}

function EditorSection({ title, description, children }: { title: string; description: string; children: React.ReactNode }) {
  return (
    <DesignCard as="section" className="space-y-4">
      <SectionHeader title={title} description={description} />
      {children}
    </DesignCard>
  );
}

function ExerciseCollection({ exercises, onChange }: { exercises: FormState["exercises"]; onChange: (items: FormState["exercises"]) => void }) {
  const update = (index: number, value: FormState["exercises"][number]) => {
    onChange(exercises.map((item, itemIndex) => itemIndex === index ? value : item));
  };
  return (
    <DesignCard as="section" className="space-y-4">
      <SectionHeader
        title="Mashqlar, savollar va javoblar"
        actions={<AppButton type="button" tone="standard" leadingIcon="add" onClick={() => onChange([...exercises, { type: "Recognition", prompt: "", options: ["", ""], correctOptionIndex: 0, hintCode: null, explanation: null }])}>Mashq qo‘shish</AppButton>}
      />
      {exercises.length === 0 ? <EmptyState title="Hozircha mashq yo‘q" icon="inbox" /> : exercises.map((exercise, index) => (
        <DesignCard as="article" padding="sm" className="space-y-4" key={index}>
          <ItemToolbar index={index} count={exercises.length} onMove={(next) => onChange(move(exercises, index, next))} onDelete={() => onChange(exercises.filter((_, itemIndex) => itemIndex !== index))} />
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Exercise type" htmlFor={`grammar-exercise-type-${index}`}><DesignSelect id={`grammar-exercise-type-${index}`} value={exercise.type} onChange={(event) => update(index, { ...exercise, type: event.target.value })}>{EXERCISE_TYPES.map((type) => <option key={type}>{type}</option>)}</DesignSelect></FormField>
            <FormField label="Hint code" htmlFor={`grammar-exercise-hint-${index}`}><DesignInput id={`grammar-exercise-hint-${index}`} value={exercise.hintCode ?? ""} onChange={(event) => update(index, { ...exercise, hintCode: event.target.value || null })} /></FormField>
          </div>
          <FormField label="Savol / prompt" htmlFor={`grammar-exercise-prompt-${index}`}><DesignTextarea id={`grammar-exercise-prompt-${index}`} rows={3} value={exercise.prompt} onChange={(event) => update(index, { ...exercise, prompt: event.target.value })} /></FormField>
          <DesignCard padding="sm" className="space-y-3">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <strong className="text-ea-text">Variantlar va to‘g‘ri javob</strong>
              <AppButton type="button" tone="standard" leadingIcon="add" onClick={() => update(index, { ...exercise, options: [...exercise.options, ""] })}>Variant</AppButton>
            </div>
            {exercise.options.map((option, optionIndex) => (
              <div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2" key={optionIndex}>
                <DesignRadio
                  name={`correct-${index}`}
                  checked={exercise.correctOptionIndex === optionIndex}
                  onChange={() => update(index, { ...exercise, correctOptionIndex: optionIndex })}
                  label={`${optionIndex + 1}-variant to‘g‘ri`}
                />
                <DesignInput
                  value={option}
                  onChange={(event) => update(index, { ...exercise, options: exercise.options.map((value, itemIndex) => itemIndex === optionIndex ? event.target.value : value) })}
                  placeholder={`${optionIndex + 1}-variant`}
                />
                <AppIconButton
                  icon="delete"
                  label="Variantni o‘chirish"
                  tone="danger"
                  onClick={() => {
                    const options = exercise.options.filter((_, itemIndex) => itemIndex !== optionIndex);
                    update(index, { ...exercise, options, correctOptionIndex: Math.min(exercise.correctOptionIndex, Math.max(0, options.length - 1)) });
                  }}
                />
              </div>
            ))}
          </DesignCard>
          <FormField label="Javob explanation" htmlFor={`grammar-exercise-explanation-${index}`}><DesignTextarea id={`grammar-exercise-explanation-${index}`} rows={3} value={exercise.explanation ?? ""} onChange={(event) => update(index, { ...exercise, explanation: event.target.value || null })} /></FormField>
        </DesignCard>
      ))}
    </DesignCard>
  );
}

type CollectionProps<T> = {
  title: string;
  items: T[];
  onAdd: () => void;
  onChange: (items: T[]) => void;
  render: (item: T, index: number, update: (index: number, item: T) => void) => React.ReactNode;
};

function Collection<T>({ title, items, onAdd, onChange, render }: CollectionProps<T>) {
  const update = (index: number, value: T) => {
    onChange(items.map((item, itemIndex) => itemIndex === index ? value : item));
  };
  return (
    <DesignCard as="section" className="space-y-4">
      <SectionHeader title={title} actions={<AppButton type="button" tone="standard" leadingIcon="add" onClick={onAdd}>Qo‘shish</AppButton>} />
      {items.length === 0 ? <EmptyState title="Hozircha element yo‘q" icon="inbox" /> : items.map((item, index) => (
        <DesignCard as="article" padding="sm" className="space-y-4" key={index}>
          <ItemToolbar index={index} count={items.length} onMove={(next) => onChange(move(items, index, next))} onDelete={() => onChange(items.filter((_, itemIndex) => itemIndex !== index))} />
          {render(item, index, update)}
        </DesignCard>
      ))}
    </DesignCard>
  );
}

function move<T>(items: T[], from: number, to: number) {
  if (to < 0 || to >= items.length) return items;
  const next = [...items];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item);
  return next;
}

function ItemToolbar({ index, count, onMove, onDelete }: { index: number; count: number; onMove: (to: number) => void; onDelete: () => void }) {
  return (
    <div className="flex items-center justify-between gap-3">
      <strong className="text-ea-text">#{index + 1}</strong>
      <div className="flex gap-2">
        <AppIconButton icon="arrow_back" label="Yuqoriga" onClick={() => onMove(index - 1)} disabled={index === 0} />
        <AppIconButton icon="arrow_forward" label="Pastga" onClick={() => onMove(index + 1)} disabled={index === count - 1} />
        <AppIconButton icon="delete" label="O‘chirish" tone="danger" onClick={onDelete} />
      </div>
    </div>
  );
}

function validate(form: FormState): string | null {
  if (!form.title.trim()) return "Title majburiy.";
  if (form.vocabularyTopicId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(form.vocabularyTopicId)) return "Vocabulary topic ID UUID formatida bo‘lishi kerak.";
  if (form.status === "Filled" && (!form.contextIntro.trim() || !(form.explanation ?? "").trim() || form.exercises.length === 0)) return "Filled darsda context, rule explanation va kamida bitta mashq bo‘lishi kerak.";
  if (form.examples.some((item) => !item.english.trim())) return "Har bir misolda English matn bo‘lishi kerak.";
  if (form.curatedFormulas.some((item) => !item.trim())) return "Bo‘sh qoida formulasi mavjud.";
  if (form.curatedRules.some((item) => !item.headingUz.trim() || !item.bodyUz.trim())) return "Qoida bloklarining sarlavha va izohi majburiy.";
  if (form.commonMistakes.some((item) => !item.text.trim())) return "Bo‘sh common mistake mavjud.";
  for (const exercise of form.exercises) {
    if (!exercise.prompt.trim()) return "Har bir mashqda savol bo‘lishi kerak.";
    if (exercise.options.length < 2 || exercise.options.some((option) => !option.trim())) return "Har bir mashqda kamida 2 ta to‘ldirilgan variant bo‘lishi kerak.";
    if (exercise.correctOptionIndex < 0 || exercise.correctOptionIndex >= exercise.options.length) return "To‘g‘ri javob tanlanmagan.";
  }
  if (form.applicationTasks.some((item) => !item.prompt.trim())) return "Bo‘sh application task mavjud.";
  return null;
}
