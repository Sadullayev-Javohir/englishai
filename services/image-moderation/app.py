from __future__ import annotations

import io
import os
from typing import Any

from fastapi import FastAPI, File, HTTPException, UploadFile
from PIL import Image
from nudenet import NudeDetector

MAX_BYTES = int(os.getenv("MAX_IMAGE_BYTES", "10485760"))
MIN_SCORE = float(os.getenv("UNSAFE_SCORE_THRESHOLD", "0.20"))
MODEL_VERSION = os.getenv("MODEL_VERSION", "nudenet-3.4.2")
UNSAFE_LABELS = {
    "FEMALE_GENITALIA_EXPOSED",
    "MALE_GENITALIA_EXPOSED",
    "FEMALE_BREAST_EXPOSED",
    "BUTTOCKS_EXPOSED",
    "ANUS_EXPOSED",
    "MALE_BREAST_EXPOSED",
}

app = FastAPI(title="EnglishAI image moderation", docs_url=None, redoc_url=None)
detector = NudeDetector()


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "modelVersion": MODEL_VERSION}


@app.post("/v1/classify")
async def classify(file: UploadFile = File(...)) -> dict[str, Any]:
    data = await file.read(MAX_BYTES + 1)
    if not data or len(data) > MAX_BYTES:
        raise HTTPException(status_code=413, detail="invalid image size")

    try:
        with Image.open(io.BytesIO(data)) as image:
            image.verify()
        detections = detector.detect(data)
    except Exception as exc:
        raise HTTPException(status_code=422, detail="invalid or unsupported image") from exc

    unsafe = [
        {"label": item["class"], "score": round(float(item["score"]), 6)}
        for item in detections
        if item.get("class") in UNSAFE_LABELS and float(item.get("score", 0.0)) >= MIN_SCORE
    ]
    unsafe.sort(key=lambda item: item["score"], reverse=True)
    return {
        "safe": len(unsafe) == 0,
        "reasons": unsafe,
        "modelVersion": MODEL_VERSION,
    }
