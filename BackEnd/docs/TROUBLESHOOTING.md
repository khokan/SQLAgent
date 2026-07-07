# TROUBLESHOOTING.md - Common Issues & Solutions

---

## 🔴 CRITICAL ISSUES

### 1. Password Verification Disabled (Security Risk)

**Symptom**: Can login with wrong password

**Location**: `SQLAgent.Services/Authentication/AuthenticationService.cs` line 48

**Issue**:
```csharp
// DISABLED FOR TESTING
// if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
```

**Fix**: Re-enable immediately
```csharp
if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    return new AuthResponse { Success = false, Message = "Invalid credentials" };
```

**Status**: ⚠️ Must fix before production

---

## API Won't Start

### Error: Port 5000 Already in Use

```
error: Unable to bind to http://localhost:5000 on the IPv4 loopback interface.
```

**Solution 1**: Find and stop process using port 5000
```powershell
netstat -ano | findstr :5000
taskkill /PID {PID} /F
```

**Solution 2**: Change port in launchSettings.json
```json
"applicationUrl": "http://localhost:7000"
```

---

### Error: Build Failed - Missing Dependencies

```
error: The project file contains an unsupported item type
```

**Check**:
1. .NET 8 SDK installed: `dotnet --version`
2. Restore NuGet: `dotnet restore`
3. Delete bin/obj: `rmdir bin obj /S /Q`
4. Rebuild: `dotnet build`

---

## Database Issues

### Error: Connection Refused

```
SqlException: A network-related or instance-specific error occurred
```

**Check connection string**:
```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SQLAgent;Trusted_Connection=true;"
```

**Fix**:
1. Start SQL Server (LocalDB): `SqlLocalDB.exe start mssqllocaldb`
2. Or use your instance name: `Server=YOUR_SERVER_NAME`

### Error: Database Migration Failed

```
error: An error occurred while migrating the database
```

**Solution**:
```powershell
cd SQLAgent.API
dotnet ef database drop --force
dotnet ef database update
```

---

## Authentication Issues

### Error: 401 Unauthorized

**Cause 1**: No JWT token
- Get token from login endpoint

**Cause 2**: Token format wrong
- Use: `Bearer eyJhbGc...` (with "Bearer " prefix)

**Cause 3**: Token expired
- Default expiration: 60 minutes
- Get fresh token from login

**Cause 4**: JWT secret mismatch
- Check appsettings: `JwtSettings:Secret` (min 32 chars)

---

## Ollama Issues

### Error: Connection Refused on 11434

```
error: Ollama server is not running on http://localhost:11434/api
```

**Solution**:
```powershell
# Check if running
tasklist | find /I "ollama"

# Start Ollama
ollama serve

# Check port
netstat -ano | findstr :11434
```

---

### Error: Model Not Found

```
error: model 'mistral' not found
```

**Solution**:
```powershell
# List available models
ollama list

# Download model
ollama pull mistral

# Or use alternate
ollama pull qwen2
ollama pull llama2
```

---

### Ollama Very Slow (> 15 seconds per query)

**Cause**: CPU-only inference (no GPU)

**Solution**:
1. Install NVIDIA CUDA (for GPU support)
2. Restart Ollama
3. Check Ollama logs for GPU initialization

Or switch to pattern matching (no LLM):
```json
{
  "LlmSettings": {
    "UseRealLlm": false
  }
}
```

---

## Chatbot Endpoint Issues

### Error: Invalid SQL Generated

```
error: Invalid column name 'IsActive' in Turnovers table
```

**Cause**: LLM generating SQL for wrong schema

**Fix**: 
1. Check database schema matches migration
2. Clear Ollama context: Stop and restart
3. Try different model: `ollama pull qwen2`

---

### Error: No Results Returned

**Check**:
1. Database has test data: 
```sql
SELECT COUNT(*) FROM Companies
SELECT COUNT(*) FROM Turnovers
```
2. Generated SQL is correct
3. WHERE clauses filter correctly

---

### Error: 500 Internal Server Error

**Solution**:
1. Check logs: `logs/app-YYYY-MM-DD.txt`
2. Look for exception details
3. Check console output

**Common causes**:
- Database connection failed
- LLM timeout
- Invalid SQL execution

---

## Logging Issues

### Logs Not Appearing

**Check directory exists**:
```powershell
mkdir SQLAgent.API\logs
```

**Check permissions**:
- Ensure read/write access to `logs` folder

**Check configuration** in `Program.cs`:
```csharp
.WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
```

### Can't Find Specific Log Entry

**Tail file**:
```powershell
Get-Content "SQLAgent.API\logs\app-*.txt" -Wait -Tail 50
```

**Search for text**:
```powershell
Select-String -Path "SQLAgent.API\logs\app-*.txt" -Pattern "ProcessQueryAsync"
```

---

## Performance Issues

### API Response Slow

**Check**:
1. Response time in Swagger (milliseconds)
2. Ollama model loaded: `ollama list`
3. Database query performance
4. Network latency

**Benchmarks**:
- Pattern matching: < 100ms
- LLM: 2-8 seconds
- Database: 100-500ms

---

### Memory Usage High

**Check**:
1. Ollama memory: Model * 2GB (Mistral = ~4GB)
2. API memory: ~200-300MB
3. Database connections: Check connection pooling

**Solution**:
- Close other apps
- Use smaller LLM model
- Enable query caching

---

## Configuration Issues

### UseRealLlm Setting Not Working

**File**: `SQLAgent.API/appsettings.Development.json`

```json
{
  "LlmSettings": {
    "UseRealLlm": true  // Must be boolean, not string
  }
}
```

**Restart API** after changing config

---

### Connection String Not Found

**Check file**: `appsettings.Development.json`

Must include:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

---

## Development Issues

### Breakpoints Not Hit

**Solution**:
1. Ensure Debug config: `dotnet run --configuration Debug`
2. Rebuild solution: `dotnet clean && dotnet build`
3. Restart debugger: Stop (Shift+F5) then Start (F5)

### IntelliSense Not Working

**Solution**:
- Restart VS Code
- Reload folder: Ctrl+K Ctrl+F
- Delete `.vs` folder and reopen

---

## Deployment Issues

### Connection String Works Local, Not Production

**Check**:
1. Production server name correct
2. Credentials valid
3. Firewall allows connections
4. SQL Server listens on network

---

## Quick Reset

If everything broken, start fresh:

```powershell
# Stop API
# Stop Ollama

# Delete database
# (or drop in SQL Server Management Studio)

# Clear logs
rmdir SQLAgent.API\logs /S /Q

# Rebuild
dotnet clean
dotnet build

# Restart everything
```

---

## Getting Help

Check in this order:
1. Logs: `logs/app-*.txt`
2. This guide: TROUBLESHOOTING.md
3. Code comments in service files
4. Error messages in Swagger responses

---

Last Updated: 2026-07-05
