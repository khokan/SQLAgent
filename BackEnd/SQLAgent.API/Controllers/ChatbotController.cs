using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SQLAgent.Core.DTOs;
using SQLAgent.Core.Models;
using SQLAgent.Infrastructure.Data.Repositories;
using SQLAgent.Services.LLM;
using System.Security.Claims;

namespace SQLAgent.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly IRepository<ChatHistory> _chatHistoryRepository;
    private readonly ITurnoverRepository _turnoverRepository;
    private readonly IRepository<Company> _companyRepository;
    private readonly Serilog.ILogger _logger;

    public ChatbotController(
        IChatbotService chatbotService,
        IRepository<ChatHistory> chatHistoryRepository,
        ITurnoverRepository turnoverRepository,
        IRepository<Company> companyRepository,
        Serilog.ILogger logger)
    {
        _chatbotService = chatbotService;
        _chatHistoryRepository = chatHistoryRepository;
        _turnoverRepository = turnoverRepository;
        _companyRepository = companyRepository;
        _logger = logger;
    }

    /// <summary>
    /// Send a query to the chatbot
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendQuery([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { message = "Query cannot be empty" });

            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            _logger.Debug("ChatbotController.SendQuery - User: {UserId}, Query: {Query}, CompanyId: {CompanyId}", 
                userId, request.Query, request.CompanyId ?? -1);

            // Process the query
            var chatResponse = await _chatbotService.ProcessQueryAsync(request.Query, request.CompanyId);

            if (chatResponse.IsSuccessful)
            {
                try
                {
                    // Execute the generated SQL query
                    var queryResult = await ExecuteQueryAsync(chatResponse.GeneratedSql, request.CompanyId);
                    chatResponse.Data = queryResult;

                    // Format the response
                    chatResponse.Response = await _chatbotService.FormatResponseAsync(
                        chatResponse.GeneratedSql, 
                        queryResult);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing SQL query");
                    chatResponse.IsSuccessful = false;
                    chatResponse.ErrorMessage = "Error executing query: " + ex.Message;
                }
            }

            // Save chat history
            var chatHistory = new ChatHistory
            {
                UserId = userId,
                UserQuery = request.Query,
                GeneratedSql = chatResponse.GeneratedSql,
                Response = chatResponse.Response,
                RawQueryResult = chatResponse.Data?.ToString(),
                IsSuccessful = chatResponse.IsSuccessful,
                ErrorMessage = chatResponse.ErrorMessage,
                CreatedAt = DateTime.UtcNow
            };

            var savedHistory = await _chatHistoryRepository.AddAsync(chatHistory);
            chatResponse.ChatHistoryId = savedHistory.Id;

            await _chatHistoryRepository.SaveChangesAsync();

            return Ok(new ApiResponse<ChatResponse>
            {
                Success = chatResponse.IsSuccessful,
                Message = chatResponse.IsSuccessful ? "Query processed successfully" : "Error processing query",
                Data = chatResponse
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in SendQuery");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error processing request",
                Errors = new Dictionary<string, string> { { "error", ex.Message } }
            });
        }
    }

    /// <summary>
    /// Get chat history for current user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<ChatHistory>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChatHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (userId == 0)
                return Unauthorized();

            var allHistory = await _chatHistoryRepository.GetAllAsync();
            var userHistory = allHistory
                .Where(ch => ch.UserId == userId)
                .OrderByDescending(ch => ch.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new ApiResponse<IEnumerable<ChatHistory>>
            {
                Success = true,
                Message = "Chat history retrieved successfully",
                Data = userHistory
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving chat history");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error retrieving chat history"
            });
        }
    }

    /// <summary>
    /// Get available companies for querying
    /// </summary>
    [HttpGet("companies")]
    [ProducesResponseType(typeof(IEnumerable<Company>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies()
    {
        try
        {
            var companies = await _companyRepository.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<Company>>
            {
                Success = true,
                Message = "Companies retrieved successfully",
                Data = companies
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error retrieving companies");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error retrieving companies"
            });
        }
    }

    /// <summary>
    /// Generate business summary and insights from query results
    /// </summary>
    [HttpPost("summarize")]
    [ProducesResponseType(typeof(SummarizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Summarize([FromBody] SummarizeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserQuery))
                return BadRequest(new { message = "User query cannot be empty" });

            if (request.Data == null)
                return BadRequest(new { message = "Data cannot be empty" });

            var summaryPrompt = BuildSummaryPrompt(request.UserQuery, request.Data, request.DataType);
            
            // Use the chatbot service to generate a summary
            var summary = await _chatbotService.FormatResponseAsync(summaryPrompt, request.Data);

            _logger.Information("Summary generated for query: {Query}", request.UserQuery);

            return Ok(new ApiResponse<SummarizeResponse>
            {
                Success = true,
                Message = "Summary generated successfully",
                Data = new SummarizeResponse 
                { 
                    Summary = summary,
                    DataType = request.DataType,
                    RecordCount = GetRecordCount(request.Data)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating summary");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error generating summary",
                Errors = new Dictionary<string, string> { { "error", ex.Message } }
            });
        }
    }

    private string BuildSummaryPrompt(string userQuery, object data, string dataType)
    {
        var dataJson = System.Text.Json.JsonSerializer.Serialize(data);
        
        var prompt = $@"You are a business analyst. Based on the following query and data results, provide a concise business-friendly summary with key insights and actionable recommendations.

User Query: {userQuery}
Data Type: {dataType}
Data Results: {dataJson}

Please provide:
1. Key Finding: The main insight from this data
2. Business Impact: What this means for the business
3. Recommendations: What should be done next
4. Alert (if applicable): Any concerning trends or anomalies

Format your response in a clear, professional manner suitable for business stakeholders.";

        return prompt;
    }

    private int GetRecordCount(object data)
    {
        if (data is System.Collections.IEnumerable enumerable)
        {
            int count = 0;
            foreach (var item in enumerable)
            {
                count++;
            }
            return count;
        }
        return 1;
    }

    private async Task<object?> ExecuteQueryAsync(string sql, int? companyId = null)
    {
        try
        {
            _logger.Debug("ExecuteQueryAsync called with SQL: {Sql}", sql);

            // Check what type of query this is and execute accordingly
            var sqlLower = sql.ToLower();

            // Handle different query types
            if (sqlLower.Contains("select") && sqlLower.Contains("turnover"))
            {
                _logger.Debug("Executing Turnover query via raw SQL");
                // Execute raw SQL query for turnovers
                return await _turnoverRepository.ExecuteRawQueryAsync(sql);
            }
            else if (sqlLower.Contains("select") && sqlLower.Contains("companies"))
            {
                _logger.Debug("Executing Companies query");
                return await _companyRepository.GetAllAsync();
            }
            else if (sqlLower.Contains("select") && sqlLower.Contains("year") && sqlLower.Contains("group by"))
            {
                _logger.Debug("Executing aggregation query");
                // Try to execute as raw query first
                try
                {
                    return await _turnoverRepository.ExecuteRawQueryAsync(sql);
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Raw SQL query failed, falling back to LINQ");
                    // Fallback to LINQ-based aggregation
                    if (companyId.HasValue)
                    {
                        return await _turnoverRepository.GetYearlyTurnoverAsync(companyId.Value);
                    }
                    else
                    {
                        var allTurnovers = await _turnoverRepository.GetAllAsync();
                        return allTurnovers
                            .GroupBy(t => t.Year)
                            .Select(g => new { Year = g.Key, TotalTurnover = g.Sum(t => t.Amount), Count = g.Count() })
                            .OrderByDescending(x => x.Year)
                            .ToList();
                    }
                }
            }
            else
            {
                _logger.Debug("Executing generic query via raw SQL");
                // Try to execute any other SELECT query
                return await _turnoverRepository.ExecuteRawQueryAsync(sql);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing query: {Sql}", sql);
            // Return error information
            return new { Error = $"Query execution failed: {ex.Message}", OriginalSql = sql };
        }
    }
}
