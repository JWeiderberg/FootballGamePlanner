using Anthropic;
using Anthropic.Models.Messages;

namespace FootballGamePlanner.Agents.Llm;

/// <summary>
/// <see cref="IModelClient"/> on top of Anthropic's official C# SDK. Prompt caching is
/// switched on for every request, because the system prompt and the documents an agent
/// reads repeat on every turn of its loop.
/// </summary>
internal sealed class AnthropicModelClient(AnthropicClient client) : IModelClient
{
    public async Task<ModelResponse> SendAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = new MessageCreateParams
        {
            Model = request.Model,
            MaxTokens = request.MaxTokens,
            System = request.SystemPrompt,
            CacheControl = new CacheControlEphemeral(),
            Tools = [.. request.Tools.Select(ToSdkTool)],
            Messages = [.. request.Messages.Select(ToSdkMessage)],
        };

        Message message = await client.Messages.Create(parameters, cancellationToken).ConfigureAwait(false);

        var parts = new List<ContentPart>();
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var text))
            {
                parts.Add(new TextPart(text.Text));
            }
            else if (block.TryPickToolUse(out var toolUse))
            {
                parts.Add(new ToolCallPart(toolUse.ID, toolUse.Name, toolUse.Input));
            }
        }

        var usage = new TokenUsage(
            message.Usage.InputTokens,
            message.Usage.OutputTokens,
            message.Usage.CacheReadInputTokens ?? 0,
            message.Usage.CacheCreationInputTokens ?? 0
        );

        return new ModelResponse(parts, ToStopKind(message.StopReason?.Raw()), usage);
    }

    private static StopKind ToStopKind(string? stopReason) =>
        stopReason switch
        {
            "end_turn" => StopKind.EndTurn,
            "tool_use" => StopKind.ToolUse,
            "max_tokens" => StopKind.MaxTokens,
            _ => StopKind.Other,
        };

    private static ToolUnion ToSdkTool(ToolDefinition tool) =>
        new Tool
        {
            Name = tool.Name,
            Description = tool.Description,
            InputSchema = new InputSchema
            {
                Properties = tool.Properties,
                Required = [.. tool.Required],
            },
        };

    private static MessageParam ToSdkMessage(ConversationMessage message)
    {
        var blocks = new List<ContentBlockParam>();
        foreach (var part in message.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    blocks.Add(new TextBlockParam { Text = text.Text });
                    break;
                case ToolCallPart call:
                    blocks.Add(
                        new ToolUseBlockParam
                        {
                            ID = call.Id,
                            Name = call.Name,
                            Input = call.Input,
                        }
                    );
                    break;
                case ToolResultPart result:
                    blocks.Add(
                        new ToolResultBlockParam
                        {
                            ToolUseID = result.ToolCallId,
                            Content = result.Content,
                            IsError = result.IsError,
                        }
                    );
                    break;
                default:
                    throw new InvalidOperationException($"Unknown content part {part.GetType().Name}.");
            }
        }

        return new MessageParam
        {
            Role = message.Role == ConversationRole.User ? Role.User : Role.Assistant,
            Content = blocks,
        };
    }
}
