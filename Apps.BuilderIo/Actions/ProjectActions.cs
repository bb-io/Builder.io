using Apps.BuilderIo.Api;
using Apps.BuilderIo.Models.Projects;
using Apps.BuilderIo.Utils;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Newtonsoft.Json.Linq;

namespace Apps.BuilderIo.Actions;

[ActionList("Projects")]
public class ProjectActions(InvocationContext context) : BaseInvocable(context)
{
    [Action("Get Projects connection details", Description = "Get the user and organization authorized by the Projects OAuth connection.")]
    public async Task<ProjectsIdentityResponse> GetConnectionDetails()
    {
        using var client = CreateClient();
        return await client.Call<ProjectsIdentityResponse>("Get authenticated user", new { });
    }

    [Action("Search projects", Description = "Find Builder Code projects in the authorized organization. Requires a Projects OAuth connection.")]
    public async Task<ProjectsResponse> SearchProjects([ActionParameter] ProjectSearchRequest input)
    {
        using var client = CreateClient();
        return new ProjectsResponse { Projects = await client.Paginate<ProjectResponse>("List projects", "projects", new { search = Clean(input.Search) }, 10000) };
    }

    [Action("Get project", Description = "Get a Builder Code project's editor URL and main branch.")]
    public async Task<ProjectResponse> GetProject([ActionParameter] ProjectIdentifier input)
    {
        using var client = CreateClient();
        return await client.Call<ProjectResponse>("Get a project", new { projectId = Input.Required(input.ProjectId, "Project ID") });
    }

    [Action("Search project branches", Description = "Find branches within a Builder Code project. Narrow Search if Builder reports incomplete results.")]
    public Task<BranchesResponse> SearchBranches([ActionParameter] BranchSearchRequest input) => ReadBranches(input, 10000);

    [Action("Get project branch", Description = "Get a branch's preview, pull request and agent state.")]
    public async Task<BranchResponse> GetBranch([ActionParameter] BranchIdentifier input)
    {
        using var client = CreateClient();
        return await client.Call<BranchResponse>("Get a branch", new { projectId = Input.Required(input.ProjectId, "Project ID"), branchName = Input.Required(input.BranchName, "Branch name") });
    }

    [Action("Get branch prompt history", Description = "Get user prompts for a branch. Builder does not include agent replies.")]
    public async Task<PromptHistoryResponse> GetPromptHistory([ActionParameter] PromptHistoryRequest input)
    {
        var limit = input.MaximumPrompts ?? 50;
        if (limit is < 1 or > 200)
            throw new PluginMisconfigurationException("Maximum prompts must be between 1 and 200.");
        using var client = CreateClient();
        return await client.Call<PromptHistoryResponse>("Get message history", new { projectId = Input.Required(input.ProjectId, "Project ID"), branchName = Input.Required(input.BranchName, "Branch name"), limit });
    }

    [Action("Create project with agent", Description = "Ask the Builder agent to create a project. Returns a Run ID immediately; use Get project task to check completion. May consume credits.")]
    public Task<AgentRunResponse> CreateProject([ActionParameter] AgentPromptRequest input)
        => Submit(input, null, null, null);

    [Action("Start project task", Description = "Start agent work on a new branch in an existing project. Returns before completion. May consume credits.")]
    public Task<AgentRunResponse> StartTask([ActionParameter] ProjectIdentifier project, [ActionParameter] AgentPromptRequest input, [ActionParameter] NewBranchRequest branch)
        => Submit(input, Input.Required(project.ProjectId, "Project ID"), null, Clean(branch.NewBranchName));

    [Action("Continue branch task", Description = "Ask the agent to continue work on an existing project branch. Returns before completion. May consume credits.")]
    public Task<AgentRunResponse> ContinueTask([ActionParameter] BranchIdentifier branch, [ActionParameter] AgentPromptRequest input)
        => Submit(input, Input.Required(branch.ProjectId, "Project ID"), Input.Required(branch.BranchName, "Branch name"), null);

    [Action("Get project task", Description = "Check a Builder agent run. Completed and failed are terminal statuses; submitted and working need a later check.")]
    public async Task<AgentRunResponse> GetTask([ActionParameter] RunIdentifier input)
    {
        using var client = CreateClient();
        return await client.Call<AgentRunResponse>("Get a Builder Code run", new { runId = Input.Required(input.RunId, "Run ID") });
    }

    public async Task<BranchesResponse> ReadBranches(BranchSearchRequest input, int maximum)
    {
        using var client = CreateClient();
        var data = await client.Call<JObject>("List branches", new { projectId = Input.Required(input.ProjectId, "Project ID"), search = Clean(input.Search) });
        if (data["branches"] is not JArray branches)
            throw new PluginApplicationException("Builder returned no structured branch list. Prose-only summaries cannot be used for branch discovery.");
        if (branches.Count > maximum || data["hasMore"]?.Value<bool>() == true || data["truncated"]?.Value<bool>() == true || data["total"]?.Value<int?>() > branches.Count)
            throw new PluginMisconfigurationException("Builder returned incomplete branch results. Narrow Search before continuing.");
        return data.ToObject<BranchesResponse>()!;
    }

    private ProjectsClient CreateClient() => new(InvocationContext.AuthenticationCredentialsProviders);

    private async Task<AgentRunResponse> Submit(AgentPromptRequest input, string? projectId, string? branchName, string? newBranchName)
    {
        var prompt = Input.Required(input.Prompt, "Prompt");
        using var client = CreateClient();
        var run = await client.Call<AgentRunResponse>("Run the Builder Code agent", new { prompt, projectId, branchName, newBranchName, idempotencyKey = Clean(input.IdempotencyKey) });
        if (string.IsNullOrWhiteSpace(run.RunId))
            throw new PluginApplicationException("Builder accepted the request but returned no usable Run ID. Inspect the project in Builder before retrying to avoid a duplicate run.");
        return run;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
