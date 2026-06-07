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

    public ReviewsController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
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
                r.Id, r.Rating, r.Comment, r.UserDisplayName,
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
            existing.Rating  = req.Rating;
            existing.Comment = req.Comment?.Trim();
        }
        else
        {
            _db.ProductReviews.Add(new ProductReview
            {
                UserId = userId, Category = req.Category, ComponentId = req.ComponentId,
                Rating = req.Rating, Comment = req.Comment?.Trim(),
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

public record SubmitReviewRequest(string Category, int ComponentId, int Rating, string? Comment);
public record PostQuestionRequest(string Category, int ComponentId, string Question);
public record PostAnswerRequest(int QuestionId, string Answer);
