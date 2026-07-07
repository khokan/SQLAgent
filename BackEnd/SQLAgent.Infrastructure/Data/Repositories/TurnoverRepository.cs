using Microsoft.EntityFrameworkCore;
using SQLAgent.Core.Models;

namespace SQLAgent.Infrastructure.Data.Repositories;

public interface ITurnoverRepository : IRepository<Turnover>
{
    Task<IEnumerable<Turnover>> GetByCompanyIdAsync(int companyId);
    Task<IEnumerable<Turnover>> GetByYearAsync(int year);
    Task<Dictionary<int, decimal>> GetYearlyTurnoverAsync(int companyId);
    Task<object> GetTurnoverByYearAndQuarterAsync(int companyId, int year, int quarter);
    Task<object> ExecuteRawQueryAsync(string sql);
}

public class TurnoverRepository : Repository<Turnover>, ITurnoverRepository
{
    private readonly ApplicationDbContext _context;

    public TurnoverRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Turnover>> GetByCompanyIdAsync(int companyId)
    {
        return await _context.Turnovers
            .Where(t => t.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Turnover>> GetByYearAsync(int year)
    {
        return await _context.Turnovers
            .Where(t => t.Year == year)
            .ToListAsync();
    }

    public async Task<Dictionary<int, decimal>> GetYearlyTurnoverAsync(int companyId)
    {
        return await _context.Turnovers
            .Where(t => t.CompanyId == companyId)
            .GroupBy(t => t.Year)
            .Select(g => new { Year = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.Year, x => x.Total);
    }

    public async Task<object> GetTurnoverByYearAndQuarterAsync(int companyId, int year, int quarter)
    {
        return await _context.Turnovers
            .Where(t => t.CompanyId == companyId && t.Year == year && t.Quarter == quarter)
            .Select(t => new { t.Id, t.Amount, t.Year, t.Quarter, t.Currency })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Execute raw SQL query and return results as list of dictionaries
    /// </summary>
    /// <param name="sql">Raw SQL query string (should be properly validated to prevent SQL injection)</param>
    /// <returns>List of dictionaries where each dictionary represents a row with column names as keys</returns>
    /// <remarks>
    /// Supports complex queries with JOINs, GROUP BY, and ORDER BY clauses.
    /// Uses ADO.NET directly to handle any SQL query structure.
    /// WARNING: Ensure SQL input is validated/trusted before calling this method.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when SQL execution fails</exception>
    public async Task<object> ExecuteRawQueryAsync(string sql)
    {
        try
        {
            var list = new List<Dictionary<string, object?>>();

            // Execute raw SQL using ADO.NET connection
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                await _context.Database.OpenConnectionAsync();

                try
                {
                    using (var result = await command.ExecuteReaderAsync())
                    {
                        // Read column names
                        while (await result.ReadAsync())
                        {
                            var dict = new Dictionary<string, object?>();
                            for (int i = 0; i < result.FieldCount; i++)
                            {
                                var columnName = result.GetName(i);
                                var value = result.IsDBNull(i) ? null : result.GetValue(i);
                                dict[columnName] = value;
                            }
                            list.Add(dict);
                        }
                    }
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing query: {ex.Message}", ex);
        }
    }
}
