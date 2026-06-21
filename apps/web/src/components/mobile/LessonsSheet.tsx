import { useNavigate } from "react-router-dom";
import { BottomSheet } from "./BottomSheet";
import { Icon } from "@/components/ui/Icon";
import { uz } from "@/content/uz";
import { tapLight } from "@/lib/haptics";
import { getNativeCapabilities } from "@/api/nativeCapabilities";

interface LessonsSheetProps {
  open: boolean;
  onClose: () => void;
}

interface SkillEntry {
  to: string;
  icon: string;
  label: string;
  hint: string;
  /** Tailwind classes for the icon tile, varied per skill so the grid reads at a glance. */
  tone: string;
}

// The seven topic-centric skill modules, in the learning-path order (words → grammar → writing →
// speaking → listening → reading), with Video last. Labels stay in English per the design system
// (uz.nav.*); hints are vetted templates (uz.lessonsSheet.items.*).
const SKILLS: SkillEntry[] = [
  { to: "/app/vocabulary/topics", icon: "menu_book", label: uz.nav.vocabulary, hint: uz.lessonsSheet.items.vocabulary, tone: "bg-primary-container text-on-primary-container" },
  { to: "/app/grammar", icon: "rule", label: uz.nav.grammar, hint: uz.lessonsSheet.items.grammar, tone: "bg-secondary-container text-on-secondary-container" },
  { to: "/writing", icon: "edit_note", label: uz.nav.writing, hint: uz.lessonsSheet.items.writing, tone: "bg-tertiary-container text-white" },
  { to: "/app/speaking", icon: "record_voice_over", label: uz.nav.speaking, hint: uz.lessonsSheet.items.speaking, tone: "bg-accent text-white" },
  { to: "/listening", icon: "headphones", label: uz.nav.listening, hint: uz.lessonsSheet.items.listening, tone: "bg-primary-container text-on-primary-container" },
  { to: "/reading", icon: "auto_stories", label: uz.nav.reading, hint: uz.lessonsSheet.items.reading, tone: "bg-secondary-container text-on-secondary-container" },
  { to: "/video", icon: "smart_display", label: uz.nav.video, hint: uz.lessonsSheet.items.video, tone: "bg-tertiary-container text-white" },
];

/**
 * The mobile "Darslar" sheet - opened by the central tab in MobileTabBar. Lists every skill
 * module that doesn't get its own bottom tab, so the full learning path is one tap away. Each row
 * navigates and closes the sheet.
 */
export function LessonsSheet({ open, onClose }: LessonsSheetProps) {
  const navigate = useNavigate();
  const capabilities = getNativeCapabilities();
  const skills = capabilities.video ? SKILLS : SKILLS.filter((skill) => skill.to !== "/video");

  function go(to: string) {
    tapLight();
    onClose();
    navigate(to);
  }

  return (
    <BottomSheet open={open} onClose={onClose} title={uz.lessonsSheet.title}>
      <p className="text-caption font-caption text-text-secondary -mt-xs mb-md">
        {uz.lessonsSheet.subtitle}
      </p>
      <div className="grid grid-cols-1 gap-sm">
        {skills.map((s) => (
          <button
            key={s.to}
            type="button"
            onClick={() => go(s.to)}
            className="flex items-center gap-md rounded-xl border border-border bg-surface-container-lowest px-md py-sm text-left transition-transform"
          >
            <span className={`w-11 h-11 shrink-0 rounded-xl flex items-center justify-center ${s.tone}`}>
              <Icon name={s.icon} />
            </span>
            <span className="min-w-0">
              <span className="block font-label-md text-label-md text-text-primary">{s.label}</span>
              <span className="block text-caption font-caption text-text-secondary truncate">
                {s.hint}
              </span>
            </span>
            <Icon name="chevron_right" className="ml-auto text-text-secondary" />
          </button>
        ))}
      </div>
    </BottomSheet>
  );
}
