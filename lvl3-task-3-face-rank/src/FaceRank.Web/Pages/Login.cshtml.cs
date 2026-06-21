using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;

namespace FaceRank.Web.Pages;

public class LoginModel : PageModel
{
    private readonly FaceRankDbContext _db;

    public LoginModel(FaceRankDbContext db) => _db = db;

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
        if (HttpContext.Session.GetString("LoggedInUser") != null)
        {
            SuccessMessage = $"Already logged in as {HttpContext.Session.GetString("LoggedInUser")}.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return Page();
        }

        var trimmed = Name.Trim();

        if (HttpContext.Session.GetString("LoggedInUser") != null)
        {
            SuccessMessage = $"Already logged in as {HttpContext.Session.GetString("LoggedInUser")}.";
            return Page();
        }

        var person = await _db.People.FirstOrDefaultAsync(p => p.Name == trimmed);
        if (person == null)
        {
            ErrorMessage = $"No user named \"{trimmed}\" found. <a href=\"/Add\">Add yourself first!</a>";
            return Page();
        }

        HttpContext.Session.SetString("LoggedInUser", person.Name);
        HttpContext.Session.SetInt32("LoggedInUserId", person.Id);

        SuccessMessage = $"Welcome back, {person.Name}!";
        return Page();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("LoggedInUser");
        HttpContext.Session.Remove("LoggedInUserId");
        return RedirectToPage("/Index");
    }
}
