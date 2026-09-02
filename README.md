# LifeTracker

LifeTracker is a very specific personal insight and analytics platform designed to uncover interesting long term correlations across varying life data streams.

Instead of leaving personal data and metrics locked inside separate isolated "walled gardens" (smartwatch biometrics, smart home sensors, desktop activity tracking, and hardware monitors), LifeTracker unifies everything under custom database ownership to draw meaningful and interesting insights over long periods.

### The Core Goal: Finding Long-Term Correlations & Insights

Data collection is just step one. The real power comes from combining long-term datasets to analyze how different aspects of daily life interact, here's some examples of the initial plan:

- **Environment, Exercise & Sleep:** How room climate measurements (ESP32 with SDC40 sensor), local weather (Buienradar) and periods of regular vs less/to no exercise directly impact sleep quality and HRV.

- **Stress & Physical State:** How staying up late or changing exercise frequency can impact baseline resting heart rate over weeks or months.

- **Activity & Physiology:** Correlating desktop usage (ActivityWatch) or competitive gaming performance (e.g., *Deadlock*, *League of Legends*) with real-time biometric spikes to quantify stress during specific matches and how your performance could alter those results, such as a bad frustrating loss vs a hard fought win.

- **Hardware, Climate & Software Workloads:** Tracking how the weather and differing seasons combined with desktop activity like idling, heavy development environments or extensive gaming sessions directly impact hardware performance and metrics such as temps, voltages and CPU/GPU usage. Since as far as im aware no real hardware monitor exists that actually combines your specific desktop activity, so this would bridge another common gap between different applications and data sets. Also seeing how long term heavy load could affect room temperature would be interesting.

### Custom Ingestion Pipelines

The architecture is designed to integrate and centralize custom data sources and APIs. The current metrics (Garmin smartwatch biometrics, ESP32 with room climate sensor, ActivityWatch desktop activity) represent the initial phase, with upcoming expansions for PC hardware telemetry(WIP), nutrition tracking(Cronometer), competetive game match data and with even more to come.

---

## Tech Stack & Architecture

.NET 10 Web API + EF Core / PostgreSQL. A small Python FastAPI bridge/sidecar handles Garmin Connect integration (unofficial API). Everything runs in 3 seperate containers via Docker Compose.


### Component Overview

| Component | Role / Description |
| --- | --- |
| **LifeTracker API** | .NET 10 Web API, JWT authentication, OpenAPI / Scalar documentation |
| **PostgreSQL 16** | Centralized database, golden record of all the different integrations and data sets |
| **Garmin Bridge** | Python FastAPI bridge/sidecar using `garminconnect`(unofficial library for Garmin API), OAuth tokens persists on a docker volume |
| **ESP32** | Ingests local room climate node with SCD40 sensor(C02, Temp, Humidity) |
| **ActivityWatch** | Desktop activity tracker integration |
| **GitHub Actions** | CI pipeline handling automated restores, builds, and test suites |

## Quick Start

> **Note for reviewers:** This is a **personal** stack built around specific hardware, accounts, and live data streams. Because it is designed solely for a single-user(as of now), reviewers will not have a matching Garmin watch, ActivityWatch instance, or physical ESP32 sensor. However, by default the repository runs in **Demo Mode** with a database that gets seeded with records on first launch(as of now just Garmin records since those are the most extensive routes)  You can spin up the stack very easily with Docker Compose to explore the OpenAPI/Scalar UI and test all(Garmin) `GET` endpoints without anything else required.
> Also Everything is **still HTTP instead of HTTPS** since it's still local development and i haven't setup Azure deployment yet.


### 1. Launch the Stack

Run the full stack in Demo Mode out of the box:

Skip if you don't have a garmin account and or no real data on there to use, docker will skip by default for demo environment 
```bash
cp Bridges/garmin_bridge/.env.example Bridges/garmin_bridge/.env
# fill GARMIN_EMAIL / GARMIN_PASSWORD only if you want live sync
# for the garmin profile
docker compose up -d db garmin_bridge
```

Normal docker command without garmin python container 
```bash
docker compose up --build -d
```

Once startup completes (automatic EF Core migrations and Garmin DB seeder apply on boot):

* **API Root:** `http://localhost:5071`
* **Scalar API Docs:** `http://localhost:5071/scalar`
* **API Health Check:** `http://localhost:5071/health`
* **Garmin Bridge Health:** `http://localhost:9002/garmin/health`

You can check out and test all the existing routes by looking at the Scalar page, most of them have been documented for OpenAPI so you can see all the parameters, expected results/return values and status codes

By default the environment is set to Demo mode after cloning, **meaning the JWT auth bearer is preset in Scalar** and the DB gets seeded with fake values for all the Garmin Entities since those are the most extensive routes(right now) and since the /sync functions wouldn't work cause there'd be no garmin account to connect to. But in this way you can still test out the Garmin GET routes  


### Run the API from Visual Studio instead

```bash
docker compose up --build -d # for without garmin
docker compose up -d db garmin_bridge # with garmin python bridge
```

Set `JWT:Key` and `JWT:Password` in user secrets or `appsettings.Development.json` (32+ character key). `appsettings.json` leaves them empty on purpose.

## Auth

All routes require JWT except `/health`, `/`, OpenAPI/Scalar, `POST /api/auth/token`, and `POST /api/room-climate`.

```http
POST /api/auth/token
{ "password": "<JWT:Password>" }
```

In Demo/Development, Scalar is pre-filled with a generated Bearer token so you can click routes without pasting headers.

## Garmin API shape

For full API overview check /scalar after setting up and running docker

Reads never call Garmin. Writes do.

| Method | Path | Source / Description |
| --- | --- | --- |
| `GET` | `/garmin/heartrate?date=yyyy-MM-dd` | Read from DB |
| `GET` | `/garmin/stress` | Read from DB |
| `GET` | `/garmin/sleep` | Read from DB |
| `GET` | `/garmin/day` | Composite query (HR + Stress required, Sleep optional) |
| `GET` | `/garmin/all` | All stored daily records |
| `POST` | `/garmin/sync/heartrate` | Syncs sidecar data to DB (Upsert) |
| `POST` | `/garmin/sync/stress` | Syncs sidecar data to DB (Upsert) |
| `POST` | `/garmin/sync/sleep` | Syncs sidecar data to DB (Upsert) |
| `POST` | `/garmin/sync/day` | Full day sync from sidecar to DB (Upsert) |
| `POST` | `/garmin/sync/backfill?days=14` | Backfills Garmin data oldest → today, with short delay for basic rate-limit prevention |
| `GET` | `/garmin/health` | Sidecar health status |

*Notes:*

* Query `date` parameters default to current date.
* Requests for unsynced or in-progress dates return `204 No Content`.
* `GarminDay` is a response/composition type, not a table. Heart, stress, and sleep are stored separately and joined on `Date`.


## Other routes

- `GET /buienradar` — pull Heino station from Buienradar, persist(will eventually be configurable but hardcoded for now since personal application)  
- `POST /api/room-climate` — ESP32 ingest  
- ActivityWatch under `/activity-watch` (needs a local AW server)

## Layout

```text
LifeTracker.slnx             # Solution root & Docker Compose config
├── LifeTracker/             # .NET 10 Web API Backend
│   ├── Endpoints/           # Minimal API Endpoint definitions
│   ├── Entities/            # Database Domain Models
│   ├── Dtos/                # Data Transfer Objects used for mapping API JSON to Entities
│   └── Services/            # Business Logic
├── Bridges/
│   ├── garmin_bridge/       # Python FastAPI bridge/sidecar(using unofficial garminconnect API lib)
│   └── ESP32RoomClimate/    # C++ code for ESP32 with .secrets.example
└── LifeTracker.Tests/       # Tests
```


## Tests

In Solution root
```bash
dotnet test LifeTracker.slnx 
# or
dotnet test 
```

CI runs tests, Persistence tests use EF InMemory, no external API calls made just local tests except for basic buienradar test

## Config / secrets

| What | Where |
|---|---|
| Postgres (dev compose) | `docker-compose.yml` / `ConnectionStrings:DefaultConnection` |
| JWT | `docker-compose.yml`, User secrets or env: `JWT__Key`, `JWT__Password` |
| Garmin login | `Bridges/garmin_bridge/.env` (gitignored). Tokens persist in the `garmin_tokens` volume |

## Acknowledgements & Slight roadmap 

Personal project with very specific integrations such as local room climate sensor, unofficial Garmin smartwatch integrations, it's not a product. No first-party Garmin API. Live sync needs *your* Garmin account and will rate-limit if you hammer it. Without those extras you can still run compose, open Scalar, hit GET routes, and inspect the schema.

Others can use it if they'd like but you'd need to have a garmin smartwatch, ESP32 with climate sensor(SCD40) and local programs installed like ActivityWatch to actually kinda make use of all this. 
The Garmin smartwatch especially is (currently) the core of the whole thing since i find the possible interactions and correlations with the biometrics personally most interesting and relevant but more things will get added over time.

Everything is still HTTP since it's still mostly local development and i haven't setup Azure deployment yet.

Currently no frontend exists yet but it's planned, Vue or React with Grafana dashboards and such.
I've just been using the OpenAPI Scalar UI page to check and test all my routes, and looking in my DB to see whats going on but it's also planned. 

Currently the state of the application has mostly been integrating all these different data sources and not creating any novel data or insights with it yet. But that will all increase over time, the first basic example of actual new data/info created from the gathered API data is the awake window. 

Garmin itself doesn't store or calculate that data, it's not in their API but obviously it can all be inferred just using the sleep start and end times.


Next: awake-window from consecutive sleep ends, tighter ESP32 auth (device key + its own API key), frontend

Unit tests and CI should and need to be more extensive, only added the first ones recently but having the automated Github actions CI is very nice and useful already

