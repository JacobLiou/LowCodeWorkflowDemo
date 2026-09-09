namespace WorkflowRulesDemo.Web.Data;

/// <summary>持久化实体：WorkflowDefinition 以 JSON 字符串列存储 Nodes/Edges。</summary>
public class WorkflowDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string? Description { get; set; }
    public string NodesJson { get; set; } = "[]";
    public string EdgesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>持久化实体：RuleDefinition 以 JSON 字符串列存储条件树和动作集合。</summary>
public class RuleDefinitionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ConditionGroupJson { get; set; } = "{}";
    public string ActionsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
