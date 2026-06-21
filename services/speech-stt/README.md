# speech-stt

Self-hosted speech-to-text for the **free tier**.

## Why it exists

Azure Speech is billed per audio hour. Even a small free-tier speaking allowance, multiplied across
free accounts, costs more than the free cohort can be worth at any realistic conversion rate. Free
learners are therefore transcribed by this sidecar, where a request's marginal cost is CPU time on a
VPS we already pay for.

Premium keeps Azure. That is not only cheaper to reason about, it is the honest product line:
Azure's accuracy on accented speech and its per-phoneme pronunciation scoring are what a
subscription buys.

## Contract

| | |
|---|---|
| `GET /health` | `{"status":"ok","modelVersion":"faster-whisper-base.en-int8"}` |
| `POST /v1/transcribe` | multipart: `file` (16 kHz mono 16-bit PCM WAV), `language` (default `en`), `prompt` (optional, ≤200 chars) |

Response:

```json
{ "text": "...", "confidence": 0.82, "durationSeconds": 3.4, "modelVersion": "..." }
```

`413` when the upload exceeds `MAX_AUDIO_BYTES`, `422` when the audio cannot be decoded.

`prompt` carries the conversation's focus words and the learner's name — the same list the Azure
adapter passes as a phrase-list grammar — so topic vocabulary and Uzbek names are recognised.

## Configuration

| Variable | Default | Notes |
|---|---|---|
| `WHISPER_MODEL` | `base.en` | Also a build arg; the weights are baked into the image |
| `WHISPER_COMPUTE_TYPE` | `int8` | |
| `WHISPER_BEAM_SIZE` | `1` | Greedy; beam search costs several times the CPU for little gain on short turns |
| `WHISPER_DEVICE` | `cpu` | |
| `MAX_AUDIO_BYTES` | `2097152` | Matches `SpeakingLive:MaxAudioBytes` |

## Operational notes

- **`confidence` is not comparable to Azure's.** It is derived from the decoder's average
  log-probability. The backend keeps a separate `WhisperStt:MinimumConfidence`; reusing the Azure
  threshold would reject or accept the wrong utterances.
- **No N-best alternatives.** The transcript-confirmation flow (offering the learner a choice of
  readings) is an Azure-only affordance and does not appear for free learners.
- **`vad_filter` is on.** Whisper otherwise emits confident text for silence, which would show a
  learner a sentence they never said (docs/development-guide.md rule 8).
- **Latency is the risk to watch.** `base.en` on two cores runs at roughly 0.3–0.6× realtime, so a
  15-second turn adds several seconds before the tutor even starts thinking. Measure on the target
  VPS before enabling for all free learners; `tiny.en` or more cores are the knobs.
