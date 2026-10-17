using System.Text.Json;
using FootballGamePlanner.Agents.Llm;
using FootballGamePlanner.Agents.Runtime;

namespace FootballGamePlanner.Agents.Tools;

/// <summary>
/// Read-only access to the Markdown documents under docs/. Paths are resolved and checked so
/// an agent cannot read anything outside that folder, whatever path it asks for.
/// </summary>
internal sealed class DocumentStore(string docsDirectory)
{
    private const int MaxDocumentChars = 200_000;

    private readonly string _root = Path.GetFullPath(docsDirectory);

    public IReadOnlyList<string> List() =>
        Directory.Exists(_root)
            ?
            [
                .. Directory
                    .EnumerateFiles(_root, "*.md", SearchOption.AllDirectories)
                    .Select(f => Path.GetRelativePath(_root, f).Replace('\\', '/'))
                    .Order(StringComparer.Ordinal),
            ]
            : [];

    /// <summary>Returns the document text, or null when the path is not an allowed document.</summary>
    public string? Read(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);
        if (Path.IsPathRooted(relativePath) || !relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var inside = fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
        if (!inside || !File.Exists(fullPath))
        {
            return null;
        }

        var text = File.ReadAllText(fullPath);
        return text.Length <= MaxDocumentChars ? text : text[..MaxDocumentChars] + "\n\n[truncated]";
    }
}

internal sealed class ListDocumentsTool(DocumentStore store) : IAgentTool
{
    public ToolDefinition Definition { get; } =
        new(
            "list_documents",
            "Lists the Markdown documents under docs/ that you can read, such as brief.md and gdpr.md.",
            new Dictionary<string, JsonElement>(),
            []
        );

    public Task<ToolOutcome> ExecuteAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken
    )
    {
        var documents = store.List();
        return Task.FromResult(
            documents.Count == 0
                ? ToolOutcome.Error("No documents found under docs/.")
                : ToolOutcome.Ok(string.Join("\n", documents))
        );
    }
}

internal sealed class ReadDocumentTool(DocumentStore store) : IAgentTool
{
    public ToolDefinition Definition { get; } =
        new(
            "read_document",
            "Reads one Markdown document under docs/. Pass the path relative to docs/, for example "
                + "\"brief.md\" or \"adr/README.md\".",
            new Dictionary<string, JsonElement>
            {
                ["path"] = ToolInput.StringProperty("Path relative to docs/, ending in .md"),
            },
            ["path"]
        );

    public Task<ToolOutcome> ExecuteAsync(
        IReadOnlyDictionary<string, JsonElement> input,
        CancellationToken cancellationToken
    )
    {
        var path = ToolInput.GetString(input, "path");
        var text = path is null ? null : store.Read(path);
        return Task.FromResult(
            text is null
                ? ToolOutcome.Error($"'{path}' is not a readable document. Call list_documents to see the documents.")
                : ToolOutcome.Ok(text)
        );
    }
}
