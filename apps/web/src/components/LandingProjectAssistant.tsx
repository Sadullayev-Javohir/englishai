import { useEffect, useRef, useState } from "react";
import { api } from "@/api/client";
import type { AssistantTurnDto } from "@/api/types";
import { AssistantFormattedText } from "@/components/LearningAssistant";
import { ParrotLogo } from "@/components/ParrotLogo";
import { AppButton, AppIconButton, DesignSheet, DesignTextarea } from "@/components/design";
import type { PublicLocale } from "@/components/public/PublicSiteChrome";
import "./LandingProjectAssistant.css";

interface Message extends AssistantTurnDto {
  failed?: boolean;
  retryQuestion?: string;
}

const suggestions = [
  "3 yil o'rgandim, nega gapira olmayapman?",
  "24/7 AI tutor qanday ishlaydi?",
  "6 skill bir mavzuga qanday bog'lanadi?",
  "Har darajadagi 50 dars qanday tuzilgan?",
  "Books va Video bo'limlarida nima bor?",
  "Support uchun qanday bog'lanaman?",
];

const welcomeMessage: Message = {
  role: "assistant",
  text: "Assalomu alaykum! Agar ingliz tilini yillardan beri o'rganib, hali ham gapirishga qiynalayotgan bo'lsangiz, aynan siz uchun shu yerdaman. 24/7 AI tutor, bitta mavzuga bog'langan 6 skill, har darajadagi 50 dars, Books, Video yoki qanday boshlash haqida so'rang.",
};

const englishSuggestions = [
  "Why is speaking harder than studying English?",
  "How does the AI tutor work?",
  "How are the six skills connected?",
  "How is the learning path organised?",
  "What can I practise with Books and Video?",
  "How do I contact support?",
];

export function LandingProjectAssistant({ locale = "uz" }: { locale?: PublicLocale } = {}) {
  const english = locale === "en";
  const copy = english ? {
    title: "AI assistant",
    description: "Your EnglishAI guide",
    open: "Open the EnglishAI assistant",
    close: "Close",
    retry: "Try again",
    suggestions: "Suggested questions",
    placeholder: "Ask a question about EnglishAI…",
    input: "Your question about EnglishAI",
    send: "Send question",
    error: "We couldn’t connect to the server. Your question is saved — please try again.",
  } : {
    title: "AI yordamchi",
    description: "EnglishAI konsultanti",
    open: "EnglishAI loyiha yordamchisini ochish",
    close: "Yopish",
    retry: "Qayta urinish",
    suggestions: "Tavsiya savollar",
    placeholder: "EnglishAI haqida savol yozing...",
    input: "EnglishAI haqida savol",
    send: "Savolni yuborish",
    error: "Hozir server bilan bog'lanib bo'lmadi. Savolingiz saqlandi — qayta urinishingiz mumkin.",
  };
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState("");
  const [sending, setSending] = useState(false);
  const [messages, setMessages] = useState<Message[]>([english ? {
    role: "assistant",
    text: "Hello! I’m here to help you get to know EnglishAI. Ask about the AI tutor, six connected skills, your learning level, Books, Video or how to get started.",
  } : welcomeMessage]);
  const endRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    if (!open) return;
    const container = endRef.current?.closest(".ea-overlay__body");
    if (container) container.scrollTop = messages.length === 1 ? 0 : container.scrollHeight;
  }, [messages, open, sending]);

  async function send(questionOverride?: string) {
    const question = (questionOverride ?? input).trim();
    if (!question || sending) return;

    const history = messages
      .filter((message) => !message.failed)
      .slice(-6)
      .map(({ role, text }) => ({ role, text }));
    setMessages((current) => [...current, { role: "user", text: question }]);
    setInput("");
    setSending(true);

    try {
      const response = await api.assistant.askProject(question, history, locale);
      if (!response.reply?.trim()) throw new Error("Empty project assistant reply");
      setMessages((current) => [...current, { role: "assistant", text: response.reply!.trim() }]);
    } catch {
      setMessages((current) => [...current, {
        role: "assistant",
        text: copy.error,
        failed: true,
        retryQuestion: question,
      }]);
    } finally {
      setSending(false);
      window.setTimeout(() => inputRef.current?.focus({ preventScroll: true }), 50);
    }
  }

  return (
    <>
      <AppButton
        className="pl-assistant-trigger"
        aria-label={copy.open}
        aria-expanded={open}
        leadingIcon="smart_toy"
        onClick={() => setOpen(true)}
      >
        {copy.title}
      </AppButton>

      <DesignSheet
        open={open}
        onClose={() => setOpen(false)}
        title={copy.title}
        description={copy.description}
        closeLabel={copy.close}
        className="pl-assistant-sheet"
        initialFocusRef={inputRef}
        footer={
          <form className="pl-assistant-composer" onSubmit={(event) => { event.preventDefault(); void send(); }}>
            <DesignTextarea ref={inputRef} value={input} maxLength={500} rows={1} placeholder={copy.placeholder} aria-label={copy.input} onChange={(event) => setInput(event.target.value)} onKeyDown={(event) => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); void send(); } }} />
            <AppIconButton type="submit" icon="send" label={copy.send} loading={sending} disabled={!input.trim()} />
          </form>
        }
      >
        <div className="pl-assistant-messages" aria-live="polite">
          {messages.map((message, index) => (
            <div key={`${message.role}-${index}`} className={`pl-assistant-message pl-assistant-message--${message.role}${message.failed ? " is-failed" : ""}`}>
              {message.role === "assistant" && <ParrotLogo size={30} imgSize={24} rounded="10px" />}
              <div>
                <AssistantFormattedText text={message.text} />
                {message.retryQuestion && (
                  <AppButton size="sm" tone="standard" disabled={sending} onClick={() => void send(message.retryQuestion)}>{copy.retry}</AppButton>
                )}
              </div>
            </div>
          ))}
          {messages.length === 1 && (
            <div className="pl-assistant-suggestions" aria-label={copy.suggestions}>
              {(english ? englishSuggestions : suggestions).map((suggestion) => <AppButton fullWidth size="sm" tone="standard" type="button" key={suggestion} onClick={() => void send(suggestion)}>{suggestion}</AppButton>)}
            </div>
          )}
          {sending && <div className="pl-assistant-message pl-assistant-message--assistant"><ParrotLogo size={30} imgSize={24} rounded="10px" /><div className="pl-assistant-typing"><span /><span /><span /></div></div>}
          <div ref={endRef} />
        </div>
      </DesignSheet>
    </>
  );
}
