/** @type {import('tailwindcss').Config} */
// Design tokens transcribed from
// design-export/style-guide/EnglishAI-design-system.md (the canonical source per
// docs/development-guide.md 17.4). The literal light-mode hex values live in src/index.css as
// CSS variables (`:root`); the `.dark` block there supplies the night-mode
// counterparts. Every color below resolves through its variable so a single
// `dark` class on <html> re-themes the whole app without touching components.
// The `rgb(var(--c-x) / <alpha-value>)` form preserves Tailwind opacity
// modifiers (e.g. `bg-primary-container/10`).
const token = (name) => `rgb(var(--c-${name}) / <alpha-value>)`;

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  darkMode: "class",
  theme: {
    // =========================================================================
    // RESPONSIVE BANDS - the same canonical scale the CSS files use (see the
    // "RESPONSIVE BANDS" block in src/index.css). Tailwind's defaults
    // (640/768/1024/1280) were a second, competing scale: `md:` fired at 768
    // while the neighbouring stylesheet switched at 700, so a tablet could get
    // the phone grid and the tablet type size at once. These floors are the
    // band boundaries, so a prefix and a media query always agree.
    //
    //   sm: >= 421px   xs dan yuqori
    //   md: >= 701px   tablet va yuqorida
    //   lg: >= 901px   laptop va yuqorida  (sidebar shu yerda paydo bo'ladi)
    //   xl: >= 1181px  desktop             (sidebar 214 -> 248px)
    // =========================================================================
    screens: {
      sm: "421px",
      md: "701px",
      lg: "901px",
      xl: "1181px",
    },
    extend: {
      colors: {
        // =====================================================================
        // CANONICAL PALETTE - "Professional Indigo". Use these on every screen.
        //   bg-ea-bg  text-ea-ink  border-ea-border  bg-ea-primary
        //   bg-ea-purple-50  text-ea-blue-600  ...
        // Defined once in src/index.css (`--ea-*-rgb`); the `<alpha-value>`
        // form keeps opacity modifiers working (e.g. `bg-ea-ink/60`).
        // Accent ramps are SEMANTIC: purple = vocabulary/review, blue =
        // listening/video, orange = grammar/writing, green = reading/success.
        // Never introduce a raw hex - add a step here instead.
        // =====================================================================
        ea: {
          bg: "rgb(var(--ea-bg-rgb) / <alpha-value>)",
          surface: "rgb(var(--ea-surface-rgb) / <alpha-value>)",
          "surface-soft": "rgb(var(--ea-surface-soft-rgb) / <alpha-value>)",
          text: "rgb(var(--ea-text-rgb) / <alpha-value>)",
          ink: "rgb(var(--ea-ink-rgb) / <alpha-value>)",
          "ink-deep": "rgb(var(--ea-ink-deep-rgb) / <alpha-value>)",
          "on-ink": "rgb(var(--ea-on-ink-rgb) / <alpha-value>)",
          muted: "rgb(var(--ea-muted-rgb) / <alpha-value>)",
          "muted-deep": "rgb(var(--ea-muted-deep-rgb) / <alpha-value>)",
          border: "rgb(var(--ea-border-rgb) / <alpha-value>)",
          "primary-soft": "rgb(var(--ea-primary-soft-rgb) / <alpha-value>)",
          primary: "rgb(var(--ea-primary-rgb) / <alpha-value>)",
          "primary-end": "rgb(var(--ea-primary-end-rgb) / <alpha-value>)",
          "primary-shadow": "rgb(var(--ea-primary-shadow-rgb) / <alpha-value>)",
          "primary-deep": "rgb(var(--ea-primary-deep-rgb) / <alpha-value>)",
          "on-primary": "rgb(var(--ea-on-primary-rgb) / <alpha-value>)",
          reward: "rgb(var(--ea-reward-rgb) / <alpha-value>)",
          "purple-50": "rgb(var(--ea-purple-50-rgb) / <alpha-value>)",
          "purple-100": "rgb(var(--ea-purple-100-rgb) / <alpha-value>)",
          "purple-200": "rgb(var(--ea-purple-200-rgb) / <alpha-value>)",
          "purple-300": "rgb(var(--ea-purple-300-rgb) / <alpha-value>)",
          "purple-600": "rgb(var(--ea-purple-600-rgb) / <alpha-value>)",
          "purple-800": "rgb(var(--ea-purple-800-rgb) / <alpha-value>)",
          "blue-50": "rgb(var(--ea-blue-50-rgb) / <alpha-value>)",
          "blue-200": "rgb(var(--ea-blue-200-rgb) / <alpha-value>)",
          "blue-600": "rgb(var(--ea-blue-600-rgb) / <alpha-value>)",
          "blue-800": "rgb(var(--ea-blue-800-rgb) / <alpha-value>)",
          "orange-50": "rgb(var(--ea-orange-50-rgb) / <alpha-value>)",
          "orange-200": "rgb(var(--ea-orange-200-rgb) / <alpha-value>)",
          "orange-500": "rgb(var(--ea-orange-500-rgb) / <alpha-value>)",
          "orange-600": "rgb(var(--ea-orange-600-rgb) / <alpha-value>)",
          "orange-800": "rgb(var(--ea-orange-800-rgb) / <alpha-value>)",
          "green-50": "rgb(var(--ea-green-50-rgb) / <alpha-value>)",
          "green-200": "rgb(var(--ea-green-200-rgb) / <alpha-value>)",
          "green-600": "rgb(var(--ea-green-600-rgb) / <alpha-value>)",
          "green-900": "rgb(var(--ea-green-900-rgb) / <alpha-value>)",
          "yellow-50": "rgb(var(--ea-yellow-50-rgb) / <alpha-value>)",
          "yellow-700": "rgb(var(--ea-yellow-700-rgb) / <alpha-value>)",
          "brand-yellow": "rgb(var(--ea-brand-yellow-rgb) / <alpha-value>)",
          "danger-50": "rgb(var(--ea-danger-50-rgb) / <alpha-value>)",
          "danger-200": "rgb(var(--ea-danger-200-rgb) / <alpha-value>)",
          danger: "rgb(var(--ea-danger-rgb) / <alpha-value>)",
          "danger-700": "rgb(var(--ea-danger-700-rgb) / <alpha-value>)",
          success: "rgb(var(--ea-success-rgb) / <alpha-value>)",
          focus: "rgb(var(--ea-focus-rgb) / <alpha-value>)",
          // Per-module accents (playful multi-hue tiles). Semantic, keyed by
          // learning module - see --ea-mod-* in index.css and TopicSkillsOverview.
          "mod-vocab": "rgb(var(--ea-mod-vocab-rgb) / <alpha-value>)",
          "mod-vocab-soft": "rgb(var(--ea-mod-vocab-soft-rgb) / <alpha-value>)",
          "mod-grammar": "rgb(var(--ea-mod-grammar-rgb) / <alpha-value>)",
          "mod-grammar-soft": "rgb(var(--ea-mod-grammar-soft-rgb) / <alpha-value>)",
          "mod-reading": "rgb(var(--ea-mod-reading-rgb) / <alpha-value>)",
          "mod-reading-soft": "rgb(var(--ea-mod-reading-soft-rgb) / <alpha-value>)",
          "mod-writing": "rgb(var(--ea-mod-writing-rgb) / <alpha-value>)",
          "mod-writing-soft": "rgb(var(--ea-mod-writing-soft-rgb) / <alpha-value>)",
          "mod-speaking": "rgb(var(--ea-mod-speaking-rgb) / <alpha-value>)",
          "mod-speaking-soft": "rgb(var(--ea-mod-speaking-soft-rgb) / <alpha-value>)",
          "mod-listening": "rgb(var(--ea-mod-listening-rgb) / <alpha-value>)",
          "mod-listening-soft": "rgb(var(--ea-mod-listening-soft-rgb) / <alpha-value>)",
          "mod-roleplay": "rgb(var(--ea-mod-roleplay-rgb) / <alpha-value>)",
          "mod-roleplay-soft": "rgb(var(--ea-mod-roleplay-soft-rgb) / <alpha-value>)",
          "mod-books": "rgb(var(--ea-mod-books-rgb) / <alpha-value>)",
          "mod-books-soft": "rgb(var(--ea-mod-books-soft-rgb) / <alpha-value>)",
          "mod-video": "rgb(var(--ea-mod-video-rgb) / <alpha-value>)",
          "mod-video-soft": "rgb(var(--ea-mod-video-soft-rgb) / <alpha-value>)",
        },
        surface: token("surface"),
        "surface-dim": token("surface-dim"),
        "surface-bright": token("surface-bright"),
        "surface-container-lowest": token("surface-container-lowest"),
        "surface-container-low": token("surface-container-low"),
        "surface-container": token("surface-container"),
        "surface-container-high": token("surface-container-high"),
        "surface-container-highest": token("surface-container-highest"),
        "on-surface": token("on-surface"),
        "on-surface-variant": token("on-surface-variant"),
        "inverse-surface": token("inverse-surface"),
        "inverse-on-surface": token("inverse-on-surface"),
        outline: token("outline"),
        "outline-variant": token("outline-variant"),
        "surface-tint": token("surface-tint"),
        primary: token("primary"),
        "brand-accent": token("brand-accent"),
        "on-primary": token("on-primary"),
        "primary-container": token("primary-container"),
        "on-primary-container": token("on-primary-container"),
        "inverse-primary": token("inverse-primary"),
        secondary: token("secondary"),
        "on-secondary": token("on-secondary"),
        "secondary-container": token("secondary-container"),
        "on-secondary-container": token("on-secondary-container"),
        tertiary: token("tertiary"),
        "on-tertiary": token("on-tertiary"),
        "tertiary-container": token("tertiary-container"),
        "on-tertiary-container": token("on-tertiary-container"),
        error: token("error"),
        "on-error": token("on-error"),
        "error-container": token("error-container"),
        "on-error-container": token("on-error-container"),
        "primary-fixed": token("primary-fixed"),
        "primary-fixed-dim": token("primary-fixed-dim"),
        "on-primary-fixed": token("on-primary-fixed"),
        "on-primary-fixed-variant": token("on-primary-fixed-variant"),
        "secondary-fixed": token("secondary-fixed"),
        "secondary-fixed-dim": token("secondary-fixed-dim"),
        "on-secondary-fixed": token("on-secondary-fixed"),
        "on-secondary-fixed-variant": token("on-secondary-fixed-variant"),
        "tertiary-fixed": token("tertiary-fixed"),
        "tertiary-fixed-dim": token("tertiary-fixed-dim"),
        "on-tertiary-fixed": token("on-tertiary-fixed"),
        "on-tertiary-fixed-variant": token("on-tertiary-fixed-variant"),
        background: token("background"),
        "on-background": token("on-background"),
        // Accent (warm orange #D97745): primary CTAs and key highlights ONLY.
        accent: token("accent"),
        "accent-hover": token("accent-hover"),
        "accent-active": token("accent-active"),
        "primary-hover": token("primary-hover"),
        "primary-active": token("primary-active"),
        "surface-variant": token("surface-variant"),
        "surface-elevated": token("surface-elevated"),
        "primary-dark": token("primary-dark"),
        "text-primary": token("text-primary"),
        "text-secondary": token("text-secondary"),
        "text-muted": token("text-muted"),
        "text-disabled": token("text-disabled"),
        border: token("border"),
        divider: token("divider"),
        success: token("success"),
        "success-bg": token("success-bg"),
        warning: token("warning"),
        "warning-bg": token("warning-bg"),
        info: token("info"),
        "info-bg": token("info-bg"),
        "on-info-bg": token("on-info-bg"),
        focus: token("focus"),
        selected: token("selected"),
        "on-selected": token("on-selected"),
        disabled: token("disabled"),
        "on-disabled": token("on-disabled"),
        locked: token("locked"),
        "on-locked": token("on-locked"),
        progress: token("progress"),
        reward: token("reward"),
        "streak-active": token("streak-active"),
        "streak-inactive": token("streak-inactive"),
        "daily-hero-from": token("daily-hero-from"),
        "daily-hero-to": token("daily-hero-to"),
      },
      borderRadius: {
        // Brief radius spec: cards 24, buttons/inputs 16, dialogs 24, badges 999.
        // Named tokens make intent legible; the t-shirt scale is kept for compatibility.
        DEFAULT: "0.75rem", // 12px
        sm: "0.5rem", // 8px
        md: "0.75rem", // 12px
        lg: "1rem", // 16px
        xl: "1.5rem", // 24px
        "2xl": "1.75rem", // 28px
        card: "1.5rem", // 24px
        button: "1rem", // 16px
        input: "1rem", // 16px
        dialog: "1.5rem", // 24px
        full: "9999px",
      },
      spacing: {
        // 8pt system. Named steps used across the app plus generous large steps
        // for the whitespace the redesign brief demands.
        xs: "4px",
        sm: "8px",
        md: "16px",
        lg: "24px",
        xl: "32px",
        "2xl": "48px",
        "3xl": "64px",
        "4xl": "96px",
        gutter: "16px",
        "margin-mobile": "20px",
        "page-gutter": "var(--responsive-page-gutter)",
        "card-responsive": "var(--responsive-card-padding)",
        "section-responsive": "var(--responsive-section-gap)",
        "control-responsive": "var(--responsive-control-height)",
        "content-bottom": "var(--responsive-content-bottom)",
      },
      minHeight: {
        "responsive-viewport": "var(--responsive-viewport-height)",
        "responsive-stable": "var(--responsive-viewport-stable-height)",
      },
      boxShadow: {
        // Canonical flat card elevation.
        "ea-card": "0 16px 45px rgb(var(--ea-green-900-rgb) / 0.06)",
        "ea-card-hover": "0 22px 55px rgb(var(--ea-green-900-rgb) / 0.10)",
        // Raised elevation for playful hero/feature cards (token-driven so the
        // DesignGuard shadow rule accepts it).
        "ea-card-raised": "var(--ea-shadow-card-raised)",
        // Three elevation levels only (brief). Very subtle, professional.
        sm: "0 2px 8px rgba(0,0,0,.05)",
        md: "0 8px 24px rgba(0,0,0,.08)",
        lg: "0 24px 60px rgba(0,0,0,.12)",
        none: "none",
      },
      fontFamily: {
        sans: ["Plus Jakarta Sans", "system-ui", "sans-serif"],
        display: ["Nunito", "Plus Jakarta Sans", "system-ui", "sans-serif"],
        "headline-lg": ["Nunito", "Plus Jakarta Sans"],
        "headline-lg-mobile": ["Nunito", "Plus Jakarta Sans"],
        "headline-md": ["Nunito", "Plus Jakarta Sans"],
        "body-lg": ["Plus Jakarta Sans"],
        "body-md": ["Plus Jakarta Sans"],
        "label-md": ["Plus Jakarta Sans"],
        caption: ["Plus Jakarta Sans"],
        // Friendly display font for the brand wordmark + playful game layer.
        duo: ["Nunito", "Plus Jakarta Sans", "system-ui", "sans-serif"],
      },
      fontSize: {
        // --- Premium display/heading scale (brief: 72/56/48/40/32/24/20/18/16/14) ---
        // Large sizes get negative tracking; body sizes get generous line-height.
        "display-2xl": ["72px", { lineHeight: "1.05", letterSpacing: "-0.03em", fontWeight: "700" }],
        "display-xl": ["56px", { lineHeight: "1.08", letterSpacing: "-0.025em", fontWeight: "700" }],
        "display-lg": ["48px", { lineHeight: "1.1", letterSpacing: "-0.02em", fontWeight: "700" }],
        "display-md": ["40px", { lineHeight: "1.15", letterSpacing: "-0.02em", fontWeight: "700" }],
        "display-sm": ["32px", { lineHeight: "1.2", letterSpacing: "-0.015em", fontWeight: "700" }],
        title: ["24px", { lineHeight: "1.3", letterSpacing: "-0.01em", fontWeight: "600" }],
        // --- Existing semantic tokens (kept; retuned for readability) ---
        "headline-lg": ["30px", { lineHeight: "38px", letterSpacing: "-0.02em", fontWeight: "700" }],
        "headline-lg-mobile": ["24px", { lineHeight: "32px", fontWeight: "700" }],
        "headline-md": ["20px", { lineHeight: "28px", letterSpacing: "-0.01em", fontWeight: "600" }],
        "body-lg": ["18px", { lineHeight: "28px", fontWeight: "400" }],
        "body-md": ["16px", { lineHeight: "26px", fontWeight: "400" }],
        "label-md": ["14px", { lineHeight: "20px", letterSpacing: "0.01em", fontWeight: "600" }],
        caption: ["12px", { lineHeight: "16px", fontWeight: "400" }],
      },
      transitionTimingFunction: {
        // Gentle spring-ish ease for canonical micro-interactions.
        premium: "cubic-bezier(0.22, 1, 0.36, 1)",
      },
      keyframes: {
        "ea-float": {
          "0%,100%": { transform: "translateY(0)" },
          "50%": { transform: "translateY(-8px)" },
        },
        "ea-pulse-ring": {
          "0%": { transform: "scale(0.9)", opacity: "0.7" },
          "100%": { transform: "scale(1.6)", opacity: "0" },
        },
        "ea-pulse-soft": {
          "0%,100%": { transform: "scale(1)", opacity: "1" },
          "50%": { transform: "scale(1.12)", opacity: "0.85" },
        },
      },
      animation: {
        "ea-float": "ea-float 3s ease-in-out infinite",
        "ea-pulse-ring": "ea-pulse-ring 1.4s ease-out infinite",
        "ea-pulse-soft": "ea-pulse-soft 1.6s ease-in-out infinite",
      },
    },
  },
  plugins: [],
};
