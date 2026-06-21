# Transcript service

A tiny Node.js sidecar that fetches a YouTube video's caption track with
[`youtubei.js`](https://github.com/LuanRT/YouTube.js) (the InnerTube API the YouTube web client
uses). It is the **first provider** in EnglishAI's transcript fallback chain - the .NET
`YoutubeiTranscriptProvider` calls it over HTTP, and if it cannot return a transcript the
orchestrator falls through to `yt-dlp`, cookie-backed `yt-dlp`, and finally the Supadata API.

The service reads English caption tracks from basic player metadata and downloads
their `json3` timed text. It does not depend on the watch-page `get_transcript`
endpoint. Manual English tracks are preferred to auto-generated English tracks;
other languages are not substituted. An upstream error, unplayable metadata, empty
HTTP 200 or malformed caption body returns **502**, never a false "no captions".

It is **stateless**: every request is fetched live and nothing is cached or stored (transcripts are
never persisted - see `docs/development-guide.md`).

## API

| Method & path            | Body              | Response |
|--------------------------|-------------------|----------|
| `GET /health`            | -                 | `200 { "status": "ok" }` |
| `POST /transcript`       | `{ "videoId" }`   | `200 { "found": true, "lines": [{ "start", "end", "text" }] }` - captions retrieved |
|                          |                   | `200 { "found": false, "reason" }` - video genuinely has no transcript (terminal) |
|                          |                   | `502 { "error" }` - transient failure (caller retries / falls through) |

The three response shapes map 1:1 to the .NET `TranscriptFetchOutcome` enum
(`Fetched` / `NoCaptions` / `ProviderUnavailable`).

## Run

```bash
cd services/transcript
npm install
PORT=8080 npm start
# POST http://localhost:8080/transcript  { "videoId": "dQw4w9WgXcQ" }
```

Run the deterministic parser/provider tests with `npm test` (no network required).

Or via Docker Compose (service name `transcript-service`, exposed on host port 8090). The .NET app
reaches it through the `Youtubei:BaseUrl` setting (e.g. `http://transcript-service:8080`).

## Config

| Env var                  | Default | Purpose |
|--------------------------|---------|---------|
| `PORT`                   | `8080`  | HTTP listen port |
| `TRANSCRIPT_TIMEOUT_MS`  | `25000` | Per-request fetch timeout |
