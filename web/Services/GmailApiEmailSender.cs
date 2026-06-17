using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TechSpecs.Services;

/// <summary>
/// Sends email through the Gmail API using an OAuth2 refresh token, so the app
/// can deliver to ANY recipient (unlike Resend's sandbox, which is limited to
/// the account owner until a domain is verified). No domain required.
///
/// Required config (appsettings.Development.json / env):
///   "Email":  { "Provider": "Gmail" }
///   "Gmail":  { "ClientId": "...", "ClientSecret": "...",
///               "RefreshToken": "...", "Sender": "you@gmail.com" }
///
/// Validation is lazy (in SendEmailAsync, not the constructor) so a missing
/// credential surfaces as a caught/logged send failure rather than a startup
/// crash on every page that injects IEmailSender.
/// </summary>
public class GmailApiEmailSender : IEmailSender
{
    private const string TokenUrl = "https://oauth2.googleapis.com/token";
    private const string SendUrl  = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<GmailApiEmailSender> _logger;

    public GmailApiEmailSender(IHttpClientFactory factory, IConfiguration config,
        ILogger<GmailApiEmailSender> logger)
    {
        _factory = factory;
        _config  = config;
        _logger  = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var clientId     = Require("Gmail:ClientId");
        var clientSecret = Require("Gmail:ClientSecret");
        var refreshToken = Require("Gmail:RefreshToken");
        var sender       = _config["Gmail:Sender"] ?? "TechSpecs";

        var http        = _factory.CreateClient();
        var accessToken = await GetAccessTokenAsync(http, clientId, clientSecret, refreshToken);
        var raw         = BuildRawMessage(sender, to, subject, htmlBody);

        using var req = new HttpRequestMessage(HttpMethod.Post, SendUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { raw }), Encoding.UTF8, "application/json");

        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Gmail API error {resp.StatusCode}: {body}");
        }
    }

    private string Require(string key) =>
        _config[key] ?? throw new InvalidOperationException($"{key} missing");

    private static async Task<string> GetAccessTokenAsync(
        HttpClient http, string clientId, string clientSecret, string refreshToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"]     = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"]    = "refresh_token",
            }),
        };
        var resp = await http.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Gmail token refresh failed {resp.StatusCode}: {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new Exception("Gmail token response missing access_token");
    }

    // RFC 2822 message, base64url-encoded as the Gmail API expects.
    private static string BuildRawMessage(string sender, string to, string subject, string html)
    {
        var encodedSubject = "=?UTF-8?B?" +
            Convert.ToBase64String(Encoding.UTF8.GetBytes(subject)) + "?=";
        var mime =
            $"From: {sender}\r\n" +
            $"To: {to}\r\n" +
            $"Subject: {encodedSubject}\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: text/html; charset=UTF-8\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n\r\n" +
            html;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(mime))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
