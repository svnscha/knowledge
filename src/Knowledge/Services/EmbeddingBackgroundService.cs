using Knowledge.Shared.Abstractions;
using Knowledge.Shared.Data;
using Knowledge.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Knowledge.Services;

/// <summary>
/// Background service that processes messages and generates embeddings.
/// Processes one conversation at a time to maintain ordering and avoid overwhelming the embedding API.
/// </summary>
public class EmbeddingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingBackgroundService> _logger;

    /// <summary>
    /// Delay between processing cycles.
    /// </summary>
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Source type identifier for message embeddings.
    /// </summary>
    private const string MessageSourceType = "Message";

    public EmbeddingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmbeddingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Embedding background service starting");

        // Give the application time to start up before processing
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processedCount = await ProcessPendingMessagesAsync(stoppingToken);

                if (processedCount > 0)
                {
                    _logger.LogInformation("Processed {Count} messages for embedding", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing embeddings");
            }

            await Task.Delay(ProcessingDelay, stoppingToken);
        }

        _logger.LogInformation("Embedding background service stopping");
    }

    /// <summary>
    /// Processes messages that don't have embeddings yet.
    /// </summary>
    private async Task<int> ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        var pendingMessages = await GetPendingMessagesAsync(dbContext, cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return 0;
        }

        _logger.LogDebug("Found {Count} messages pending embedding", pendingMessages.Count);

        var processedCount = 0;

        foreach (var message in pendingMessages)
        {
            try
            {
                await ProcessMessageAsync(dbContext, embeddingService, message, cancellationToken);
                processedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to embed message {MessageId}", message.Id);
                // Continue with next message instead of failing the entire batch
            }
        }

        return processedCount;
    }

    /// <summary>
    /// Gets messages that don't have embeddings yet.
    /// </summary>
    private static async Task<List<Message>> GetPendingMessagesAsync(
        KnowledgeDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Simple query: find messages where EmbeddingId is null
        // The partial index on embedding_id makes this efficient
        return await dbContext.Messages
            .Where(m => m.EmbeddingId == null)
            .Where(m => !string.IsNullOrEmpty(m.Content)) // Skip empty messages
            .OrderBy(m => m.SequenceNumber)
            .Take(10) // Process in small batches
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Processes a single message and creates its embedding.
    /// </summary>
    private async Task ProcessMessageAsync(
        KnowledgeDbContext dbContext,
        IEmbeddingService embeddingService,
        Message message,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Embedding message {MessageId} from conversation {ConversationId}",
            message.Id,
            message.ConversationId);

        // Generate embedding for the message content
        var vector = await embeddingService.EmbedAsync(message.Content, cancellationToken);

        // Create and store the embedding record
        var embedding = new Embedding
        {
            SourceType = MessageSourceType,
            SourceId = message.Id,
            Content = message.Content,
            Vector = vector
        };

        dbContext.Embeddings.Add(embedding);

        // Link the message to its embedding
        message.EmbeddingId = embedding.Id;

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Created embedding {EmbeddingId} for message {MessageId}", embedding.Id, message.Id);
    }
}
