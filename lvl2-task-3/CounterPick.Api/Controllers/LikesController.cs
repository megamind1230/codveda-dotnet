using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Data;
using CounterPick.Core.Models;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("api/comments/{commentId}/like")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly AppDbContext _db;

    public LikesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLike(int commentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment is null) return NotFound();

        var existing = await _db.SuggestionLikes
            .FirstOrDefaultAsync(sl => sl.CommentId == commentId && sl.UserId == userId);

        if (existing is not null)
        {
            _db.SuggestionLikes.Remove(existing);
            comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
        }
        else
        {
            _db.SuggestionLikes.Add(new SuggestionLike
            {
                CommentId = commentId,
                UserId = userId
            });
            comment.LikeCount++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { likeCount = comment.LikeCount });
    }
}
