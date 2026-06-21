// youtubei.js-backed YouTube transcript sidecar (the FIRST provider in EnglishAI's transcript
// fallback chain). It exposes a tiny HTTP API the .NET YoutubeiTranscriptProvider calls:
//
//   GET  /health                      -> { status: "ok" }
//   POST /transcript { videoId }      -> 200 { found: true,  lines: [...] }   (captions retrieved)
//                                        200 { found: false, reason: "..." }  (video has no captions)
//                                        502 { error: "..." }                 (fetch failed - transient)
//
// Contract mirrors the .NET TranscriptFetchOutcome triplet so the orchestrator can tell a genuinely
// caption-less video (terminal) apart from a transient provider failure (retry / fall through):
//   - found:true  -> Fetched
//   - found:false -> NoCaptions
//   - non-2xx     -> ProviderUnavailable
//
// Stateless by design: every request is fetched live from YouTube and nothing is cached or stored
// (docs/development-guide.md - transcripts are never persisted to a DB/cache; each request is real-time).

import http from 'node:http';
import { Innertube } from 'youtubei.js';
import { fetchCaptionTranscript } from './transcript.mjs';

const PORT = Number(process.env.PORT) || 8080;
const REQUEST_TIMEOUT_MS = Number(process.env.TRANSCRIPT_TIMEOUT_MS) || 25000;

// A single shared InnerTube session is reused across requests (creating one negotiates keys with
// YouTube and is comparatively expensive). It is lazily (re)created so a transient session failure
// self-heals on the next request instead of wedging the service.
let innertubePromise = null;
function getInnertube() {
  if (!innertubePromise) {
    innertubePromise = Innertube.create({
      retrieve_player: false,
      generate_session_locally: true,
      lang: 'en',
      location: 'US',
      fetch: (input, init) => fetch(input, { ...init, signal: init?.signal ?? AbortSignal.timeout(REQUEST_TIMEOUT_MS) }),
    }).catch((err) => {
      innertubePromise = null; // allow a fresh attempt next request
      throw err;
    });
  }
  return innertubePromise;
}

/**
 * Fetches a video's transcript via youtubei.js. Returns:
 *   { found: true, lines }            when a caption track was retrieved,
 *   { found: false, reason }          when the video genuinely has no transcript,
 * and throws on any transient failure (network, session, rate-limit) so the caller answers 502.
 */
async function fetchTranscript(videoId, signal) {
  const yt = await getInnertube();
  return fetchCaptionTranscript(videoId, yt, fetch, signal);
}

/** Reads and JSON-parses a request body, capped so a malformed/huge body cannot exhaust memory. */
function readJsonBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on('data', (chunk) => {
      size += chunk.length;
      if (size > 64 * 1024) {
        reject(new Error('Request body too large'));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on('end', () => {
      if (chunks.length === 0) return resolve({});
      try {
        resolve(JSON.parse(Buffer.concat(chunks).toString('utf8')));
      } catch (err) {
        reject(err);
      }
    });
    req.on('error', reject);
  });
}

function sendJson(res, status, payload) {
  const body = JSON.stringify(payload);
  res.writeHead(status, { 'Content-Type': 'application/json; charset=utf-8' });
  res.end(body);
}

/** Rejects after the configured timeout so a hung YouTube fetch never holds the connection open. */
function withTimeout(promise, ms) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error(`Transcript fetch timed out after ${ms}ms`)), ms);
    promise.then(
      (value) => {
        clearTimeout(timer);
        resolve(value);
      },
      (err) => {
        clearTimeout(timer);
        reject(err);
      },
    );
  });
}

const server = http.createServer(async (req, res) => {
  if (req.method === 'GET' && req.url === '/health') {
    return sendJson(res, 200, { status: 'ok' });
  }

  if (req.method !== 'POST' || !req.url?.startsWith('/transcript')) {
    return sendJson(res, 404, { error: 'Not found' });
  }

  let videoId;
  try {
    const body = await readJsonBody(req);
    videoId = typeof body.videoId === 'string' ? body.videoId.trim() : '';
  } catch {
    return sendJson(res, 400, { error: 'Invalid JSON body' });
  }

  if (!videoId || !/^[A-Za-z0-9_-]{11}$/.test(videoId)) {
    return sendJson(res, 400, { error: 'A valid 11-character YouTube videoId is required' });
  }

  try {
    const result = await withTimeout(fetchTranscript(videoId, AbortSignal.timeout(REQUEST_TIMEOUT_MS)), REQUEST_TIMEOUT_MS);
    return sendJson(res, 200, result);
  } catch (err) {
    // Transient: network, rate-limit, session, timeout, or an unexpected payload shape. The .NET
    // side maps any non-2xx to ProviderUnavailable and falls through to the next provider.
    console.error(`[transcript] ${videoId} failed: ${err?.message ?? err}`);
    return sendJson(res, 502, { error: String(err?.message ?? 'Transcript fetch failed') });
  }
});

server.listen(PORT, () => {
  console.log(`youtube-transcript-service listening on :${PORT}`);
});
