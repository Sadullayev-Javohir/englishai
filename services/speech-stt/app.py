"""Self-hosted speech-to-text for the free tier.

Azure Speech is billed per audio hour. At the free tier's usage that cost is larger than the whole
free cohort can ever be worth, so free learners are transcribed here instead: the model runs on the
same VPS and the marginal cost of a request is CPU time we have already paid for. Premium keeps
Azure, whose accuracy on accented speech - and its pronunciation scoring - is what the subscription
is actually for.

Contract mirrors services/image-moderation: FastAPI on 8080, GET /health plus one POST verb, all
configuration from the environment, no database access and no baked secrets (docs/development-guide.md).
"""

from __future__ import annotations

import io
import math
import os
import wave
from typing import Any

from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from faster_whisper import WhisperModel

# 2 MB matches SpeakingLive:MaxAudioBytes on the backend, so a clip the hub accepted is never
# rejected here for size.
MAX_AUDIO_BYTES = int(os.getenv("MAX_AUDIO_BYTES", "2097152"))
MODEL_SIZE = os.getenv("WHISPER_MODEL", "base.en")
# int8 keeps the model inside a modest memory limit and roughly doubles CPU throughput; the accuracy
# it costs is not the differentiator between the tiers (pronunciation scoring is).
COMPUTE_TYPE = os.getenv("WHISPER_COMPUTE_TYPE", "int8")
# Greedy decoding. Beam search costs several times the CPU for a small gain on short utterances, and
# latency is what a learner notices in a conversation.
BEAM_SIZE = int(os.getenv("WHISPER_BEAM_SIZE", "1"))
DEVICE = os.getenv("WHISPER_DEVICE", "cpu")
MODEL_VERSION = f"faster-whisper-{MODEL_SIZE}-{COMPUTE_TYPE}"
# Longest prompt passed to the decoder. The backend sends the conversation's focus words and the
# learner's name so those are recognised; an unbounded prompt would eat the context window.
MAX_PROMPT_CHARS = 200

app = FastAPI(title="EnglishAI speech-to-text", docs_url=None, redoc_url=None)
model = WhisperModel(MODEL_SIZE, device=DEVICE, compute_type=COMPUTE_TYPE)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "modelVersion": MODEL_VERSION}


def _duration_seconds(data: bytes) -> float:
    """Audio length from the WAV header, falling back to the 16 kHz mono 16-bit rate."""
    try:
        with wave.open(io.BytesIO(data), "rb") as handle:
            frames = handle.getnframes()
            rate = handle.getframerate() or 16_000
            return frames / float(rate)
    except (wave.Error, EOFError):
        return len(data) / 32_000.0


def _confidence(segments: list[Any]) -> float | None:
    """
    A 0..1 confidence derived from the decoder's average log-probability.

    Deliberately NOT comparable to Azure's confidence - the backend keeps a separate threshold for
    this service, because reusing Azure's would silently reject or accept the wrong utterances.
    """
    scored = [segment for segment in segments if segment.avg_logprob is not None]
    if not scored:
        return None
    mean_logprob = sum(segment.avg_logprob for segment in scored) / len(scored)
    return max(0.0, min(1.0, math.exp(mean_logprob)))


@app.post("/v1/transcribe")
async def transcribe(
    file: UploadFile = File(...),
    language: str = Form("en"),
    prompt: str = Form(""),
) -> dict[str, Any]:
    data = await file.read(MAX_AUDIO_BYTES + 1)
    if not data or len(data) > MAX_AUDIO_BYTES:
        raise HTTPException(status_code=413, detail="invalid audio size")

    initial_prompt = prompt.strip()[:MAX_PROMPT_CHARS] or None

    try:
        segments, _info = model.transcribe(
            io.BytesIO(data),
            language=language or "en",
            beam_size=BEAM_SIZE,
            initial_prompt=initial_prompt,
            # Whisper hallucinates confident text on silence; the VAD filter is what stops a learner
            # who said nothing from being shown a sentence they never spoke (docs/development-guide.md rule 8).
            vad_filter=True,
            condition_on_previous_text=False,
        )
        collected = list(segments)
    except Exception as exc:  # noqa: BLE001 - any decode failure is the same answer to the caller
        raise HTTPException(status_code=422, detail="invalid or unsupported audio") from exc

    text = " ".join(segment.text.strip() for segment in collected).strip()
    return {
        "text": text,
        "confidence": _confidence(collected),
        "durationSeconds": _duration_seconds(data),
        "modelVersion": MODEL_VERSION,
    }
