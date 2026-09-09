using System.Text.RegularExpressions;
using WorkflowRulesDemo.Web.Models;

namespace WorkflowRulesDemo.Web.Engine;

/// <summary>
/// 解析并对 context 求值 Gateway 边上的简单表达式，形如：
///   context.result == 'approved'
///   context.days > 3
/// 支持 ==, !=, >, <, >=, <=, contains 运算符，值可以是带引号的字符串、数字或布尔。
/// </summary>
public static class EdgeConditionEvaluator
{
    private static readonly Regex ExpressionPattern = new(
        @"^\s*context\.(?<field>[\w\.]+)\s*(?<op>==|!=|>=|<=|>|<|contains)\s*(?<value>.+?)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool Evaluate(string? expression, Dictionary<string, object> context, out string description)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            description = "(无条件/默认分支)";
            return true;
        }

        var match = ExpressionPattern.Match(expression);
        if (!match.Success)
        {
            description = $"无法解析的条件表达式: {expression}";
            return false;
        }

        var field = match.Groups["field"].Value;
        var opText = match.Groups["op"].Value.ToLowerInvariant();
        var rawValue = match.Groups["value"].Value.Trim().Trim('\'', '"');

        var op = opText switch
        {
            "==" => CompareOperator.Equals,
            "!=" => CompareOperator.NotEquals,
            ">" => CompareOperator.GreaterThan,
            "<" => CompareOperator.LessThan,
            ">=" => CompareOperator.GreaterOrEqual,
            "<=" => CompareOperator.LessOrEqual,
            "contains" => CompareOperator.Contains,
            _ => CompareOperator.Equals
        };

        context.TryGetValue(field, out var actual);
        var matched = CompareEvaluator.Compare(actual, op, rawValue);
        description = $"{expression} (context.{field}={(actual ?? "<null>")}) => {(matched ? "满足" : "不满足")}";
        return matched;
    }
}
