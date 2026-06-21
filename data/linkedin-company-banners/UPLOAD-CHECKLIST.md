# LinkedIn owner upload checklist

Use this only from a desktop browser while signed in as a **Premium Company Page super admin** for:

`https://www.linkedin.com/company/englishai-uz/`

## Upload files in this exact order

1. `englishai-linkedin-company-cover-01.jpg`
2. `englishai-linkedin-company-cover-02.jpg`
3. `englishai-linkedin-company-cover-03.jpg`
4. `englishai-linkedin-company-cover-04.jpg`
5. `englishai-linkedin-company-cover-05.jpg`

The same five files are available in `englishai-linkedin-company-covers-upload.zip`.

**Do not upload any 1080 × 1350 feed-carousel image.** That obsolete package was removed. A LinkedIn Page dynamic cover must use the numbered **4200 × 700** JPEG files above.

## Before upload

- Run `python3 ops/tools/validate_linkedin_company_banners.py` and require `LINKEDIN_COMPANY_BANNERS_ACCEPTANCE=PASS`.
- Confirm there are exactly five numbered JPEGs.
- Confirm the red safe-zone guide exists only in `englishai-linkedin-company-covers-safe-zone-preview.jpg`. Never upload the preview.
- Keep the existing Page cover available locally so it can be restored if LinkedIn crops the new set unexpectedly.

## LinkedIn editor path

Based on LinkedIn's live Help flow:

1. Open the Page **super admin view** on desktop.
2. Find the Page cover image in the upper-left area.
3. Click the edit icon in the cover image's upper-right corner.
4. Choose the dynamic cover slideshow option.
5. Select the five numbered JPEGs above.
6. Preserve the `01` through `05` order in the editor.
7. Review each editor crop before saving.
8. Save only after every slide keeps the main message visible and the left logo area unobstructed.

## Post-upload acceptance

Observe the actual Page, not only the editor preview.

- Desktop logged-in view: all five slides rotate in the intended order.
- Desktop logged-out/incognito view: the public Page shows the slideshow when LinkedIn permits public access.
- Mobile LinkedIn app or mobile browser: headline and CTA remain visible after LinkedIn's crop.
- Company logo does not cover text or product graphics on any slide.
- No key text is clipped at the top, bottom, or lower-right corner.
- Every slide remains readable at normal browser zoom.
- The slideshow transitions through all five slides without a blank or duplicate frame.

## Accept or rollback

Accept only when every post-upload item above passes. If any slide is clipped, reordered, duplicated, blurry, or covered by the logo, do not leave the broken slideshow live. Restore the prior cover, capture which slide/device failed, and adjust the source generator before uploading again.

Record the final result in `ACCEPTANCE.md` with the date, device/browser, observed order, and any LinkedIn crop adjustments.
