using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using CounterPick.Core.Models;

namespace CounterPick.Api.Authorization;

public class CommentOwnerHandler : AuthorizationHandler<CommentOwnerRequirement, Comment>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CommentOwnerRequirement requirement,
        Comment resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == resource.UserId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
