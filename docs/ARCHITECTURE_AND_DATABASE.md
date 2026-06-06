# System Architecture & Database Schema

## 1. Architectural Layers
The system follows a Decoupled / Hybrid Architecture:
- **Presentation Layer:** Views in ASP.NET Core MVC, heavily utilizing AJAX to interact with backend endpoints without reloading.
- **Application Layer (Stateless):** Controllers and Services in C#. Includes an `AIAssistantService` with try-catch fallback logic (Google Gemini -> Groq/OpenRouter).
- **Data Layer:** PostgreSQL (Supabase). Functions as both the primary ACID-compliant DB and the Search Engine (using `pg_trgm` extension) [1, 4].

## 2. Database Schema (Entity Framework Core - Code First)
To run the compatibility algorithm efficiently, hardware components are separated into distinct tables rather than a single massive table [5].

### Core Tables:
- `Users`, `Orders`, `OrderDetails`, `Cart`.
- `cpu`: `Id`, `Name`, `Price`, `Manufacturer`, `CoreCount`, `BaseClock`, `Socket`, `TDP`, `ApproximatePerformance` (Derived field) [5, 6].
- `motherboard`: `Id`, `Name`, `Price`, `SocketCompatibility` (Matches CPU), `FormFactor`, `MemoryCompatibility` (Matches RAM Type).
- `memory`: `Id`, `Name`, `Price`, `Type` (DDR4/DDR5), `Capacity`, `Speed`.
- `video_card` (GPU): `Id`, `Name`, `Price`, `VRAM`, `Length`, `TDP`, `ApproximatePerformance`.
- `power_supply` (PSU): `Id`, `Name`, `Price`, `Wattage`, `Efficiency`.
- `case_enclosure`: `Id`, `Name`, `Price`, `FormFactorSupport`, `MaxVGALength`.

### Derived & Computed Fields:
For accurate P/P scoring, the DB includes computed fields:
- `ApproximatePerformance` (For CPU/GPU): A heuristic score based on cores, clocks, and cache to calculate Performance-per-Price [6].