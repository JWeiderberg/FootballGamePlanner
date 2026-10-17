using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FootballGamePlanner.Agents.GitHub;

/// <summary>
/// <see cref="IGitHubClient"/> over the GitHub REST API with a plain <see cref="HttpClient"/>.
/// The agent system needs only a handful of endpoints, so this avoids a client library.
/// </summary>
internal sealed class GitHubRestClient : IGitHubClient
{
    private const int PageSize = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly string _repoPath;

    /// <param name="http">A client whose lifetime the caller owns.</param>
    /// <param name="owner">Repository owner, for example JWeiderberg.</param>
    /// <param name="repository">Repository name, for example FootballGamePlanner.</param>
    /// <param name="token">A token with read and write access to issues in this repository only.</param>
    public GitHubRestClient(HttpClient http, string owner, string repository, string token)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
        _http.BaseAddress ??= new Uri("https://api.github.com/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FootballGamePlanner-Agents", "1.0"));
        _repoPath = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}";
    }

    public async Task<IReadOnlyList<GitHubIssue>> ListOpenIssuesAsync(
        string label,
        CancellationToken cancellationToken
    )
    {
        var result = new List<GitHubIssue>();
        for (var page = 1; ; page++)
        {
            var url =
                $"{_repoPath}/issues?state=open&labels={Uri.EscapeDataString(label)}&per_page={PageSize}&page={page}";
            var issues =
                await _http.GetFromJsonAsync<List<IssueDto>>(url, JsonOptions, cancellationToken).ConfigureAwait(false)
                ?? [];

            result.AddRange(issues.Where(i => i.PullRequest is null).Select(ToIssue));
            if (issues.Count < PageSize)
            {
                return result;
            }
        }
    }

    public async Task<GitHubIssue> CreateIssueAsync(
        string title,
        string body,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken
    )
    {
        using var response = await _http
            .PostAsJsonAsync($"{_repoPath}/issues", new CreateIssueDto(title, body, labels), JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var issue =
            await response.Content.ReadFromJsonAsync<IssueDto>(JsonOptions, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("GitHub returned an empty issue.");
        return ToIssue(issue);
    }

    public async Task AddLabelsAsync(int issueNumber, IReadOnlyList<string> labels, CancellationToken cancellationToken)
    {
        using var response = await _http
            .PostAsJsonAsync($"{_repoPath}/issues/{issueNumber}/labels", new LabelsDto(labels), JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveLabelAsync(int issueNumber, string label, CancellationToken cancellationToken)
    {
        using var response = await _http
            .DeleteAsync($"{_repoPath}/issues/{issueNumber}/labels/{Uri.EscapeDataString(label)}", cancellationToken)
            .ConfigureAwait(false);

        // A label that is already gone is fine.
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task AddCommentAsync(int issueNumber, string body, CancellationToken cancellationToken)
    {
        using var response = await _http
            .PostAsJsonAsync($"{_repoPath}/issues/{issueNumber}/comments", new CommentDto(body), JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureLabelAsync(LabelDefinition label, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(label);

        using var existing = await _http
            .GetAsync($"{_repoPath}/labels/{Uri.EscapeDataString(label.Name)}", cancellationToken)
            .ConfigureAwait(false);
        if (existing.IsSuccessStatusCode)
        {
            return;
        }

        using var response = await _http
            .PostAsJsonAsync($"{_repoPath}/labels", new LabelDto(label.Name, label.Color, label.Description), JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static GitHubIssue ToIssue(IssueDto dto) =>
        new(
            dto.Number,
            dto.Title,
            dto.Body ?? string.Empty,
            dto.User?.Login ?? string.Empty,
            [.. (dto.Labels ?? []).Select(l => l.Name)]
        );

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"GitHub returned {(int)response.StatusCode} for {response.RequestMessage?.Method} "
                + $"{response.RequestMessage?.RequestUri}: {detail}",
            inner: null,
            response.StatusCode
        );
    }

    private sealed record IssueDto(
        int Number,
        string Title,
        string? Body,
        UserDto? User,
        List<LabelNameDto>? Labels,
        JsonElement? PullRequest
    );

    private sealed record UserDto(string Login);

    private sealed record LabelNameDto(string Name);

    private sealed record CreateIssueDto(string Title, string Body, IReadOnlyList<string> Labels);

    private sealed record LabelsDto(IReadOnlyList<string> Labels);

    private sealed record CommentDto(string Body);

    private sealed record LabelDto(string Name, string Color, string Description);
}
