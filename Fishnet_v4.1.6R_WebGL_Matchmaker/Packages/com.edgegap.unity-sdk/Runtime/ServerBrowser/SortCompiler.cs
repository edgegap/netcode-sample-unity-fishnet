using System.Collections.Generic;
using System.Linq;

namespace Edgegap.ServerBrowser
{
    public class SortCompiler
    {
        public List<SortBase> Sorts;

        public SortCompiler(List<SortBase> sorts)
        {
            if (sorts is null || sorts.Count == 0)
            {
                throw new System.Exception("Edgegap SDK SortCompiler | Sorts cannot be empty.");
            }

            Sorts = sorts;
        }

        public override string ToString()
        {
            return string.Join(",", Sorts.Select(f => f.ToString()));
        }
    }

    public abstract class SortBase
    {
        public string Field;

        public abstract override string ToString();
    }

    public class SimpleSort : SortBase
    {
        public string Direction;

        public SimpleSort(string field, string direction)
        {
            Field = field;
            Direction = direction;
        }

        public override string ToString()
        {
            return $"{Field} {Direction}";
        }
    }

    public class RankSort : SortBase
    {
        public List<string> Values = new List<string>();

        public RankSort(string field, List<string> values)
        {
            if (values is null || values.Count == 0)
            {
                throw new System.Exception("Edgegap SDK RankSort | Values cannot be empty.");
            }

            Field = field;
            Values = values;
        }

        public override string ToString()
        {
            return $"rank({Field}, {string.Join(",", Values.Select(x => $"'{x}'"))})";
        }
    }

    public static class SortDirection
    {
        public static readonly string _Ascending = "asc";
        public static readonly string _Descending = "desc";
    }
}
