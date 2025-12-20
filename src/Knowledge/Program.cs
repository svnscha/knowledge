using Microsoft.Agents.AI.DevUI;
using Knowledge.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure shared services and infrastructure
builder.ConfigureKnowledgeDefaults();

// Add OpenAI services required for DevUI
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// TODO: Register your custom agents here as the blog series progresses
// builder.Services.AddSingleton<IYourAgent, YourAgent>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.ConfigureKnowledgePipeline();

// Map DevUI endpoints
app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

// Redirect root to DevUI
app.MapGet("/", () => Results.Redirect("/devui"));

// Log startup complete with available endpoints
app.LogStartupComplete();

app.Run();
