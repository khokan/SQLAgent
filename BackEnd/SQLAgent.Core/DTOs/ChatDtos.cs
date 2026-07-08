namespace SQLAgent.Core.DTOs;

public class ChatRequest
{
    public string Query { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
}

public class ChatResponse
{
    public int ChatHistoryId { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SummarizeRequest
{
    public string UserQuery { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string DataType { get; set; } = "table"; // kpi, chart, table, empty
}

public class SummarizeResponse
{
    public string Summary { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

public class TurnoverDto
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class CompanyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
