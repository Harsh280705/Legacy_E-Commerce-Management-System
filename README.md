# LegacyEcommerce — .NET Framework 4.7 + MVC 5 + EF6 + SQL Server + Docker + Nginx

Traditional server-rendered ecommerce demo ("Yantra Bazaar") built on the exact
legacy stack, containerised so the **host toolchain is untouched**.

| Layer | Technology (pinned) |
|---|---|
| Web | .NET Framework 4.7, ASP.NET MVC 5.2.7, Razor, OWIN cookie auth, Identity 2.2.3, Unity 5.11.7, jQuery 3.4.1, Bootstrap 3.4.1 |
| Data | Entity Framework 6.4.4 (Database-First EDMX `Ecommerce.Data/EcommerceModel.edmx` + matching DbContext), SQL Server 2022 |
| Runtime | Mono 6.12 + XSP4 (Linux containers), Nginx 1.27-alpine reverse proxy |
| DB init | Idempotent `db/schema.sql` + `db/seed.sql` (100 products) / `db/seed_1000.sql` (1000) |

Architecture: `Ecommerce.Web → Ecommerce.Services → Ecommerce.Core`, `Ecommerce.Data → Ecommerce.Core`.
Controllers never touch EF; views bind ViewModels. Cart is session-backed with
server-side totals; checkout uses idempotency keys; payments are demo-only.

## 1. Prerequisites

- Windows 10/11, Docker Desktop (Linux containers), PowerShell 5.1+, Git.
- Host ports **8085** free (verified 2026-10-11; change in `docker-compose.yml` if needed).
- No global .NET/MSBuild/VS install required — builds run inside `mono:6.12`.

## 2. Configure

```powershell
Copy-Item .env.example .env
# then edit .env and set a strong DB_PASSWORD (8+ chars, upper/lower/digit/symbol)
```

`.env` is git-ignored. Never commit it.

## 3. Build & start (first time, ~10 min: pulls + NuGet + MSBuild)

```powershell
docker compose -p legacyecommerce build web
docker compose -p legacyecommerce up -d
docker compose -p legacyecommerce ps        # wait until db/web/nginx are healthy
```

Open **http://localhost:8085** · Health: **http://localhost:8085/health** (returns `OK`).

## 4. Verify / tests

```powershell
powershell -ExecutionPolicy Bypass -File tests/smoke.ps1      # routes, cart, checkout, auth
powershell -ExecutionPolicy Bypass -File tests/extended.ps1   # bad login, history, static refs
docker logs legacyecommerce-dbinit   # expect "Seed demo-100 OK / DB init complete"
```

## 5. Everyday commands

```powershell
docker compose -p legacyecommerce ps
docker compose -p legacyecommerce logs web | Select-Object -Last 50
docker compose -p legacyecommerce logs db  | Select-Object -Last 20
docker compose -p legacyecommerce stop          # stop, KEEP data
docker compose -p legacyecommerce start         # restart, data intact
docker compose -p legacyecommerce build web; docker compose -p legacyecommerce up -d   # rebuild after code changes
```

Re-running `up` re-applies schema/seed **idempotently** (no drops, no duplicates).

## 6. Data & volumes

- Volume `legacyecommerce_mssql_data` holds `/var/opt/mssql` (survives stop/start).
- Backup: `docker exec legacyecommerce-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<pw>" -C -Q "BACKUP DATABASE LegacyEcommerceDb TO DISK='/var/opt/mssql/backup.bak'"`
  then `docker cp legacyecommerce-db:/var/opt/mssql/backup.bak .\backup.bak`.
- Expand catalogue to 1000: set `SEED_SIZE=1000` in `.env`, then `up -d` (adds missing rows only).

## 7. DANGER — database reset (deletes this project's data only)

```powershell
# WARNING: permanently deletes LegacyEcommerceDb data and seed. Other projects untouched.
docker compose -p legacyecommerce down
docker volume rm legacyecommerce_mssql_data
docker compose -p legacyecommerce up -d   # recreates + reseeds from scratch
```
Never run `docker compose down -v` casually, never run global `docker system prune`.

## 8. Port conflicts (without killing anything)

```powershell
netstat -ano | Select-String ":8085"
docker ps --format "{{.Names}} {{.Ports}}"
```
If 8085 is taken, edit the `ports:` line in `docker-compose.yml` (e.g. `8086:80`).

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `dbinit` exit 1, `QUOTED_IDENTIFIER` | schema needs `SET QUOTED_IDENTIFIER ON` (already in `db/schema.sql`) |
| `IX_... already exists` on rerun | every index in schema.sql is `IF NOT EXISTS`-guarded |
| `Could not load ... Unsafe 4.0.4.1` | Unity needs `System.Runtime.CompilerServices.Unsafe` ref (in Web.csproj) |
| `SqlProviderServices could not be loaded` | Web must reference `EntityFramework.SqlServer` directly |
| Razor `MvcWebRazorHostFactory 5.0.0.0` not found | `Views/Web.config` pins factory to bin version 5.2.7.0 |
| `ObjectToDictionaryUncached` missing | bin must contain WebPages/Razor **3.x** — Dockerfile overwrites from NuGet (Mono GAC has 2.0) |
| Register/Login 500 (NRE in Unity.Mvc5) | custom `Infrastructure/UnityResolver.cs` replaces Unity.Mvc5 module |
| Anti-forgery mismatch on multi-form pages | one token per request via `Views/_ViewStart.cshtml` stash (`AF_TOKEN_HTML`) |
| Login `HttpContext is required` | auth is synchronous (Task.Run bridge); no awaits on request thread |
| `localhost/Account/Login` (port 80) redirects | nginx uses `proxy_set_header Host $http_host` to preserve `:8085` |
| NuGet `Unity.Abstractions.5.11.8 not found` | that version was never published; Unity pinned to **5.11.7** |
| `deb.debian.org buster 404` in docker build | Buster archived; Dockerfile repoints apt to `archive.debian.org` |
| Web unhealthy, `/health` 500 | `docker exec legacyecommerce-web curl -s localhost:8080/<route>` shows Mono yellow-screen (local only) |

## 10. Measured storage (2026-10-11)

- `legacyecommerce-web:1.0` **813 MB** · `mssql/server:2022` **2.34 GB** · `nginx:1.27-alpine` **74.5 MB**
  (+ `mono:6.12` 1.09 GB / `mono:6.12-slim` 388 MB build bases, shared layers).
- Seed images: 12 PNGs, ~27 KB total. Seed SQL: 100 rows default.

## 11. What was actually executed vs inspected

Executed: full `build` + `up`, health checks, smoke + extended suites
(18 + 3 assertions, all passing), guest + authed checkouts, duplicate-submit
replay, stop/start persistence, rerun idempotency, port/volume isolation.
Inspected only: responsive breakpoints (CSS media queries present, no device lab),
browser console (no browser automation on this laptop; all asset URLs return 200).
