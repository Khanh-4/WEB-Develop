# Compatibility Engine & Filtering Algorithm

The Compatibility Engine is the core backend service of TechSpecs. It uses a **Multi-Pass Refinement** algorithm to filter components [7].

## 1. The 3-Pass Refinement Pipeline
When a user selects a component (or sets preferences), the system executes a 3-pass filtering pipeline [7]:

### Pass 1: Preferences & Soft Matching (Fuzzy Search)
- Retrieves candidates based on user preferences (Brand, Budget, Color).
- Utilizes PostgreSQL `pg_trgm` for fuzzy matching (e.g., typing "Geforze" matches "GeForce") [1].

### Pass 2: Hard Constraints (Rule Enforcement)
Injects strict hardware rules. If a component fails these rules, it is entirely excluded [3].
- **CPU & Mainboard:** `cpu.Socket` MUST EQUAL `motherboard.SocketCompatibility`.
- **Mainboard & RAM:** `memory.Type` MUST MATCH `motherboard.MemoryCompatibility`.
- **Clearance:** `video_card.Length` MUST BE <= `case_enclosure.MaxVGALength`.
- **Power Supply (PSU):** `power_supply.Wattage` MUST BE >= `(cpu.TDP + video_card.TDP) * 1.3`. (30% headroom for motherboard, storage, fans).

### Pass 3: Scoring & Randomization (Selection)
- Components passing the first two phases are scored based on **Performance-per-Price (P/P)**.
- Algorithm adds controlled randomness (stochastic selection) to top-tier components so identical inputs don't always yield the exact same build [7, 8].

## 2. Dynamic Update (AJAX)
Every time the user selects a part in the Frontend, an AJAX request is sent to the API. The Backend runs the pipeline based on the *currently selected components* and returns the filtered lists for the remaining categories.