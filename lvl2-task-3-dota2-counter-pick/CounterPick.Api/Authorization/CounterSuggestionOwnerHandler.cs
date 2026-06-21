using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CounterPick.Core.Models;

namespace CounterPick.Api.Authorization;

//#baka resource-based auth: succeeds if the current user either owns the suggestion OR is an Admin
//#baka context.Succeed() means "this handler approves the request"; if no handler Succeeds, auth fails with 403
public class CounterSuggestionOwnerHandler : AuthorizationHandler<CounterSuggestionOwnerRequirement, CounterSuggestion>
{
    private readonly UserManager<IdentityUser> _userManager;

    public CounterSuggestionOwnerHandler(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CounterSuggestionOwnerRequirement requirement,
        CounterSuggestion resource)
    {
        //#baka check 1: is the JWT's nameidentifier the same as the suggestion's author?
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == resource.SuggestedById)
        {
            context.Succeed(requirement);
            return;
        }

        //#baka check 2: is the user an Admin? if yes, they can edit/delete any suggestion
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is not null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                context.Succeed(requirement);
            }
        }
    }
}
