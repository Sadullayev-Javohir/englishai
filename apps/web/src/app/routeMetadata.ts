import { uz } from "@/content/uz";

export type ShellMode = "standard" | "lessonFrame";
export type NavSection =
  | "home"
  | "levels"
  | "speaking"
  | "progress"
  | "leaderboard"
  | "profile"
  | "admin"
  | "lessons";

interface RouteMetadataDefinition {
  pattern: RegExp;
  title: string;
  documentTitle?: string;
  section: NavSection;
  shellMode?: ShellMode;
  homeBack?: boolean;
  mobileBack?: boolean;
}

export interface RouteMetadata {
  title: string;
  documentTitle: string;
  section: NavSection;
  shellMode: ShellMode;
  homeBack: boolean;
  mobileBack: boolean;
}

const LESSON_ROUTES: RouteMetadataDefinition[] = [
  { pattern: /^\/app\/speaking\/free$/, title: uz.nav.speaking, documentTitle: "Erkin suhbat", section: "speaking", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/speaking\/topic\/[^/]+$/, title: uz.nav.speaking, documentTitle: "Speaking darsi", section: "speaking", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/speaking\/free-talk\/[^/]+$/, title: uz.nav.speaking, documentTitle: "Erkin suhbat", section: "speaking", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/speaking\/role-talk\/[^/]+$/, title: uz.nav.speaking, documentTitle: "Rolli suhbat", section: "speaking", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/speaking\/pronunciation\/[^/]+$/, title: uz.nav.speaking, documentTitle: "Talaffuz", section: "speaking", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/video\/playlists\/[^/]+\/\d+$/, title: uz.nav.video, documentTitle: "Playlist qismi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/video\/[^/]+\/play$/, title: uz.nav.video, documentTitle: "Video darsi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/video\/[^/]+\/quiz$/, title: uz.nav.video, documentTitle: "Video testi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/books\/[^/]+\/sections\/[^/]+$/, title: uz.nav.reading, documentTitle: "Kitob bo‘limi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/books\/[^/]+$/, title: uz.nav.reading, documentTitle: "Kitob", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/vocabulary\/topic\/[^/]+\/pronunciation\/[^/]+$/, title: uz.nav.vocabulary, documentTitle: "Talaffuz", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/vocabulary\/topic\/[^/]+$/, title: uz.nav.vocabulary, documentTitle: "Vocabulary darsi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/vocabulary\/review$/, title: uz.nav.vocabulary, documentTitle: "Vocabulary takrorlash", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/vocabulary\/saved\/[^/]+\/practice$/, title: uz.nav.vocabulary, documentTitle: "Saqlangan so‘zlar mashqi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/app\/grammar\/topic\/[^/]+$/, title: uz.nav.grammar, documentTitle: "Grammar darsi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/reading\/topic\/[^/]+$/, title: uz.nav.reading, documentTitle: "Reading darsi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/listening\/topic\/[^/]+$/, title: uz.nav.listening, documentTitle: "Listening darsi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/writing\/topic\/[^/]+$/, title: uz.nav.writing, documentTitle: "Writing darsi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
  { pattern: /^\/writing\/task\/[^/]+$/, title: uz.nav.writing, documentTitle: "Writing vazifasi", section: "lessons", shellMode: "lessonFrame", mobileBack: true },
];

const ROUTES: RouteMetadataDefinition[] = [
  ...LESSON_ROUTES,
  { pattern: /^\/$/, title: uz.brand, documentTitle: "AI bilan ingliz tilini o‘rganing", section: "home" },
  { pattern: /^\/landing$/, title: uz.brand, documentTitle: "AI bilan ingliz tilini o‘rganing", section: "home" },
  { pattern: /^\/learn$/, title: "Ingliz tili qo‘llanmalari", section: "home" },
  { pattern: /^\/learn\/[^/]+$/, title: "Ingliz tili qo‘llanmasi", section: "home" },
  { pattern: /^\/methodology$/, title: "O‘qitish metodikasi", section: "home" },
  { pattern: /^\/about$/, title: "EnglishAI haqida", section: "home" },
  { pattern: /^\/pricing$/, title: "Narxlar", section: "home" },
  { pattern: /^\/product$/, title: "EnglishAI imkoniyatlari", section: "home" },
  { pattern: /^\/contact$/, title: "Aloqa", section: "home" },
  { pattern: /^\/editorial-policy$/, title: "Tahrir siyosati", section: "home" },
  { pattern: /^\/login$/, title: "Kirish", section: "home" },
  { pattern: /^\/hero$/, title: "EnglishAI haqida", section: "home" },
  { pattern: /^\/dev\/lesson-foundation$/, title: "Lesson foundation", section: "lessons" },
  { pattern: /^\/username$/, title: "Foydalanuvchi nomi", section: "profile" },
  { pattern: /^\/onboarding\/goal$/, title: "O‘quv maqsadi", section: "levels" },
  { pattern: /^\/welcome$/, title: "Xush kelibsiz", section: "levels" },
  { pattern: /^\/assessment$/, title: "Darajani aniqlash", section: "levels" },
  { pattern: /^\/placement$/, title: "Daraja testi", section: "levels" },
  { pattern: /^\/placement\/result$/, title: "Daraja natijasi", section: "levels" },
  { pattern: /^\/levels\/exit-test$/, title: "Chiqish testi", section: "levels", mobileBack: true },
  { pattern: /^\/levels\/exit-test\/result$/, title: "Chiqish testi natijasi", section: "levels", mobileBack: true },
  { pattern: /^\/home$/, title: uz.nav.home, documentTitle: "Asosiy sahifa", section: "home" },
  { pattern: /^\/levels(?:\/.*)?$/, title: uz.nav.levels, section: "levels", mobileBack: true },
  { pattern: /^\/progress(?:\/.*)?$/, title: uz.nav.progress, section: "progress", homeBack: true, mobileBack: true },
  { pattern: /^\/leaderboard(?:\/.*)?$/, title: uz.nav.leaderboard, section: "leaderboard", mobileBack: true },
  { pattern: /^\/profile(?:\/.*)?$/, title: uz.nav.profile, section: "profile", mobileBack: true },
  { pattern: /^\/admin\/curriculum$/, title: uz.nav.admin, documentTitle: "Curriculum boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/users\/[^/]+$/, title: uz.nav.admin, documentTitle: "Foydalanuvchi tafsilotlari", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/users$/, title: uz.nav.admin, documentTitle: "Foydalanuvchilar", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/support$/, title: uz.nav.admin, documentTitle: "Support", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/vocabulary\/[^/]+\/edit$/, title: uz.nav.admin, documentTitle: "Vocabulary mavzusini tahrirlash", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/vocabulary-images$/, title: uz.nav.admin, documentTitle: "So‘z rasmlarini boshqarish", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/vocabulary$/, title: uz.nav.admin, documentTitle: "Vocabulary boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/grammar\/[^/]+\/edit$/, title: uz.nav.admin, documentTitle: "Grammar darsini tahrirlash", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/grammar$/, title: uz.nav.admin, documentTitle: "Grammar boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/listening\/[^/]+\/edit$/, title: uz.nav.admin, documentTitle: "Listening mashqini tahrirlash", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/listening$/, title: uz.nav.admin, documentTitle: "Listening boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/reading\/[^/]+\/edit$/, title: uz.nav.admin, documentTitle: "Reading matnini tahrirlash", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/reading$/, title: uz.nav.admin, documentTitle: "Reading boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/metrics$/, title: uz.nav.admin, documentTitle: "Mahsulot ko‘rsatkichlari", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/notifications$/, title: uz.nav.admin, documentTitle: "Bildirishnomalar boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/sections$/, title: uz.nav.admin, documentTitle: "Bo‘limlar boshqaruvi", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin\/server$/, title: uz.nav.admin, documentTitle: "Server holati", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/admin$/, title: uz.nav.admin, documentTitle: "Admin panel", section: "admin", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/vocabulary\/topics$/, title: uz.nav.vocabulary, documentTitle: "Vocabulary mavzulari", section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/vocabulary\/saved$/, title: uz.nav.vocabulary, documentTitle: "Mening so‘zlarim", section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/vocabulary(?:\/.*)?$/, title: uz.nav.vocabulary, section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/grammar(?:\/.*)?$/, title: uz.nav.grammar, section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/writing(?:\/.*)?$/, title: uz.nav.writing, section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/speaking\/free-talk$/, title: uz.nav.speaking, documentTitle: "Erkin suhbat mavzulari", section: "speaking", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/speaking\/topics$/, title: uz.nav.speaking, documentTitle: "Mavzuli suhbatlar", section: "speaking", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/speaking\/role-talk$/, title: uz.nav.speaking, documentTitle: "Rolli suhbatlar", section: "speaking", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/speaking\/roleplay$/, title: uz.nav.speaking, documentTitle: "Rolli suhbatlar", section: "speaking", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/speaking\/practice-words$/, title: uz.nav.speaking, documentTitle: "Talaffuz mashqi", section: "speaking", homeBack: true, mobileBack: true },
  { pattern: /^\/app\/speaking(?:\/.*)?$/, title: uz.nav.speaking, section: "speaking", homeBack: true, mobileBack: true },
  { pattern: /^\/listening(?:\/.*)?$/, title: uz.nav.listening, section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/reading(?:\/.*)?$/, title: uz.nav.reading, section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/books(?:\/.*)?$/, title: uz.nav.reading, documentTitle: "Kitoblar", section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/video(?:\/.*)?$/, title: uz.nav.video, section: "lessons", homeBack: true, mobileBack: true },
  { pattern: /^\/notifications(?:\/.*)?$/, title: uz.nav.notifications, section: "profile", mobileBack: true },
  { pattern: /^\/support$/, title: "Support", documentTitle: "Yordam va support", section: "profile", mobileBack: true },
];

const DEFAULT_METADATA: RouteMetadata = {
  title: uz.notFound.title,
  documentTitle: uz.notFound.title,
  section: "home",
  shellMode: "standard",
  homeBack: false,
  mobileBack: false,
};

export function getRouteMetadata(pathname: string): RouteMetadata {
  const match = ROUTES.find((route) => route.pattern.test(pathname));
  return match
    ? {
        title: match.title,
        documentTitle: match.documentTitle ?? match.title,
        section: match.section,
        shellMode: match.shellMode ?? "standard",
        homeBack: match.homeBack ?? false,
        mobileBack: match.mobileBack ?? pathname.split("/").filter(Boolean).length > 1,
      }
    : DEFAULT_METADATA;
}

export function isRouteSection(pathname: string, section: NavSection): boolean {
  return getRouteMetadata(pathname).section === section;
}
