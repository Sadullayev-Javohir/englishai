import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "@/api/client";
import type { AdminReadingPassageDetailDto, AdminReadingPassageFullUpsertDto } from "@/api/types";
import { useDocumentTitle } from "@/app/documentTitle";
import {
  AppButton,
  AppIconButton,
  DesignCard,
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
import { formatDateTime } from "@/lib/labels";
import { useAsync } from "@/lib/useAsync";

const LEVELS = ["A1", "A2", "B1", "B2", "C1", "C2"];
type EditorState = AdminReadingPassageFullUpsertDto;

function toForm(data: AdminReadingPassageDetailDto): EditorState {
  return {
    title: data.title,
    topic: data.topic,
    category: data.category,
    level: data.level,
    status: data.status,
    sections: data.sections.length ? [...data.sections] : [data.body],
    contextGaps: data.contextGaps,
    vocabulary: data.vocabulary.map(({ word, translation, exampleSentence }) => ({ word, translation, exampleSentence })),
    questions: data.questions.map(({ prompt, options, correctOptionIndex, hintCode, explanation }) => ({ prompt, options: [...options], correctOptionIndex, hintCode, explanation })),
  };
}

export function AdminReadingEditPage() {
  const { id } = useParams<{ id: string }>();
  const { data, loading, error, reload } = useAsync(
    () => id ? api.admin.reading.get(id) : Promise.reject(new Error("Missing reading id")),
    [id],
    Boolean(id),
  );
  useDocumentTitle(data?.title, "Reading tahrirlash");
  const [form, setForm] = useState<EditorState | null>(null);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  useEffect(() => { if (data) setForm(toForm(data)); }, [data]);
  const validation = useMemo(() => validate(form), [form]);

  const save = async () => {
    if (!id || !form || validation) return;
    setSaving(true);
    setSaveError(null);
    try {
      await api.admin.reading.updateFull(id, form);
      setToast("Reading passage saqlandi");
      window.opener?.location.reload();
      window.setTimeout(() => window.close(), 300);
    } catch {
      setSaveError("Saqlash amalga oshmadi. Qayta urinib ko‘ring.");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <LoadingSkeleton variant="form" />;
  if (error || (!loading && !data)) {
    return <ErrorState title="Reading passage yuklanmadi" action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton>} />;
  }
  if (!form || !data) return <EmptyState title="Reading passage topilmadi" icon="menu_book" />;

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="PostgreSQL full editor"
        title={form.title}
        description="Passage, section, vocabulary va comprehension savollarini boshqaring."
        actions={(
          <>
            <AppButton tone="standard" onClick={() => window.close()}>Bekor qilish</AppButton>
            <AppButton leadingIcon="check_circle" onClick={() => void save()} loading={saving} disabled={Boolean(validation)}>Saqlash</AppButton>
          </>
        )}
      />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_320px] xl:items-start">
        <div className="space-y-6">
          <EditorSection title="Asosiy ma’lumotlar">
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <FormField label="Title" htmlFor="reading-edit-title"><DesignInput id="reading-edit-title" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} /></FormField>
              <FormField label="Topic" htmlFor="reading-edit-topic"><DesignInput id="reading-edit-topic" value={form.topic} onChange={(event) => setForm({ ...form, topic: event.target.value })} /></FormField>
              <FormField label="Category" htmlFor="reading-edit-category"><DesignInput id="reading-edit-category" value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value })} /></FormField>
              <FormField label="CEFR" htmlFor="reading-edit-level"><DesignSelect id="reading-edit-level" value={form.level} onChange={(event) => setForm({ ...form, level: event.target.value })}>{LEVELS.map((level) => <option key={level}>{level}</option>)}</DesignSelect></FormField>
              <FormField label="Status" htmlFor="reading-edit-status"><DesignSelect id="reading-edit-status" value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option>Pending</option><option>Filled</option></DesignSelect></FormField>
            </div>
          </EditorSection>

          <EditorSection title="Paragraph va sectionlar" onAdd={() => setForm({ ...form, sections: [...form.sections, ""] })}>
            {form.sections.length === 0 ? <EmptyState title="Sectionlar hali qo‘shilmagan" icon="inbox" /> : form.sections.map((section, index) => (
              <EditorItem
                key={index}
                title={`Section ${index + 1}`}
                onUp={() => setForm({ ...form, sections: move(form.sections, index, -1) })}
                onDown={() => setForm({ ...form, sections: move(form.sections, index, 1) })}
                onDelete={() => setForm({ ...form, sections: form.sections.filter((_, itemIndex) => itemIndex !== index) })}
              >
                <DesignTextarea aria-label={`Section ${index + 1} matni`} rows={7} value={section} onChange={(event) => setForm({ ...form, sections: replace(form.sections, index, event.target.value) })} />
              </EditorItem>
            ))}
          </EditorSection>

          <EditorSection title="Target vocabulary va tarjimalar" onAdd={() => setForm({ ...form, vocabulary: [...form.vocabulary, { word: "", translation: "", exampleSentence: "" }] })}>
            {form.vocabulary.length === 0 ? <EmptyState title="Vocabulary hali qo‘shilmagan" icon="inbox" /> : form.vocabulary.map((entry, index) => (
              <EditorItem
                key={index}
                title={`Vocabulary ${index + 1}`}
                onUp={() => setForm({ ...form, vocabulary: move(form.vocabulary, index, -1) })}
                onDown={() => setForm({ ...form, vocabulary: move(form.vocabulary, index, 1) })}
                onDelete={() => setForm({ ...form, vocabulary: form.vocabulary.filter((_, itemIndex) => itemIndex !== index) })}
              >
                <div className="grid gap-4 sm:grid-cols-2">
                  <FormField label="Word" htmlFor={`reading-vocabulary-word-${index}`}><DesignInput id={`reading-vocabulary-word-${index}`} value={entry.word} onChange={(event) => setForm({ ...form, vocabulary: update(form.vocabulary, index, "word", event.target.value) })} /></FormField>
                  <FormField label="Translation" htmlFor={`reading-vocabulary-translation-${index}`}><DesignInput id={`reading-vocabulary-translation-${index}`} value={entry.translation} onChange={(event) => setForm({ ...form, vocabulary: update(form.vocabulary, index, "translation", event.target.value) })} /></FormField>
                </div>
                <FormField label="Context" htmlFor={`reading-vocabulary-context-${index}`}><DesignTextarea id={`reading-vocabulary-context-${index}`} rows={3} value={entry.exampleSentence ?? ""} onChange={(event) => setForm({ ...form, vocabulary: update(form.vocabulary, index, "exampleSentence", event.target.value) })} /></FormField>
              </EditorItem>
            ))}
          </EditorSection>

          <EditorSection title="Comprehension savollari" onAdd={() => setForm({ ...form, questions: [...form.questions, { prompt: "", options: ["", ""], correctOptionIndex: 0, hintCode: "", explanation: "" }] })}>
            {form.questions.length === 0 ? <EmptyState title="Savollar hali qo‘shilmagan" icon="inbox" /> : form.questions.map((question, index) => (
              <QuestionEditor key={index} index={index} question={question} form={form} setForm={setForm} />
            ))}
          </EditorSection>
        </div>

        <aside className="space-y-6 xl:sticky xl:top-4">
          <EditorSection title="Context gaplar">
            <FormField label="Context gap metadata" htmlFor="reading-context-gaps"><DesignTextarea id="reading-context-gaps" rows={12} value={form.contextGaps ?? ""} onChange={(event) => setForm({ ...form, contextGaps: event.target.value })} placeholder="Context gap metadata..." /></FormField>
          </EditorSection>
          <DesignCard as="section" className="space-y-4">
            <SectionHeader title="Database ma’lumoti" />
            <dl className="space-y-3 text-sm">
              <Fact label="ID" value={data.id} />
              <Fact label="Vocabulary topic" value={data.vocabularyTopicId ?? "Bog‘lanmagan"} />
              <Fact label="Created" value={formatDateTime(data.createdAt)} />
              <Fact label="Sections" value={String(form.sections.length)} />
              <Fact label="Vocabulary" value={String(form.vocabulary.length)} />
              <Fact label="Questions" value={String(form.questions.length)} />
            </dl>
          </DesignCard>
          {validation && <ErrorState title={validation} />}
          {saveError && <ErrorState title={saveError} action={<AppButton tone="standard" onClick={() => void save()}>Qayta saqlash</AppButton>} />}
        </aside>
      </div>
      <DesignToast open={Boolean(toast)} title={toast ?? ""} tone="success" onClose={() => setToast(null)} />
    </main>
  );
}

function QuestionEditor({ index, question, form, setForm }: { index: number; question: EditorState["questions"][number]; form: EditorState; setForm: React.Dispatch<React.SetStateAction<EditorState | null>> }) {
  const setQuestions = (questions: EditorState["questions"]) => setForm({ ...form, questions });
  return (
    <EditorItem
      title={`Savol ${index + 1}`}
      onUp={() => setQuestions(move(form.questions, index, -1))}
      onDown={() => setQuestions(move(form.questions, index, 1))}
      onDelete={() => setQuestions(form.questions.filter((_, itemIndex) => itemIndex !== index))}
    >
      <FormField label="Question" htmlFor={`reading-question-${index}`}><DesignTextarea id={`reading-question-${index}`} rows={3} value={question.prompt} onChange={(event) => setQuestions(update(form.questions, index, "prompt", event.target.value))} /></FormField>
      <DesignCard padding="sm" className="space-y-3">
        {question.options.map((option, optionIndex) => (
          <div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2" key={optionIndex}>
            <DesignRadio name={`correct-${index}`} checked={question.correctOptionIndex === optionIndex} onChange={() => setQuestions(update(form.questions, index, "correctOptionIndex", optionIndex))} label={`${optionIndex + 1}-variant to‘g‘ri`} />
            <DesignInput aria-label={`${optionIndex + 1}-variant matni`} value={option} onChange={(event) => setQuestions(updateOption(form.questions, index, optionIndex, event.target.value))} />
            <AppIconButton icon="delete" tone="danger" disabled={question.options.length <= 2} onClick={() => setQuestions(removeOption(form.questions, index, optionIndex))} label={`${optionIndex + 1}-variantni o‘chirish`} />
          </div>
        ))}
        <AppButton type="button" tone="standard" leadingIcon="add" onClick={() => setQuestions(addOption(form.questions, index))}>Variant qo‘shish</AppButton>
      </DesignCard>
      <div className="grid gap-4 sm:grid-cols-2">
        <FormField label="Hint code" htmlFor={`reading-question-hint-${index}`}><DesignInput id={`reading-question-hint-${index}`} value={question.hintCode ?? ""} onChange={(event) => setQuestions(update(form.questions, index, "hintCode", event.target.value))} /></FormField>
        <FormField label="Explanation" htmlFor={`reading-question-explanation-${index}`}><DesignTextarea id={`reading-question-explanation-${index}`} rows={3} value={question.explanation ?? ""} onChange={(event) => setQuestions(update(form.questions, index, "explanation", event.target.value))} /></FormField>
      </div>
    </EditorItem>
  );
}

function EditorSection({ title, onAdd, children }: { title: string; onAdd?: () => void; children: React.ReactNode }) {
  return (
    <DesignCard as="section" className="space-y-4">
      <SectionHeader title={title} actions={onAdd ? <AppButton type="button" tone="standard" leadingIcon="add" onClick={onAdd}>Qo‘shish</AppButton> : undefined} />
      <div className="space-y-4">{children}</div>
    </DesignCard>
  );
}

function EditorItem({ title, onUp, onDown, onDelete, children }: { title: string; onUp: () => void; onDown: () => void; onDelete: () => void; children: React.ReactNode }) {
  return (
    <DesignCard as="article" padding="sm" className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h3 className="font-bold text-ea-text">{title}</h3>
        <div className="flex gap-2">
          <AppIconButton icon="arrow_back" label={`${title} yuqoriga`} onClick={onUp} />
          <AppIconButton icon="arrow_forward" label={`${title} pastga`} onClick={onDown} />
          <AppIconButton icon="delete" label={`${title} o‘chirish`} tone="danger" onClick={onDelete} />
        </div>
      </div>
      {children}
    </DesignCard>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return <div className="border-b border-ea-border pb-3 last:border-b-0 last:pb-0"><dt className="text-xs uppercase tracking-wide text-ea-muted">{label}</dt><dd className="mt-1 break-all font-bold text-ea-text">{value}</dd></div>;
}

function replace<T>(items: T[], index: number, value: T) {
  return items.map((item, itemIndex) => itemIndex === index ? value : item);
}

function move<T>(items: T[], index: number, offset: number) {
  const target = index + offset;
  if (target < 0 || target >= items.length) return items;
  const next = [...items];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

function update<T extends object, K extends keyof T>(items: T[], index: number, key: K, value: T[K]) {
  return items.map((item, itemIndex) => itemIndex === index ? { ...item, [key]: value } : item);
}

function updateOption(questions: EditorState["questions"], questionIndex: number, optionIndex: number, value: string) {
  const question = questions[questionIndex];
  return replace(questions, questionIndex, { ...question, options: replace(question.options, optionIndex, value) });
}

function addOption(questions: EditorState["questions"], questionIndex: number) {
  const question = questions[questionIndex];
  return replace(questions, questionIndex, { ...question, options: [...question.options, ""] });
}

function removeOption(questions: EditorState["questions"], questionIndex: number, optionIndex: number) {
  const question = questions[questionIndex];
  if (question.options.length <= 2) return questions;
  const options = question.options.filter((_, index) => index !== optionIndex);
  return replace(questions, questionIndex, {
    ...question,
    options,
    correctOptionIndex: question.correctOptionIndex === optionIndex ? 0 : Math.min(question.correctOptionIndex, options.length - 1),
  });
}

function validate(form: EditorState | null) {
  if (!form) return null;
  if (!form.title.trim() || !form.topic.trim() || !form.category.trim()) return "Title, topic va category majburiy.";
  if (form.status === "Filled" && form.sections.every((section) => !section.trim())) return "Filled passage uchun section kerak.";
  if (form.vocabulary.some((entry) => !entry.word.trim() || !entry.translation.trim())) return "Vocabulary word va translation to‘liq bo‘lishi kerak.";
  if (form.questions.some((question) => !question.prompt.trim() || question.options.length < 2 || question.options.some((option) => !option.trim()) || question.correctOptionIndex >= question.options.length)) return "Savollarda prompt, variantlar va correct answer bo‘lishi kerak.";
  if (form.status === "Filled" && !form.questions.length) return "Filled passage uchun comprehension savoli kerak.";
  return null;
}
