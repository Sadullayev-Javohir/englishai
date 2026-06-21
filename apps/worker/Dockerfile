FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY apps/worker/Jobs.csproj apps/worker/
RUN dotnet restore apps/worker/Jobs.csproj
COPY src/ src/
COPY apps/ apps/
RUN dotnet publish apps/worker/Jobs.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Worker.dll"]
