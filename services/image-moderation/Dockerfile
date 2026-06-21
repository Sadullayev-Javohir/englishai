FROM python:3.12-slim AS python-base
# Same intent as the `apk upgrade` in services/transcript/Dockerfile: the Debian base carries
# whatever OS CVEs were open the day the tag was published, and the release scan fails the build
# on any HIGH. Upgrading here patches both the build and the runtime stage in one layer.
RUN apt-get update \
    && apt-get upgrade -y --no-install-recommends \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /usr/local/lib/python3.12/site-packages/msgpack* \
        /usr/local/lib/python3.12/site-packages/setuptools*

FROM python-base AS build
WORKDIR /app
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
COPY requirements.txt .
RUN python -m venv /opt/venv \
    && /opt/venv/bin/pip install --no-cache-dir --upgrade pip \
    && /opt/venv/bin/pip install --no-cache-dir -r requirements.txt \
    && /opt/venv/bin/python -c "from nudenet import NudeDetector; NudeDetector()" \
    && rm -rf /opt/venv/lib/python3.12/site-packages/pip* \
        /opt/venv/lib64/python3.12/site-packages/pip* \
        /opt/venv/lib/python3.12/site-packages/setuptools* \
        /opt/venv/lib64/python3.12/site-packages/setuptools*

FROM python-base
WORKDIR /app
ENV PATH="/opt/venv/bin:$PATH" PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
COPY --from=build /opt/venv /opt/venv
COPY app.py .
RUN useradd --create-home --uid 10001 appuser
USER appuser
EXPOSE 8080
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080", "--workers", "1"]
