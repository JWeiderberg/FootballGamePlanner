using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Runtime;

namespace FootballGamePlanner.Agents.Tests;

public sealed class AgentRunnerTests
{
    private static readonly AgentLimits Generous = new(MaxTurns: 10, TokenBudget: 1_000_000);

    private static AgentSpec Agent(params IAgentTool[] tools) => new("test", "model-x", "system prompt", tools);

    [Fact]
    public async Task Completes_when_the_model_ends_its_turn()
    {
        var model = new FakeModelClient(FakeModelClient.Text("All done.", tokens: 42));

        var result = await new AgentRunner(model).RunAsync(Agent(), "Do it", Generous, CancellationToken.None);

        Assert.Equal(RunOutcome.Completed, result.Outcome);
        Assert.Equal("All done.", result.FinalText);
        Assert.Equal(1, result.Turns);
        Assert.Equal(42, result.Usage.Total);
    }

    [Fact]
    public async Task Sends_the_system_prompt_model_and_tools_with_every_call()
    {
        var model = new FakeModelClient(FakeModelClient.Text("ok"));

        await new AgentRunner(model).RunAsync(Agent(new EchoTool()), "Do it", Generous, CancellationToken.None);

        var request = Assert.Single(model.Requests);
        Assert.Equal("model-x", request.Model);
        Assert.Equal("system prompt", request.SystemPrompt);
        Assert.Equal("echo", Assert.Single(request.Tools).Name);
        var first = Assert.Single(request.Messages);
        Assert.Equal(ConversationRole.User, first.Role);
        Assert.Equal("Do it", Assert.IsType<TextPart>(Assert.Single(first.Parts)).Text);
    }

    [Fact]
    public async Task Runs_a_requested_tool_and_sends_the_result_back()
    {
        var model = new FakeModelClient(
            FakeModelClient.ToolCall("echo", new { text = "hi" }, id: "abc"),
            FakeModelClient.Text("Finished.")
        );

        var result = await new AgentRunner(model).RunAsync(Agent(new EchoTool()), "Go", Generous, CancellationToken.None);

        Assert.Equal(RunOutcome.Completed, result.Outcome);
        Assert.Equal(2, result.Turns);
        var call = Assert.Single(result.ToolCalls);
        Assert.Equal("echo", call.Name);
        Assert.False(call.IsError);

        var followUp = model.Requests[1].Messages;
        Assert.Equal(3, followUp.Count);
        Assert.Equal(ConversationRole.Assistant, followUp[1].Role);
        var toolResult = Assert.IsType<ToolResultPart>(Assert.Single(followUp[2].Parts));
        Assert.Equal("abc", toolResult.ToolCallId);
        Assert.Equal("echo: hi", toolResult.Content);
        Assert.False(toolResult.IsError);
    }

    [Fact]
    public async Task An_unknown_tool_gives_the_model_an_error_instead_of_failing_the_run()
    {
        var model = new FakeModelClient(
            FakeModelClient.ToolCall("delete_everything", new { }),
            FakeModelClient.Text("Sorry.")
        );

        var result = await new AgentRunner(model).RunAsync(Agent(new EchoTool()), "Go", Generous, CancellationToken.None);

        Assert.Equal(RunOutcome.Completed, result.Outcome);
        Assert.True(Assert.Single(result.ToolCalls).IsError);
        var toolResult = Assert.IsType<ToolResultPart>(Assert.Single(model.Requests[1].Messages[2].Parts));
        Assert.True(toolResult.IsError);
        Assert.Contains("Unknown tool", toolResult.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_tool_that_throws_gives_the_model_an_error_instead_of_failing_the_run()
    {
        var model = new FakeModelClient(
            FakeModelClient.ToolCall("echo", new { text = "boom" }),
            FakeModelClient.Text("Recovered.")
        );

        var result = await new AgentRunner(model).RunAsync(Agent(new EchoTool()), "Go", Generous, CancellationToken.None);

        Assert.Equal(RunOutcome.Completed, result.Outcome);
        var toolResult = Assert.IsType<ToolResultPart>(Assert.Single(model.Requests[1].Messages[2].Parts));
        Assert.True(toolResult.IsError);
        Assert.Contains("boom happened", toolResult.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stops_when_the_token_budget_is_used_up()
    {
        var model = new FakeModelClient(
            FakeModelClient.ToolCall("echo", new { text = "a" }, tokens: 600),
            FakeModelClient.ToolCall("echo", new { text = "b" }, tokens: 600),
            FakeModelClient.Text("never reached")
        );

        var result = await new AgentRunner(model).RunAsync(
            Agent(new EchoTool()),
            "Go",
            new AgentLimits(MaxTurns: 10, TokenBudget: 1_000),
            CancellationToken.None
        );

        Assert.Equal(RunOutcome.BudgetExceeded, result.Outcome);
        Assert.Equal(2, model.Requests.Count);
        Assert.Equal(1_200, result.Usage.Total);
    }

    [Fact]
    public async Task Stops_at_the_turn_limit()
    {
        var model = new FakeModelClient(
            FakeModelClient.ToolCall("echo", new { text = "1" }),
            FakeModelClient.ToolCall("echo", new { text = "2" }),
            FakeModelClient.ToolCall("echo", new { text = "3" })
        );

        var result = await new AgentRunner(model).RunAsync(
            Agent(new EchoTool()),
            "Go",
            new AgentLimits(MaxTurns: 2, TokenBudget: 1_000_000),
            CancellationToken.None
        );

        Assert.Equal(RunOutcome.TurnLimitReached, result.Outcome);
        Assert.Equal(2, model.Requests.Count);
    }

    [Fact]
    public async Task Hitting_the_output_limit_is_reported_as_an_unexpected_stop()
    {
        var model = new FakeModelClient(
            new ModelResponse([new TextPart("cut off")], StopKind.MaxTokens, new TokenUsage(10, 10, 0, 0))
        );

        var result = await new AgentRunner(model).RunAsync(Agent(), "Go", Generous, CancellationToken.None);

        Assert.Equal(RunOutcome.StoppedUnexpectedly, result.Outcome);
    }
}
