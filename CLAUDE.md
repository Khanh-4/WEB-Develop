# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**TechSpecs** — Vietnamese e-commerce site for PC components with a custom PC Builder feature. Two-person team: `/web` (ASP.NET Core, C#) and `/scraper` (Python data pipeline) are separate ownership areas.

---

## Commands

### Web App (`/web`)

```bash
# Run dev server → http://localhost:5003
dotnet run --launch-profile http

# If port 5003 is already in use (kill the old process first):
fuser -k 5003/tcp && dotnet run --launch-profile http

# Build only (no run)
dotnet build

# EF Core migrations (always use the LOCAL tool, not the global one)
dotnet dotnet-ef migrations add <MigrationName> --output-dir Data/Migrations
dotnet dotnet-ef database update
dotnet dotnet-ef migrations remove   # undo last unapplied migration
```

> `appsettings.Development.json` is gitignored and holds the real connection string, Google OAuth keys, and AI API keys. Never commit it.

### Scraper (`/scraper`)

```bash
cd scraper
source venv/bin/activate

python main.py                    # scrape all 8 categories (~25 min)
python main.py cpu gpu ram        # scrape specific categories only
python main.py cpu                # single category test (~5 min)
```

Valid category names: `cpu gpu ram motherboard psu case storage cooler`

---

## Architecture

### Web App — ASP.NET Core MVC (.NET 8)

**Service layer** (the important logic lives here, not in controllers):

| Service | File | Purpose |
|---------|------|---------|
| `CompatibilityEngine` | `Services/CompatibilityEngine.cs` | Core 3-pass filter — called on every AJAX selection in Builder |
| `AIAssistantService` | `Services/AIAssistantService.cs` | Gemini → Groq → OpenRouter fallback chain; parses natural language → `AiBuildParams` JSON |

**Request flows to understand:**

1. **PC Builder AJAX** — Frontend sends `POST /Builder/Filter` with a `BuildState` JSON body on every component selection. `CompatibilityEngine.FilterAsync()` runs all 3 passes and returns `FilteredResult` (8 lists of `ComponentDto` + totals). The JS in `Builder/Index.cshtml` re-renders the product grid for the active tab only.

2. **AI Chatbot** — `POST /Chat/Ask` with a plain text message → `AIAssistantService` calls the LLM and parses the JSON response into `AiBuildParams` → `ChatController` distributes the budget across component categories (see `GetBudgetAllocation`) → calls `CompatibilityEngine` with no per-component cap → `PickBest()` selects one item per category within the allocated slice. The chat widget stores the result in `sessionStorage['aiBuildPreset']` and redirects to `/Builder`, which reads and applies the preset on load.

3. **Products Filter AJAX** — `GET /Products/Filter?category=...&sort=...&page=...` → `ProductsController.LoadAllAsync()` queries all 8 hardware tables and merges into `List<ProductListItem>`. Default sort is `name` which uses `Interleave()` (round-robin 3 items/category/page) so the "all" view shows variety.

**Compatibility Engine — 3-pass pipeline:**

- **Pass 1** (every call): `ILike` name search, budget cap, brand filter via `ApplyPass1<T>()`
- **Pass 2** (only when related component is already selected): hard constraints — CPU↔MB socket, RAM↔MB type, GPU length ≤ case MaxVGALength, MB form factor ∈ case FormFactorSupport, PSU wattage ≥ (CPU.TDP + GPU.TDP) × 1.3, cooler socket + MaxTDP
- **Pass 3**: score by `ApproximatePerformance / Price × 1,000,000`, shuffle top 5 for stochastic variety, return max 30 items

**Database — PostgreSQL on Supabase:**

`AppDbContext` extends `IdentityDbContext<ApplicationUser>`. 8 hardware tables use lowercase snake_case names via `[Table("cpu")]` etc. EF Core uses the **Session Pooler** connection (port 5432, not the direct connection which resolves to IPv6 on WSL2). The `dotnet-ef` local tool v8 is in `.config/dotnet-tools.json` — always use `dotnet dotnet-ef` not the global `dotnet-ef` to avoid version mismatch.

**Frontend design system:**

All glassmorphism utilities are in `wwwroot/css/site.css`. Key classes: `.glass`, `.glass-sm`, `.btn-gradient`, `.category-pill`, `.component-card`, `.build-step`. The `_ChatWidget.cshtml` partial is included in `_Layout.cshtml` and renders inline CSS/JS (not a `@section Scripts` because partials don't support sections).

---

### Scraper (`/scraper`) — Python 3.11

Phong Vũ pages are **static HTML** — no Selenium/Chrome needed. Pagination: `?page=N`, stops when `div.product-card` count = 0. Price selector: `.att-product-detail-retail-price`. The scraper fetches each product's detail page to extract specs; 0.3s delay per request to avoid rate-limiting.

Upsert logic: skips products where `Name` already exists in the table (no update on re-run, only inserts new ones).

**Performance scoring** (`scoring/performance.py`):
- CPU: `(cores^0.75) × boost_clock × 10` — boost clock is extracted from product name if not in specs (pattern: "Boost tối đa 4.4 GHz")
- GPU: tier lookup table keyed by model substring (longest match first), then VRAM bonus

---

## Key Configuration

`appsettings.Development.json` (gitignored) must contain:

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=...pooler.supabase.com;Port=5432;..." },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." }
  },
  "AI": {
    "GeminiApiKey": "...",
    "GroqApiKey": "...",
    "OpenRouterApiKey": "..."
  }
}
```

`scraper/.env` (gitignored):
```
DATABASE_URL=postgresql+psycopg2://postgres.[ref]:[password]@...pooler.supabase.com:5432/postgres
```

---

## What's Not Built Yet

Cart, Orders, and Admin Dashboard are the next priorities — see `Tasks/TODO.md`. `AppDbContext` already has commented-out `DbSet` placeholders for `Order`, `OrderDetail`, `Cart`.

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **WEB-Develop** (48 symbols, 46 relationships, 0 execution flows). Use the GitNexus MCP tools to understand code, assess impact, and navigate safely.

> If any GitNexus tool warns the index is stale, run `npx gitnexus analyze` in terminal first.

## Always Do

- **MUST run impact analysis before editing any symbol.** Before modifying a function, class, or method, run `gitnexus_impact({target: "symbolName", direction: "upstream"})` and report the blast radius (direct callers, affected processes, risk level) to the user.
- **MUST run `gitnexus_detect_changes()` before committing** to verify your changes only affect expected symbols and execution flows.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- When exploring unfamiliar code, use `gitnexus_query({query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `gitnexus_context({name: "symbolName"})`.

## Never Do

- NEVER edit a function, class, or method without first running `gitnexus_impact` on it.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis.
- NEVER rename symbols with find-and-replace — use `gitnexus_rename` which understands the call graph.
- NEVER commit changes without running `gitnexus_detect_changes()` to check affected scope.

## Resources

| Resource | Use for |
|----------|---------|
| `gitnexus://repo/WEB-Develop/context` | Codebase overview, check index freshness |
| `gitnexus://repo/WEB-Develop/clusters` | All functional areas |
| `gitnexus://repo/WEB-Develop/processes` | All execution flows |
| `gitnexus://repo/WEB-Develop/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
|------|---------------------|
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
