using System.Text.Json;
using WorkflowRulesDemo.Web.Models;

namespace WorkflowRulesDemo.Web.Engine;

/// <summary>单步执行日志，供 UI 以时间线形式展示。</summary>
public class ExecutionLogEntry
{
    public int Step { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public NodeType NodeType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ContextBeforeJson { get; set; }
    public string? ContextAfterJson { get; set; }
    public List<string>? RuleDetails { get; set; }
    public string? RouteDecision { get; set; }
}

/// <summary>流程执行的最终结果。</summary>
public class WorkflowExecutionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<ExecutionLogEntry> Logs { get; set; } = new();
    public Dictionary<string, object> FinalContext { get; set; } = new();
}

/// <summary>
/// 自研状态机式流程执行器：从 Start 出发，按 Edges 遍历节点，
/// 在 RuleActivity 节点调用 RuleEngine 驱动 context，在 Gateway 节点按 context 路由。
/// </summary>
public class WorkflowEngine
{
    private readonly RuleEngine _ruleEngine;

    public WorkflowEngine(RuleEngine ruleEngine)
    {
        _ruleEngine = ruleEngine;
    }

    public WorkflowExecutionResult Execute(
        WorkflowDefinition workflow,
        Dictionary<string, object> initialContext,
        IReadOnlyDictionary<string, RuleDefinition> ruleLookup,
        int maxSteps = 100)
    {
        var result = new WorkflowExecutionResult();
        var context = new Dictionary<string, object>(initialContext);

        var startNode = workflow.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
        if (startNode is null)
        {
            result.Success = false;
            result.Error = "流程缺少 Start 节点，无法执行。";
            return result;
        }

        var current = startNode;
        var step = 0;
        var visitedEdgeCount = 0; // 防止环路导致死循环

        while (current is not null)
        {
            step++;
            if (step > maxSteps)
            {
                result.Success = false;
                result.Error = $"超过最大步数 {maxSteps}，可能存在环路，已终止执行。";
                break;
            }

            var beforeJson = Serialize(context);
            var log = new ExecutionLogEntry
            {
                Step = step,
                NodeId = current.Id,
                NodeName = current.Name,
                NodeType = current.Type,
                ContextBeforeJson = beforeJson
            };

            switch (current.Type)
            {
                case NodeType.Start:
                    log.Message = $"流程开始：{current.Name}";
                    break;

                case NodeType.Task:
                    log.Message = $"执行任务节点「{current.Name}」（demo 中仅记录日志，不含真实业务逻辑）。";
                    break;

                case NodeType.End:
                    log.Message = $"流程结束：{current.Name}";
                    break;

                case NodeType.RuleActivity:
                    {
                        var config = current.GetRuleActivityConfig();
                        if (string.IsNullOrWhiteSpace(config.RuleId) || !ruleLookup.TryGetValue(config.RuleId, out var rule))
                        {
                            log.Message = $"规则节点「{current.Name}」未绑定有效规则（RuleId={config.RuleId}），跳过。";
                            break;
                        }

                        // InputMapping: 把 context 中的字段映射为规则输入字段名（若未配置映射，规则直接读取同名字段）。
                        var ruleInputContext = new Dictionary<string, object>(context);
                        foreach (var (ruleField, contextField) in config.InputMapping)
                        {
                            if (context.TryGetValue(contextField, out var v))
                            {
                                ruleInputContext[ruleField] = v;
                            }
                        }

                        var ruleResult = _ruleEngine.Execute(rule, ruleInputContext);

                        // 把规则可能修改的字段写回真实 context（应用 OutputMapping 重命名）。
                        foreach (var kv in ruleInputContext)
                        {
                            var targetField = config.OutputMapping.TryGetValue(kv.Key, out var mapped) ? mapped : kv.Key;
                            context[targetField] = kv.Value;
                        }

                        log.RuleDetails = ruleResult.Details;
                        log.Message = ruleResult.Matched
                            ? $"规则节点「{current.Name}」调用规则「{ruleResult.RuleName}」命中，写回 context: " +
                              string.Join(", ", ruleResult.AppliedActions.Select(a => $"{a.Field}={a.Value}"))
                            : $"规则节点「{current.Name}」调用规则「{ruleResult.RuleName}」未命中，context 未变化。";
                        break;
                    }

                case NodeType.Gateway:
                    log.Message = $"网关节点「{current.Name}」根据 context 判断路由分支。";
                    break;
            }

            log.ContextAfterJson = Serialize(context);
            result.Logs.Add(log);

            if (current.Type == NodeType.End)
            {
                break;
            }

            var outgoing = workflow.Edges.Where(e => e.FromNodeId == current.Id).ToList();
            if (outgoing.Count == 0)
            {
                result.Success = false;
                result.Error = $"节点「{current.Name}」没有出边，流程无法继续。";
                break;
            }

            WorkflowEdge? chosen = null;

            if (current.Type == NodeType.Gateway)
            {
                // 有条件的边优先按顺序求值，取第一个满足的；否则回退到没有条件的默认边。
                foreach (var edge in outgoing.Where(e => !string.IsNullOrWhiteSpace(e.Condition)))
                {
                    if (EdgeConditionEvaluator.Evaluate(edge.Condition, context, out var desc))
                    {
                        chosen = edge;
                        log.RouteDecision = desc;
                        break;
                    }
                }

                chosen ??= outgoing.FirstOrDefault(e => string.IsNullOrWhiteSpace(e.Condition));

                if (chosen is null)
                {
                    result.Success = false;
                    result.Error = $"网关节点「{current.Name}」没有分支条件满足，且无默认边。";
                    break;
                }

                if (log.RouteDecision is null)
                {
                    log.RouteDecision = "无分支条件满足，走默认边";
                }
            }
            else
            {
                chosen = outgoing.First();
            }

            visitedEdgeCount++;
            if (visitedEdgeCount > maxSteps)
            {
                result.Success = false;
                result.Error = "边遍历次数过多，可能存在环路。";
                break;
            }

            current = workflow.Nodes.FirstOrDefault(n => n.Id == chosen.ToNodeId);
            if (current is null)
            {
                result.Success = false;
                result.Error = $"找不到目标节点 {chosen.ToNodeId}。";
                break;
            }
        }

        result.Success = result.Error is null;
        result.FinalContext = context;
        return result;
    }

    private static string Serialize(Dictionary<string, object> context)
    {
        return JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = false });
    }
}
