import test from 'node:test';
import assert from 'node:assert/strict';
import { fetchCaptionTranscript, parseCaptionJson } from './transcript.mjs';

const json3 = { events: [
  { tStartMs: 1000, dDurationMs: 2000, segs: [{ utf8: 'Hello', tOffsetMs: 0 }, { utf8: 'world.', tOffsetMs: 300 }] },
  { tStartMs: 4000, dDurationMs: 1000, segs: [{ utf8: 'Welcome back.' }] },
] };
const metadata = tracks => ({
  playability_status: { status: 'OK' },
  captions: { caption_tracks: tracks },
});
const english = { language_code: 'en', base_url: 'https://www.youtube.com/api/timedtext?v=abcdefghijk' };
const yt = info => ({ getBasicInfo: async (_video, options) => { assert.equal(options.client, 'IOS'); return info; } });
const response = (body = JSON.stringify(json3), status = 200) => new Response(body, { status });

test('reads real caption json3 without invoking getTranscript', async () => {
  const result = await fetchCaptionTranscript('abcdefghijk', yt(metadata([english])), async url => {
    assert.equal(url.searchParams.get('fmt'), 'json3');
    return response();
  });
  assert.equal(result.found, true);
  assert.equal(result.lines[0].text, 'Hello world.');
  assert.equal(result.lines[0].start, 1);
  assert.ok(result.lines[0].end > result.lines[0].start);
});
test('prefers manual English over auto captions or a foreign first track', async () => {
  let requested;
  await fetchCaptionTranscript('abcdefghijk', yt(metadata([
    { ...english, language_code: 'fr' },
    { ...english, kind: 'asr', base_url: `${english.base_url}&kind=asr` },
    { ...english, language_code: 'en-GB', base_url: `${english.base_url}&manual=1` },
  ])), async url => { requested = url; return response(); });
  assert.equal(requested.searchParams.get('manual'), '1');
});
test('accepts auto-generated English when it is the available track', async () => {
  const result = await fetchCaptionTranscript('abcdefghijk', yt(metadata([{ ...english, kind: 'asr' }])), async () => response());
  assert.equal(result.found, true);
});
test('only confirmed playable metadata with no English track is terminal', async () => {
  for (const tracks of [[], [{ ...english, language_code: 'fr' }]]) {
    assert.deepEqual(await fetchCaptionTranscript('abcdefghijk', yt(metadata(tracks))), { found: false, reason: 'no-english-captions' });
  }
});
test('unplayable/challenged metadata remains retryable', async () => {
  for (const status of ['UNPLAYABLE', 'LOGIN_REQUIRED', undefined]) {
    await assert.rejects(fetchCaptionTranscript('abcdefghijk', yt({ playability_status: { status } })), /temporarily unavailable/);
  }
});
test('empty 200 is retryable, never no captions', async () => {
  await assert.rejects(fetchCaptionTranscript('abcdefghijk', yt(metadata([english])), async () => response('')), /no usable text/);
});
test('malformed body, missing events and empty events remain retryable', async () => {
  for (const body of ['not json', '{}', '{"events":[]}']) {
    await assert.rejects(fetchCaptionTranscript('abcdefghijk', yt(metadata([english])), async () => response(body)));
  }
});
test('HTTP failures and aborted transport propagate as retryable failures', async () => {
  await assert.rejects(fetchCaptionTranscript('abcdefghijk', yt(metadata([english])), async () => response('rate limit', 429)), /429/);
  const signal = AbortSignal.abort();
  await assert.rejects(fetchCaptionTranscript('abcdefghijk', yt(metadata([english])), async (_url, init) => {
    assert.equal(init.signal, signal);
    init.signal.throwIfAborted();
  }, signal));
});
test('only trusted HTTPS caption URLs are followed', async () => {
  for (const base_url of ['https://youtube.com.evil.test/captions', 'http://youtube.com/captions', 'https://127.0.0.1/private']) {
    await assert.rejects(fetchCaptionTranscript('abcdefghijk', yt(metadata([{ ...english, base_url }]))), /Unexpected caption host/);
  }
});
test('an empty preferred track can fall back to another English track', async () => {
  let calls = 0;
  const result = await fetchCaptionTranscript('abcdefghijk', yt(metadata([english, { ...english, kind: 'asr' }])), async () => response(++calls === 1 ? '' : JSON.stringify(json3)));
  assert.equal(calls, 2);
  assert.equal(result.found, true);
});
test('rolling newline events and duplicate words do not produce duplicate lines', () => {
  const result = parseCaptionJson({ events: [
    json3.events[0], { tStartMs: 1000, dDurationMs: 2000, segs: [{ utf8: 'Hello', tOffsetMs: 0 }] },
    { tStartMs: 3000, segs: [{ utf8: '\n' }] }, json3.events[1],
  ] });
  assert.deepEqual(result.map(line => line.text), ['Hello world.', 'Welcome back.']);
});
test('numeric string times are accepted and invalid/negative times skipped', () => {
  const result = parseCaptionJson({ events: [
    { tStartMs: '1000', dDurationMs: '1000', segs: [{ utf8: '>> Hello.', tOffsetMs: '100' }] },
    { tStartMs: -1, segs: [{ utf8: 'Bad.' }] }, { segs: [{ utf8: 'Bad.' }] },
  ] });
  assert.equal(result.length, 1);
  assert.equal(result[0].start, 1.1);
  assert.equal(result[0].text, 'Hello.');
});
test('long unpunctuated captions are split into bounded readable lines', () => {
  const result = parseCaptionJson({ events: [{ tStartMs: 0, dDurationMs: 6000, segs: [{ utf8: Array(40).fill('word').join(' ') }] }] });
  assert.equal(result.length, 3);
  assert.ok(result.every(line => line.text.split(' ').length <= 14 && line.end > line.start));
});
