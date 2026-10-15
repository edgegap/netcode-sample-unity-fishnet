using System.Collections.Generic;
using System.Linq;

namespace Edgegap.ServerBrowser
{
    using FloatOperator = IntOperator;

    public class FilterCompiler
    {
        public List<FilterBase> Filters;

        public FilterCompiler(List<FilterBase> filters)
        {
            if (filters is null || filters.Count == 0)
            {
                throw new System.Exception("Edgegap SDK FilterCompiler | Filters cannot be empty.");
            }

            Filters = filters;
        }

        public override string ToString()
        {
            return string.Join(" and ", Filters.Select(f => f.ToString()));
        }
    }

    public abstract class FilterBase
    {
        public string Field;
        public abstract override string ToString();
    }

    public abstract class Filter<T> : FilterBase
    {
        public string Operator;
        public T Value;

        public override string ToString()
        {
            return $"{Field} {Operator} {Value}";
        }
    }

    public class StringFilter : Filter<string>
    {
        public override string ToString()
        {
            return $"{Field} {Operator} '{Value}'";
        }
    }

    public class IntFilter : Filter<int> { }

    public class FloatFilter : Filter<float> { }

    public class BoolFilter : Filter<bool> { }

    public class LiteralFilter : FilterBase
    {
        public List<string> Value;

        public override string ToString()
        {
            return $"{Field} in ({string.Join(",", Value.Select(f => $"'{f}'"))})";
        }
    }

    public static class StringOperator
    {
        public static readonly string _Equals = "eq";
        public static readonly string _NotEquals = "ne";
        public static readonly string _LessThan = "lt";
        public static readonly string _LessThanOrEqualTo = "le";
        public static readonly string _GreaterThan = "gt";
        public static readonly string _GreaterThanOrEqualTo = "ge";
        public static readonly string _Contains = "contains";
    }

    public static class IntOperator
    {
        public static readonly string _Equals = "eq";
        public static readonly string _NotEquals = "ne";
        public static readonly string _LessThan = "lt";
        public static readonly string _LessThanOrEqualTo = "le";
        public static readonly string _GreaterThan = "gt";
        public static readonly string _GreaterThanOrEqualTo = "ge";
    }

    public static class BoolOperator
    {
        public static readonly string _Equals = "eq";
        public static readonly string _NotEquals = "ne";
    }
}
