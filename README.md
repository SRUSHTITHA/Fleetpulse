# FleetPulse

**Real-time fleet & delivery tracking with automatic exception detection.**

FleetPulse tracks delivery vehicles live on a map and watches for problems — a vehicle
stopped too long, a delivery running late, a driver off the planned route, or a vehicle
speeding — and alerts dispatchers the moment something looks wrong.

---

## The idea

Most tracking tools just show where vehicles are. FleetPulse goes one step further: it
continuously monitors every active trip and **detects problems automatically**.

- Rules run on every incoming GPS point and again in a background engine every 30 seconds,
  so nothing is missed — even when a driver goes silent.
- Each ongoing problem produces **one alert**, not a flood — the same alert is updated while
  the problem persists and resolved automatically once it clears.
- Dispatchers see locations and alerts **live, with no refreshing** (SignalR).

Three kinds of users:

| Role | What they can do |
| **Driver** | Start/end trips, share GPS location every ~30 s, see their own trip |
| **Dispatcher** | Live map of the whole fleet, real-time alert feed, manage vehicles |
| **Admin** | Everything a dispatcher can do, plus settings, rules, and user roles |

## Features

- **Live fleet map** — React + Leaflet (free OSM tiles, no API key required).
- **Real-time updates** — SignalR pushes every vehicle position and new alert to dispatchers.
- **Automatic exception engine** — four configurable detection rules (see below).
- **Role-based auth** — JWT with `Admin` / `Dispatcher` / `Driver` roles; optional Google sign-in.
- **Driver app** — start/end trips and stream location, with GPS lock indicators.
- **One-command startup** — the whole stack runs in Docker Compose.
- **Self-hosted routing** — no dependency on public routing services (with optional Google Directions when a key is set).

### The four detection rules

| Rule | Fires when |
| **Stop Too Long** | Vehicle hasn't moved more than ~50 m for X minutes |
| **Delivery Trending Late** | Much more time used than route progress (e.g. 50 % of the time gone, only 20 % of the trip done) |
| **Route Deviation** | Vehicle is more than X meters off the straight line between start and finish |
| **Speed Anomaly** | Vehicle drives faster than X km/h |

## Architecture (the basics)

```
Browser (React app)
     │
     ▼
nginx  ── serves the website, proxies API + live-updates
     │
     ▼
.NET 8 API ── login, trips, locations, alerts, SignalR hub, background rule engine
     │
     ▼
PostgreSQL 16 ── all data: users, vehicles, trips, GPS history, alerts, rules
```

- The browser only ever talks to one address (nginx). Everything else happens behind it.
- The frontend and backend are separate and communicate over a clean REST + SignalR API,
  so either can be changed without touching the other.
- The backend is a small layered monolith: `Api → Application → Infrastructure → Domain`,
  each layer depending only on the one below it.

### The trip loop

1. Driver starts a trip and streams GPS points (~every 30 s).
2. The API saves each point, checks the rules right away, and broadcasts live to dispatchers.
3. A background engine re-checks every active trip every 30 s so quiet drivers aren't missed.
4. Detected problems appear in the dispatcher's alert feed in real time.

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | React 19, Vite, TypeScript, React-Leaflet, SignalR client |
| Backend | .NET 8 Web API, SignalR, EF Core |
| Database | PostgreSQL 16 (Npgsql) |
| Auth | JWT Bearer + BCrypt
| Infra | Docker Compose (web/nginx + api + db + self-hosted OSRM router) |
## Getting started

**Requirements:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (free).
That's the only thing you need — no .NET, Node, or database installs.

```powershell
# 1. Start everything (first start downloads images + compiles)
docker compose up -d --build

# 2. Open the app
#
```