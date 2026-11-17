using Apps.BuilderIo.Actions;
using Apps.BuilderIo.Connections;
using Apps.BuilderIo.Handlers;
using Apps.BuilderIo.Models.Projects;
using Blackbird.Applications.Sdk.Common.Dynamic;
using Microsoft.Extensions.Configuration;
using Tests.BuilderIo.Base;

namespace Tests.BuilderIo;

[TestClass]
public class ProjectActionsTests : TestBase
{
    private readonly IConfiguration config;

    public ProjectActionsTests() : base("ProjectsConnectionDefinition")
    {
        config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: true).Build();
    }

    [TestMethod]
    public async Task ConnectionDetails_ReturnsAuthorizedOrganization()
    {
        var result = await new ProjectActions(Context).GetConnectionDetails();
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.Organization.Id));
    }

    [TestMethod]
    public async Task ConnectionValidator_AcceptsProjectsOAuth()
    {
        var result = await new ConnectionValidator(Context).ValidateConnection(Creds, CancellationToken.None);
        Assert.IsTrue(result.IsValid, result.Message);
    }

    [TestMethod]
    public async Task SearchProjects_FindsSelectedProject()
    {
        var actions = new ProjectActions(Context);
        var project = await actions.GetProject(Project());
        var result = await actions.SearchProjects(new ProjectSearchRequest { Search = project.Name });
        Assert.IsTrue(result.Projects.Any(x => x.ProjectId == project.ProjectId));
    }

    [TestMethod]
    public async Task GetProject_ReturnsSelectedProject()
    {
        var result = await new ProjectActions(Context).GetProject(Project());
        Assert.AreEqual(Required("ProjectId"), result.ProjectId);
    }

    [TestMethod]
    public async Task SearchBranches_FindsSelectedBranch()
    {
        var result = await new ProjectActions(Context).SearchBranches(new BranchSearchRequest { ProjectId = Required("ProjectId"), Search = Required("BranchName") });
        Assert.IsTrue(result.Branches.Any(x => x.Name == Required("BranchName")));
    }

    [TestMethod]
    public async Task GetBranch_ReturnsSelectedBranch()
    {
        var result = await new ProjectActions(Context).GetBranch(Branch());
        Assert.AreEqual(Required("BranchName"), result.Name);
    }

    [TestMethod]
    public async Task GetHistory_ReturnsUserPrompts()
    {
        var result = await new ProjectActions(Context).GetPromptHistory(new PromptHistoryRequest { ProjectId = Required("ProjectId"), BranchName = Required("BranchName"), MaximumPrompts = 10 });
        Assert.AreEqual(Required("BranchName"), result.BranchName);
        Assert.IsNotNull(result.Messages);
    }

    [TestMethod]
    public async Task CreateProject_ReturnsRunId()
    {
        AllowMutation();
        var result = await new ProjectActions(Context).CreateProject(Prompt());
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.RunId));
    }

    [TestMethod]
    public async Task StartTask_ReturnsRunId()
    {
        AllowMutation();
        var result = await new ProjectActions(Context).StartTask(Project(), Prompt(), new NewBranchRequest { NewBranchName = "blackbird-test/" + Guid.NewGuid().ToString("N") });
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.RunId));
    }

    [TestMethod]
    public async Task ContinueTask_ReturnsRunId()
    {
        AllowMutation();
        var result = await new ProjectActions(Context).ContinueTask(Branch(), Prompt());
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.RunId));
    }

    [TestMethod]
    public async Task GetTask_ReturnsSelectedRun()
    {
        var result = await new ProjectActions(Context).GetTask(new RunIdentifier { RunId = Required("RunId") });
        Assert.AreEqual(Required("RunId"), result.RunId);
        CollectionAssert.Contains(new[] { "submitted", "working", "completed", "failed" }, result.Status);
    }

    [TestMethod]
    public async Task ProjectsDropdown_FindsSelectedProject()
    {
        var project = await new ProjectActions(Context).GetProject(Project());
        var result = await new ProjectDataHandler(Context).GetDataAsync(new DataSourceContext { SearchString = project.Name }, CancellationToken.None);
        Assert.IsTrue(result.Any(x => x.Value == project.ProjectId));
    }

    [TestMethod]
    public async Task BranchDropdown_UsesSelectedProject()
    {
        var result = await new BranchDataHandler(Context, Project()).GetDataAsync(new DataSourceContext { SearchString = Required("BranchName") }, CancellationToken.None);
        Assert.IsTrue(result.Any(x => x.Value == Required("BranchName")));
    }

    private ProjectIdentifier Project() => new() { ProjectId = Required("ProjectId") };

    private BranchIdentifier Branch() => new() { ProjectId = Required("ProjectId"), BranchName = Required("BranchName") };

    private AgentPromptRequest Prompt() => new() { Prompt = Required("Prompt"), IdempotencyKey = Guid.NewGuid().ToString("N") };

    private string Required(string key)
    {
        var value = config["ProjectsTest:" + key];
        if (string.IsNullOrWhiteSpace(value))
            Assert.Inconclusive($"Set ProjectsTest:{key} for this live test.");
        return value!;
    }

    private void AllowMutation()
    {
        if (!bool.TryParse(config["ProjectsTest:AllowAgentMutations"], out var allowed) || !allowed)
            Assert.Inconclusive("Agent tests create or modify projects and may incur Builder charges. Set AllowAgentMutations to true only for an isolated test organization.");
    }
}
