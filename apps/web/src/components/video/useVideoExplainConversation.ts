import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError, api } from "@/api/client";
import type { ChatTurn } from "@/api/types";
import { uz } from "@/content/uz";

export interface VideoExplainMessage {
  role: "user" | "assistant";
  text: string;
  /** True when the assistant request failed; the text is a vetted local fallback. */
  failed?: boolean;
  streaming?: boolean;
}

export interface VideoExplainConversation {
  messages: VideoExplainMessage[];
  input: string;
  setInput: (value: string) => void;
  sending: boolean;
  focusToken: number;
  focusInput: () => void;
  prefill: (text: string) => void;
  send: (questionOverride?: string) => Promise<void>;
}

/** Trailing turns resent as follow-up context (also capped server-side). */
const MAX_HISTORY_TURNS = 4;
const CONVERSATION_TTL_MS = 24 * 60 * 60 * 1000;
const STORAGE_KEY_PREFIX = "englishai:video-explain:";

interface StoredConversation {
  expiresAt: number;
  messages: VideoExplainMessage[];
  input: string;
}

function storageKey(videoLessonId: string): string {
  return `${STORAGE_KEY_PREFIX}${videoLessonId}`;
}

function isStoredMessage(value: unknown): value is VideoExplainMessage {
  if (!value || typeof value !== "object") return false;
  const message = value as Partial<VideoExplainMessage>;
  return (message.role === "user" || message.role === "assistant")
    && typeof message.text === "string"
    && (message.failed === undefined || typeof message.failed === "boolean");
}

function loadConversation(videoLessonId: string): Pick<StoredConversation, "messages" | "input"> {
  if (typeof window === "undefined") return { messages: [], input: "" };
  const key = storageKey(videoLessonId);
  try {
    const raw = window.localStorage.getItem(key);
    if (!raw) return { messages: [], input: "" };
    const stored = JSON.parse(raw) as Partial<StoredConversation>;
    if (typeof stored.expiresAt !== "number" || stored.expiresAt <= Date.now()) {
      window.localStorage.removeItem(key);
      return { messages: [], input: "" };
    }
    if (!Array.isArray(stored.messages) || !stored.messages.every(isStoredMessage)) {
      window.localStorage.removeItem(key);
      return { messages: [], input: "" };
    }
    return {
      messages: stored.messages.map((message) => ({
        role: message.role,
        text: message.text,
        ...(message.failed === undefined ? {} : { failed: message.failed }),
      })),
      input: typeof stored.input === "string" ? stored.input : "",
    };
  } catch {
    window.localStorage.removeItem(key);
    return { messages: [], input: "" };
  }
}

function saveConversation(videoLessonId: string, messages: VideoExplainMessage[], input: string): void {
  if (typeof window === "undefined") return;
  const key = storageKey(videoLessonId);
  if (messages.length === 0 && input.length === 0) {
    window.localStorage.removeItem(key);
    return;
  }
  try {
    window.localStorage.setItem(key, JSON.stringify({
      expiresAt: Date.now() + CONVERSATION_TTL_MS,
      messages: messages.map((message) => ({
        role: message.role,
        text: message.text,
        ...(message.failed === undefined ? {} : { failed: message.failed }),
      })),
      input,
    } satisfies StoredConversation));
  } catch {
    // Storage can be unavailable in private browsing or when the quota is full.
  }
}

function getFailureMessage(error: unknown): string {
  if (!(error instanceof ApiError)) return uz.videoChat.networkError;
  if (error.status === 0) return uz.videoChat.networkError;
  if (error.status === 401 || error.status === 403) return uz.videoChat.unauthorized;
  if (error.status === 429) return uz.videoChat.rateLimited;
  if (error.status === 400 || error.status === 422) return uz.videoChat.invalidQuestion;
  return uz.videoChat.unavailable;
}

/**
 * Owns one video lesson's explain-chat. Multiple panels may render this controller, but only this
 * hook can issue the AI request, so desktop/fullscreen views cannot spend tokens independently.
 */
export function useVideoExplainConversation(
  videoLessonId: string,
  getContextText: () => string,
  sentenceKey?: string,
): VideoExplainConversation {
  const conversationId = sentenceKey === undefined ? videoLessonId : `${videoLessonId}:${sentenceKey}`;
  const [messages, setMessages] = useState<VideoExplainMessage[]>([]);
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);
  const [focusToken, setFocusToken] = useState(0);
  const messagesRef = useRef<VideoExplainMessage[]>([]);
  const inFlightRef = useRef(false);
  const lessonRef = useRef(conversationId);
  const restoredRef = useRef(false);
  const generationRef = useRef(0);
  const inputRef = useRef(input);

  useEffect(() => {
    // Persist the previous sentence before loading another; never write stale turns under
    // the new sentence key during React's effect transition.
    if (restoredRef.current) saveConversation(lessonRef.current, messagesRef.current, inputRef.current);
    const stored = loadConversation(conversationId);
    generationRef.current += 1;
    lessonRef.current = conversationId;
    restoredRef.current = true;
    messagesRef.current = stored.messages;
    inFlightRef.current = false;
    setMessages(stored.messages);
    setInput(stored.input);
    setSending(false);
  }, [conversationId]);

  useEffect(() => {
    inputRef.current = input;
    if (messagesRef.current !== messages) return;
    saveConversation(conversationId, messages, input);
  }, [input, messages, conversationId]);

  useEffect(() => () => { generationRef.current += 1; }, []);

  const updateMessages = useCallback((update: (current: VideoExplainMessage[]) => VideoExplainMessage[]) => {
    setMessages((current) => {
      const next = update(current);
      messagesRef.current = next;
      return next;
    });
  }, []);

  const focusInput = useCallback(() => setFocusToken((token) => token + 1), []);

  const prefill = useCallback((text: string) => {
    setInput(text);
    setFocusToken((token) => token + 1);
  }, []);

  const send = useCallback(async (questionOverride?: string) => {
    const question = (questionOverride ?? input).trim();
    if (!question || inFlightRef.current) return;

    const requestLessonId = videoLessonId;
    const requestConversationId = conversationId;
    const requestGeneration = generationRef.current;
    const isCurrent = () => lessonRef.current === requestConversationId && generationRef.current === requestGeneration;
    const focusText = getContextText().trim();
    const history: ChatTurn[] = messagesRef.current
      .filter((message) => !message.failed)
      .slice(-MAX_HISTORY_TURNS)
      .map(({ role, text }) => ({ role, text }));

    inFlightRef.current = true;
    updateMessages((current) => [...current, { role: "user", text: question }]);
    setInput("");
    setSending(true);

    try {
      let streamedText = "";
      const result = await api.video.explainStream(requestLessonId, focusText, question, history, (chunk) => {
        if (!isCurrent()) return;
        streamedText += chunk;
        updateMessages((current) => current.at(-1)?.role === "assistant" && current.at(-1)?.streaming
          ? current.map((message, messageIndex) => messageIndex === current.length - 1 ? { ...message, text: streamedText } : message)
          : [...current, { role: "assistant", text: streamedText, streaming: true }]);
      });
      if (!isCurrent()) return;
      if (!result.replyUz) {
        updateMessages((current) => current.at(-1)?.role === "assistant"
          ? current.map((message, messageIndex) => messageIndex === current.length - 1 ? { role: "assistant", text: uz.videoChat.unavailable, failed: true } : message)
          : [...current, { role: "assistant", text: uz.videoChat.unavailable, failed: true }]);
        return;
      }
      updateMessages((current) => current.at(-1)?.role === "assistant"
        ? current.map((message, messageIndex) => messageIndex === current.length - 1 ? { ...message, text: result.replyUz ?? message.text, streaming: false } : message)
        : [...current, { role: "assistant", text: result.replyUz!, streaming: false }]);
    } catch (error) {
      if (!isCurrent()) return;
      const failureMessage = getFailureMessage(error);
      updateMessages((current) => {
        const last = current.at(-1);
        return last?.role === "assistant" && last.text === ""
          ? current.map((message, messageIndex) => messageIndex === current.length - 1
            ? { role: "assistant", text: failureMessage, failed: true }
            : message)
          : [...current, { role: "assistant", text: failureMessage, failed: true }];
      });
    } finally {
      if (isCurrent()) {
        inFlightRef.current = false;
        setSending(false);
      }
    }
  }, [conversationId, getContextText, input, updateMessages, videoLessonId]);

  return { messages, input, setInput, sending, focusToken, focusInput, prefill, send };
}
