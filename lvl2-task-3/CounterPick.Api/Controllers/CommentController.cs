using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Constants;
using CounterPick.Core.Data;
using CounterPick.Core.Models;

namespace CounterPick.Api.Controllers;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAuthorizationService _auth;

    public CommentController(AppDbContext db, IAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentDto dto)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment is null) return NotFound();

        var authResult = await _auth.AuthorizeAsync(User, comment, AppPolicies.OwnsComment);
        if (!authResult.Succeeded)
            return Forbid();

        comment.Content = dto.Content;
        await _db.SaveChangesAsync();

        return Ok(new { comment.Id, comment.Content, comment.LikeCount });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment is null) return NotFound();

        var authResult = await _auth.AuthorizeAsync(User, comment, AppPolicies.OwnsComment);
        if (!authResult.Succeeded)
            return Forbid();

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}
