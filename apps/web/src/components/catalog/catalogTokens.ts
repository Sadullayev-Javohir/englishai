/**
 * Catalog compatibility tokens. Every module uses the same canonical
 * Professional Indigo accent so catalog hierarchy stays consistent with /home.
 */
export type CatalogModule = "video" | "reading" | "books" | "grammar" | "writing" | "listening";

interface AccentToken {
  /** Tailwind text color for the accent (icons, level badge text). */
  text: string;
  /** Tailwind bg color for the accent chip / active filter. */
  bg: string;
  /** Tailwind bg color for the soft tinted module band (header flourish). */
  bgSoft: string;
  /** Border color used for the module accent. */
  border: string;
  /** Compatibility color consumed by the flat DuoButton adapter. */
  duoButtonColor: "blue" | "green" | "amber" | "purple" | "orange" | "teal";
}

export const CATALOG_ACCENT: Record<CatalogModule, AccentToken> = {
  video: {
    text: "text-ea-primary",
    bg: "bg-ea-primary",
    bgSoft: "bg-ea-primary/10",
    border: "border-ea-primary/30",
    duoButtonColor: "blue",
  },
  reading: {
    text: "text-ea-primary",
    bg: "bg-ea-primary",
    bgSoft: "bg-ea-primary/10",
    border: "border-ea-primary/30",
    duoButtonColor: "blue",
  },
  books: {
    text: "text-ea-primary",
    bg: "bg-ea-primary",
    bgSoft: "bg-ea-primary/10",
    border: "border-ea-primary/30",
    duoButtonColor: "blue",
  },
  grammar: {
    text: "text-ea-primary",
    bg: "bg-ea-primary",
    bgSoft: "bg-ea-primary/10",
    border: "border-ea-primary/30",
    duoButtonColor: "blue",
  },
  writing: {
    text: "text-ea-primary",
    bg: "bg-ea-primary",
    bgSoft: "bg-ea-primary/10",
    border: "border-ea-primary/30",
    duoButtonColor: "blue",
  },
  listening: {
    text: "text-ea-primary",
    bg: "bg-ea-primary",
    bgSoft: "bg-ea-primary/10",
    border: "border-ea-primary/30",
    duoButtonColor: "blue",
  },
};

/**
 * Vetted parrot greeter line per module (rule 11: Uzbek copy lives in code, not free-form).
 * Kept here (a stable module file) rather than content/uz.ts to avoid touching the large
 * generated locale file.
 */
export const CATALOG_GREETING: Record<CatalogModule, string> = {
  video: "Bugun qaysi videoni ko'ramiz?",
  reading: "Yangi matnlarni o'qib ko'ramizmi?",
  books: "Qaysi kitobni ochamiz?",
  grammar: "Yangi qoidani o'rganamizmi?",
  writing: "Yozishni mashq qilamizmi?",
  listening: "Audioni tinglab ko'ramizmi?",
};
