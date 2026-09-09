namespace WorkflowRulesDemo.Web.Models;

/// <summary>流程节点类型。</summary>
public enum NodeType
{
    Start,
    Task,
    RuleActivity,
    Gateway,
    End
}

/// <summary>条件组合逻辑。</summary>
public enum LogicOperator
{
    And,
    Or
}

/// <summary>条件比较运算符。</summary>
public enum CompareOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    Contains
}
