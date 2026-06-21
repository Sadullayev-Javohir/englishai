# youtubei.js YouTube-transcript sidecar (first provider in the transcript fallback chain).
FROM node:20-alpine AS dependencies

WORKDIR /app

# Install the exact dependency graph from the committed lockfile.
COPY package.json package-lock.json ./
RUN npm ci --omit=dev --no-audit --no-fund

FROM node:20-alpine AS runtime

WORKDIR /app

RUN apk upgrade --no-cache \
    && rm -rf /usr/local/lib/node_modules/npm /opt/yarn* \
    && rm -f /usr/local/bin/npm /usr/local/bin/npx /usr/local/bin/yarn /usr/local/bin/yarnpkg /usr/local/bin/corepack

COPY --from=dependencies /app/node_modules ./node_modules
COPY package.json ./
COPY server.mjs ./
COPY transcript.mjs ./

ENV PORT=8080
EXPOSE 8080

# Lightweight liveness probe against the in-process /health endpoint (no curl needed).
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=5 \
  CMD node -e "fetch('http://localhost:'+(process.env.PORT||8080)+'/health').then(r=>process.exit(r.ok?0:1)).catch(()=>process.exit(1))"

CMD ["node", "server.mjs"]
