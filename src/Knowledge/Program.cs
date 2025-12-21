using System.ClientModel;
using Knowledge.Shared.Extensions;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Configure shared services and infrastructure
builder.ConfigureKnowledgeDefaults(settings =>
{
    var options = new OpenAIClientOptions();

    // Use default endpoint, unless specified otherwise.
    if (!string.IsNullOrEmpty(settings.ApiEndpoint))
    {
        options.Endpoint = new Uri(settings.ApiEndpoint);
    }

    OpenAIClient client = new OpenAIClient(new ApiKeyCredential(settings.ApiKey), options);

    // Add Chat Client
    builder.Services.AddChatClient(client.GetChatClient("gpt-4.1").AsIChatClient());

    // Hello World AI Agent.
    builder.AddAIAgent("Knowledge", "You are a helpful agent named Knowledge.");

});

// Add OpenAI services required for DevUI
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigureKnowledgePipeline();

// Map DevUI endpoints
app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

// Redirect root to DevUI
app.MapGet("/", () => Results.Redirect("/devui/"));

// Log startup complete with available endpoints
app.LogStartupComplete();

app.Run();
