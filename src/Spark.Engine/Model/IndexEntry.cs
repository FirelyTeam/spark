/* 
 * Copyright (c) 2015-2018, Firely (info@fire.ly) and contributors
 * Copyright (c) 2021-2024, Incendi (info@incendi.no) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.github.com/furore-fhir/spark/master/LICENSE
 */

using Spark.Search;
using System.Collections.Generic;
using System.Linq;

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
