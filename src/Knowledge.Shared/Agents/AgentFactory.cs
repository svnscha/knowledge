using System.ComponentModel;
using Knowledge.Shared.Data;
using Knowledge.Shared.Storage;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Shared.Agents;

/// <summary>
/// Factory for creating AI agents.
/// </summary>
public static class AgentFactory
{
    private const string KnowledgeSystemPrompt = "You are a helpful agent named Knowledge.";

    /// <summary>
    /// Creates the Knowledge agent with database-backed message storage.
    /// </summary>
    public static ChatClientAgent CreateKnowledgeAgent(IChatClient chatClient, IServiceProvider services, string key)
    {
        return chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Id = key,
            Name = key,
            ChatOptions = new ChatOptions
            {
                ConversationId = "global",
                Instructions = KnowledgeSystemPrompt,
            },
            ChatMessageStoreFactory = context =>
            {
                var dbContextFactory = services.GetRequiredService<IDbContextFactory<KnowledgeDbContext>>();
                return new KnowledgeChatMessageStore(dbContextFactory, context.SerializedState, context.JsonSerializerOptions);
            }
        });
    }

    private const string KnowledgeSearchSystemPrompt = @"You are a helpful assistant with access to conversation history search.
AVAILABLE TOOLS:
- SearchConversationHistory: Use when the user asks about past discussions, references previous topics, or needs to recall something mentioned earlier

DECISION FRAMEWORK:
- For general knowledge (math, common facts, definitions): Answer directly without tools
- For recall questions ('did we discuss X?', 'what did I say about Y?'): Use SearchConversationHistory
- For building on past context: Use SearchConversationHistory to refresh your memory
- When unsure if something was discussed: Search first rather than guessing

RESPONSE GUIDELINES:
- When using retrieved information: 'Based on our earlier conversation...'
- Be concise - summarize findings rather than dumping raw results
- If search returns no results, acknowledge you don't recall discussing that topic
- Synthesize multiple messages into coherent context

Remember: You decide when search helps. Don't search reflexively for every question.";

    public static ChatClientAgent CreateKnowledgeSearchAgent(IChatClient chatClient, IServiceProvider services, string key)
    {
        var searchAgent = services.GetRequiredService<KnowledgeSearchAgent>();

        return chatClient.CreateAIAgent(new ChatClientAgentOptions
        {
            Id = key,
            Name = key,
            ChatOptions = new ChatOptions
            {
                ConversationId = "global",
                Instructions = KnowledgeSearchSystemPrompt,
                Tools = [AIFunctionFactory.Create(searchAgent.SearchConversationHistoryAsync)],
                ToolMode = ChatToolMode.Auto
            }
        });
    }
}
