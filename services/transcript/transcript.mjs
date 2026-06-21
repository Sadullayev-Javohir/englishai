/** Public English caption tracks, not the separately guarded watch-page transcript panel. */
export async function fetchCaptionTranscript(videoId, yt, fetchImpl = fetch, signal) {
  const info = await yt.getBasicInfo(videoId, { client: 'IOS' });
  if (info.playability_status?.status !== 'OK') {
    // Unplayable/challenged metadata does not prove the absence of subtitles.
    throw new Error('YouTube caption metadata is temporarily unavailable');
  }
  const tracks = info.captions?.caption_tracks;
  if (tracks !== undefined && !Array.isArray(tracks)) throw new Error('Unexpected caption metadata');
  const english = (tracks ?? []).filter(track => /^en(?:-|$)/i.test(track.language_code ?? ''))
    .sort((a, b) => Number(a.kind === 'asr') - Number(b.kind === 'asr'));
  if (english.length === 0) return { found: false, reason: 'no-english-captions' };

  for (const track of english) {
    if (!track.base_url) continue;
    const url = new URL(track.base_url);
    if (url.protocol !== 'https:' || !/(^|\.)youtube\.com$/.test(url.hostname)) {
      throw new Error('Unexpected caption host');
    }
    url.searchParams.set('fmt', 'json3');
    const response = await fetchImpl(url, { signal });
    if (!response.ok) throw new Error(`Caption track returned HTTP ${response.status}`);
    const text = await response.text();
    // An empty HTTP 200 is an upstream failure, NOT evidence that the video has no captions.
    if (!text.trim()) continue;
    const lines = parseCaptionJson(JSON.parse(text));
    if (lines.length) return { found: true, lines };
  }
  throw new Error('English caption tracks returned no usable text');
}

/** Flatten rolling json3 events and regroup timed words into readable, non-duplicated lines. */
export function parseCaptionJson(payload) {
  if (!Array.isArray(payload?.events)) throw new Error('Unexpected caption response');
  const words = [];
  for (const event of payload.events) {
    if (!Array.isArray(event.segs)) continue;
    const start = Number(event.tStartMs);
    if (!Number.isFinite(start) || start < 0) continue;
    const duration = Number(event.dDurationMs) || 0;
    for (const segment of event.segs) {
      if (typeof segment.utf8 !== 'string') continue;
      const tokens = segment.utf8.replace(/\s*>+\s*/g, ' ').trim().split(/\s+/).filter(Boolean);
      const offset = Number(segment.tOffsetMs ?? 0);
      if (!Number.isFinite(offset) || offset < 0) continue;
      const step = tokens.length > 1 && duration > 0 ? Math.min(500, Math.max(51, duration / tokens.length)) : 51;
      tokens.forEach((text, index) => words.push({ text, start: (start + offset + index * step) / 1000 }));
    }
  }
  words.sort((a, b) => a.start - b.start);
  const unique = words.filter((word, index) => !words.slice(Math.max(0, index - 8), index)
    .some(previous => previous.text === word.text && Math.abs(previous.start - word.start) < 0.05));
  const lines = [];
  let sentence = [];
  function flush(nextStart) {
    if (!sentence.length) return;
    const start = sentence[0].start;
    const last = sentence.at(-1).start;
    const end = Math.max(start + 0.5, Math.min(last + 0.6, nextStart ?? Infinity));
    lines.push({ start, end, text: sentence.map(word => word.text).join(' ') });
    sentence = [];
  }
  for (const word of unique) {
    if (sentence.length && (word.start - sentence.at(-1).start > 1 || sentence.length >= 14)) flush(word.start);
    sentence.push(word);
    if (/[.!?]["')\]]*$/.test(word.text)) flush();
  }
  flush();
  return lines;
}
