using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting.OpenAI;
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

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

// Needed for DevUI to function
app.MapOpenAIResponses();
app.MapOpenAIConversations();
app.MapDevUI();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
