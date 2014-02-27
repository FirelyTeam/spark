using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Spark.Search
{
    public class SearchException : Exception
    {
        public SearchException(string message) : base(message) { }
    }
}