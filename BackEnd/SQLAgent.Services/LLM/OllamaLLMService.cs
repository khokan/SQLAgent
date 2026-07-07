using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Serilog;
using SQLAgent.Core.DTOs;

namespace SQLAgent.Services.LLM;

/// <summary>
/// Interface for Ollama LLM service for generating SQL and analyzing results
/// </summary>
public interface IOllamaLLMService
{
    /// <summary>
    /// Generate SQL query from natural language user query
    /// </summary>
    Task<string> GenerateSqlAsync(string userQuery, int? companyId = null);

    /// <summary>
    /// Analyze database results and provide natural language summary
    /// </summary>
    Task<string> AnalyzeResultAsync(string query, object? result);

    /// <summary>
    /// Check if Ollama server is running
    /// </summary>
    Task<bool> IsOllamaRunningAsync();
}

/// <summary>
/// Real LLM integration using Ollama
/// Connects to local Ollama instance for SQL generation and result analysis
/// Reads configuration from appsettings.json (LlmSettings section)
/// </summary>
public class OllamaLLMService : IOllamaLLMService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string _ollamaUrl;
    private readonly string _model;
    private const int DefaultTimeoutSeconds = 60;

    public OllamaLLMService(ILogger logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
        
        // Read configuration from appsettings.json
        var llmSettings = configuration.GetSection("LlmSettings");
        _ollamaUrl = llmSettings["OllamaUrl"] ?? "http://localhost:11434/api";
        _model = llmSettings["Model"] ?? "mistral";
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Increase to 5 minutes
        _logger.Information("OllamaLLMService initialized with URL: {Url}, Model: {Model}", _ollamaUrl, _model);
    }

    /// <summary>
    /// Generate SQL query from natural language query using Ollama
    /// </summary>
    public async Task<string> GenerateSqlAsync(string userQuery, int? companyId = null)
    {
        try
        {
            _logger.Information("Generating SQL with Ollama (model: {Model}) for query: {Query}", _model, userQuery);

            // Check if Ollama is running
            var isRunning = await IsOllamaRunningAsync();
            if (!isRunning)
            {
                _logger.Error("Ollama server is not running on {Url}", _ollamaUrl);
                throw new Exception($"Ollama server is not running on {_ollamaUrl}. Please start it with: ollama serve");
            }

            var prompt = BuildSqlPrompt(userQuery, companyId);
            var sqlQuery = await CallOllamaAsync(prompt);

            _logger.Information("Successfully generated SQL query");
            return sqlQuery;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating SQL with Ollama for query: {Query}", userQuery);
            throw;
        }
    }

    /// <summary>
    /// Analyze query results and provide natural language summary
    /// </summary>
    public async Task<string> AnalyzeResultAsync(string query, object? result)
    {
        try
        {
            _logger.Information("Analyzing results with Ollama for query: {Query}", query);

            if (result == null)
            {
                return "The query was executed successfully, but no results were found.";
            }

            var prompt = $@"You are a helpful data analyst. A user asked: ""{query}""

The database returned these results:
{JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })}

Please provide a clear, concise natural language summary of these results in 2-3 sentences. Focus on the key insights.";

            var analysis = await CallOllamaAsync(prompt);
            _logger.Information("Successfully analyzed results");
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error analyzing results");
            throw;
        }
    }

    /// <summary>
    /// Check if Ollama server is running by attempting to access /api/tags endpoint
    /// </summary>
    public async Task<bool> IsOllamaRunningAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_ollamaUrl}/tags");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Ollama health check failed");
            return false;
        }
    }

    /// <summary>
    /// Call Ollama API to generate text
    /// </summary>
    private async Task<string> CallOllamaAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                temperature = 0.7,
                top_p = 0.9,
                top_k = 40
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.Debug("Calling Ollama API with model {Model}", _model);

            var response = await _httpClient.PostAsync($"{_ollamaUrl}/generate", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.Error("Ollama API error {StatusCode}: {Error}", response.StatusCode, errorContent);
                throw new Exception($"Ollama API error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseContent);

            if (jsonDoc.RootElement.TryGetProperty("response", out var responseElement))
            {
                var result = responseElement.GetString();
                return result?.Trim() ?? string.Empty;
            }

            throw new Exception("Unexpected Ollama response format");
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "Ollama connection error - is server running on {Url}?", _ollamaUrl);
            throw new Exception($"Cannot connect to Ollama on {_ollamaUrl}. Please ensure Ollama is running: ollama serve");
        }
        catch (TaskCanceledException ex)
        {
            _logger.Error(ex, "Ollama request timeout after {Timeout} seconds", DefaultTimeoutSeconds);
            throw new Exception($"Ollama request timed out. The model may be generating a long response. Try again or increase timeout.");
        }
    }

    /// <summary>
    /// Build an optimized prompt for SQL generation
    /// Provides accurate schema information from actual migration
    /// </summary>
    private string BuildSqlPrompt(string userQuery, int? companyId)
    {
        var databaseSchema = @"
Database: SQLAgent
IMPORTANT: Use EXACT column names from migration - no assumptions

1. Users Table
   - Id (INT PRIMARY KEY)
   - Username (NVARCHAR(450) UNIQUE)
   - Email (NVARCHAR(450) UNIQUE)
   - PasswordHash (NVARCHAR(MAX))
   - FullName (NVARCHAR(MAX))
   - IsActive (BIT)
   - CreatedAt (DATETIME2)
   - UpdatedAt (DATETIME2)

2. Companies Table
   - Id (INT PRIMARY KEY)
   - Name (NVARCHAR(MAX))
   - Code (NVARCHAR(450) UNIQUE)
   - Description (NVARCHAR(MAX))
   - IsActive (BIT)
   - CreatedAt (DATETIME2)

3. Turnovers Table (CRITICAL: Only these columns exist)
   - Id (INT PRIMARY KEY)
   - CompanyId (INT FOREIGN KEY to Companies.Id)
   - Amount (DECIMAL(18,2))
   - Year (INT) [e.g., 2023, 2024]
   - Quarter (INT) [values: 1, 2, 3, 4]
   - Currency (NVARCHAR(MAX)) [e.g., USD, EUR, GBP]
   - Description (NVARCHAR(MAX))
   - RecordDate (DATETIME2)
   *** NOTE: Turnovers has NO IsActive column ***

4. ChatHistories Table
   - Id (INT PRIMARY KEY)
   - UserId (INT FOREIGN KEY to Users.Id)
   - UserQuery (NVARCHAR(MAX))
   - GeneratedSql (NVARCHAR(MAX))
   - Response (NVARCHAR(MAX))
   - RawQueryResult (NVARCHAR(MAX))
   - IsSuccessful (BIT)
   - ErrorMessage (NVARCHAR(MAX))
   - CreatedAt (DATETIME2)

Common Joins:
- Turnovers -> Companies: ON Turnovers.CompanyId = Companies.Id
- ChatHistories -> Users: ON ChatHistories.UserId = Users.Id
- ChatHistories -> Companies: When filtering by company

Sample Data:
- Companies: TechCorp (Id=1), FinanceHub (Id=2), RetailPlus (Id=3)
- Years: 2023, 2024
- Quarters: 1, 2, 3, 4
- Currencies: USD";

        var instruction = $@"You are an expert SQL Server T-SQL generator. Convert natural language to SQL.

CRITICAL REQUIREMENTS:
1. ONLY output the SQL query - no explanations, no markdown, no extra text
2. Query must START with SELECT and END with semicolon
3. Use EXACT column names from schema above
4. DO NOT add columns that don't exist (e.g., no IsActive in Turnovers)
5. Join tables when needed
6. Use proper WHERE clauses for filters
7. Use GROUP BY for aggregations with correct column ordering
8. Use ORDER BY to sort results (DESC for dates/amounts)
9. Handle NULL values with ISNULL() if needed
10. Use T-SQL syntax (SQL Server specific)

SCHEMA REFERENCE:
{databaseSchema}

QUERY REQUEST:
Natural language: ""{userQuery}""
{(companyId.HasValue ? $"Company filter: WHERE CompanyId = {companyId}" : "")}

GENERATE SQL (query only, no explanation):";

        return instruction;
    }
}
