using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Data;
using CounterPick.Core.Models;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("api/counters/{suggestionId}/comments")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public CommentsController(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetComments(int suggestionId)
    {
        var users = await _userManager.Users.ToListAsync();
        var userMap = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Id);

        var comments = await _db.Comments
            .Where(c => c.CounterSuggestionId == suggestionId)
            .OrderByDescending(c => c.LikeCount)
            .ThenByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.LikeCount,
                c.CreatedAt,
                UserId = c.UserId
            })
            .ToListAsync();

        var result = comments.Select(c => new
        {
            c.Id,
            c.Content,
            c.LikeCount,
            c.CreatedAt,
            UserName = userMap.GetValueOrDefault(c.UserId, c.UserId)
        });

        return Ok(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(int suggestionId, [FromBody] AddCommentDto dto)
    {
        var exists = await _db.CounterSuggestions.AnyAsync(cs => cs.Id == suggestionId);
        if (!exists) return NotFound("Suggestion not found");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var comment = new Comment
        {
            CounterSuggestionId = suggestionId,
            UserId = userId,
            Content = dto.Content
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetComments), new { suggestionId }, comment);
    }
}

public class AddCommentDto
{
    public string Content { get; set; } = string.Empty;
}
