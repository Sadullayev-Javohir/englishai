import { useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "@/api/client";
import type { AdminListeningExerciseDetailDto, AdminListeningExerciseFullUpdateDto } from "@/api/types";
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
  ErrorState,
  FormField,
  PageHeader,
  ResponsiveGrid,
  SectionHeader,
  StatCard,
} from "@/components/design";
import { LoadingSkeleton } from "@/components/ui/LoadingSkeleton";
import { useAsync } from "@/lib/useAsync";

const LEVELS = ["A1", "A2", "B1", "B2", "C1", "C2"];
type FormState = AdminListeningExerciseFullUpdateDto;

function toForm(data: AdminListeningExerciseDetailDto): FormState {
  return {
    title: data.title,
    topic: data.topic,
    level: data.level,
    status: data.status,
    vocabularyTopicId: data.vocabularyTopicId,
    transcript: data.transcript,
    segments: data.segments.map((segment) => ({ ...segment })),
    questions: data.questions.map((question) => ({ ...question, options: [...question.options] })),
  };
}

export function AdminListeningEditPage() {
  const { id } = useParams<{ id: string }>();
  const { data, loading, error, reload } = useAsync(
    () => id ? api.admin.listening.get(id) : Promise.reject(new Error("Missing listening exercise id")),
    [id],
    Boolean(id),
  );
  useDocumentTitle(data?.title, "Listening tahrirlash");

  return (
    <main className="ea-page-boundary space-y-6 pb-12">
      <PageHeader
        eyebrow="Listening boshqaruvi"
        title="Listening mashqini tahrirlash"
        description="Transcript, audio segmentlar va test savollarini bitta joyda boshqaring."
        actions={<AppButton tone="standard" leadingIcon="close" onClick={() => window.close()}>Tahrirlash oynasini yopish</AppButton>}
      />
      {loading && <LoadingSkeleton variant="form" />}
      {(error || (!loading && !data)) && (
        <ErrorState
          title="Mashqni yuklab bo‘lmadi"
          description="Internet aloqasi yoki server holatini tekshirib, qayta urinib ko‘ring."
          action={<AppButton tone="standard" leadingIcon="refresh" onClick={reload}>Qayta urinish</AppButton>}
        />
      )}
      {data && id && <ListeningEditor id={id} initial={toForm(data)} />}
    </main>
  );
}

function ListeningEditor({ id, initial }: { id: string; initial: FormState }) {
  const [form, setForm] = useState(initial);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const validation = validate(form);
  const transcriptWords = form.transcript.trim() ? form.transcript.trim().split(/\s+/).length : 0;

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
  };

  const requestSave = (event: React.FormEvent) => {
    event.preventDefault();
    if (validation) {
      setSaveError(validation);
      return;
    }
    setSaveError(null);
    setConfirmOpen(true);
  };

  const save = async () => {
    setSaving(true);
    setSaveError(null);
    try {
      await api.admin.listening.updateFull(id, form);
      window.opener?.location.reload();
      window.close();
    } catch {
      setSaveError("Saqlashda xatolik yuz berdi. Qayta urinib ko‘ring.");
      setConfirmOpen(false);
    } finally {
      setSaving(false);
    }
  };

  const updateSegment = (index: number, patch: Partial<FormState["segments"][number]>) => {
    set("segments", form.segments.map((segment, itemIndex) => itemIndex === index ? { ...segment, ...patch } : segment));
  };

  const updateQuestion = (index: number, patch: Partial<FormState["questions"][number]>) => {
    set("questions", form.questions.map((question, itemIndex) => itemIndex === index ? { ...question, ...patch } : question));
  };

  const removeOption = (questionIndex: number, optionIndex: number) => {
    const question = form.questions[questionIndex];
    if (question.options.length <= 2) return;
    const options = question.options.filter((_, index) => index !== optionIndex);
    const correctOptionIndex = question.correctOptionIndex === optionIndex
      ? 0
      : question.correctOptionIndex > optionIndex
        ? question.correctOptionIndex - 1
        : question.correctOptionIndex;
    updateQuestion(questionIndex, { options, correctOptionIndex });
  };

  return (
    <form className="space-y-6" onSubmit={requestSave}>
      <ResponsiveGrid minItemWidth={200} aria-label="Mashq statistikasi">
        <StatCard icon="subject" value={transcriptWords} label="Transcript so‘zi" />
        <StatCard icon="graphic_eq" value={form.segments.length} label="Audio segment" />
        <StatCard icon="quiz" value={form.questions.length} label="Test savoli" />
        <StatCard icon="school" value={form.level} label="CEFR daraja" />
      </ResponsiveGrid>

      <EditorSection title="Asosiy ma’lumotlar" description="Mashq katalogda qanday ko‘rinishini belgilang.">
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <FormField label="Mashq nomi" htmlFor="listening-edit-title" help="O‘quvchiga ko‘rinadigan sarlavha"><DesignInput id="listening-edit-title" value={form.title} onChange={(event) => set("title", event.target.value)} placeholder="Masalan: At the airport" /></FormField>
          <FormField label="Mavzu" htmlFor="listening-edit-topic" help="Katalogdagi mavzu nomi"><DesignInput id="listening-edit-topic" value={form.topic} onChange={(event) => set("topic", event.target.value)} placeholder="Masalan: Travel" /></FormField>
          <FormField label="CEFR daraja" htmlFor="listening-edit-level"><DesignSelect id="listening-edit-level" value={form.level} onChange={(event) => set("level", event.target.value)}>{LEVELS.map((level) => <option key={level}>{level}</option>)}</DesignSelect></FormField>
          <FormField label="Holati" htmlFor="listening-edit-status"><DesignSelect id="listening-edit-status" value={form.status} onChange={(event) => set("status", event.target.value)}><option value="Pending">Tayyorlanmoqda</option><option value="Filled">Tayyor</option></DesignSelect></FormField>
        </div>
        <FormField label="Vocabulary mavzu ID" htmlFor="listening-edit-vocabulary-topic" help="Ixtiyoriy UUID — tegishli lug‘at mavzusiga bog‘laydi"><DesignInput id="listening-edit-vocabulary-topic" value={form.vocabularyTopicId ?? ""} onChange={(event) => set("vocabularyTopicId", event.target.value.trim() || null)} placeholder="UUID kiriting yoki bo‘sh qoldiring" /></FormField>
      </EditorSection>

      <EditorSection title="Transcript" description="Audio yozuvning to‘liq va tekshirilgan matnini kiriting." aside={`${transcriptWords} ta so‘z`}>
        <FormField label="To‘liq transcript" htmlFor="listening-edit-transcript"><DesignTextarea id="listening-edit-transcript" rows={12} value={form.transcript} onChange={(event) => set("transcript", event.target.value)} placeholder="Audio matnini shu yerga yozing..." /></FormField>
      </EditorSection>

      <EditorSection
        title="Audio segmentlar"
        description="Transcriptni vaqt oralig‘i va speaker bo‘yicha bo‘ling."
        onAdd={() => set("segments", [...form.segments, { id: null, order: form.segments.length + 1, startMs: 0, endMs: 1000, speaker: "", text: "" }])}
      >
        {form.segments.length === 0 ? <ErrorState title="Segmentlar yo‘q" description="Audio qismlarini vaqt bo‘yicha belgilash uchun birinchi segmentni qo‘shing." /> : null}
        <div className="space-y-4">
          {form.segments.map((segment, index) => (
            <DesignCard as="article" padding="sm" className="space-y-4" key={segment.id ?? index}>
              <ItemHeading title={`Segment ${index + 1}`} subtitle={`${formatTime(segment.startMs)} — ${formatTime(segment.endMs)}`} onDelete={() => set("segments", form.segments.filter((_, itemIndex) => itemIndex !== index))} />
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <FormField label="Tartib raqami" htmlFor={`listening-segment-order-${index}`}><DesignInput id={`listening-segment-order-${index}`} type="number" min="1" value={segment.order} onChange={(event) => updateSegment(index, { order: Number(event.target.value) })} /></FormField>
                <FormField label="Boshlanish (ms)" htmlFor={`listening-segment-start-${index}`}><DesignInput id={`listening-segment-start-${index}`} type="number" min="0" value={segment.startMs} onChange={(event) => updateSegment(index, { startMs: Number(event.target.value) })} /></FormField>
                <FormField label="Tugash (ms)" htmlFor={`listening-segment-end-${index}`}><DesignInput id={`listening-segment-end-${index}`} type="number" min="0" value={segment.endMs} onChange={(event) => updateSegment(index, { endMs: Number(event.target.value) })} /></FormField>
                <FormField label="Speaker" htmlFor={`listening-segment-speaker-${index}`}><DesignInput id={`listening-segment-speaker-${index}`} value={segment.speaker} onChange={(event) => updateSegment(index, { speaker: event.target.value })} placeholder="Masalan: Speaker 1" /></FormField>
              </div>
              <FormField label="Segment matni" htmlFor={`listening-segment-text-${index}`}><DesignTextarea id={`listening-segment-text-${index}`} rows={3} value={segment.text} onChange={(event) => updateSegment(index, { text: event.target.value })} placeholder="Ushbu vaqt oralig‘idagi matn..." /></FormField>
            </DesignCard>
          ))}
        </div>
      </EditorSection>

      <EditorSection
        title="Test savollari"
        description="O‘quvchining tinglab tushunishini tekshiradigan savollar."
        onAdd={() => set("questions", [...form.questions, { id: null, prompt: "", options: ["", ""], correctOptionIndex: 0, hintCode: null, explanation: null }])}
      >
        {form.questions.length === 0 ? <ErrorState title="Savollar yo‘q" description="Listening mashqi uchun birinchi test savolini qo‘shing." /> : null}
        <div className="space-y-4">
          {form.questions.map((question, index) => (
            <DesignCard as="article" padding="sm" className="space-y-4" key={question.id ?? index}>
              <ItemHeading title={`Savol ${index + 1}`} subtitle={`${question.options.length} ta javob varianti`} onDelete={() => set("questions", form.questions.filter((_, itemIndex) => itemIndex !== index))} />
              <FormField label="Savol matni" htmlFor={`listening-question-${index}`}><DesignTextarea id={`listening-question-${index}`} rows={3} value={question.prompt} onChange={(event) => updateQuestion(index, { prompt: event.target.value })} placeholder="Savolni ingliz tilida yozing..." /></FormField>
              <DesignCard padding="sm" className="space-y-3">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div><strong className="text-ea-text">Javob variantlari</strong><p className="mt-1 text-xs text-ea-muted">To‘g‘ri javob yonidagi doirani belgilang.</p></div>
                  <AppButton type="button" tone="standard" leadingIcon="add" onClick={() => updateQuestion(index, { options: [...question.options, ""] })}>Variant qo‘shish</AppButton>
                </div>
                {question.options.map((option, optionIndex) => (
                  <div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2" key={optionIndex}>
                    <DesignRadio name={`correct-${index}`} checked={question.correctOptionIndex === optionIndex} onChange={() => updateQuestion(index, { correctOptionIndex: optionIndex })} label={String.fromCharCode(65 + optionIndex)} />
                    <DesignInput aria-label={`${index + 1}-savol ${optionIndex + 1}-variant`} value={option} onChange={(event) => updateQuestion(index, { options: question.options.map((value, itemIndex) => itemIndex === optionIndex ? event.target.value : value) })} placeholder={`${optionIndex + 1}-javob variantini kiriting`} />
                    <AppIconButton icon="close" label={`${optionIndex + 1}-variantni o‘chirish`} tone="danger" disabled={question.options.length <= 2} onClick={() => removeOption(index, optionIndex)} />
                  </div>
                ))}
              </DesignCard>
              <div className="grid gap-4 sm:grid-cols-2">
                <FormField label="Yordam kodi" htmlFor={`listening-question-hint-${index}`} help="Ixtiyoriy ichki hint kodi"><DesignInput id={`listening-question-hint-${index}`} value={question.hintCode ?? ""} onChange={(event) => updateQuestion(index, { hintCode: event.target.value || null })} placeholder="Masalan: listen-location" /></FormField>
                <FormField label="Javob izohi" htmlFor={`listening-question-explanation-${index}`} help="Nega shu javob to‘g‘ri ekanini tushuntiring"><DesignTextarea id={`listening-question-explanation-${index}`} rows={3} value={question.explanation ?? ""} onChange={(event) => updateQuestion(index, { explanation: event.target.value || null })} placeholder="Qisqa va tushunarli izoh..." /></FormField>
              </div>
            </DesignCard>
          ))}
        </div>
      </EditorSection>

      {(validation || saveError) && <ErrorState title={saveError || validation || "Tekshiruv xatosi"} />}
      <DesignCard className="sticky bottom-3 z-10 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between" padding="sm">
        <div><strong className="text-ea-text">O‘zgarishlarni tekshiring</strong><p className="mt-1 text-xs text-ea-muted">Saqlash barcha bo‘limlarni bir vaqtda yangilaydi.</p></div>
        <div className="flex flex-col gap-2 sm:flex-row">
          <AppButton type="button" tone="standard" onClick={() => window.close()}>Bekor qilish</AppButton>
          <AppButton type="submit" leadingIcon="check_circle" disabled={Boolean(validation)} loading={saving}>O‘zgarishlarni saqlash</AppButton>
        </div>
      </DesignCard>

      <DesignConfirm
        open={confirmOpen}
        onClose={() => { if (!saving) setConfirmOpen(false); }}
        title="Listening mashqini saqlash"
        message="Listening mashqining barcha ma’lumotlari saqlansinmi?"
        confirmLabel="O‘zgarishlarni saqlash"
        loading={saving}
        closeOnBackdrop={!saving}
        closeOnEscape={!saving}
        onConfirm={() => void save()}
      />
    </form>
  );
}

function EditorSection({ title, description, aside, onAdd, children }: { title: string; description: string; aside?: string; onAdd?: () => void; children: React.ReactNode }) {
  return (
    <DesignCard as="section" className="space-y-4">
      <SectionHeader
        title={title}
        description={description}
        actions={onAdd ? <AppButton type="button" tone="standard" leadingIcon="add" onClick={onAdd}>Qo‘shish</AppButton> : aside ? <span className="rounded-full bg-ea-primary-soft px-3 py-1 text-xs font-extrabold text-ea-primary-deep">{aside}</span> : undefined}
      />
      {children}
    </DesignCard>
  );
}

function ItemHeading({ title, subtitle, onDelete }: { title: string; subtitle: string; onDelete: () => void }) {
  return (
    <header className="flex items-start justify-between gap-3">
      <div><h3 className="font-bold text-ea-text">{title}</h3><p className="mt-1 text-xs text-ea-muted">{subtitle}</p></div>
      <AppIconButton icon="delete" label={`${title}ni o‘chirish`} tone="danger" onClick={onDelete} />
    </header>
  );
}

function formatTime(milliseconds: number) {
  const totalSeconds = Math.max(0, Math.floor(milliseconds / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function validate(form: FormState): string | null {
  if (!form.title.trim() || !form.topic.trim()) return "Mashq nomi va mavzu majburiy.";
  if (form.vocabularyTopicId && !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(form.vocabularyTopicId)) return "Vocabulary mavzu ID to‘g‘ri UUID formatida bo‘lishi kerak.";
  if (form.segments.some((segment) => segment.startMs < 0 || segment.endMs <= segment.startMs || !segment.text.trim())) return "Har bir segmentda matn bo‘lishi va tugash vaqti boshlanish vaqtidan katta bo‘lishi kerak.";
  if (form.questions.some((question) => !question.prompt.trim() || question.options.length < 2 || question.options.some((option) => !option.trim()) || question.correctOptionIndex < 0 || question.correctOptionIndex >= question.options.length)) return "Har bir savolda matn, kamida 2 ta to‘ldirilgan variant va bitta to‘g‘ri javob bo‘lishi kerak.";
  return null;
}
