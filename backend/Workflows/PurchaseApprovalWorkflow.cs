using WorkflowCore.Interface;
using WorkflowCore.Models;
using PurchaseApproval.Models;

namespace PurchaseApproval.Workflows;

/// <summary>
/// 采购审批工作流定义
/// 根据金额分级审批：
/// - ≤5000: 部门经理
/// - 5001-20000: 部门经理 → 财务总监
/// - >20000: 部门经理 → 财务总监 → 总经理
/// </summary>
public class PurchaseApprovalWorkflow : IWorkflow<PurchaseWorkflowData>
{
    public const string WorkflowId = "PurchaseApprovalWorkflow";
    public const string ApprovalEventName = "ApprovalEvent";

    public string Id => WorkflowId;
    public int Version => 1;

    public void Build(IWorkflowBuilder<PurchaseWorkflowData> builder)
    {
        builder
            // 启动步骤
            .StartWith<LogStartStep>()
                .Input(step => step.Data, data => data)
            
            // 等待第一级审批 (部门经理)
            .WaitFor(ApprovalEventName, (data, context) => context.Workflow.Id)
                .Output(data => data.LastAction, step => ((ApprovalEventData)step.EventData).Action)
            
            // 检查第一级审批结果
            .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount <= 5000)
                .Do(then => then
                    .StartWith<MarkCompleteStep>()
                        .Input(step => step.Data, data => data)
                        .Input(step => step.Level, _ => "经理")
                )
            
            .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount > 5000)
                .Do(then => then
                    // 等待第二级审批 (财务总监)
                    .StartWith(context => ExecutionResult.Next())
                    .WaitFor(ApprovalEventName, (data, context) => context.Workflow.Id)
                        .Output(data => data.LastAction, step => ((ApprovalEventData)step.EventData).Action)
                    
                    .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount <= 20000)
                        .Do(then2 => then2
                            .StartWith<MarkCompleteStep>()
                                .Input(step => step.Data, data => data)
                                .Input(step => step.Level, _ => "财务")
                        )
                    
                    .If(data => data.LastAction == ApprovalAction.Approve && data.TotalAmount > 20000)
                        .Do(then2 => then2
                            // 等待第三级审批 (总经理)
                            .StartWith(context => ExecutionResult.Next())
                            .WaitFor(ApprovalEventName, (data, context) => context.Workflow.Id)
                                .Output(data => data.LastAction, step => ((ApprovalEventData)step.EventData).Action)
                            .If(data => data.LastAction == ApprovalAction.Approve)
                                .Do(then3 => then3
                                    .StartWith<MarkCompleteStep>()
                                        .Input(step => step.Data, data => data)
                                        .Input(step => step.Level, _ => "总经理")
                                )
                        )
                )
            
            // 结束步骤
            .Then<LogEndStep>()
                .Input(step => step.Data, data => data);
    }
}

/// <summary>
/// 日志启动步骤
/// </summary>
public class LogStartStep : StepBody
{
    public PurchaseWorkflowData? Data { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        Console.WriteLine($"📋 工作流启动: RequestId={Data?.RequestId}, Amount={Data?.TotalAmount}");
        return ExecutionResult.Next();
    }
}

/// <summary>
/// 标记完成步骤
/// </summary>
public class MarkCompleteStep : StepBody
{
    public PurchaseWorkflowData? Data { get; set; }
    public string Level { get; set; } = "";

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (Data != null)
        {
            Data.IsCompleted = true;
        }
        Console.WriteLine($"✅ 工作流完成 ({Level}审批)");
        return ExecutionResult.Next();
    }
}

/// <summary>
/// 日志结束步骤
/// </summary>
public class LogEndStep : StepBody
{
    public PurchaseWorkflowData? Data { get; set; }

    public override ExecutionResult Run(IStepExecutionContext context)
    {
        if (Data != null && !Data.IsCompleted)
        {
            Console.WriteLine($"❌ 工作流结束 (未通过): Action={Data.LastAction}");
        }
        return ExecutionResult.Next();
    }
}
