using Pgvector;

namespace Knowledge.Shared.Entities;

/// <summary>
/// Stores vector embeddings for semantic search.
/// Uses a polymorphic pattern (SourceType + SourceId) to reference different content types.
/// </summary>
public class Embedding
{
    /// <summary>
    /// Unique identifier for the embedding record.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The type of source entity this embedding was generated from.
    /// Examples: "Message", "DocumentChunk"
    /// </summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the source entity this embedding references.
    /// </summary>
    public Guid SourceId { get; set; }

    /// <summary>
    /// The original text content that was embedded.
    /// Stored for display and re-generation purposes.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The vector embedding (1536 dimensions for text-embedding-3-small).
    /// Uses pgvector's Vector type for efficient similarity search.
    /// </summary>
    public Vector Vector { get; set; } = null!;

    /// <summary>
    /// When the embedding was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the message this embedding belongs to (if source is a Message).
    /// </summary>
    public Message? Message { get; set; }
}
