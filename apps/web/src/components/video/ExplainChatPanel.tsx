import { useEffect, useRef } from "react";
import { ArrowUp, Sparkles, X } from "lucide-react";
import { uz } from "@/content/uz";
import { Icon } from "@/components/ui/Icon";
import { ParrotLogo } from "@/components/ParrotLogo";
import type { VideoExplainConversation } from "@/components/video/useVideoExplainConversation";
import { cn } from "@/lib/cn";
import { AssistantFormattedText } from "@/components/LearningAssistant";
import { AppIconButton, DesignSheet, DesignTextarea } from "@/components/design";
import "./ExplainChatPanel.css";

const noop = () => undefined;

interface ExplainChatPanelProps {
  /** Both visible chat surfaces receive the same controller and therefore mirror one request. */
  conversation: VideoExplainConversation;
  /** Embedded desktop panel, mobile sheet, or panel inside the fullscreen video subtree. */
  variant: "panel" | "sheet" | "fullscreen" | "contextual";
  selectedSentence?: string;
  timestamp?: string;
  onInteract?: () => void;
  onClose?: () => void;
  /** Prevents an off-screen mirrored view from stealing focus from the visible fullscreen panel. */
  focusEnabled?: boolean;
  /** Opens at the latest message without moving keyboard focus into the composer. */
  focusInputOnOpen?: boolean;
}

export function ExplainChatPanel({ conversation, variant, onClose, focusEnabled = true, focusInputOnOpen = true, selectedSentence, timestamp, onInteract }: ExplainChatPanelProps) {
  const { messages, input, setInput, sending, focusToken, send } = conversation;
  const listRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLTextAreaElement | null>(null);

  useEffect(() => {
    if (focusToken === 0 || !focusEnabled || !focusInputOnOpen) return;
    window.setTimeout(() => inputRef.current?.focus(), 0);
  }, [focusEnabled, focusInputOnOpen, focusToken]);

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, sending]);

  useEffect(() => {
    const textarea = inputRef.current;
    if (!textarea) return;
    textarea.style.height = "auto";
    const maximumHeight = Number.parseFloat(window.getComputedStyle(textarea).maxHeight);
    const nextHeight = Number.isFinite(maximumHeight)
      ? Math.min(textarea.scrollHeight, maximumHeight)
      : textarea.scrollHeight;
    textarea.style.height = `${nextHeight}px`;
    textarea.style.overflowY = Number.isFinite(maximumHeight) && textarea.scrollHeight > maximumHeight
      ? "auto"
      : "hidden";
  }, [input]);

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      void send();
    }
  }

  if (variant === "contextual") {
    return (
      <section className="video-ai-contextual" aria-label="AI’dan so‘rash">
        <header className="video-ai-contextual__heading">
          <Sparkles size={20} aria-hidden />
          <h2>AI’dan so‘rash</h2>
          <span>{timestamp}<span className="video-ai-contextual__desktop"> · Tanlangan jumla</span></span>
          {onClose && <button type="button" onClick={onClose} aria-label="Subtitrlarga qaytish" className="video-ai-contextual__close"><X size={18} /></button>}
        </header>
        {selectedSentence && <p className="video-ai-contextual__quote">“{selectedSentence}”</p>}
        <div ref={listRef} className="video-ai-contextual__messages" role="log" aria-live="polite" aria-relevant="additions text" aria-busy={sending}>
          {messages.length === 0 && <p className="video-ai-contextual__empty">{selectedSentence ? "Shu jumlaning ma’nosi, ishlatilishi yoki grammatikasi haqida so‘rang." : "Izoh olish uchun transkriptdan jumlani tanlang."}</p>}
          {messages.map((message, index) => (
            <div key={index} className={cn("video-ai-contextual__message", message.role === "user" && "is-user", message.failed && "is-failed")}>
              {message.role === "assistant" && !message.failed
                ? <AssistantFormattedText text={message.text} streaming={message.streaming} />
                : <p>{message.text}</p>}
            </div>
          ))}
          {sending && messages.at(-1)?.role !== "assistant" && <p role="status">{uz.videoChat.thinking}</p>}
        </div>
        <form className="video-ai-contextual__composer" onFocusCapture={onInteract} onSubmit={(event) => { event.preventDefault(); void send(); }}>
          <textarea ref={inputRef} aria-label="Shu jumla haqida savol yozing" rows={1} value={input} onChange={event => setInput(event.target.value)}
            onKeyDown={handleKeyDown} placeholder="Shu jumla haqida savol yozing…" disabled={!selectedSentence} />
          <button type="submit" aria-label={uz.videoChat.send} disabled={sending || !input.trim() || !selectedSentence}><ArrowUp size={16} aria-hidden /></button>
        </form>
      </section>
    );
  }

  const body = (
    <div className="video-ai-chat">
      {variant !== "sheet" && <div className="video-ai-chat__header">
        <h3>
          <span className="video-ai-chat__logo">
            <Icon name="smart_toy" filled />
          </span>
          <span><small>Video yordamchi</small>{uz.videoChat.title}</span>
        </h3>
        {onClose && (
          <AppIconButton icon="close" label={uz.videoChat.close} onClick={onClose} />
        )}
      </div>}

      <div
        ref={listRef}
        className="video-ai-chat__messages"
        role="log"
        aria-live="polite"
        aria-relevant="additions text"
        aria-busy={sending}
      >
        {messages.length === 0 && (
          <div className="video-ai-chat__empty">
            <Icon name="auto_awesome" filled />
            <p>
              {uz.videoChat.empty}
            </p>
          </div>
        )}
        {messages.map((m, i) => (
          <div key={i} className={cn("video-ai-chat__row", m.role === "user" ? "is-user" : "is-assistant")}>
            {m.role === "assistant" ? (
              <span className="video-ai-chat__avatar" aria-hidden="true">
                <ParrotLogo size={30} rounded="10px" />
                <i />
              </span>
            ) : null}
            <div
              className={cn(
                "video-ai-chat__bubble",
                m.role === "user"
                  ? "video-ai-chat__bubble--user"
                  : m.failed
                    ? "video-ai-chat__bubble--failed"
                    : "video-ai-chat__bubble--assistant",
              )}
            >
              {m.role === "assistant" && !m.failed
                ? <AssistantFormattedText text={m.text} streaming={m.streaming} />
                : <p>{m.text}</p>}
            </div>
          </div>
        ))}
        {sending && messages.at(-1)?.role !== "assistant" && (
          <div className="video-ai-chat__row is-assistant">
            <span className="video-ai-chat__avatar" aria-hidden="true">
              <ParrotLogo size={30} rounded="10px" />
              <i />
            </span>
            <p className="ea-assistant-thinking"><i /><i /><i /><span className="sr-only">{uz.videoChat.thinking}</span></p>
          </div>
        )}
      </div>

      <div className="video-ai-chat__composer">
        <div className="video-ai-chat__input-wrap">
          <DesignTextarea
            ref={inputRef}
            aria-label={uz.videoChat.placeholder}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={uz.videoChat.placeholder}
            rows={1}
            className="video-ai-chat__input"
          />
        </div>
        <button
          type="button"
          aria-label={uz.videoChat.send}
          onClick={() => void send()}
          disabled={sending || !input.trim()}
          className="video-ai-chat__send"
        >
          <Icon name="send" filled />
        </button>
      </div>
    </div>
  );

  if (variant === "sheet") {
    return (
      <DesignSheet
        open
        onClose={onClose ?? noop}
        title={uz.videoChat.title}
        description="Video yordamchi"
        className="video-ai-sheet-panel"
        initialFocusRef={focusInputOnOpen ? inputRef : undefined}
        portalTarget={typeof document === "undefined" ? null : document.fullscreenElement}
      >
        {body}
      </DesignSheet>
    );
  }

  return (
    <div
      className={cn(
        "video-ai-panel",
        variant === "fullscreen" ? "video-ai-panel--fullscreen" : "video-ai-panel--desktop",
      )}
    >
      {body}
    </div>
  );
}
