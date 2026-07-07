namespace SQLAgent.Core.Models;

public class ChatHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserQuery { get; set; } = string.Empty;
    public string GeneratedSql { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string? RawQueryResult { get; set; }
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Foreign Key
    public User? User { get; set; }
}
