# Yantra Bazaar — Legacy E-Commerce Management System

A server-rendered e-commerce demo store with product catalog, search and
filtering, shopping cart, user authentication, and demo checkout.

## Technologies

ASP.NET MVC 5, C#, Entity Framework 6, SQL Server, Razor, jQuery, Docker,
and Nginx. Builds run inside a Mono container, so no local .NET install
is required.

## Features

- Product catalog with categories, search, filtering, and sorting
- Pagination and infinite-scroll browsing
- Session-backed shopping cart with AJAX mini-cart
- Registration, login, and order history
- Multi-step demo checkout (no real payments)

## Run it

Prerequisites: Docker Desktop and a free host port 8085.

```powershell
Copy-Item .env.example .env   # then set a strong DB_PASSWORD in .env
docker compose build web
docker compose up -d
```

Open http://localhost:8085 (health: http://localhost:8085/health).

## Credentials

`.env` is git-ignored — never commit it. Copy `.env.example`, set a
strong `DB_PASSWORD`, and keep real secrets off GitHub. Leave the optional
admin bootstrap blank on public deployments.
