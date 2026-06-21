# EnglishAI.uz frontend (React + Vite) served as static files by nginx.
#   docker build -f apps/web/Dockerfile -t englishai-frontend apps/web
# VITE_GOOGLE_CLIENT_ID must be passed at BUILD time (Vite inlines env vars into the
# bundle): docker build --build-arg VITE_GOOGLE_CLIENT_ID=... ./frontend

# ---- build ----
FROM node:22-alpine AS build
WORKDIR /app

# sharp normally installs a prebuilt binary. Keep the native toolchain available
# so a transient prebuild download failure cannot break the release image build.
RUN apk add --no-cache python3 make g++

# Google OAuth client id is baked into the bundle (LoginPage reads import.meta.env).
ARG VITE_GOOGLE_CLIENT_ID=""
ENV VITE_GOOGLE_CLIENT_ID=${VITE_GOOGLE_CLIENT_ID}
ARG VITE_PUBLIC_ASSET_BASE_URL=""
ARG VITE_PUBLIC_ASSET_VERSION=""
ENV VITE_PUBLIC_ASSET_BASE_URL=${VITE_PUBLIC_ASSET_BASE_URL}
ENV VITE_PUBLIC_ASSET_VERSION=${VITE_PUBLIC_ASSET_VERSION}

COPY package.json package-lock.json ./
RUN npm ci --ignore-scripts

COPY . .
RUN npm run build

# ---- serve ----
FROM nginx:1.29-alpine AS serve
RUN apk upgrade --no-cache
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY seo-routes.conf /etc/nginx/seo-routes.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
