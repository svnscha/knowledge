using Microsoft.Agents.AI.DevUI;
using Shared.Extensions;
using Shared.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSharedServices(builder.Configuration);

// Add OpenAI services required for DevUI
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

// Configure logging with shared configuration
builder.Logging.ConfigureSharedLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();


// Needed for DevUI to function
app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

app.Run();
