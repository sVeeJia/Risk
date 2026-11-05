# Risk
INF4001
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**Live deployed demo URL:** https://risk-mnbo.onrender.com

Architecture digram: ![Architecture diagram](<docs/Architecture Diagram/INF4001_ArchDia.drawio.png>)
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
## Run locally
### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22](https://nodejs.org/) (includes npm)
- A MongoDB database. The project was built on a free [MongoDB Atlas](https://www.mongodb.com/atlas) cluster. You need its connection string, and your IP address must be allowed under Atlas → Network Access.
- Optional: a [Mistral AI](https://console.mistral.ai/) API key for AI commentary. Without one the game uses built-in template commentary.

### 1. Get the code

```bash
git clone https://github.com/sVeeJia/Risk.git
cd Risk
```

### 2. Configure the backend secrets

Secrets are never stored in the repo. Locally they live in .NET user secrets:

```bash
dotnet user-secrets set "Mongo:ConnectionString" "<your MongoDB connection string>" --project Risk.Api
dotnet user-secrets set "Jwt:Key" "<a random string, at least 32 characters>" --project Risk.Api

# optional
dotnet user-secrets set "Mistral:ApiKey" "<your Mistral API key>" --project Risk.Api
```

To generate a random JWT key in PowerShell:

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

### 3. Start the backend

```bash
dotnet run --project Risk.Api
```

The API listens on http://localhost:5270. On first start it seeds the 234 countries from the Excel file into the `countries` collection. In Development, Swagger is available at http://localhost:5270/swagger/index.html

### 4. Start the frontend

In a second terminal:

```bash
cd risk-web
npm install
npm run dev
```

Open http://localhost:5173, register an account, and start a game.

In development, Vite proxies `/api` and `/hubs` (including the SignalR WebSocket) to `http://localhost:5270` (see `risk-web/vite.config.js`), so no frontend `.env` file is needed. Start the backend first.

### 5. Run the tests

```bash
dotnet test            # backend: unit, engine and API tests
cd risk-web
npm test               # frontend tests
```

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
## Run with Docker

The Dockerfile at the repo root builds the React app and the API into one image. The API serves the built frontend, so there is only one container and one port.

### Prerequisites

[Docker Desktop](https://www.docker.com/products/docker-desktop/), running.

### 1. Create the environment file

Docker Compose reads its secrets from `docker-compose/.env`. This file is git-ignored. Copy the template and fill it in:

```bash
cd docker-compose
copy .env.example .env     # Windows (use cp on macOS/Linux)
```

```env
MONGO_CONNECTION_STRING=<your MongoDB connection string>
JWT_KEY=<a random string, at least 32 characters>
MISTRAL_API_KEY=            # optional, leave blank for template commentary
```

No quotes around the values. If a value contains a `$`, write it as `$$`.

### 2. Build and start

```bash
docker compose up --build
```

Open http://localhost:8080. Add `-d` to run in the background.

### 3. Useful commands

```bash
docker compose logs -f     # follow the logs
docker compose down        # stop and remove the container
docker compose up --build  # rebuild after changing code
```

Your MongoDB Atlas cluster must allow connections from your IP address (Atlas → Network Access).

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
## About the game

A single-player, Risk-style geopolitics game. You pick a starting country and play against **9 NPC opponents**, each starting from a randomly assigned country. The server is the referee: every dice roll, battle and rule is resolved in the backend. tThe browser only sends your choices.

**Live demo:** https://risk-mnbo.onrender.com
(The free instance sleeps when idle, so the first load can take 30–60 seconds.)

### How to play

1. Register and login, then start a new game by choosing your country. 
2. On your turn you place your reinforcements, attack neighbouring countries, move soldiers, and end your turn.
3. The NPCs then take their turns one after another, and each move is played back so you can follow it.
4. You win by eliminating every NPC. You lose if you are eliminated.

### Battle rules

- Attacker dice = `min(attacker soldiers − 1, defender soldiers)`, and the defender rolls the same number.
- Dice are compared **in order** (first with first, second with second, and so on). The higher die wins, and a tie costs nobody anything.
- Each lost comparison removes one soldier from that side.
- Conquering a country gives the winner a reward of `max(2, defender's base soldiers ÷ 2)`.
- A player with no countries left is eliminated.

### Features
- Register, log in and log out, with a secure cookie session

<details>
<summary>Screenshots</summary>

![Landing page](<docs/Screenshots/Landing page.png>)

![Login](<docs/Screenshots/Login.png>)

![Register](<docs/Screenshots/Register.png>)

![Home](<docs/Screenshots/Home.png>)

</details>

- Choose from 234 countries (seeded from an Excel file) and play a 10-country board against 9 NPCs

<details>
<summary>Screenshots</summary>

![Country selection](<docs/Screenshots/CountrySelection.png>)

![Country filter](<docs/Screenshots/CountryFIlters1.png>)

![Country filter](<docs/Screenshots/CountryFilters2.png>)

</details>

- Interactive map: place reinforcements, attack, roll dice, move and transfer soldiers

<details>
<summary>Screenshots</summary>

![Game start](<docs/Screenshots/StartGame.png>)

![Country filter](<docs/Screenshots/InGamePage.png>)

![Player turn](<docs/Screenshots/PlayerTurn.png>)

![Attack1](<docs/Screenshots/Attack.png>)

![Attack2](<docs/Screenshots/AttackMore.png>)

![Attack rolls](<docs/Screenshots/AttackRolls.png>)

![AI commentary](<docs/Screenshots/AttackRolls_Commentary.png>)

![NPCAttack](<docs/Screenshots/NPCAttack.png>)

![Reinforcements1](<docs/Screenshots/Reinforcemants1.png>)

![Reinforcements2](<docs/Screenshots/Reinforcemant2.png>)

![Move soldiers1](<docs/Screenshots/MoveSoldiers1.png>)

![Move soldiers2](<docs/Screenshots/MoveSoldier2_AnimationPopUp.png>)

![WinGame](<docs/Screenshots/WinGame.png>)

</details>

- Players list with each player's total soldiers and countries owned

<details>
<summary>Screenshots</summary>

![Players List](<docs/Screenshots/PlayersList.png>)

</details>

- Saved games: resume an unfinished game or review a finished one

<details>
<summary>Screenshots</summary>

![Saved games](<docs/Screenshots/SavedgamesContinueCampaign.png>)

![Replay](<docs/Screenshots/History_ReviewGame_replays.png>)

![Continue Campaign1](<docs/Screenshots/ContinueCampaign1.png>)

![Continue Campaign2](<docs/Screenshots/ContinueCampaign2.png>)

</details>

- AI commentary on your dice rolls, does not comment on npcs (Mistral AI, with a template fallback)

<details>
<summary>Screenshots</summary>

![AI commentary](<docs/Screenshots/AttackRolls_Commentary.png>)

</details>

- **Bonus feature:** Share a link and anyone can watch your game live over SignalR, or replay a finished game step by step with speed controls and more.

**Live viewing:**

<details>
<summary>Screenshots</summary>

![Live game1](<docs/Screenshots/LiveGameURLBtn1.png>)

![Live game2](<docs/Screenshots/LiveGameURLBtn2.png>)

![Live game3](<docs/Screenshots/LiveGame3.png>)

![Live game4](<docs/Screenshots/LiveGame4.png>)

![Live game5](<docs/Screenshots/LiveGame5.png>)

![RegisterToPlayForViewers](<docs/Screenshots/RegisterToPlayForViewers.png>)

</details>

**Replay**

<details>
<summary>Screenshots</summary>

![Replay](<docs/Screenshots/History_ReviewGame_replays.png>)

![WinReplay1](<docs/Screenshots/WinReplay1.png>)

![WinReplay2](<docs/Screenshots/WinReplay2.png>)

![WinReplay3](<docs/Screenshots/WinReplay3_X16.png>)

![WinReplay4](<docs/Screenshots/WinReplay4.png>)

![WinReplay5](<docs/Screenshots/WinReplay5.png>)

</details>

### Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | React, Vite, Tailwind CSS |
| Backend | ASP.NET Core Web API (.NET 10, C#) |
| Real time | SignalR (WebSockets) |
| Database | MongoDB Atlas (MongoDB.Driver) |
| AI | Mistral AI (`mistral-small-latest`) |
| Tests | xUnit (backend), Node test runner (frontend) |
| Hosting | Docker container on Render |

### Architecture

![Architecture diagram](docs/architecture.png)

The browser talks only to the API, over HTTPS with an HttpOnly cookie, plus a WebSocket for live viewing. The API is the only component that talks to MongoDB Atlas and to Mistral. In production, one Docker container serves both the API and the built React app, so the frontend and the API share an origin.

| Folder | Contents |
| --- | --- |
| `Risk.Api/` | The ASP.NET Core API: controllers, services, battle engine, NPC strategies, repositories |
| `Risk.Tests/` | xUnit tests: engine, services and API integration tests |
| `risk-web/` | The React frontend |
| `docker-compose/` | Docker Compose file and `.env.example` for running locally in a container |
| `Dockerfile` | Multi-stage build used by Docker Compose and Render |
| `docs/` | Architecture diagram + images|

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
## Design patterns, algorithms and data structures

### Design patterns

| Pattern | What it does in this project |
| --- | --- |
| **Command** | Each player or NPC action is a command object: start attack, roll, stop attack, transfer after conquest, place reinforcements, transfer, end turn, and a whole NPC turn. A command validates itself and returns the events it produced. |
| **State** | The game phase is a state object: **Reinforcing**, **Attacking**, **Conquest transfer** (moving soldiers into a newly conquered country) and **Finished**. The current state decides which actions are allowed, and an illegal action is rejected. |
| **Strategy** | NPC behaviour sits behind an interface. The `Opportunist` strategy looks for the best attack, and other strategies can be added without touching the game logic. |
| **Observer** | Observers implement a game-observer interface and are notified when game events happen, so other parts of the system can react without the game logic knowing about them. |
| **Factory** | A game factory builds a new game: the board, the NPC players and their starting countries and soldiers. |
| **Repository** | All MongoDB access (games, events, users, countries) sits behind interfaces, so services and tests never touch the database directly. |

### Algorithms

- **Seeded random numbers (SplitMix64):** every game has a seed, and each dice roll comes from the seed plus the roll's position. The number of rolls used is saved, so reloading or resuming a game continues the exact same sequence, and a game can be replayed from its seed. The random source is behind an interface, so tests can supply scripted dice.
- **Battle resolution:** dice are paired by position, the higher die wins, ties cost nothing, and conquest rewards follow the rules above.
- **Win probability (dynamic programming):** the chance that an attacker conquers a country is computed over every combination of soldier counts and reused, so each state is calculated once.
- **NPC target selection (priority queue):** the Opportunist strategy ranks possible attacks in a priority queue and picks the most promising one.
- **Fisher–Yates shuffle:** an unbiased shuffle, used when randomly drawing the countries for a new game.
- **Soldier allocation:** distributes the starting soldiers across the countries when a game is created.
- **Turn flow and rules:** turn order and the rules for reinforcements and elimination are kept in the game engine, separate from the controllers.
- **Optimistic concurrency:** each game has a `Version` that increases on every save. A save based on an old version is rejected with 409, so two requests can't overwrite each other.

### Data structures

- **Append-only event log:** every action is stored as an immutable, numbered event. The log is the source of truth for replay, live viewing and AI commentary.
- **Priority queue:** used for NPC attack choices.

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
## Configuration

Settings come from environment variables (or .NET user secrets locally). Nothing secret is stored in the repo.

| Setting | Purpose |
| --- | --- |
| `Mongo__ConnectionString` | MongoDB connection string (required) |
| `Jwt__Key` | Secret used to sign login tokens, at least 32 characters (required) |
| `Mistral__ApiKey` | Mistral AI key. Optional: without it the game uses template commentary |
| `Jwt__ExpiryMinutes` | Login lifetime, 120 minutes by default |
| `Cors__AllowedOrigins` | Allowed browser origins (for local development) |

## Security

| Area | What is implemented |
| --- | --- |
| Passwords | Hashed with BCrypt (work factor 12). Login does the same hash work for unknown emails, so response times don't reveal which emails exist. |
| Sessions | JWT in an HttpOnly cookie (Secure, SameSite=Strict), so JavaScript can't read it. It lasts 120 minutes. |
| Authorization | Every game endpoint requires a login and checks that you own the game. Someone else's game returns 404. The shared watch link is public and read-only. |
| Rate limiting | Login and register: 10 per minute per IP. Game actions: 120 per minute per user. Public watch: 120 per minute per IP. Over the limit returns 429. |
| Security headers | nosniff, frame deny, no referrer, Permissions-Policy and a Content-Security-Policy. |
| Validation | All input is validated on the server (email, password length, display name, country code, soldier amounts, page limits). Bad input returns 400. |
| Secrets | Environment variables only. `.env` files are git-ignored, and a scan of the git history found no secrets. |
| Transport | HTTPS in production (certificate and redirect from Render). TLS to Atlas and Mistral. |
| Data sent to Mistral | Only country names and numbers. No user data. |

### Anti-cheating

The server is authoritative. The browser sends only choices, and the server rolls the dice, applies the rules and stores the result. A client can't send dice results, soldier counts or owners. Dice come from a stored seed, every action is recorded in an append-only event log, and a stale or duplicate save is rejected with 409.

### Known limitations

- There is no account lockout. Login brute force is limited by rate limiting only.
- Registering with an email that's already taken reveals that the email exists.
- Logging out clears the cookie, but the token isn't revoked and stays valid until it expires.
- The live-viewing hub has no per-connection limit.
- `npm audit` reports moderate findings in development tooling only. The production dependencies are clean.

## Testing

```bash
dotnet test              # backend
cd risk-web && npm test  # frontend
```

- **Backend (xUnit, 336 tests):** engine rules (dice, battles, rewards, conquest), seeded dice resume, NPC strategy, game flow, and 38 API integration tests that run the real API in memory (login, cookies, ownership, validation, rate limits, security headers).
- **Frontend (135 tests):** API client, dice and battle display, map and Players list logic, replay and live-viewing logic, commentary linking.

## Deployment

The app is deployed on **Render** as one Docker container built from the root `Dockerfile`. The container serves both the API and the built React app. The database is **MongoDB Atlas**.

- CI (GitHub Actions) runs on every push to `main` and on pull requests: it builds and tests the backend, and lints and builds the frontend.
- A push to `main` deploys to Render only after CI passes.
- Environment variables are set in the Render dashboard.
- Live URL: https://risk-mnbo.onrender.com
- Health check: `/health`


## API overview

The API serves everything under `/api`, plus a SignalR hub for live viewing.

| Area | Purpose | Login needed |
| --- | --- | --- |
| Auth | Register and log in (no login needed); log out / current user (login needed) | Mixed |
| Games | Start a game, list and open your games | Yes |
| Game actions | Reinforce, attack, roll, move, transfer, end turn, NPC turns | Yes |
| Public games | Read-only game and event history for watching and replay | No |
| Countries | List, search, filter and sort countries | Yes |
| `/hubs/game` | SignalR hub for live viewing | No |
| `/health` | Health check | No |

--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
References:
GeeksforGeeks. 2025. Shuffle a given array using Fisher–yates shuffle algorithm. [Online]. Available at: https://www.geeksforgeeks.org/dsa/shuffle-a-given-array-using-fisher-yates-shuffle-algorithm/ [Accessed 26 October 2026].
worldometer. 2025. Countries in the world by population (2025). [Online]. Available at: https://www.worldometers.info/world-population/population-by-country/ [Accessed 20 October 2026].













