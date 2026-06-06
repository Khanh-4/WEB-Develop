# Data Acquisition Pipeline (Python)

The system relies on a decoupled Python scraping architecture to gather up-to-date component specs and prices [2].

## 1. Technologies
- **Python:** The main scraping engine.
- **Beautiful Soup 4:** To parse static HTML elements [2].
- **Selenium:** To handle dynamic, JavaScript-rendered pagination on tech retail sites [2].
- **SQLAlchemy:** As the ORM to map scraped data directly to PostgreSQL tables [2].

## 2. Scraping Workflow
1. **Extraction:** Navigate to retail sites (e.g., Phong Vũ, An Phát) [2, 9, 10]. Extract raw HTML.
2. **Normalization:** Clean the data. 
   - Convert units to canonical forms (e.g., "8GB", "8gb", "8192 MB" all become integer `8`) [11, 12].
   - Extract numeric values into `_parsed` secondary fields for easy backend range filtering [12].
3. **Derived Metrics Generation:** Calculate `ApproximatePerformance` heuristic scores for CPUs and GPUs [6].
4. **Insertion:** Use SQLAlchemy to push the normalized objects to the Supabase PostgreSQL database [2].

## 3. Automation
The script is containerized (Docker) and scheduled via GitHub Actions (Cron Job) to run every 12 hours, ensuring the database stays updated without affecting the stateless ASP.NET Core Web Servers.