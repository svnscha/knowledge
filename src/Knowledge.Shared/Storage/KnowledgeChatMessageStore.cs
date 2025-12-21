using System.Text.Json;
using Knowledge.Shared.Data;
using Knowledge.Shared.Entities;
using Knowledge.Shared.Workarounds;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Knowledge.Shared.Storage;

/// <summary>
/// A PostgreSQL-backed chat message store using Entity Framework Core.
/// </summary>
public sealed class KnowledgeChatMessageStore : ChatMessageStore
{
    private readonly IDbContextFactory<KnowledgeDbContext> _dbContextFactory;

    /// <summary>
    /// Creates a new message store.
    /// </summary>
    public KnowledgeChatMessageStore(IDbContextFactory<KnowledgeDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Creates a message store from serialized state (used when resuming).
    /// </summary>
    public KnowledgeChatMessageStore(
        IDbContextFactory<KnowledgeDbContext> dbContextFactory,
        JsonElement? serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public override async Task AddMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var entityMessages = messages.Select(chatMessage => new Message
        {
            Id = Guid.TryParse(chatMessage.MessageId, out var id) ? id : Guid.NewGuid(),
            ConversationId = ConversationWorkaround.CurrentConversationId,
            Role = chatMessage.Role.Value,
            AuthorName = chatMessage.AuthorName,
            Content = chatMessage.Text ?? string.Empty,
            SequenceNumber = 0, // Trigger auto-assigns
            CreatedAt = chatMessage.CreatedAt?.UtcDateTime ?? DateTime.UtcNow
        });

        dbContext.Messages.AddRange(entityMessages);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var conversationId = ConversationWorkaround.CurrentConversationId;

        var messages = await dbContext.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SequenceNumber)
            .ToListAsync(cancellationToken);

        return messages.Select(m => new ChatMessage
        {
            MessageId = m.Id.ToString(),
            Role = new ChatRole(m.Role),
            AuthorName = m.AuthorName,
            Contents = [new TextContent(m.Content)],
            CreatedAt = new DateTimeOffset(m.CreatedAt, TimeSpan.Zero)
        });
    }

    /// <inheritdoc />
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.SerializeToElement(new
        {
            ConversationId = ConversationWorkaround.CurrentConversationId
        }, jsonSerializerOptions);
    }
}
