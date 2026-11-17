using Apps.BuilderIo.Handlers;
using Blackbird.Applications.Sdk.Common;
using Blackbird.Applications.Sdk.Common.Dynamic;

namespace Apps.BuilderIo.Models.Projects;

public class PromptHistoryRequest : BranchIdentifier
{
    [Display("Maximum prompts", Description = "From 1 to 200. Default: 50. Builder returns user prompts, not agent replies.")]
    public int? MaximumPrompts { get; set; }
}
