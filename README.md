# EnglishAI

EnglishAI — ingliz tilini o'rganish uchun .NET, React va yordamchi AI servislaridan tashkil topgan monorepo.

## Repository map

| Path | Vazifa |
| --- | --- |
| `apps/api/` | ASP.NET Core API host va HTTP/SignalR composition root |
| `apps/worker/` | Background job worker host |
| `apps/web/` | React + TypeScript web/mobile client |
| `src/` | Domain, Application va Infrastructure qatlamlari |
| `services/` | Mustaqil transcript va image moderation sidecar servislar |
| `ops/` | Deploy, operational scripts va developer tooling |
| `tests/` | Backend unit, integration va load tests |
| `data/` | Versionlangan curriculum/source ma'lumotlari |

Arxitektura, kod standartlari va delivery qoidalari:
[`docs/development-guide.md`](docs/development-guide.md).

Mobil migratsiya hujjatlari: [`master plan`](docs/flutter-mobile-master-plan.md)
va [`implementation plan`](docs/flutter-mobile-implementation-plan.md).

## Quick start

```bash
docker compose up -d postgres redis transcript-service image-moderation
dotnet run --project apps/api/Api.csproj

cd apps/web
npm ci
npm run dev
```

Backend odatda `http://localhost:5045`, frontend esa `http://localhost:5173` da ishlaydi.

## Main validation

```bash
dotnet build EnglishAI.sln
dotnet test EnglishAI.sln --no-build

cd apps/web
npx tsc -b --pretty false
npm run lint
npm test -- --run
npm run build
```
