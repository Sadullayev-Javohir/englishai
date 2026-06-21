import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { Mic, Pause, Play, RotateCcw } from "lucide-react";
import { api } from "@/api/client";
import type { TopicWordDto, VocabularyTopicDetailDto, WordPronunciationCheckDto } from "@/api/types";
import { ModulePageLoader } from "@/components/ui/ModulePageLoader";
import { VocabularyAction, VocabularyProgress } from "@/components/vocabulary/VocabularyChrome";
import { useAsync } from "@/lib/useAsync";
import { useMicRecorder } from "@/lib/useMicRecorder";
import { blobToWav16kMono, bytesToBase64 } from "@/lib/audio";
import { useVocabularyVoice } from "@/components/vocabulary/useVocabularyVoice";
import { MicFrequencyBars } from "@/components/speaking/MicFrequencyBars";
import { publishAssistantContext } from "@/components/assistantContext";
import "./VocabularyPronunciationPage.css";

export function VocabularyPronunciationPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { topicId = "", wordIndex = "" } = useParams();
  const parsedIndex = /^\d+$/.test(wordIndex) ? Number(wordIndex) : -1;
  const { data: topic, loading, error, reload } = useAsync(() => api.vocabulary.topic(topicId), [topicId]);
  const word = topic?.words?.[parsedIndex];
  const goBack = () => navigate(`/app/vocabulary/topic/${topicId}`, { state: location.state ?? { vocabularyReturn: { topicId, index: Math.max(0, parsedIndex), learned: [] } } });
  if (loading) return <ModulePageLoader icon="mic" accent="purple" />;
  if (error || !topic?.isReady || !word) return <section className="vocabulary-state" role="alert"><h1>Talaffuz mashqini yuklab bo‘lmadi</h1><VocabularyAction onClick={reload}>Qayta urinish</VocabularyAction><button type="button" onClick={goBack}>So‘zga qaytish</button></section>;
  return <VocabularyPronunciationPractice key={`${topicId}:${parsedIndex}`} topic={topic} word={word} index={parsedIndex} onBack={goBack} />;
}

export function VocabularyPronunciationPractice({ topic, word, index, onBack }: { topic: VocabularyTopicDetailDto; word: TopicWordDto; index: number; onBack: () => void }) {
  const detail = useAsync(() => api.speaking.wordDetail(word.word), [word.word]);
  const [result, setResult] = useState<WordPronunciationCheckDto | null>(null);
  const [checking, setChecking] = useState(false);
  const [assessmentError, setAssessmentError] = useState<string | null>(null);
  const [slow, setSlow] = useState(false);
  const voice = useVocabularyVoice(word.word);
  const playing = voice.state === "playing";
  const active = useRef(false);
  const controller = useRef<AbortController | null>(null);
  useEffect(() => {
    active.current = true;
    return () => { active.current = false; controller.current?.abort(); };
  }, []);
  useEffect(() => publishAssistantContext({ area: "vocabulary", resourceId: topic.id, title: `${word.word} — talaffuz`, context: `${word.word} — ${word.translation}\n${word.ipa}\n${word.exampleSentence}`, focusText: word.word, route: `/app/vocabulary/topic/${topic.id}/pronunciation/${index}`, stage: "pronunciation" }), [index, topic.id, word]);
  const recorder = useMicRecorder(async blob => {
    if (!active.current) return;
    setChecking(true); setAssessmentError(null);
    const attempt = new AbortController();
    controller.current = attempt;
    const timeout = window.setTimeout(() => attempt.abort(), 20_000);
    try {
      const wav = await blobToWav16kMono(blob);
      if (!active.current) return;
      if (attempt.signal.aborted) throw new Error("Audio conversion timed out");
      const response = await api.vocabulary.pronounce(word.word, bytesToBase64(wav), attempt.signal);
      if (active.current) setResult(response);
    } catch {
      if (active.current) { setResult(null); setAssessmentError(attempt.signal.aborted ? "Tekshirish vaqti tugadi. Qayta urinib ko‘ring." : "Talaffuzni tekshirib bo‘lmadi. Qayta urinib ko‘ring."); }
    } finally { window.clearTimeout(timeout); if (controller.current === attempt) controller.current = null; if (active.current) setChecking(false); }
  });
  const validScore = result?.recognized && result.isAuthentic && Number.isFinite(result.overallScore);
  function listen() {
    if (recorder.status === "recording" || checking) return;
    voice.play(detail.data?.audioBase64, slow ? .6 : 1);
  }
  async function record() {
    if (recorder.status === "recording") { recorder.stop(); return; }
    voice.stop();
    setResult(null); setAssessmentError(null);
    await recorder.start();
  }
  return <section className="vocabulary-pronunciation" data-pen-screen="19">
    <VocabularyProgress step="flashcards" substep="Talaffuz mashqi" onBack={onBack} />
    <div className="vocabulary-pronunciation__practice">
      <span className="vocabulary-tag">{topic.title?.toLocaleUpperCase("en")} · {String(index + 1).padStart(2, "0")} / {topic.words.length}</span>
      <h1>{word.word}</h1><p className="vocabulary-pronunciation__ipa">{word.ipa || detail.data?.ipa || "Talaffuz tayyorlanmoqda"}</p>
      <div className={`vocabulary-voice${playing ? " is-playing" : ""}`} data-playback-state={voice.state}>
        <div className="vocabulary-voice__player"><button type="button" onClick={playing || voice.state === "loading" ? voice.stop : listen} aria-label={playing || voice.state === "loading" ? "Namunani to‘xtatish" : "Namunani tinglash"} aria-busy={voice.state === "loading"} disabled={recorder.status === "recording" || checking}>{playing ? <Pause size={22} /> : <Play size={22} />}</button>
          <div className="vocabulary-voice__wave" aria-hidden="true">{[12,26,18,36,24,16,10,18,30,14,35,20,12,14,28,34,18].map((height, i) => <span key={i} style={{ height }} />)}</div>
          <button type="button" className="vocabulary-voice__speed" aria-label="Ovoz tezligini o‘zgartirish" onClick={() => setSlow(!slow)}>{slow ? "0.6×" : "1×"}</button></div>
        <div className="vocabulary-voice__caption"><span>Namuna · British English</span><button type="button" onClick={listen} disabled={recorder.status === "recording" || checking}><RotateCcw size={13} /> Qayta tinglash</button></div>
        {voice.state === "error" && <p role="status">Ovozni ijro qilib bo‘lmadi.</p>}
      </div>
      {validScore ? <>
        <div className="vocabulary-pronunciation__score" role="status"><strong>{Math.round(Math.max(0, Math.min(100, result.overallScore)))} / 100</strong><p>{result.feedbackUz || "Natijani ko‘rib, yana mashq qiling."}</p></div>
        <div className="vocabulary-pronunciation__phonemes">{result.phonemes.map((phoneme, i) => <div key={i} className={phoneme.accuracyScore >= 80 ? "is-good" : "is-review"}><strong>{phoneme.phoneme}</strong><span className="sr-only">{Math.round(phoneme.accuracyScore)} / 100</span></div>)}</div>
        {detail.data?.tipUz && <p className="vocabulary-pronunciation__tip">{detail.data.tipUz}</p>}
      </> : <div className={`vocabulary-pronunciation__empty${recorder.status === "recording" ? " is-recording" : ""}`} role="status"><Mic size={38} />
        {recorder.status === "recording" && <MicFrequencyBars stream={recorder.stream} className="vocabulary-mic-wave" />}
        <p>{checking ? "Talaffuz tekshirilmoqda…" : recorder.status === "recording" ? "Tinglayapmiz. Tugatish uchun tugmani bosing." : assessmentError || (recorder.micError ? "Mikrofonga ruxsat berilmadi. Brauzer sozlamalarini tekshiring." : result ? result.isAuthentic ? "Ovoz aniqlanmadi. So‘zni qayta ayting." : "Ishonchli baho olinmadi. Qayta urinib ko‘ring." : "Namunani tinglang, keyin so‘zni ayting.")}</p></div>}
    </div>
    <footer className="vocabulary-slide__actions"><VocabularyAction onClick={() => void record()} disabled={checking}>{recorder.status === "recording" ? "Yozishni tugatish" : result ? "Yana bir marta aytish" : "So‘zni aytish"}</VocabularyAction><button type="button" className="vocabulary-secondary" onClick={onBack}>So‘zga qaytish</button></footer>
  </section>;
}
