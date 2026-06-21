using System.Net.Http.Json;
using DotaLane.Frontend.Models;

namespace DotaLane.Frontend.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<HeroDto>> GetHeroesAsync()
    {
        var result = await _http.GetFromJsonAsync<List<HeroDto>>("/api/heroes");
        return result ?? new List<HeroDto>();
    }

    public async Task<MatchupResponse?> GetMatchupAsync(int yourHeroId, int enemyHeroId, string lane)
    {
        var url = $"/api/matchup/{yourHeroId}/{enemyHeroId}?lane={lane}";
        return await _http.GetFromJsonAsync<MatchupResponse>(url);
    }
}
