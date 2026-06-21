# EnglishAI.uz backend (.NET 8 Web API + SignalR). Build context = repo root.
#   docker build -f apps/api/Dockerfile -t englishai-backend .
# Multi-stage: SDK image restores/publishes, runtime image stays small.

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# global.json pins the SDK feature band; Directory.Build.props holds shared MSBuild props.
COPY global.json Directory.Build.props ./

# Copy only the csproj files first so `restore` is cached until a dependency changes.
COPY src/Domain/Domain.csproj            src/Domain/
COPY src/Application/Application.csproj   src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY apps/api/Api.csproj apps/api/
RUN dotnet restore apps/api/Api.csproj

# Now copy the rest of the source and publish a self-contained framework-dependent build.
COPY src/ src/
COPY apps/ apps/
RUN dotnet publish apps/api/Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# .NET 8 images already listen on 8080 by default (ASPNETCORE_HTTP_PORTS=8080).
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production

# curl: compose healthcheck against /health. python3 + pip: run the yt-dlp zipapp that fetches video
# captions for the interactive transcript (PROJECT-SPEC B.3). curl_cffi gives yt-dlp browser TLS
# impersonation (--impersonate): from this image's datacenter/VPS IP YouTube otherwise serves a bot
# challenge and reports EVERY video as "unavailable", so no transcript ever loads. unzip: installs the
# deno JS runtime below.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl unzip python3 python3-pip \
    && pip3 install --no-cache-dir --break-system-packages "curl_cffi>=0.7" \
    && rm -rf /var/lib/apt/lists/*

# deno: a JavaScript runtime on PATH. Recent yt-dlp DEPRECATED YouTube extraction without one
# ("No supported JavaScript runtime could be found") - without it the player challenge can't be
# solved and many videos fail to resolve. yt-dlp auto-detects deno on PATH, so no flag is needed.
RUN curl -fsSL https://github.com/denoland/deno/releases/latest/download/deno-x86_64-unknown-linux-gnu.zip -o /tmp/deno.zip \
    && unzip -o /tmp/deno.zip -d /usr/local/bin \
    && chmod +x /usr/local/bin/deno \
    && rm /tmp/deno.zip \
    && deno --version

# Use the real-browser impersonation just installed (curl_cffi) for every caption fetch - this is the
# fix that lets the server reach YouTube's caption surface from a datacenter IP. Read by YtDlpOptions
# (config section "YtDlp"); double-underscore is the .NET env-var nesting convention.
ENV YtDlp__Impersonate=chrome

# Ship yt-dlp into the image so the transcript provider (YtDlpExecutable resolves tools/yt-dlp by
# walking up from the app base dir) can fetch captions. The repo-local tools/yt-dlp is gitignored,
# so the CI checkout that builds this image won't have it - download it here instead. "latest"
# keeps it current with YouTube's caption surface (an old yt-dlp breaks on YouTube changes); the
# layer is cached until this line changes, so it is not re-downloaded every build. The final check
# confirms the impersonation targets are actually available (curl_cffi wired up correctly).
RUN mkdir -p /app/tools \
    && curl -fsSL https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /app/tools/yt-dlp \
    && chmod +x /app/tools/yt-dlp \
    && python3 /app/tools/yt-dlp --version \
    && python3 /app/tools/yt-dlp --list-impersonate-targets

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Web.dll"]
