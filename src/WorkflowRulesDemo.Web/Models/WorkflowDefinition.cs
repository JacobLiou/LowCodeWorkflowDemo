namespace WorkflowRulesDemo.Web.Models;

/// <summary>
/// RuleActivity 节点的配置：绑定的规则 Id + 输入/输出映射。
/// InputMapping: 规则输入字段名 -&gt; context 中的字段名（读取时来源）。
/// OutputMapping: 规则动作字段名 -&gt; context 中要写入的字段名（默认同名，可用于改名）。
/// </summary>
public class RuleActivityConfig
{
    public string? RuleId { get; set; }
    public Dictionary<string, string> InputMapping { get; set; } = new();
    public Dictionary<string, string> OutputMapping { get; set; } = new();
}

/// <summary>流程节点。Config 以 JSON 字符串形式持久化，便于扩展不同节点类型的配置。</summary>
public class WorkflowNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public NodeType Type { get; set; } = NodeType.Task;
    public string Name { get; set; } = string.Empty;

    /// <summary>节点配置的 JSON 表示，例如 RuleActivity 节点存 {"RuleId":"..","InputMapping":{},"OutputMapping":{}}。</summary>
    public string Config { get; set; } = "{}";

    /// <summary>便捷方法：解析 RuleActivity 节点配置。</summary>
    public RuleActivityConfig GetRuleActivityConfig()
    {
        if (string.IsNullOrWhiteSpace(Config))
        {
            return new RuleActivityConfig();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<RuleActivityConfig>(Config) ?? new RuleActivityConfig();
        }
        catch
        {
            return new RuleActivityConfig();
        }
    }

    public void SetRuleActivityConfig(RuleActivityConfig config)
    {
        Config = System.Text.Json.JsonSerializer.Serialize(config);
    }
}

/// <summary>流程连线。Condition 用于 Gateway 节点的分支路由，对 context 求值。</summary>
public class WorkflowEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FromNodeId { get; set; } = string.Empty;
    public string ToNodeId { get; set; } = string.Empty;

    /// <summary>可选路由条件表达式，例如 "context.result == 'approved'"。为空表示默认/无条件边。</summary>
    public string? Condition { get; set; }

    /// <summary>用于 UI 展示的连线说明，例如 "总监审批分支"。</summary>
    public string? Label { get; set; }
}

/// <summary>流程定义：节点 + 连线的完整拓扑。</summary>
public class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string? Description { get; set; }
    public List<WorkflowNode> Nodes { get; set; } = new();
    public List<WorkflowEdge> Edges { get; set; } = new();
}
