# SETUP_GUIDE.md - Complete Setup Instructions

## Prerequisites

- Windows 10/11
- .NET 8 SDK
- SQL Server (LocalDB or named instance)
- VS Code (optional for debugging)
- Git (optional)

---

## Step 1: Install & Run Ollama

### Download
https://ollama.ai - Download for Windows

### Install
- Run installer
- Accept defaults
- Add to PATH (automatic)

### Start Ollama
**Option A: UI (Easiest)**
- Click Ollama in Start menu or taskbar

**Option B: Command Line**
```powershell
ollama serve
```

### Load a Model
In another PowerShell:
```powershell
# Download Mistral (recommended)
ollama pull mistral

# Or use alternatives
ollama pull qwen2
ollama pull llama2

# List available models
ollama list
```

### Verify Ollama Running
```powershell
curl http://localhost:11434/api/tags
# Should return JSON with available models
```

✅ **Expected**: See list of models in JSON format

---

## Step 2: Configure Database

### Option A: SQL Server LocalDB (Recommended)

LocalDB is included with Visual Studio. If not installed:

```powershell
# Check if installed
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"

# If error, download SQL Server Express or Developer Edition
# https://www.microsoft.com/en-us/sql-server/sql-server-downloads
```

### Option B: Named SQL Server Instance

Update connection string in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=SQLAgent;Trusted_Connection=true;"
  }
}
```

### Verify Database Connection

# This should show your connection string from API's appsettings.json



```powershell
Add-Migration InitialCreate
Update-Database
```

In `SQLAgent.API` folder:
```powershell
dotnet ef database update
# This creates database and runs migrations
```
or 
```powershell
dotnet ef dbcontext info --startup-project SQLAgent.API --project SQLAgent.Infrastructure

```

✅ **Expected**: Database created, migrations applied

---

## Step 3: Configure Application

### Open Project
```powershell
cd i:\CSE\Projects\Current\WEB\ai\SQLAgent
code .  # Opens in VS Code
# Or: explorer .  # Opens in File Explorer
```

### Edit Configuration

**File**: `SQLAgent.API/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SQLAgent;Trusted_Connection=true;"
  },
  "LlmSettings": {
    "UseRealLlm": true,
    "OllamaUrl": "http://localhost:11434/api",
    "Model": "mistral"
  },
  "JwtSettings": {
    "Secret": "your-secret-key-min-32-chars-here!!!!",
    "Issuer": "SQLAgent",
    "Audience": "SQLAgent",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug"
    }
  }
}
```

**Key Settings**:
- `UseRealLlm`: true = Use Ollama, false = Use pattern matching
- `OllamaUrl`: Must match Ollama server address
- `Model`: Available models: mistral, qwen2, llama2, neural-chat
- `Secret`: Should be at least 32 characters

---

## Step 4: Build Solution

```powershell
cd i:\CSE\Projects\Current\WEB\ai\SQLAgent
dotnet build SQLAgent.sln
```

**Expected**: 
```
Build succeeded with X warning(s) in Y.Zs
```

⚠️ **If failed**: 
- Check .NET 8 installed: `dotnet --version`
- Restore NuGet: `dotnet restore`
- Build specific project: `dotnet build SQLAgent.API`

---

## Step 5: Run the API

### Terminal Method
```powershell
cd SQLAgent.API
dotnet run --configuration Debug
```

### VS Code Method
- F5 or Debug → Start Debugging
- Select ".NET 8" debugger

**Expected Output**:
```
info: SQLAgent.Services.LLM.LocalLLMChatbotService[0]
      LocalLLMChatbotService initialized: UseLLM=True

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

✅ **Success**: API is running

---

## Step 6: Test Swagger UI

1. **Open**: http://localhost:5000/swagger
2. **You should see**: SQLAgent API v1 with endpoints listed

---

## Step 7: Register & Authenticate

### Register a User

In Swagger:
1. Find `POST /api/auth/register`
2. Click "Try it out"
3. Enter:
```json
{
  "username": "testuser",
  "email": "testuser@example.com",
  "password": "TestPassword123!"
}
```
4. Click "Execute"

✅ **Expected**: 200 OK with JWT token

### Authorize Swagger

1. Copy the JWT token from response
2. Click "Authorize" button (top right)
3. Paste: `Bearer {token}`
4. Click "Authorize"

---

## Step 8: Test Chatbot Endpoint

### Send Query

1. Find `POST /api/chatbot/query`
2. Click "Try it out"
3. Enter:
```json
{
  "query": "Show me all companies",
  "companyId": null
}
```
4. Click "Execute"

✅ **Expected Response**:
```json
{
  "success": true,
  "message": "Query processed successfully",
  "data": {
    "isSuccessful": true,
    "query": "Show me all companies",
    "generatedSql": "SELECT ...",
    "response": "Query executed successfully...",
    "data": [...],
    "chatHistoryId": 1
  }
}
```

---

## Step 9: Check Logs

### Location
```
i:\CSE\Projects\Current\WEB\ai\SQLAgent\SQLAgent.API\logs\app-YYYY-MM-DD.txt
```

### Look For
```
>>> ProcessQueryAsync CALLED <<<
>>> Using Ollama LLM for SQL generation <<<
Successfully generated SQL query
```

---

## Optional: VS Code Debugging Setup

### Open Workspace
```powershell
code i:\CSE\Projects\Current\WEB\ai\SQLAgent
```

### Install Extensions
- C# Dev Kit (Microsoft)
- REST Client (Huachao Mao)

### Debug Steps
1. Set breakpoint in `LocalLLMChatbotService.cs` line 50
2. Press F5
3. Go to Swagger and send a query
4. Debugger will pause at breakpoint
5. Step through code with F10/F11

### Debug Console
- View → Debug Console
- See all logging output in real-time

---

## Verify Everything Works

Use this checklist:

- [ ] Ollama running: `curl http://localhost:11434/api/tags`
- [ ] Model loaded: See output from above
- [ ] API running: http://localhost:5000/swagger accessible
- [ ] Database created: Check SQL Server
- [ ] Register works: Can create user in Swagger
- [ ] Login works: Get JWT token
- [ ] Query works: See SQL generated and executed
- [ ] Logs written: Check `logs/app-*.txt`

---

## Common Setup Issues

| Issue | Solution |
|-------|----------|
| Ollama not found | Install from https://ollama.ai |
| Model not found | Run: `ollama pull mistral` |
| Port 5000 in use | Stop other apps or change port in launchSettings.json |
| Database error | Check connection string, run migrations |
| Token invalid | Get fresh token from login endpoint |

See **TROUBLESHOOTING.md** for detailed solutions.

---

## Configuration Options

### Use Only Pattern Matching (No Ollama Required)
```json
{
  "LlmSettings": {
    "UseRealLlm": false
  }
}
```

### Change Ollama Model
```json
{
  "LlmSettings": {
    "Model": "qwen2"
  }
}
```

### Change API Port
**File**: `SQLAgent.API/Properties/launchSettings.json`
```json
{
  "applicationUrl": "http://localhost:7000"
}
```

---

## What's Next?

1. ✅ Done with setup
2. 👉 Go to **TESTING_GUIDE.md** to test queries
3. 📖 Check **CODE_REVIEW_REPORT.md** for code structure

---

Last Updated: 2026-07-05
