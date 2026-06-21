// Derive every installed icon from the approved vector: englishai.pen / TVpvv.
import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";

// sharp is a declared dependency of the project's existing @capacitor/assets tool.
const requireAssets = createRequire(import.meta.resolve("@capacitor/assets"));
const sharp = requireAssets("sharp");
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const publicDir = path.join(root, "public");
const output = path.join(publicDir, "assets/brand");
const source = await fs.readFile(path.join(output, "dialog.svg"), "utf8");
const artwork = source.match(/<path\b[^>]*\/>/s)?.[0];
if (!artwork) throw new Error("Dialog SVG must contain the approved compound path.");
const background = "#F0EAFF";
function svg(size, { solid = false, fraction = 1, round = false } = {}) {
  const inset = (1 - fraction) * size / 2;
  const backdrop = solid ? (round
    ? `<circle cx="${size / 2}" cy="${size / 2}" r="${size / 2}" fill="${background}"/>`
    : `<rect width="${size}" height="${size}" fill="${background}"/>`) : "";
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">${backdrop}<g transform="translate(${inset} ${inset}) scale(${size * fraction / 120})">${artwork}</g></svg>`;
}
async function png(file, size, options) {
  await fs.mkdir(path.dirname(file), { recursive: true });
  await sharp(Buffer.from(svg(size, options))).png().toFile(file);
}
for (const size of [16, 32, 48]) await png(path.join(output, `favicon-${size}.png`), size);
for (const size of [192, 512]) await png(path.join(output, `icon-${size}.png`), size, { solid: true, fraction: .84 });
await png(path.join(output, "icon-maskable-512.png"), 512, { solid: true, fraction: .64 });
await png(path.join(output, "apple-touch-icon.png"), 180, { solid: true, fraction: .84 });
await png(path.join(output, "app-icon-1024.png"), 1024, { solid: true, fraction: .84 });
await fs.copyFile(path.join(output, "dialog.svg"), path.join(publicDir, "favicon.svg"));
await fs.copyFile(path.join(output, "favicon-48.png"), path.join(publicDir, "favicon.png"));

// ICO directory with PNG-compressed 16/32/48 representations.
const sizes = [16, 32, 48];
const images = await Promise.all(sizes.map(s => fs.readFile(path.join(output, `favicon-${s}.png`))));
const header = Buffer.alloc(6 + sizes.length * 16);
header.writeUInt16LE(1, 2); header.writeUInt16LE(sizes.length, 4);
let offset = header.length;
images.forEach((image, i) => {
  const at = 6 + i * 16;
  header[at] = sizes[i]; header[at + 1] = sizes[i];
  header.writeUInt16LE(1, at + 4); header.writeUInt16LE(32, at + 6);
  header.writeUInt32LE(image.length, at + 8); header.writeUInt32LE(offset, at + 12);
  offset += image.length;
});
await fs.writeFile(path.join(publicDir, "favicon.ico"), Buffer.concat([header, ...images]));

const res = path.join(root, "android/app/src/main/res");
for (const [density, scale] of Object.entries({ ldpi: .75, mdpi: 1, hdpi: 1.5, xhdpi: 2, xxhdpi: 3, xxxhdpi: 4 })) {
  const dir = path.join(res, `mipmap-${density}`);
  await png(path.join(dir, "ic_launcher.png"), 48 * scale, { solid: true, fraction: .84 });
  await png(path.join(dir, "ic_launcher_round.png"), 48 * scale, { solid: true, fraction: .84, round: true });
  // 108dp adaptive layer; the actual silhouette stays inside Android's 66dp safe zone.
  await png(path.join(dir, "ic_launcher_foreground.png"), 108 * scale, { fraction: .64 });
  await sharp({ create: { width: 108 * scale, height: 108 * scale, channels: 4, background } }).png().toFile(path.join(dir, "ic_launcher_background.png"));
}
for (const dir of await fs.readdir(res)) {
  if (!dir.startsWith("drawable")) continue;
  const file = path.join(res, dir, "splash.png");
  try { await fs.access(file); } catch { continue; }
  const { width, height } = await sharp(file).metadata();
  const markSize = Math.round(Math.min(width, height) * .36);
  const night = dir.includes("night");
  const mark = night ? artwork.replace('#7545E8', '#CDB8FF') : artwork;
  const scene = `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}"><rect width="100%" height="100%" fill="${night ? '#211B30' : '#FFFFFF'}"/><g transform="translate(${(width - markSize) / 2} ${(height - markSize) / 2}) scale(${markSize / 120})">${mark}</g></svg>`;
  const buffer = await sharp(Buffer.from(scene)).png().toBuffer();
  await fs.writeFile(file, buffer);
}
const pathData = artwork.match(/\sd="([^"]+)"/)?.[1];
if (!pathData) throw new Error("Missing Dialog path data.");
// Keep the historical resource ID: push notifications and inactive launcher aliases
// already point here. All of them now receive the approved Dialog silhouette.
await fs.writeFile(path.join(res, "drawable/ic_stat_parrot.xml"), `<?xml version="1.0" encoding="utf-8"?>
<!-- Approved Dialog mark / TVpvv. Legacy resource name retained for native callers. -->
<vector xmlns:android="http://schemas.android.com/apk/res/android"
    android:width="24dp" android:height="24dp" android:viewportWidth="120" android:viewportHeight="120">
    <path android:fillColor="#FFFFFFFF" android:fillType="evenOdd" android:pathData="${pathData}" />
</vector>\n`);
await fs.writeFile(path.join(res, "values/ic_launcher_background.xml"), `<?xml version="1.0" encoding="utf-8"?>
<resources><color name="ic_launcher_background">${background}</color></resources>\n`);
console.log("Dialog: favicons, PWA icons, Apple touch icon, Android launcher, splash and notification assets generated.");
