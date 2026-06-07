using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;

namespace TechSpecs.Controllers;

public class ReviewsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;

    public ReviewsController(AppDbContext db, UserManager<ApplicationUser> users, IConfiguration config)
    {
        _db = db;
        _users = users;
        _config = config;
    }

    // POST /Reviews/UploadPhoto — upload review image to Supabase Storage
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto(IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return BadRequest(new { error = "No file" });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowed.Contains(photo.ContentType.ToLower()))
            return BadRequest(new { error = "Chỉ chấp nhận ảnh JPEG/PNG/WebP/GIF" });

        if (photo.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Ảnh tối đa 5 MB" });

        var supabaseUrl   = _config["Supabase:Url"];
        var supabaseKey   = _config["Supabase:AnonKey"];

        if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            return BadRequest(new { error = "Storage chưa được cấu hình." });

        var ext      = Path.GetExtension(photo.FileName).ToLower().TrimStart('.');
        var fileName = $"reviews/{Guid.NewGuid():N}.{ext}";
        var uploadUrl = $"{supabaseUrl}/storage/v1/object/reviews/{fileName}";

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
        http.DefaultRequestHeaders.Add("apikey", supabaseKey);

        using var stream  = photo.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(photo.ContentType);

        var resp = await http.PostAsync(uploadUrl, content);
        if (!resp.IsSuccessStatusCode)
            return StatusCode(500, new { error = "Upload thất bại" });

        var publicUrl = $"{supabaseUrl}/storage/v1/object/public/reviews/{fileName}";
        return Ok(new { url = publicUrl });
    }

    // GET /Reviews/ForProduct?category=cpu&id=123
    [HttpGet]
    public async Task<IActionResult> ForProduct(string category, int id)
    {
        var reviews = await _db.ProductReviews
            .AsNoTracking()
            .Where(r => r.Category == category && r.ComponentId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id, r.Rating, r.Comment, r.UserDisplayName, r.ImageUrl,
                CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy")
            })
            .ToListAsync();

        var avg = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;

        string? myReviewUserId = null;
        if (User.Identity?.IsAuthenticated == true)
            myReviewUserId = _users.GetUserId(User);

        bool hasReviewed = myReviewUserId != null && await _db.ProductReviews
            .AnyAsync(r => r.UserId == myReviewUserId && r.Category == category && r.ComponentId == id);

        return Json(new { reviews, avg = Math.Round(avg, 1), count = reviews.Count, hasReviewed });
    }

    // POST /Reviews/Submit
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([FromBody] SubmitReviewRequest req)
    {
        if (req.Rating < 1 || req.Rating > 5)
            return BadRequest(new { error = "Rating must be 1–5" });

        var userId = _users.GetUserId(User)!;
        var displayName = User.FindFirst("FullName")?.Value
                       ?? User.Identity!.Name?.Split('@')[0]
                       ?? "Ẩn danh";

        var existing = await _db.ProductReviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Category == req.Category && r.ComponentId == req.ComponentId);

        if (existing != null)
        {
            existing.Rating   = req.Rating;
            existing.Comment  = req.Comment?.Trim();
            if (!string.IsNullOrEmpty(req.ImageUrl)) existing.ImageUrl = req.ImageUrl;
        }
        else
        {
            _db.ProductReviews.Add(new ProductReview
            {
                UserId = userId, Category = req.Category, ComponentId = req.ComponentId,
                Rating = req.Rating, Comment = req.Comment?.Trim(),
                ImageUrl = string.IsNullOrEmpty(req.ImageUrl) ? null : req.ImageUrl,
                UserDisplayName = displayName
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // GET /Reviews/Questions?category=cpu&id=123
    [HttpGet]
    public async Task<IActionResult> Questions(string category, int id)
    {
        var questions = await _db.ProductQuestions
            .AsNoTracking()
            .Where(q => q.Category == category && q.ComponentId == id)
            .OrderByDescending(q => q.CreatedAt)
            .Include(q => q.Answers.OrderBy(a => a.CreatedAt))
            .Select(q => new
            {
                q.Id, q.Question, q.UserDisplayName,
                CreatedAt = q.CreatedAt.ToString("dd/MM/yyyy"),
                Answers = q.Answers.Select(a => new
                {
                    a.Answer, a.UserDisplayName,
                    CreatedAt = a.CreatedAt.ToString("dd/MM/yyyy")
                }).ToList()
            })
            .ToListAsync();

        return Json(questions);
    }

    // POST /Reviews/PostQuestion
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> PostQuestion([FromBody] PostQuestionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Question) || req.Question.Length > 500)
            return BadRequest();

        var userId = _users.GetUserId(User)!;
        var displayName = User.FindFirst("FullName")?.Value
                       ?? User.Identity!.Name?.Split('@')[0]
                       ?? "Ẩn danh";

        _db.ProductQuestions.Add(new ProductQuestion
        {
            UserId = userId, Category = req.Category, ComponentId = req.ComponentId,
            Question = req.Question.Trim(), UserDisplayName = displayName
        });
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // POST /Reviews/PostAnswer
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> PostAnswer([FromBody] PostAnswerRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Answer) || req.Answer.Length > 1000)
            return BadRequest();

        var question = await _db.ProductQuestions.FindAsync(req.QuestionId);
        if (question == null) return NotFound();

        var userId = _users.GetUserId(User)!;
        var displayName = User.FindFirst("FullName")?.Value
                       ?? User.Identity!.Name?.Split('@')[0]
                       ?? "Ẩn danh";

        _db.ProductAnswers.Add(new ProductAnswer
        {
            QuestionId = req.QuestionId, UserId = userId,
            Answer = req.Answer.Trim(), UserDisplayName = displayName
        });
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}

public record SubmitReviewRequest(string Category, int ComponentId, int Rating, string? Comment, string? ImageUrl);
public record PostQuestionRequest(string Category, int ComponentId, string Question);
public record PostAnswerRequest(int QuestionId, string Answer);
