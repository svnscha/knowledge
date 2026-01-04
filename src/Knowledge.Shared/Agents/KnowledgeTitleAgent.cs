using Knowledge.Shared.Data;
using Knowledge.Shared.Storage;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Shared.Agents;

/// <summary>
/// Simple agent for generating conversation titles.
/// No tools, no embeddings - just a basic helpful assistant.
/// Designed for use with LibreChat's title generation feature.
/// </summary>
public static class KnowledgeTitleAgent
{
    private const string TitleSystemPrompt = @"You are a helpful assistant that generates concise, descriptive titles for conversations.
When given conversation content, create a brief title (3-7 words) that captures the main topic or purpose.
Be specific and informative. Avoid generic titles like 'Chat' or 'Conversation'.";

    /// <summary>
    /// Creates a title generation agent.
    /// </summary>
    public static ChatClientAgent Create(IChatClient chatClient, IServiceProvider services, string key)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Id = key,
            Name = key,
            ChatOptions = new ChatOptions
            {
                ConversationId = "global",
                Instructions = TitleSystemPrompt,
            }
        });
    }
}
