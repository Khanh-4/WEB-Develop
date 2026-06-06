# TechSpecs - E-Commerce & Custom PC Builder

## 1. Project Overview
"TechSpecs" is an advanced e-commerce web application focused on selling PC components and providing a "Custom PC Builder" feature. Unlike basic stores, this system features a robust Compatibility Engine that guides users to build functional PCs using hard hardware constraints and Performance-per-Price (P/P) scoring.

## 2. Tech Stack
- **Frontend Layer:** HTML5, CSS3, JavaScript, Bootstrap, jQuery/AJAX. UI style: Modern Glassmorphism.
- **Backend Layer (API & Business Logic):** ASP.NET Core MVC (C#) using Visual Studio Code.
- **Database & Search Layer:** PostgreSQL hosted on Supabase, utilizing Entity Framework Core (`Npgsql`). Uses `pg_trgm` extension for Fuzzy Search [1].
- **Data Acquisition Layer:** Python (Beautiful Soup, Selenium, SQLAlchemy) for automated web scraping [2].
- **Cloud & AI Services:** 
  - Google Gemini API (with Groq/OpenRouter fallback) for AI Chatbot.
  - Firebase Storage / GCP Cloud Storage for image hosting.
- **Deployment:** Railway for ASP.NET Core app, GitHub Actions for Python cron jobs.

## 3. Core Features
- **Skilled Builder:** Step-by-step PC building interface (CPU -> Mainboard -> RAM -> VGA...). Uses AJAX for dynamic, page-reload-free updates.
- **Beginner Builder (AI Chatbot):** NLP-based builder using Gemini API. Users input budget and use-case; AI returns a fully compatible build.
- **Compatibility Engine:** The core algorithm that prevents incompatible parts from being selected together (e.g., mismatched CPU sockets, insufficient PSU wattage) [3].
- **E-commerce:** Cart, checkout, order history, advanced fuzzy search for products.
- **Auth:** ASP.NET Core Identity with role-based access (Admin/Customer).