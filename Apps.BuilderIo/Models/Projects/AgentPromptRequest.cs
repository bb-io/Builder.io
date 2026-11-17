using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class AgentPromptRequest
{
    [Display("Prompt", Description = "Instructions for the Builder agent. It may modify project code and consume Builder credits.")]
    public string Prompt { get; set; } = "";

    [Display("Idempotency key", Description = "Reuse the same unique key when retrying this submission to avoid duplicate runs.")]
    public string? IdempotencyKey { get; set; }
}
