using System.ComponentModel;
using System.Text;
using Knowledge.Shared.Abstractions;
using Knowledge.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Knowledge.Shared.Agents;

/// <summary>
/// Agent providing semantic search over conversation history.
/// </summary>
public class KnowledgeSearchAgent
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IDbContextFactory<KnowledgeDbContext> _dbContextFactory;
    private readonly ILogger<KnowledgeSearchAgent> _logger;

    private const int DefaultTopK = 10;
    private const double DefaultMinScore = 0.40;

    public KnowledgeSearchAgent(
        IEmbeddingService embeddingService,
        IDbContextFactory<KnowledgeDbContext> dbContextFactory,
        ILogger<KnowledgeSearchAgent> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches past conversation messages to recall previous discussions, context, and information shared in earlier chats.
    /// </summary>
    [Description("Search past conversation messages to recall previous discussions, context, and information shared in earlier chats.")]
    public async Task<string> SearchConversationHistoryAsync(
        [Description("The search query describing what past discussion or information you're looking for")]
        string query)
    {
        _logger.LogInformation("KnowledgeSearch: Searching for '{Query}'", query);

        // Generate embedding for the search query
        var queryVector = await _embeddingService.EmbedAsync(query);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Perform vector similarity search using pgvector's cosine distance
        // The <=> operator returns cosine distance (1 - similarity), so we filter and sort accordingly
        var results = await dbContext.Embeddings
            .Where(e => e.SourceType == "Message")
            .Select(e => new
            {
                Embedding = e,
                Distance = e.Vector.CosineDistance(queryVector)
            })
            .Where(x => x.Distance <= (1 - DefaultMinScore))
            .OrderBy(x => x.Distance)
            .Take(DefaultTopK)
            .ToListAsync();

        if (results.Count == 0)
        {
            _logger.LogInformation("KnowledgeSearch: No results found for '{Query}'", query);
            return "No relevant past conversations found for that query.";
        }

        _logger.LogInformation("KnowledgeSearch: Found {Count} results for '{Query}'", results.Count, query);

        // Fetch the actual messages with full context
        var messageIds = results.Select(r => r.Embedding.SourceId).ToList();
        var messages = await dbContext.Messages
            .Where(m => messageIds.Contains(m.Id))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        var formatted = new StringBuilder();
        formatted.AppendLine($"Found {messages.Count} relevant past messages:\n");

        // Note: For production, consider summarizing, chunking, or trimming long messages
        // to avoid exceeding LLM token limits. For this example, we keep messages as-is
        // since chat messages are typically short.
        foreach (var message in messages)
        {
            var role = message.Role == "user" ? "User" : "Assistant";
            formatted.AppendLine($"[{message.CreatedAt:g}] {role}:");
            formatted.AppendLine(message.Content);
            formatted.AppendLine();
        }

        return formatted.ToString();
    }
}
