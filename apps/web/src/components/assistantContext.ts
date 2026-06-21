export interface AssistantPageContext {
  area: "general" | "video" | "vocabulary" | "grammar" | "reading" | "writing" | "speaking" | "listening" | "books" | "progress";
  resourceId?: string;
  title: string;
  context: string;
  focusText: string;
  route?: string;
  stage?: string;
}

const EMPTY_CONTEXT: AssistantPageContext = { area: "general", title: "EnglishAI", context: "", focusText: "" };

export function publishAssistantContext(context: AssistantPageContext): () => void {
  (window as typeof window & { __englishAiAssistantContext?: AssistantPageContext }).__englishAiAssistantContext = context;
  window.dispatchEvent(new CustomEvent("assistant:context", { detail: context }));
  return () => {
    const current = readAssistantContext();
    if (current !== context) return;
    (window as typeof window & { __englishAiAssistantContext?: AssistantPageContext }).__englishAiAssistantContext = EMPTY_CONTEXT;
    window.dispatchEvent(new CustomEvent("assistant:context", { detail: EMPTY_CONTEXT }));
  };
}

export function readAssistantContext(): AssistantPageContext {
  return (window as typeof window & { __englishAiAssistantContext?: AssistantPageContext }).__englishAiAssistantContext ?? EMPTY_CONTEXT;
}

export function installAssistantContextListener(onChange: (context: AssistantPageContext) => void): () => void {
  const handle = (event: Event) => {
    const context = (event as CustomEvent<AssistantPageContext>).detail;
    (window as typeof window & { __englishAiAssistantContext?: AssistantPageContext }).__englishAiAssistantContext = context;
    onChange(context);
  };
  window.addEventListener("assistant:context", handle);
  onChange(readAssistantContext());
  return () => window.removeEventListener("assistant:context", handle);
}
