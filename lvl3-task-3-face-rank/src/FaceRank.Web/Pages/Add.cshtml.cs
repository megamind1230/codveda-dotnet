using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FaceRank.Core.Data;
using FaceRank.Core.Models;
using FaceRank.Web.Services;

namespace FaceRank.Web.Pages;

public class AddModel : PageModel
{
    private readonly FaceRankDbContext _db;
    private readonly BlobStorageService _blob;

    public AddModel(FaceRankDbContext db, BlobStorageService blob)
    {
        _db = db;
        _blob = blob;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        public string Name { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? ContactInfo { get; set; }
        public IFormFile? Avatar { get; set; }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var existing = await _db.People
            .FirstOrDefaultAsync(p => p.Name == Input.Name
                && p.Gender == Input.Gender);

        if (existing != null)
        {
            var label = Input.Gender == "Male" ? "Men" : "Women";
            ModelState.AddModelError(string.Empty,
                $"\"{Input.Name}\" already exists in {label}.");
            return Page();
        }

        string? avatarUrl = null;
        if (Input.Avatar is { Length: > 0 })
        {
            var ext = Path.GetExtension(Input.Avatar.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            using var stream = Input.Avatar.OpenReadStream();
            avatarUrl = await _blob.UploadAsync(fileName, stream);
        }

        var minElo = await _db.People
            .Where(p => p.Gender == Input.Gender)
            .MinAsync(p => (int?)p.EloRating) ?? 1400;

        var person = new Person
        {
            Name = Input.Name,
            Gender = Input.Gender,
            ContactInfo = Input.ContactInfo,
            AvatarUrl = avatarUrl,
            EloRating = Math.Max(minElo - 1, 0)
        };

        _db.People.Add(person);
        await _db.SaveChangesAsync();

        SuccessMessage = $"You're in! Your FaceRank ID is {person.Id}. Start voting!";
        ModelState.Clear();
        return Page();
    }
}
