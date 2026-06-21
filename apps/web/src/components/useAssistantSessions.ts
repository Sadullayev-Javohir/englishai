import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { api } from "@/api/client";
import type { AssistantMessageDto, AssistantSessionDto, AssistantSessionSummaryDto, AssistantSkill } from "@/api/types";
import type { AssistantPageContext } from "./assistantContext";

const resourceTypeFor = (context: AssistantPageContext) => context.area === "video" ? "video" : context.resourceId ? "topic" : "page";
const keyFor = (context: AssistantPageContext) => `${context.area}:${context.resourceId ?? "page"}`;

export function useAssistantSessions(context: AssistantPageContext) {
  const [sessions, setSessions] = useState<AssistantSessionSummaryDto[]>([]);
  const [active, setActive] = useState<AssistantSessionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const restoreKey = keyFor(context);
  const requestRef = useRef(0);

  const refresh = useCallback(async () => {
    const page = await api.assistant.sessions.list();
    setSessions(page.items);
    return page.items;
  }, []);

  const create = useCallback(async () => {
    const created = await api.assistant.sessions.create({
      skill: context.area as AssistantSkill,
      resourceType: resourceTypeFor(context),
      resourceId: context.resourceId ?? null,
      title: context.title || "AI Yordamchi",
    });
    localStorage.setItem(`assistant-session:${restoreKey}`, created.id);
    setActive(created);
    await refresh();
    return created;
  }, [context, refresh, restoreKey]);

  useEffect(() => {
    const request = ++requestRef.current;
    setLoading(true);
    void (async () => {
      const list = await refresh();
      if (request !== requestRef.current) return;
      const savedId = localStorage.getItem(`assistant-session:${restoreKey}`);
      const candidate = list.find((item) => item.id === savedId)
        ?? list.find((item) => item.skill === context.area && item.resourceId === (context.resourceId ?? null));
      const restored = candidate ? await api.assistant.sessions.get(candidate.id) : await create();
      if (request !== requestRef.current) return;
      setActive(restored);
      localStorage.setItem(`assistant-session:${restoreKey}`, restored.id);
    })().finally(() => request === requestRef.current && setLoading(false));
  }, [context.area, context.resourceId, create, refresh, restoreKey]);

  const open = useCallback(async (sessionId: string) => {
    const session = await api.assistant.sessions.get(sessionId);
    localStorage.setItem(`assistant-session:${restoreKey}`, session.id);
    setActive(session);
  }, [restoreKey]);

  const remove = useCallback(async (sessionId: string) => {
    await api.assistant.sessions.delete(sessionId);
    const list = await refresh();
    if (active?.id !== sessionId) return;
    const next = list.find((item) => item.skill === context.area && item.resourceId === (context.resourceId ?? null));
    if (next) await open(next.id);
    else await create();
  }, [active?.id, context.area, context.resourceId, create, open, refresh]);

  const append = useCallback((message: AssistantMessageDto) => {
    setActive((current) => {
      if (!current) return current;
      const existingIndex = current.messages.findIndex((item) =>
        item.id === message.id
        || Boolean(message.clientRequestId && item.clientRequestId === message.clientRequestId && item.role === message.role));
      const messages = existingIndex >= 0
        ? current.messages.map((item, index) => index === existingIndex ? message : item)
        : [...current.messages, message];
      return { ...current, messages };
    });
  }, []);

  const messages = useMemo(() => active?.messages ?? [], [active]);
  return { sessions, active, messages, loading, create, open, remove, append, setActive, refresh };
}
