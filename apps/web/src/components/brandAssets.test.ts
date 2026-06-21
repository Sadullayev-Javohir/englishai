// @vitest-environment node
import { readFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const publicDir = resolve(process.cwd(), "public");
const read = (file: string) => readFileSync(resolve(publicDir, file));
const dimensions = (file: string) => {
  const png = read(file);
  expect(png.subarray(1, 4).toString()).toBe("PNG");
  return [png.readUInt32BE(16), png.readUInt32BE(20)];
};

describe("approved Dialog brand assets", () => {
  it("uses the selected vector with transparent counters, including the SVG favicon", () => {
    const svg = read("assets/brand/dialog.svg").toString();
    expect(svg).toContain("TVpvv");
    expect(svg).toContain('fill-rule="evenodd"');
    expect(svg).toContain('fill="#7545E8"');
    expect(read("favicon.svg").toString()).toBe(svg);
  });

  it("keeps Android notification and inactive launcher aliases on the same Dialog path", () => {
    const res = resolve(process.cwd(), "android/app/src/main/res");
    const nativeMark = readFileSync(resolve(res, "drawable/ic_stat_parrot.xml"), "utf8");
    const vectorPath = read("assets/brand/dialog.svg").toString().match(/\sd="([^"]+)"/)?.[1];
    expect(nativeMark).toContain(`android:pathData="${vectorPath}"`);
    expect(nativeMark).toContain('android:fillType="evenOdd"');
    for (const tone of ["red", "gray"]) {
      expect(readFileSync(resolve(res, `drawable/ic_launcher_inactive_${tone}.xml`), "utf8")).toContain("@drawable/ic_stat_parrot");
    }
  });

  it("ships dedicated PWA, maskable, touch and favicon sizes", () => {
    for (const [name, size] of Object.entries({ "favicon-16.png": 16, "favicon-32.png": 32, "favicon-48.png": 48, "icon-192.png": 192, "icon-512.png": 512, "icon-maskable-512.png": 512, "apple-touch-icon.png": 180, "app-icon-1024.png": 1024 })) {
      expect(dimensions(`assets/brand/${name}`)).toEqual([size, size]);
    }
    const manifest = JSON.parse(read("site.webmanifest").toString());
    expect(manifest.icons.find((icon: { purpose: string }) => icon.purpose === "maskable").src).toBe("/assets/brand/icon-maskable-512.png");
    for (const icon of manifest.icons) expect(existsSync(resolve(publicDir, icon.src.slice(1)))).toBe(true);
    const ico = read("favicon.ico");
    expect(ico.readUInt16LE(2)).toBe(1);
    expect(ico.readUInt16LE(4)).toBe(3);
  });
});
