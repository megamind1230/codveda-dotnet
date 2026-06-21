using Dapper;
using Microsoft.Data.Sqlite;
using DotaLane.HeroService.Models;

namespace DotaLane.HeroService.Data;

public class HeroRepository
{
    private readonly string _connectionString;

    public HeroRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<Hero>> GetAllAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        return await conn.QueryAsync<Hero>("SELECT * FROM Heroes ORDER BY Name");
    }

    public async Task<Hero?> GetByIdAsync(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Hero>(
            "SELECT * FROM Heroes WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Hero>> GetByLaneAsync(string lane)
    {
        using var conn = new SqliteConnection(_connectionString);
        return await conn.QueryAsync<Hero>(
            "SELECT * FROM Heroes WHERE Lane = @Lane ORDER BY Name",
            new { Lane = lane.ToLower() });
    }
}
