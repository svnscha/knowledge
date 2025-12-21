# 🧠 Knowledge

> *"Because calling it 'AI-Stuff-I-Throw-Together-At-2AM' didn't fit the domain name."*

A hands-on companion repository for the AI Agents in .NET blog series at [svnscha.de](https://svnscha.de). Each branch builds on the previous, taking you from "Hello World" to agentic systems.

## Series Branches

| Branch | Topic | What You'll Learn |
|--------|-------|-------------------|
| `main` | Repository & Hello World Agent | Project setup, DevUI, your first conversational agent |


> *New branches added as the series progresses. Star the repo to stay updated!*

## Getting Started

1. **Clone** & checkout the branch for your current article
2. **Open in VS Code** with Dev Containers
3. **Configure your API key** (see [Configuration](#configuration) below)
4. **Run** `dotnet run --project src/Knowledge`
5. **Navigate** to `http://localhost:5000/devui`

## Configuration

Application settings are managed via `appsettings.json` and `appsettings.Development.json`. For sensitive values like API keys, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

### Setting up User Secrets

Initialize and set your API key:

```bash
# Navigate to the Knowledge project
cd src/Knowledge

# Set your OpenAI API key
dotnet user-secrets set "Knowledge:ApiKey" "your-api-key-here"

```

To view your current secrets:

```bash
dotnet user-secrets list
```

To remove a secret:

```bash
dotnet user-secrets remove "Knowledge:ApiKey"
```

> **Note:** User secrets are stored outside the project directory and are never committed to source control. They only work in the `Development` environment.

## Tech Stack

- .devcontainer with .NET 10 and PostgreSQL + pgvector
- ASP.NET including [DevUI](https://learn.microsoft.com/en-us/agent-framework/user-guide/devui/?pivots=programming-language-csharp) and [Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)

## Blog Series

Follow along at [svnscha.de](https://svnscha.de) where each article walks through the code in detail.

## License

MIT - Build something awesome.

