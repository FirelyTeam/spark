using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Search
{
    public interface ITerm
    {
        string Resource { get; set; }
        string Field { get; set; }
        string Operator { get; set; }
        string Value { get; set; }
        Argument Argument { get; set; }
    }
}