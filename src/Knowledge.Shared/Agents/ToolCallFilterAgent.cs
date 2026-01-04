using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Knowledge.Shared.Agents;

/// <summary>
/// A delegating agent that filters out tool call content from responses.
/// This prevents downstream consumers from seeing FunctionCallContent and FunctionResultContent
/// that they cannot execute.
/// </summary>
public sealed class ToolCallFilterAgent : DelegatingAIAgent
{
    public ToolCallFilterAgent(AIAgent innerAgent) : base(innerAgent) { }

    public override async Task<AgentRunResponse> RunAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread,
        AgentRunOptions? options,
        CancellationToken cancellationToken)
    {
        var response = await InnerAgent.RunAsync(messages, thread, options, cancellationToken);
        response.Messages = FilterToolCalls(response.Messages);
        return response;
    }

    public override async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentThread? thread,
        AgentRunOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in InnerAgent.RunStreamingAsync(messages, thread, options, cancellationToken))
        {
            yield return FilterToolCalls(update);
        }
    }

    private static IList<ChatMessage> FilterToolCalls(IEnumerable<ChatMessage> messages) =>
        messages.Select(m => new ChatMessage(m.Role,
            m.Contents.Where(c => c is not FunctionCallContent && c is not FunctionResultContent).ToList()
        )).ToList();

    private static AgentRunResponseUpdate FilterToolCalls(AgentRunResponseUpdate update) =>
        new(update.Role, update.Contents
            .Where(c => c is not FunctionCallContent && c is not FunctionResultContent).ToList());
}
