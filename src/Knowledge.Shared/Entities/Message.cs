namespace Knowledge.Shared.Entities;

/// <summary>
/// Represents a single chat message.
/// Compatible with Microsoft.Extensions.AI.ChatMessage.
/// </summary>
public class Message
{
    /// <summary>
    /// Unique identifier for the message.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// The role of the message author.
    /// Valid values: "user", "assistant", "system", "tool"
    /// Maps to ChatRole.Value from Microsoft.Extensions.AI.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional name of the message author.
    /// Corresponds to ChatMessage.AuthorName.
    /// </summary>
    public string? AuthorName { get; set; }

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number for ordering messages globally.
    /// Auto-assigned by database trigger.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional reference to the embedding generated for this message.
    /// Null until the message has been processed by the embedding service.
    /// </summary>
    public Guid? EmbeddingId { get; set; }

    /// <summary>
    /// Navigation property to the embedding.
    /// </summary>
    public Embedding? Embedding { get; set; }
}
