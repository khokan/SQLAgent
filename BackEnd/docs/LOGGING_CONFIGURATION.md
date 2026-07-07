# LOGGING_CONFIGURATION.md

## 📝 SQLAgent Logging Configuration Guide

**Updated**: July 6, 2026  
**Status**: ✅ Optimized for Production & Development

---

## 🎯 Logging Strategy

### Log Levels (from least to most verbose)
1. **Fatal** - Application terminating
2. **Error** - Errors that need attention
3. **Warning** - Warnings about potential issues
4. **Information** - General informational messages (DEFAULT in Production)
5. **Debug** - Detailed debugging information (DEFAULT in Development)
6. **Verbose** - Very detailed trace information

---

## ⚙️ Configuration by Environment

### Production Environment (appsettings.json)

**Default Log Level**: `Information`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**What Gets Logged**:
- ✅ Application startup/shutdown
- ✅ Errors and warnings
- ✅ Important business operations
- ✅ Failed authentication attempts
- ✅ Database migrations
- ❌ Entity Framework debug details
- ❌ Verbose framework logs

**Log File Size**: ~5-10 MB per day

---

### Development Environment (appsettings.Development.json)

**Default Log Level**: `Information` (for application code)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SQLAgent": "Debug",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "DetailedErrors": true
}
```

**What Gets Logged**:
- ✅ All application logs (Information level)
- ✅ SQLAgent namespace logs (Debug level)
- ✅ Errors and warnings from frameworks
- ❌ Verbose Entity Framework details (kept at Warning level)
- ❌ Request/response tracing
- ❌ ASP.NET Core internals

**Why?**
- Clean logs without Entity Framework noise
- Easy to find your application logs
- Still see important framework warnings
- Reduced log file size

**Log File Size**: ~2-5 MB per day

---

## 📊 Log Categories

### Application Logs (Level: Debug in Dev, Info in Prod)
```csharp
_logger.Debug("Processing user query: {Query}", query);
_logger.Information("User {UserId} logged in successfully", userId);
_logger.Warning("Query took longer than expected: {Duration}ms", duration);
_logger.Error(ex, "Failed to process query");
```

**When to Use**:
- ✅ Debug: Detailed flow information (variable values, method entry/exit)
- ✅ Information: Important events (login, query completion, etc.)
- ✅ Warning: Something unexpected but recoverable
- ✅ Error: Operations failed

### Framework Logs (Level: Warning in both environments)
```
Microsoft.AspNetCore.*
Microsoft.EntityFrameworkCore.*
Microsoft.AspNetCore.Authentication.*
```

**Filtered to Warning** to reduce noise:
- ❌ Request details (usually too verbose)
- ❌ EF Core connection management
- ❌ Model binding details
- ✅ Actual warnings/errors from frameworks

---

## 📂 Log Files

### Location
```
SQLAgent.API/logs/app-YYYY-MM-DD.txt
```

### Rolling Policy
- **Interval**: Daily
- **File Naming**: `app-20260705.txt`, `app-20260706.txt`, etc.
- **Retention**: Depends on disk space (keep 7-30 days typically)

### Example Log Entry (Production)
```
2026-07-05 10:00:55.272 +06:00 [INF] SQLAgent API starting up
2026-07-05 10:01:12.453 +06:00 [INF] User 'testuser' logged in successfully
2026-07-05 10:01:25.678 +06:00 [INF] Query processed: SELECT * FROM Turnovers
2026-07-05 10:01:26.123 +06:00 [ERR] Failed to execute query: Timeout
```

### Example Log Entry (Development)
```
2026-07-05 10:00:55.272 +06:00 [DBG] Registering services
2026-07-05 10:00:55.275 +06:00 [DBG] Database migration completed
2026-07-05 10:01:12.453 +06:00 [DBG] ChatbotController.SendQuery - User: 1, Query: Show me all companies
2026-07-05 10:01:25.678 +06:00 [DBG] ExecuteQueryAsync called with SQL: SELECT * FROM Turnovers
2026-07-05 10:01:26.123 +06:00 [ERR] Failed to execute query: Timeout
```

---

## 🔧 Logging Implementation

### In Program.cs
```csharp
// Environment-aware logging
var isDevelopment = builder.Environment.IsDevelopment();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Is(isDevelopment 
        ? Serilog.Events.LogEventLevel.Information 
        : Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();
```

### In Your Code
```csharp
private readonly ILogger _logger;

public MyService(ILogger logger)
{
    _logger = logger;
}

public async Task ProcessAsync(string query)
{
    // Debug: Detailed information (only in dev)
    _logger.Debug("Processing query: {Query}", query);
    
    try
    {
        // Do work...
        
        // Information: Important events
        _logger.Information("Query processed successfully");
    }
    catch (Exception ex)
    {
        // Error: Failed operations
        _logger.Error(ex, "Failed to process query");
    }
}
```

---

## 📊 What NOT to Log

### ❌ Don't Log at Information Level (in Development):
- Every method entry/exit
- Loop iterations
- Variable values (unless in Debug)
- Entity Framework connection management
- Request routing details

### ❌ Don't Log Sensitive Data:
- ❌ Passwords
- ❌ API Keys
- ❌ Personal information
- ❌ Credit card numbers
- ✅ Instead: Log masked values or generic messages

### Example (BAD):
```csharp
_logger.Information("User logged in with password: {Password}", password); // ❌ Don't do this!
_logger.Information("Processing turnovers: {Turnovers}", turnovers); // ❌ Too much detail
```

### Example (GOOD):
```csharp
_logger.Information("User {Username} logged in successfully", username); // ✅ Good
_logger.Debug("Processing {Count} turnovers", turnoverList.Count); // ✅ Good
```

---

## 🎯 Recommended Logging Patterns

### Authentication
```csharp
_logger.Information("User {Username} logged in", username);
_logger.Warning("Failed login attempt for user {Username}", username);
```

### Database Operations
```csharp
_logger.Debug("Executing query: {Query}", query); // Only in Debug mode
_logger.Error(ex, "Database error during operation {Operation}", operationName);
```

### Business Operations
```csharp
_logger.Information("Query processed successfully");
_logger.Warning("Query took {Duration}ms, expected < 5000ms", duration);
_logger.Error(ex, "Query processing failed: {Query}", query);
```

### Performance
```csharp
var stopwatch = Stopwatch.StartNew();
// ... operation ...
stopwatch.Stop();
if (stopwatch.ElapsedMilliseconds > 5000)
{
    _logger.Warning("Slow operation: {Operation} took {Duration}ms", operationName, stopwatch.ElapsedMilliseconds);
}
```

---

## 🔍 Troubleshooting

### Issue: Too Many Logs
**Solution**: Check appsettings for your environment
- Production: Should have `"Default": "Information"`
- Development: Check if Entity Framework is set to Debug (should be Warning)

### Issue: Not Enough Logs
**Solution**: 
1. Ensure logging is configured correctly
2. Check your log level in appsettings
3. Make sure your logger is injected properly
4. Verify log file path is writable

### Issue: Log File Too Large
**Solution**:
- Reduce log level to `Warning` or `Error`
- Archive old log files (7+ days old)
- Check for infinite loops logging data

### Issue: Sensitive Data in Logs
**Solution**:
- Use structured logging with named parameters
- Log IDs instead of full objects
- Mask sensitive information
- Implement log sanitization

---

## 📈 Log Analysis

### Using Logs for Debugging

**Find errors quickly:**
```powershell
# Windows
Select-String "\[ERR\]|\[WARN\]" .\logs\app-*.txt

# Linux/Mac
grep "\[ERR\]\|\[WARN\]" ./logs/app-*.txt
```

**Find specific user activity:**
```powershell
Select-String "User: 123" .\logs\app-*.txt
```

**Find slow operations:**
```powershell
Select-String "took.*ms" .\logs\app-*.txt
```

---

## 📋 Best Practices

### ✅ DO:
- Log errors with exceptions
- Use structured logging with named parameters
- Log important business operations
- Log authentication events
- Use appropriate log levels

### ❌ DON'T:
- Log sensitive data
- Log at Information level in loops
- Log framework internals
- Use string concatenation (use named parameters)
- Log the same error multiple times

---

## 🚀 Performance Impact

### Minimal Performance Impact:
- Console output: ~1-5ms per log
- File output: ~5-50ms per log
- Filtered logs: No performance impact

### Optimization Tips:
- Keep log level at `Information` or higher in production
- Use filtering to reduce noise
- Archive old log files regularly
- Monitor log file size

---

## 📚 References

### Serilog Documentation
- https://serilog.net/
- https://github.com/serilog/serilog/wiki

### .NET Logging
- https://docs.microsoft.com/en-us/dotnet/core/extensions/logging

### Entity Framework Core Logging
- https://docs.microsoft.com/en-us/ef/core/logging-events-diagnostics/simple-logging

---

## Summary

**Production**: Clean, minimal logs (Information level) - focus on errors and important events  
**Development**: Detailed logs (Information level) - useful debugging information without framework noise  
**Both**: No verbose Entity Framework debugging - keep at Warning level

This reduces log file size while keeping essential information visible.

---

*Configure logging wisely. Log smartly. Debug efficiently.*
