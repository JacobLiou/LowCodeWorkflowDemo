namespace WorkflowRulesDemo.Web.Models;

/// <summary>单条比较条件：字段 - 运算符 - 值。</summary>
public class ConditionRule
{
    public string Field { get; set; } = string.Empty;
    public CompareOperator Operator { get; set; } = CompareOperator.Equals;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// 嵌套条件组：AND/OR 逻辑下可以包含子条件组和/或叶子条件规则，
/// 用于支持规则条件构建器里任意深度的 AND/OR 嵌套。
/// </summary>
public class ConditionGroup
{
    public LogicOperator Logic { get; set; } = LogicOperator.And;

    public List<ConditionRule> Rules { get; set; } = new();

    public List<ConditionGroup> Groups { get; set; } = new();
}

/// <summary>规则命中后写回 context 的动作：字段 = 值。</summary>
public class RuleAction
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>规则定义：条件树 + 命中后的动作集合。</summary>
public class RuleDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ConditionGroup ConditionGroup { get; set; } = new();
    public List<RuleAction> Actions { get; set; } = new();
}
