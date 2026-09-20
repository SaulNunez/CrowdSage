# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

CrowdSage is a Stack Overflow–style Q&A app: an ASP.NET Core 10 Web API (`CrowdSage.Server`) serving a React 19 + Vite SPA (`crowdsage.client`), with PostgreSQL via EF Core and OpenIddict for auth. The `.sln` ties all three projects together; the server `.csproj` references the client `.esproj` (`ReferenceOutputAssembly=false`) so a server build also builds the SPA, and `Microsoft.AspNetCore.SpaProxy` launches `npm run dev` during development.

## Commands

```bash
# Backend
dotnet build CrowdSage.sln
dotnet run --project CrowdSage.Server          # https://localhost:7017 / http://localhost:5017, Swagger at /swagger
dotnet test CrowdSage.Server.Tests             # all tests
dotnet test CrowdSage.Server.Tests --filter "FullyQualifiedName~QuestionsServiceTests"
dotnet test CrowdSage.Server.Tests --filter "Name=AddQuestionAsync_NullPayload_ThrowsArgumentNullException"

# EF Core migrations (Npgsql provider; a reachable Postgres is not needed to add one)
dotnet ef migrations add <Name> --project CrowdSage.Server
```

```bash
# Frontend (run from crowdsage.client/)
npm run dev      # https://localhost:51708 — generates an ASP.NET dev cert on first run via `dotnet dev-certs`
npm run build    # tsc -b && vite build
npm run lint     # eslint .
```

```bash
# Full stack in Docker (server + Postgres); reads DATABASE_NAME / DATABASE_PASSWORD from ./.env
./start-dev-compose.sh
```

CI (`.github/workflows/unit-tests.yml`) restores, builds Release, and runs the test project on push/PR to `master`. There is no frontend job.

## Architecture

**Request flow:** Controller → Service interface → `CrowdsageDbContext`. Controllers are thin: they pull `userId` from `User.FindFirstValue(ClaimTypes.NameIdentifier)`, call the service, and translate exceptions to status codes (`KeyNotFoundException` → 404, `ArgumentNullException` → 400, everything else logged → 500). Services throw those exceptions rather than returning result types, and they construct output DTOs themselves — controllers never see entities. Each service is declared alongside its interface in the same file (e.g. `IQuestionsService` at the bottom of `Services/QuestionsService.cs`) and registered scoped in `Program.cs`.

**Two DTO namespaces, and they overlap by filename:**
- `Models/InsertUpdate/` — request payloads (mutable classes). Note the type names don't match the filenames: `InsertUpdate/QuestionDto.cs` declares `QuestionPayload`, `InsertUpdate/AnswerDto.cs` declares `AnswerPayload`, etc.
- `Models/Outputs/` — response `record` types with `required`/`init` members. `QuestionDto`, `AnswerDto`, `AuthorDto`, … Because both namespaces contain a `QuestionDto`, files that use both need fully-qualified or aliased names.

**Auth:** ASP.NET Identity (`CrowdsageUser : IdentityUser`, extended with `ProfilePicObjectKey`) plus OpenIddict in password-grant mode. The only token endpoint is `POST /connect/token` (`AuthorizationController.Exchange`) — note it is *not* under `/api`. Registration is `POST /register` (`AccountController`, which has no `[Route]` attribute, so the action's own template is the full path). Data Protection keys persist to the DB (`IDataProtectionKeyContext`), so tokens survive restarts and multiple instances.

**Database:** `CrowdsageDbContext` extends `IdentityDbContext<CrowdsageUser>` and calls `options.UseOpenIddict()`. `Program.cs` runs `context.Database.Migrate()` at startup via `InitializeDb`, so the app self-migrates on boot. Provider is Npgsql; a SQLite line is commented out next to it and `SqliteDemoDb.db` is a leftover — the Sqlite package is still referenced.

**Frontend state:** RTK Query is the whole data layer. `src/store/reducers.ts` holds every endpoint in a single `questionsApi` with tag-based invalidation (`Question`, `Answer`, `QuestionComment`, `AnswerComment`); answer-scoped tags use composite ids like `` `${questionId}#${answerId}` ``. `src/store/authSlice.ts` holds the bearer token and mirrors it into `localStorage`; `prepareHeaders` in the base query reads it from the Redux state. Base URL comes from `VITE_CROWDSAGE_BACKEND_URL` in `crowdsage.client/.env`.

**Frontend structure:** `src/Components/` = presentational pieces, `src/Screens/` = routed pages (routes declared in `main.tsx`), `src/Shared/` = reusable widgets like `MarkdownEditor`. Tailwind v4 + daisyUI are configured purely through `src/index.css` (`@import "tailwindcss"; @plugin "daisyui";`) — there is no `tailwind.config`. Strings go through `react-i18next` (`src/locales/{en,es}/translation.json`); newer components use `t(...)`, older ones still have literals. The `@` alias maps to `./src`.

## Known inconsistencies to watch for

- **Route naming is not uniform.** `api/questions` (plural) for questions and `api/questions/{questionId}/answers/{answerId}/comments`, but `api/question/{questionId}/answers`, `api/question/{questionId}/comment`, `api/question/bookmark`, `api/answer/bookmark` (singular). Check the controller's `[Route]` before assuming a path.
- **Client base URL already ends in `/api`**, and `fetchBaseQuery` joins it with each endpoint's relative path, so endpoint paths in `reducers.ts` must not repeat the prefix. Two endpoints are deliberate exceptions: register and `connect/token` live at the server root, so they build an absolute URL from `serverRootUrl` (the base URL with `/api` stripped), which `joinUrls` passes through untouched.
- **CORS** allows exactly one origin, `https://localhost:51708` (the Vite dev port), hardcoded in `Program.cs`.
- **Dark mode is per-screen local state.** Several screens each keep their own `darkMode` `useState` and toggle `documentElement.classList` — there is no shared theme store yet.
- **`editQuestionComment` in `reducers.ts` targets the wrong route.** It PUTs to `questions/{questionId}` (the question itself) instead of `question/{questionId}/comment/{commentId}`, and its params carry no comment id. Fixing it requires changing the mutation's argument type; it currently has no call sites.
- `WeatherForecast.cs` and the `^/weatherforecast` proxy rule in `vite.config.ts` are scaffolding remnants.
