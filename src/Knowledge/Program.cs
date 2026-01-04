using System.ClientModel;
using Knowledge.Services;
using Knowledge.Shared.Agents;
using Knowledge.Shared.Extensions;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.AI;
using OpenAI;

// Agents are registered as scoped services and wired via AddAIAgent

var builder = WebApplication.CreateBuilder(args);

// Collect agent builders for endpoint mapping
var agentBuilders = new List<IHostedAgentBuilder>();

builder.ConfigureKnowledgeDefaults((settings, logger) =>
{
    if (string.IsNullOrWhiteSpace(settings.ApiKey))
    {
        logger.LogWarning("No API key configured. Set Knowledge:ApiKey in user secrets or environment.");
    }

    var options = new OpenAIClientOptions();

    if (!string.IsNullOrEmpty(settings.ApiEndpoint))
    {
        options.Endpoint = new Uri(settings.ApiEndpoint);
    }

    var client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);
    var chatClient = client.GetChatClient("gpt-4.1").AsIChatClient();

    // Configure embedding service
    var embeddingClient = client.GetEmbeddingClient("text-embedding-3-small").AsIEmbeddingGenerator();
    builder.Services.AddEmbeddingService(embeddingClient);

    // Register agents and collect builders for endpoint mapping
    builder.Services.AddSingleton<KnowledgeSearchAgent>();
    agentBuilders.Add(builder.AddAIAgent("Knowledge", (services, key) => AgentFactory.CreateKnowledgeAgent(chatClient, services, key)));
    agentBuilders.Add(builder.AddAIAgent("KnowledgeSearch", (services, key) => AgentFactory.CreateKnowledgeSearchAgent(chatClient, services, key)));
    agentBuilders.Add(builder.AddAIAgent("KnowledgeTitle", (services, key) => AgentFactory.CreateKnowledgeTitleAgent(chatClient, services, key)));
});

// Register background service for embedding processing
builder.Services.AddHostedService<EmbeddingBackgroundService>();

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// Rewrite /v1/chat/completions to /{model}/v1/chat/completions based on request body
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path?.Equals("/v1/chat/completions", StringComparison.OrdinalIgnoreCase) == true)
    {
        context.Request.EnableBuffering();

        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        string? model = null;
        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(body);
                if (json.RootElement.TryGetProperty("model", out var modelElement))
                {
                    model = modelElement.GetString();
                }
            }
            catch (System.Text.Json.JsonException)
            {
            }
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Chat completions request missing required 'model' field");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = new { message = "The 'model' field is required", type = "invalid_request_error", param = "model", code = "missing_required_parameter" } });
            return;
        }

        context.Request.Path = $"/{model}/v1/chat/completions";
    }
    await next();
});

app.UseRouting();

app.ConfigureKnowledgePipeline();

// Map OpenAI chat completions endpoint for each registered agent
foreach (var agentBuilder in agentBuilders)
{
    app.MapOpenAIChatCompletions(agentBuilder);
}

app.MapGet("/", () => Results.Redirect("/swagger"));

app.LogStartupComplete();

app.Run();

