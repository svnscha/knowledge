using System.ClientModel;
using Knowledge.Services;
using Knowledge.Shared.Agents;
using Knowledge.Shared.Extensions;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OpenAI;

// Agents are registered as scoped services and wired via AddAIAgent

var builder = WebApplication.CreateBuilder(args);

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

    // Register agents
    builder.Services.AddSingleton<KnowledgeSearchAgent>();
    builder.AddAIAgent("Knowledge", (services, key) => AgentFactory.CreateKnowledgeAgent(chatClient, services, key));
    builder.AddAIAgent("KnowledgeSearch", (services, key) => AgentFactory.CreateKnowledgeSearchAgent(chatClient, services, key));
});

// Register background service for embedding processing
builder.Services.AddHostedService<EmbeddingBackgroundService>();

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.ConfigureKnowledgePipeline();

app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

app.MapGet("/", () => Results.Redirect("/devui/"));

app.LogStartupComplete();

app.Run();

