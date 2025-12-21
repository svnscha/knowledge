using Knowledge.Shared.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace Knowledge.Shared.Services;

/// <summary>
/// Embedding service implementation using Microsoft.Extensions.AI.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<EmbeddingService> _logger;

    /// <summary>
    /// Default dimensions for text-embedding-3-small.
    /// </summary>
    public const int DefaultDimensions = 1536;

    public EmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<EmbeddingService> logger)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public int Dimensions => DefaultDimensions;

    /// <inheritdoc />
    public async Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or whitespace.", nameof(text));
        }

        _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

        var result = await _embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
        var vector = result.Vector.ToArray();

        _logger.LogDebug("Generated embedding with {Dimensions} dimensions", vector.Length);

        return new Vector(vector);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vector>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();

        if (textList.Count == 0)
        {
            return Array.Empty<Vector>();
        }

        _logger.LogDebug("Generating embeddings for {Count} texts", textList.Count);

        var results = await _embeddingGenerator.GenerateAsync(textList, cancellationToken: cancellationToken);

        var vectors = results
            .Select(e => new Vector(e.Vector.ToArray()))
            .ToList();

        _logger.LogDebug("Generated {Count} embeddings", vectors.Count);

        return vectors;
    }
}
