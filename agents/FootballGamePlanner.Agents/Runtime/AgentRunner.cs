using FootballGamePlanner.Agents.Llm;

namespace FootballGamePlanner.Agents.Runtime;

/// <summary>How an agent run ended.</summary>
internal enum RunOutcome
{
    /// <summary>The model finished on its own.</summary>
    Completed,

    /// <summary>The run used up its token budget and was stopped.</summary>
    BudgetExceeded,

    /// <summary>The run reached the maximum number of model calls and was stopped.</summary>
    TurnLimitReached,

    /// <summary>The model stopped for another reason, such as hitting its output limit.</summary>
    StoppedUnexpectedly,
}

/// <summary>What an agent is, as the runner sees it.</summary>
internal sealed record AgentSpec(
    string Name,
    string Model,
    string SystemPrompt,
    IReadOnlyList<IAgentTool> Tools,
    int MaxTokensPerCall = 8_000
);

internal sealed record AgentLimits(int MaxTurns, long TokenBudget);

internal sealed record ToolCallRecord(string Name, bool IsError);

internal sealed record AgentRunResult(
    RunOutcome Outcome,
    string FinalText,
    TokenUsage Usage,
    int Turns,
    IReadOnlyList<ToolCallRecord> ToolCalls
);

/// <summary>
/// The tool-use loop shared by every agent: send the conversation, run the tools the model
/// asks for, send the results back, and repeat until the model is done or a limit is hit.
/// It is plain code with no judgement of its own, so it is fully testable with a fake model.
/// </summary>
internal sealed class AgentRunner(IModelClient model)
{
    public async Task<AgentRunResult> RunAsync(
        AgentSpec agent,
        string task,
        AgentLimits limits,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(limits);

        var tools = agent.Tools.ToDictionary(t => t.Definition.Name, StringComparer.Ordinal);
        var definitions = agent.Tools.Select(t => t.Definition).ToList();
        var messages = new List<ConversationMessage> { ConversationMessage.UserText(task) };
        var toolCalls = new List<ToolCallRecord>();
        var usage = default(TokenUsage);
        var finalText = string.Empty;

        for (var turn = 1; turn <= limits.MaxTurns; turn++)
        {
            var response = await model
                .SendAsync(
                    new ModelRequest(agent.Model, agent.SystemPrompt, messages, definitions, agent.MaxTokensPerCall),
                    cancellationToken
                )
                .ConfigureAwait(false);

            usage += response.Usage;
            messages.Add(new ConversationMessage(ConversationRole.Assistant, response.Parts));

            var text = string.Join("\n", response.Parts.OfType<TextPart>().Select(p => p.Text)).Trim();
            if (text.Length > 0)
            {
                finalText = text;
            }

            var calls = response.Parts.OfType<ToolCallPart>().ToList();
            if (response.Stop != StopKind.ToolUse || calls.Count == 0)
            {
                var outcome = response.Stop == StopKind.EndTurn ? RunOutcome.Completed : RunOutcome.StoppedUnexpectedly;
                return new AgentRunResult(outcome, finalText, usage, turn, toolCalls);
            }

            if (usage.Total >= limits.TokenBudget)
            {
                return new AgentRunResult(RunOutcome.BudgetExceeded, finalText, usage, turn, toolCalls);
            }

            var results = new List<ContentPart>();
            foreach (var call in calls)
            {
                var outcome = await ExecuteToolAsync(tools, call, cancellationToken).ConfigureAwait(false);
                toolCalls.Add(new ToolCallRecord(call.Name, outcome.IsError));
                results.Add(new ToolResultPart(call.Id, outcome.Content, outcome.IsError));
            }

            messages.Add(new ConversationMessage(ConversationRole.User, results));
        }

        return new AgentRunResult(RunOutcome.TurnLimitReached, finalText, usage, limits.MaxTurns, toolCalls);
    }

    private static async Task<ToolOutcome> ExecuteToolAsync(
        Dictionary<string, IAgentTool> tools,
        ToolCallPart call,
        CancellationToken cancellationToken
    )
    {
        if (!tools.TryGetValue(call.Name, out var tool))
        {
            return ToolOutcome.Error($"Unknown tool '{call.Name}'. Use only the tools you were given.");
        }

        try
        {
            return await tool.ExecuteAsync(call.Input, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // A failing tool must not end the run; the model gets the error and can adapt.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return ToolOutcome.Error($"The tool failed: {ex.Message}");
        }
    }
}
