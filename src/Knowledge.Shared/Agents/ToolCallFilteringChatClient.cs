using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Knowledge.Shared.Agents;

/// <summary>
/// A delegating chat client that filters out tool call content from responses.
/// This is useful when the upstream model uses tools internally but the downstream
/// consumer doesn't have access to those tools.
/// </summary>
public class ToolCallFilteringChatClient : DelegatingChatClient
{
    private readonly ILogger<ToolCallFilteringChatClient>? _logger;

    public ToolCallFilteringChatClient(IChatClient innerClient, ILogger<ToolCallFilteringChatClient>? logger = null)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        // Filter and transform the response messages
        var filteredMessages = new List<ChatMessage>();
        foreach (var message in response.Messages)
        {
            var filtered = FilterMessage(message);
            if (filtered != null)
            {
                filteredMessages.Add(filtered);
            }
        }

        return new ChatResponse(filteredMessages)
        {
            CreatedAt = response.CreatedAt,
            FinishReason = response.FinishReason,
            ModelId = response.ModelId,
            RawRepresentation = response.RawRepresentation,
            ResponseId = response.ResponseId,
            Usage = response.Usage
        };
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            _logger?.LogDebug("ToolCallFilter: Received update with {ContentCount} contents, Role={Role}, FinishReason={FinishReason}",
                update.Contents.Count, update.Role, update.FinishReason);

            foreach (var content in update.Contents)
            {
                _logger?.LogDebug("ToolCallFilter: Content type={Type}", content.GetType().Name);
            }

            var filtered = FilterUpdate(update);
            if (filtered != null)
            {
                _logger?.LogDebug("ToolCallFilter: Yielding filtered update with {ContentCount} contents", filtered.Contents.Count);
                yield return filtered;
            }
            else
            {
                _logger?.LogDebug("ToolCallFilter: Skipping update (filtered to null)");
            }
        }
    }

    private static ChatMessage? FilterMessage(ChatMessage message)
    {
        // Skip tool call messages entirely
        if (message.Role == ChatRole.Assistant)
        {
            var hasToolCalls = message.Contents.Any(c => c is FunctionCallContent);
            var hasToolResults = message.Contents.Any(c => c is FunctionResultContent);

            if (hasToolCalls || hasToolResults)
            {
                // Filter out tool-related content, keep only non-tool content
                var filteredContent = message.Contents
                    .Where(c => c is not FunctionCallContent and not FunctionResultContent)
                    .ToList();

                if (filteredContent.Count == 0)
                {
                    return null;
                }

                return new ChatMessage(message.Role, filteredContent);
            }
        }

        // Skip tool role messages entirely
        if (message.Role == ChatRole.Tool)
        {
            return null;
        }

        return message;
    }

    private static ChatResponseUpdate? FilterUpdate(ChatResponseUpdate update)
    {
        // Check if this update contains tool calls or results
        var hasToolCalls = update.Contents.Any(c => c is FunctionCallContent);
        var hasToolResults = update.Contents.Any(c => c is FunctionResultContent);

        if (hasToolCalls || hasToolResults)
        {
            // Filter out tool-related content, keep only non-tool content
            var filteredContents = update.Contents
                .Where(c => c is not FunctionCallContent and not FunctionResultContent)
                .ToList();

            // If there's no non-tool content, skip this update entirely
            if (filteredContents.Count == 0)
            {
                return null;
            }

            return new ChatResponseUpdate
            {
                Contents = filteredContents,
                CreatedAt = update.CreatedAt,
                FinishReason = update.FinishReason,
                ModelId = update.ModelId,
                RawRepresentation = update.RawRepresentation,
                ResponseId = update.ResponseId,
                Role = update.Role
            };
        }

        // Check if this is from a tool role - skip it
        if (update.Role == ChatRole.Tool)
        {
            return null;
        }

        return update;
    }
}
