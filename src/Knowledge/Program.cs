// Knowledge API - Demo Application for AI Agent Development

using System.ClientModel;
using Knowledge.Shared.Extensions;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OpenAI;

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

    builder.Services.AddChatClient(client.GetChatClient("gpt-4.1").AsIChatClient());

    builder.AddAIAgent("Knowledge", "You are a helpful agent named Knowledge.");
});

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
