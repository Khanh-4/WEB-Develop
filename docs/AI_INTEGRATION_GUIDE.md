# AI Chatbot Integration (Beginner Builder)

## 1. Concept
A feature specifically for non-technical users. The user interacts via a chat interface: *"I have 25 million VND. I need a PC for Graphic Design and Valorant."*

## 2. Implementation Logic
- **Primary AI:** Google Gemini API (Vertex AI / Google AI Studio).
- **Fallback Logic:** Implement an `AIAssistantService` interface in ASP.NET Core. Wrap the API call in a `try-catch` block. If Gemini times out or throws an HTTP 500, gracefully degrade and switch the request to Groq API or OpenRouter API.
- **Prompt Engineering System Prompt:** 
  *"You are an expert PC builder assistant. Extract the budget and use-case from the user's prompt. Formulate a JSON response specifying the desired CPU approximate performance, GPU tier, and RAM capacity. Do not output text, only JSON."*
- **Backend Processing:** The C# backend parses the AI's JSON output and feeds those parameters directly into the **Compatibility Engine (Pass 1 & Pass 2)** to retrieve the actual hardware from the database and return the build to the user.
