import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { AnimatePresence, motion } from "framer-motion";
import { api, rateLimitDetails } from "@/api/client";
import type { AssistantMessageDto } from "@/api/types";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { cn } from "@/lib/cn";
import {
  installAssistantContextListener,
  readAssistantContext,
} from "./assistantContext";
import type { AssistantPageContext } from "./assistantContext";
import { useAssistantSessions } from "./useAssistantSessions";
import "./LearningAssistant.css";

interface AssistantMessage {
  role: "user" | "assistant";
  text: string;
  failed?: boolean;
  streaming?: boolean;
  sources?: AssistantMessageDto["sources"];
}

type FormattedSegment =
  | { kind: "text"; value: string }
  | { kind: "bold"; value: string };

const maxChars = 500;
const replyRecoveryAttempts = 3;
const replyRecoveryDelay = 250;

function findAssistantReply(
  messages: AssistantMessageDto[],
  clientRequestId: string
) {
  return [...messages]
    .reverse()
    .find(
      (message) =>
        message.role === "assistant" &&
        message.text.trim() &&
        (!message.clientRequestId ||
          message.clientRequestId === clientRequestId)
    );
}

async function waitForPersistedReply(
  sessionId: string,
  clientRequestId: string,
  pause = true
) {
  for (let attempt = 0; attempt < replyRecoveryAttempts; attempt += 1) {
    if (pause && attempt > 0)
      await new Promise((resolve) =>
        window.setTimeout(resolve, replyRecoveryDelay)
      );
    const restored = await api.assistant.sessions.get(sessionId);
    if (findAssistantReply(restored.messages, clientRequestId)) return restored;
  }
  return null;
}

function formatInline(text: string): FormattedSegment[] {
  const segments: FormattedSegment[] = [];
  const boldPattern = /\*\*([^*\n]+)\*\*/g;
  let cursor = 0;

  for (const match of text.matchAll(boldPattern)) {
    const index = match.index ?? 0;
    if (index > cursor)
      segments.push({ kind: "text", value: text.slice(cursor, index) });
    segments.push({ kind: "bold", value: match[1] });
    cursor = index + match[0].length;
  }

  if (cursor < text.length)
    segments.push({ kind: "text", value: text.slice(cursor) });
  return segments.length > 0 ? segments : [{ kind: "text", value: text }];
}

function InlineText({ text }: { text: string }) {
  return formatInline(text).map((segment, index) =>
    segment.kind === "bold" ? (
      <strong key={`${segment.kind}-${index}`}>{segment.value}</strong>
    ) : (
      <span key={`${segment.kind}-${index}`}>{segment.value}</span>
    )
  );
}

export function AssistantFormattedText({
  text,
  streaming,
  sources,
}: {
  text: string;
  streaming?: boolean;
  sources?: AssistantMessageDto["sources"];
}) {
  const lines = text.replace(/\r\n/g, "\n").split("\n");
  const blocks: React.ReactNode[] = [];
  let listItems: string[] = [];
  let ordered = false;

  const flushList = () => {
    if (listItems.length === 0) return;
    const ListTag = ordered ? "ol" : "ul";
    blocks.push(
      <ListTag key={`list-${blocks.length}`}>
        {listItems.map((item, index) => (
          <li key={`${index}-${item}`}>
            <InlineText text={item} />
          </li>
        ))}
      </ListTag>
    );
    listItems = [];
  };

  lines.forEach((rawLine) => {
    const line = rawLine.trim();
    if (!line) {
      flushList();
      return;
    }

    const heading = line.match(/^#{1,3}\s+(.+)$/);
    const bullet = line.match(/^[-*]\s+(.+)$/);
    const numbered = line.match(/^\d+[.)]\s+(.+)$/);

    if (heading) {
      flushList();
      blocks.push(
        <h4 key={`heading-${blocks.length}`}>
          <InlineText text={heading[1]} />
        </h4>
      );
      return;
    }

    if (bullet || numbered) {
      const nextOrdered = Boolean(numbered);
      if (listItems.length > 0 && ordered !== nextOrdered) flushList();
      ordered = nextOrdered;
      listItems.push((bullet ?? numbered)?.[1] ?? line);
      return;
    }

    flushList();
    blocks.push(
      <p key={`paragraph-${blocks.length}`}>
        <InlineText text={line} />
      </p>
    );
  });
  flushList();

  return (
    <div className="ea-assistant-message__content">
      {blocks}
      {streaming ? (
        <span className="ea-assistant-stream-cursor" aria-hidden="true" />
      ) : null}
      {sources && sources.length > 0 ? (
        <div className="ea-assistant-sources" aria-label="Javob manbalari">
          {sources.map((source) => (
            <a key={`${source.area}:${source.resourceId}`} href={source.route}>
              Manba: {source.title}
            </a>
          ))}
        </div>
      ) : null}
    </div>
  );
}

export function LearningAssistant() {
  const [open, setOpen] = useState(false);
  useEffect(() => {
    const show = () => setOpen(true);
    window.addEventListener("englishai:open-assistant", show);
    return () => window.removeEventListener("englishai:open-assistant", show);
  }, []);
  const [bottomInset, setBottomInset] = useState(0);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const wasOpenRef = useRef(false);

  useEffect(() => {
    if (open) {
      wasOpenRef.current = true;
      return;
    }
    if (!wasOpenRef.current) return;
    const frame = window.requestAnimationFrame(() =>
      triggerRef.current?.focus()
    );
    return () => window.cancelAnimationFrame(frame);
  }, [open]);

  useEffect(() => {
    const closeForRecording = () => setOpen(false);
    window.addEventListener("assistant:close-for-recording", closeForRecording);
    return () =>
      window.removeEventListener(
        "assistant:close-for-recording",
        closeForRecording
      );
  }, []);

  useEffect(() => {
    const viewport = window.visualViewport;
    const updateInset = () => {
      let inset = Math.max(
        0,
        window.innerHeight -
          (viewport?.height ?? window.innerHeight) -
          (viewport?.offsetTop ?? 0)
      );
      document
        .querySelectorAll<HTMLElement>('nav[aria-label="Mobil navigatsiya"]')
        .forEach((nav) => {
          const rect = nav.getBoundingClientRect();
          const styles = window.getComputedStyle(nav);
          if (
            rect.width > 0 &&
            rect.height > 0 &&
            styles.display !== "none" &&
            styles.visibility !== "hidden"
          ) {
            inset = Math.max(inset, window.innerHeight - rect.top);
          }
        });
      setBottomInset(inset);
    };
    const resizeObserver =
      typeof ResizeObserver === "undefined"
        ? null
        : new ResizeObserver(updateInset);
    const observeNavigation = () => {
      resizeObserver?.disconnect();
      document
        .querySelectorAll('nav[aria-label="Mobil navigatsiya"]')
        .forEach((nav) => resizeObserver?.observe(nav));
      updateInset();
    };
    const observer = new MutationObserver(observeNavigation);
    observer.observe(document.body, { childList: true });
    observeNavigation();
    window.addEventListener("resize", updateInset);
    viewport?.addEventListener("resize", updateInset);
    viewport?.addEventListener("scroll", updateInset);
    return () => {
      observer.disconnect();
      resizeObserver?.disconnect();
      window.removeEventListener("resize", updateInset);
      viewport?.removeEventListener("resize", updateInset);
      viewport?.removeEventListener("scroll", updateInset);
    };
  }, []);

  const assistant = (
    <>
      <AnimatePresence>
        {!open ? (
          <motion.button
            ref={triggerRef}
            type="button"
            aria-label={uz.assistant.open}
            title={uz.assistant.open}
            aria-haspopup="dialog"
            onClick={() => setOpen(true)}
            initial={{ opacity: 0, y: 16, scale: 0.94 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.96 }}
            whileTap={{ scale: 0.97 }}
            transition={{ type: "spring", stiffness: 360, damping: 22 }}
            className="ea-assistant-trigger"
            style={
              {
                "--ea-assistant-bottom-inset": `${bottomInset}px`,
              } as React.CSSProperties
            }
          >
            <Icon name="auto_awesome" className="ea-assistant-trigger__icon" />
          </motion.button>
        ) : null}
      </AnimatePresence>

      <AnimatePresence>
        {open ? <AssistantPanel onClose={() => setOpen(false)} /> : null}
      </AnimatePresence>
    </>
  );

  return typeof document === "undefined"
    ? assistant
    : createPortal(assistant, document.body);
}

function AssistantPanel({ onClose }: { onClose: () => void }) {
  const [pageContext, setPageContext] = useState<AssistantPageContext>(() =>
    readAssistantContext()
  );
  const sessionState = useAssistantSessions(pageContext);
  const messages: AssistantMessage[] = sessionState.messages.map((message) => ({
    role: message.role,
    text: message.text,
    failed: message.status === "failed",
    sources: message.sources,
  }));
  const [streamedReply, setStreamedReply] = useState("");
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);
  const [sendError, setSendError] = useState<string | null>(null);
  const [retryRequest, setRetryRequest] = useState<{
    question: string;
    clientRequestId: string;
  } | null>(null);
  const inFlightRef = useRef(false);
  const panelRef = useRef<HTMLElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const [viewportHeight, setViewportHeight] = useState<number | null>(null);
  const [viewportOffsetTop, setViewportOffsetTop] = useState(0);
  const [keyboardOpen, setKeyboardOpen] = useState(false);
  const [compactViewport, setCompactViewport] = useState(false);
  const [mobilePanelHeight, setMobilePanelHeight] = useState<number | null>(
    null
  );
  const panelHeightBeforeKeyboardRef = useRef<number | null>(null);

  useEffect(() => {
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    document.documentElement.dataset.assistantOpen = "true";
    const focusTimer = window.setTimeout(() => inputRef.current?.focus(), 100);
    const onEscape = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      const activeDialog = document.activeElement?.closest('[role="dialog"]');
      if (activeDialog && activeDialog !== panelRef.current) return;
      onClose();
    };
    window.addEventListener("keydown", onEscape);
    return () => {
      window.clearTimeout(focusTimer);
      document.body.style.overflow = previousOverflow;
      delete document.documentElement.dataset.assistantOpen;
      window.removeEventListener("keydown", onEscape);
    };
  }, [onClose]);

  useEffect(() => installAssistantContextListener(setPageContext), []);

  useEffect(() => {
    const viewport = window.visualViewport;
    const updateViewport = () => {
      const nextViewportHeight = viewport?.height ?? window.innerHeight;
      const keyboardInset = Math.max(
        0,
        window.innerHeight - nextViewportHeight - (viewport?.offsetTop ?? 0)
      );
      const nextKeyboardOpen = window.innerWidth <= 1180 && keyboardInset > 120;
      if (nextKeyboardOpen && panelHeightBeforeKeyboardRef.current === null) {
        panelHeightBeforeKeyboardRef.current =
          mobilePanelHeight ?? defaultPanelHeight(window.innerHeight);
      }
      setViewportHeight(nextViewportHeight);
      setViewportOffsetTop(viewport?.offsetTop ?? 0);
      setKeyboardOpen(nextKeyboardOpen);
      setCompactViewport(window.innerWidth <= 700 || nextViewportHeight <= 560);
    };
    updateViewport();
    viewport?.addEventListener("resize", updateViewport);
    viewport?.addEventListener("scroll", updateViewport);
    window.addEventListener("resize", updateViewport);
    return () => {
      viewport?.removeEventListener("resize", updateViewport);
      viewport?.removeEventListener("scroll", updateViewport);
      window.removeEventListener("resize", updateViewport);
    };
  }, [mobilePanelHeight]);

  useEffect(() => {
    if (!viewportHeight || window.innerWidth > 1180) return;
    const minimumHeight = Math.min(320, viewportHeight);
    const maximumHeight = Math.max(
      minimumHeight,
      viewportHeight - (window.innerWidth <= 700 ? 18 : 44)
    );
    if (keyboardOpen) {
      setMobilePanelHeight(viewportHeight);
      return;
    }
    const previousHeight = panelHeightBeforeKeyboardRef.current;
    panelHeightBeforeKeyboardRef.current = null;
    const defaultHeight = defaultPanelHeight(viewportHeight);
    setMobilePanelHeight((current) =>
      Math.min(
        maximumHeight,
        Math.max(minimumHeight, previousHeight ?? current ?? defaultHeight)
      )
    );
  }, [keyboardOpen, viewportHeight]);

  function defaultPanelHeight(availableHeight: number) {
    const minimumHeight = Math.min(320, availableHeight);
    const maximumHeight = Math.max(
      minimumHeight,
      availableHeight - (window.innerWidth <= 700 ? 18 : 44)
    );
    return Math.min(
      maximumHeight,
      Math.max(
        minimumHeight,
        Math.round(availableHeight * (window.innerWidth <= 700 ? 0.91 : 0.8))
      )
    );
  }

  useEffect(() => {
    if (listRef.current)
      listRef.current.scrollTop = listRef.current.scrollHeight;
  }, [messages, sending]);

  useEffect(() => {
    const textarea = inputRef.current;
    if (!textarea) return;
    const maximumTextareaHeight = keyboardOpen ? 96 : 168;
    textarea.style.height = "auto";
    textarea.style.height = `${Math.min(
      textarea.scrollHeight,
      maximumTextareaHeight
    )}px`;
    textarea.style.overflowY =
      textarea.scrollHeight > maximumTextareaHeight ? "auto" : "hidden";
  }, [input, keyboardOpen]);

  useEffect(() => {
    if (!keyboardOpen || !listRef.current) return;
    const frame = window.requestAnimationFrame(() => {
      if (listRef.current)
        listRef.current.scrollTop = listRef.current.scrollHeight;
    });
    return () => window.cancelAnimationFrame(frame);
  }, [keyboardOpen, input]);

  const starterTopic =
    pageContext.area === "general"
      ? "MY MOTHER · VOCABULARY"
      : `${pageContext.title.toUpperCase()} · ${pageContext.area.toUpperCase()}`;

  async function send(
    questionOverride?: string,
    retryClientRequestId?: string
  ) {
    const question = (questionOverride ?? input).trim();
    if (!question || inFlightRef.current || !sessionState.active) return;
    const clientRequestId = retryClientRequestId ?? crypto.randomUUID();
    const optimistic: AssistantMessageDto = {
      id: clientRequestId,
      role: "user",
      text: question,
      status: "completed",
      source: "local",
      clientRequestId,
      latencyMs: null,
      createdAt: new Date().toISOString(),
      sources: [],
    };
    inFlightRef.current = true;
    if (!retryClientRequestId) sessionState.append(optimistic);
    setInput("");
    setSendError(null);
    setSending(true);
    setStreamedReply("");
    try {
      const reply = await api.assistant.sessions.send(
        sessionState.active.id,
        question,
        pageContext.context,
        pageContext.focusText,
        clientRequestId,
        pageContext.route ?? window.location.pathname,
        pageContext.stage,
        (text) => {
          setStreamedReply((current) => current + text);
          requestAnimationFrame(() =>
            listRef.current?.scrollTo({
              top: listRef.current.scrollHeight,
              behavior: "smooth",
            })
          );
        }
      );
      if (reply.text.trim()) {
        sessionState.append(reply);
      } else {
        const restored = await waitForPersistedReply(
          sessionState.active.id,
          clientRequestId,
          false
        );
        if (!restored) throw new Error("Assistant returned an empty reply");
        sessionState.setActive(restored);
      }
      setRetryRequest(null);
      setStreamedReply("");
      void sessionState.refresh();
    } catch (error) {
      const restored = await waitForPersistedReply(
        sessionState.active.id,
        clientRequestId
      ).catch(() => null);
      if (restored) {
        sessionState.setActive(restored);
        void sessionState.refresh();
      } else {
        const details = rateLimitDetails(error);
        setSendError(
          details?.code === "rate_limited"
            ? `AI savol limiti tugadi. ${details.retryAfterSeconds} soniyadan keyin qayta urinib ko'ring.`
            : uz.assistant.unavailable
        );
        setRetryRequest({ question, clientRequestId });
      }
      setStreamedReply("");
    } finally {
      inFlightRef.current = false;
      setSending(false);
    }
  }

  return (
    <motion.div
      className="ea-assistant-overlay ea-overlay ea-overlay--modal"
      role="presentation"
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      style={
        viewportHeight
          ? ({
              top: viewportOffsetTop,
              height: viewportHeight,
              bottom: "auto",
              "--ea-assistant-viewport-height": `${viewportHeight}px`,
            } as React.CSSProperties)
          : undefined
      }
    >
      <button
        type="button"
        className="ea-overlay__backdrop"
        aria-label="Yordamchi fonini yopish"
        onClick={onClose}
      />
      <motion.section
        ref={panelRef}
        onKeyDown={(event) => {
          if (
            event.key !== "Tab" ||
            (event.target as HTMLElement).closest('[role="dialog"]') !==
              event.currentTarget
          )
            return;
          const controls = Array.from(
            event.currentTarget.querySelectorAll<HTMLElement>(
              'button:not(:disabled), a[href], textarea:not(:disabled), input:not(:disabled), [tabindex="0"]'
            )
          ).filter((element) => element.getClientRects().length > 0);
          const first = controls[0];
          const last = controls[controls.length - 1];
          if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last?.focus();
          } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first?.focus();
          }
        }}
        role="dialog"
        aria-modal="true"
        aria-labelledby="learning-assistant-title"
        initial={{ opacity: 0, y: 34, scale: 0.97 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 22, scale: 0.98 }}
        transition={{ type: "spring", stiffness: 310, damping: 28 }}
        className={cn(
          "ea-assistant-panel",
          keyboardOpen && "is-keyboard-open",
          compactViewport && "is-compact-viewport"
        )}
        style={
          {
            ...(viewportHeight
              ? { "--ea-assistant-viewport-height": `${viewportHeight}px` }
              : {}),
            ...(mobilePanelHeight
              ? { "--ea-assistant-mobile-height": `${mobilePanelHeight}px` }
              : {}),
          } as React.CSSProperties
        }
      >
        <header className="ea-assistant-header">
          <div className="ea-assistant-header__identity">
            <span className="ea-assistant-header__mark" aria-hidden="true">
              <Icon name="auto_awesome" className="text-[24px]" />
            </span>
            <div>
              <h2 id="learning-assistant-title">{uz.assistant.title}</h2>
              <span>Sizning shaxsiy o‘quv hamrohingiz</span>
            </div>
          </div>
          <button
            className="ea-assistant-close"
            type="button"
            aria-label={uz.assistant.close}
            onClick={onClose}
            data-native-back-close
          >
            <Icon name="close" className="text-[20px]" />
          </button>
        </header>

        <div ref={listRef} className="ea-assistant-messages">
          {messages.length === 0 && !sending && !streamedReply ? (
            <section
              className="ea-assistant-starter"
              aria-label="AI yordamchi namunasi"
            >
              <div className="ea-assistant-topic">{starterTopic}</div>
              <div className="ea-assistant-starter__intro">
                <h3>Salom, Javohir!</h3>
                <p>
                  So‘z, grammatika yoki bugungi dars haqida savol bering. Birga
                  tushunib olamiz.
                </p>
              </div>
              <div className="ea-assistant-starter__question">
                “Kind” so‘zini qanday ishlataman?
              </div>
              <article className="ea-assistant-starter__answer">
                <span>AI YORDAMCHI</span>
                <p>
                  “Kind” — mehribon degani. Odamning fe’l-atvorini tasvirlashda
                  ishlatiladi.
                </p>
                <div className="ea-assistant-starter__example">
                  <strong>My mother is very kind.</strong>
                  <small>Mening onam juda mehribon.</small>
                </div>
                <p>Endi siz ham “kind” bilan bitta gap tuzib ko‘ring!</p>
                <div className="ea-assistant-starter__actions">
                  <button
                    type="button"
                    onClick={() => setInput("My mother is very kind.")}
                  >
                    <Icon name="volume_up" className="text-[18px]" />
                    <span>Tinglash</span>
                  </button>
                  <button
                    type="button"
                    aria-label="Misolni nusxalash"
                    onClick={() => setInput("My mother is very kind.")}
                  >
                    <Icon name="content_copy" className="text-[17px]" />
                  </button>
                  <button
                    type="button"
                    aria-label="Javob foydali"
                    onClick={() => setInput("Misol foydali bo‘ldi.")}
                  >
                    <Icon name="favorite" className="text-[17px]" />
                  </button>
                </div>
              </article>
              <div
                className="ea-assistant-suggestions"
                aria-label="Tavsiya etilgan savollar"
              >
                <button
                  type="button"
                  onClick={() =>
                    void send("“Kind” so‘ziga yana misollar keltir.")
                  }
                >
                  Misollar keltir
                </button>
                <button
                  type="button"
                  onClick={() => void send("“Kind” so‘zi bilan mashq ber.")}
                >
                  Mashq ber
                </button>
              </div>
            </section>
          ) : null}

          {messages.map((message, index) => (
            <div
              key={`${message.role}-${index}`}
              className={cn(
                "ea-assistant-message",
                `ea-assistant-message--${message.role}`,
                message.failed && "is-failed",
                message.streaming && "is-streaming"
              )}
            >
              <motion.div
                className="ea-assistant-message__bubble"
                initial={{ opacity: 0, y: 7, scale: 0.985 }}
                animate={{ opacity: 1, y: 0, scale: 1 }}
                transition={{ duration: 0.2, ease: "easeOut" }}
              >
                {message.role === "assistant" ? (
                  <>
                    <span className="ea-assistant-message__eyebrow">
                      AI YORDAMCHI
                    </span>
                    <AssistantFormattedText
                      text={message.text}
                      streaming={message.streaming}
                      sources={message.sources}
                    />
                  </>
                ) : (
                  <p>{message.text}</p>
                )}
              </motion.div>
            </div>
          ))}

          {streamedReply ? (
            <div className="ea-assistant-message ea-assistant-message--assistant is-streaming">
              <div className="ea-assistant-message__bubble">
                <span className="ea-assistant-message__eyebrow">
                  AI YORDAMCHI
                </span>
                <AssistantFormattedText text={streamedReply} streaming />
              </div>
            </div>
          ) : sending && messages.at(-1)?.role !== "assistant" ? (
            <div className="ea-assistant-message ea-assistant-message--assistant">
              <p className="ea-assistant-thinking">
                <i />
                <i />
                <i />
                <span className="sr-only">{uz.assistant.thinking}</span>
              </p>
            </div>
          ) : null}
          {sendError ? (
            <div className="ea-assistant-send-error" role="alert">
              <span>{sendError}</span>
              {retryRequest ? (
                <button
                  type="button"
                  onClick={() =>
                    void send(
                      retryRequest.question,
                      retryRequest.clientRequestId
                    )
                  }
                  disabled={sending}
                >
                  {uz.assistant.retry}
                </button>
              ) : null}
            </div>
          ) : null}
        </div>

        <footer className="ea-assistant-composer">
          <div className="ea-assistant-composer__box">
            <div className="ea-assistant-composer__field">
              <textarea
                ref={inputRef}
                value={input}
                onChange={(event) => setInput(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter" && !event.shiftKey) {
                    event.preventDefault();
                    void send();
                  }
                }}
                placeholder={uz.assistant.placeholder}
                aria-label={uz.assistant.placeholder}
                rows={1}
                maxLength={maxChars}
              />
              <span>
                {input.length}/{maxChars}
              </span>
            </div>
            <button
              type="button"
              aria-label={uz.assistant.send}
              title={uz.assistant.send}
              onClick={() => void send()}
              disabled={sending || !input.trim()}
            >
              <Icon name="send" filled className="text-[20px]" />
            </button>
          </div>
          <p>{uz.assistant.keyboardHint}</p>
        </footer>
      </motion.section>
    </motion.div>
  );
}
