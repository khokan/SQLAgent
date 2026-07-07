# SQLAgent - Complete Guide

**Start here** if you're new to the project.

---

## 📋 Quick Overview

**What is SQLAgent?**
- AI-powered chatbot API built with .NET 8
- Converts natural language queries to SQL
- Uses Ollama (local LLM) or pattern matching for SQL generation
- Secure JWT authentication
- Persistent chat history in SQL Server

**Key Features:**
- ✅ Real LLM integration (Ollama)
- ✅ Pattern matching fallback
- ✅ JWT authentication
- ✅ Database persistence
- ✅ Comprehensive logging
- ✅ Full-stack debugging in VS Code

---

## 🚀 Quick Start (5 Minutes)

### Prerequisites
- Ollama installed on Windows: https://ollama.ai
- .NET 8 SDK
- SQL Server (LocalDB or named instance)
- VS Code (optional, for debugging)

### Step 1: Start Ollama
```powershell
ollama serve
# In another terminal, load a model:
ollama pull mistral
```

### Step 2: Start the API
```powershell
cd i:\CSE\Projects\Current\WEB\ai\SQLAgent\SQLAgent.API
dotnet run --configuration Debug
```

Expected: `Now listening on: http://localhost:5000`

### Step 3: Test via Swagger
1. Open: http://localhost:5000/swagger
2. Register user: `POST /api/auth/register`
3. Login: `POST /api/auth/login` (copy token)
4. Click "Authorize" button, paste token
5. Test query: `POST /api/chatbot/query`

```json
{
  "query": "Show me all companies",
  "companyId": null
}
```

**Success**: See `isSuccessful: true` with generated SQL and results

---

## 📁 Project Structure

```
SQLAgent/
├── SQLAgent.API/                 # Main API project
│   ├── Controllers/              # API endpoints
│   │   ├── AuthController.cs     # JWT authentication
│   │   └── ChatbotController.cs  # Main chatbot endpoint
│   ├── Middleware/               # Global error handling
│   ├── Program.cs                # Dependency injection setup
│   ├── appsettings.json          # Production config
│   └── appsettings.Development.json  # Dev config (LLM settings)
│
├── SQLAgent.Core/                # Models and DTOs
│   ├── Models/                   # Database entities
│   │   ├── User.cs
│   │   ├── Company.cs
│   │   ├── Turnover.cs
│   │   └── ChatHistory.cs
│   └── DTOs/                     # Data transfer objects
│       ├── AuthDtos.cs
│       └── ChatDtos.cs
│
├── SQLAgent.Infrastructure/      # Data access layer
│   ├── Data/
│   │   ├── ApplicationDbContext.cs    # EF Core context
│   │   └── Repositories/             # Repository pattern
│   └── Migrations/               # Database migrations
│
├── SQLAgent.Services/            # Business logic
│   ├── Authentication/
│   │   ├── AuthenticationService.cs
│   │   └── JwtTokenService.cs
│   └── LLM/
│       ├── LocalLLMChatbotService.cs  # Main chatbot processor
│       └── OllamaLLMService.cs        # Ollama integration
│
├── docs/                         # Documentation
└── README.md
```

---

## 🔑 Key Components

### 1. LocalLLMChatbotService
- **File**: `SQLAgent.Services/LLM/LocalLLMChatbotService.cs`
- **Purpose**: Main query processor
- **Decision Logic**: Uses real LLM if enabled, falls back to pattern matching

### 2. OllamaLLMService
- **File**: `SQLAgent.Services/LLM/OllamaLLMService.cs`
- **Purpose**: Integrates with Ollama API for SQL generation
- **Configuration**: `appsettings.Development.json` (OllamaUrl, Model)

### 3. ChatbotController
- **File**: `SQLAgent.API/Controllers/ChatbotController.cs`
- **Endpoint**: `POST /api/chatbot/query`
- **Flow**: 
  1. Validates JWT token
  2. Calls ProcessQueryAsync()
  3. Executes generated SQL
  4. Saves to chat history
  5. Returns results

### 4. Database Schema
**Key Tables**:
- **Companies**: Id, Name, Code, Description, IsActive
- **Turnovers**: Id, CompanyId, Amount, Year, Quarter, Currency
- **Users**: Id, Username, Email, PasswordHash, IsActive
- **ChatHistories**: Id, UserId, UserQuery, GeneratedSql, Response, IsSuccessful

---

## ⚙️ Configuration

### Enable Real LLM (Ollama)
**File**: `SQLAgent.API/appsettings.Development.json`
```json
{
  "LlmSettings": {
    "UseRealLlm": true,
    "OllamaUrl": "http://localhost:11434/api",
    "Model": "mistral"
  }
}
```

### Disable Real LLM (Use Pattern Matching)
```json
{
  "LlmSettings": {
    "UseRealLlm": false
  }
}
```

### Connection String
**File**: `SQLAgent.API/appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=SQLAgent;Trusted_Connection=true;"
  }
}
```

---

## 📚 Documentation

| Document | Purpose | When to Read |
|----------|---------|--------------|
| **00_START_HERE.md** | This file - overview & quick start | First time |
| **SETUP_GUIDE.md** | Detailed setup instructions | Setup |
| **TESTING_GUIDE.md** | How to test the system | Testing |
| **CODE_REVIEW_REPORT.md** | Code quality & architecture | Development |
| **TROUBLESHOOTING.md** | Common issues & solutions | Problems |

---

## 🧪 Common Test Queries

Try these in Swagger after getting JWT token:

**Query 1: Simple List**
```json
{"query": "Show me all companies", "companyId": null}
```

**Query 2: Aggregation**
```json
{"query": "What is the total turnover by year?", "companyId": null}
```

**Query 3: Filtered**
```json
{"query": "Show quarterly turnover for company 1", "companyId": 1}
```

**Query 4: Company-Specific**
```json
{"query": "Show all data for company 1", "companyId": 1}
```

---

## 🔍 Verify Installation

Check that everything works:

```bash
# 1. Ollama running?
curl http://localhost:11434/api/tags
# Should return model list

# 2. API running?
curl http://localhost:5000/swagger
# Should show Swagger UI

# 3. Database accessible?
# SQL Server running? Check connection string in appsettings
```

---

## 🆘 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| "Connection refused" on 11434 | Start Ollama: `ollama serve` |
| "Model not found" | Load model: `ollama pull mistral` |
| "401 Unauthorized" | Get token from login, click Authorize in Swagger |
| "500 Internal Server Error" | Check logs: `logs/app-YYYY-MM-DD.txt` |
| Database connection error | Check connection string in appsettings |

See **TROUBLESHOOTING.md** for detailed solutions.

---

## 🏗️ Architecture Overview

```
User Request (Swagger)
    ↓
ChatbotController
    ↓ (JWT verification)
ProcessQueryAsync()
    ↓
LocalLLMChatbotService
    ├→ LLM Enabled? → OllamaLLMService → Ollama API
    └→ LLM Disabled? → Pattern Matching → Hardcoded SQL
    ↓
ExecuteQueryAsync()
    ↓
TurnoverRepository.ExecuteRawQueryAsync()
    ↓
SQL Server Database
    ↓
Format Results
    ↓
Save to ChatHistory
    ↓
Return to User
```

---

## 🔐 Security

- ✅ JWT authentication on protected endpoints
- ✅ Password hashing with BCrypt
- ✅ SQL Server with parameterized queries
- ✅ CORS configured
- ✅ Global exception handling (no stack traces exposed)

---

## 📖 Next Steps

1. **Setup**: Follow SETUP_GUIDE.md
2. **Test**: Follow TESTING_GUIDE.md
3. **Debug**: Use VS Code with built-in debugging
4. **Code Review**: Check CODE_REVIEW_REPORT.md
5. **Issues**: See TROUBLESHOOTING.md

---

## 💡 Key URLs

| URL | Purpose |
|-----|---------|
| http://localhost:5000/swagger | Swagger UI for API testing |
| http://localhost:11434/api/tags | Ollama available models |
| http://localhost:5000/api/chatbot/query | Main chatbot endpoint |
| i:\CSE\Projects\Current\WEB\ai\SQLAgent\SQLAgent.API\logs | Log files |

---

**Ready to go?** 👉 Start with **SETUP_GUIDE.md**

Last Updated: 2026-07-05
