﻿using Spark.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Engine.Model
{
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

    public static class IndexValueExtensions
    {
        public static IEnumerable<IndexValue> IndexValues(this IndexValue root)
        {
            return root.Values.Where(v => v is IndexValue).Select(v => (IndexValue)v);
        }
    }
}
