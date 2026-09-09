using System.Globalization;
using WorkflowRulesDemo.Web.Models;

namespace WorkflowRulesDemo.Web.Engine;

/// <summary>规则命中详情，用于执行日志展示。</summary>
public class RuleExecutionResult
{
    public bool Matched { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();
    public List<(string Field, string Value)> AppliedActions { get; set; } = new();
}

/// <summary>
/// 轻量规则引擎：自研条件树评估器（支持 AND/OR 任意深度嵌套），
/// 不依赖 Microsoft.RulesEngine，避免与 .NET 9 的兼容性/集成成本问题，
/// 但保留同样的“条件 -&gt; 命中 -&gt; 写回 Actions”语义。
/// </summary>
public class RuleEngine
{
    /// <summary>对给定 context 执行规则：求值条件树，命中则把 Actions 写回 context。</summary>
    public RuleExecutionResult Execute(RuleDefinition rule, Dictionary<string, object> context)
    {
        var result = new RuleExecutionResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name
        };

        result.Matched = EvaluateGroup(rule.ConditionGroup, context, result.Details);

        if (result.Matched)
        {
            foreach (var action in rule.Actions)
            {
                var value = ConditionValueParser.Parse(action.Value);
                context[action.Field] = value!;
                result.AppliedActions.Add((action.Field, action.Value));
            }
        }

        return result;
    }

    private static bool EvaluateGroup(ConditionGroup group, Dictionary<string, object> context, List<string> details)
    {
        var results = new List<bool>();

        foreach (var rule in group.Rules)
        {
            var matched = EvaluateRule(rule, context);
            results.Add(matched);
            details.Add($"{rule.Field} {DescribeOperator(rule.Operator)} {rule.Value} => {(matched ? "命中" : "不命中")}");
        }

        foreach (var sub in group.Groups)
        {
            results.Add(EvaluateGroup(sub, context, details));
        }

        if (results.Count == 0)
        {
            return true;
        }

        return group.Logic == LogicOperator.And ? results.All(r => r) : results.Any(r => r);
    }

    private static bool EvaluateRule(ConditionRule rule, Dictionary<string, object> context)
    {
        context.TryGetValue(rule.Field, out var actual);
        return CompareEvaluator.Compare(actual, rule.Operator, rule.Value);
    }

    private static string DescribeOperator(CompareOperator op) => op switch
    {
        CompareOperator.Equals => "==",
        CompareOperator.NotEquals => "!=",
        CompareOperator.GreaterThan => ">",
        CompareOperator.LessThan => "<",
        CompareOperator.GreaterOrEqual => ">=",
        CompareOperator.LessOrEqual => "<=",
        CompareOperator.Contains => "contains",
        _ => op.ToString()
    };
}

/// <summary>把字符串形式的期望值解析为合适的运行时类型（数字/布尔/字符串），供比较和写回使用。</summary>
public static class ConditionValueParser
{
    public static object? Parse(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        if (bool.TryParse(raw, out var b))
        {
            return b;
        }

        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        return raw;
    }
}

/// <summary>公用的值比较逻辑，供规则条件与 Gateway 边表达式共用，保证语义一致。</summary>
public static class CompareEvaluator
{
    public static bool Compare(object? actual, CompareOperator op, string expected)
    {
        // 数值比较：只要双方都能转成 double 就走数值比较
        var actualIsNumeric = TryToDouble(actual, out var actualNum);
        var expectedIsNumeric = double.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out var expectedNum);

        switch (op)
        {
            case CompareOperator.Equals:
                if (actualIsNumeric && expectedIsNumeric)
                {
                    return Math.Abs(actualNum - expectedNum) < 1e-9;
                }
                if (actual is bool ab && bool.TryParse(expected, out var eb))
                {
                    return ab == eb;
                }
                return string.Equals(ToText(actual), expected, StringComparison.OrdinalIgnoreCase);

            case CompareOperator.NotEquals:
                return !Compare(actual, CompareOperator.Equals, expected);

            case CompareOperator.GreaterThan:
                return actualIsNumeric && expectedIsNumeric && actualNum > expectedNum;

            case CompareOperator.LessThan:
                return actualIsNumeric && expectedIsNumeric && actualNum < expectedNum;

            case CompareOperator.GreaterOrEqual:
                return actualIsNumeric && expectedIsNumeric && actualNum >= expectedNum;

            case CompareOperator.LessOrEqual:
                return actualIsNumeric && expectedIsNumeric && actualNum <= expectedNum;

            case CompareOperator.Contains:
                return ToText(actual).Contains(expected, StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }

    private static bool TryToDouble(object? value, out double result)
    {
        switch (value)
        {
            case null:
                result = 0;
                return false;
            case double d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case float f:
                result = f;
                return true;
            case bool:
                result = 0;
                return false;
            default:
                return double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }
    }

    private static string ToText(object? value) => value switch
    {
        null => string.Empty,
        bool b => b ? "true" : "false",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
}
