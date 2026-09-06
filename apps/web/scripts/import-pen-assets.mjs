import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const pen = JSON.parse(readFileSync(resolve(root, '../../data/design-assets/englishai.pen'), 'utf8'));
const nodes = new Map();
function visit(node) { nodes.set(node.id, node); (node.children ?? []).forEach(visit); }
pen.children.forEach(visit);
const esc = value => String(value).replaceAll('&', '&amp;').replaceAll('"', '&quot;').replaceAll('<', '&lt;');
const color = value => typeof value === 'string' && value.startsWith('$') ? pen.variables[value.slice(1)].value : value;
function render(n, top = false) {
  if (n.enabled === false) return '';
  const { x = 0, y = 0, width = 0, height = 0 } = n;
  const fill = typeof n.fill === 'string' ? color(n.fill) : 'none';
  const attrs = `fill="${esc(fill)}"${n.stroke ? ` stroke="${esc(color(n.stroke))}" stroke-width="${n.strokeWidth ?? 1}" stroke-linecap="${n.strokeLinecap ?? 'butt'}"` : ''}`;
  let shape;
  if (n.type === 'path') shape = `<svg x="${x}" y="${y}" width="${width}" height="${height}" viewBox="${(n.viewBox ?? [0, 0, width, height]).join(' ')}" preserveAspectRatio="none" overflow="visible"><path d="${esc(n.geometry)}" ${attrs} fill-rule="${n.fillRule ?? 'nonzero'}"/></svg>`;
  else if (n.type === 'ellipse') shape = `<ellipse cx="${x + width / 2}" cy="${y + height / 2}" rx="${width / 2}" ry="${height / 2}" ${attrs}/>`;
  else if (n.type === 'rectangle') shape = `<rect x="${x}" y="${y}" width="${width}" height="${height}" rx="${typeof n.cornerRadius === 'number' ? n.cornerRadius : 0}" ${attrs}/>`;
  else if (n.children) shape = `<g transform="translate(${top ? 0 : x} ${top ? 0 : y})">${n.children.map(c => render(c)).join('')}</g>`;
  else throw new Error(`Unsupported artwork node ${n.id}: ${n.type}`);
  return n.rotation ? `<g transform="rotate(${-n.rotation} ${x} ${y})">${shape}</g>` : shape;
}
const out = resolve(root, 'public/assets/play');
mkdirSync(out, { recursive: true });
for (const [id, name] of [['TTwkH', 'mascot'], ['X6ZBze', 'parrot'], ['MDAzH', 'words-world'], ['vmejw', 'my-mother'], ['V0vmL', 'island-secret']]) {
  const n = nodes.get(id);
  writeFileSync(resolve(out, `${name}.svg`), `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${n.width} ${n.height}" role="img">${render(n, true)}</svg>`);
}
const screenFrames = pen.children.filter(n => / • (WEB|MOBILE) • /.test(n.name));
// The manifest is the canonical product inventory: 72 paired WEB/MOBILE states.
// Later Pen frames document supplementary/admin/mobile/playlist explorations and
// must not expand the route-audit contract.
const canonicalRoute = route => {
  if (route.startsWith('/app/speaking/pronunciation/')) return '/app/speaking/pronunciation/:word';
  if (route === '/video/import') return '/video/search';
  if (route.startsWith('/video/playlists/')) return '/video/playlists/:playlistId';
  return route;
};
const screens = screenFrames.slice(0, 144).map(n => {
  const [number, viewport, route, title] = n.name.split(' • ');
 return { nodeId: n.id, number, viewport, width: n.width, route: canonicalRoute(route), title };
});
writeFileSync(resolve(root, 'src/app/penScreenManifest.json'), JSON.stringify(screens, null, 2) + '\n');
console.log(`Imported 5 original vector assets and ${screens.length} screen references.`);
