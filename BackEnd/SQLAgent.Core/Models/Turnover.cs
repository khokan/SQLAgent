namespace SQLAgent.Core.Models;

public class Turnover
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public decimal Amount { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    
    // Foreign Key
    public Company? Company { get; set; }
}
