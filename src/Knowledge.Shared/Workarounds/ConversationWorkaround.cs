namespace Knowledge.Shared.Workarounds;

/// <summary>
/// Generates a per-instance conversation ID as a workaround for DevUI's lack of AgentThread support.
/// </summary>
/// <remarks>
/// See: https://github.com/microsoft/agent-framework/issues/3000
/// </remarks>
public static class ConversationWorkaround
{
    /// <summary>
    /// The conversation ID for the current application instance.
    /// </summary>
    public static Guid CurrentConversationId { get; } = Guid.NewGuid();
}
