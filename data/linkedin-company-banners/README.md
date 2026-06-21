# EnglishAI LinkedIn Company Page covers

- Official LinkedIn Page Cover size: **4200 × 700 px**.
- Format: high-resolution JPEG, each file below LinkedIn's 3 MB maximum.
- The leftmost **30% (1260 px)** is intentionally free of foreground content for the company logo overlay.
- Text is kept away from outer edges and the lower-right corner because LinkedIn may crop covers across devices.
- Product claims come from `https://englishai.uz/llms-full.txt`, updated 2026-08-06.
- LinkedIn specification source: `https://www.linkedin.com/help/linkedin/answer/a563309/`.
- Dynamic cover slides require a Premium Company Page subscription.
- LinkedIn allows up to five images per slideshow; this package intentionally contains exactly five.
- The feature is configured from Page super admin view and is available on desktop.
- Run `python3 ops/tools/validate_linkedin_company_banners.py` after regeneration.

## Canonical facts used

1. 24/7 AI tutor and immediate feedback.
2. Six linked skills around one topic.
3. CEFR A1–C2 with 50 ordered lessons at each level.
4. Active recall with 3/7/30-day SRS review.
5. Web and Android access, placement test, two free topics, and daily practice.

The `safe-zone-preview` and `logo-placement` files are review-only proofs. Never upload either preview. Upload the five numbered JPEG files in order, or extract `englishai-linkedin-company-covers-upload.zip`. Follow `UPLOAD-CHECKLIST.md` from an authenticated Premium super-admin desktop session.

## Acceptance status

- Canonical company URL: `https://www.linkedin.com/company/englishai-uz/` from the app's `officialContacts` configuration.
- LinkedIn Help was verified in a real browser: Page Cover is 4200 × 700, high-resolution JPEG is recommended, and key content should avoid edges and the lower-right crop area.
- All five JPEGs decode through Chromium over HTTP as `image/jpeg` at a natural size of 4200 × 700.
- The public company page and `/admin/` route redirect unauthenticated browsers to LinkedIn's auth wall. Final upload and the Premium dynamic-cover UI require the owner's authenticated LinkedIn admin session.
- LinkedIn's live dynamic-cover Help page confirms that a slideshow accepts up to five ordered images and rotates them at the top of the Page.
