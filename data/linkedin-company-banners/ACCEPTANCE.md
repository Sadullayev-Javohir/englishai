# LinkedIn Company Page covers: acceptance report

Validated on 2026-08-11 against the final package in this directory.

## Requirement-to-observation matrix

| Explicit requirement | Public output / boundary | Concrete check | Observed result |
|---|---|---|---|
| LinkedIn Company Page sizing | `englishai-linkedin-company-cover-01.jpg` through `-05.jpg` | LinkedIn Help live page plus Pillow and Chromium natural dimensions | LinkedIn Help displayed Page Cover **4200 × 700**. All five JPEGs decoded as **4200 × 700**. PASS. |
| Five-slide dynamic cover | Five numbered JPEGs and upload ZIP | LinkedIn dynamic-cover Help live page; validator exact-count check; ZIP inventory | Live Help states up to five ordered images. Package contains exactly five in order `01`–`05`. PASS. |
| Company logo space on left | Every numbered JPEG | Validator analyzes leftmost 1260 px, exactly 30% of 4200 px, for foreground edges | All five reported `safe-edge=0.000`. PASS. |
| Real EnglishAI information | Visible copy in all five JPEGs | Claims sourced from `https://englishai.uz/llms-full.txt`; exported JPEG OCR mapped per cover | Facts read back 2/2, 3/3, 2/2, 2/2, and 3/3. PASS. |
| Modern visual presentation | Five JPEGs and safe-zone preview | Rendered preview and Chromium decode | Files render successfully and use the project’s canonical indigo system. This confirms renderability, not subjective user approval. User visual approval remains unobserved. |
| LinkedIn upload format and size | Every numbered JPEG | File-format and byte-size validator | All are JPEG; files are 129–169 KB, below LinkedIn’s 3 MB maximum. PASS. |
| Device crop resilience | Visible content placement | Live Help crop guidance plus validator core-content region check | Key content is outside outer edges and lower-right risk area; core dynamic range checks pass. Actual post-upload device renders remain unobserved. |
| Responsive crop validation | Every numbered JPEG | Durable validator OCR after 5% trim from both horizontal edges, 7% trim from top/bottom, and combined trimming | Every validation run checks 15 cropped renders; the latest run retained both mapped primary-message terms for all modes. PASS. This is representative evidence, not the authenticated LinkedIn renderer. |
| One convenient deliverable | `englishai-linkedin-company-covers-upload.zip` | ZIP ordered-name comparison and CRC/integrity test | ZIP contains exactly the five ordered JPEGs; `unzip -t` reports no errors. PASS. |
| Extracted upload boundary | Five JPEGs extracted from the upload ZIP | SHA-256/source byte comparison plus HTTP/Chromium decode of the clean extraction | All 5 extracted files byte-match accepted sources; Chromium returned HTTP 200, `image/jpeg`, `complete=true`, 4200 × 700 for 5/5. PASS. |
| No obsolete wrong-format output | Repository inventory | Assert old `data/linkedin-carousel` is absent | The rejected 1080 × 1350 package and generator were removed. PASS. |
| Regression rejection: missing slide | Validator public CLI with isolated package | Removed slide 3 and ran `--output` | Exit 1; reported four images and the missing filename. PASS. |
| Regression rejection: polluted logo zone | Validator public CLI with isolated package | Drew foreground content across cover 2 left 30% | Exit 1; reported `left 30% foreground-free`. PASS. |
| Browser/public file decode | Five JPEGs served over HTTP | Chromium requested each upload image | HTTP 200, `image/jpeg`, `complete=true`, natural size 4200 × 700 for 5/5. PASS. |
| Company-logo placement proof | `englishai-linkedin-company-covers-logo-placement.jpg` | Actual EnglishAI logo composited inside each left safe zone; geometry compared with source foreground start; proof decoded in Chromium | Preview logo ends at x=274 while foreground begins at x=438; Chromium reported `complete=true`, 1260 × 1098. Non-overlap PASS. This is a representative proof, not a LinkedIn screenshot. |
| Real LinkedIn integration | Canonical public and admin URLs | Chromium visited `linkedin.com/company/englishai-uz/` and `/admin/` | Both redirected to LinkedIn authentication. Final editor upload, slideshow rotation, Page crop, and real device render are **acceptance-blocked** by owner Premium super-admin authentication. |

## Honest status

The asset package is technically validated and ready for the owner to upload. It is not valid to claim that LinkedIn accepted or rendered it because the authenticated Premium Company Page editor was unavailable. It is also not valid to claim final visual approval until the user reviews the supplied preview or uploaded Page.
