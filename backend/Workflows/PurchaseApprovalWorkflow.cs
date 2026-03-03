using WorkflowCore.Interface;
using WorkflowCore.Models;
using PurchaseApproval.Models;

namespace PurchaseApproval.Workflows;

public class PurchaseApprovalWorkflow : IWorkflow<PurchaseWorkflowData>
{
    public const string WorkflowId = "PurchaseApprovalWorkflow";
    public const string ApprovalEventName = "ApprovalEvent";

    public string Id => WorkflowId;
    public int Version => 1;

    public void Build(IWorkflowBuilder<PurchaseWorkflowData> builder)
    {
        builder
            .StartWith<LogStartStep>()
                .Input(step => step.Data, data => data)

            .WaitFor(ApprovalEventName, (_, context) => context.Workflow.Id)
                .Output(data => data.LastAction, step => ((ApprovalEventData)step.EventData).Action)

            .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount <= 5000)
                .Do(then => then
                    .StartWith<MarkCompleteStep>()
                        .Input(step => step.Data, data => data)
                        .Input(step => step.Level, _ => "经理")
                )

            .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount > 5000)
                .Do(then => then
                    .StartWith(_ => ExecutionResult.Next())
                    .WaitFor(ApprovalEventName, (_, context) => context.Workflow.Id)
                        .Output(data => data.LastAction, step => ((ApprovalEventData)step.EventData).Action)

                    .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount <= 20000)
                        .Do(then2 => then2
                            .StartWith<MarkCompleteStep>()
                                .Input(step => step.Data, data => data)
                                .Input(step => step.Level, _ => "财务")
                        )

                    .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount > 20000)
                        .Do(then2 => then2
                            .StartWith(_ => ExecutionResult.Next())
                            .WaitFor(ApprovalEventName, (_, context) => context.Workflow.Id)
                                .Output(data => data.LastAction, step => ((ApprovalEventData)step.EventData).Action)
                            .If(data => data.LastAction == ApprovalAction.Approve)
                                .Do(then3 => then3
                                    .StartWith<MarkCompleteStep>()
                                        .Input(step => step.Data, data => data)
                                        .Input(step => step.Level, _ => "总经理")
                                )
                        )
                )

            .Then<LogEndStep>()
                .Input(step => step.Data, data => data);
    }
}

public class LogStartStep : StepBody
{
    public PurchaseWorkflowData? Data { get; set; }
    public ILogger<LogStartStep>? Logger { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Logger?.LogInformation(
            "工作流启动: workflowId={WorkflowId}, requestId={RequestId}, totalAmount={TotalAmount}, correlationId={CorrelationId}",
            context.Workflow.Id,
            Data?.RequestId,
            Data?.TotalAmount,
            Data?.CorrelationId);
        return ExecutionResult.Next();
    }
}

public class MarkCompleteStep : StepBody
{
    public PurchaseWorkflowData? Data { get; set; }
    public string Level { get; set; } = string.Empty;
    public ILogger<MarkCompleteStep>? Logger { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (Data != null)
        {
            Data.IsCompleted = true;
        }

        Logger?.LogInformation(
            "工作流审批完成: workflowId={WorkflowId}, requestId={RequestId}, level={Level}, correlationId={CorrelationId}",
            context.Workflow.Id,
            Data?.RequestId,
            Level,
            Data?.CorrelationId);

        return ExecutionResult.Next();
    }
}

public class LogEndStep : StepBody
{
    public PurchaseWorkflowData? Data { get; set; }
    public ILogger<LogEndStep>? Logger { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (Data != null && !Data.IsCompleted)
        {
            Logger?.LogWarning(
                "工作流结束未通过: workflowId={WorkflowId}, requestId={RequestId}, lastAction={LastAction}, correlationId={CorrelationId}",
                context.Workflow.Id,
                Data.RequestId,
                Data.LastAction,
                Data.CorrelationId);
        }
        else
        {
            Logger?.LogInformation(
                "工作流结束: workflowId={WorkflowId}, requestId={RequestId}, completed={Completed}, correlationId={CorrelationId}",
                context.Workflow.Id,
                Data?.RequestId,
                Data?.IsCompleted,
                Data?.CorrelationId);
        }

        return ExecutionResult.Next();
    }
}
