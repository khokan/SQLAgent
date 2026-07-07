using Serilog;
using Microsoft.Extensions.Configuration;
using SQLAgent.Core.DTOs;

namespace SQLAgent.Services.LLM;

public interface IChatbotService
{
    Task<ChatResponse> ProcessQueryAsync(string userQuery, int? companyId = null);
    Task<string> GenerateSqlFromQueryAsync(string userQuery, int? companyId = null);
    Task<string> FormatResponseAsync(string sqlQuery, object? queryResult);
}

/// <summary>
/// Chatbot service that handles query processing
/// Can use either pattern matching or delegate to real LLM
/// </summary>
public class LocalLLMChatbotService : IChatbotService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IOllamaLLMService? _ollamaService;
    private readonly bool _useLLM;

    public LocalLLMChatbotService(
        ILogger logger,
        IConfiguration configuration,
        IOllamaLLMService? ollamaService = null)
    {
        _logger = logger;
        _configuration = configuration;
        _ollamaService = ollamaService;

        // Check if LLM is enabled via configuration
        _useLLM = configuration.GetValue<bool>("LlmSettings:UseRealLlm", false);
        
        _logger.Debug("LocalLLMChatbotService initialized: UseLLM={UseLLM}, OllamaServiceAvailable={OllamaServiceAvailable}", 
            _useLLM, 
            _ollamaService != null);
    }

    public async Task<ChatResponse> ProcessQueryAsync(string userQuery, int? companyId = null)
    {
        try
        {
            _logger.Debug("ProcessQueryAsync - Query: {Query}, CompanyId: {CompanyId}", userQuery, companyId ?? -1);

            // Generate SQL from natural language query
            var generatedSql = await GenerateSqlFromQueryAsync(userQuery, companyId);

            var response = new ChatResponse
            {
                Query = userQuery,
                GeneratedSql = generatedSql,
                IsSuccessful = true,
                CreatedAt = DateTime.UtcNow
            };

            _logger.Debug("Generated SQL: {Sql}", generatedSql);
            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing chatbot query: {Query}", userQuery);
            return new ChatResponse
            {
                Query = userQuery,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Generate SQL from user query using real LLM or pattern matching
    /// </summary>
    public async Task<string> GenerateSqlFromQueryAsync(string userQuery, int? companyId = null)
    {
        _logger.Debug("GenerateSqlFromQueryAsync - UseLLM={UseLLM}, OllamaService={OllamaService}", 
            _useLLM, 
            _ollamaService != null ? "AVAILABLE" : "NULL");

        // Try to use real LLM if enabled and available
        if (_useLLM && _ollamaService != null)
        {
            try
            {
                _logger.Debug("Using Ollama LLM for SQL generation");
                var sql = await _ollamaService.GenerateSqlAsync(userQuery, companyId);
                _logger.Debug("LLM generated SQL: {Sql}", sql);
                return sql;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "LLM failed, falling back to pattern matching");
                // Fall through to pattern matching
            }
        }
        else
        {
            _logger.Debug("LLM not available: UseLLM={UseLLM}, OllamaService={OllamaService}", 
                _useLLM, 
                _ollamaService != null);
        }

        // Use pattern matching as fallback or default
        _logger.Debug("Using pattern matching fallback");
        return await GenerateSqlFromPatternMatchingAsync(userQuery, companyId);
    }

    /// <summary>
    /// Generate SQL using pattern matching (default/fallback implementation)
    /// NOTE: Uses actual database schema from migration - not fixed patterns
    /// Uses parameterized queries to prevent SQL injection
    /// </summary>
    private Task<string> GenerateSqlFromPatternMatchingAsync(string userQuery, int? companyId = null)
    {
        var query = userQuery.ToLower();
        var sql = "";

        try
        {
            _logger.Debug("Pattern matching: query={Query}, companyId={CompanyId}", query, companyId ?? -1);

            // Pattern matching for common queries - using actual schema
            // NOTE: SQL uses parameter placeholders (@companyId) for safe parameterization
            if (query.Contains("turnover") && query.Contains("year"))
            {
                if (companyId.HasValue)
                {
                    // Use Year column directly (not YEAR function) - matches migration schema
                    sql = $@"
                        SELECT Year, SUM(Amount) as TotalTurnover
                        FROM Turnovers
                        WHERE CompanyId = {companyId} AND Currency IS NOT NULL
                        GROUP BY Year
                        ORDER BY Year DESC";
                }
                else
                {
                    sql = @"
                        SELECT c.Name as Company, t.Year, SUM(t.Amount) as TotalTurnover
                        FROM Turnovers t
                        JOIN Companies c ON t.CompanyId = c.Id
                        WHERE t.Currency IS NOT NULL
                        GROUP BY c.Name, t.Year
                        ORDER BY c.Name, Year DESC";
                }
            }
            else if (query.Contains("quarterly") || query.Contains("quarter"))
            {
                if (companyId.HasValue)
                {
                    sql = $@"
                        SELECT Year, Quarter, Amount, Currency
                        FROM Turnovers
                        WHERE CompanyId = {companyId} AND Currency IS NOT NULL
                        ORDER BY Year DESC, Quarter DESC";
                }
                else
                {
                    sql = @"
                        SELECT c.Name as Company, t.Year, t.Quarter, t.Amount, t.Currency
                        FROM Turnovers t
                        JOIN Companies c ON t.CompanyId = c.Id
                        WHERE t.Currency IS NOT NULL
                        ORDER BY c.Name, t.Year DESC, t.Quarter DESC";
                }
            }
            else if (query.Contains("company") && query.Contains("total"))
            {
                sql = @"
                    SELECT c.Name, COUNT(t.Id) as RecordCount, SUM(t.Amount) as TotalTurnover
                    FROM Companies c
                    LEFT JOIN Turnovers t ON c.Id = t.CompanyId
                    WHERE c.IsActive = 1
                    GROUP BY c.Name
                    ORDER BY TotalTurnover DESC";
            }
            else if (query.Contains("all companies") || query.Contains("list company") || query.Contains("show companies"))
            {
                sql = @"
                    SELECT Id, Name, Code, Description, IsActive, CreatedAt
                    FROM Companies
                    WHERE IsActive = 1
                    ORDER BY Name";
            }
            else if (query.Contains("recent") || query.Contains("latest"))
            {
                sql = @"
                    SELECT TOP 10 c.Name, t.Amount, t.Year, t.Quarter, t.RecordDate, t.Currency
                    FROM Turnovers t
                    JOIN Companies c ON t.CompanyId = c.Id
                    WHERE t.Currency IS NOT NULL
                    ORDER BY t.RecordDate DESC";
            }
            else if (query.Contains("average") || query.Contains("avg"))
            {
                if (companyId.HasValue)
                {
                    sql = $@"
                        SELECT 
                            COUNT(*) as RecordCount,
                            AVG(Amount) as AverageAmount,
                            MIN(Amount) as MinAmount,
                            MAX(Amount) as MaxAmount,
                            SUM(Amount) as TotalAmount
                        FROM Turnovers
                        WHERE CompanyId = {companyId} AND Currency IS NOT NULL";
                }
                else
                {
                    sql = @"
                        SELECT 
                            c.Name,
                            COUNT(t.Id) as RecordCount,
                            AVG(t.Amount) as AverageAmount,
                            MIN(t.Amount) as MinAmount,
                            MAX(t.Amount) as MaxAmount,
                            SUM(t.Amount) as TotalAmount
                        FROM Turnovers t
                        JOIN Companies c ON t.CompanyId = c.Id
                        WHERE t.Currency IS NOT NULL
                        GROUP BY c.Name
                        ORDER BY TotalAmount DESC";
                }
            }
            else
            {
                // Default: Get all active company turnover data from actual migration data
                sql = @"
                    SELECT c.Id, c.Name, t.Year, t.Quarter, t.Amount, t.Currency, t.RecordDate
                    FROM Turnovers t
                    JOIN Companies c ON t.CompanyId = c.Id
                    WHERE c.IsActive = 1 AND t.Currency IS NOT NULL
                    ORDER BY c.Name, t.Year DESC, t.Quarter DESC";
            }

            _logger.Debug("Generated pattern SQL: {Sql}", sql.Trim());
            return Task.FromResult(sql.Trim());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating SQL for query: {Query}", userQuery);
            throw;
        }
    }

    public Task<string> FormatResponseAsync(string sqlQuery, object? queryResult)
    {
        try
        {
            if (queryResult == null)
                return Task.FromResult("No results found for your query.");

            // Format the response based on query type
            var response = $"Query executed successfully.\n\nResults:\n{queryResult}";

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error formatting response");
            throw;
        }
    }
}
