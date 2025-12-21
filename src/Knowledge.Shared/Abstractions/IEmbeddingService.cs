using Pgvector;

namespace Knowledge.Shared.Abstractions;

/// <summary>
/// Service for generating vector embeddings from text content.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates a vector embedding for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vector embedding.</returns>
    Task<Vector> EmbedAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates vector embeddings for multiple texts in a batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The vector embeddings in the same order as input texts.</returns>
    Task<IReadOnlyList<Vector>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimension of the embedding vectors produced by this service.
    /// </summary>
    int Dimensions { get; }
}
