import { useCallback, useEffect, useMemo, useState } from "react";
import { api } from "@/api/client";
import { TranscriptStatus, type VideoLessonDto } from "@/api/types";
import { sanitizeVideoTranscript } from "@/lib/videoTranscript";

export type TranscriptLoadState = "loading" | "delayed" | "ready" | "unavailable" | "error";
const POLL_MS = 2500;
const DELAYED_MS = 30_000;
const GIVE_UP_MS = 90_000;

function terminalState(lesson: VideoLessonDto): TranscriptLoadState | null {
  if (lesson.transcriptStatus === TranscriptStatus.Unavailable) return lesson.transcript.length ? "ready" : "unavailable";
  if (lesson.transcriptStatus === TranscriptStatus.Available) return lesson.transcript.length ? "ready" : "unavailable";
  return null;
}

/** Display open()'s captions immediately and keep one bounded, non-overlapping refresh loop. */
export function usePlaylistTranscript(opened: VideoLessonDto | null) {
  const initial = useMemo(() => opened ? sanitizeVideoTranscript(opened) : null, [opened]);
  const [lesson, setLesson] = useState<VideoLessonDto | null>(initial);
  const [state, setState] = useState<TranscriptLoadState>("loading");
  const [attempt, setAttempt] = useState(0);
  const retry = useCallback(() => setAttempt(value => value + 1), []);

  useEffect(() => {
    if (!initial) { setLesson(null); setState("loading"); return; }
    let cancelled = false;
    let failures = 0;
    let current = initial;
    let nextPoll: number | undefined;
    setLesson(initial);
    setState(terminalState(initial) ?? "loading");

    const delayedTimer = window.setTimeout(() => {
      if (!terminalState(current)) setState("delayed");
    }, DELAYED_MS);
    const deadlineTimer = window.setTimeout(() => {
      if (terminalState(current)) return;
      cancelled = true;
      window.clearTimeout(nextPoll);
      setState("error");
    }, GIVE_UP_MS);
    const finish = (nextState: TranscriptLoadState) => {
      window.clearTimeout(delayedTimer);
      window.clearTimeout(deadlineTimer);
      setState(nextState);
    };

    const poll = async () => {
      try {
        const raw = await api.video.lesson(initial.id);
        if (cancelled) return;
        if (raw.id !== initial.id || raw.youTubeVideoId !== initial.youTubeVideoId) throw new Error("Mismatched video transcript");
        const fresh = sanitizeVideoTranscript(raw);
        failures = 0;
        // A transient stale read must never erase captions already delivered by open() or polling.
        if (fresh.transcript.length >= current.transcript.length) {
          current = fresh;
          setLesson(fresh); // Also accept same-count corrections and translated lines.
        }
        const terminal = terminalState(current);
        if (terminal) { finish(terminal); return; }
      } catch {
        if (cancelled) return;
        failures += 1;
        if (terminalState(current) === "ready") { finish("ready"); return; }
        if (failures >= 3) { finish("error"); return; }
        setState("delayed");
      }
      nextPoll = window.setTimeout(() => { void poll(); }, POLL_MS);
    };
    void poll();
    return () => {
      cancelled = true;
      window.clearTimeout(nextPoll);
      window.clearTimeout(delayedTimer);
      window.clearTimeout(deadlineTimer);
    };
  }, [attempt, initial]);

  // The initial open response can render before the polling effect has even run.
  const displayed = lesson?.id === initial?.id ? lesson : initial;
  return { lesson: displayed, state, retry };
}
