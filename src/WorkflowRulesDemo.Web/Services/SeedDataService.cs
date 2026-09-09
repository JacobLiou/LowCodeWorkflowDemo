using WorkflowRulesDemo.Web.Models;

namespace WorkflowRulesDemo.Web.Services;

/// <summary>
/// 内置示例数据：请假审批流程。
/// Start -> RuleActivity(判断请假天数是否 &gt; 3 天) -> Gateway(按 context.approver 路由)
///        -> Task(总监审批) / Task(经理审批) -> End
/// 直观展示“规则命中结果驱动网关路由”这一核心思想。
/// </summary>
public class SeedDataService
{
    private readonly RuleService _ruleService;
    private readonly WorkflowService _workflowService;

    public const string SampleRuleId = "leave-days-rule";
    public const string SampleWorkflowId = "leave-approval-workflow";

    public SeedDataService(RuleService ruleService, WorkflowService workflowService)
    {
        _ruleService = ruleService;
        _workflowService = workflowService;
    }

    public async Task EnsureSeedDataAsync()
    {
        var existingRules = await _ruleService.GetAllAsync();
        if (!existingRules.Any(r => r.Id == SampleRuleId))
        {
            var rule = new RuleDefinition
            {
                Id = SampleRuleId,
                Name = "请假天数超3天需总监审批",
                Description = "当请假天数(days)大于3天时，命中并将 approver 置为 director，供网关路由到总监审批分支；未命中则走默认分支（经理审批）。",
                ConditionGroup = new ConditionGroup
                {
                    Logic = LogicOperator.And,
                    Rules = new List<ConditionRule>
                    {
                        new() { Field = "days", Operator = CompareOperator.GreaterThan, Value = "3" }
                    }
                },
                Actions = new List<RuleAction>
                {
                    new() { Field = "approver", Value = "director" }
                }
            };
            await _ruleService.SaveAsync(rule);
        }

        var existingWorkflows = await _workflowService.GetAllAsync();
        if (!existingWorkflows.Any(w => w.Id == SampleWorkflowId))
        {
            const string startId = "n-start";
            const string ruleNodeId = "n-rule-days";
            const string gatewayId = "n-gateway-approver";
            const string directorTaskId = "n-task-director";
            const string managerTaskId = "n-task-manager";
            const string endId = "n-end";

            var workflow = new WorkflowDefinition
            {
                Id = SampleWorkflowId,
                Name = "请假审批流程",
                Version = "1.0",
                Description = "示例流程：演示 RuleActivity 命中结果驱动 Gateway 路由。",
                Nodes = new List<WorkflowNode>
                {
                    new() { Id = startId, Type = NodeType.Start, Name = "提交请假申请" },
                    new() { Id = ruleNodeId, Type = NodeType.RuleActivity, Name = "判断是否需要总监审批" },
                    new() { Id = gatewayId, Type = NodeType.Gateway, Name = "按审批人路由" },
                    new() { Id = directorTaskId, Type = NodeType.Task, Name = "总监审批" },
                    new() { Id = managerTaskId, Type = NodeType.Task, Name = "经理审批" },
                    new() { Id = endId, Type = NodeType.End, Name = "流程结束" }
                },
                Edges = new List<WorkflowEdge>
                {
                    new() { FromNodeId = startId, ToNodeId = ruleNodeId },
                    new() { FromNodeId = ruleNodeId, ToNodeId = gatewayId },
                    new()
                    {
                        FromNodeId = gatewayId, ToNodeId = directorTaskId,
                        Condition = "context.approver == 'director'", Label = "天数>3，需总监审批"
                    },
                    new()
                    {
                        FromNodeId = gatewayId, ToNodeId = managerTaskId,
                        Condition = null, Label = "默认：经理审批"
                    },
                    new() { FromNodeId = directorTaskId, ToNodeId = endId },
                    new() { FromNodeId = managerTaskId, ToNodeId = endId }
                }
            };

            workflow.Nodes.First(n => n.Id == ruleNodeId).SetRuleActivityConfig(new RuleActivityConfig
            {
                RuleId = SampleRuleId
            });

            await _workflowService.SaveAsync(workflow);
        }
    }

    public static string SampleInitialContextJson =>
        "{\n  \"applicant\": \"张三\",\n  \"days\": 5\n}";
}
