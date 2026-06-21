import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";
import fg from "fast-glob";
import { describe, expect, it } from "vitest";

/**
 * The canonical responsive scale. Five bands, derived from where the shell
 * actually changes shape - the sidebar appears at 901px and widens at 1181px -
 * not from device marketing names:
 *
 *   xs      <= 420px    mobile  <= 700px    tablet  701-900px
 *   laptop  901-1180px  desktop >= 1181px
 *
 * Before this test the codebase carried 39 distinct breakpoint values across 53
 * stylesheets (700px, 701px, 699px, 680px, 640px, 620px, 600px, 599px ... all
 * meaning "phone"), so neighbouring components switched layout at different
 * widths and tablets fell through the gaps. Any new width has to be one of
 * these eight numbers or this test fails the build.
 */
const LEGAL_MAX = new Set([420, 700, 900, 1180]);
const LEGAL_MIN = new Set([421, 701, 901, 1181]);

const here = path.dirname(fileURLToPath(import.meta.url));
const srcRoot = path.resolve(here, "..");

const WIDTH_FEATURE = /\((min|max)-width:\s*(\d+)px\)/g;
const MEDIA_RULE = /@media([^{]+)\{/g;

interface Offence {
  file: string;
  line: number;
  condition: string;
  value: string;
}

function collectOffences(): Offence[] {
  const files = fg.sync("**/*.css", { cwd: srcRoot, absolute: true });
  const offences: Offence[] = [];

  for (const file of files) {
    const source = readFileSync(file, "utf8");
    for (const rule of source.matchAll(MEDIA_RULE)) {
      const condition = rule[1].trim();
      for (const feature of condition.matchAll(WIDTH_FEATURE)) {
        const kind = feature[1];
        const px = Number(feature[2]);
        const legal = kind === "min" ? LEGAL_MIN : LEGAL_MAX;
        if (legal.has(px)) continue;
        offences.push({
          file: path.relative(srcRoot, file),
          line: source.slice(0, rule.index).split("\n").length,
          condition,
          value: `${kind}-width: ${px}px`,
        });
      }
    }
  }
  return offences;
}

describe("responsive bands", () => {
  it("only uses canonical breakpoint values in stylesheets", () => {
    const offences = collectOffences();
    const report = offences
      .map((o) => `  ${o.file}:${o.line}  ${o.value}  in  @media ${o.condition}`)
      .join("\n");

    expect(
      offences,
      offences.length
        ? `Kanonik bo'lmagan breakpoint topildi.\n` +
            `max-width faqat ${[...LEGAL_MAX].join("/")}px, ` +
            `min-width faqat ${[...LEGAL_MIN].join("/")}px bo'lishi mumkin.\n${report}`
        : ""
    ).toEqual([]);
  });

  it("never leaves a band range inverted", () => {
    const files = fg.sync("**/*.css", { cwd: srcRoot, absolute: true });
    const inverted: string[] = [];

    for (const file of files) {
      const source = readFileSync(file, "utf8");
      for (const rule of source.matchAll(MEDIA_RULE)) {
        const condition = rule[1];
        const min = /\(min-width:\s*(\d+)px\)/.exec(condition);
        const max = /\(max-width:\s*(\d+)px\)/.exec(condition);
        if (min && max && Number(min[1]) > Number(max[1])) {
          const line = source.slice(0, rule.index).split("\n").length;
          inverted.push(
            `  ${path.relative(srcRoot, file)}:${line}  @media ${condition.trim()}`
          );
        }
      }
    }

    expect(
      inverted,
      inverted.length ? `Hech qachon mos kelmaydigan oraliq:\n${inverted.join("\n")}` : ""
    ).toEqual([]);
  });
});
