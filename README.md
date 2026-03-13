<div align="center">

# 🤖 Zendesk Support Chatbot

**A production-ready, full-stack AI support chatbot with a multi-level agentic pipeline and Human-in-the-Loop escalation.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-18-DD0031?logo=angular)](https://angular.dev/)
[![OpenAI](https://img.shields.io/badge/OpenAI-GPT--4o-412991?logo=openai)](https://platform.openai.com/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/your-username/zendesk-support-chatbot/pulls)

<br/>

*Fork it. Configure it. Ship it. No vendor lock-in beyond OpenAI and SQL Server.*

</div>

---

## 📸 Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        User Browser                             │
│                                                                 │
│   ┌─────────────────────────────┐                               │
│   │   Angular 18 Chatbot Widget │  ← Embeddable in any page     │
│   │   · Slide-in/out animation  │                               │
│   │   · Typing indicator        │                               │
│   │   · Session memory UI       │                               │
│   └──────────────┬──────────────┘                               │
└──────────────────┼──────────────────────────────────────────────┘
                   │  POST /api/chat
                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core 8 Web API                        │
│                                                                 │
│   ╔═══════════════════════════════════════════════════════╗     │
│   ║              4-Stage Agent Pipeline                   ║     │
│   ║                                                       ║     │
│   ║  ① IntentClassificationAgent   (gpt-4o-mini)         ║     │
│   ║     └─ Detects: reset_password │ unsubscribe │ faq…  ║     │
│   ║                                                       ║     │
│   ║  ② KnowledgeRetrievalAgent     (Vector Store / RAG)  ║     │
│   ║     └─ Fetches relevant FAQ + website docs            ║     │
│   ║                                                       ║     │
│   ║  ③ ResponseGenerationAgent     (gpt-4o)              ║     │
│   ║     └─ Grounded answer from retrieved context         ║     │
│   ║                                                       ║     │
│   ║  ④ WorkflowExecutionAgent      (SQL Server / HITL)   ║     │
│   ║     └─ Runs actions OR creates a Human-Review ticket  ║     │
│   ╚═══════════════════════════════════════════════════════╝     │
│                                                                 │
│   ← SQL Server (EF Core)   ← OpenAI API   ← Serilog logs       │
└─────────────────────────────────────────────────────────────────┘
```

---

## ✨ Features

<table>
<tr>
<td width="50%">

**🧠 AI Pipeline**
- GPT-4o-mini for fast intent classification
- GPT-4o for grounded response generation
- RAG via OpenAI Vector Stores — no hallucinations
- Session memory across conversation turns

</td>
<td width="50%">

**⚙️ Workflow Automation**
- Password reset via email link
- Unsubscribe / Do-Not-Sell actions
- Profile update against your database
- Direct SQL Server integration via EF Core

</td>
</tr>
<tr>
<td width="50%">

**🙋 Human-in-the-Loop**
- Auto-escalates after 2 failed clarifications
- Pluggable `ITicketingService` interface
- Drop-in Zendesk / Jira / Freshdesk adapters
- HITL management REST endpoints

</td>
<td width="50%">

**🔒 Auth & Security**
- ASP.NET Core Identity + cookie sessions
- Authenticated users unlock database workflows
- Serilog structured logging + escalation audit trail
- `.gitignore` hardened — secrets never committed

</td>
</tr>
</table>

---

## 🗂️ Project Structure

```
zendesk-support-chatbot/
│
├── frontend/                          # Angular 18 standalone app
│   └── src/
│       ├── app/
│       │   ├── app.component.html         # Host page
│       │   └── components/
│       │       └── chatbot-widget/        # Self-contained chat UI
│       ├── services/
│       │   ├── chat.service.ts            # API communication layer
│       │   └── app-config.service.ts      # Runtime config loader
│       ├── models/chat.models.ts
│       └── assets/config.json            # { "apiUrl": "..." }
│
└── backend/KyrisCBL/                  # ASP.NET Core 8 Web API
    │
    ├── Pipeline/                      # ← The core of the system
    │   ├── Core/
    │   │   ├── IAgent.cs              #   Agent contract
    │   │   ├── AgentContext.cs        #   Shared state bus
    │   │   └── AgentPipeline.cs       #   Sequential orchestrator
    │   ├── Agents/
    │   │   ├── IntentClassificationAgent.cs
    │   │   ├── KnowledgeRetrievalAgent.cs
    │   │   ├── ResponseGenerationAgent.cs
    │   │   └── WorkflowExecutionAgent.cs
    │   └── HumanReview/
    │       ├── ITicketingService.cs   #   ← Swap for Zendesk/Jira here
    │       ├── InMemoryTicketingService.cs
    │       └── SupportTicket.cs
    │
    ├── Controllers/
    │   ├── ChatController.cs          # POST /api/chat
    │   ├── AuthController.cs          # /api/auth/*
    │   ├── AccountController.cs       # /api/account/*
    │   ├── HumanReviewController.cs   # /api/humanreview/*
    │   └── FaqAdminController.cs
    │
    ├── Services/
    │   ├── RetrievalService.cs        # OpenAI Vector Store queries
    │   ├── WorkflowService.cs         # Business action implementations
    │   └── Embedding/
    │       ├── EmbeddingsService.cs
    │       ├── EmbeddingMatcher.cs
    │       └── FaqEmbeddingGenerator.cs
    │
    ├── Data/
    │   ├── AppDbContext.cs
    │   ├── ChatDataService.cs
    │   ├── faqs.json                  # Seed FAQ data
    │   └── Prompts/
    │       └── system_prompt.txt      # ← Customize your bot's personality
    │
    ├── Models/          Config/          Helpers/
    └── Program.cs                    # DI wiring + pipeline registration
```

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Node.js | 18.0+ | [nodejs.org](https://nodejs.org/) |
| Angular CLI | 18.0+ | `npm install -g @angular/cli` |
| SQL Server | Any recent | [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) |
| OpenAI API Key | — | [platform.openai.com](https://platform.openai.com/api-keys) |

---

<details>
<summary><b>🖥️ Backend Setup</b> (click to expand)</summary>

<br/>

**1. Configure secrets**

Open `backend/KyrisCBL/appsettings.json` and fill in your values:

```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "ModelName": "text-embedding-3-small"
  },
  "ConnectionStrings": {
    "Default": "server=YOUR_SERVER;database=YOUR_DB;uid=YOUR_USER;pwd=YOUR_PASS;TrustServerCertificate=True;"
  }
}
```

> ⚠️ Never commit real credentials. Use `appsettings.Production.json` (already gitignored), environment variables, or a secrets manager in production.

**2. Set your Vector Store IDs**

In `Pipeline/Agents/KnowledgeRetrievalAgent.cs`:

```csharp
private const string FaqVectorStoreId     = "YOUR_FAQ_VECTOR_STORE_ID";
private const string WebsiteVectorStoreId = "YOUR_WEBSITE_VECTOR_STORE_ID";
```

Upload your FAQ and website content as files to your [OpenAI Vector Store](https://platform.openai.com/docs/api-reference/vector-stores), then paste the IDs above.

**3. Customize the system prompt**

Edit `Data/Prompts/system_prompt.txt` — this is the personality and grounding instruction given to the response agent. Replace `YourCompany` with your brand name and adjust the tone.

**4. Set your CORS origin**

In `Program.cs`, update the allowed origin for production:

```csharp
policy.WithOrigins("https://your-production-domain.com")
```

**5. Apply migrations and run**

```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run --project KyrisCBL
```

API starts at `https://localhost:7275` by default.

</details>

---

<details>
<summary><b>🌐 Frontend Setup</b> (click to expand)</summary>

<br/>

**1. Point to your API**

Edit `frontend/src/assets/config.json`:

```json
{
  "apiUrl": "https://localhost:7275"
}
```

This file is loaded at runtime — no rebuild needed when switching environments.

**2. (Optional) Add Google Tag Manager**

In `src/app/app.component.html`, uncomment the GTM snippet and replace `GTM-XXXXXXXXX` with your container ID.

**3. Run the dev server**

```bash
cd frontend
npm install
ng serve
```

Open `http://localhost:4200` — the chatbot widget appears fixed in the bottom-right corner.

</details>

---

## 🔌 API Reference

### Chat

```
POST /api/chat
```

**Request**
```json
{ "message": "I need to reset my password" }
```

**Response**
```json
{
  "message": "I've sent a password reset link to your registered email.",
  "solved": true,
  "requiresHumanReview": false,
  "ticketId": null
}
```

When `requiresHumanReview` is `true`, a ticket has been created and `ticketId` will be populated.

---

### Authentication

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Sign in |
| `POST` | `/api/auth/logout` | Sign out |
| `GET` | `/api/auth/me` | Current user info |
| `POST` | `/api/auth/register` | Create account |

---

### Human Review (support staff)

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/humanreview/pending` | List all tickets pending review |
| `POST` | `/api/humanreview/{id}/resolve` | Resolve a ticket with optional resolution note |

> Both endpoints require `[Authorize]`. See Security Notes to add role-based access.

---

## 🔧 Extending the System

### Add a new intent

**1.** Add the intent name to the classification prompt in `IntentClassificationAgent.cs`.

**2.** Handle it in `WorkflowExecutionAgent.cs`:

```csharp
case "your_new_intent":
    await _workflowService.HandleYourNewIntentAsync(context);
    break;
```

**3.** Implement the method in `IWorkflowService` / `WorkflowService.cs`.

---

### Plug in a real ticketing system

Implement `ITicketingService` and register it — no changes to agent code:

```csharp
public sealed class ZendeskTicketingService : ITicketingService
{
    public async Task CreateOrUpdateAsync(SupportTicket ticket, CancellationToken ct = default)
    {
        // POST https://your-domain.zendesk.com/api/v2/tickets
    }

    public async Task<IReadOnlyList<SupportTicket>> GetPendingAsync(CancellationToken ct = default)
    {
        // GET tickets with status: pending
    }

    public async Task ResolveAsync(string ticketId, string? resolution = null, CancellationToken ct = default)
    {
        // PUT https://your-domain.zendesk.com/api/v2/tickets/{id}
    }
}
```

In `Program.cs`, swap the registration:

```csharp
// builder.Services.AddScoped<ITicketingService, InMemoryTicketingService>();
builder.Services.AddScoped<ITicketingService, ZendeskTicketingService>();
```

---

### Add a new pipeline agent

1. Implement `IAgent`:

```csharp
public sealed class SentimentAnalysisAgent : IAgent
{
    public string Name => "SentimentAnalysis";

    public Task ExecuteAsync(AgentContext context, CancellationToken ct = default)
    {
        // analyse context.UserMessage, write to context
        return Task.CompletedTask;
    }
}
```

2. Register and insert in `Program.cs`:

```csharp
builder.Services.AddScoped<SentimentAnalysisAgent>();

builder.Services.AddScoped<AgentPipeline>(sp => new AgentPipeline(
    agents: new IAgent[]
    {
        sp.GetRequiredService<IntentClassificationAgent>(),
        sp.GetRequiredService<SentimentAnalysisAgent>(),   // ← inserted here
        sp.GetRequiredService<KnowledgeRetrievalAgent>(),
        sp.GetRequiredService<ResponseGenerationAgent>(),
        sp.GetRequiredService<WorkflowExecutionAgent>()
    },
    logger: sp.GetRequiredService<ILogger<AgentPipeline>>()
));
```

Any agent can short-circuit the rest of the pipeline by setting `context.IsComplete = true`.

---

## 🔐 Security Checklist

> Address all of these before going to production.

- [ ] **Password hashing** — `AuthController` uses a plaintext stub marked `// TODO`. Replace with `PasswordHasher<ChatUser>` or Argon2.
- [ ] **Cookie security** — Set `SecurePolicy = Always` and `SameSite = None` for HTTPS production in `Program.cs`.
- [ ] **CORS** — Lock the allowed origin to your production domain.
- [ ] **HITL role auth** — Add `[Authorize(Roles = "SupportAgent")]` to `HumanReviewController`.
- [ ] **Secrets management** — Use environment variables, `dotnet user-secrets`, Azure Key Vault, or AWS Secrets Manager. Never `appsettings.json` in production.

---

## 🌍 Environment Variables

ASP.NET Core maps environment variables using `__` as a section separator:

```bash
# Linux / macOS
export OpenAI__ApiKey="sk-..."
export ConnectionStrings__Default="server=...;database=...;"

# Windows
set OpenAI__ApiKey=sk-...
set ConnectionStrings__Default=server=...;database=...;
```

---

## 🤝 Contributing

Contributions, issues and feature requests are welcome.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

Distributed under the MIT License. See [`LICENSE`](LICENSE) for more information.

---

<div align="center">

Built with ❤️ using Angular, ASP.NET Core, and OpenAI

</div>
