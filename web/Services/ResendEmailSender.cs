using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TechSpecs.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}

public class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _from;

    public ResendEmailSender(IHttpClientFactory factory, IConfiguration config)
    {
        _http   = factory.CreateClient();
        _apiKey = config["Resend:ApiKey"] ?? throw new InvalidOperationException("Resend:ApiKey missing");
        _from   = config["Resend:From"] ?? "TechSpecs <onboarding@resend.dev>";
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var payload = JsonSerializer.Serialize(new
        {
            from    = _from,
            to      = new[] { to },
            subject = subject,
            html    = htmlBody,
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Resend API error {resp.StatusCode}: {body}");
        }
    }
}
