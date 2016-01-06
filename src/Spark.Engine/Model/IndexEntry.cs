using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Model
{
    public class IndexEntry
    {
        public IndexEntry()
        {
            Parts = new List<IndexValue>();
        }

        public IndexEntry(List<IndexValue> parts): this()
        {
            Parts.AddRange(parts);
        }

        public IndexEntry(params IndexValue[] parts): this()
        {
            Parts.AddRange(parts);
        }

        public List<IndexValue> Parts { get; set; }
    }

    public class IndexValue : ValueExpression
    {
        public IndexValue()
        {
            _values = new List<Expression>();
        }

        public IndexValue(string name): this()
        {
            Name = name;
        }

        public IndexValue(string name, List<Expression> values): this(name)
        {
            Values = values;
        }

        public IndexValue(string name, params Expression[] values): this(name)
        {
            Values = values.ToList();
        }

        public string Name { get; set; }

        private List<Expression> _values;
        public List<Expression> Values { get { return _values; } set { _values.AddRange(value); } }

        public void AddValue(Expression value)
        {
            _values.Add(value);
        }
    }
}
