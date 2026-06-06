using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TechSpecs.Services;

public class AIAssistantService : IAIAssistantService
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AIAssistantService> _log;

    private static readonly string SystemPrompt = """
        You are an expert PC builder assistant for a Vietnamese e-commerce site.
        Extract the budget (in VND) and use-case from the user's message.
        Return ONLY a JSON object — no text, no markdown, no explanation.
        JSON schema:
        {
          "minCpuPerformance": <int 0-150, higher = stronger CPU>,
          "minGpuPerformance": <int 0-999, higher = stronger GPU. 0 if no GPU needed>,
          "minRamGb": <int, minimum RAM in GB>,
          "maxBudget": <number in VND>,
          "useCase": "<gaming|office|design|streaming|general>"
        }
        Use-case guide:
        - gaming/fps → high GPU (400+), mid CPU (70+)
        - design/3D/render → high CPU (100+), high GPU (400+), 32GB+ RAM
        - office/study → low CPU (30+), no GPU (0), 8-16GB RAM
        - streaming → mid CPU (70+), mid GPU (300+)
        - general → balanced mid-range
        Budget examples: "15 triệu" = 15000000, "20 củ" = 20000000, "$500" ≈ 12500000
        """;

    public AIAssistantService(IHttpClientFactory http, IConfiguration config, ILogger<AIAssistantService> log)
    {
        _http = http; _config = config; _log = log;
    }

    public async Task<AiBuildParams?> ParseBuildRequestAsync(string userMessage)
    {
        return await TryGeminiAsync(userMessage)
            ?? await TryGroqAsync(userMessage)
            ?? await TryOpenRouterAsync(userMessage);
    }

    // ── Gemini ────────────────────────────────────────────────────────────────

    private async Task<AiBuildParams?> TryGeminiAsync(string message)
    {
        var apiKey = _config["AI:GeminiApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return null;

        try
        {
            var client = _http.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

            var body = JsonSerializer.Serialize(new
            {
                systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents = new[] { new { parts = new[] { new { text = message } } } },
                generationConfig = new { temperature = 0.2, maxOutputTokens = 256 }
            });

            var resp = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var text = json?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
            return ParseJson(text);
        }
        catch (Exception ex)
        {
            _log.LogWarning("Gemini failed: {Msg} — falling back to Groq", ex.Message);
            return null;
        }
    }

    // ── Groq (OpenAI-compatible) ───────────────────────────────────────────────

    private async Task<AiBuildParams?> TryGroqAsync(string message)
    {
        var apiKey = _config["AI:GroqApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _log.LogError("Both Gemini and Groq API keys are missing");
            return null;
        }

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var body = JsonSerializer.Serialize(new
            {
                model = "llama-3.1-8b-instant",
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user",   content = message }
                },
                temperature = 0.2,
                max_tokens = 256,
                response_format = new { type = "json_object" }
            });

            var resp = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions",
                new StringContent(body, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var text = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            return ParseJson(text);
        }
        catch (Exception ex)
        {
            _log.LogError("Groq also failed: {Msg}", ex.Message);
            return null;
        }
    }

    // ── OpenRouter (OpenAI-compatible, many models) ───────────────────────────

    private async Task<AiBuildParams?> TryOpenRouterAsync(string message)
    {
        var apiKey = _config["AI:OpenRouterApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _log.LogError("All AI providers failed — no OpenRouter key configured");
            return null;
        }

        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://techspecs.app");
            client.DefaultRequestHeaders.Add("X-Title", "TechSpecs PC Builder");

            var body = JsonSerializer.Serialize(new
            {
                model = "meta-llama/llama-3.1-8b-instruct:free",
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user",   content = message }
                },
                temperature = 0.2,
                max_tokens = 256,
                response_format = new { type = "json_object" }
            });

            var resp = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions",
                new StringContent(body, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var text = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
            return ParseJson(text);
        }
        catch (Exception ex)
        {
            _log.LogError("OpenRouter also failed: {Msg}", ex.Message);
            return null;
        }
    }

    // ── Parse AI JSON output ──────────────────────────────────────────────────

    private static AiBuildParams? ParseJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            // Strip markdown code fences if model added them
            raw = raw.Trim().TrimStart('`');
            if (raw.StartsWith("json")) raw = raw[4..];
            raw = raw.TrimEnd('`').Trim();

            var node = JsonNode.Parse(raw);
            if (node is null) return null;

            return new AiBuildParams(
                MinCpuPerformance: node["minCpuPerformance"]?.GetValue<int>() ?? 0,
                MinGpuPerformance: node["minGpuPerformance"]?.GetValue<int>() ?? 0,
                MinRamGb:          node["minRamGb"]?.GetValue<int>() ?? 8,
                MaxBudget:         node["maxBudget"]?.GetValue<decimal>() ?? 0,
                UseCase:           node["useCase"]?.GetValue<string>() ?? "general"
            );
        }
        catch { return null; }
    }
}
