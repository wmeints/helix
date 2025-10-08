using Microsoft.SemanticKernel;
using SpecForge.Agents;

namespace Helix.Filters;

public class FunctionCallReportingFilter: IFunctionInvocationFilter
{
    public CodingAgentCallContext? CallContext { get; set; }

    public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        CallContext?.UpdateStatus($"Calling {context.Function.Name}");
        return Task.CompletedTask;
    }
}
